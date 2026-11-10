// Cenário da Fortaleza em pixel art: céu do pôr do sol, montanhas ao longe com a Cidadela, morros com pinheiros,
// cachoeira (4 frames) e lagoa (2 frames). Camadas de fundo andam com ParallaxLayer; a água com SpriteLoop.
// C# 5; compilado com PixelKit.cs por gerar.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;

public static class EnvironmentSpriteGen
{
    static void InitPalette()
    {
        PixelKit.Pal.Clear();
        // céu (de cima para baixo)
        string[] sky = { "#1f1533", "#2e1a42", "#4a2150", "#6e2a5a", "#9a3a5a", "#c4504f", "#e0703f", "#f09a45", "#f7c063" };
        for (int i = 0; i < sky.Length; i++) PixelKit.P((char)('0' + i), sky[i]);
        PixelKit.P('S', "#ffe9a8"); PixelKit.P('s', "#ffd070"); PixelKit.P('c', "#e88a7a"); PixelKit.P('C', "#f2b0a0"); // sol e nuvens
        PixelKit.P('M', "#3a2247"); PixelKit.P('m', "#6a3a6a"); PixelKit.P('n', "#8a5a7a");                         // montanhas longe
        PixelKit.P('K', "#2a1838"); PixelKit.P('k', "#ffd070"); PixelKit.P('Q', "#301c3d");                                                     // a Cidadela e janelas
        PixelKit.P('H', "#24162f"); PixelKit.P('h', "#33203f"); PixelKit.P('T', "#1a1024");                          // morros e pinheiros
        PixelKit.P('W', "#9fd4f0"); PixelKit.P('w', "#6fb0e0"); PixelKit.P('L', "#e8f8ff"); PixelKit.P('D', "#4a86c0"); // água
    }

    static PixelCanvas Sky()
    {
        int W = 480, H = 270;
        var cv = new PixelCanvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float t = y / (float)(H - 1) * 8f;              // 0 em cima, 8 no horizonte
                int band = (int)t; float f = t - band;
                // pontilhado 2×2 na transição entre faixas
                bool next = f > 0.5f && ((x + y) % 2 == 0 || f > 0.8f);
                cv.Px[x, y] = (char)('0' + Math.Min(8, band + (next ? 1 : 0)));
            }
        // sol baixo, meio escondido atrás das montanhas (as montanhas cobrem a parte de baixo)
        float scx = 300f, scy = 178f, r = 30f;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = (float)Math.Sqrt((x - scx) * (x - scx) + (y - scy) * (y - scy));
                if (d <= r) cv.Px[x, y] = d < r - 5 ? 'S' : 's';
                else if (d <= r + 3 && (x + y) % 2 == 0) cv.Px[x, y] = 's';
            }
        // faixas de nuvem
        Cloud(cv, 40, 150, 120, 'c'); Cloud(cv, 260, 132, 90, 'C'); Cloud(cv, 380, 176, 70, 'c'); Cloud(cv, 120, 196, 80, 'C');
        return cv;
    }

    static void Cloud(PixelCanvas cv, int x0, int y0, int len, char c)
    {
        for (int i = 0; i < len; i++)
        {
            int thick = 1 + (int)(2.2f * Math.Sin(i / (float)len * Math.PI));
            for (int k = 0; k < thick; k++) cv.Set(x0 + i, y0 + k, c);
        }
    }

    /// <summary>Cumeeira por soma de senos (repetível na largura).</summary>
    static float Ridge(int x, int w, float baseY, float amp, int seed)
    {
        double t = x / (double)w * Math.PI * 2;
        return baseY - (float)(amp * (0.55 * Math.Sin(t * 2 + seed) + 0.3 * Math.Sin(t * 5 + seed * 2) + 0.15 * Math.Sin(t * 11 + seed * 3)));
    }

    static PixelCanvas FarMountains()
    {
        int W = 640, H = 200;
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
        {
            float top = Ridge(x, W, 90f, 60f, 1);
            float topNext = Ridge(x + 1, W, 90f, 60f, 1);
            for (int y = (int)top; y < H; y++)
            {
                if (y < 0) continue;
                char c = 'M';
                if (y < top + 3 && topNext > top) c = 'm';                         // luz do sol na encosta que desce para a direita
                if (y < top + 2 && top < 50) c = 'n';                              // neve nos picos
                cv.Px[x, y] = c;
            }
        }
        Citadel(cv, W, 395);
        return cv;
    }

    /// <summary>
    /// A Cidadela do Sol Eterno ao longe, sem base quadrada: as muralhas seguem a encosta em degraus e as torres
    /// nascem da montanha (o corpo do castelo só existe ACIMA da linha da montanha). Cor quase a da montanha
    /// (perspectiva atmosférica), luz de borda do lado do sol (direita) e poucas janelas acesas.
    /// </summary>
    static void Citadel(PixelCanvas cv, int W, int cx)
    {
        Func<int, int> ridge = x => (int)Ridge(x, W, 90f, 60f, 1);

        // muralhas: trechos de 7 px com o topo reto, em degraus que acompanham a encosta
        for (int seg = cx - 42; seg <= cx + 42; seg += 7)
        {
            int segTop = int.MaxValue;
            for (int x = seg; x < seg + 7; x++) segTop = Math.Min(segTop, ridge(x));
            int wallH = 6 + (Math.Abs(seg - cx) < 20 ? 3 : 0) - (Math.Abs(seg - cx) > 34 ? 2 : 0);
            int top = segTop - wallH;
            for (int x = seg; x < seg + 7; x++)
            {
                for (int y = top; y <= ridge(x); y++) cv.Force(x, y, 'Q');
                if ((x - seg) % 3 == 0) cv.Force(x, top - 1, 'Q');                // ameias
            }
            cv.Force(seg + 6, top, 'm');                                            // quina do degrau pega luz
        }

        // torres: (deslocamento, largura, altura acima da muralha); a do meio é a Torre do Selo
        int[,] towers = { { -33, 5, 12 }, { -21, 7, 22 }, { -9, 5, 15 }, { 0, 8, 40 }, { 11, 6, 18 }, { 22, 7, 26 }, { 33, 5, 11 } };
        for (int i = 0; i < towers.GetLength(0); i++)
        {
            int tx = cx + towers[i, 0], tw = towers[i, 1], th = towers[i, 2];
            int ground = int.MaxValue;
            for (int x = tx - tw / 2; x <= tx + tw / 2; x++) ground = Math.Min(ground, ridge(x));
            int top = ground - 8 - th;
            for (int x = tx - tw / 2; x <= tx + tw / 2; x++)
                for (int y = top; y <= ridge(x); y++)
                    cv.Force(x, y, x == tx + tw / 2 && y < ground - 4 ? 'm' : 'Q');      // borda direita iluminada
            // telhado cônico (as altas) ou ameias (as baixas)
            if (th >= 18)
                for (int k = 1; k <= tw; k++)
                    for (int dx = -(tw - k) / 2; dx <= (tw - k) / 2; dx++) cv.Force(tx + dx, top - k, dx == (tw - k) / 2 && k < tw - 1 ? 'm' : 'Q');
            else
                for (int x = tx - tw / 2; x <= tx + tw / 2; x += 2) cv.Force(x, top - 1, 'Q');
            // janelas acesas (1 px), só algumas
            if (i % 2 == 1 || i == 3) cv.Force(tx, top + th / 3, 'k');
            if (i == 3) { cv.Force(tx, top + 6, 'k'); cv.Force(tx - 1, top + 18, 'k'); }
        }
        // a luz da encosta (linha clara logo abaixo da cumeeira) some embaixo do castelo: ele está na frente dela
        for (int x = cx - 44; x <= cx + 50; x++)
            for (int y = ridge(x) + 1; y <= ridge(x) + 3; y++)
                if (cv.Get(x, y) == 'm' || cv.Get(x, y) == 'n') cv.Force(x, y, 'M');
        // poucas luzes na muralha
        for (int x = cx - 30; x <= cx + 30; x += 13) cv.Force(x, ridge(x) - 3, 'k');
    }

    static void Block(PixelCanvas cv, int x0, int y0, int x1, int y1, char c)
    {
        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) cv.Set(x, y, c);
        for (int x = x0; x <= x1; x += 3) cv.Set(x, y0 - 1, c);                    // ameias
    }
    static void Spire(PixelCanvas cv, int x, int y, int h)
    {
        for (int k = 0; k < h; k++) for (int dx = -(h - k) / 2; dx <= (h - k) / 2; dx++) cv.Set(x + dx, y - k, 'K');
    }

    static PixelCanvas NearHills()
    {
        int W = 640, H = 140;
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
        {
            float top = Ridge(x, W, 70f, 26f, 4);
            for (int y = (int)top; y < H; y++) if (y >= 0) cv.Px[x, y] = y < top + 2 ? 'h' : 'H';
        }
        // pinheiros
        for (int x = 6; x < W; x += 11 + (x * 7) % 9)
        {
            int baseY = (int)Ridge(x, W, 70f, 26f, 4) + 2;
            int h = 10 + (x * 13) % 9;
            for (int k = 0; k < h; k++)
            {
                int half = (h - k) / 3 + ((k % 4 == 0) ? 1 : 0);
                for (int dx = -half; dx <= half; dx++) cv.Set(x + dx, baseY - k, 'T');
            }
            cv.Set(x, baseY + 1, 'T');
        }
        return cv;
    }

    // Cachoeira: pedaço de 32×32 que se repete na vertical; as faixas descem 8 px por frame.
    static PixelCanvas Waterfall(int frame)
    {
        var cv = new PixelCanvas(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                if (x < 2 || x > 29) { if ((y + frame * 8 + x) % 5 != 0) cv.Px[x, y] = 'D'; continue; }
                int lane = (x * 7) % 5;
                int phase = (y - frame * 8 + lane * 6 + 64) % 16;
                char c = 'W';
                if (phase < 2) c = 'L';
                else if (phase > 11) c = 'w';
                if (x < 5 || x > 26) c = c == 'L' ? 'W' : 'w';
                cv.Px[x, y] = c;
            }
        return cv;
    }

    static PixelCanvas Foam(int frame)
    {
        var cv = new PixelCanvas(48, 14);
        for (int i = 0; i < 9; i++)
        {
            float cx = 4 + i * 5 + ((i + frame) % 2) * 2, cy = 9 - ((i * 3 + frame * 2) % 4);
            PixelKit.Ellipse(cv, cx, cy, 3.4f, 2.6f, (a, b) => b < -0.2f ? 'L' : 'W');
        }
        return cv;
    }

    static PixelCanvas Pool(int frame)
    {
        var cv = new PixelCanvas(16, 20);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 20; y++)
            {
                char c = y < 2 ? 'L' : (y < 4 ? 'W' : (y < 12 ? 'w' : 'D'));
                if (y == 1 && (x + frame * 4) % 8 < 3) c = 'W';                         // ondinhas na superfície
                if (y == 6 && (x + frame * 5) % 11 == 0) c = 'L';
                cv.Px[x, y] = c;
            }
        return cv;
    }

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string dir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Outdoors");
        var log = new System.Text.StringBuilder();
        Action<PixelCanvas, string> write = (cv, name) =>
        {
            string gu = PixelKit.WriteSprite(cv, System.IO.Path.Combine(dir, name + ".png"), "env/" + name, template, false);
            log.Append(name + " " + gu + "\n");
        };
        var sky = Sky(); write(sky, "env_sky_sunset");
        var far = FarMountains(); write(far, "env_mountains_far");
        var near = NearHills(); write(near, "env_hills_near");
        // A cachoeira e a lagoa agora vêm de Tools/EnvironmentSprites/WaterSpriteGen.cs (Exterior/Water).
        if (!string.IsNullOrEmpty(previewDir)) Preview(sky, far, near, System.IO.Path.Combine(previewDir, "fortaleza_cenario.png"));
        return log.ToString();
    }

    /// <summary>As camadas sobrepostas como ficam na tela (480×270), mais a cachoeira ao lado.</summary>
    static void Preview(PixelCanvas sky, PixelCanvas far, PixelCanvas near, string path)
    {
        var comp = sky.Clone();
        Overlay(comp, far, 0, 270 - 200 + 10);
        Overlay(comp, near, 0, 270 - 140 + 30);
        using (var b = PixelKit.ToBitmap(comp))
        using (var big = new Bitmap(480 * 3, 270 * 3))
        using (var g = Graphics.FromImage(big))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(b, new Rectangle(0, 0, 480 * 3, 270 * 3), new Rectangle(0, 0, 480, 270), GraphicsUnit.Pixel);
            big.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    static void Overlay(PixelCanvas dst, PixelCanvas src, int ox, int oy)
    {
        for (int x = 0; x < src.W; x++) for (int y = 0; y < src.H; y++) if (src.Px[x, y] != '.') dst.Set(ox + x, oy + y, src.Px[x, y]);
    }
}
