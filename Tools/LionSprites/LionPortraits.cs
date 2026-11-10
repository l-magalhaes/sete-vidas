// Retratos de diálogo do Lion (48×48, olhando para a direita, do peito para cima).
// Spec: SeteVidasDocs/Personagens.md §1.1 e Lion_Arte_e_Animacoes.md §8.
// Persa caramelo de rosto redondo e achatado, olhos âmbar grandes, orelhas pequenas furando o capuz cinza-escuro,
// colete de couro, capa com a faixa da classe e a alça da bolsa de chaves.
// C# 5 (compilado pelo PowerShell junto com LionSpriteGen.cs).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static partial class LionSpriteGen
{
    public const int PW = 48, PH = 48;

    // nome do arquivo → expressão (os dois primeiros já existem no projeto e mantêm o GUID)
    static readonly string[][] PortraitList = {
        new[] { "portrait_cat_neutral", "neutro" },
        new[] { "portrait_cat_ironic", "ironico" },
        new[] { "portrait_cat_surprised", "surpreso" },
        new[] { "portrait_cat_hurt", "ferido" },
        new[] { "portrait_cat_serious", "serio" },
        new[] { "portrait_cat_tender", "terno" },
    };

    static void Put(Grid g, int x, int y, char c) { if (x >= 0 && y >= 0 && x < g.W && y < g.H) g.Px[x, y] = c; }
    static char At(Grid g, int x, int y) { return (x >= 0 && y >= 0 && x < g.W && y < g.H) ? g.Px[x, y] : '.'; }

    static void Ell(Grid g, float cx, float cy, float rx, float ry, char c)
    {
        for (int x = 0; x < g.W; x++)
            for (int y = 0; y < g.H; y++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) g.Px[x, y] = c;
            }
    }

    static float EllD(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    static void Tri(Grid g, PointF a, PointF b, PointF c, char col)
    {
        for (int x = 0; x < g.W; x++)
            for (int y = 0; y < g.H; y++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d1 = (px - b.X) * (a.Y - b.Y) - (a.X - b.X) * (py - b.Y);
                float d2 = (px - c.X) * (b.Y - c.Y) - (b.X - c.X) * (py - c.Y);
                float d3 = (px - a.X) * (c.Y - a.Y) - (c.X - a.X) * (py - a.Y);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) g.Px[x, y] = col;
            }
    }

    static void Seg(Grid g, int x0, int y0, int x1, int y1, char c)
    {
        foreach (var p in Line(x0, y0, x1, y1)) Put(g, p.X, p.Y, c);
    }

    // Orelha furando o capuz. tilt: 0 = em pé; 1 = deitada para trás (ferido).
    static void Ear(Grid g, float bx0, float bx1, float by, float tipX, float tipY)
    {
        Tri(g, new PointF(bx0, by), new PointF(bx1, by), new PointF(tipX, tipY), 'F');
        // interior rosado e ponta clara
        float mx = (bx0 + bx1) / 2f;
        Tri(g, new PointF(bx0 + 1.6f, by - 0.3f), new PointF(bx1 - 1.6f, by - 0.3f),
            new PointF(tipX + (mx - tipX) * 0.35f, tipY + (by - tipY) * 0.35f), 'N');
        Put(g, (int)Math.Floor(tipX), (int)Math.Floor(tipY + 0.6f), 'L');
        // furo no capuz: sombra logo abaixo da base
        for (int x = (int)Math.Floor(bx0); x <= (int)Math.Floor(bx1); x++) if (At(g, x, (int)by) == 'H' || At(g, x, (int)by) == 'G') Put(g, x, (int)by, 'h');
    }

    static Grid Portrait(string expr)
    {
        var g = new Grid(PW, PH);
        bool hurt = expr == "ferido";

        // ---- capa nos ombros
        Ell(g, 24f, 48f, 23f, 12.5f, 'H');
        for (int x = 0; x < PW; x++) for (int y = 36; y < PH; y++)
                if (g.Px[x, y] == 'H' && (x < 9 || (x < 13 && y > 42))) g.Px[x, y] = 'h';

        // ---- colete de couro, gola de pelo e faixa da classe nas bordas da capa
        for (int y = 39; y < PH; y++)
        {
            int l = 18 - (y - 39) / 3, r = 34 + (y - 39) / 3;
            for (int x = l; x <= r; x++) g.Px[x, y] = x < l + 2 ? 'v' : 'V';
            Put(g, l - 1, y, 'T'); Put(g, r + 1, y, 'T');
        }
        Tri(g, new PointF(21f, 38f), new PointF(31.5f, 38f), new PointF(26.5f, 45.5f), 'F');   // decote: pelo do peito
        Tri(g, new PointF(23f, 38f), new PointF(30f, 38f), new PointF(26.5f, 43f), 'L');
        Seg(g, 34, 40, 22, 47, 'B');                                                        // alça da bolsa
        Seg(g, 35, 40, 23, 47, 'B');
        Put(g, 30, 43, 'k'); Put(g, 31, 43, 'k'); Put(g, 30, 44, 'k');                        // fivela

        // ---- capuz
        Ell(g, 23f, 22f, 17.5f, 16.5f, 'H');
        Ell(g, 25f, 36.5f, 13f, 4f, 'H');                                                   // caimento sobre os ombros
        for (int x = 0; x < PW; x++)
            for (int y = 0; y < 40; y++)
            {
                if (g.Px[x, y] != 'H') continue;
                float d = EllD(x, y, 23f, 22f, 17.5f, 16.5f);
                if (d > 0.9f && y < 18 && x < 28) g.Px[x, y] = 'G';                            // luz de borda no topo
                else if (x < 9 && y > 18) g.Px[x, y] = 'h';                                      // lado de trás na sombra
            }
        // dobra do capuz atrás da cabeça
        Seg(g, 9, 14, 7, 24, 'h'); Seg(g, 10, 26, 9, 32, 'h');

        // ---- rosto (persa: redondo, largo e achatado), com sombra do capuz em volta
        const float fx = 27f, fy = 25.5f, frx = 11.5f, fry = 10.5f;
        for (int x = 0; x < PW; x++)
            for (int y = 0; y < PH; y++)
            {
                float d = EllD(x, y, fx, fy, frx, fry);
                if (d <= 1f) g.Px[x, y] = 'F';
                else if (d <= 1.13f && g.Px[x, y] != '.') g.Px[x, y] = 'h';
            }
        // bochechas felpudas (persa): o rosto alarga embaixo
        Ell(g, 19.5f, 31f, 5.5f, 4f, 'F'); Ell(g, 35f, 31f, 5f, 4f, 'F');
        // juba do queixo sobre a gola
        Ell(g, 27f, 36.5f, 8.5f, 2.6f, 'F');
        Put(g, 22, 39, 'F'); Put(g, 26, 39, 'F'); Put(g, 27, 39, 'F'); Put(g, 31, 39, 'F');
        // luz e sombra do pelo
        for (int x = 0; x < PW; x++)
            for (int y = 14; y < 41; y++)
            {
                if (g.Px[x, y] != 'F') continue;
                if (x < 19 || y > 35) g.Px[x, y] = 'f';
            }
        Ell(g, 21f, 30.5f, 3f, 1.6f, 'L'); Ell(g, 34f, 30.5f, 3.2f, 1.6f, 'L');             // bochechas claras
        Ell(g, 28f, 33f, 3.5f, 1.4f, 'L');                                                   // queixo claro
        if (hurt) { Put(g, 15, 22, 'f'); Put(g, 14, 21, 'f'); Put(g, 39, 23, 'F'); Put(g, 40, 22, 'F'); Put(g, 26, 15, 'f'); Put(g, 27, 14, 'f'); } // pelo desarrumado

        // ---- aba do capuz caída sobre a testa, em bico no meio (para logo acima dos olhos)
        for (int x = 0; x < PW; x++)
        {
            int brim = (int)Math.Floor(BrimY(x));
            for (int y = 0; y <= brim; y++)
            {
                char c = g.Px[x, y];
                if (c == 'F' || c == 'f' || c == 'L' || (c == 'h' && y > 8)) g.Px[x, y] = 'H';
            }
            if (g.Px[x, brim] == 'H' && At(g, x, brim + 1) != 'H' && At(g, x, brim + 1) != 'h' && At(g, x, brim + 1) != '.')
                g.Px[x, brim] = 'G';                                                           // luz na borda da aba
            for (int y = brim + 1; y <= brim + 2; y++)                                         // sombra da aba na testa
                if (At(g, x, y) == 'F' || At(g, x, y) == 'L') g.Px[x, y] = 'f';
        }

        // ---- orelhas pequenas furando o capuz
        if (hurt)
        {
            Ear(g, 12f, 18f, 9f, 8.5f, 4.5f);    // baixas, para trás
            Ear(g, 27f, 33f, 7f, 25.5f, 2.5f);
        }
        else if (expr == "ironico")
        {
            Ear(g, 11f, 18f, 9f, 11.5f, 2f);
            Ear(g, 27f, 34f, 7f, 34f, 0.5f);     // a da frente de lado
        }
        else if (expr == "terno")
        {
            Ear(g, 11f, 18f, 9f, 13f, 2.5f);
            Ear(g, 27f, 34f, 7f, 31f, 1.5f);     // relaxadas
        }
        else
        {
            Ear(g, 11f, 18f, 9f, 13.5f, 1.5f);
            Ear(g, 27f, 34f, 7f, 31.5f, 0f);
        }

        // ---- olhos âmbar grandes (o de trás menor, 3/4 olhando para a direita)
        Eye(g, 21, 24, 2.6f, expr, true);
        Eye(g, 32, 24, 3.1f, expr, false);

        // ---- nariz achatado e boca
        Put(g, 27, 28, 'N'); Put(g, 28, 28, 'N'); Put(g, 29, 28, 'N'); Put(g, 28, 29, 'N');
        Mouth(g, expr);

        GridOutline(g, 'o');
        return g;
    }

    /// <summary>Linha de baixo da aba do capuz: bico em x = 27 (sobre o nariz), subindo para os lados.</summary>
    static float BrimY(int x) { return 20.4f - Math.Abs(x + 0.5f - 27.5f) * 0.38f; }

    static void Eye(Grid g, int cx, int cy, float r, string expr, bool back)
    {
        float ecx = cx + 0.5f, ecy = cy + 0.5f;
        if (expr == "ferido")
        {
            // olhos apertados: > <
            int s = back ? 2 : 3;
            for (int i = 0; i <= s; i++)
            {
                if (back) { Put(g, cx - s + i, cy - s + i, 'o'); Put(g, cx - s + i, cy + s - i, 'o'); }
                else { Put(g, cx + s - i, cy - s + i, 'o'); Put(g, cx + s - i, cy + s - i, 'o'); }
            }
            return;
        }
        // contorno, íris, brilho
        for (int x = cx - 5; x <= cx + 5; x++)
            for (int y = cy - 5; y <= cy + 5; y++)
            {
                float dx = x + 0.5f - ecx, dy = y + 0.5f - ecy;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d <= r + 0.9f) Put(g, x, y, 'o');
            }
        for (int x = cx - 5; x <= cx + 5; x++)
            for (int y = cy - 5; y <= cy + 5; y++)
            {
                float dx = x + 0.5f - ecx, dy = y + 0.5f - ecy;
                if (dx * dx + dy * dy <= r * r) Put(g, x, y, 'E');
            }
        // pupila: fenda vertical olhando para a direita; na surpresa, um ponto pequeno
        int px = cx + 1;
        if (expr == "surpreso") { Put(g, px, cy, 'o'); }
        else
        {
            int h = expr == "terno" ? 2 : 1;
            for (int y = cy - h; y <= cy + h + 1; y++) Put(g, px, y, 'o');
            if (!back && expr == "terno") for (int y = cy - 1; y <= cy + 2; y++) Put(g, px - 1, y, 'o'); // pupila larga e suave
        }
        Put(g, cx - 1, cy - 1, 'w');
        if (expr == "terno" && !back) Put(g, cx + 2, cy + 1, 'w');

        // pálpebras
        int lid = 0;           // linhas cobertas a partir do topo do olho
        switch (expr)
        {
            case "neutro": lid = back ? 2 : 3; break;                // semicerrados
            case "ironico": lid = back ? 2 : 4; break;               // o da frente mais fechado
            case "serio": lid = back ? 1 : 2; break;                 // reto e direto
            case "terno": lid = 1; break;
            default: lid = 0; break;                                  // surpreso: totalmente abertos
        }
        int top = cy - (int)Math.Ceiling(r);
        for (int y = top; y < top + lid; y++)
            for (int x = cx - 4; x <= cx + 4; x++)
                if (At(g, x, y) == 'E' || At(g, x, y) == 'o' || At(g, x, y) == 'w') Put(g, x, y, 'F');
        if (lid > 0)
            for (int x = cx - 4; x <= cx + 4; x++)
                if (At(g, x, top + lid) == 'E' || At(g, x, top + lid) == 'w' || At(g, x, top + lid) == 'o') Put(g, x, top + lid, 'o');
        // a pálpebra de baixo, curvinha, no olhar terno
        if (expr == "terno") { Put(g, cx - 1, cy + (int)r + 1, 'f'); Put(g, cx, cy + (int)r + 1, 'f'); Put(g, cx + 1, cy + (int)r + 1, 'f'); }

        // sem sobrancelhas: a aba do capuz cobre a testa e a sombra dela faz o olhar
    }

    /// <summary>Só a prévia ampliada, sem gravar nada no projeto.</summary>
    public static void PreviewPortraits(string previewPath)
    {
        InitPalette();
        var grids = new List<Grid>();
        foreach (var p in PortraitList) grids.Add(Portrait(p[1]));
        PortraitPreview(grids, previewPath, 6);
    }

    static void Mouth(Grid g, string expr)
    {
        switch (expr)
        {
            case "ironico": // canto puxado para cima: sorriso torto
                Put(g, 28, 30, 'o'); Put(g, 27, 31, 'o'); Put(g, 26, 31, 'o'); Put(g, 29, 31, 'o'); Put(g, 30, 31, 'o'); Put(g, 31, 30, 'o'); Put(g, 32, 29, 'o');
                break;
            case "surpreso": // boquinha aberta
                Put(g, 28, 30, 'o'); Put(g, 27, 31, 'o'); Put(g, 29, 31, 'o'); Put(g, 27, 32, 'o'); Put(g, 29, 32, 'o'); Put(g, 28, 33, 'o'); Put(g, 28, 31, 'N'); Put(g, 28, 32, 'N');
                break;
            case "ferido": // dentes cerrados
                Put(g, 28, 30, 'o'); Seg(g, 25, 32, 31, 32, 'o'); Put(g, 26, 31, 'o'); Put(g, 30, 31, 'o'); Put(g, 27, 31, 'w'); Put(g, 28, 31, 'w'); Put(g, 29, 31, 'w');
                break;
            case "terno": // sorriso pequeno em "w"
                Put(g, 28, 30, 'o'); Put(g, 27, 31, 'o'); Put(g, 26, 30, 'o'); Put(g, 29, 31, 'o'); Put(g, 30, 30, 'o');
                break;
            default: // neutro e sério: boca reta
                Put(g, 28, 30, 'o'); Seg(g, 26, 31, 30, 31, 'o');
                break;
        }
    }

    /// <summary>Grava os retratos em Assets/_Game/Art/UI/Portraits. Os que já têm .meta mantêm o GUID.</summary>
    public static string Portraits(string projectRoot, string metaTemplatePath, string previewPath)
    {
        InitPalette();
        MetaTemplate = File.ReadAllText(metaTemplatePath);
        string dir = Path.Combine(projectRoot, "Assets/_Game/Art/UI/Portraits");
        Directory.CreateDirectory(dir);
        var log = new System.Text.StringBuilder();
        var grids = new List<Grid>();
        foreach (var p in PortraitList)
        {
            var g = Portrait(p[1]);
            grids.Add(g);
            string path = Path.Combine(dir, p[0] + ".png");
            string meta = path + ".meta";
            string guid;
            if (File.Exists(meta))
            {
                using (var bmp = ToBitmap(g.Px, g.W, g.H)) bmp.Save(path, ImageFormat.Png);
                guid = ReadGuid(meta);
            }
            else
            {
                guid = Guid("portrait/" + p[0]);
                WritePng(g.Px, g.W, g.H, path, guid, false);
            }
            log.Append(p[1] + " " + p[0] + " " + guid + "\n");
        }
        if (!string.IsNullOrEmpty(previewPath)) PortraitPreview(grids, previewPath, 6);
        return log.ToString();
    }

    static string ReadGuid(string metaPath)
    {
        foreach (var line in File.ReadAllLines(metaPath))
            if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
        return "";
    }

    static void PortraitPreview(List<Grid> grids, string path, int S)
    {
        int cell = PW * S + 24;
        using (var bmp = new Bitmap(cell * 3, (PH * S + 40) * 2))
        using (var gr = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 12))
        {
            gr.Clear(ColorTranslator.FromHtml("#14101e"));
            gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < grids.Count; i++)
            {
                int cx = (i % 3) * cell + 12, cy = (i / 3) * (PH * S + 40) + 24;
                using (var b = new SolidBrush(ColorTranslator.FromHtml("#2a2238"))) gr.FillRectangle(b, cx, cy, PW * S, PH * S);
                using (var pen = new Pen(ColorTranslator.FromHtml("#c9962e"), 6)) gr.DrawRectangle(pen, cx - 3, cy - 3, PW * S + 6, PH * S + 6);
                using (var b = ToBitmap(grids[i].Px, PW, PH)) gr.DrawImage(b, new Rectangle(cx, cy, PW * S, PH * S), new Rectangle(0, 0, PW, PH), GraphicsUnit.Pixel);
                gr.DrawString(PortraitList[i][1], font, Brushes.White, cx, cy - 22);
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    /// <summary>Prévia ampliada de animações com uma parede de pedra a partir da coluna 16 (escalada).</summary>
    public static void PreviewClips(string path, string clipCsv, int S)
    {
        InitPalette();
        PosesByClip.Clear();
        BuildClips();
        var names = clipCsv.Split(',');
        var list = new List<Clip>();
        foreach (var n in names) { var c = Clips.Find(k => k.Name == n.Trim()); if (c != null) list.Add(c); }
        int cols = 1; foreach (var c in list) cols = Math.Max(cols, c.Frames.Count);
        const int x0 = 6, y0 = 6, cw = 34, ch = 38;
        using (var bmp = new Bitmap(cols * (cw * S + 8), list.Count * (ch * S + 20)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 10))
        using (var stone = new SolidBrush(ColorTranslator.FromHtml("#4b4a5e")))
        using (var seam = new Pen(ColorTranslator.FromHtml("#2c2b3a"), 2))
        {
            g.Clear(ColorTranslator.FromHtml("#1e1b2c"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < list.Count; r++)
            {
                int oy = r * (ch * S + 20);
                g.DrawString(list[r].Name, font, Brushes.White, 2, oy);
                for (int i = 0; i < list[r].Frames.Count; i++)
                {
                    var fr = list[r].Frames[i];
                    int ox = i * (cw * S + 8);
                    int wallX = ox + (16 + OX - x0) * S;
                    g.FillRectangle(stone, wallX, oy + 18, ox + cw * S - wallX, ch * S);
                    for (int k = 0; k < ch; k += 8) g.DrawLine(seam, wallX, oy + 18 + k * S, ox + cw * S, oy + 18 + k * S);
                    var dst = new Rectangle(ox, oy + 18, cw * S, ch * S);
                    using (var b = ToBitmap(fr.Body.Px, W, H)) g.DrawImage(b, dst, new Rectangle(x0, y0, cw, ch), GraphicsUnit.Pixel);
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
