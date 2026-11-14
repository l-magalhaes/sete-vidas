// Desenha o graybox da Floresta (Assets/_Game/Editor/ForestLayout.cs) num PNG por cena, sem abrir o Unity, com o
// resultado do ForestReach por cima: chão alcançável (verde), fora de alcance (cinza), softlock (vermelho).
// C# 5. Uso: Tools/LevelPreview/checar_floresta.ps1
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.EditorTools;

public static class ForestPreview
{
    public static string Run(string outDir, int S, bool verbose)
    {
        Directory.CreateDirectory(outDir);
        var reports = ForestReach.CheckAll();
        foreach (var rep in reports)
        {
            var reg = ForestLayout.Get(rep.Scene);
            if (reg == null) continue;   // o relatório do mapa (salas sobrepostas)
            Render(reg, rep, Path.Combine(outDir, "floresta_" + rep.Scene + ".png"), S);
        }
        Overview(Path.Combine(outDir, "floresta_mapa.png"), 2);
        return ForestReach.Format(reports, verbose);
    }

    /// <summary>O anel inteiro (todas as cenas da floresta), sem o alcance: para ver como as salas se encaixam.</summary>
    static void Overview(string path, int S)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        var regs = new List<ForestLayout.Region>();
        foreach (var reg in ForestLayout.Regions())
        {
            if (reg.MapRegion != ForestLayout.ForestMap) continue;
            regs.Add(reg);
            int a, b, c, d;
            reg.Bounds(out a, out b, out c, out d);
            x0 = Math.Min(x0, a); y0 = Math.Min(y0, b); x1 = Math.Max(x1, c); y1 = Math.Max(y1, d);
        }
        x0 -= 4; y0 -= 4; x1 += 4; y1 += 4;
        int w = x1 - x0 + 1, h = y1 - y0 + 1;
        var tint = new[] { "#5a4a3a", "#3a5a3a", "#2e3a5c", "#4a3a5c", "#5c3a3a", "#5c5a3a" };
        using (var bmp = new Bitmap(w * S, h * S))
        using (var g = Graphics.FromImage(bmp))
        using (var bold = new Font("Consolas", 8, FontStyle.Bold))
        {
            g.Clear(ColorTranslator.FromHtml("#0d1020"));
            for (int k = 0; k < regs.Count; k++)
            {
                var reg = regs[k];
                var solid = new Dictionary<long, bool>();
                foreach (var t in reg.Tiles())
                {
                    if (t.Map != ForestLayout.Map.Ground) continue;
                    for (int x = t.X0; x <= t.X1; x++)
                        for (int y = t.Y0; y <= t.Y1; y++) solid[(long)x * 100000 + y] = !t.Clear;
                }
                using (var b = new SolidBrush(ColorTranslator.FromHtml(tint[k % tint.Length])))
                    foreach (var kv in solid)
                    {
                        if (!kv.Value) continue;
                        long xx = kv.Key / 100000, yy = kv.Key % 100000;
                        if (yy > 50000) { xx += 1; yy -= 100000; }       // negativos
                        else if (yy < -50000) { xx -= 1; yy += 100000; }
                        g.FillRectangle(b, (int)(xx - x0) * S, (int)(y1 - yy) * S, S, S);
                    }
                foreach (var r in reg.Rooms)
                {
                    using (var pen = new Pen(Color.FromArgb(120, 255, 230, 150), 1))
                        g.DrawRectangle(pen, (r.X - x0) * S, (y1 - (r.Y + r.H - 1)) * S, r.W * S, r.H * S);
                    g.DrawString(r.Id, bold, Brushes.Khaki, (r.X - x0 + 1) * S, (y1 - (r.Y + r.H - 2)) * S);
                }
                foreach (var o in reg.Objects)
                    if (o.Kind == "sc_vine" || o.Kind == "sc_block")
                        using (var b = new SolidBrush(Color.Violet))
                            g.FillRectangle(b, (int)(o.X - x0) * S, (int)(y1 - (o.Y + o.H - 1)) * S, Math.Max(S, (int)(o.W * S)), (int)(o.H * S));
            }
            bmp.Save(path);
        }
    }

    static void Render(ForestLayout.Region reg, ForestReach.Report rep, string path, int S)
    {
        int minX = rep.MinX, minY = rep.MinY, w = rep.W, h = rep.H;
        var ground = new bool[w, h]; var vine = new bool[w, h]; var back = new bool[w, h];
        foreach (var t in reg.Tiles())
            for (int x = Math.Max(minX, t.X0); x <= Math.Min(minX + w - 1, t.X1); x++)
                for (int y = Math.Max(minY, t.Y0); y <= Math.Min(minY + h - 1, t.Y1); y++)
                {
                    int i = x - minX, j = y - minY;
                    if (t.Map == ForestLayout.Map.Ground) ground[i, j] = !t.Clear;
                    else if (t.Map == ForestLayout.Map.Climbable) vine[i, j] = !t.Clear;
                    else back[i, j] = !t.Clear;
                }
        Func<float, int> PX = x => (int)((x - minX) * S);
        Func<float, int> PY = y => (int)((minY + h - y) * S);

        using (var bmp = new Bitmap(w * S, h * S))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 7))
        using (var bold = new Font("Consolas", 9, FontStyle.Bold))
        {
            g.Clear(ColorTranslator.FromHtml("#0d1020"));
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    Color c = Color.Empty;
                    if (back[i, j]) c = ColorTranslator.FromHtml("#242233");
                    if (ground[i, j]) c = ColorTranslator.FromHtml("#4a4658");
                    if (vine[i, j]) c = ColorTranslator.FromHtml("#3f8a3a");
                    if (c != Color.Empty) using (var b = new SolidBrush(c)) g.FillRectangle(b, i * S, (h - 1 - j) * S, S, S);
                    byte st = rep.Stand[i, j];
                    if (st != 0)
                    {
                        var sc = st == 1 ? Color.FromArgb(140, 90, 220, 110) : st == 2 ? Color.FromArgb(110, 150, 150, 150) : Color.FromArgb(220, 255, 40, 40);
                        using (var b = new SolidBrush(sc)) g.FillRectangle(b, i * S + 1, (h - 1 - j) * S + S / 2, S - 2, S / 2);
                    }
                }
            // grade a cada 10
            using (var pen = new Pen(Color.FromArgb(28, 255, 255, 255)))
            {
                for (int x = (int)Math.Ceiling(minX / 10.0) * 10; x < minX + w; x += 10) g.DrawLine(pen, PX(x), 0, PX(x), h * S);
                for (int y = (int)Math.Ceiling(minY / 10.0) * 10; y < minY + h; y += 10) g.DrawLine(pen, 0, PY(y), w * S, PY(y));
            }
            // salas
            foreach (var r in reg.Rooms)
            {
                using (var pen = new Pen(r.Optional ? Color.FromArgb(160, 200, 160, 255) : Color.FromArgb(170, 255, 230, 150), 1))
                    g.DrawRectangle(pen, PX(r.X), PY(r.Y + r.H), r.W * S, r.H * S);
                g.DrawString(r.Id + " " + r.Name, bold, Brushes.Khaki, PX(r.X + 1.2f), PY(r.Y + r.H - 1.2f));
            }
            foreach (var o in reg.Objects) DrawObj(g, o, PX, PY, S, font);
            bmp.Save(path);
        }
    }

    static void DrawObj(Graphics g, ForestLayout.Obj o, Func<float, int> PX, Func<float, int> PY, int S, Font font)
    {
        int x = PX(o.X), y = PY(o.Y);
        switch (o.Kind)
        {
            case "branch": case "sway": case "rotten":
            {
                var c = o.Kind == "sway" ? Color.SandyBrown : o.Kind == "rotten" ? Color.Gray : Color.Peru;
                using (var b = new SolidBrush(c)) g.FillRectangle(b, x, y, (int)(o.W * S), Math.Max(2, S / 3));
                break;
            }
            case "leaf": using (var b = new SolidBrush(Color.YellowGreen)) g.FillRectangle(b, PX(o.X - 1.5f), y, 3 * S, Math.Max(2, S / 3)); break;
            case "mushroom": using (var b = new SolidBrush(Color.Turquoise)) g.FillEllipse(b, PX(o.X - 1f), PY(o.Y + 1f), 2 * S, S); break;
            case "cactus": using (var b = new SolidBrush(Color.MediumSeaGreen)) g.FillEllipse(b, x - S / 2, y - S / 2, S, S);
                using (var p = new Pen(Color.Turquoise, 2)) g.DrawLine(p, x - S / 3, y - S / 2, x + S / 3, y - S / 2); break;
            case "spikes": case "gspikes": using (var b = new SolidBrush(Color.Magenta)) g.FillRectangle(b, PX(o.X - o.W * 0.5f), y - S / 2, (int)(o.W * S), S / 2); break;
            case "trunk": using (var b = new SolidBrush(Color.Orange)) g.FillRectangle(b, x - S / 2, y - 4 * S, S, 4 * S); break;
            case "chest": using (var b = new SolidBrush(Color.Gold)) g.FillRectangle(b, x - S, y - S, 2 * S, S); g.DrawString(o.Id, font, Brushes.Gold, x - S, y - 2 * S - 4); break;
            case "coin": using (var b = new SolidBrush(Color.Yellow)) g.FillEllipse(b, x - 2, y - 2, 4, 4); break;
            case "sanctuary": using (var b = new SolidBrush(Color.Cyan)) g.FillRectangle(b, x - S, y - 2 * S, 2 * S, 2 * S); g.DrawString(o.Id, font, Brushes.Cyan, x - S, y - 3 * S - 4); break;
            case "sentinel":
                using (var p = new Pen(Color.FromArgb(200, 255, 70, 70), 2)) g.DrawLine(p, PX(o.A), y - S / 2, PX(o.B), y - S / 2);
                using (var b = new SolidBrush(Color.Red)) g.FillRectangle(b, x - S / 2, y - 2 * S, S, 2 * S); break;
            case "archer": using (var b = new SolidBrush(Color.Crimson)) g.FillRectangle(b, x - S / 2, y - 2 * S, S, 2 * S); g.DrawString("besta", font, Brushes.Crimson, x - S, y - 3 * S); break;
            case "bat": using (var b = new SolidBrush(Color.MediumPurple)) g.FillEllipse(b, x - S / 2, y, S, S); break;
            case "dry": case "sprout": using (var b = new SolidBrush(Color.SaddleBrown)) g.FillRectangle(b, x - S / 2, y - (int)(5.5f * S), S, (int)(5.5f * S)); break;
            case "plant": using (var b = new SolidBrush(Color.IndianRed)) g.FillEllipse(b, x - S / 2, y - S, S, S); break;
            case "shadow": using (var b = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillRectangle(b, PX(o.X - o.W * 0.5f), PY(o.Y + o.H * 0.5f), (int)(o.W * S), (int)(o.H * S));
                using (var p = new Pen(Color.FromArgb(120, 120, 120, 200))) g.DrawRectangle(p, PX(o.X - o.W * 0.5f), PY(o.Y + o.H * 0.5f), (int)(o.W * S), (int)(o.H * S)); break;
            case "bush": using (var b = new SolidBrush(Color.DarkGreen)) g.FillRectangle(b, x - S, y - S, 2 * S, S); break;
            case "saw": case "gsaw": using (var p = new Pen(Color.Silver, 2)) g.DrawLine(p, PX(o.A), y, PX(o.B), y); using (var b = new SolidBrush(Color.Silver)) g.FillEllipse(b, x - S / 2, y - S / 2, S, S); break;
            case "blade": using (var p = new Pen(Color.Silver, 1)) g.DrawLine(p, x, y, x, PY(o.Y - o.H)); using (var b = new SolidBrush(Color.LightGray)) g.FillEllipse(b, x - S, PY(o.Y - o.H) - S / 2, 2 * S, S); break;
            case "tripwire": using (var b = new SolidBrush(Color.Yellow)) g.FillRectangle(b, x - S, y - 2, 2 * S, 2); break;
            case "spears": case "flame": case "launcher": using (var b = new SolidBrush(Color.OrangeRed)) g.FillRectangle(b, x - S / 2, y - S, S, S); break;
            case "pool": using (var b = new SolidBrush(Color.FromArgb(120, 80, 140, 220))) g.FillRectangle(b, PX(o.X - o.W * 0.5f), y - S, (int)(o.W * S), S); break;
            case "entrance": using (var b = new SolidBrush(Color.White)) g.FillPolygon(b, new[] { new Point(x, y - 2 * S), new Point(x + o.Facing * S, y - S), new Point(x, y) }); g.DrawString(o.Id, font, Brushes.White, x + 2, y - 3 * S); break;
            case "exit": using (var p = new Pen(string.IsNullOrEmpty(o.Link) ? Color.Orange : Color.Plum, 2)) g.DrawRectangle(p, PX(o.X - o.W * 0.5f), PY(o.Y + o.H * 0.5f), (int)(o.W * S), (int)(o.H * S));
                g.DrawString("→" + o.Text.Replace("Forest_", "") + "/" + o.Id, font, Brushes.Orange, PX(o.X - o.W * 0.5f), PY(o.Y + o.H * 0.5f) - 11); break;
            case "shortcut": using (var b = new SolidBrush(Color.Plum)) g.FillRectangle(b, x - S / 2, y - 2 * S, S, 2 * S); g.DrawString("atalho→" + o.Id, font, Brushes.Plum, x - S, y - 3 * S); break;
            case "sign": g.DrawString("»", font, Brushes.White, x - 3, y - 10); break;
            case "slope":
            {
                int x1 = PX(o.X + o.W), top = PY(o.Y + o.H), hx = o.Facing > 0 ? x1 : x;
                var c = o.H > o.W + 0.01f ? Color.FromArgb(200, 150, 110, 90) : Color.FromArgb(220, 110, 104, 132);
                using (var b = new SolidBrush(c)) g.FillPolygon(b, new[] { new Point(x, y), new Point(x1, y), new Point(hx, top) });
                break;
            }
            case "infected":
                using (var p = new Pen(Color.FromArgb(200, 140, 220, 90), 2)) g.DrawLine(p, PX(o.A), y - S / 2, PX(o.B), y - S / 2);
                using (var b = new SolidBrush(o.Value > 0 ? Color.SteelBlue : Color.OliveDrab)) g.FillRectangle(b, x - S / 2, y - 2 * S, S, 2 * S);
                break;
            case "horde": using (var b = new SolidBrush(Color.FromArgb(160, 140, 220, 90))) g.FillRectangle(b, x - S / 2, y - 2 * S, S, 2 * S); break;
            case "bridge": using (var b = new SolidBrush(Color.FromArgb(200, 120, 112, 140))) g.FillRectangle(b, x, y, (int)(o.W * S), S);
                g.DrawString("desaba", font, Brushes.Orange, x, y + S); break;
            case "torch": using (var b = new SolidBrush(Color.Orange)) g.FillEllipse(b, x - 3, y - 2 * S, 6, 6); break;
            case "hook":
                using (var p = new Pen(Color.Gold, 2)) g.DrawEllipse(p, x - S / 3, y - S / 3, 2 * S / 3, 2 * S / 3);
                break;
            case "snake":
                if (o.Value > 0) using (var p = new Pen(Color.FromArgb(200, 120, 230, 60), 1)) g.DrawLine(p, PX(o.A), y - 2, PX(o.B), y - 2);
                using (var b = new SolidBrush(Color.LawnGreen)) g.FillEllipse(b, x - S, y - S / 2, 2 * S, S / 2 + 1);
                break;
            case "vsaw": using (var p = new Pen(Color.Silver, 2)) g.DrawLine(p, x, PY(o.A), x, PY(o.B)); using (var b = new SolidBrush(Color.Silver)) g.FillEllipse(b, x - S / 2, PY(o.A) - S / 2, S, S); break;
            case "switch": using (var b = new SolidBrush(Color.DodgerBlue)) g.FillRectangle(b, x - S / 2, y - S / 2, S, S / 2); break;
            case "key": using (var b = new SolidBrush(Color.Gold)) g.FillEllipse(b, x - S / 2, y - S / 2, S, S); g.DrawString("chave", font, Brushes.Gold, x - S, y - 2 * S); break;
            case "residence": using (var b = new SolidBrush(Color.Sienna)) g.FillRectangle(b, x - S, y - 3 * S, 2 * S, 3 * S); g.DrawString("casa", font, Brushes.Sienna, x - S, y - 4 * S); break;
            case "arena": using (var p = new Pen(Color.FromArgb(160, 255, 120, 60), 1)) g.DrawRectangle(p, PX(o.X - o.W * 0.5f), PY(o.Y + o.H * 0.5f), (int)(o.W * S), (int)(o.H * S)); g.DrawString("arena", font, Brushes.OrangeRed, PX(o.X - o.W * 0.5f) + 2, PY(o.Y + o.H * 0.5f) + 2); break;
            case "arena_gate": using (var b = new SolidBrush(Color.FromArgb(160, 200, 200, 210))) g.FillRectangle(b, x - S / 2, y - 4 * S, S, 4 * S); break;
            case "sc_vine": using (var b = new SolidBrush(Color.FromArgb(150, 200, 120, 255))) g.FillRectangle(b, PX(o.X), PY(o.Y + o.H), S, (int)(o.H * S)); break;
            case "sc_block": using (var b = new SolidBrush(Color.FromArgb(220, 190, 120, 255))) g.FillRectangle(b, PX(o.X), PY(o.Y + o.H), (int)(o.W * S), (int)(o.H * S)); break;
            case "sc_rolled": using (var b = new SolidBrush(Color.Orchid)) g.FillEllipse(b, x - S / 2, y - S / 2, S, S); break;
            case "sc_lever":
                using (var b = new SolidBrush(o.Opens ? Color.Violet : Color.Plum)) g.FillRectangle(b, x - S / 3, y - 2 * S, 2 * S / 3, 2 * S);
                g.DrawString(o.Opens ? "abre " + o.Flag : "fechado", font, Brushes.Violet, x - S, y - 3 * S - 4);
                break;
        }
    }
}
