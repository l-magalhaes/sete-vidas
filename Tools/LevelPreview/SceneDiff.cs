using System; using System.Collections.Generic; using System.IO; using System.Text; using System.Text.RegularExpressions; using System.Globalization;
using SeteVidas.EditorTools;
public static class SceneDiff {
  class T { public long Go; public double X, Y; public long Father; public string Name; }
  static string Clean(string s) {
    s = s.Trim();
    if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') {
      s = s.Substring(1, s.Length - 2);
      s = Regex.Replace(s, @"\\x([0-9A-Fa-f]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
      s = Regex.Replace(s, @"\\u([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
      s = s.Replace("\\\"", "\"");
    } else s = s.Trim('\'');
    return s;
  }
  static double D(string s) { return double.Parse(s, CultureInfo.InvariantCulture); }
  static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

  static Dictionary<long, T> tr = new Dictionary<long, T>();
  static Dictionary<long, string> goName = new Dictionary<long, string>();
  static string NameOf(long id) { T t = tr[id]; if (t.Name != null) return t.Name; string n; return goName.TryGetValue(t.Go, out n) ? n : "?"; }
  static double[] World(long id) { T t = tr[id]; double x = t.X, y = t.Y; long f = t.Father; int g = 0; while (f != 0 && tr.ContainsKey(f) && g++ < 30) { x += tr[f].X; y += tr[f].Y; f = tr[f].Father; } return new[] { x, y }; }
  static string ParentName(long id) { long f = tr[id].Father; return f != 0 && tr.ContainsKey(f) ? NameOf(f) : ""; }

  public static string Run(string scenePath) {
    tr.Clear(); goName.Clear();
    var text = File.ReadAllText(scenePath);
    var blocks = Regex.Split(text, @"\n--- ");
    var prefabs = new Dictionary<long, Dictionary<string, string>>(); var prefabParent = new Dictionary<long, long>(); var strippedTr = new Dictionary<long, long>();
    foreach (var b in blocks) {
      var head = Regex.Match(b, @"^!u!(\d+) &(-?\d+)( stripped)?"); if (!head.Success) continue;
      int cls = int.Parse(head.Groups[1].Value); long id = long.Parse(head.Groups[2].Value); bool stripped = head.Groups[3].Success;
      if (cls == 1 && !stripped) { var m = Regex.Match(b, @"m_Name: (.*)"); if (m.Success) goName[id] = Clean(m.Groups[1].Value); }
      else if (cls == 4) {
        if (stripped) { var p = Regex.Match(b, @"m_PrefabInstance: \{fileID: (-?\d+)\}"); if (p.Success) strippedTr[id] = long.Parse(p.Groups[1].Value); continue; }
        var t = new T(); t.Go = long.Parse(Regex.Match(b, @"m_GameObject: \{fileID: (-?\d+)\}").Groups[1].Value);
        var pos = Regex.Match(b, @"m_LocalPosition: \{x: ([^,]+), y: ([^,]+),"); t.X = D(pos.Groups[1].Value); t.Y = D(pos.Groups[2].Value);
        var f = Regex.Match(b, @"m_Father: \{fileID: (-?\d+)\}"); t.Father = f.Success ? long.Parse(f.Groups[1].Value) : 0;
        tr[id] = t;
      }
      else if (cls == 1001) {
        var mods = new Dictionary<string, string>();
        foreach (Match mm in Regex.Matches(b, @"propertyPath: ([^\n]+)\n\s+value: ([^\n]*)")) { string k = mm.Groups[1].Value.Trim(); if (!mods.ContainsKey(k)) mods[k] = mm.Groups[2].Value.Trim(); }
        prefabs[id] = mods; var tp = Regex.Match(b, @"m_TransformParent: \{fileID: (-?\d+)\}"); prefabParent[id] = tp.Success ? long.Parse(tp.Groups[1].Value) : 0;
      }
    }
    foreach (var kv in strippedTr) { long p = kv.Value; Dictionary<string, string> mods; if (!prefabs.TryGetValue(p, out mods)) continue;
      var t = new T(); string v; t.X = mods.TryGetValue("m_LocalPosition.x", out v) ? D(v) : 0; t.Y = mods.TryGetValue("m_LocalPosition.y", out v) ? D(v) : 0;
      t.Father = prefabParent[p]; t.Name = mods.TryGetValue("m_Name", out v) ? Clean(v) : "(prefab)"; tr[kv.Key] = t; }

    var byName = new Dictionary<string, List<long>>();
    foreach (var id in tr.Keys) { string n = NameOf(id); List<long> l; if (!byName.TryGetValue(n, out l)) byName[n] = l = new List<long>(); l.Add(id); }
    var used = new HashSet<long>();
    Func<string, string, double, double, long> nearest = (name, parent, x, y) => {
      List<long> l; long best = 0; double bd = 1e9; if (!byName.TryGetValue(name, out l)) return 0;
      foreach (var id in l) { if (ParentName(id) != parent || used.Contains(id)) continue; var w = World(id); double d = Math.Abs(w[0] - x) + Math.Abs(w[1] - y); if (d < bd) { bd = d; best = id; } }
      return best; };

    var sb = new StringBuilder();
    sb.AppendLine("== LAYOUT (Fortaleza) ==");
    foreach (var o in FortressLayout.Objects()) {
      string sn = o.Kind == "hint" ? "Dica: " + o.Text : o.Kind == "trigger" ? "Conversa: " + o.Name : o.Kind == "sanctuary" ? "Santuário (" + o.Id + ")" : o.Name;
      long id = nearest(sn, "Fortaleza", o.X, o.Y);
      if (id == 0) { sb.AppendLine("SUMIU   " + o.Name + " [" + o.Kind + "] (" + F(o.X) + ", " + F(o.Y) + ")"); continue; }
      used.Add(id); var w = World(id); double dx = w[0] - o.X, dy = w[1] - o.Y;
      if (Math.Abs(dx) > 0.05 || Math.Abs(dy) > 0.05) sb.AppendLine("MOVEU   " + o.Name + " [" + o.Kind + "] código (" + F(o.X) + ", " + F(o.Y) + ") cena (" + F(w[0]) + ", " + F(w[1]) + ") d=(" + F(dx) + ", " + F(dy) + ")");
    }
    sb.AppendLine("== ENFEITES ==");
    var typical = new Dictionary<string, List<double>>(); var matches = new List<object[]>();
    foreach (var it in FortressDressing.Items()) {
      long id = nearest(it.Name, "Enfeites", it.X, it.Y);
      if (id == 0) { sb.AppendLine("SUMIU   " + it.Name + " (" + it.AssetId + ") (" + F(it.X) + ", " + F(it.Y) + ")"); continue; }
      used.Add(id); var w = World(id); matches.Add(new object[] { it, w });
      List<double> l; if (!typical.TryGetValue(it.AssetId, out l)) typical[it.AssetId] = l = new List<double>(); l.Add(w[1] - it.Y);
    }
    foreach (var m in matches) {
      var it = (FortressDressing.Item)m[0]; var w = (double[])m[1]; var l = typical[it.AssetId]; l.Sort(); double ty = l[l.Count / 2];
      double dx = w[0] - it.X, dy = w[1] - it.Y - ty;
      if (Math.Abs(dx) > 0.2 || Math.Abs(dy) > 0.15) sb.AppendLine("MOVEU   " + it.Name + " (" + it.AssetId + ") código (" + F(it.X) + ", " + F(it.Y) + ") cena (" + F(w[0]) + ", " + F(w[1]) + ") dx=" + F(dx) + " dy=" + F(dy) + " (normal " + F(ty) + ")");
    }
    sb.AppendLine("== A MAIS NA CENA ==");
    foreach (var id in tr.Keys) { string p = ParentName(id); if ((p == "Enfeites" || p == "Fortaleza") && !used.Contains(id)) { var w = World(id); sb.AppendLine("NOVO    [" + p + "] " + NameOf(id) + " em (" + F(w[0]) + ", " + F(w[1]) + ")"); } }
    return sb.ToString();
  }
}
