// Floresta Silenciosa, F5: as zonas de sombra (furtividade) ganham QUEM as faz. Regra do usuário (14/11/2026): toda
// sombra tem um objeto no cenário projetando-a, e a forma da sombra segue a do objeto.
//   flo_arvore_sombra_<W>x<H>_b<n>[_praga]  árvore de fundo: copa larga por cima da zona, tronco dentro dela
//                                          ((W + 2) × (H + 6 + n) tiles; n = tronco abaixo da zona até o chão)
//   flo_sombra_copa_<W>x<H>                o escuro da zona (W × H tiles): a borda de cima acompanha o recorte da copa e
//                                          os lados se inclinam para a direita (a lua está no alto à esquerda)
//   flo_sombra_teto_<W>x<H>                o escuro embaixo de um teto/saliência: reto em cima, lados irregulares
// Sombra em cor única (branco; o SpriteRenderer tinge e o LightZone dá a opacidade) com uma faixa de borda mais clara:
// sem pontilhado. Quais tamanhos existem vem do ForestLayout (ForestTerrain.Shadows), a mesma regra que o Unity usa.
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.EditorTools;

public static class ForestShadowGen
{
    static string s_dir, s_template;

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Shadows");
        var done = new HashSet<string>();
        int trees = 0, shades = 0;
        var sample = new List<ForestTerrain.ShadowInfo>();
        foreach (var reg in ForestLayout.Regions())
        {
            var m = ForestTerrain.Build(reg);
            foreach (var s in ForestTerrain.Shadows(m))
            {
                if (done.Add(s.ShadeName)) { Write(s.ShadeName, Shade(s)); shades++; }
                if (!s.Roof && done.Add(s.TreeName)) { Write(s.TreeName, Tree(s)); trees++; if (sample.Count < 6) sample.Add(s); }
            }
        }
        Preview(Path.Combine(previewDir, "floresta_sombras_f5.png"), sample);
        return trees + " árvores, " + shades + " sombras";
    }

    static void Write(string name, PixelCanvas cv)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_dir, name + ".png"), "flo_sombra/" + name, s_template, false);
    }

    static double Hash(int a, int b) { return PixelKit.Noise(a, b, 977); }

    // =============================================================== copa (a mesma forma para a árvore e a sombra)

    const int CrownPx = ForestTerrain.ShadowInfo.Crown * 16;

    /// <summary>Lobos da copa (x, y, raio, achatamento; px da árvore), fixos para cada tamanho de zona. Copa alta e redonda,
    /// com a base larga encostando no alto da zona (é ela que faz a sombra).</summary>
    static List<double[]> Lobes(int W, int H)
    {
        int wpx = (W + 2) * 16;
        int seed = W * 31 + H;
        var list = new List<double[]>();
        // a copa (19/11/2026, pedido do usuário: "a sombra da árvore ainda está muito quadrada"): antes eram fileiras de
        // lobos com uma elipse larga e achatada na base, e os lados e o fundo saíam retos. Agora o contorno é uma elipse
        // (a forma geral da copa) coberta de lobos de tamanhos diferentes, com alguns no meio para encher: o recorte de
        // fora fica redondo e irregular em toda a volta.
        double cx = wpx / 2.0, cy = CrownPx * 0.52, ax = wpx / 2.0 - 6, ay = CrownPx * 0.5 - 5;
        int ring = Math.Max(8, (int)(Math.PI * (ax + ay) / 13));
        for (int i = 0; i < ring; i++)
        {
            double ang = 2 * Math.PI * (i + (Hash(seed, i + 200) - 0.5) * 0.5) / ring;
            double r = 11 + Hash(seed, i + 220) * 7;
            double k = 1 - r * 0.6 / Math.Min(ax, ay) - Hash(seed, i + 240) * 0.08;   // uns mais para fora, outros para dentro
            list.Add(new[] { cx + Math.Cos(ang) * ax * k, cy + Math.Sin(ang) * ay * k, r, 0.82 });
        }
        list.Add(new[] { cx, cy, ax * 0.6, ay / ax });   // o miolo (sem buracos dentro da copa)
        int fill = 3 + W * 2;
        for (int i = 0; i < fill; i++)
        {
            double ang = 2 * Math.PI * Hash(seed, i + 260), rad = 0.7 * Math.Sqrt(Hash(seed, i + 280));
            list.Add(new[] { cx + Math.Cos(ang) * ax * rad, cy + Math.Sin(ang) * ay * rad, 16 + Hash(seed, i + 300) * 6, 0.85 });
        }
        return list;
    }
    /// <summary>Fundo da copa em cada coluna da árvore (px, de cima): onde a folhagem termina. -1 = sem copa.</summary>
    static int CanopyBottom(List<double[]> lobes, int x)
    {
        double[] l;
        for (int y = CrownPx + 8; y >= 0; y--) if (InCanopy(lobes, x, y, out l)) return y;
        return -1;
    }

    static bool InCanopy(List<double[]> lobes, int x, int y, out double[] lobe)
    {
        lobe = null;
        double bestD = 2;
        foreach (var l in lobes)
        {
            double dx = (x - l[0]) / l[2], dy = (y - l[1]) / (l[2] * l[3]);
            double d = dx * dx + dy * dy;
            if (d <= 1 && d < bestD) { bestD = d; lobe = l; }
        }
        return lobe != null;
    }
    // =============================================================== árvore

    static void TreePalette(bool blight)
    {
        PixelKit.Pal.Clear();
        if (!blight)
        {
            PixelKit.P('K', "#140f0c"); PixelKit.P('b', "#1d1612"); PixelKit.P('B', "#261d17"); PixelKit.P('L', "#3a2d22");
            PixelKit.P('k', "#0f1813"); PixelKit.P('g', "#132119"); PixelKit.P('G', "#182a1f"); PixelKit.P('h', "#203628");
        }
        else
        {
            PixelKit.P('K', "#120c10"); PixelKit.P('b', "#191117"); PixelKit.P('B', "#21171f"); PixelKit.P('L', "#33232e");
            PixelKit.P('k', "#110a13"); PixelKit.P('g', "#170f1b"); PixelKit.P('G', "#1f1424"); PixelKit.P('h', "#2b1c33");
        }
    }

    static PixelCanvas Tree(ForestTerrain.ShadowInfo s)
    {
        TreePalette(s.Blight);
        int wpx = (s.W + 2) * 16, hpx = (s.H + ForestTerrain.ShadowInfo.Crown + s.Below) * 16;
        var cv = new PixelCanvas(wpx, hpx);
        var lobes = Lobes(s.W, s.H);
        // troncos: o principal (um pouco à esquerda do meio) e, nas zonas largas, um segundo
        var trunks = new List<int>();
        trunks.Add((1 + ForestTerrain.TrunkTile(s.W)) * 16 + 8);
        if (s.W >= 9) trunks.Add((1 + (int)(s.W * 0.78)) * 16 + 8);
        int tw = s.W <= 3 ? 8 : s.W <= 6 ? 12 : 16;
        foreach (int tc in trunks)
        {
            int x0 = tc - tw / 2;
            for (int y = 24; y < hpx; y++)
            {
                int flare = y > hpx - 8 ? (y - (hpx - 8)) / 2 : 0;                // raízes abrindo no pé
                for (int dx = -flare; dx < tw + flare; dx++)
                {
                    char c = dx <= 0 ? 'L' : dx >= tw - 2 ? 'K' : ((dx + y / 9) % 4 == 0 ? 'b' : 'B');
                    cv.Force(x0 + dx, y, c);
                }
            }
            // dois galhos subindo para dentro da copa
            for (int i = 0; i < 26; i++)
            {
                cv.Force(x0 - i, CrownPx - 30 - i, 'b'); cv.Force(x0 - i, CrownPx - 29 - i, 'K');
                cv.Force(x0 + tw + i, CrownPx - 36 - i, 'b'); cv.Force(x0 + tw + i, CrownPx - 35 - i, 'K');
            }
        }
        // copa: lobos com luz no alto à esquerda e cachos de folha menores dentro
        for (int y = 0; y < Math.Min(hpx, CrownPx + 8); y++)
            for (int x = 0; x < wpx; x++)
            {
                double[] l;
                if (!InCanopy(lobes, x, y, out l)) continue;
                double gl = -(x - wpx / 2.0) / (wpx / 2.0) * 0.6 - (y - CrownPx / 2.0) / (CrownPx / 2.0) * 0.8;
                double light = gl * 0.7 + (-(x - l[0]) * 0.6 - (y - l[1]) * 0.8) / l[2] * 0.5;
                char c = light > 0.75 ? 'h' : light > 0.15 ? 'G' : light > -0.45 ? 'g' : 'k';
                cv.Force(x, y, c);
            }
        int seed = s.W * 31 + s.H;
        int clusters = (s.W + 2) * 4;
        for (int i = 0; i < clusters; i++)
        {
            double cx = Hash(seed + 7, i) * wpx, cy = 12 + Hash(seed + 9, i) * (CrownPx - 30), r = 4 + Hash(seed + 11, i) * 2;
            for (int y = (int)(cy - r); y <= cy + r; y++)
                for (int x = (int)(cx - r); x <= cx + r; x++)
                {
                    double dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy > r * r || cv.Get(x, y) == '.') continue;
                    double light = (-dx * 0.6 - dy * 0.8) / r;
                    char c = cv.Get(x, y);
                    if (light > 0.55) c = c == 'k' ? 'g' : c == 'g' ? 'G' : c;                 // lado ao luar do cacho
                    else if (light < -0.6) c = c == 'h' ? 'G' : c == 'G' ? 'g' : 'k';            // lado de baixo do cacho
                    cv.Force(x, y, c);
                }
        }
        // franja de folhas pendurada embaixo da copa
        for (int x = 1; x < wpx - 1; x += 3)
        {
            int b = CanopyBottom(lobes, x);
            if (b < 0) continue;
            int len = 2 + (int)(Hash(seed + 13, x) * 5);
            for (int i = 1; i <= len; i++) cv.Force(x, b + i, i == len ? 'k' : 'g');
        }
        return cv;
    }

    // =============================================================== sombra

    /// <summary>
    /// O escuro da zona com forma de sombra de árvore (pedido do usuário: "as sombras estão quadradas"): cantos
    /// arredondados, borda de cima seguindo a copa, lados com bojos e inclinados para a direita (luz da esquerda),
    /// degradê em 3 degraus da borda para dentro e algumas manchas de luar que passam pelas folhas. Embaixo do teto: reto em
    /// cima, sem manchas. Embaixo encosta no chão (sem borda). Sem pontilhado.
    /// </summary>
    static PixelCanvas Shade(ForestTerrain.ShadowInfo s)
    {
        PixelKit.Pal.Clear();
        PixelKit.Pal['D'] = Color.FromArgb(255, 255, 255, 255);
        PixelKit.Pal['d'] = Color.FromArgb(175, 255, 255, 255);
        PixelKit.Pal['e'] = Color.FromArgb(95, 255, 255, 255);
        int w = s.W * 16, h = s.ShadeH * 16;
        var lobes = Lobes(s.W, s.H);
        int seed = s.W * 31 + s.H;
        int lowest = 0;
        for (int x = 0; x < w; x++) lowest = Math.Max(lowest, CanopyBottom(lobes, x + 16));
        var inside = new bool[w, h];
        for (int y = 0; y < h; y++)
        {
            double t = y / (double)h;
            double bulge = 2.5 * Math.Sin(y * 0.17 + seed) + 1.5 * Math.Sin(y * 0.43 + seed * 3);
            double inL = s.Roof ? 1.5 + bulge * 0.6 : 2 + 6 * t + bulge;
            double inR = s.Roof ? 1.5 - bulge * 0.6 : 7 - 6 * t - bulge * 0.7;
            for (int x = 0; x < w; x++)
            {
                if (x < inL || x >= w - Math.Max(0, inR)) continue;
                int top = 0;
                if (!s.Roof)
                {
                    int cb = CanopyBottom(lobes, x + 16);
                    top = cb < 0 ? 3 : Math.Max(0, Math.Min(3, (int)Math.Round((lowest - cb) * 0.3)));   // colada na copa
                }
                if (y < top) continue;
                // cantos de cima levemente arredondados (raio 4): a sombra nasce da copa, sem vão
                double cxL = inL + 4, cxR = w - Math.Max(0, inR) - 5, cy = top + 4;
                if (y < cy && ((x < cxL && Sq(x - cxL) + Sq(y - cy) > 16) || (x > cxR && Sq(x - cxR) + Sq(y - cy) > 16))) continue;
                inside[x, y] = true;
            }
        }
        var cv = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!inside[x, y]) continue;
                int dist = 6;                                                   // distância até a borda (o chão não conta)
                for (int oy = -5; oy <= 5; oy++)
                    for (int ox = -5; ox <= 5; ox++)
                    {
                        int nx = x + ox, ny = y + oy;
                        if (ny >= h) continue;
                        bool outside = nx < 0 || nx >= w || ny < 0 || !inside[nx, ny];
                        if (outside) dist = Math.Min(dist, Math.Max(Math.Abs(ox), Math.Abs(oy)));
                    }
                cv.Force(x, y, dist <= 2 ? 'e' : dist <= 4 ? 'd' : 'D');
            }
        // manchas de luar entre as folhas (só debaixo da copa, na metade de cima)
        if (!s.Roof)
            for (int i = 0; i < Math.Max(1, s.W / 2); i++)
            {
                double mx = 10 + Hash(seed + 21, i) * (w - 20), my = 10 + Hash(seed + 23, i) * (h * 0.45);
                double rx = 3 + Hash(seed + 25, i) * 3, ry = 2 + Hash(seed + 27, i) * 1.5;
                for (int y = (int)(my - ry); y <= my + ry; y++)
                    for (int x = (int)(mx - rx); x <= mx + rx; x++)
                        if (x >= 0 && y >= 0 && x < w && y < h && cv.Get(x, y) == 'D' && Sq((x - mx) / rx) + Sq((y - my) / ry) <= 1)
                            cv.Force(x, y, 'd');
            }
        return cv;
    }

    static double Sq(double v) { return v * v; }
    // =============================================================== prévia

    static void Preview(string path, List<ForestTerrain.ShadowInfo> sample)
    {
        const int S = 2;
        int width = 16, height = 0;
        foreach (var s in sample) { width += (s.W + 2) * 16 + 16; height = Math.Max(height, (s.H + ForestTerrain.ShadowInfo.Crown + s.Below) * 16); }
        using (var sheet = new Bitmap(Math.Max(64, width) * S, (height + 32) * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            int x = 16;
            foreach (var s in sample)
            {
                int th = (s.H + ForestTerrain.ShadowInfo.Crown + s.Below) * 16;
                int top = 16 + height - th;
                using (var tree = new Bitmap(Path.Combine(s_dir, s.TreeName + ".png")))
                    g.DrawImage(tree, x * S, top * S, tree.Width * S, tree.Height * S);
                // a sombra tingida como no jogo (azul-escuro, opacidade 0,42)
                using (var shade = new Bitmap(Path.Combine(s_dir, s.ShadeName + ".png")))
                using (var tinted = Tint(shade, Color.FromArgb(10, 8, 26), 0.42f))
                    g.DrawImage(tinted, (x + 16) * S, (top + CrownPx) * S, shade.Width * S, shade.Height * S);
                x += (s.W + 2) * 16 + 16;
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    public static Bitmap Tint(Bitmap src, Color c, float alpha)
    {
        var b = new Bitmap(src.Width, src.Height);
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var p = src.GetPixel(x, y);
                b.SetPixel(x, y, Color.FromArgb((int)(p.A * alpha), c.R, c.G, c.B));
            }
        return b;
    }
}
