// Vegetação (plano de assets, Etapa 4): árvores por idade, árvore morta, tronco caído, tocos, arbustos de fundo,
// galhos de primeiro plano, flores, raízes e plantas mortas da câmara.
// Verdes azulados (os mesmos da grama): longe da hera escalável #4f5a2e/#8a9a4e e do matinho #456633.
// Luz do pôr do sol vindo da DIREITA: borda direita das copas com #d0a060. O vento da montanha sopra para a direita
// (na direção do salto de fé): as árvores velhas se inclinam para a direita.
// Copas balançam com o vento: 4 quadros (deslocamento 0, +1, 0, -1 px só na copa; o tronco fica parado).
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public static class VegetationSpriteGen
{
    static string s_template, s_ext, s_nec;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_ext = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Exterior/Vegetation");
        s_nec = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Necro");
        Palette();

        // (nome, largura, altura, altura do tronco até a copa, grossura do tronco, inclinação, nº de tufos da copa, raízes)
        Tree("muda", 0, 10, 12, 6, 1, 0f, 3, false);
        Tree("muda", 1, 12, 12, 5, 1, 0.1f, 4, false);
        Tree("muda", 2, 9, 11, 6, 1, -0.1f, 3, false);
        Tree("jovem", 0, 22, 30, 14, 2, 0.05f, 6, false);
        Tree("jovem", 1, 24, 32, 12, 2, 0.12f, 7, false);
        Tree("jovem", 2, 20, 28, 15, 2, -0.05f, 5, false);
        Tree("adulta", 0, 44, 60, 26, 4, 0.08f, 11, true);
        Tree("adulta", 1, 48, 62, 24, 4, 0.15f, 12, true);
        Tree("adulta", 2, 42, 56, 28, 3, 0.02f, 10, true);
        Tree("ancia", 0, 72, 104, 44, 7, 0.22f, 18, true);
        Tree("ancia", 1, 78, 100, 40, 8, 0.3f, 20, true);

        Write(DeadTree(), s_ext, "env_ext_veg_morta_a");
        Write(FallenLog(), s_ext, "env_ext_veg_tronco_caido_a");
        Write(Stump(14, 10, 0), s_ext, "env_ext_veg_toco_a");
        Write(Stump(12, 8, 1), s_ext, "env_ext_veg_toco_b");
        Write(Stump(16, 9, 2), s_ext, "env_ext_veg_toco_c");

        for (int v = 0; v < 3; v++)
        {
            var b = BgBush(36 + v * 6, 18 + v * 2, v);
            for (int f = 0; f < 2; f++) Write(Shift(b, f == 0 ? 0 : 1, 6), s_ext, "env_ext_veg_arbusto_fundo_" + (char)('a' + v) + "_" + f);
        }
        for (int v = 0; v < 2; v++) Write(FgBranch(96, 40, v), s_ext, "env_ext_veg_galho_frente_" + (char)('a' + v));
        for (int v = 0; v < 3; v++) Write(Flower(v), s_ext, "env_ext_veg_flor_" + v);
        for (int v = 0; v < 3; v++) Write(HangingRoot(8, 10 + v * 3, v), s_ext, "env_ext_veg_raiz_" + v);
        for (int v = 0; v < 2; v++) Write(DeadPotPlant(v), s_nec, "env_nec_veg_vaso_seco_" + (char)('a' + v));
    }

    static void Write(PixelCanvas cv, string dir, string name)
    {
        PixelKit.WriteSprite(cv, Path.Combine(dir, name + ".png"), name, s_template, false);
    }

    static float N(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        // folhas: do mais escuro (contorno/sombra) ao mais claro
        PixelKit.P('0', "#1b2a24"); PixelKit.P('1', "#22352c"); PixelKit.P('2', "#2e4436"); PixelKit.P('3', "#4a6e50");
        PixelKit.P('4', "#6a8e5e"); PixelKit.P('5', "#8fb070"); PixelKit.P('f', "#d0a060");
        // casca
        PixelKit.P('a', "#2a1c18"); PixelKit.P('b', "#3d2a22"); PixelKit.P('c', "#5e4030"); PixelKit.P('d', "#7d5a3e"); PixelKit.P('e', "#9a7a5a");
        // madeira morta / seca
        PixelKit.P('m', "#3e322c"); PixelKit.P('n', "#5e4c48"); PixelKit.P('o', "#7a6a5a"); PixelKit.P('p', "#9a8a76");
        // arbusto de fundo (mais escuro: não é o matinho jogável)
        PixelKit.P('u', "#141c1c"); PixelKit.P('v', "#1c2a26"); PixelKit.P('w', "#26382f"); PixelKit.P('x', "#31473a");
        // primeiro plano (silhueta)
        PixelKit.Pal['F'] = Color.FromArgb(220, 0x12, 0x0d, 0x1a); PixelKit.Pal['G'] = Color.FromArgb(200, 0x1c, 0x16, 0x26);
        // flores (sem amarelo: não parecer moeda)
        PixelKit.P('r', "#e8a0b0"); PixelKit.P('l', "#c8b0e8"); PixelKit.P('h', "#f0ece0"); PixelKit.P('g', "#4a6e50");
        // vaso de barro
        PixelKit.P('A', "#7a4a34"); PixelKit.P('B', "#a0644a"); PixelKit.P('C', "#4e2e22"); PixelKit.P('k', "#1c1a24");
    }

    // --------------------------------------------------------------- árvores

    /// <summary>
    /// Árvore procedural: tronco afinando para cima (luz à direita), inclinado pelo vento, raízes na base (adultas
    /// e anciãs), galhos saindo do alto do tronco e uma copa de tufos (elipses com borda irregular) sombreada pela luz
    /// do alto à direita, com a borda de luz do pôr do sol. Grava 4 quadros: a copa balança 0, +1, 0, -1 px.
    /// </summary>
    static void Tree(string kind, int v, int W, int H, int trunkH, int trunkW, float lean, int puffs, bool roots)
    {
        int seed = kind.Length * 97 + v * 31;
        var trunk = new PixelCanvas(W, H);
        var crown = new PixelCanvas(W, H);
        int baseX = W / 2;
        // tronco: da base (y = H-1) até o começo da copa, afinando e inclinando
        int crownBase = H - 1 - trunkH;
        for (int y = H - 1; y >= crownBase - trunkH / 3; y--)
        {
            float t = (H - 1 - y) / (float)Math.Max(1, trunkH);
            int w = Math.Max(1, (int)Math.Round(trunkW * (1f - t * 0.45f)));
            int cx = baseX + (int)Math.Round(lean * (H - 1 - y));
            for (int x = cx - w / 2; x <= cx - w / 2 + w - 1; x++)
            {
                int rel = x - (cx - w / 2);
                char c = rel == w - 1 && w > 1 ? 'd' : rel == 0 && w > 2 ? 'b' : (N(x, y, seed) > 0.75f ? 'b' : 'c');
                if (w >= 4 && N(x, y / 3, seed + 1) > 0.85f) c = 'e';    // casca com veios claros
                trunk.Force(x, y, c);
            }
        }
        if (roots)
            for (int k = 1; k <= trunkW + 1; k++)
            {
                trunk.Force(baseX - trunkW / 2 - k, H - 1, 'b'); trunk.Force(baseX + (trunkW + 1) / 2 + k - 1, H - 1, 'c');
                if (k <= trunkW / 2) { trunk.Force(baseX - trunkW / 2 - k, H - 2, 'b'); trunk.Force(baseX + (trunkW + 1) / 2 + k - 1, H - 2, 'd'); }
            }
        // galhos: linhas do alto do tronco para fora e para cima
        int topY = crownBase - trunkH / 3, topX = baseX + (int)Math.Round(lean * (H - 1 - topY));
        int branches = Math.Max(2, puffs / 4);
        var tips = new List<Point>();
        for (int i = 0; i < branches; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            int sy = topY + 2 + (int)(N(i, 1, seed) * trunkH * 0.3f);
            int ex = topX + (int)(side * (W * 0.2f + N(i, 2, seed) * W * 0.12f));
            int ey = Math.Max(2, sy - (int)(H * 0.2f + N(i, 3, seed) * H * 0.15f));
            ex = Math.Max(W / 5, Math.Min(W - 1 - W / 5, ex)); // a ponta do galho fica dentro da copa
            foreach (var p in PixelKit.Line(topX, sy, ex, ey)) trunk.Force(p.X, p.Y, trunkW >= 4 ? 'b' : 'c');
            tips.Add(new Point(ex, ey));
        }
        // copa: tufos em volta do topo e das pontas dos galhos
        // tudo dentro do quadro: a copa inclina um pouco com o tronco, mas nunca passa das bordas (antes ficava cortada)
        var centers = new List<float[]>();
        float rx = W * 0.3f, ry = (H - trunkH) * 0.34f;
        float maxR = Math.Max(2f, Math.Min(W, H - trunkH) * 0.2f);
        float crownCx = Math.Max(W * 0.44f, Math.Min(W * 0.56f, topX));
        float crownCy = Math.Max(ry * 0.85f + maxR + 1f, Math.Min(topY - 1f, H * 0.4f));
        Func<float, float, float, float[]> puff = (px, py, r) =>
            new[] { Math.Max(r + 1f, Math.Min(W - 2f - r, px)), Math.Max(r + 1f, py), r };
        for (int i = 0; i < puffs; i++)
        {
            float a = (float)(i / (double)puffs * Math.PI * 2 + N(i, 4, seed));
            float d = 0.35f + N(i, 5, seed) * 0.55f;
            float r = Math.Max(2f, Math.Min(W, H - trunkH) * (0.12f + N(i, 6, seed) * 0.08f));
            centers.Add(puff(crownCx + (float)Math.Cos(a) * rx * d, crownCy + (float)Math.Sin(a) * ry * d * 0.85f, r));
        }
        foreach (var tp in tips) centers.Add(puff(tp.X, Math.Max(tp.Y, crownCy - ry), Math.Max(2f, Math.Min(W, H - trunkH) * 0.13f)));
        centers.Add(puff(crownCx, crownCy, Math.Max(2.5f, Math.Min(rx, ry) * 0.8f)));
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float best = 9f, bnx = 0f, bny = 0f;
                foreach (var c in centers)
                {
                    float dx = (x + 0.5f - c[0]) / c[2], dy = (y + 0.5f - c[1]) / c[2];
                    float ang = (float)Math.Atan2(dy, dx);
                    float edge = 1f + 0.22f * (N((int)((ang + 4f) * 4f), (int)c[2], seed + (int)c[0]) - 0.5f);
                    float dd = (dx * dx + dy * dy) / (edge * edge);
                    if (dd < best) { best = dd; bnx = dx; bny = dy; }
                }
                if (best > 1f) continue;
                // luz do alto à direita sobre a copa inteira + o tufo
                float gx = (x + 0.5f - crownCx) / rx, gy = (y + 0.5f - crownCy) / ry;
                float dot = 0.6f * (bnx * 0.5f + gx * 0.5f) - 0.8f * (bny * 0.5f + gy * 0.5f);
                float clump = N(x / 3, y / 3, seed + 9);
                char ch = dot > 0.55f ? (clump > 0.5f ? '5' : '4') : dot > 0.2f ? (clump > 0.6f ? '4' : '3') : dot > -0.2f ? (clump > 0.55f ? '3' : '2') : dot > -0.55f ? '2' : '1';
                if (best > 0.82f && dot < 0f) ch = '0';                       // borda de sombra
                if (best > 0.7f && dot > 0.45f && N(x, y, seed + 11) > 0.55f) ch = 'f'; // borda de luz do pôr do sol
                crown.Force(x, y, ch);
            }
        for (int f = 0; f < 4; f++)
        {
            int dx = f == 1 ? 1 : f == 3 ? -1 : 0;
            var cv = trunk.Clone();
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    char ch = crown.Get(x - dx, y);
                    if (ch != '.') cv.Force(x, y, ch);
                }
            Write(cv, s_ext, "env_ext_veg_" + kind + "_" + (char)('a' + v) + "_" + f);
        }
    }

    /// <summary>Árvore morta 40×56: tronco seco rachado e galhos nus retorcidos, inclinada pelo vento.</summary>
    static PixelCanvas DeadTree()
    {
        var cv = new PixelCanvas(40, 56);
        int seed = 500;
        for (int y = 55; y >= 18; y--)
        {
            float t = (55 - y) / 37f;
            int w = Math.Max(1, (int)Math.Round(5 * (1f - t * 0.6f)));
            int cx = 18 + (int)Math.Round(0.2f * (55 - y));
            for (int x = cx - w / 2; x < cx - w / 2 + w; x++)
                cv.Force(x, y, x == cx - w / 2 + w - 1 ? 'p' : x == cx - w / 2 ? 'm' : (N(x, y, seed) > 0.8f ? 'm' : 'n'));
        }
        int[,] br = { { 22, 26, 34, 12 }, { 20, 30, 8, 16 }, { 23, 20, 30, 4 }, { 21, 36, 31, 28 }, { 19, 24, 12, 8 }, { 30, 12, 36, 6 }, { 12, 14, 6, 10 } };
        for (int i = 0; i < br.GetLength(0); i++)
            foreach (var p in PixelKit.Line(br[i, 0], br[i, 1], br[i, 2], br[i, 3])) { cv.Force(p.X, p.Y, 'n'); if (i < 3) cv.Force(p.X + 1, p.Y, 'o'); }
        for (int k = 1; k <= 5; k++) { cv.Force(17 - k, 55, 'm'); cv.Force(20 + k, 55, 'n'); }
        PixelKit.Outline(cv, 'a');
        return cv;
    }

    /// <summary>Tronco caído 48×12: cilindro deitado com a ponta cortada (anéis), musgo por cima.</summary>
    static PixelCanvas FallenLog()
    {
        var cv = new PixelCanvas(48, 12);
        for (int x = 0; x < 42; x++)
            for (int y = 3; y < 12; y++)
            {
                char c = y == 3 ? 'd' : y == 4 ? 'c' : y > 9 ? 'b' : (N(x, y, 600) > 0.8f ? 'b' : 'c');
                if (y == 3 && N(x, 0, 601) > 0.5f) c = '3';           // musgo no topo
                cv.Force(x, y, c);
            }
        PixelKit.Ellipse(cv, 43.5f, 7.5f, 3.5f, 4.5f, (nx, ny) => nx * nx + ny * ny < 0.3f ? 'e' : nx * nx + ny * ny < 0.65f ? 'd' : 'c'); // ponta cortada
        PixelKit.Outline(cv, 'a');
        return cv;
    }

    /// <summary>Toco cortado (zona de tiro limpa perto das muralhas): topo claro com anéis, casca, raízes.</summary>
    static PixelCanvas Stump(int W, int H, int v)
    {
        var cv = new PixelCanvas(W, H);
        int x0 = 2, x1 = W - 3;
        for (int y = 2; y < H; y++)
            for (int x = x0; x <= x1; x++)
                cv.Force(x, y, x == x1 ? 'd' : x == x0 ? 'b' : 'c');
        for (int x = x0; x <= x1; x++) { cv.Force(x, 1, 'e'); cv.Force(x, 2, (x - x0) % 3 == 1 ? 'd' : 'e'); } // topo cortado
        cv.Force(x0 - 1, H - 1, 'b'); cv.Force(x0 - 2, H - 1, 'b'); cv.Force(x1 + 1, H - 1, 'c'); cv.Force(x1 + 2, H - 1, 'c');
        if (v == 1) { cv.Force(x0 + 2, 0, '3'); cv.Force(x0 + 3, 0, '4'); cv.Force(x0 + 3, 1, '3'); } // broto nascendo
        PixelKit.Outline(cv, 'a');
        return cv;
    }

    // -------------------------------------------------------- arbustos e galhos

    /// <summary>Arbusto de fundo: maior e bem mais escuro que o matinho (que é esconderijo): só cenário.</summary>
    static PixelCanvas BgBush(int W, int H, int v)
    {
        var cv = new PixelCanvas(W, H);
        int seed = 700 + v * 13;
        int puffs = 4 + v;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool inside = false; float shade = 0f;
                for (int i = 0; i < puffs; i++)
                {
                    float r = H * (0.3f + 0.12f * N(i, 2, seed));
                    float cx = Math.Max(r, Math.Min(W - r, W * (0.15f + 0.7f * i / (puffs - 1f)))), cy = Math.Max(r + 1f, H * (0.62f + 0.15f * N(i, 1, seed)));
                    float dx = (x + 0.5f - cx) / r, dy = (y + 0.5f - cy) / r;
                    if (dx * dx + dy * dy <= 1f && y < H) { inside = true; shade = Math.Max(shade, 0.6f * dx - 0.8f * dy); }
                }
                if (!inside) continue;
                cv.Force(x, y, shade > 0.5f ? 'x' : shade > 0.1f ? 'w' : shade > -0.3f ? 'v' : 'u');
            }
        return cv;
    }

    /// <summary>Copia o desenho deslocando as linhas de cima (balanço do vento) em dx px.</summary>
    static PixelCanvas Shift(PixelCanvas src, int dx, int rows)
    {
        if (dx == 0) return src.Clone();
        var cv = new PixelCanvas(src.W, src.H);
        for (int x = 0; x < src.W; x++)
            for (int y = 0; y < src.H; y++)
            {
                int sx = y < rows ? x - dx : x;
                cv.Force(x, y, src.Get(sx, y));
            }
        return cv;
    }

    /// <summary>
    /// Galho de primeiro plano 96×40 pendurado do alto da tela, em silhueta escura e meio transparente
    /// (plano, 3.5: no máximo 15% da tela, nunca no meio, nunca na frente do caminho).
    /// </summary>
    static PixelCanvas FgBranch(int W, int H, int v)
    {
        // a borda de baixo de uma copa em primeiro plano: massa de folhas presa no alto da tela, com a borda de baixo
        // irregular caindo (mais funda de um lado) e folhas soltas na ponta. Sem "cabinhos": lia como lustre.
        var cv = new PixelCanvas(W, H);
        int seed = 800 + v * 17;
        for (int x = 0; x < W; x++)
        {
            float t = v == 0 ? x / (float)W : 1f - x / (float)W;          // 0 no lado preso, 1 na ponta
            double wave = Math.Sin(x * 0.35 + seed) * 3 + Math.Sin(x * 0.13 + seed * 0.5) * 5;
            int depth = (int)Math.Round((1f - t * t) * (H - 8) + wave);    // fundo de um lado, raso na ponta
            depth = Math.Max(2, Math.Min(H - 1, depth));
            for (int y = 0; y < depth; y++)
            {
                bool hole = y > depth - 5 && N(x, y, seed) > 0.55f;      // borda recortada em folhas
                if (!hole) cv.Force(x, y, y < depth - 6 && N(x / 2, y / 2, seed + 1) > 0.7f ? 'G' : 'F');
            }
            if (t > 0.6f && N(x, 99, seed) > 0.8f) cv.Force(x, depth + 1, 'F');  // folha solta pingando da ponta
        }
        return cv;
    }

    // ------------------------------------------------- flores, raízes, vasos

    static PixelCanvas Flower(int v)
    {
        var cv = new PixelCanvas(6, 7);
        char petal = v == 0 ? 'r' : v == 1 ? 'l' : 'h';
        for (int y = 3; y < 7; y++) cv.Force(2, y, 'g');
        cv.Force(1, 5, 'g'); cv.Force(3, 4, 'g');
        cv.Force(2, 0, petal); cv.Force(1, 1, petal); cv.Force(3, 1, petal); cv.Force(2, 2, petal); cv.Force(2, 1, v == 2 ? 'l' : 'h');
        if (v == 1) { cv.Force(4, 3, petal); cv.Force(5, 2, petal); cv.Force(4, 2, 'h'); }
        return cv;
    }

    /// <summary>Raiz pendurada do teto (na parte abandonada da Cidadela, a mata está entrando por cima).</summary>
    static PixelCanvas HangingRoot(int W, int H, int v)
    {
        var cv = new PixelCanvas(W, H);
        int x = 3 + v % 2;
        for (int y = 0; y < H; y++)
        {
            cv.Force(x, y, y < 2 ? 'b' : 'c');
            if (y < H / 2) cv.Force(x + 1, y, 'd');
            if (y == H / 3) { cv.Force(x - 1, y + 1, 'c'); cv.Force(x - 2, y + 2, 'c'); }
            if (N(x, y, 900 + v) > 0.65f) x = Math.Max(1, Math.Min(W - 2, x + (N(y, x, 901) > 0.5f ? 1 : -1)));
        }
        return cv;
    }

    /// <summary>Vaso de barro com planta morta 12×16 (perto do Tomo a vida some: plano, 6.3).</summary>
    static PixelCanvas DeadPotPlant(int v)
    {
        var cv = new PixelCanvas(12, 16);
        for (int y = 10; y < 16; y++)
            for (int x = 2; x < 10; x++)
            {
                if ((y == 15 && (x == 2 || x == 9))) continue;
                cv.Force(x, y, y == 10 ? 'B' : x == 9 ? 'C' : x == 2 ? 'C' : 'A');
            }
        for (int x = 1; x < 11; x++) cv.Force(x, 10, 'B');
        // hastes secas caídas
        int[,] st = v == 0 ? new[,] { { 5, 10, 3, 2 }, { 6, 10, 9, 3 }, { 6, 10, 10, 8 } } : new[,] { { 5, 10, 1, 6 }, { 6, 10, 7, 1 } };
        for (int i = 0; i < st.GetLength(0); i++) foreach (var p in PixelKit.Line(st[i, 0], st[i, 1], st[i, 2], st[i, 3])) cv.Force(p.X, p.Y, 'n');
        cv.Force(st[0, 2], st[0, 3], 'o'); cv.Force(st[1, 2], st[1, 3], 'm');
        PixelKit.Outline(cv, 'k');
        return cv;
    }
}
