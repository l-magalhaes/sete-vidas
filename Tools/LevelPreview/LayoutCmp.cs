// Compara o ForestLayout (o que "Floresta: gerar as cenas" monta) com as cenas salvas: o que foi apagado, movido ou
// acrescentado à mão, e os desenhos (filhos) arrastados sozinhos para longe do objeto. O pivô de cada tipo de objeto
// fica num lugar diferente do ponto do código: o deslocamento normal é a mediana daquele tipo em TODAS as cenas (os
// ajustes à mão são minoria). Zumbis, horda, aparições da Isyathy, saídas e atalhos não entram (a cena guarda de outro
// jeito). Usado por comparar_floresta.ps1. C# 5.
using System; using System.Collections.Generic; using System.Globalization; using System.Linq; using System.Text;
using SeteVidas.EditorTools;

public static class LayoutCmp
{
    static string F(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

    static readonly HashSet<string> Skip = new HashSet<string> {
        "infected", "horde", "sighting", "bark", "exit", "entrance", "slope", "sc_vine", "sc_rolled", "sc_lever", "sc_block",
        "arena", "bridge", "debris" };

    static IEnumerable<string> Names(ForestLayout.Obj o)
    {
        yield return o.Name;
        if (o.Infected) yield return o.Name + " (infectado)";
        if (o.Kind == "sign") yield return "Placa: " + o.Text;
        if (o.Kind == "hint") yield return "Dica: " + o.Text;
        if (o.Kind == "prilore" && o.Text != null) yield return o.Text;
        if (o.Kind == "sanctuary") yield return "Santuário (" + o.Id + ")";
    }

    static string Key(ForestLayout.Obj o, bool fine) { return o.Kind + "|" + (o.Text ?? "") + (fine ? "|" + o.W + "x" + o.H + "|" + o.Facing + "|" + o.A : ""); }

    class Match { public string Scene; public ForestLayout.Obj O; public long Id; public SceneCmp.Scene S; }

    /// <summary>Ponto do código: galhos (de mão única, que balança, podre) têm o X na ponta esquerda e o pivô no meio.</summary>
    static double CodeX(ForestLayout.Obj o) { return o.Kind == "branch" || o.Kind == "sway" || o.Kind == "rotten" ? o.X + o.W * 0.5 : o.X; }

    public static string Run(string[] scenePaths, string[] sceneNames)
    {
        var all = new List<Match>();
        var report = new Dictionary<string, StringBuilder>();
        var loaded = new List<Tuple<string, SceneCmp.Scene, HashSet<long>, ForestLayout.Region>>();
        for (int i = 0; i < scenePaths.Length; i++)
        {
            var sb = new StringBuilder(); report[sceneNames[i]] = sb;
            var reg = ForestLayout.Get(sceneNames[i]);
            var s = SceneCmp.Load(scenePaths[i]);
            var byRoom = new Dictionary<string, List<long>>();
            foreach (var id in s.tr.Keys)
            {
                long f = s.tr[id].Father;
                if (f == 0 || !s.tr.ContainsKey(f)) continue;
                List<long> l; string pn = s.NameOf(f);
                if (!byRoom.TryGetValue(pn, out l)) byRoom[pn] = l = new List<long>(); l.Add(id);
            }
            var used = new HashSet<long>();
            foreach (var o in reg.Objects)
            {
                if (Skip.Contains(o.Kind)) continue;
                string room = o.Name.Contains(":") ? o.Name.Substring(0, o.Name.IndexOf(':')) : "";
                var names = new HashSet<string>(Names(o));
                long best = 0; double bd = 1e9; List<long> cands;
                if (byRoom.TryGetValue(room, out cands))
                    foreach (var id in cands)
                    {
                        if (used.Contains(id) || !names.Contains(s.NameOf(id))) continue;
                        var w = s.World(id); double d = Math.Abs(w[0] - o.X) + Math.Abs(w[1] - o.Y);
                        if (d < bd) { bd = d; best = id; }
                    }
                if (best == 0) { sb.AppendLine("SUMIU    " + o.Name + " [" + o.Kind + "]" + (o.Kind == "sign" ? " \"" + o.Text + "\"" : "") + " (" + F(o.X) + ", " + F(o.Y) + ")"); continue; }
                used.Add(best); all.Add(new Match { Scene = sceneNames[i], O = o, Id = best, S = s });
            }
            loaded.Add(Tuple.Create(sceneNames[i], s, used, reg));
        }
        // deslocamento normal: mediana do tipo fino (largura, lado...) com 3 ou mais; senão o tipo; senão nenhum
        Func<IEnumerable<Match>, int, double> med = (ms, k) => { var v = ms.Select(m => m.S.World(m.Id)[k] - (k == 0 ? CodeX(m.O) : m.O.Y)).OrderBy(x => x).ToList(); return v[v.Count / 2]; };
        var fineG = all.GroupBy(m => Key(m.O, true)).ToDictionary(g => g.Key, g => g.ToList());
        var coarseG = all.GroupBy(m => Key(m.O, false)).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var m in all)
        {
            var g = fineG[Key(m.O, true)]; if (g.Count < 3) g = coarseG[Key(m.O, false)];
            double ox = g.Count >= 3 ? med(g, 0) : 0, oy = g.Count >= 3 ? med(g, 1) : 0;
            var w = m.S.World(m.Id); double dx = w[0] - CodeX(m.O) - ox, dy = w[1] - m.O.Y - oy;
            var sb = report[m.Scene];
            if (Math.Abs(dx) > 0.15 || Math.Abs(dy) > 0.15)
                sb.AppendLine("MOVEU    " + m.O.Name + " [" + m.O.Kind + (m.O.Text != null && m.O.Kind.StartsWith("ruin") ? " " + m.O.Text : "") + "] código (" + F(m.O.X) + ", " + F(m.O.Y) + ") cena (" + F(w[0] - ox) + ", " + F(w[1] - oy) + ") d=(" + F(dx) + ", " + F(dy) + ")" + (g.Count < 3 ? " (tipo raro: confira)" : ""));
            if (!m.S.tr[m.Id].Active) sb.AppendLine("DESLIGOU " + m.O.Name + " [" + m.O.Kind + "]");
        }
        // filhos arrastados: comparados com os do mesmo tipo fino em todas as cenas
        var kids = new List<Tuple<string, Match, long>>();
        foreach (var m in all)
        {
            if (m.O.Kind == "pool" || m.O.Kind == "shadow") continue;   // água em tiles; árvore e sombra dependem da altura até o chão
            foreach (var id in m.S.tr.Keys) if (m.S.tr[id].Father == m.Id) kids.Add(Tuple.Create(Key(m.O, true) + "|" + m.S.NameOf(id), m, id));
        }
        foreach (var g in kids.GroupBy(k => k.Item1))
        {
            var list = g.ToList(); if (list.Count < 2) continue;
            var xs = list.Select(k => k.Item2.S.tr[k.Item3].X).OrderBy(v => v).ToList();
            var ys = list.Select(k => k.Item2.S.tr[k.Item3].Y).OrderBy(v => v).ToList();
            foreach (var k in list)
            {
                double dx = k.Item2.S.tr[k.Item3].X - xs[xs.Count / 2], dy = k.Item2.S.tr[k.Item3].Y - ys[ys.Count / 2];
                if (Math.Abs(dx) > 0.3 || Math.Abs(dy) > 0.3)
                { var w = k.Item2.S.World(k.Item3); report[k.Item2.Scene].AppendLine("DESENHO  " + k.Item2.O.Name + " / " + k.Item2.S.NameOf(k.Item3) + " arrastado d=(" + F(dx) + ", " + F(dy) + ") -> mundo (" + F(w[0]) + ", " + F(w[1]) + ")"); }
            }
        }
        // acrescentados: filhos de sala com nome de objeto que não vieram do layout, e soltos na raiz
        foreach (var t in loaded)
        {
            var s = t.Item2; var used = t.Item3; var rooms = new HashSet<string>(t.Item4.Rooms.Select(r => r.Id));
            foreach (var id in s.tr.Keys)
            {
                if (used.Contains(id)) continue;
                string n = s.NameOf(id); long f = s.tr[id].Father;
                string pn = f != 0 && s.tr.ContainsKey(f) ? s.NameOf(f) : "";
                bool roomChild = rooms.Contains(pn), rootLoose = f == 0 || !s.tr.ContainsKey(f);
                if (!roomChild && !rootLoose) continue;
                if (n.Contains("saída") || n.Contains("chegada") || n.Contains("passagem") || n.Contains("modelo") || n.Contains("trilho") || n.StartsWith("Luz")) continue;
                bool looksLikeObj = rooms.Any(r => n.StartsWith(r + ":")) || n.Contains(" (1)") || n.Contains(" (2)") || n.Contains(" (3)");
                if (!looksLikeObj) continue;
                var w = s.World(id);
                report[t.Item1].AppendLine("NOVO     " + (rootLoose ? "(solto na raiz) " : pn + " / ") + n + " (" + F(w[0]) + ", " + F(w[1]) + ")");
            }
        }
        var outSb = new StringBuilder();
        foreach (var n in sceneNames) { outSb.AppendLine("===== " + n + " ====="); outSb.AppendLine(report[n].ToString()); }
        return outSb.ToString();
    }
}
