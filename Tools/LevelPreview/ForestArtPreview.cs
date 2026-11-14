// Prévia da Floresta com a arte da F5 (sem o Unity): uma tela 480 × 270 com a câmera num ponto, montada como o
// ForestPainter monta a cena: fundo em paralaxe (ForestBackgrounds), massa, chão, vinha, rampas e detalhes.
// Sem inimigos, objetos, luz nem animação. Uso: Tools/LevelPreview/ver_floresta.ps1 <cena> <sala> [dx dy]
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SeteVidas.EditorTools;
using SeteVidas.World.Scenery;

public static class ForestArtPreview
{
    const int ScreenW = 480, ScreenH = 270, Px = 16;
    static string s_root;
    static readonly Dictionary<string, Bitmap> s_cache = new Dictionary<string, Bitmap>();

    static Bitmap Load(string name)
    {
        Bitmap b;
        if (s_cache.TryGetValue(name, out b)) return b;
        string[] dirs = { "Assets/_Game/Art/Environments/Forest/Terrain", "Assets/_Game/Art/Environments/Backgrounds/Flo", "Assets/_Game/Art/Environments/Forest/Shadows" };
        b = null;
        foreach (var d in dirs)
        {
            string p = Path.Combine(s_root, d, name + ".png");
            if (File.Exists(p)) { using (var tmp = new Bitmap(p)) b = new Bitmap(tmp); break; }
        }
        if (b == null)   // o chão de fora da Fortaleza (estrada) e os fundos dela: a biblioteca inteira
            foreach (var p in Directory.GetFiles(Path.Combine(s_root, "Assets/_Game/Art/Environments"), name + ".png", SearchOption.AllDirectories))
            { using (var tmp = new Bitmap(p)) b = new Bitmap(tmp); break; }
        s_cache[name] = b;
        return b;
    }

    /// <summary>Sprite de objeto (Forest/Placeholder), além dos de terreno e fundo.</summary>
    static Bitmap LoadAny(string name)
    {
        var b = Load(name);
        if (b != null) return b;
        string p = Path.Combine(s_root, "Assets/_Game/Art/Environments/Forest/Placeholder", name + ".png");
        if (!File.Exists(p)) return null;
        using (var tmp = new Bitmap(p)) b = new Bitmap(tmp);
        s_cache[name] = b;
        return b;
    }

    /// <summary>Grava a tela com a câmera em (cx, cy) tiles. Devolve quantos tiles faltaram (sem arte).</summary>
    public static int Render(string root, string scene, float cx, float cy, string outPath, int scale)
    {
        s_root = root;
        var reg = ForestLayout.Get(scene);
        if (reg == null) throw new ArgumentException("cena desconhecida: " + scene);
        var m = ForestTerrain.Build(reg);
        int missing = 0;
        using (var screen = new Bitmap(ScreenW, ScreenH, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(screen))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            uint c = reg.Sky;
            g.Clear(Color.FromArgb((int)(c >> 24) & 0xff, (int)(c >> 16) & 0xff, (int)(c >> 8) & 0xff));

            // fundo
            float ax, ay;
            ForestBackgrounds.Anchor(reg, out ax, out ay);
            foreach (var l in ForestBackgrounds.Layers(scene))
            {
                var bmp = Load(l.Id);
                if (bmp == null) { missing++; continue; }
                // centro da camada na tela: (âncora − câmera) × fator, em tiles; y do mundo para cima
                float lay = float.IsNaN(l.AnchorY) ? ay : l.AnchorY;
                float lx = (ax - cx) * l.FactorX, ly = (lay - cy) * l.FactorY;
                if (l.FactorX == 0f && l.FactorY == 0f) { lx = 0f; ly = 0f; }
                int left = (int)Math.Round(ScreenW / 2f + lx * Px - bmp.Width / 2f);
                int top = (int)Math.Round(ScreenH / 2f - ly * Px - bmp.Height / 2f);
                bool wrap = l.FactorX > 0f;
                if (wrap) { left = ((left % bmp.Width) + bmp.Width) % bmp.Width - bmp.Width; }
                for (int x = left; x < ScreenW; x += bmp.Width)
                {
                    g.DrawImage(bmp, x, top, bmp.Width, bmp.Height);
                    if (!string.IsNullOrEmpty(l.FillBelow) && top + bmp.Height < ScreenH)
                        using (var br = new SolidBrush(ColorTranslator.FromHtml(l.FillBelow)))
                            g.FillRectangle(br, x, top + bmp.Height - 1, bmp.Width, ScreenH);
                    if (!wrap) break;
                }
            }

            // tiles visíveis
            float vx0 = cx - ScreenW / 2f / Px, vy1 = cy + ScreenH / 2f / Px;
            int tx0 = (int)Math.Floor(vx0) - 1, tx1 = (int)Math.Ceiling(cx + ScreenW / 2f / Px) + 1;
            int ty0 = (int)Math.Floor(cy - ScreenH / 2f / Px) - 1, ty1 = (int)Math.Ceiling(vy1) + 1;
            Func<int, int, Point> at = (x, y) => new Point((int)Math.Round((x - vx0) * Px), (int)Math.Round((vy1 - y - 1) * Px));
            Action<string, int, int> draw = (name, x, y) =>
            {
                if (string.IsNullOrEmpty(name)) return;
                var b = Load(name);
                if (b == null) { missing++; return; }
                var p = at(x, y);
                g.DrawImage(b, p.X, p.Y, Px, Px);
            };
            var details = new List<KeyValuePair<string, PointF>>();
            for (int x = tx0; x <= tx1; x++)
                for (int y = ty0; y <= ty1; y++)
                {
                    if (m.Mass(x, y)) { draw(ForestTerrain.MassTile(m, x, y), x, y); continue; }
                    if (m.Back(x, y)) draw(ForestAutotile.BackTileFor(ForestTerrain.DecorMaterial(m.MaterialAt(x, y)), x, y), x, y);
                    if (m.CabinInside(x, y)) draw(ForestTerrain.CabinBackTile(x, y), x, y);
                }
            // a janela velha de cada cabaninha (como o ForestPainter põe)
            foreach (var box in m.CabinBoxes)
            {
                if (box[2] - box[0] < 2 || box[3] - box[1] < 2) continue;
                var wb = LoadAny("cabana_janela_velha");
                if (wb == null) { missing++; continue; }
                float wx = (box[0] + box[2] + 1) * 0.5f, wy = (box[1] + box[3] + 1) * 0.5f + 0.4f;
                g.DrawImage(wb, (int)Math.Round((wx - vx0) * Px - wb.Width / 2f), (int)Math.Round((vy1 - wy) * Px - wb.Height / 2f), wb.Width, wb.Height);
            }
            // poços das saídas verticais: parede de fundo (as vinhas vêm depois do chão)
            var shafts = ForestTerrain.Shafts(m);
            foreach (var sh in shafts)
                for (int y = sh.Y0; y <= sh.Y1; y++)
                    for (int x = sh.X0; x <= sh.X1; x++)
                        if (!m.Real(x, y)) draw(ForestAutotile.BackTileFor(ForestTerrain.DecorMaterial(m.MaterialAt(sh.X0, sh.Shell)), x, y), x, y);
            // árvores que fazem as sombras (atrás do chão)
            var shadows = ForestTerrain.Shadows(m);
            foreach (var s in shadows)
            {
                if (s.Roof) continue;
                var b = Load(s.TreeName);
                if (b == null) { missing++; continue; }
                var p = at(s.X0 - 1, s.Y0 + s.H + ForestTerrain.ShadowInfo.Crown - 1);
                g.DrawImage(b, p.X, p.Y, b.Width, b.Height);
            }
            for (int x = tx0; x <= tx1; x++)
                for (int y = ty0; y <= ty1; y++)
                {
                    if (m.Mass(x, y)) continue;
                    string mat = m.MaterialAt(x, y);
                    if (m.Ground(x, y))
                    {
                        string cabin = m.Cabin(x, y);
                        if (!m.Hidden(x, y)) draw(cabin != null ? ForestTerrain.CabinTile(cabin, x, y) : ForestTerrain.GroundTile(mat, m.Visual, x, y), x, y);
                        if (!m.Climb(x, y) && !m.Hidden(x, y) && cabin == null)
                        {
                            var ds = m.Visual(x, y + 1) ? new List<TerrainAutotile.Detail>() : ForestTerrain.TopDetailsFor(mat, m.BlightAt(x, y), x, y);
                            if (!m.Visual(x, y - 1)) ds.AddRange(ForestTerrain.CeilingDetailsFor(mat, x, y));
                            foreach (var d in ds) details.Add(new KeyValuePair<string, PointF>(d.Sprite, new PointF(d.X, d.Y)));
                        }
                    }
                    if (m.Climb(x, y)) draw(ForestAutotile.ClimbTileFor(ForestTerrain.DecorMaterial(mat), m.Visual, x, y), x, y);
                }
            foreach (var sh in shafts)
                for (int y = sh.Y0; y <= sh.Y1; y++)
                {
                    if (y == sh.Shell) continue;
                    foreach (int x in new[] { sh.X0 - 1, sh.X1 + 1 })
                        if (m.Visual(x, y) && !m.Climb(x, y)) draw(ForestAutotile.ClimbTileFor(ForestTerrain.DecorMaterial(m.MaterialAt(sh.X0, sh.Shell)), m.Visual, x, y), x, y);
                }
            foreach (var s in m.Slopes)
                foreach (var p in ForestAutotile.SlopePieces(s.Material, s.X0, s.Y0, s.W, s.H, s.RisesRight))
                    draw(p.Sprite, p.X, p.Y);
            foreach (var d in details)
            {
                var b = Load(d.Key);
                if (b == null) { missing++; continue; }
                // (X, Y) = meio da base do sprite, em tiles
                int sx = (int)Math.Round((d.Value.X - vx0) * Px - b.Width / 2f);
                int sy = (int)Math.Round((vy1 - d.Value.Y) * Px - b.Height);
                g.DrawImage(b, sx, sy, b.Width, b.Height);
            }
            // o escuro das sombras, tingido como no jogo (LightZone: azul-escuro, opacidade 0,42 com exposição 0,3)
            foreach (var s in shadows)
            {
                var b = Load(s.ShadeName);
                if (b == null) { missing++; continue; }
                var p = at(s.X0, s.Y0 + s.H - 1);
                using (var t = Tint(b, Color.FromArgb(10, 8, 26), 0.42f)) g.DrawImage(t, p.X, p.Y, b.Width, b.Height);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            using (var big = new Bitmap(ScreenW * scale, ScreenH * scale))
            using (var g2 = Graphics.FromImage(big))
            {
                g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g2.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g2.DrawImage(screen, 0, 0, ScreenW * scale, ScreenH * scale);
                big.Save(outPath, ImageFormat.Png);
            }
        }
        return missing;
    }

    /// <summary>A sombra como o jogo a pinta: a cor do SpriteRenderer com a opacidade do LightZone vezes o alfa do desenho.</summary>
    static Bitmap Tint(Bitmap src, Color c, float alpha)
    {
        var b = new Bitmap(src.Width, src.Height);
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
                b.SetPixel(x, y, Color.FromArgb((int)(src.GetPixel(x, y).A * alpha), c.R, c.G, c.B));
        return b;
    }

    /// <summary>Centro de uma sala (tiles), com deslocamento.</summary>
    public static PointF RoomCenter(string scene, string room, float dx, float dy)
    {
        var reg = ForestLayout.Get(scene);
        var r = reg == null ? null : reg.FindRoom(room);
        if (r == null) throw new ArgumentException("sala desconhecida: " + room);
        return new PointF(r.X + r.W * 0.5f + dx, r.Y + 7.5f + dy);
    }

    /// <summary>Confere todas as cenas: cada tile, detalhe e rampa que o ForestPainter vai pedir tem PNG. Devolve o relatório.</summary>
    public static string CheckAll(string root)
    {
        s_root = root;
        var sb = new System.Text.StringBuilder();
        foreach (var reg in ForestLayout.Regions())
        {
            var m = ForestTerrain.Build(reg);
            var names = new HashSet<string>();
            int mass = 0, ground = 0;
            for (int x = m.X0; x <= m.X1; x++)
                for (int y = m.Y0; y <= m.Y1; y++)
                {
                    string mat = m.MaterialAt(x, y);
                    if (m.Ground(x, y)) { ground++; names.Add(m.Hidden(x, y) ? "tile_flo_invisivel" : ForestTerrain.GroundTile(mat, m.Visual, x, y)); }
                    if (m.Climb(x, y)) names.Add(ForestAutotile.ClimbTileFor(ForestTerrain.DecorMaterial(mat), m.Visual, x, y));
                    if (m.Back(x, y)) names.Add(ForestAutotile.BackTileFor(ForestTerrain.DecorMaterial(mat), x, y));
                    string t = ForestTerrain.MassTile(m, x, y);
                    if (t != "") { mass++; names.Add(t); }
                    if (m.Ground(x, y) && !m.Visual(x, y + 1)) foreach (var d in ForestTerrain.TopDetailsFor(mat, m.BlightAt(x, y), x, y)) names.Add(d.Sprite);
                    if (m.Ground(x, y) && !m.Visual(x, y - 1)) foreach (var d in ForestTerrain.CeilingDetailsFor(mat, x, y)) names.Add(d.Sprite);
                }
            foreach (var s in m.Slopes) foreach (var p in ForestAutotile.SlopePieces(s.Material, s.X0, s.Y0, s.W, s.H, s.RisesRight)) names.Add(p.Sprite);
            foreach (var s in ForestTerrain.Shadows(m)) { names.Add(s.ShadeName); if (!s.Roof) names.Add(s.TreeName); }
            var missing = new List<string>();
            foreach (var n in names) if (Load(n) == null) missing.Add(n);
            sb.AppendLine(reg.Scene + ": " + ground + " tiles de chão, " + mass + " de massa, " + names.Count + " desenhos diferentes, " + m.Slopes.Count + " rampas" +
                (missing.Count > 0 ? "; FALTAM: " + string.Join(", ", missing.ToArray()) : ""));
        }
        return sb.ToString();
    }
}
