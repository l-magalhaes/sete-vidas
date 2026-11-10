// Terreno natural do exterior (plano de assets, Etapa 2): tilesets de solo e rocha, pedaços de rampa, tufos,
// seixos, rochas de 3 tamanhos e placas da estrada negra dos Primatas. Os nomes dos tiles vêm de
// Assets/_Game/Editor/Scenery/TerrainAutotile.cs (a mesma regra que o Unity usa para escolher cada tile).
// Luz: o sol se põe à DIREITA (fundo do conjunto A), então as bordas viradas para a direita recebem luz.
// Estratos da rocha: linhas caindo para a direita (1 px a cada 4), com período de 8 px; a fase (x par/ímpar)
// faz as linhas continuarem de um tile para o outro.
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.World.Scenery;

public static class TerrainSpriteGen
{
    static string s_template;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        InitPalette();
        string ext = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Exterior");
        string terrain = Path.Combine(ext, "Terrain");
        string pri = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Primates");

        foreach (var material in TerrainAutotile.Materials)
            foreach (var name in TerrainAutotile.AllTileNames(material))
                Write(DrawTile(name), Path.Combine(terrain, name + ".png"));

        foreach (var kind in TerrainAutotile.SlopePieceKinds)
            foreach (bool right in new[] { true, false })
                Write(DrawSlopePiece(kind, right), Path.Combine(terrain, TerrainAutotile.SlopePieceName(kind, right) + ".png"));

        for (int v = 0; v < TerrainAutotile.TuftVariants; v++) Write(DrawTuft(v), Path.Combine(terrain, TerrainAutotile.TuftName(v) + ".png"));
        for (int v = 0; v < TerrainAutotile.PebbleVariants; v++) Write(DrawPebble(v), Path.Combine(ext, TerrainAutotile.PebbleName(v) + ".png"));
        for (int v = 0; v < TerrainAutotile.CrackVariants; v++) Write(DrawCrack(v), Path.Combine(ext, TerrainAutotile.CrackName(v) + ".png"));
        string[] fossils = { "a_cranio", "b_costelas", "c_osso", "d_coluna" };
        for (int v = 0; v < fossils.Length; v++) Write(DrawFossil(v), Path.Combine(ext, "env_ext_roc_fossil_" + fossils[v] + ".png"));

        int[,] rockSizes = { { 12, 8 }, { 24, 15 }, { 44, 28 } };
        string[] rockNames = { "pequena", "media", "rochedo" };
        int[] rockVariants = { 3, 3, 2 };
        for (int s = 0; s < 3; s++)
            for (int v = 0; v < rockVariants[s]; v++)
                Write(DrawRock(rockSizes[s, 0] + v * 2 - 2, rockSizes[s, 1] + (v % 2) * 2, v + 10 * s, v % 2 == 1),
                      Path.Combine(ext, "env_ext_roc_" + rockNames[s] + "_" + (char)('a' + v) + ".png"));

        int[,] plates = { { 32, 8 }, { 24, 7 }, { 14, 6 } };
        for (int v = 0; v < 3; v++)
            Write(DrawPrimatePlate(plates[v, 0], plates[v, 1], v), Path.Combine(pri, "env_pri_ter_placa_" + (char)('a' + v) + ".png"));
    }

    static void Write(PixelCanvas cv, string path)
    {
        PixelKit.WriteSprite(cv, path, Path.GetFileNameWithoutExtension(path), s_template, false);
    }

    // --------------------------------------------------------------- paleta

    static void InitPalette()
    {
        PixelKit.Pal.Clear();
        // grama (verdes azulados: longe da hera escalável #4f5a2e/#8a9a4e e do matinho #456633)
        PixelKit.P('a', "#22352c"); PixelKit.P('b', "#2e4436"); PixelKit.P('c', "#4a6e50"); PixelKit.P('e', "#8fb070");
        PixelKit.P('f', "#d0a060"); // luz do pôr do sol nas pontas
        // terra
        PixelKit.P('m', "#33241f"); PixelKit.P('n', "#4e3830"); PixelKit.P('o', "#6a4c3e"); PixelKit.P('p', "#7d5a3e");
        // rocha natural (quente)
        PixelKit.P('q', "#2a2434"); PixelKit.P('r', "#3b3346"); PixelKit.P('s', "#574c62"); PixelKit.P('t', "#7b6d80"); PixelKit.P('u', "#a08f98");
        PixelKit.P('v', "#36524a"); // musgo
        // Primatas
        PixelKit.P('x', "#15171c"); PixelKit.P('y', "#2a2e36"); PixelKit.P('w', "#454c5a"); PixelKit.P('z', "#7fd8e0");
        PixelKit.P('k', "#1c1a24");
    }

    static float N(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    // ---------------------------------------------------------------- tiles

    /// <summary>tile_&lt;material&gt;_&lt;faixa&gt;_&lt;máscara&gt;_&lt;variação&gt; → desenho 16×16.</summary>
    static PixelCanvas DrawTile(string name)
    {
        // "tile_ext_solo_topo_b_2" → material ext_solo, faixa topo, máscara 0xb, variação 2
        string[] parts = name.Split('_');
        string material = parts[1] + "_" + parts[2];
        string band = parts[3];
        int mask = Convert.ToInt32(parts[4], 16);
        int variant = int.Parse(parts[5]);
        bool rock = material == TerrainAutotile.Rock;
        int seed = (rock ? 53 : 17) * 31 + variant * 7 + mask;

        var cv = new PixelCanvas(16, 16);
        bool phased = band == "rocha" || band == "transicao";
        int phase = phased ? variant : 0;
        bool dirt = band == "terra" || (band == "topo" && !rock);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                cv.Force(x, y, dirt ? Dirt(x, y, seed, variant) : RockPx(x + 16 * phase, y, seed));

        if (band == "topo" && !rock) GrassTop(cv, seed, variant);
        if (band == "topo" && rock) RockTop(cv, seed);
        if (band == "terra" && variant == 2) Root(cv, seed);
        if (band == "transicao") DirtIntoRock(cv, seed, phase);
        Edges(cv, mask, dirt ? "terra" : "rocha", band == "topo" && !rock);
        if (rock && band == "topo") RoundTopCorners(cv, mask);
        return cv;
    }

    /// <summary>Pedra: canto de cima arredondado (3 px) do lado exposto, para não parecer caixote.</summary>
    static void RoundTopCorners(PixelCanvas cv, int mask)
    {
        int[][] cut = { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 0, 2 } };
        int[][] rim = { new[] { 3, 0 }, new[] { 2, 1 }, new[] { 1, 2 }, new[] { 0, 3 } };
        bool up = (mask & TerrainAutotile.Up) != 0;
        foreach (bool right in new[] { false, true })
        {
            if (!up || (mask & (right ? TerrainAutotile.Right : TerrainAutotile.Left)) == 0) continue;
            foreach (var p in cut) cv.Force(right ? 15 - p[0] : p[0], p[1], '.');
            foreach (var p in rim) cv.Force(right ? 15 - p[0] : p[0], p[1], 'k');
        }
    }

    static char Dirt(int x, int y, int seed, int variant)
    {
        float n = N(x, y, seed);
        if (n < 0.14f) return 'm';
        if (n > 0.9f) return 'o';
        // seixos dentro da terra (2×1); a variação 1 tem mais
        float p = N(x / 2, y, seed + 9);
        if (p > (variant == 1 ? 0.88f : 0.96f)) return y % 2 == 0 ? 't' : 's';
        return 'n';
    }

    /// <summary>
    /// Rocha calma e escura com estratos caindo para a direita: uma linha a cada 16 px, 1 px de queda a cada 4.
    /// xw = x no mundo em px, módulo 64 (4 fases), para as linhas continuarem de um tile para o outro.
    /// </summary>
    static char RockPx(int xw, int y, int seed)
    {
        int s = y - xw / 4;
        int f = TerrainAutotile.Mod(s, 16);
        float n = N(xw, y, seed);
        if (f == 0) return n > 0.85f ? 'r' : 'q';      // linha do estrato
        if (f == 1) return n > 0.55f ? 's' : 'r';      // luz leve logo abaixo da linha
        // cada camada entre duas linhas tem um tom (rocha sedimentar), com uma linha fina pontilhada no meio
        int layer = TerrainAutotile.Mod((s - f) / 16, 3);
        char c = layer == 1 ? 's' : 'r';
        if (f == 9 && n > 0.45f) c = 'q';
        if (n < 0.05f) c = 'q';
        else if (n > 0.97f) c = 't';
        return c;
    }

    /// <summary>Transição: terra em cima entrando na rocha com a borda irregular (3 a 9 px por coluna).</summary>
    static void DirtIntoRock(PixelCanvas cv, int seed, int phase)
    {
        for (int x = 0; x < 16; x++)
        {
            // borda ondulada, contínua entre tiles: soma de senos com período de 64 px (as 4 fases)
            double t = 2 * Math.PI * (x + 16 * phase) / 64.0;
            int depth = 6 + (int)Math.Round(2.0 * Math.Sin(t) + 1.2 * Math.Sin(3 * t + 1) + 0.7 * Math.Sin(5 * t + 2));
            depth = Math.Max(2, Math.Min(10, depth));
            for (int y = 0; y < depth; y++) cv.Force(x, y, Dirt(x, y, seed + 3, 0));
            cv.Force(x, depth, 'm');
        }
    }

    /// <summary>Grama nas 3–5 primeiras linhas, com o fundo recortado (franja) e pontas claras.</summary>
    static void GrassTop(PixelCanvas cv, int seed, int variant)
    {
        for (int x = 0; x < 16; x++)
        {
            int depth = 3 + (int)(N(x, variant, seed + 3) * 3f); // 3..5
            for (int y = 0; y < depth; y++)
            {
                char c = y == depth - 1 ? 'a' : y == 0 ? (N(x, 0, seed + 4) > 0.72f ? 'e' : 'c') : (N(x, y, seed + 5) > 0.55f ? 'c' : 'b');
                cv.Force(x, y, c);
            }
            if (N(x, 1, seed + 6) > 0.9f) cv.Force(x, 0, 'f'); // brilho do pôr do sol
        }
    }

    static void RockTop(PixelCanvas cv, int seed)
    {
        for (int x = 0; x < 16; x++)
        {
            cv.Force(x, 0, N(x, 0, seed) > 0.5f ? 'u' : 't');
            cv.Force(x, 1, 't');
            if (N(x, 1, seed + 2) > 0.8f) cv.Force(x, 0, 'v'); // musgo no topo
        }
    }

    static void Root(PixelCanvas cv, int seed)
    {
        int x = 3 + (int)(N(1, 1, seed) * 8f);
        for (int y = 2; y < 13; y++)
        {
            cv.Force(x, y, 'p');
            if (N(x, y, seed + 1) > 0.6f) x += N(x, y, seed + 2) > 0.5f ? 1 : -1;
            x = Math.Max(1, Math.Min(14, x));
        }
    }

    /// <summary>Lados expostos: contorno escuro, luz no lado direito (sol), raízes embaixo, cantos chanfrados.</summary>
    static void Edges(PixelCanvas cv, int mask, string body, bool grass)
    {
        char dark = body == "rocha" ? 'q' : 'm';
        char light = body == "rocha" ? 'u' : 'o';
        bool up = (mask & TerrainAutotile.Up) != 0, right = (mask & TerrainAutotile.Right) != 0;
        bool down = (mask & TerrainAutotile.Down) != 0, left = (mask & TerrainAutotile.Left) != 0;
        for (int i = 0; i < 16; i++)
        {
            if (left) { cv.Force(0, i, 'k'); if (cv.Get(1, i) != 'a' && cv.Get(1, i) != 'b' && cv.Get(1, i) != 'c') cv.Force(1, i, dark); }
            if (right) { cv.Force(15, i, 'k'); if (i > 2 || !grass) cv.Force(14, i, light); }
            if (down)
            {
                cv.Force(i, 15, 'k'); cv.Force(i, 14, dark);
                if (body == "terra" && N(i, 13, 77) > 0.75f) { cv.Force(i, 13, 'p'); cv.Force(i, 14, 'p'); } // raízes aparecendo
            }
        }
        if (up && left) { cv.Force(0, 0, '.'); cv.Force(1, 0, '.'); cv.Force(0, 1, '.'); cv.Force(1, 1, 'k'); cv.Force(2, 0, 'k'); cv.Force(0, 2, 'k'); }
        if (up && right) { cv.Force(15, 0, '.'); cv.Force(14, 0, '.'); cv.Force(15, 1, '.'); cv.Force(14, 1, 'k'); cv.Force(13, 0, 'k'); cv.Force(15, 2, 'k'); }
        if (down && left) { cv.Force(0, 15, '.'); cv.Force(1, 15, 'k'); cv.Force(0, 14, 'k'); }
        if (down && right) { cv.Force(15, 15, '.'); cv.Force(14, 15, 'k'); cv.Force(15, 14, 'k'); }
    }

    // ----------------------------------------------------------------- rampas

    /// <summary>
    /// Pedaço de rampa 16×16: abaixo da diagonal, faixa de grama de 3–4 px seguindo a diagonal e terra embaixo.
    /// "12a"/"12b": metades da rampa 1:2 (a diagonal sobe 8 px por tile).
    /// </summary>
    static PixelCanvas DrawSlopePiece(string kind, bool risesRight)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 100 + Array.IndexOf(TerrainAutotile.SlopePieceKinds, kind) * 13;
        for (int x = 0; x < 16; x++)
        {
            // altura da superfície (em px a partir da base) no meio da coluna, rampa subindo para a direita
            float xc = x + 0.5f;
            float surface = kind == "11" ? xc : kind == "12a" ? xc * 0.5f : 8f + xc * 0.5f;
            int top = 16 - (int)Math.Round(surface); // primeira linha (de cima) dentro da rampa
            int grass = 3 + (int)(N(x, 0, seed) * 2f);
            for (int y = Math.Max(0, top); y < 16; y++)
            {
                int d = y - top;
                char c = d >= grass ? Dirt(x, y, seed, 0) : d == 0 ? (N(x, y, seed + 1) > 0.7f ? 'e' : 'c') : d == grass - 1 ? 'a' : (N(x, y, seed + 2) > 0.5f ? 'c' : 'b');
                cv.Force(x, y, c);
            }
            if (top >= 0 && top < 16 && N(x, 3, seed) > 0.85f) cv.Force(x, top, 'f');
        }
        return risesRight ? cv : cv.Mirror();
    }

    // ---------------------------------------------------- tufos, seixos, rochas

    /// <summary>Tufo de grama 16×6 que fica EM CIMA do chão: lâminas de 2–5 px, base colada no tile.</summary>
    static PixelCanvas DrawTuft(int v)
    {
        var cv = new PixelCanvas(16, 6);
        for (int x = 0; x < 16; x++)
        {
            float n = N(x, v, 400 + v);
            if (n < 0.35f) continue;
            int hgt = 2 + (int)(N(x, v + 1, 410) * 4f); // 2..5
            for (int i = 0; i < hgt; i++)
            {
                int y = 5 - i;
                char c = i == hgt - 1 ? (N(x, i, 420 + v) > 0.6f ? 'e' : 'c') : i == 0 ? 'b' : 'c';
                cv.Force(x, y, c);
            }
            if (hgt >= 4 && N(x, 7, 430 + v) > 0.75f) cv.Force(x, 6 - hgt, 'f');
        }
        return cv;
    }

    static PixelCanvas DrawPebble(int v)
    {
        int w = 3 + v, h = 2 + v / 2;
        var cv = new PixelCanvas(w + 1, h + 1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if ((x == 0 || x == w - 1) && y == 0) continue;
                cv.Force(x, y + 1, y == 0 ? (x >= w / 2 ? 'u' : 't') : y == h - 1 ? 'r' : 's');
            }
        return cv;
    }

    /// <summary>
    /// Fenda grande na rocha 14×28 (2 tiles): escura, mais larga no meio, com um galho; só a parte de cima da borda
    /// direita pega luz. Poucas e grandes (3% das células de rocha) leem como rocha de verdade; muitas e pequenas, como rabisco.
    /// </summary>
    static PixelCanvas DrawCrack(int v)
    {
        var cv = new PixelCanvas(14, 28);
        int x = 6 + v % 2;
        for (int y = 0; y < 28; y++)
        {
            bool wide = y > 6 && y < 20;
            cv.Force(x, y, wide ? 'k' : 'q');
            if (wide) cv.Force(x + 1, y, 'q');
            if (y < 12 && cv.Get(x + 2, y) == '.' && y % 2 == 0) cv.Force(x + (wide ? 2 : 1), y, 's');
            float n = N(x, y, 800 + v);
            if (n > 0.66f) x = Math.Min(11, x + 1); else if (n < 0.33f) x = Math.Max(2, x - 1);
            if (y == 10 + 3 * v)
                for (int i = 1; i <= 5; i++) cv.Force(x - i, y + i / 2 + i, 'q');
        }
        return cv;
    }

    /// <summary>
    /// Fósseis presos na terra e na rocha (ossos de bichos de antes do Despertar): 0 crânio 20×12, 1 costelas 22×11,
    /// 2 osso longo 14×7, 3 coluna 24×6. Osso envelhecido, claro em cima (luz) e escuro embaixo, contorno marrom-escuro.
    /// </summary>
    static PixelCanvas DrawFossil(int v)
    {
        string[][] maps =
        {
            new[] {
                "....kkkkkk..........",
                "..kkBBBBBBkkk.......",
                ".kBBBBBBBBBBBkkk....",
                "kBBBkkBBBBBBBBBBkk..",
                "kBBkdkBBBBbbBBBBBBk.",
                "kBBBkkBBBbbbbBBBBBBk",
                ".kBBBBBbbkkkbbBBBBk.",
                "..kbbbbbkk.kkbBkBk..",
                "...kkbbk...kBkBkBk..",
                "....kBBBkkkBBBBBk...",
                ".....kbbbbbbbbbk....",
                "......kkkkkkkkk.....",
            },
            Ribs(),
            new[] {
                ".kk.......kk..",
                "kBBk.....kBBk.",
                "kBBBkkkkkkBBbk",
                ".kBBBBBBBBBbk.",
                "kBbbkkkkkkbbbk",
                "kbbk.....kbdk.",
                ".kk.......kk..",
            },
            new[] {
                "..k...k...k...k...k.....",
                ".kBk.kBk.kBk.kBk.kBk....",
                "kBBBkBBBkBBBkBbBkBbbk...",
                "kbBbkbBbkbBbkbbbkbbdkkk.",
                ".kbk.kbk.kbk.kbk.kbk.kk.",
                "..k...k...k...k...k.....",
            },
        };
        // letras próprias do osso (a paleta do terreno já usa 'b' para a grama e 'k' para a tinta)
        PixelKit.P('I', "#d9cdb0"); PixelKit.P('i', "#b0a386"); PixelKit.P('j', "#7f7460"); PixelKit.P('K', "#3a3028");
        var map = maps[v];
        var cv = new PixelCanvas(map[0].Length, map.Length);
        for (int r = 0; r < map.Length; r++)
            for (int c = 0; c < map[r].Length; c++)
            {
                char ch = map[r][c];
                cv.Set(c, r, ch == 'B' ? 'I' : ch == 'b' ? 'i' : ch == 'd' ? 'j' : ch == 'k' ? 'K' : ch);
            }
        return cv;
    }

    /// <summary>
    /// Caixa de costelas vista de lado, 24×13: a coluna em cima e 4 costelas em arco "(" que descem e voltam,
    /// cada uma um pouco mais curta (a caixa afunila para trás).
    /// </summary>
    static string[] Ribs()
    {
        const int W = 24, H = 13;
        var g = new char[H, W];
        for (int r = 0; r < H; r++) for (int c = 0; c < W; c++) g[r, c] = '.';
        // coluna: vértebras claras com juntas mais escuras
        for (int c = 1; c < W - 1; c++) { g[0, c] = 'k'; g[1, c] = c % 4 == 0 ? 'b' : 'B'; g[2, c] = 'k'; }
        g[1, 0] = 'k'; g[1, W - 1] = 'k';
        // costela: deslocamento horizontal por linha (curva que vai para a esquerda e volta)
        int[] curve = { 1, 0, 0, 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < 4; i++)
        {
            int start = 3 + i * 5, len = curve.Length - i;
            for (int r = 0; r < len; r++)
            {
                int c = start + curve[r], row = 3 + r;
                if (row >= H || c + 1 >= W) continue;
                g[row, c] = r < len / 2 ? 'B' : 'b';
                if (g[row, c - 1] == '.') g[row, c - 1] = 'k';
                if (g[row, c + 1] == '.') g[row, c + 1] = 'k';
            }
            int endRow = 3 + len, endCol = start + curve[len - 1];
            if (endRow < H) g[endRow, endCol] = 'k';
        }
        var rows = new string[H];
        for (int r = 0; r < H; r++) { var sb = new System.Text.StringBuilder(); for (int c = 0; c < W; c++) sb.Append(g[r, c]); rows[r] = sb.ToString(); }
        return rows;
    }

    /// <summary>Rocha solta: forma irregular de base reta, luz do alto à direita, estratos e musgo opcional.</summary>
    static PixelCanvas DrawRock(int w, int h, int seed, bool moss)
    {
        var cv = new PixelCanvas(w + 2, h + 1);
        float cx = (w + 2) * 0.5f, rx = w * 0.5f, ry = h;
        for (int y = 0; y <= h; y++)
            for (int x = 0; x < w + 2; x++)
            {
                float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - (h + 1)) / ry; // ny: -1 no topo, 0 na base
                // contorno irregular: raio varia com o ângulo
                float ang = (float)Math.Atan2(ny, nx);
                float bump = 1f + 0.16f * (N((int)((ang + 4f) * 3f), 0, seed) - 0.5f) * 2f;
                if (nx * nx + ny * ny > bump * bump * 0.92f) continue;
                float dot = 0.6f * nx - 0.8f * ny;
                // rocha de cenário (atrás do gato): mais escura que o plano do jogo, só uma borda de luz no alto à direita
                char c = dot > 0.85f ? 't' : dot > 0.4f ? 's' : 'r';
                if (TerrainAutotile.Mod(y - x / 4 + seed, 6) == 0 && c != 'u') c = 'q';
                if (N(x, y, seed + 50) > 0.92f) c = 'q';
                cv.Force(x, y, c);
            }
        if (moss)
            for (int x = 0; x < cv.W; x++)
                for (int y = 0; y < cv.H; y++)
                    if (cv.Px[x, y] != '.' && cv.Get(x, y - 1) == '.' && N(x, y, seed + 60) > 0.4f) { cv.Force(x, y, 'v'); break; }
        PixelKit.Outline(cv, 'q');
        return cv;
    }

    /// <summary>
    /// Placa da estrada negra dos Primatas: pedra lisa e reta demais para ser natural, com uma costura de cristal
    /// ciano apagado e a ponta direita quebrada. Fica meio enterrada no chão ou embutida na rocha.
    /// </summary>
    static PixelCanvas DrawPrimatePlate(int w, int h, int v)
    {
        var cv = new PixelCanvas(w, h);
        for (int x = 0; x < w; x++)
        {
            int cut = x >= w - 6 ? (int)(N(x, v, 700) * (x - (w - 7))) : 0; // ponta quebrada, irregular
            for (int y = cut; y < h; y++)
            {
                char c = y == cut ? 'w' : y == cut + 1 ? 'y' : 'x';
                if (y == h / 2 + 1 && x > 1 && x < w - 3 && (x / 5 + v) % 3 != 0) c = 'z'; // costura de cristal, com falhas
                cv.Force(x, y, c);
            }
        }
        for (int y = 0; y < h; y++) cv.Force(0, y, 'k');
        return cv;
    }
}
