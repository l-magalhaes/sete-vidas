// Floresta Silenciosa, F5 (Docs/Floresta/plano-implementacao.md): tilesets do chão da floresta (solo, praga e concreto dos
// Primatas), vinha de escalar, massa da floresta (fora das salas), paredes do fundo, rampas, detalhes (samambaias, fungos,
// brotos da praga, musgo, entulho, raízes, vergalhão) e as camadas de fundo noturnas (céu, copas, árvores, troncos).
// Os nomes dos tiles vêm de Assets/_Game/Scripts/World/Scenery/ForestAutotile.cs (a mesma regra que o Unity usa).
// Guia: Docs/Arte/floresta-guia-visual.md. Paleta: Docs/Arte/paleta_floresta.gpl. Luz: a LUA, no alto à esquerda
// (bordas viradas para a esquerda e para cima pegam o luar frio). Fundo noturno abaixo de 25% de valor.
// Cores reservadas: turquesa do pogo e amarelo da rachadura NUNCA aparecem aqui; roxo e verde-ácido só na praga; a hera
// de escalar (#4f5a2e / #8a9a4e) só na vinha.
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.World.Scenery;

public static class ForestSpriteGen
{
    static string s_template;
    static string s_terrain, s_bg;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_terrain = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Terrain");
        s_bg = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Backgrounds/Flo");

        foreach (var m in ForestAutotile.Materials)
            foreach (var name in ForestAutotile.AllTileNames(m))
                Write(GroundTile(name), name);
        foreach (var name in ForestAutotile.AllExtraNames())
            Write(ExtraTile(name), name);
        foreach (var name in ForestAutotile.AllDetailNames())
            Write(Detail(name), name);

        Backgrounds();
        Preview(Path.Combine(previewDir, "floresta_tiles_f5.png"));
    }

    static void Write(PixelCanvas cv, string name)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_terrain, name + ".png"), name, s_template, false);
    }

    static float N(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    // =============================================================== paleta

    static void Palette(string material)
    {
        PixelKit.Pal.Clear();
        bool blight = material == ForestAutotile.Blight;
        PixelKit.P('k', "#0e0c10");                                                   // contorno
        if (!blight)
        {
            // musgo (verdes azulados, longe do matinho #456633 e da hera #4f5a2e/#8a9a4e)
            PixelKit.P('a', "#16241f"); PixelKit.P('b', "#22382e"); PixelKit.P('c', "#34503f"); PixelKit.P('e', "#5e7e6a");
            PixelKit.P('f', "#8aa69a");                                               // luar nas pontas
            PixelKit.P('l', "#4a3424");                                               // folha caída
            // terra escura e raiz
            PixelKit.P('m', "#161012"); PixelKit.P('n', "#231a18"); PixelKit.P('o', "#33261f"); PixelKit.P('p', "#4c3828");
            // rocha fria
            PixelKit.P('q', "#121118"); PixelKit.P('r', "#1c1b26"); PixelKit.P('s', "#282836"); PixelKit.P('t', "#3a3b4c"); PixelKit.P('u', "#55596c");
        }
        else
        {
            // musgo morto arroxeado e terra com veias da praga
            PixelKit.P('a', "#1a1420"); PixelKit.P('b', "#2e2234"); PixelKit.P('c', "#4a3852"); PixelKit.P('e', "#7a6482");
            PixelKit.P('f', "#a08aac");
            PixelKit.P('l', "#3a2a26");
            PixelKit.P('m', "#140e14"); PixelKit.P('n', "#241a22"); PixelKit.P('o', "#3a2a34"); PixelKit.P('p', "#4e3a44");
            PixelKit.P('q', "#110e16"); PixelKit.P('r', "#1b1622"); PixelKit.P('s', "#272030"); PixelKit.P('t', "#383044"); PixelKit.P('u', "#524a60");
        }
        PixelKit.P('g', "#5b2a6e"); PixelKit.P('G', "#30143a"); PixelKit.P('h', "#8fd14f");   // praga (roxo, roxo fundo, verde-ácido)
        // concreto dos Primatas
        PixelKit.P('A', "#1e2022"); PixelKit.P('B', "#2e3234"); PixelKit.P('C', "#3c4042"); PixelKit.P('D', "#4e5456");
        PixelKit.P('E', "#686e70"); PixelKit.P('R', "#4a2e22"); PixelKit.P('S', "#623e2c");
        // casca e hera de escalar (a linguagem do Ladino)
        PixelKit.P('w', "#141012"); PixelKit.P('x', "#2a1e18"); PixelKit.P('y', "#3e2e24"); PixelKit.P('z', "#5a4636");
        if (blight) { PixelKit.P('x', "#241a1e"); PixelKit.P('y', "#352630"); PixelKit.P('z', "#4c3a46"); } // casca da praga (troncos)
        PixelKit.P('i', "#3a4422"); PixelKit.P('j', "#687538"); PixelKit.P('J', "#8c9852"); PixelKit.P('V', "#161a0c"); // hera da Fortaleza, no tom da noite
        // massa da floresta
        if (!blight)
        {
            PixelKit.P('1', "#09080b"); PixelKit.P('2', "#0f0d10"); PixelKit.P('3', "#141113"); PixelKit.P('4', "#1a1514"); PixelKit.P('5', "#121a18");
        }
        else
        {
            PixelKit.P('1', "#09070b"); PixelKit.P('2', "#100b12"); PixelKit.P('3', "#18101a"); PixelKit.P('4', "#22161e"); PixelKit.P('5', "#1e0e24");
        }
        // fungo (azul da magia natural: nunca o turquesa do pogo) e haste
        PixelKit.P('F', "#348ca0"); PixelKit.P('H', "#58c4e8"); PixelKit.P('W', "#c8d4dc");
        // templo dos Primatas: pedra cinza (luar à esquerda), grama viva e flores rosa e brancas
        PixelKit.P('K', "#24242c"); PixelKit.P('L', "#3e3f4a"); PixelKit.P('M', "#555764"); PixelKit.P('N', "#6e717c"); PixelKit.P('O', "#8c909a");
        PixelKit.P('P', "#1c3420"); PixelKit.P('Q', "#2c5028"); PixelKit.P('T', "#447034"); PixelKit.P('U', "#64944a"); PixelKit.P('X', "#94bc68");
        PixelKit.P('Y', "#d07aa8"); PixelKit.P('Z', "#f0a8cc"); PixelKit.P('d', "#e8e2d4"); PixelKit.P('v', "#b8a0d8");
        PixelKit.P('6', "#17171d"); PixelKit.P('7', "#202028"); PixelKit.P('8', "#2a2b34"); PixelKit.P('9', "#33343e");   // fundo de pedra
    }

    // --------------------------------------------------------------- templo dos Primatas

    /// <summary>Bloco de pedra 16 × 8 em fiadas desencontradas (junta a cada 16, meio bloco na fiada de baixo).</summary>
    static char TempleStone(int x, int y, int seed, int variant)
    {
        int r = y % 8;
        int joint = y < 8 ? 0 : 8;
        if (r == 7) return 'K';
        if (x == joint) return 'K';
        if (r == 0 || x == joint + 1) return 'N';                                          // luz de cima e da esquerda
        if (r == 6 || x == TerrainAutotile.Mod(joint - 1, 16)) return 'L';
        float n = N(x, y, seed);
        if (n > 0.93f) return 'O';
        if (n < 0.1f) return 'L';
        return 'M';
    }

    /// <summary>
    /// Pedra do templo: blocos cinza com luar; variação 1 rachada, 2 com musgo nas juntas. Topo: grama viva de 4–6 px
    /// que cai pela borda em franjas (como os terraços da referência).
    /// </summary>
    static PixelCanvas TempleTile(string band, int mask, int variant)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 811 + variant * 13 + mask;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++) cv.Force(x, y, TempleStone(x, y, seed, variant));
        if (band == "miolo" && variant == 1)
        {
            int x = 4 + (int)(N(3, 3, seed) * 7f);
            for (int y = 1; y < 14; y++) { cv.Force(x, y, 'K'); if (N(x, y, seed + 2) > 0.55f) x += N(x, y, seed + 3) > 0.5f ? 1 : -1; x = Math.Max(2, Math.Min(13, x)); }
        }
        if (band == "miolo" && variant == 2)
            for (int x = 0; x < 16; x++) { if (N(x, 7, seed + 4) > 0.45f) cv.Force(x, 7, N(x, 6, seed) > 0.5f ? 'Q' : 'P'); if (N(x, 15, seed + 5) > 0.6f) cv.Force(x, 15, 'P'); }
        bool up = (mask & TerrainAutotile.Up) != 0, right = (mask & TerrainAutotile.Right) != 0;
        bool down = (mask & TerrainAutotile.Down) != 0, left = (mask & TerrainAutotile.Left) != 0;
        if (band == "topo")
        {
            for (int x = 0; x < 16; x++)
            {
                int depth = 4 + (int)(N(x, variant, seed + 5) * 3f);                    // 4..6 px de grama
                if (variant == 2 && N(x, 9, seed + 8) > 0.6f) depth += 2;               // franja caindo
                for (int y = 0; y < depth; y++)
                    cv.Force(x, y, y == 0 ? (N(x, 0, seed + 6) > 0.6f ? 'X' : 'U') : y == depth - 1 ? 'P' : y < 3 ? 'T' : 'Q');
                if (variant == 1 && N(x, 2, seed + 7) > 0.8f) cv.Force(x, 1, 'U');
            }
        }
        for (int i = 0; i < 16; i++)
        {
            if (left) { cv.Force(0, i, 'k'); if (!(up && i < 6)) cv.Force(1, i, 'O'); }
            if (right) { cv.Force(15, i, 'k'); if (!(up && i < 5)) cv.Force(14, i, 'L'); }
            if (down) { cv.Force(i, 15, 'k'); cv.Force(i, 14, 'L'); }
        }
        if (up && left)
        {
            cv.Force(0, 0, '.'); cv.Force(1, 0, 'k'); cv.Force(0, 1, 'k');
            for (int y = 2; y < 8; y++) if (N(0, y, seed + 9) > 0.5f) cv.Force(0, y, y < 6 ? 'Q' : 'P');  // grama caindo pela quina
        }
        if (up && right)
        {
            cv.Force(15, 0, '.'); cv.Force(14, 0, 'k'); cv.Force(15, 1, 'k');
            for (int y = 2; y < 8; y++) if (N(15, y, seed + 10) > 0.5f) cv.Force(15, y, y < 6 ? 'Q' : 'P');
        }
        return cv;
    }

    static PixelCanvas TempleBack(int v)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 640 + v * 11;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int joint = y < 8 ? 0 : 8;
                char c = y % 8 == 7 || x == joint ? '6' : N(x, y, seed) > 0.88f ? '8' : '7';
                cv.Force(x, y, c);
            }
        if (v == 1) for (int x = 2; x < 14; x++) if (N(x, 3, seed + 1) > 0.5f) cv.Force(x, 7, N(x, 4, seed) > 0.5f ? 'P' : 'Q'); // musgo na junta
        return cv;
    }

    /// <summary>Tufo de capim 16 × 8 (3 a 5 folhas finas, pontas claras).</summary>
    static PixelCanvas Grass(int v)
    {
        var cv = new PixelCanvas(16, 8);
        int blades = 4 + v;
        for (int b = 0; b < blades; b++)
        {
            int x0 = 3 + b * 10 / blades + (int)(N(b, v, 900) * 2f);
            int h = 3 + (int)(N(b, v + 3, 901) * 4f);
            int lean = N(b, v + 5, 902) > 0.5f ? 1 : -1;
            for (int i = 0; i < h; i++)
            {
                int x = x0 + (i > h / 2 ? lean : 0);
                cv.Force(x, 7 - i, i == h - 1 ? 'X' : i < 2 ? 'Q' : 'U');
            }
        }
        return cv;
    }

    /// <summary>Florzinha 8 × 8: rosa, rosa-clara, branca ou lilás clara, com caule e folha.</summary>
    static PixelCanvas Flower(int v)
    {
        var cv = new PixelCanvas(8, 8);
        char petal = v == 0 ? 'Y' : v == 1 ? 'Z' : v == 2 ? 'd' : 'v';
        char center = v == 2 ? 'Y' : 'd';
        int cx = 3 + v % 2, top = 1 + v % 3;
        for (int y = top + 2; y < 8; y++) cv.Force(cx, y, 'Q');
        cv.Force(cx + 1, 6, 'T'); cv.Force(cx - 1, 5, 'U');
        cv.Force(cx, top, petal); cv.Force(cx - 1, top + 1, petal); cv.Force(cx + 1, top + 1, petal); cv.Force(cx, top + 2, petal);
        cv.Force(cx, top + 1, center);
        if (v == 3) { cv.Force(cx + 3, top + 3, 'Y'); cv.Force(cx + 3, top + 4, 'Q'); cv.Force(cx + 3, 7, 'Q'); }
        return cv;
    }

    // =============================================================== chão

    /// <summary>tile_&lt;material&gt;_&lt;faixa&gt;_&lt;máscara&gt;_&lt;variação&gt; → 16×16.</summary>
    static PixelCanvas GroundTile(string name)
    {
        // "tile_flo_solo_topo_b_2" → material flo_solo; "tile_pri_concreto_miolo_0_1" → pri_concreto
        string[] p = name.Split('_');
        string material = p[1] + "_" + p[2];
        string band = p[3];
        int mask = Convert.ToInt32(p[4], 16);
        int variant = int.Parse(p[5]);
        Palette(material);
        if (ForestAutotile.IsConcrete(material)) return ConcreteTile(band, mask, variant);
        if (material == ForestAutotile.Temple) return TempleTile(band, mask, variant);

        bool blight = material == ForestAutotile.Blight;
        int seed = (blight ? 71 : 37) * 31 + variant * 7 + mask;
        var cv = new PixelCanvas(16, 16);
        bool phased = band == "rocha" || band == "transicao";
        int phase = phased ? variant : 0;
        bool dirt = band == "terra" || band == "topo";
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                cv.Force(x, y, dirt ? Dirt(x, y, seed, variant) : RockPx(x + 16 * phase, y, seed));

        if (band == "topo") MossTop(cv, seed, variant, blight);
        if (band == "terra" && variant == 2) Root(cv, seed);
        if (band == "transicao") DirtIntoRock(cv, seed, phase);
        if (blight && band == "terra" && variant == 1) Vein(cv, seed);
        Edges(cv, mask, dirt ? "terra" : "rocha", band == "topo");
        return cv;
    }

    static char Dirt(int x, int y, int seed, int variant)
    {
        float n = N(x, y, seed);
        if (n < 0.16f) return 'm';
        if (n > 0.92f) return 'o';
        float pb = N(x / 2, y, seed + 9);
        if (pb > (variant == 1 ? 0.87f : 0.96f)) return y % 2 == 0 ? 't' : 's';   // seixos
        return 'n';
    }

    /// <summary>Rocha fria com estratos quase horizontais (1 px de queda a cada 4), contínuos entre tiles (4 fases).</summary>
    static char RockPx(int xw, int y, int seed)
    {
        int s = y - xw / 4;
        int f = TerrainAutotile.Mod(s, 16);
        float n = N(xw, y, seed);
        if (f == 0) return n > 0.85f ? 'r' : 'q';
        if (f == 1) return n > 0.6f ? 's' : 'r';
        int layer = TerrainAutotile.Mod((s - f) / 16, 3);
        char c = layer == 1 ? 's' : 'r';
        if (f == 9 && n > 0.5f) c = 'q';
        if (n < 0.06f) c = 'q'; else if (n > 0.975f) c = 't';
        return c;
    }

    static void DirtIntoRock(PixelCanvas cv, int seed, int phase)
    {
        for (int x = 0; x < 16; x++)
        {
            double t = 2 * Math.PI * (x + 16 * phase) / 64.0;
            int depth = 6 + (int)Math.Round(2.0 * Math.Sin(t) + 1.2 * Math.Sin(3 * t + 1) + 0.7 * Math.Sin(5 * t + 2));
            depth = Math.Max(2, Math.Min(10, depth));
            for (int y = 0; y < depth; y++) cv.Force(x, y, Dirt(x, y, seed + 3, 0));
            cv.Force(x, depth, 'm');
        }
    }

    /// <summary>Musgo nas 3–5 primeiras linhas, franja recortada, pontas com luar e folhas caídas por cima.</summary>
    static void MossTop(PixelCanvas cv, int seed, int variant, bool blight)
    {
        for (int x = 0; x < 16; x++)
        {
            int depth = 4 + (int)(N(x, variant, seed + 3) * 4f);                     // 4..7 px
            for (int y = 0; y < depth; y++)
            {
                char c = y == depth - 1 ? 'a' : y == 0 ? (N(x, 0, seed + 4) > 0.7f ? 'e' : 'c') : (N(x, y, seed + 5) > 0.55f ? 'c' : 'b');
                cv.Force(x, y, c);
            }
            if (N(x, 1, seed + 6) > 0.86f) cv.Force(x, 0, 'f');                         // luar
            if (N(x, 2, seed + 7) > (variant == 2 ? 0.7f : 0.88f)) cv.Force(x, 1, 'l');  // folha caída
        }
        // praga: um pontinho de verde-ácido no musgo morto (raro: só lê de perto)
        if (blight && variant == 1) cv.Force(4 + (int)(N(1, 1, seed) * 8f), 1, 'h');
    }

    static void Root(PixelCanvas cv, int seed)
    {
        int x = 3 + (int)(N(1, 1, seed) * 8f);
        for (int y = 2; y < 14; y++)
        {
            cv.Force(x, y, 'p');
            if (N(x, y, seed + 1) > 0.6f) x += N(x, y, seed + 2) > 0.5f ? 1 : -1;
            x = Math.Max(1, Math.Min(14, x));
        }
    }

    /// <summary>Veia da praga: linha roxa torta com o centro mais fundo (só no material da praga).</summary>
    static void Vein(PixelCanvas cv, int seed)
    {
        // trecho curto (5–9 px) descendo na diagonal, com um galhinho: lê como veia, não como listra
        int y = 3 + (int)(N(2, 2, seed) * 6f);
        int x0 = 2 + (int)(N(3, 2, seed) * 6f), len = 5 + (int)(N(4, 2, seed) * 5f);
        int dir = N(5, 2, seed) > 0.5f ? 1 : -1;
        int x = x0;
        for (int i = 0; i < len; i++)
        {
            cv.Force(x, y, i == len / 2 ? 'g' : 'G');
            x += dir;
            if (N(x, y, seed + 11) > 0.45f) y++;
            if (x < 1 || x > 14 || y > 13) break;
            if (i == 2) cv.Force(x, y - 1, 'G');
        }
    }

    /// <summary>Lados expostos: contorno escuro; à esquerda e em cima o luar; embaixo, raízes aparecendo.</summary>
    static void Edges(PixelCanvas cv, int mask, string body, bool moss)
    {
        char dark = body == "rocha" ? 'q' : 'm';
        char light = body == "rocha" ? 'u' : 'o';
        bool up = (mask & TerrainAutotile.Up) != 0, right = (mask & TerrainAutotile.Right) != 0;
        bool down = (mask & TerrainAutotile.Down) != 0, left = (mask & TerrainAutotile.Left) != 0;
        for (int i = 0; i < 16; i++)
        {
            if (right) { cv.Force(15, i, 'k'); char c = cv.Get(14, i); if (c != 'a' && c != 'b' && c != 'c') cv.Force(14, i, dark); }
            if (left) { cv.Force(0, i, 'k'); if (i > 2 || !moss) cv.Force(1, i, light); }
            if (down)
            {
                cv.Force(i, 15, 'k'); cv.Force(i, 14, dark);
                if (body == "terra" && N(i, 13, 77) > 0.72f) { cv.Force(i, 13, 'p'); cv.Force(i, 14, 'p'); }
            }
        }
        if (up && left) { cv.Force(0, 0, '.'); cv.Force(1, 0, '.'); cv.Force(0, 1, '.'); cv.Force(1, 1, 'k'); cv.Force(2, 0, 'k'); cv.Force(0, 2, 'k'); }
        if (up && right) { cv.Force(15, 0, '.'); cv.Force(14, 0, '.'); cv.Force(15, 1, '.'); cv.Force(14, 1, 'k'); cv.Force(13, 0, 'k'); cv.Force(15, 2, 'k'); }
        if (down && left) { cv.Force(0, 15, '.'); cv.Force(1, 15, 'k'); cv.Force(0, 14, 'k'); }
        if (down && right) { cv.Force(15, 15, '.'); cv.Force(14, 15, 'k'); cv.Force(15, 14, 'k'); }
    }

    // --------------------------------------------------------------- concreto

    /// <summary>
    /// Laje de concreto (linhas retas, 90°): forma de madeira marcada a cada 4 px, junta a cada 16 px; variação 1 rachada,
    /// 2 com escorrido de ferrugem. Topo: musgo por cima da laje (nada fica limpo). Luar na borda esquerda.
    /// </summary>
    static PixelCanvas ConcreteTile(string band, int mask, int variant)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 211 + variant * 13 + mask;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float n = N(x, y, seed);
                char c = n < 0.18f ? 'B' : n > 0.9f ? 'D' : 'C';
                if (y % 4 == 3 && N(x, y, seed + 1) > 0.35f) c = 'B';                 // marcas da forma
                if (y == 15 && variant == 0 && band == "miolo") c = 'A';               // junta de laje (só às vezes: lajes grandes)
                cv.Force(x, y, c);
            }
        if (band == "miolo" && variant == 1)
        {
            int x = 4 + (int)(N(3, 3, seed) * 6f);
            for (int y = 0; y < 15; y++) { cv.Force(x, y, 'A'); if (N(x, y, seed + 2) > 0.55f) x += N(x, y, seed + 3) > 0.5f ? 1 : -1; x = Math.Max(1, Math.Min(13, x)); }
        }
        if (band == "miolo" && variant == 2)
        {
            int x = 3 + (int)(N(4, 4, seed) * 9f);
            for (int y = 0; y < 13; y++) { cv.Force(x, y, y < 5 ? 'S' : 'R'); if (y > 6 && N(x, y, seed + 4) > 0.6f) cv.Force(x + 1, y, 'R'); }
        }
        if (band == "topo")
        {
            for (int x = 0; x < 16; x++)
            {
                int depth = 1 + (int)(N(x, variant, seed + 5) * 3f);                  // 1..3 px de musgo
                for (int y = 0; y < depth; y++) cv.Force(x, y, y == 0 ? (N(x, 0, seed + 6) > 0.7f ? 'e' : 'c') : 'b');
                cv.Force(x, depth, 'a');
                if (depth == 3 && N(x, 5, seed + 7) > 0.6f) cv.Force(x, depth + 1, 'a'); // musgo escorrendo
                cv.Force(x, 5, cv.Get(x, 5) == 'a' ? 'a' : 'E');                      // quina da laje
            }
        }
        bool up = (mask & TerrainAutotile.Up) != 0, right = (mask & TerrainAutotile.Right) != 0;
        bool down = (mask & TerrainAutotile.Down) != 0, left = (mask & TerrainAutotile.Left) != 0;
        for (int i = 0; i < 16; i++)
        {
            if (left) { cv.Force(0, i, 'k'); if (i > 3 || !up) cv.Force(1, i, 'E'); }
            if (right) { cv.Force(15, i, 'k'); cv.Force(14, i, 'B'); }
            if (down) { cv.Force(i, 15, 'k'); cv.Force(i, 14, 'A'); }
        }
        // concreto quebra em degrau (90°), não arredondado
        if (up && left) { cv.Force(0, 0, '.'); cv.Force(0, 1, 'k'); cv.Force(1, 0, 'k'); }
        if (up && right) { cv.Force(15, 0, '.'); cv.Force(15, 1, 'k'); cv.Force(14, 0, 'k'); }
        return cv;
    }

    // =============================================================== vinha, massa, fundo, rampas

    static PixelCanvas ExtraTile(string name)
    {
        if (name.Contains("_tronco_"))
        {
            // tile_flo_solo_tronco_<lados>_<v> / tile_flo_praga_tronco_...
            string[] p = name.Split('_');
            string fam = p[1] + "_" + p[2];
            Palette(fam);
            return Trunk(Convert.ToInt32(p[4], 16), int.Parse(p[5]), fam == ForestAutotile.Blight);
        }
        if (name.Contains("_escalavel_"))
        {
            // tile_flo_solo_escalavel_<lados>_<v> / tile_pri_concreto_escalavel_...
            string[] p = name.Split('_');
            string fam = p[1] + "_" + p[2];
            Palette(fam);
            return Climb(fam, Convert.ToInt32(p[4], 16), int.Parse(p[5]));
        }
        if (name.Contains("_massa_"))
        {
            string[] p = name.Split('_');
            string fam = p[1] + "_" + p[2];
            Palette(fam);
            return Mass(fam == ForestAutotile.Blight, int.Parse(p[4]));
        }
        if (name.Contains("_fundo_"))
        {
            string[] p = name.Split('_');
            string fam = p[1] + "_" + p[2];
            Palette(fam);
            return BackWall(fam, int.Parse(p[4]));
        }
        if (name.Contains("_rampa_"))
        {
            // env_flo_ter_rampa_12a_r / env_flo_praga_ter_rampa_11_l
            bool blight = name.StartsWith("env_flo_praga_");
            Palette(blight ? ForestAutotile.Blight : ForestAutotile.Soil);
            string tail = name.Substring(name.IndexOf("_rampa_") + 7);
            string kind = tail.Substring(0, tail.LastIndexOf('_'));
            return SlopePiece(kind, tail.EndsWith("_r"), blight);
        }
        throw new ArgumentException("tile desconhecido: " + name);
    }

    /// <summary>
    /// Vinha: casca de tronco (veio vertical, luar à esquerda) ou concreto, com a hera da Fortaleza (dois ramos e folhas de
    /// 6 px com contorno), em tons de noite.
    /// </summary>
    static PixelCanvas Climb(string fam, int side, int v)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 300 + v * 17 + side;
        bool concrete = fam == ForestAutotile.Concrete;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char c;
                if (concrete) c = y % 4 == 3 ? 'B' : N(x, y, seed) < 0.2f ? 'B' : 'C';
                else
                {
                    // veio da casca: faixas verticais tortas
                    int g = TerrainAutotile.Mod(x + (int)(1.5 * Math.Sin((y + v * 5) * 0.5)), 5);
                    c = g == 0 ? 'w' : g == 1 ? 'y' : 'x';
                    if (N(x, y, seed + 1) > 0.93f) c = 'z';
                }
                cv.Force(x, y, c);
            }
        // a hera da Fortaleza (mesmo desenho do primeiro mapa, pedido do usuário 14/11/2026), mais escura porque é noite:
        // dois ramos que ondulam de cima a baixo, folhas de hera de 6 px alternando os lados (folha j, brilho J, talo i)
        // e um contorno escuro (V) que separa a planta da casca
        int phase = (v % 2) * 2;
        for (int s = 0; s < 2; s++)
        {
            for (int y = 0; y < 16; y++)
            {
                int wobble = (y / 4) % 2;
                cv.Force(s == 0 ? 3 + wobble : 12 - wobble, y, 'i');
            }
            for (int k = 0; k < 4; k++)
            {
                int y0 = 1 + ((k * 4 + phase + s * 2) % 16);
                if (y0 > 14) continue;
                int stem = s == 0 ? 3 + (y0 / 4) % 2 : 12 - (y0 / 4) % 2;
                int d = (k + s) % 2 == 0 ? 1 : -1;
                if (stem + d * 3 < 0 || stem + d * 3 > 15) d = -d;
                cv.Force(stem + d, y0, 'i');
                cv.Force(stem + d * 2, y0, 'j'); cv.Force(stem + d * 3, y0, 'j');
                cv.Force(stem + d * 2, y0 - 1, 'J'); cv.Force(stem + d * 3, y0 - 1, 'j');
                cv.Force(stem + d * 2, y0 + 1, 'j');
            }
        }
        var plant = cv.Clone();
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char c = plant.Get(x, y);
                if (c == 'i' || c == 'j' || c == 'J') continue;
                bool touches = false;
                for (int oy = -1; oy <= 1 && !touches; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (Math.Abs(ox) + Math.Abs(oy) != 1) continue;
                        char n = plant.Get(x + ox, y + oy);
                        if (n == 'j' || n == 'J' || (n == 'i' && ox != 0)) { touches = true; break; }
                    }
                if (touches) cv.Force(x, y, 'V');
            }        if ((side & TerrainAutotile.Left) != 0) for (int y = 0; y < 16; y++) if (N(0, y, seed + 4) > 0.5f) cv.Force(0, y, 'k');
        if ((side & TerrainAutotile.Right) != 0) for (int y = 0; y < 16; y++) if (N(15, y, seed + 5) > 0.5f) cv.Force(15, y, 'k');
        return cv;
    }

    /// <summary>
    /// Tronco (coluna fina de chão): casca com veio vertical torto, luar na borda esquerda, sombra na direita, nós e um
    /// pouco de musgo; na praga, uma veia roxa. Contínuo de cima a baixo (o veio depende só de x).
    /// </summary>
    static PixelCanvas Trunk(int side, int v, bool blight)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 400 + v * 19 + side;
        bool left = (side & TerrainAutotile.Left) != 0, right = (side & TerrainAutotile.Right) != 0;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int g = TerrainAutotile.Mod(x + ((y + v * 3) / 6) % 2, 5);                     // veio quase reto, com degraus
                char c = g == 0 ? 'w' : g == 2 ? 'y' : 'x';
                if (N(x, y, seed) > 0.95f) c = 'w';
                cv.Force(x, y, c);
            }
        // nó da madeira
        if (v == 1) { int nx = 5 + (int)(N(1, 1, seed) * 5f); for (int dy = 0; dy < 3; dy++) for (int dx = 0; dx < 3; dx++) cv.Force(nx + dx, 6 + dy, dx == 1 && dy == 1 ? 'w' : 'y'); }
        // musgo (verde) ou veia (praga) subindo pela casca
        if (v == 2)
        {
            int mx = 3 + (int)(N(2, 1, seed) * 8f);
            for (int y = 0; y < 16; y++) if (N(mx, y, seed + 1) > 0.35f) cv.Force(mx + (y / 4) % 2, y, blight ? (y % 5 == 0 ? 'g' : 'G') : (y % 3 == 0 ? 'c' : 'b'));
        }
        for (int y = 0; y < 16; y++)
        {
            if (left) { cv.Force(0, y, 'k'); cv.Force(1, y, 'z'); if (N(2, y, seed + 2) > 0.5f) cv.Force(2, y, 'z'); }
            if (right) { cv.Force(15, y, 'k'); cv.Force(14, y, 'w'); }
        }
        return cv;
    }

    /// <summary>
    /// Massa da floresta (fora das salas): terra quase preta com raízes e troncos emaranhados em tom um pouco acima, e
    /// manchas de folhagem escura. Baixo contraste de propósito: as emendas entre tiles quase não aparecem.
    /// </summary>
    static PixelCanvas Mass(bool blight, int v)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 500 + v * 23 + (blight ? 7 : 0);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float n = N(x / 2, y / 2, seed);
                cv.Force(x, y, n < 0.3f ? '1' : n > 0.82f ? '3' : '2');
            }
        // duas raízes: uma diagonal e uma quase vertical
        for (int k = 0; k < 2; k++)
        {
            float x = 2 + N(k, v, seed + 1) * 12f;
            float slope = k == 0 ? 0.6f + N(v, k, seed + 2) * 0.6f : (N(v, k, seed + 3) - 0.5f) * 0.4f;
            for (int y = 0; y < 16; y++)
            {
                int xi = (int)x;
                cv.Force(xi, y, '4');
                if (k == 0) cv.Force(xi + 1, y, '3');
                x += slope * (k == 0 && v % 2 == 1 ? -1 : 1);
                if (x < 0) x += 16; if (x > 15) x -= 16;
            }
        }
        // folhagem escura (ou a praga roxa funda)
        for (int i = 0; i < 6; i++)
        {
            int fx = (int)(N(i, v, seed + 4) * 16f), fy = (int)(N(v, i, seed + 5) * 16f);
            if (N(i, i, seed + 6) > 0.5f) { cv.Force(fx, fy, '5'); cv.Force(fx + 1, fy, '5'); cv.Force(fx, fy + 1, '5'); }
        }
        return cv;
    }

    /// <summary>Parede do fundo dentro de uma sala: tábuas velhas e escuras (o posto, a cabana) ou concreto escuro.</summary>
    static PixelCanvas BackWall(string fam, int v)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 600 + v * 11;
        if (fam == ForestAutotile.Temple) return TempleBack(v);
        if (fam == ForestAutotile.Concrete)
        {
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    cv.Force(x, y, y == 15 || x == 15 ? '1' : N(x, y, seed) < 0.3f ? 'A' : 'B');
            return cv;
        }
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int plank = x / 4;
                char c = x % 4 == 0 ? 'w' : (plank + v) % 3 == 0 ? 'y' : 'x';
                if (N(x, y, seed) > 0.94f) c = 'w';                                    // nó da madeira
                if ((y == 3 || y == 12) && x % 4 == 2) c = 'z';                        // prego
                cv.Force(x, y, c);
            }
        return cv;
    }

    static PixelCanvas SlopePiece(string kind, bool risesRight, bool blight)
    {
        var cv = new PixelCanvas(16, 16);
        int seed = 700 + Array.IndexOf(TerrainAutotile.SlopePieceKinds, kind) * 13 + (blight ? 5 : 0);
        for (int x = 0; x < 16; x++)
        {
            float xc = x + 0.5f;
            float surface = kind == "11" ? xc : kind == "12a" ? xc * 0.5f : 8f + xc * 0.5f;
            int top = 16 - (int)Math.Round(surface);
            int moss = 3 + (int)(N(x, 0, seed) * 2f);
            for (int y = Math.Max(0, top); y < 16; y++)
            {
                int d = y - top;
                char c = d >= moss ? Dirt(x, y, seed, 0) : d == 0 ? (N(x, y, seed + 1) > 0.7f ? 'e' : 'c') : d == moss - 1 ? 'a' : (N(x, y, seed + 2) > 0.5f ? 'c' : 'b');
                cv.Force(x, y, c);
            }
            if (top >= 0 && top < 16 && N(x, 3, seed) > 0.85f) cv.Force(x, top, 'f');
        }
        return risesRight ? cv : cv.Mirror();
    }

    // =============================================================== detalhes

    static PixelCanvas Detail(string name)
    {
        bool blight = name.Contains("_praga");
        Palette(blight ? ForestAutotile.Blight : ForestAutotile.Soil);
        int v = name.EndsWith("_l") || name.EndsWith("_r")
            ? int.Parse(name.Substring(0, name.Length - 2).Substring(name.Substring(0, name.Length - 2).LastIndexOf('_') + 1))
            : int.Parse(name.Substring(name.LastIndexOf('_') + 1));
        if (ForestAutotile.IsFern(name))
        {
            var fern = Fern(v, blight);
            if (name.EndsWith("_l")) return Bend(fern, -1);
            if (name.EndsWith("_r")) return Bend(fern, 1);
            return fern;
        }
        if (name.StartsWith("env_flo_veg_fungo_")) return Fungus(v);
        if (name.StartsWith("env_flo_praga_broto_")) return Sprout(v);
        if (name.StartsWith("env_pri_ter_musgo_")) return ConcreteMoss(v);
        if (name.StartsWith("env_pri_ter_entulho_")) return Debris(v);
        if (name.StartsWith("env_flo_veg_raiz_pendente")) return HangingRoot(v, blight);
        if (name.StartsWith("env_pri_ter_vergalhao_")) return Rebar(v);
        if (name.StartsWith("env_pri_tem_capim_")) return Grass(v);
        if (name.StartsWith("env_pri_tem_flor_")) return Flower(v);
        throw new ArgumentException("detalhe desconhecido: " + name);
    }

    /// <summary>Samambaia 16×8 sobre o chão: 3–4 frondes em arco, a do meio mais alta; luar nas pontas.</summary>
    static PixelCanvas Fern(int v, bool blight)
    {
        var cv = new PixelCanvas(16, 8);
        int fronds = 3 + v % 2;
        for (int f = 0; f < fronds; f++)
        {
            float baseX = 7.5f + (f - (fronds - 1) * 0.5f) * 1.5f;
            float lean = (f - (fronds - 1) * 0.5f) * (1.6f + N(f, v, 800) * 0.8f);
            int h = 4 + (int)(N(v, f, 801) * 3f) + (f == fronds / 2 ? 1 : 0);
            for (int i = 0; i < h; i++)
            {
                float t = i / (float)h;
                int x = (int)Math.Round(baseX + lean * t * t * 1.6f);
                int y = 7 - i;
                cv.Force(x, y, i == h - 1 ? (N(x, y, 802 + v) > 0.4f ? 'f' : 'e') : i < 2 ? 'b' : 'c');
                // folíolos alternados
                if (i > 0 && i < h - 1 && i % 2 == 0) cv.Force(x + (lean >= 0 ? 1 : -1), y, 'c');
                if (i > 0 && i < h - 1 && i % 2 == 1) cv.Force(x - (lean >= 0 ? 1 : -1), y, blight ? 'b' : 'e');
            }
        }
        return cv;
    }

    static PixelCanvas Bend(PixelCanvas src, int dx)
    {
        var cv = new PixelCanvas(src.W, src.H);
        for (int x = 0; x < src.W; x++)
            for (int y = 0; y < src.H; y++)
            {
                int shift = y <= 2 ? dx * 2 : y <= 4 ? dx : 0;
                char c = src.Get(x - shift, y);
                if (c != '.') cv.Force(x, y, c);
            }
        return cv;
    }

    /// <summary>Fungo bioluminescente: 1 a 3 chapéus azuis (#348ca0, brilho #58c4e8) em hastes claras. 10×8.</summary>
    static PixelCanvas Fungus(int v)
    {
        var cv = new PixelCanvas(10, 8);
        int caps = 1 + v;
        for (int i = 0; i < caps; i++)
        {
            int cx = caps == 1 ? 5 : 2 + i * (6 / Math.Max(1, caps - 1));
            int h = 3 + (i + v) % 3;
            for (int y = 0; y < h; y++) cv.Force(cx, 7 - y, 'W');
            int top = 7 - h;
            cv.Force(cx - 1, top, 'F'); cv.Force(cx, top, 'H'); cv.Force(cx + 1, top, 'F');
            cv.Force(cx, top - 1, 'F');
            if (i == 0 || v == 2) { cv.Force(cx - 2, top + 1, 'F'); cv.Force(cx + 2, top + 1, 'F'); }
        }
        return cv;
    }

    /// <summary>Broto da praga 8×10: talo roxo torto e um bulbo verde-ácido (pulsa no jogo).</summary>
    static PixelCanvas Sprout(int v)
    {
        var cv = new PixelCanvas(8, 10);
        int h = 5 + v;
        int x = 3;
        for (int i = 0; i < h; i++)
        {
            cv.Force(x, 9 - i, i < 2 ? 'G' : 'g');
            if (i == 2 && v != 1) x += 1;
            if (i == 4 && v == 2) x -= 1;
        }
        int top = 9 - h;
        cv.Force(x, top, 'h'); cv.Force(x + 1, top, 'h'); cv.Force(x, top - 1, 'h');
        if (v > 0) { cv.Force(x - 1, top + 2, 'g'); cv.Force(x - 2, top + 1, 'h'); }      // folhinha
        return cv;
    }

    static PixelCanvas ConcreteMoss(int v)
    {
        var cv = new PixelCanvas(12, 3);
        for (int x = 0; x < 12; x++)
        {
            float n = N(x, v, 820);
            if (n < 0.25f) continue;
            int h = n > 0.75f ? 3 : n > 0.5f ? 2 : 1;
            for (int i = 0; i < h; i++) cv.Force(x, 2 - i, i == h - 1 ? (N(x, i, 821 + v) > 0.6f ? 'e' : 'c') : 'b');
        }
        return cv;
    }

    static PixelCanvas Debris(int v)
    {
        int w = 4 + v * 2, h = 2 + v % 2;
        var cv = new PixelCanvas(w, h + 1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (y == 0 && (x == 0 || x == w - 1)) continue;
                cv.Force(x, y + 1, y == 0 ? (x < w / 2 ? 'E' : 'D') : y == h - 1 ? 'B' : 'C');
            }
        if (v == 2) cv.Force(w - 2, 0, 'S');                                          // ponta de ferrugem
        return cv;
    }

    /// <summary>Raiz pendurada no teto: 2–3 fios que descem afinando, com pelinhos. Topo no teto. 7 × (10 + 4v).</summary>
    static PixelCanvas HangingRoot(int v, bool blight)
    {
        int h = ForestAutotile.HangingRootHeight(v);
        var cv = new PixelCanvas(7, h);
        int strands = 2 + v % 2;
        for (int s = 0; s < strands; s++)
        {
            float x = 2 + s * 1.5f;
            int len = h - s * 3 - (int)(N(s, v, 830) * 3f);
            for (int y = 0; y < len; y++)
            {
                cv.Force((int)x, y, y < 2 ? 'o' : y < len / 2 ? 'p' : 'o');
                x += (N(s, y, 831 + v) - 0.5f) * 0.9f;
                x = Math.Max(0, Math.Min(6, x));
            }
            if (blight && s == 0) cv.Force((int)x, len - 1, 'g');                     // pontinha roxa
        }
        return cv;
    }

    static PixelCanvas Rebar(int v)
    {
        int h = ForestAutotile.RebarHeight(v);
        var cv = new PixelCanvas(4, h);
        for (int y = 0; y < h; y++) cv.Force(1 + (y > h / 2 && v == 1 ? 1 : 0), y, y % 3 == 0 ? 'R' : 'S');
        if (v == 0) cv.Force(2, h - 1, 'R');
        return cv;
    }

    // =============================================================== fundos (paralaxe)

    static void Backgrounds()
    {
        WriteBg("bg_flo_ceu_noite", Sky(false));
        WriteBg("bg_flo_ceu_praga", Sky(true));
        WriteBg("bg_flo_copas_longe", Canopy(false));
        WriteBg("bg_flo_copas_longe_praga", Canopy(true));
        WriteBg("bg_flo_arvores_medio", MidTrees(false));
        WriteBg("bg_flo_arvores_medio_praga", MidTrees(true));
        WriteBg("bg_flo_troncos_perto", NearTrunks(false));
        WriteBg("bg_flo_troncos_perto_praga", NearTrunks(true));
        WriteBg("bg_flo_nevoa_longe", Mist(false, false));
        WriteBg("bg_flo_nevoa_longe_praga", Mist(true, false));
        WriteBg("bg_flo_nevoa_perto", Mist(false, true));
        WriteBg("bg_flo_nevoa_perto_praga", Mist(true, true));
    }

    /// <summary>
    /// Névoa rasteira animada (o ParallaxLayer a faz derivar): fiapos compridos e uma faixa no pé, em 3 degraus de
    /// transparência com bordas onduladas, SEM pontilhado (pedido do usuário, 14/11/2026: a névoa antiga era ruído cinza).
    /// Emenda sem costura a cada 960 px; some aos poucos embaixo para não marcar uma linha reta.
    /// </summary>
    static PixelCanvas Mist(bool blight, bool near)
    {
        PixelKit.Pal.Clear();
        int r = blight ? 0x7a : 0x9a, g = blight ? 0x5a : 0xac, b = blight ? 0x8c : 0xc8;
        int k = blight ? 0 : near ? 2 : 1;                                        // praga mais fraca
        int[][] alphas = { new[] { 10, 18, 26 }, new[] { 12, 22, 34 }, new[] { 16, 28, 42 } };
        PixelKit.Pal['x'] = Color.FromArgb(alphas[k][0], r, g, b);
        PixelKit.Pal['y'] = Color.FromArgb(alphas[k][1], r, g, b);
        PixelKit.Pal['z'] = Color.FromArgb(alphas[k][2], r, g, b);
        var cv = new PixelCanvas(BgW, BgH);
        int seed = near ? 960 : 950;
        int n = near ? 6 : 9;
        var wisps = new double[n][];
        for (int i = 0; i < n; i++)
        {
            double cx = (i + N(i, 0, seed) * 0.8) * BgW / n;
            double cy = (near ? 250 : 220) + N(i, 1, seed) * (near ? 50 : 60);
            double len = (near ? 150 : 100) + N(i, 2, seed) * (near ? 110 : 90);
            double thick = (near ? 10 : 7) + N(i, 3, seed) * (near ? 8 : 6);
            wisps[i] = new[] { cx, cy, len, thick, N(i, 4, seed) * 6.28 };
        }
        for (int y = 150; y < BgH; y++)
            for (int x = 0; x < BgW; x++)
            {
                double d = 0;
                foreach (var w in wisps)
                {
                    double dx = x - w[0];
                    dx -= BgW * Math.Round(dx / BgW);                                // repete sem costura
                    double bend = 5 * Math.Sin(2 * Math.PI * x / BgW * 4 + w[4]) + 2 * Math.Sin(2 * Math.PI * x / BgW * 11 + w[4]);
                    double u = dx / w[2], v = (y - w[1] - bend) / w[3];
                    double f = 1 - u * u - v * v;
                    if (f > d) d = f;
                }
                // faixa no pé: ondulada em cima, mais densa no meio, sumindo nos últimos 24 px
                double baseY = (near ? 286 : 276) + 6 * Math.Sin(2 * Math.PI * x / BgW * 3 + seed) + 3 * Math.Sin(2 * Math.PI * x / BgW * 13);
                if (y > baseY)
                {
                    double t = Math.Min(1, (y - baseY) / 14.0);
                    double fade = Math.Min(1, (BgH - 1 - y) / 24.0);
                    d = Math.Max(d, 0.8 * t * fade);
                }
                if (d > 0.62) cv.Force(x, y, 'z');
                else if (d > 0.3) cv.Force(x, y, 'y');
                else if (d > 0.06) cv.Force(x, y, 'x');
            }
        return cv;
    }

    static void WriteBg(string name, PixelCanvas cv)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_bg, name + ".png"), name, s_template, false);
    }

    /// <summary>Largura das camadas que repetem (emenda sem costura) e altura com folga para a paralaxe vertical.</summary>
    public const int BgW = 960, BgH = 320, SkyW = 480, SkyH = 270;

    static void BgPalette(bool blight)
    {
        PixelKit.Pal.Clear();
        // céu índigo (GDD §3.1): degradê em 5 faixas pontilhadas
        PixelKit.P('0', "#0c0e20"); PixelKit.P('1', "#10132a"); PixelKit.P('2', "#161a38"); PixelKit.P('3', "#1c2246"); PixelKit.P('4', "#262c58");
        PixelKit.P('M', "#e2e8f0"); PixelKit.P('m', "#b8c4d8"); PixelKit.P('O', "#96a8c8");          // lua e halo
        PixelKit.P('*', "#7a86a8"); PixelKit.P('+', "#c8d0e0");                                       // estrelas
        PixelKit.Pal['h'] = Color.FromArgb(40, 0x96, 0xa8, 0xc8);                                     // halo (anel de dentro)
        PixelKit.Pal['i'] = Color.FromArgb(18, 0x96, 0xa8, 0xc8);                                     // halo (anel de fora)
        PixelKit.Pal['v'] = Color.FromArgb(40, 0x5b, 0x2a, 0x6e);                                     // véu da praga
        PixelKit.Pal['V'] = Color.FromArgb(75, 0x5b, 0x2a, 0x6e);
        // copas distantes, árvores médias, troncos próximos
        if (!blight)
        {
            PixelKit.P('a', "#0e1718"); PixelKit.P('b', "#121e1e"); PixelKit.P('c', "#182826");      // silhuetas
            PixelKit.P('x', "#1f332c"); PixelKit.P('y', "#2a4438"); PixelKit.P('z', "#0a1010");      // copas: corpo aceso, luar, vão
            PixelKit.P('d', "#140f0e"); PixelKit.P('e', "#1a1412"); PixelKit.P('f', "#1e3028"); PixelKit.P('g', "#26392f"); // médias
            PixelKit.P('k', "#0e0a0a"); PixelKit.P('t', "#1c1512"); PixelKit.P('u', "#231a15"); PixelKit.P('w', "#32261e"); // próximas
            PixelKit.P('l', "#1c2a22"); PixelKit.P('n', "#2a3c30");                                   // musgo nos troncos
        }
        else
        {
            PixelKit.P('a', "#100c16"); PixelKit.P('b', "#160f1c"); PixelKit.P('c', "#1e1424");
            PixelKit.P('x', "#261a2e"); PixelKit.P('y', "#34243e"); PixelKit.P('z', "#0a070c");
            PixelKit.P('d', "#0f0a0e"); PixelKit.P('e', "#140d12"); PixelKit.P('f', "#1c1022"); PixelKit.P('g', "#26162c");
            PixelKit.P('k', "#0c080b"); PixelKit.P('t', "#170f14"); PixelKit.P('u', "#1c1319"); PixelKit.P('w', "#281c24");
            PixelKit.P('l', "#26142c"); PixelKit.P('n', "#3a1c46");                                   // veias roxas
        }
        PixelKit.P('S', blight ? "#0a070b" : "#0a0807");                                              // sombra dos troncos no chão
        PixelKit.P('C', "#22262a"); PixelKit.P('D', "#2c3034"); PixelKit.P('A', "#16181a");          // ruína dos Primatas (fundo: escura)
        PixelKit.P('Y', "#c8f0a0");                                                                   // vaga-lume parado
    }

    /// <summary>Céu índigo com a lua (alto à esquerda: a luz da cena vem de lá), halo e estrelas. Praga: véu roxo embaixo.</summary>
    static PixelCanvas Sky(bool blight)
    {
        BgPalette(blight);
        var cv = new PixelCanvas(SkyW, SkyH);
        // faixas lisas (sem pontilhado: o ruído virava "pixelado cinza" no jogo, pedido do usuário 14/11/2026),
        // com a divisa ondulando devagar
        for (int y = 0; y < SkyH; y++)
            for (int x = 0; x < SkyW; x++)
            {
                float t = (y + 4f * (float)Math.Sin(x * 0.021 + y * 0.004)) / SkyH * 4.2f;
                int band = Math.Max(0, Math.Min(4, (int)t));
                cv.Force(x, y, (char)('0' + band));
            }
        int mx = 96, my = 56;
        for (int y = 0; y < SkyH; y++)
            for (int x = 0; x < SkyW; x++)
            {
                double d = Math.Sqrt((x - mx) * (x - mx) + (y - my) * (y - my));
                if (d < 10) cv.Force(x, y, (x - mx) + (y - my) > 6 ? 'm' : 'M');      // lua (sombra embaixo à direita)
                else if (d < 12) cv.Force(x, y, 'O');
                else if (d < 17) cv.Force(x, y, 'h');                                  // halo em dois anéis lisos
                else if (d < 24) cv.Force(x, y, 'i');
            }
        for (int i = 0; i < 70; i++)
        {
            int x = (int)(N(i, 1, 902) * SkyW), y = (int)(N(i, 2, 903) * SkyH * 0.6f);
            if (Math.Abs(x - mx) < 34 && Math.Abs(y - my) < 34) continue;
            cv.Force(x, y, N(i, 3, 904) > 0.8f ? '+' : '*');
        }
        if (blight)
            for (int y = SkyH / 2; y < SkyH; y++)
                for (int x = 0; x < SkyW; x++)
                {
                    // véu em duas faixas lisas subindo de baixo (sem pontilhado)
                    float t = (y - SkyH / 2 + 6f * (float)Math.Sin(x * 0.03)) / (SkyH / 2f);
                    if (t > 0.65f) cv.Force(x, y, 'V');
                    else if (t > 0.3f) cv.Force(x, y, 'v');
                }
        return cv;
    }

    /// <summary>Copa contínua (borda de cima com 3 escalas: copas, galhos, folhas), recortada, cheia até embaixo.</summary>
    static float CanopyEdge(int x, int W, float baseY, int seed)
    {
        double t = 2 * Math.PI * x / W;
        double big = 22 * Math.Sin(t * 3 + seed) + 12 * Math.Sin(t * 7 + seed * 2);
        // copas redondas: bossas de 30–50 px
        double bump = 0;
        int period = 40;
        double local = TerrainAutotile.Mod(x, period) / (double)period;
        bump = -14 * Math.Sqrt(Math.Max(0, 1 - (2 * local - 1) * (2 * local - 1)));
        double fine = (PixelKit.Noise(x / 2, 0, seed) - 0.5) * 4;
        return (float)(baseY + big + bump + fine);
    }

    /// <summary>
    /// Copas distantes como ÁRVORES (pedido do usuário, 14/11/2026: "as árvores no fundo não estão parecendo árvores"):
    /// duas fileiras. A de trás, mais escura, só dá volume. A da frente alterna copas redondas (5–8 tufos, cada um com o
    /// luar no alto à esquerda e a sombra embaixo à direita, então os tufos se separam) e pinheiros em andares (borda
    /// acesa à esquerda, sombra embaixo de cada andar), com o tronco aparecendo entre as copas até o chão.
    /// Repete sem costura a cada 960 px. O pé (y ≥ 200) é cheio, para emendar com o FillBelow.
    /// </summary>
    static PixelCanvas Canopy(bool blight)
    {
        BgPalette(blight);
        var cv = new PixelCanvas(BgW, BgH);
        const int ground = 200;
        for (int x = 0; x < BgW; x++)
            for (int y = ground; y < BgH; y++) cv.Force(x, y, 'b');

        // fileira de trás: copas baixas e escuras, só a borda de cima pega um pouco de luz
        for (int i = 0, x = 0; x < BgW; i++)
        {
            int r = 18 + (int)(N(i, 0, 930) * 12);
            int cy = 128 + (int)(N(i, 1, 931) * 24);
            Blob(cv, x, cy, r, r * 0.85, 'a', 'b', 'a', 'z');
            for (int y = cy; y < ground; y++) for (int dx = -r; dx <= r; dx++) Put(cv, x + dx, y, 'a');
            x += r + 4 + (int)(N(i, 2, 932) * 10);
        }

        // fileira da frente
        int xpos = 6, k = 0;
        while (xpos < BgW - 6)
        {
            bool pine = N(k, 0, 940) > 0.6f;
            int h = 62 + (int)(N(k, 1, 941) * 40);                                     // do chão até a ponta
            int baseY = ground + 6 - (int)(N(k, 2, 942) * 10);
            if (pine) { Pine(cv, xpos, baseY, h + 26, k); xpos += 18 + (int)(N(k, 3, 943) * 10); }
            else { RoundTree(cv, xpos, baseY, h, k, blight); xpos += 26 + (int)(N(k, 3, 943) * 14); }
            k++;
        }
        return cv;
    }

    static void Put(PixelCanvas cv, int x, int y, char c) { cv.Force(TerrainAutotile.Mod(x, BgW), y, c); }

    /// <summary>Tufo de folhas: luar no alto à esquerda, corpo, sombra embaixo à direita e contorno escuro só embaixo.</summary>
    static void Blob(PixelCanvas cv, double cx, double cy, double rx, double ry, char body, char light, char shade, char edge)
    {
        for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; y++)
            for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; x++)
            {
                double nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry;
                double wob = 0.08 * Math.Sin(Math.Atan2(ny, nx) * 7 + cx);              // borda de folhas, não círculo liso
                double d = nx * nx + ny * ny;
                if (d > (1 + wob) * (1 + wob) || y < 0 || y >= cv.H) continue;
                char c = body;
                if (d > 0.72 && (nx + ny) > 0.55) c = edge;                             // contorno embaixo à direita
                else if ((nx + ny) > 0.35 && d > 0.25) c = shade;
                else if (nx + ny < -0.75 && d > 0.3) c = light;                          // luar
                Put(cv, x, y, c);
            }
    }

    /// <summary>Árvore de copa redonda: tronco com galhos em Y e a copa feita de tufos (de baixo para cima, os de cima por cima).</summary>
    static void RoundTree(PixelCanvas cv, int x, int baseY, int h, int k, bool blight)
    {
        int crownR = 20 + (int)(N(k, 5, 944) * 10);
        int crownCy = baseY - h + crownR;
        // tronco
        for (int y = crownCy; y <= baseY; y++)
        {
            int w = y > baseY - 6 ? 5 : 3;
            for (int dx = 0; dx < w; dx++) Put(cv, x - w / 2 + dx, y, dx == 0 ? 'c' : 'z');
        }
        for (int i = 0; i < 8; i++) { Put(cv, x - 1 - i / 2, crownCy + 6 - i, 'z'); Put(cv, x + 1 + i / 2, crownCy + 5 - i, 'z'); }   // galhos em Y
        int n = 5 + (int)(N(k, 6, 945) * 4);
        var tufts = new List<double[]>();
        for (int i = 0; i < n; i++)
        {
            double a = Math.PI * (0.1 + 0.8 * i / (n - 1.0));                         // em leque: dos lados para o alto
            double dist = crownR * (0.45 + N(k, 10 + i, 946) * 0.25);
            double r = crownR * (0.48 + N(k, 20 + i, 947) * 0.2);
            tufts.Add(new[] { x - Math.Cos(a) * dist, crownCy + 4 - Math.Sin(a) * dist * 0.8, r });
        }
        tufts.Add(new[] { (double)x, crownCy + crownR * 0.35, crownR * 0.6 });          // tufo de baixo, no meio
        tufts.Sort((p, q) => q[1].CompareTo(p[1]));                                    // de baixo para cima
        foreach (var t in tufts) Blob(cv, t[0], t[1], t[2], t[2] * 0.82, 'x', 'y', 'b', 'a');
        if (blight && N(k, 7, 948) > 0.5f) Put(cv, x + 2, crownCy, 'c');
    }

    /// <summary>Pinheiro em 5–6 andares que crescem para baixo; cada andar com a borda esquerda acesa e sombra no pé.</summary>
    static void Pine(PixelCanvas cv, int x, int baseY, int h, int k)
    {
        int top = baseY - h;
        for (int y = top + 4; y <= baseY; y++) { Put(cv, x, y, 'z'); Put(cv, x + 1, y, 'z'); }        // tronco
        int tiers = 5 + (int)(N(k, 8, 950) * 2);
        int crownBottom = baseY - 12;
        int step = (crownBottom - top) / tiers;
        for (int t = 0; t < tiers; t++)
        {
            int ty = top + t * step, tierH = step + 5, w = 3 + (t + 1) * 13 / tiers;
            int lean = (int)(N(k, 30 + t, 951) * 3) - 1;
            for (int y = ty; y < ty + tierH; y++)
            {
                double kk = (y - ty) / (double)(tierH - 1);
                int half = (int)Math.Round(1 + kk * w);
                for (int dx = -half; dx <= half; dx++)
                {
                    int yy = y + (Math.Abs(dx) > half - 2 && kk > 0.6 ? 1 : 0);          // pontas caídas
                    char c = dx <= -half + 1 ? 'y' : dx >= half - 1 ? 'a' : 'x';
                    if (kk > 0.82) c = dx < -half / 2 ? 'x' : 'b';                     // sombra embaixo do andar
                    if (Math.Abs(dx) == half && (dx + y) % 2 == 0) continue;           // borda recortada
                    Put(cv, x + dx + lean, yy, c);
                }
            }
        }
        for (int y = top - 3; y < top + 1; y++) Put(cv, x, y, 'x');                     // a ponta
    }

    /// <summary>
    /// Árvores médias: troncos escuros do alto até o chão, galhos, folhagem pendurada em cima e o chão ao luar, com a
    /// sombra de cada tronco saindo do pé para a direita (a lua está no alto à esquerda). Sem fachos soltos: toda sombra
    /// tem uma árvore que a faz (pedido do usuário, 14/11/2026). Transparente onde não há árvore.
    /// </summary>
    static PixelCanvas MidTrees(bool blight)
    {
        BgPalette(blight);
        var cv = new PixelCanvas(BgW, BgH);
        var trunks = new List<int[]>();                                       // {x do pé, largura} de cada tronco
        int xpos = 12;
        int k = 0;
        while (xpos < BgW - 12)
        {
            int w = 8 + (int)(N(k, 0, 923) * 12);
            for (int y = 0; y < BgH; y++)
            {
                int sway = (int)Math.Round(2 * Math.Sin(y * 0.03 + k));
                for (int dx = 0; dx < w; dx++)
                {
                    int x = TerrainAutotile.Mod(xpos + dx + sway, BgW);
                    char c = dx == 0 ? 'f' : dx >= w - 2 ? 'd' : 'e';                 // luar à esquerda
                    if (blight && dx == w / 2 && N(x, y / 4, 924) > 0.8f) c = 'g';
                    cv.Force(x, y, c);
                }
                // raízes abrindo no pé
                if (y > BgH - 30)
                {
                    int flare = (y - (BgH - 30)) / 3;
                    for (int dx = -flare; dx < w + flare; dx++) cv.Force(TerrainAutotile.Mod(xpos + dx + sway, BgW), y, 'd');
                }
            }
            // galhos
            for (int b = 0; b < 3; b++)
            {
                int by = 40 + (int)(N(k, b, 925) * 140);
                int dir = N(k, b, 926) > 0.5f ? 1 : -1;
                int len = 14 + (int)(N(b, k, 927) * 22);
                int startX = dir > 0 ? xpos + w : xpos;
                for (int i = 0; i < len; i++) { int x = TerrainAutotile.Mod(startX + dir * i, BgW); cv.Force(x, by - i / 3, 'e'); cv.Force(x, by - i / 3 + 1, 'd'); }
            }
            trunks.Add(new[] { xpos + (int)Math.Round(2 * Math.Sin((BgH - 1) * 0.03 + k)), w });
            xpos += w + 30 + (int)(N(k, 1, 928) * 60);
            k++;
        }
        // folhagem pendurada no alto (manchas largas e recortadas)
        for (int x = 0; x < BgW; x++)
        {
            int depth = 30 + (int)(26 * Math.Sin(2 * Math.PI * x / BgW * 5 + 1) + 14 * Math.Sin(2 * Math.PI * x / BgW * 13) + (N(x / 2, 0, 929) - 0.5f) * 8);
            for (int y = 0; y < depth; y++) cv.Force(x, y, y >= depth - 2 && N(x, y, 930) > 0.4f ? 'g' : 'f');
        }
        // chão escuro com folhagem no pé; o topo pega o luar
        var groundTop = new int[BgW];
        for (int x = 0; x < BgW; x++)
        {
            int ground = BgH - 22 + (int)(4 * Math.Sin(2 * Math.PI * x / BgW * 9) + (N(x / 3, 1, 931) - 0.5f) * 4);
            groundTop[x] = ground;
            for (int y = ground; y < BgH; y++) cv.Force(x, y, y < ground + 2 ? 'f' : 'd');
        }
        // sombra de cada tronco no chão: a lua está no alto à esquerda, então a sombra sai do pé do tronco para a direita,
        // da largura do tronco no começo, afinando até uma ponta irregular (regra: toda sombra tem quem a faça)
        int ti = 0;
        foreach (var t in trunks)
        {
            int baseX = t[0], w = t[1];
            int len = w * 3 + 18 + (int)(N(ti, 7, 932) * 16);
            for (int i = -2; i < w + len; i++)
            {
                int x = TerrainAutotile.Mod(baseX + i, BgW);
                float u = i < w ? 0f : (i - w) / (float)len;
                int thick = (int)Math.Round(4 * (1 - u) + 1.2 * Math.Sin(i * 0.35 + ti) * u);
                for (int y = groundTop[x]; y < groundTop[x] + thick && y < BgH; y++) cv.Force(x, y, 'S');
            }
            ti++;
        }
        return cv;
    }

    /// <summary>
    /// Troncos próximos: poucos e grossos (28–48 px), casca com veio, luar na borda esquerda, musgo, raízes abrindo no pé;
    /// uma coluna de concreto dos Primatas tomada pela mata. Sem névoa pontilhada (pedido do usuário, 14/11/2026).
    /// </summary>
    static PixelCanvas NearTrunks(bool blight)
    {
        BgPalette(blight);
        var cv = new PixelCanvas(BgW, BgH);
        int[] xs = { 70, 300, 520, 760 };
        for (int k = 0; k < xs.Length; k++)
        {
            int w = 28 + (int)(N(k, 0, 940) * 20);
            int x0 = xs[k] + (int)(N(k, 1, 941) * 50);
            if (k == 2)
            {
                // coluna dos Primatas (reta, 90°) com trepadeira
                for (int y = 30; y < BgH; y++)
                    for (int dx = 0; dx < 26; dx++)
                    {
                        char c = dx < 2 ? 'D' : dx > 22 ? 'A' : (y % 24 == 0 ? 'A' : 'C');
                        if (N(dx, y / 2, 942) > 0.7f && dx > 6 && dx < 20) c = 'l';
                        cv.Force(x0 + dx, y, c);
                    }
                for (int dx = -3; dx < 29; dx++) for (int y = 26; y < 31; y++) cv.Force(x0 + dx, y, y == 26 ? 'D' : 'C'); // capitel quebrado
                continue;
            }
            for (int y = 0; y < BgH; y++)
            {
                int flare = y > BgH - 46 ? (y - (BgH - 46)) * (y - (BgH - 46)) / 40 : 0;
                for (int dx = -flare; dx < w + flare; dx++)
                {
                    int x = TerrainAutotile.Mod(x0 + dx, BgW);
                    int grain = TerrainAutotile.Mod(dx + (int)Math.Round(2 * Math.Sin(y * 0.08 + k)), 6);
                    char c = dx < 2 ? 'w' : dx > w - 4 ? 'k' : grain == 0 ? 't' : 'u';
                    if (N(dx / 2, y / 3, 943 + k) > (blight ? 0.9f : 0.8f) && dx > 3 && dx < w / 2) c = blight ? 'n' : 'l';  // musgo (ou veia da praga)
                    cv.Force(x, y, c);
                }
            }
        }
        // um par de vaga-lumes parados (os de verdade vêm do AmbientEmitter)
        if (!blight) for (int i = 0; i < 5; i++) cv.Force((int)(N(i, 5, 945) * BgW), BgH - 60 - (int)(N(i, 6, 946) * 80), 'Y');
        return cv;
    }

    // =============================================================== prévia

    static void Preview(string path)
    {
        // folha com uma amostra de cada material: um morro de 10 × 6 tiles com vinha, massa e detalhes, ampliado 3×
        const int S = 3, Wt = 34, Ht = 9;
        using (var sheet = new Bitmap(Wt * 16 * S, Ht * 16 * S * 3))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            for (int row = 0; row < 3; row++)
            {
                string mat = ForestAutotile.Materials[row];
                Func<int, int, bool> solid = (x, y) => y <= 2 || (x >= 6 && x <= 12 && y <= 5) || (x >= 20 && x <= 21 && y <= 7) || y <= -1;
                TerrainAutotile.SolidAt sa = (x, y) => solid(x, y);
                for (int x = 0; x < Wt; x++)
                    for (int y = 0; y < Ht; y++)
                    {
                        if (!solid(x, y)) continue;
                        string name = (x >= 20 && x <= 21 && y >= 3) ? ForestAutotile.ClimbTileFor(mat, sa, x, y) : ForestAutotile.TileFor(mat, sa, x, y);
                        var path2 = Path.Combine(s_terrain, name + ".png");
                        if (!File.Exists(path2)) continue;
                        using (var bmp = new Bitmap(path2))
                            g.DrawImage(bmp, x * 16 * S, (row * Ht + (Ht - 1 - y)) * 16 * S, 16 * S, 16 * S);
                    }
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
