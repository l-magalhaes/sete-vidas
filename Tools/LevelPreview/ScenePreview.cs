// Prévia de UMA TELA da Fortaleza (480×270) com a arte de verdade, sem abrir o Unity: fundo (paralaxe),
// tiles de terreno (TerrainAutotile), rampas, tufos e seixos, enfeites (FortressDressing + manifestos),
// cachoeira, lagoa, espinhos, moedas e o gato no lugar. Grava também a versão em cinza (teste de leitura).
// Aproximação: sem luz 2D, sem animação (quadro 0) e sem os objetos que o Unity desenha por código.
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;
using SeteVidas.EditorTools;
using SeteVidas.World.Scenery;

public static class ScenePreview
{
    const int W = 480, H = 270, PPU = 16;

    class Entry
    {
        public string Dir; public List<string> Sprites = new List<string>(); public string Layer = "decor_atras";
        public int Sorting; public string Support = "livre"; public bool Flip; public int OffsetPx;
        public float[] Parallax, Anchor; public bool Repeat; public string FillBelow = "";
    }

    class Draw { public int Order; public Bitmap Image; public float X, Y; public bool Flip; public float Alpha = 1f; } // X,Y = canto sup. esq. em px de tela

    static Dictionary<string, Entry> s_lib;
    static readonly Dictionary<string, Bitmap> s_cache = new Dictionary<string, Bitmap>();
    static string s_root;

    /// <summary>Uma tela com a câmera centrada em (camX, camY), em tiles. Grava "out" e "out" com sufixo _cinza.</summary>
    public static void Render(string projectRoot, float camX, float camY, string outPath, int scale)
    {
        s_root = projectRoot;
        LoadLibrary(Path.Combine(projectRoot, "Assets/_Game/Art/Environments"));
        float left = camX - W / 2f / PPU, top = camY + H / 2f / PPU;
        var draws = new List<Draw>();

        // fundo (categoria plx), na ordem de desenho do manifesto
        foreach (var id in new[] { "bg_ceu_por_do_sol", "bg_ext_cordilheira_cidadela", "bg_ext_morros_pinheiros" })
        {
            Entry e;
            if (!s_lib.TryGetValue(id, out e)) continue;
            var img = Img(Path.Combine(e.Dir, e.Sprites[0]));
            float fx = e.Parallax[0], fy = e.Parallax[1];
            float px = e.Anchor[0] + (camX - e.Anchor[0]) * (1f - fx), py = e.Anchor[1] + (camY - e.Anchor[1]) * (1f - fy);
            float wTiles = img.Width / (float)PPU;
            for (int i = -3; i <= 3; i++)
            {
                if (!e.Repeat && i != 0) continue;
                AddCentered(draws, img, px + i * wTiles, py, left, top, Order(e), false);
                if (e.FillBelow != "")
                {
                    // mesmo preenchimento do DressingBuilder: a cor da base continua para baixo (1 px de sobreposição)
                    var fill = Solid(img.Width, H + 32, e.FillBelow);
                    draws.Add(new Draw { Order = Order(e), Image = fill, X = (px + i * wTiles - left) * PPU - img.Width / 2f, Y = (top - py) * PPU + img.Height / 2f - 1 });
                }
            }
        }

        // tiles
        var ground = new HashSet<long>(); var climb = new HashSet<long>(); var back = new HashSet<long>();
        foreach (var r in FortressLayout.Tiles())
            for (int x = r.X0; x <= r.X1; x++)
                for (int y = r.Y0; y <= r.Y1; y++)
                {
                    var set = r.Map == FortressLayout.Map.Ground ? ground : r.Map == FortressLayout.Map.Climbable ? climb : back;
                    if (r.Clear) set.Remove(Key(x, y)); else set.Add(Key(x, y));
                }
        var slopeSolid = new HashSet<long>();
        var slopePieces = new List<TerrainAutotile.Piece>();
        foreach (var s in FortressLayout.Slopes())
            foreach (var p in TerrainAutotile.SlopePieces(TerrainAutotile.Soil, s.X0, s.Y0, s.W, s.H, s.RisesRight))
            {
                slopePieces.Add(p);
                if (p.Solid) slopeSolid.Add(Key(p.X, p.Y));
            }
        TerrainAutotile.SolidAt solid = (x, y) => ground.Contains(Key(x, y)) || climb.Contains(Key(x, y)) || slopeSolid.Contains(Key(x, y));

        string terrain = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Exterior/Terrain");
        string ph = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Placeholder");
        int x0 = (int)Math.Floor(left) - 1, x1 = (int)Math.Ceiling(left + W / (float)PPU) + 1;
        int y0 = (int)Math.Floor(top - H / (float)PPU) - 1, y1 = (int)Math.Ceiling(top) + 1;
        string arch = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Citadel/Architecture");
        string extArt = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Exterior");
        Func<string, string> pathOf = name =>
        {
            foreach (var d in new[] { terrain, extArt, arch, Path.Combine(extArt, "Vegetation") }) { string p = Path.Combine(d, name + ".png"); if (File.Exists(p)) return p; }
            return Path.Combine(terrain, name + ".png");
        };
        TerrainAutotile.MaterialAt matAt = (mx, my) => FortressTerrain.MaterialAt(mx, my);
        Action<TerrainAutotile.Detail, int> detailAt = (d, order) =>
        {
            var img = Img(pathOf(d.Sprite));
            draws.Add(new Draw { Order = order, Image = img, X = (d.X - left) * PPU - img.Width / 2f, Y = (top - d.Y) * PPU - img.Height });
        };
        Action<TerrainAutotile.Detail> detail = d => detailAt(d, 1);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                long k = Key(x, y);
                string mat = FortressTerrain.MaterialAt(x, y);
                if (back.Contains(k))
                {
                    string b = TerrainAutotile.BackTileFor(mat, x, y);
                    AddCell(draws, Img(b != "" ? pathOf(b) : Path.Combine(ph, "tile_background_placeholder.png")), x, y, left, top, -10);
                    var ev = b != "" ? TerrainAutotile.WallEvent(solid, x, y) : null;
                    if (ev != null) detailAt(ev, -9);
                }
                if (climb.Contains(k))
                {
                    string c = TerrainAutotile.ClimbTileFor(mat, solid, x, y);
                    AddCell(draws, Img(c != "" ? pathOf(c) : Path.Combine(ph, "tile_climbable_placeholder.png")), x, y, left, top, 1);
                    if (c != "" && !climb.Contains(Key(x, y + 1)) && (!solid(x - 1, y) || !solid(x + 1, y)))
                    {
                        detail(TerrainAutotile.Gargoyle(x, y, !solid(x - 1, y), mat));
                        var drip = TerrainAutotile.GargoyleDrip(x, y, !solid(x - 1, y), mat);
                        drip.Sprite = TerrainAutotile.DripFrameName(5); // a prévia mostra a gota em plena queda
                        detailAt(drip, 2);
                    }
                }
                if (!ground.Contains(k)) continue;
                if (mat == "") { AddCell(draws, Img(Path.Combine(ph, "tile_ground_placeholder.png")), x, y, left, top, 0); continue; }
                AddCell(draws, Img(pathOf(TerrainAutotile.TileFor(mat, solid, matAt, x, y))), x, y, left, top, 0);
                if (FortressTerrain.DetailsAllowed(x, y))
                    foreach (var d in solid(x, y + 1)
                                 ? TerrainAutotile.DeepDetails(TerrainAutotile.BandFor(mat, solid, matAt, x, y), x, y)
                                 : TerrainAutotile.TopDetails(mat, x, y))
                        detail(d);
                if (!solid(x, y - 1)) foreach (var d in TerrainAutotile.CeilingDetails(mat, x, y)) detail(d);
            }
        foreach (var p in slopePieces)
            if (p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1)
                AddCell(draws, Img(Path.Combine(terrain, p.Sprite + ".png")), p.X, p.Y, left, top, 0);

        // enfeites
        var history = new Dictionary<string, int[]>();
        foreach (var it in FortressDressing.Items())
        {
            Entry e;
            if (!s_lib.TryGetValue(it.AssetId, out e)) continue;
            for (int i = 0; i < Math.Max(1, it.Count); i++)
            {
                float ax = it.X + it.Step * i, ay = it.Y;
                uint seed = DressingRules.Seed(it.AssetId, (int)Math.Round(ax * PPU), (int)Math.Round(ay * PPU));
                int[] prev;
                if (!history.TryGetValue(it.AssetId, out prev)) prev = new[] { -1, -1 };
                int v = it.Variant >= 0 && it.Variant < e.Sprites.Count ? it.Variant : DressingRules.PickVariant(seed, e.Sprites.Count, prev[0], prev[1]);
                history[it.AssetId] = new[] { v, prev[0] };
                var img = Img(Path.Combine(e.Dir, e.Sprites[v]));
                float hTiles = img.Height / (float)PPU;
                float cy = e.Support == "chao" ? ay + hTiles / 2f : e.Support == "teto" ? ay - hTiles / 2f : ay;
                float cx = ax + DressingRules.PickOffsetPx(seed, e.OffsetPx) / (float)PPU;
                AddCentered(draws, img, cx, cy, left, top, Order(e), DressingRules.PickFlip(seed, e.Flip));
            }
        }

        // objetos jogáveis que importam para a leitura
        string art = Path.Combine(projectRoot, "Assets/_Game/Art");
        foreach (var o in FortressLayout.Objects())
        {
            if (o.Kind == "waterfall")
            {
                // mesma montagem do FortressSetup.Waterfall: nascente, coluna de cima para baixo, espuma
                string water = Path.Combine(art, "Environments/Exterior/Water");
                float wtop = o.Y + o.H, px = 1f / PPU;
                AddCentered(draws, Img(Path.Combine(water, "env_ext_ani_cachoeira_nascente_0.png")), o.X - 3.5f * px, wtop - 12f * px, left, top, 1, false);
                var col = Img(Path.Combine(water, "env_ext_ani_cachoeira_0.png"));
                for (float pieceTop = wtop - 24f * px; pieceTop > o.Y; pieceTop -= 2f) AddCentered(draws, col, o.X, pieceTop - 1f, left, top, -6, false);
                AddCentered(draws, Img(Path.Combine(water, "env_ext_ani_cachoeira_espuma_0.png")), o.X, o.Y + 2f * px, left, top, 9, false);
            }
            else if (o.Kind == "pool")
            {
                var img = Img(Path.Combine(art, "Environments/Exterior/Water/env_ext_ani_lagoa_0.png"));
                int first = (int)Math.Round(o.X - o.W * 0.5f);
                for (int i = 0; i < (int)o.W; i++) AddCell(draws, img, first + i, (int)Math.Floor(o.Y), left, top, 8);
            }
            else if (o.Kind == "spikes")
            {
                var img = Img(Path.Combine(art, "Environments/Traps/trap_spikes.png"));
                for (int i = 0; i < (int)o.W; i++)
                    draws.Add(new Draw { Order = 2, Image = img, X = (o.X - o.W / 2f + i - left) * PPU, Y = (top - o.Y) * PPU - img.Height });
            }
            else if (o.Kind == "wall")
            {
                // parede quebrável na pedra do lugar (FortressSetup.Wall)
                string m = FortressTerrain.MaterialAt((int)Math.Floor(o.X), (int)Math.Floor(o.Y));
                string name = TerrainAutotile.IsMasonry(m) && m != TerrainAutotile.Wood ? TerrainAutotile.BreakableWallName(m, "intacta") : "";
                string p = Path.Combine(art, "Environments/Citadel/Architecture/" + name + ".png");
                if (name != "" && File.Exists(p)) AddCentered(draws, Img(p), o.X, o.Y + 2f, left, top, 1, false);
            }
            else if (o.Kind == "shadow")
            {
                // a sombra jogável: escuro translúcido com a borda pontilhada de 6 px (como o 9-slice do jogo)
                int w = (int)(o.W * PPU), h = (int)(o.H * PPU);
                var sh = new Bitmap(w, h);
                int[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                for (int sy = 0; sy < h; sy++)
                    for (int sx = 0; sx < w; sx++)
                    {
                        int d = Math.Min(Math.Min(sx, w - 1 - sx), Math.Min(sy, h - 1 - sy));
                        float t = Math.Min(1f, (d + 0.5f) / 6f);
                        if (t >= 1f || t * 16f > bayer[(sy % 4) * 4 + (sx % 4)]) sh.SetPixel(sx, sy, Color.FromArgb(105, 10, 8, 26));
                    }
                draws.Add(new Draw { Order = 8, Image = sh, X = (o.X - o.W / 2f - left) * PPU, Y = (top - o.Y - o.H / 2f) * PPU });
            }
            else if (o.Kind == "bush")
            {
                // esconderijo (FortressSetup "bush"): matinho, ou o desenho do texto (caixotes, feno), base no y, na frente do gato
                string p = string.IsNullOrEmpty(o.Text) ? Path.Combine(art, "Environments/Placeholder/env_hiding_bush.png")
                    : Path.Combine(art, "Environments/Soluki/env_sol_obj_esconderijo_" + o.Text + ".png");
                if (File.Exists(p)) AddCentered(draws, Img(p), o.X, o.Y + 0.5f, left, top, 7, false);
            }
            else if (o.Kind == "noisy")
            {
                // objeto barulhento (FortressSetup.Noisy): o quadro de repouso; tapetes repetem a cada tile
                string p = Path.Combine(art, "Environments/Common/Noisy/env_com_obj_" + o.Text + "_0.png");
                if (File.Exists(p))
                {
                    var img = Img(p);
                    int n = o.Text == "cacos" || o.Text == "folhas_secas" || o.Text == "palha" ? Math.Max(1, (int)Math.Round(o.W)) : 1;
                    for (int i = 0; i < n; i++)
                        AddCentered(draws, img, o.X - n / 2f + 0.5f + i, o.Y + img.Height / 32f, left, top, 2, false);
                }
            }
            else if (o.Kind == "sanctuary")
            {
                string p = Path.Combine(art, "Environments/Common/env_com_obj_santuario_seco.png");
                if (File.Exists(p)) AddCentered(draws, Img(p), o.X + 0.5f, o.Y + 1f, left, top, 2, false); // 80×32, fonte no x do santuário
            }
            else if (o.Kind == "coin")
                AddCentered(draws, Img(Path.Combine(art, "Environments/Placeholder/item_gold_coin.png")), o.X, o.Y, left, top, 6, false);
        }
        var cat = Img(Path.Combine(art, "Characters/Cat/Lion/Body/idle/chr_lion_idle_0.png"));
        float catX = camX, catY = FortressLayout.SpawnY;
        // o gato fica sobre o chão mais alto embaixo do meio da câmera
        for (int y = (int)Math.Ceiling(top); y > y0; y--) if (solid((int)Math.Floor(catX), y - 1)) { catY = y; break; }
        draws.Add(new Draw { Order = 5, Image = cat, X = (catX - left) * PPU - cat.Width / 2f, Y = (top - catY) * PPU - cat.Height + 8 });

        draws.Sort((a, b) => a.Order.CompareTo(b.Order));
        using (var bmp = new Bitmap(W, H))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(ColorTranslator.FromHtml("#1f1533"));
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                foreach (var d in draws) Blit(g, d);
            }
            Save(bmp, outPath, scale, false);
            Save(bmp, outPath.Replace(".png", "_cinza.png"), scale, true);
        }
    }

    // ------------------------------------------------------------------ apoio

    static long Key(int x, int y) { return ((long)x << 32) ^ (uint)y; }

    static int Order(Entry e) { return e.Sorting != 0 ? e.Sorting : SceneryLayers.DefaultOrder(e.Layer); }

    static void AddCell(List<Draw> draws, Bitmap img, int x, int y, float left, float top, int order)
    {
        draws.Add(new Draw { Order = order, Image = img, X = (x - left) * PPU, Y = (top - (y + 1)) * PPU });
    }

    static void AddCentered(List<Draw> draws, Bitmap img, float cx, float cy, float left, float top, int order, bool flip)
    {
        draws.Add(new Draw { Order = order, Image = img, X = (cx - left) * PPU - img.Width / 2f, Y = (top - cy) * PPU - img.Height / 2f, Flip = flip });
    }

    static void Blit(Graphics g, Draw d)
    {
        int x = (int)Math.Round(d.X), y = (int)Math.Round(d.Y);
        if (x > W || y > H || x + d.Image.Width < 0 || y + d.Image.Height < 0) return;
        var dest = d.Flip ? new Rectangle(x + d.Image.Width, y, -d.Image.Width, d.Image.Height) : new Rectangle(x, y, d.Image.Width, d.Image.Height);
        if (d.Alpha >= 1f) { g.DrawImage(d.Image, dest); return; }
        var cm = new ColorMatrix { Matrix33 = d.Alpha };
        using (var ia = new ImageAttributes())
        {
            ia.SetColorMatrix(cm);
            g.DrawImage(d.Image, new Rectangle(x, y, d.Image.Width, d.Image.Height), 0, 0, d.Image.Width, d.Image.Height, GraphicsUnit.Pixel, ia);
        }
    }

    static void Save(Bitmap src, string path, int scale, bool gray)
    {
        using (var big = new Bitmap(src.Width * scale, src.Height * scale))
        {
            for (int x = 0; x < src.Width; x++)
                for (int y = 0; y < src.Height; y++)
                {
                    var c = src.GetPixel(x, y);
                    if (gray) { int l = (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114); c = Color.FromArgb(255, l, l, l); }
                    for (int i = 0; i < scale; i++) for (int j = 0; j < scale; j++) big.SetPixel(x * scale + i, y * scale + j, c);
                }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            big.Save(path, ImageFormat.Png);
        }
    }

    static Bitmap Solid(int w, int h, string hex)
    {
        string key = "solid:" + w + "x" + h + hex;
        Bitmap b;
        if (s_cache.TryGetValue(key, out b)) return b;
        b = new Bitmap(w, h);
        using (var g = Graphics.FromImage(b)) g.Clear(ColorTranslator.FromHtml(hex));
        s_cache[key] = b;
        return b;
    }

    static Bitmap Img(string path)
    {
        Bitmap b;
        if (s_cache.TryGetValue(path, out b)) return b;
        if (!File.Exists(path)) { b = new Bitmap(16, 16); using (var g = Graphics.FromImage(b)) g.Clear(Color.Magenta); }
        else using (var f = new Bitmap(path)) b = new Bitmap(f);
        s_cache[path] = b;
        return b;
    }

    static void LoadLibrary(string artRoot)
    {
        s_lib = new Dictionary<string, Entry>();
        var json = new JavaScriptSerializer();
        foreach (var file in Directory.GetFiles(artRoot, "manifest.json", SearchOption.AllDirectories))
        {
            var root = (Dictionary<string, object>)json.DeserializeObject(File.ReadAllText(file));
            string dir = Path.GetDirectoryName(file);
            foreach (var o in (object[])root["assets"])
            {
                var a = (Dictionary<string, object>)o;
                var e = new Entry { Dir = dir };
                if (a.ContainsKey("sprites") && a["sprites"] != null) foreach (var s in (object[])a["sprites"]) e.Sprites.Add((string)s);
                if (a.ContainsKey("sprites_prefixo") && a["sprites_prefixo"] != null)
                {
                    string prefix = (string)a["sprites_prefixo"];
                    string sub = Path.GetDirectoryName(Path.Combine(dir, prefix));
                    var found = new List<string>(Directory.GetFiles(sub, Path.GetFileName(prefix) + "*.png"));
                    found.Sort(StringComparer.Ordinal);
                    foreach (var f in found) e.Sprites.Add(f.Substring(dir.Length + 1));
                }
                if (a.ContainsKey("camada")) e.Layer = (string)a["camada"];
                if (a.ContainsKey("sorting")) e.Sorting = Convert.ToInt32(a["sorting"]);
                if (a.ContainsKey("espelhar")) e.Flip = (bool)a["espelhar"];
                if (a.ContainsKey("posicionamento"))
                {
                    var p = (Dictionary<string, object>)a["posicionamento"];
                    if (p.ContainsKey("apoio")) e.Support = (string)p["apoio"];
                    if (p.ContainsKey("deslocamento_px")) e.OffsetPx = Convert.ToInt32(p["deslocamento_px"]);
                }
                if (a.ContainsKey("fundo"))
                {
                    var f = (Dictionary<string, object>)a["fundo"];
                    e.Parallax = Floats(f, "paralaxe"); e.Anchor = Floats(f, "ancora");
                    if (f.ContainsKey("repete")) e.Repeat = (bool)f["repete"];
                    if (f.ContainsKey("preencher_abaixo")) e.FillBelow = (string)f["preencher_abaixo"] ?? "";
                }
                if (e.Sprites.Count > 0) s_lib[(string)a["id"]] = e;
            }
        }
    }

    static float[] Floats(Dictionary<string, object> d, string key)
    {
        if (!d.ContainsKey(key)) return new[] { 0f, 0f };
        var arr = (object[])d[key];
        return new[] { Convert.ToSingle(arr[0]), Convert.ToSingle(arr[1]) };
    }
}
