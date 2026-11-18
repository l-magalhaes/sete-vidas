// Floresta e Estrada de Soluki: objetos que tomam o lugar das "luzes sem dono" (pedido do usuário, 14/11/2026: toda luz
// tem um objeto que a faz) e a arte que faltava. Grava em Assets/_Game/Art/Environments/Forest/Placeholder (os nomes que
// o ForestTestSetup/ForestGrayboxSetup carregam), preservando .meta e GUID.
//   tocha_poste_0..2   16 × 40  tocha de poste (chama em 3 quadros), pés na base
//   fogueira_0..2      32 × 24  fogueira de acampamento (pedras, lenha cruzada, chama)
//   luz_quente         64 × 64  halo liso (3 degraus, sem pontilhado) que acompanha a tocha, a fogueira e a lanterna
//   marco_trilha       16 × 48  marco da trilha nas saídas: poste, placa-seta e uma lanterna acesa pendurada
//   ponte_pedra_<i>    32 × 40  bloco i da ponte de pedra em arco (parapeito, tabuleiro e o arco embaixo)
//   tronco_rachado_*   16 × 64  tronco que a marreta quebra: fenda com brilho dourado (linguagem do Paladino)
//   posto_abandonado  128 × 112 posto da guarda de Soluki abandonado (R1): telhado quebrado, estandarte rasgado
//   tile_flo_invisivel 16 × 16  (Forest/Terrain) tile transparente: colisão sem desenho (teto da estrada)
// Luz da lua no alto à esquerda. Pedra = a mesma da Fortaleza (as duas fases parecem ligadas). C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using SeteVidas.EditorTools;
using SeteVidas.World.Scenery;

public static class ForestPropGen
{
    static string s_dir, s_terrain, s_template;

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        s_terrain = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Terrain");
        Palette();
        for (int f = 0; f < 3; f++) { Write("tocha_poste_" + f, TorchPost(f)); Write("fogueira_" + f, Campfire(f)); }
        Write("luz_quente", Halo());
        Write("marco_trilha", TrailMarker());
        Write("tronco_rachado_intacto", CrackedTrunk(false), 4, 6);   // agora a porta rachada da cabaninha
        Write("tronco_rachado_lascado", CrackedTrunk(true), 4, 6);
        CabinTiles();
        Write("cabana_janela_velha", OldWindow());
        Write("cabana_fachada", CabinFacade());
        Write("cabana_telhado", CabinRoof());
        Write("porta_casa", HouseDoor(false));
        Write("porta_casa_trancada", HouseDoor(true));
        CabinProps();
        TemplePalette();
        Write("templo_coluna", TempleColumn(28 * 16));
        Write("templo_nicho", TempleNiche());
        BridgeDebris();
        Write("tronco_lascas", Chips());
        Write("posto_abandonado", Outpost());
        int bridges = 0;
        var done = new HashSet<int>();
        foreach (var reg in ForestLayout.Regions())
            foreach (var o in reg.Objects)
                if (o.Kind == "bridge")
                {
                    int n = BridgeBlocks(o.W);
                    if (!done.Add(n)) continue;
                    for (int i = 0; i < n; i++) Write(BridgeName(i, n), BridgeBlock(i, n));
                    bridges++;
                }
        PixelKit.WriteSprite(new PixelCanvas(16, 16), Path.Combine(s_terrain, "tile_flo_invisivel.png"), "tile_flo_invisivel", s_template, false);
        Preview(Path.Combine(previewDir, "floresta_objetos_f5b.png"));
        return "tochas, fogueira, marco, tronco rachado, posto, " + bridges + " ponte(s)";
    }

    /// <summary>Só os blocos das pontes do ForestLayout (a ponte dobrada da R4, 17/11/2026), sem regravar o resto.</summary>
    public static string RunBridges(string projectRoot, string metaTemplatePath)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        s_terrain = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Terrain");
        Palette();
        var done = new HashSet<int>();
        foreach (var reg in ForestLayout.Regions())
            foreach (var o in reg.Objects)
                if (o.Kind == "bridge")
                {
                    int n = BridgeBlocks(o.W);
                    if (!done.Add(n)) continue;
                    for (int i = 0; i < n; i++) Write(BridgeName(i, n), BridgeBlock(i, n));
                }
        return string.Join(", ", done);
    }

    /// <summary>Blocos da ponte (2 tiles cada), como o ForestGrayboxSetup.Bridge monta.</summary>
    public static int BridgeBlocks(float w) { return Math.Max(1, (int)Math.Round(w / 2f)); }
    public static string BridgeName(int i, int n) { return "ponte_pedra_" + n + "_" + i; }

    static void Write(string name, PixelCanvas cv) { Write(name, cv, 0, 0); }

    /// <summary>Grava (preserva o .meta) e acerta a borda do fatiado vertical (embaixo/em cima, px).</summary>
    static void Write(string name, PixelCanvas cv, int bottom, int top)
    {
        string path = Path.Combine(s_dir, name + ".png");
        PixelKit.WriteSprite(cv, path, "flo_prop/" + name, s_template, false);
        string meta = path + ".meta";
        string text = File.ReadAllText(meta);
        string fixedText = Regex.Replace(text, @"spriteBorder: \{[^}]*\}", "spriteBorder: {x: 0, y: " + bottom + ", z: 0, w: " + top + "}");
        if (fixedText != text) File.WriteAllText(meta, fixedText);
    }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1210");
        PixelKit.P('K', "#2c1e17"); PixelKit.P('b', "#45301f"); PixelKit.P('B', "#644630"); PixelKit.P('L', "#8c6844"); PixelKit.P('l', "#a8845a");
        PixelKit.P('s', "#c8b48a"); PixelKit.P('S', "#a08c66");                                       // madeira de dentro (clara)
        PixelKit.P('g', "#24402e"); PixelKit.P('G', "#3c6440"); PixelKit.P('H', "#64905a");          // musgo
        PixelKit.P('q', "#150f0c");                                                                   // escuro (vão)
        PixelKit.P('5', "#34383f"); PixelKit.P('6', "#646a75"); PixelKit.P('7', "#a4aab4");          // ferro
        PixelKit.P('8', "#7a5a32"); PixelKit.P('9', "#b8945a");                                       // corda
        PixelKit.P('O', "#b8442a"); PixelKit.P('A', "#f08a3c"); PixelKit.P('Y', "#f2c96b"); PixelKit.P('y', "#fff0c0"); // fogo
        PixelKit.P('d', "#e0b04a");                                                                   // dourado (rachadura do Paladino)
        // pedra da Fortaleza (as duas fases ligadas)
        PixelKit.P('k', "#2e2a3a"); PixelKit.P('m', "#4e4a60"); PixelKit.P('M', "#6e6884"); PixelKit.P('N', "#8a84a0");
        // Soluki: o vermelho e o dourado da Fortaleza
        PixelKit.P('r', "#6a2228"); PixelKit.P('R', "#9a3434"); PixelKit.P('u', "#d9a441");
        // halo em 5 degraus suaves (o de antes, em 3 anéis fortes, parecia um alvo)
        PixelKit.Pal['h'] = Color.FromArgb(34, 255, 255, 255); PixelKit.Pal['i'] = Color.FromArgb(24, 255, 255, 255);
        PixelKit.Pal['j'] = Color.FromArgb(15, 255, 255, 255); PixelKit.Pal['e'] = Color.FromArgb(8, 255, 255, 255); PixelKit.Pal['f'] = Color.FromArgb(4, 255, 255, 255);
    }

    static void Stamp(PixelCanvas cv, int x0, int y0, string[] rows)
    {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '.') cv.Force(x0 + x, y0 + y, rows[y][x]);
    }

    // =============================================================== fogo

    /// <summary>Chama em gota com base em (cx, baseY), altura h, ponta deslocada por sway: borda vermelha, meio laranja, miolo claro.</summary>
    static void Flame(PixelCanvas cv, double cx, int baseY, int h, double halfW, int sway)
    {
        for (int y = baseY - h; y <= baseY; y++)
        {
            double t = (baseY - y) / (double)h;                  // 0 na base, 1 na ponta
            double w = halfW * Math.Sqrt(Math.Max(0, 1 - t)) * (t < 0.15 ? 0.8 + t : 1);
            double c = cx + sway * t * t;
            for (int x = (int)Math.Floor(c - w); x <= (int)Math.Ceiling(c + w); x++)
            {
                double d = Math.Abs(x + 0.5 - c) / Math.Max(0.6, w);
                if (d > 1) continue;
                char ch = d > 0.72 ? 'O' : (t < 0.55 && d < 0.4) ? (t < 0.3 ? 'y' : 'Y') : 'A';
                cv.Force(x, y, ch);
            }
        }
    }

    static PixelCanvas TorchPost(int f)
    {
        var cv = new PixelCanvas(16, 40);
        int[] hs = { 10, 9, 10 }; int[] sw = { 0, 1, 0 };   // chama calma (pedido: "as tochas estão piscando")
        Flame(cv, 8, 11, hs[f], 3.2, sw[f]);
        Stamp(cv, 4, 11, new[] { "o677765o", "o566665o", ".o5555o.", "..o55o.." });   // cuia de ferro
        for (int y = 15; y < 37; y++)
            for (int x = 6; x <= 9; x++)
                cv.Force(x, y, x == 6 ? 'L' : x == 9 ? 'K' : (y % 7 == 0 ? 'b' : 'B'));
        for (int y = 15; y < 37; y++) { cv.Force(5, y, 'o'); cv.Force(10, y, 'o'); }
        Stamp(cv, 5, 20, new[] { "o9898o", "o8989o" });                                // corda amarrada
        Stamp(cv, 3, 37, new[] { ".oMMMMMo.", "oMNMmMMmo", "ommmmmmmo" });              // pedras do pé
        return cv;
    }

    static PixelCanvas Campfire(int f)
    {
        var cv = new PixelCanvas(32, 24);
        int[] hs = { 13, 12, 13 }; int[] sw = { 0, 1, 0 };
        Flame(cv, 16, 18, hs[f], 5.2, sw[f]);
        Flame(cv, 12, 18, hs[f] - 6, 2.6, -sw[f]);
        Flame(cv, 20, 18, hs[f] - 5, 2.6, sw[f]);
        Stamp(cv, 7, 16, new[] { "oLBo.......oBLo", ".oLBBo...oBBLo.", "...oLBBoBBLo...", "....oKBKBKo...." }); // lenha cruzada
        Stamp(cv, 3, 20, new[] { ".oMMo.oNMo..oMMo.oMNo..oMMo", "oMNmmoMmmmooMNmmoMmmmooMNmo", "ommmmommmmoommmmommmmoommmo" });  // pedras
        cv.Force(15, 19, 'y'); cv.Force(17, 19, 'Y');                                                  // brasa
        return cv;
    }

    /// <summary>Halo liso: três anéis de opacidade (branco; o SpriteRenderer tinge). Sem pontilhado.</summary>
    static PixelCanvas Halo()
    {
        var cv = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                double d = Math.Sqrt((x - 31.5) * (x - 31.5) + (y - 31.5) * (y - 31.5));
                if (d < 7) cv.Force(x, y, 'h'); else if (d < 13) cv.Force(x, y, 'i'); else if (d < 20) cv.Force(x, y, 'j'); else if (d < 26) cv.Force(x, y, 'e'); else if (d < 31) cv.Force(x, y, 'f');
            }
        return cv;
    }

    /// <summary>Marco da trilha (as saídas): poste, braço com a lanterna acesa e a placa em seta para fora (direita; o Unity espelha).</summary>
    static PixelCanvas TrailMarker()
    {
        var cv = new PixelCanvas(16, 48);
        for (int y = 3; y < 48; y++)
            for (int x = 5; x <= 8; x++)
                cv.Force(x, y, x == 5 ? 'L' : x == 8 ? 'K' : (y % 9 == 0 ? 'b' : 'B'));
        for (int y = 3; y < 48; y++) { cv.Force(4, y, 'o'); cv.Force(9, y, 'o'); }
        Stamp(cv, 4, 2, new[] { "oooooooooooo", "oLLLLLLLLLBo", "oKKKKKKKKKKo" });   // braço
        Stamp(cv, 12, 5, new[] { ".8.", ".8.", "o5o", "5y5", "6Y5", "6Y5", "5A5", "o5o" }); // lanterna pendurada
        Stamp(cv, 1, 20, new[] {                                                        // placa em seta
            "oooooooooooo..",
            "oLLLLLLLLLLLo.",
            "oBlBBlBBBlBBLo",
            "oBBBBBBBBBBBBo",
            "obbbbbbbbbbbo.",
            "oooooooooooo.." });
        Stamp(cv, 3, 45, new[] { "G.HG", "GGGG" });
        return cv;
    }

    // =============================================================== porta rachada da cabaninha (marreta)

    /// <summary>
    /// A "madeira rachada" vira a porta de uma cabaninha abandonada (pedido do usuário, 14/11/2026). 16 × 64, repete o meio
    /// (borda: 4 px embaixo, a soleira; 6 em cima, a verga): tábuas em pé com o luar na esquerda, travessas, dobradiças de
    /// ferro e a rachada do Paladino (escura, com brilhos dourados espaçados). Lascada: tábuas quebradas, buracos e a
    /// rachada aberta.
    /// </summary>
    static PixelCanvas CrackedTrunk(bool chipped)
    {
        var cv = new PixelCanvas(16, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 16; x++)
            {
                char c;
                if (x == 0 || x == 15) c = 'o';
                else if (y < 6) c = y == 0 ? 'o' : y == 5 ? 'K' : (y == 1 ? 'l' : 'L');           // verga
                else if (y >= 60) c = y == 63 ? 'o' : y == 60 ? 'L' : 'b';                          // soleira
                else
                {
                    int px = (x - 1) % 5;                                                           // tábuas de 5 px
                    c = px == 0 ? 'K' : px == 1 ? 'L' : (y * 7 + x * 3) % 29 == 0 ? 'b' : 'B';
                    int m = (y - 6) % 18;
                    if (m == 8 || m == 9) c = m == 8 ? 'L' : 'K';                                   // travessa
                    if (m >= 2 && m <= 6 && (x == 2 + (m - 2) * 2 || x == 3 + (m - 2) * 2)) c = 'b';    // diagonal
                }
                cv.Force(x, y, c);
            }
        // dobradiças de ferro (lado esquerdo)
        foreach (int hy in new[] { 13, 49 }) Stamp(cv, 1, hy, new[] { "6665", "5555" });
        // a rachada: em zigue-zague do alto ao pé, escura, com bordas claras e brilho dourado espaçado
        for (int y = 7; y < 59; y++)
        {
            int z = 9 + ((y / 4) % 2 == 0 ? 0 : 1);
            int open = chipped ? 1 : 0;
            for (int x = z - open; x <= z + open; x++) cv.Force(x, y, 'q');
            cv.Force(z - open - 1, y, 'S');
            if (y % (chipped ? 6 : 10) == 3) cv.Force(z, y, chipped ? 'y' : 'd');
        }
        if (chipped)
        {
            Stamp(cv, 3, 22, new[] { "qqq", "Sqq", ".Sq" });                                       // buracos de marretada
            Stamp(cv, 11, 38, new[] { "qqS", "qq.", "S.." });
            Stamp(cv, 12, 9, new[] { "sS", "S." });
        }
        return cv;
    }

    static PixelCanvas Chips()
    {
        var cv = new PixelCanvas(16, 8);
        Stamp(cv, 0, 1, new[] { "..LBBb.....oLBb.", ".LBBb..6....LBo.", "oBBo...5..sS....", "......LBBBb..sS.", "..sS.oBBBBo.....", ".SS...........B." });
        return cv;
    }

    // =============================================================== cabaninha (tiles) e a cabana da Vulpis

    /// <summary>
    /// Cores da cabana de toras (pedido do usuário, 14/11/2026: "mais parecido com essa cabaninha de exemplo"): toras
    /// horizontais marrom-avermelhadas, pontas das toras alaranjadas nos cantos, telhado de telhas cor de ameixa, porta
    /// ameixa-escura, janela acesa e, por dentro, reboco bege com tábuas aparecendo.
    /// </summary>
    static void CabinPalette()
    {
        PixelKit.P('T', "#38191c"); PixelKit.P('U', "#5a2c28"); PixelKit.P('V', "#7a4030"); PixelKit.P('W', "#9c5a3c"); // tora
        PixelKit.P('X', "#c47a46"); PixelKit.P('Z', "#e2a464");                                                   // ponta da tora
        PixelKit.P('P', "#24141e"); PixelKit.P('Q', "#3a2030"); PixelKit.P('C', "#502c44"); PixelKit.P('D', "#6c4060"); // telhado
        PixelKit.P('E', "#33181f"); PixelKit.P('F', "#4a2630"); PixelKit.P('I', "#633a40");                         // porta
        PixelKit.P('p', "#9a7e5e"); PixelKit.P('t', "#b89c74"); PixelKit.P('v', "#cdb38a"); PixelKit.P('x', "#6e5644"); // reboco
        PixelKit.Pal['1'] = Color.FromArgb(150, 0x7a, 0x7a, 0x84); PixelKit.Pal['2'] = Color.FromArgb(110, 0x9a, 0x9a, 0xa4); // fumaça
    }

    static uint H(int x, int y, int s) { return TerrainAutotile.Hash(x, y, s); }

    /// <summary>Uma tora deitada de 8 px (linha <paramref name="r"/> = 0..7 dentro dela): emenda escura, brilho, corpo, sombra.</summary>
    static char Log(int x, int y, int r, int seed)
    {
        if (r == 0) return 'T';
        if (r == 1) return 'W';
        if (r >= 6) return 'U';
        if ((r == 3 || r == 4) && H(x / 3, y, seed) % 11u == 0) return 'U';   // veio da madeira
        return 'V';
    }

    /// <summary>Ponta redonda de tora (anéis) com o canto de cima à esquerda em (x0, y0), 12 × 8.</summary>
    static void LogEnd(PixelCanvas c, int x0, int y0, bool cracked)
    {
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 12; x++)
            {
                double dx = (x + 0.5 - 6) / 6.0, dy = (y + 0.5 - 4) / 4.0, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 1.05) continue;
                char ch = d > 0.82 ? 'T' : d > 0.6 ? 'X' : d > 0.3 ? 'Z' : 'X';
                if (dx < -0.4 && dy < -0.2 && d > 0.6 && d <= 0.82) ch = 'Z';         // luz de cima à esquerda
                c.Force(x0 + x, y0 + y, ch);
            }
        if (cracked) { c.Force(x0 + 6, y0 + 2, 'U'); c.Force(x0 + 7, y0 + 3, 'U'); c.Force(x0 + 8, y0 + 4, 'U'); }
    }

    /// <summary>Tiles da cabaninha (Forest/Terrain): parede = pontas das toras (o canto da cabana), telhado de telhas
    /// ameixa com musgo, fundo de dentro em reboco bege e piso de tábuas sobre pedras.</summary>
    static void CabinTiles()
    {
        CabinPalette();
        for (int v = 0; v < 3; v++)
        {
            // cabana da Vulpis por dentro (DressCabin): toras deitadas nas paredes grossas e o reboco bege
            var logs = new PixelCanvas(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++) logs.Force(x, y, Log(x + v * 16, y, y % 8, 160));
            Tile("tile_flo_cabana_tora_" + v, logs);

            var back = new PixelCanvas(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    uint h = H(x, y, 170 + v) % 100u;
                    back.Force(x, y, h < 6 ? 'p' : h < 9 ? 'v' : 't');
                }
            if (v == 1) Stamp(back, 3, 2, new[] { "x.....", ".x....", ".xx...", "...x..", "...x.x", "....x.", "....x." });
            if (v == 2) Stamp(back, 3, 4, new[] { "..pppp...", ".pTVVTVp.", "pTVVTVVTp", "pTVUTVVTp", "pTVVTVUTp", ".pTVVTVp.", "..ppp.pp." });
            Tile("tile_flo_cabana_reboco_" + v, back);

            // parede de dentro da abandonada: tábuas em pé, escuras e gastas, a mesma de cima a baixo; 2 = uma caiu
            back = Planks(v, 'K', 'b', 'o', 'q');
            if (v == 2)
                for (int y = 0; y < 16; y++)
                    for (int x = 6; x < 11; x++) back.Force(x, y, (y > 12 && x == 6) || (y < 2 && x == 10) ? 'b' : 'q');
            Tile("tile_flo_cabana_fundo_" + v, back);
        }
        for (int v = 0; v < 2; v++)
        {
            // por fora: as tábuas de dentro, um tom mais claro (e sem tábua faltando: é a parede que segura)
            var outer = Planks(v, 'B', 'L', 'o', 'b');
            Tile("tile_flo_cabana_tabua_" + v, outer);
            // telhado: telhas ameixa com musgo em cima, beiral de madeira e, embaixo, as tábuas da parede continuando
            var roof = Planks(v, 'B', 'L', 'o', 'b');
            for (int x = 0; x < 16; x++)
            {
                roof.Force(x, 0, (x + v * 3) % 5 == 0 ? 'H' : (x + v) % 3 == 0 ? 'G' : 'g');
                for (int y = 1; y < 7; y++) roof.Force(x, y, (x + v * 8 + (y > 3 ? 4 : 0)) % 8 == 0 ? 'P' : y == 1 ? 'D' : y == 6 ? 'Q' : 'C');
                roof.Force(x, 7, 'W'); roof.Force(x, 8, 'U'); roof.Force(x, 9, 'o');
            }
            Tile("tile_flo_cabana_telhado_" + v, roof);

            var floor = new PixelCanvas(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char c;
                    if (y == 0) c = 'Z';
                    else if (y < 5) c = (x + v * 7) % 16 == 0 ? 'T' : y == 1 ? 'W' : 'V';      // tábuas do piso
                    else if (y == 5) c = 'T';
                    else c = Stone(x + v * 3, y);                                             // alicerce de pedra
                    floor.Force(x, y, c);
                }
            Tile("tile_flo_cabana_piso_" + v, floor);
        }
    }

    /// <summary>
    /// Janela da cabaninha abandonada (26 × 26, na parede de dentro): moldura de madeira, cruz, vidros com o verde do
    /// luar que passa pela mata, um vidro quebrado (escuro, com caco) e uma tábua pregada torta por cima.
    /// </summary>
    static PixelCanvas OldWindow()
    {
        var c = new PixelCanvas(26, 26);
        for (int y = 0; y < 26; y++)
            for (int x = 0; x < 26; x++)
            {
                char ch;
                if (x == 0 || x == 25 || y == 0 || y == 25) ch = 'o';
                else if (x < 3 || y < 3) ch = 'L';
                else if (x > 22 || y > 22) ch = 'K';
                else if (x == 12 || x == 13 || y == 12 || y == 13) ch = x == 12 || y == 12 ? 'B' : 'K';  // cruz
                else if (x > 13 && y > 13) ch = 'q';                                                  // vidro quebrado
                else ch = (x + y) % 9 == 0 || (x < 6 && y < 6) ? 'G' : 'g';                           // vidro com luar
                c.Force(x, y, ch);
            }
        Stamp(c, 14, 14, new[] { "gG.", "g..", "..." });                                              // caco
        for (int i = 0; i < 30; i++)                                                                  // tábua torta pregada
        {
            int x = -2 + i, y = 18 - i * 2 / 5;
            for (int k = 0; k < 4; k++) c.Force(x, y + k, k == 0 ? 'l' : k == 3 ? 'o' : 'B');
        }
        c.Force(3, 18, '7'); c.Force(21, 11, '7');                                                    // pregos
        return c;
    }

    /// <summary>Tábuas em pé (16 × 16): emendas em <paramref name="seam"/>, a borda da tábua acesa à esquerda e o veio
    /// espaçado. Iguais de cima a baixo (sem faixa que repete); a variação 1 desloca as emendas.</summary>
    static PixelCanvas Planks(int v, char body, char light, char seam, char grain)
    {
        var c = new PixelCanvas(16, 16);
        int[] seams = v == 1 ? new[] { 0, 6, 11 } : new[] { 0, 5, 11 };
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char ch = Array.IndexOf(seams, x) >= 0 ? seam : Array.IndexOf(seams, x - 1) >= 0 ? light : body;
                if (ch == body && H(x, y / 2, 172 + v) % 23u == 0) ch = grain;
                c.Force(x, y, ch);
            }
        return c;
    }

    /// <summary>Telha de madeira cor de ameixa (fileiras de 4 px, desencontradas).</summary>
    static char Shingle(int x, int y)
    {
        int row = TerrainAutotile.Mod(y, 8 * 1000) / 8, r = TerrainAutotile.Mod(y, 8);
        int off = row % 2 == 0 ? 0 : 8;
        if (r == 7) return 'Q';
        if (TerrainAutotile.Mod(x + off, 16) == 0 && r > 1) return 'Q';
        if (r == 0) return H(x / 4, row, 177) % 3u == 0 ? 'D' : 'C';
        return H(x / 2, y / 2, 175) % 23u == 0 ? 'D' : 'C';
    }

    /// <summary>Pedra do alicerce (fileiras de 5 px, a mesma pedra da Fortaleza).</summary>
    static char Stone(int x, int y)
    {
        int row = (y - 6) / 5, r = (y - 6) % 5;
        if (r == 4) return 'k';
        if (TerrainAutotile.Mod(x + (row % 2) * 4, 8) == 0) return 'k';
        return r == 0 ? 'N' : H(x, y, 176) % 9u == 0 ? 'm' : 'M';
    }

    static void cv(PixelCanvas c, int x, int y, char ch) { c.Force(x, y, ch); }

    static void Tile(string name, PixelCanvas c)
    {
        PixelKit.WriteSprite(c, Path.Combine(s_terrain, name + ".png"), name, s_template, false);
    }

    // ---------------------------------------------------------------- a casa da Vulpis por fora

    /// <summary>Largura da fachada (tiles) e quanto a porta fica à direita do meio (tiles): a casa vai de 15 tiles em
    /// volta da porta, como a parede de fundo do layout (D3: x 3..17, porta em 12).</summary>
    public const int FacadeTiles = 15, FacadeTilesHigh = 7;
    public const float FacadeDoorOffset = 1.5f;

    /// <summary>
    /// Fachada da casa da Vulpis (240 × 112, atrás da porta): toras deitadas, pontas das toras nos cantos, o trecho da
    /// porta em tábuas em pé entre duas colunas de pontas, janela com luz quente, lanterna ao lado da porta e o alicerce
    /// de pedra. As 2 fileiras de cima ficam sob o telhado (<see cref="CabinRoof"/>).
    /// </summary>
    static PixelCanvas CabinFacade()
    {
        const int W = FacadeTiles * 16, Hh = FacadeTilesHigh * 16;
        var c = new PixelCanvas(W, Hh);
        int doorCx = W / 2 + (int)(FacadeDoorOffset * 16);                  // 144
        int panel0 = doorCx - 28, panel1 = doorCx + 28;                       // trecho de tábuas em pé
        for (int y = 0; y < Hh - 8; y++)
            for (int x = 0; x < W; x++)
            {
                char ch;
                if (x >= panel0 && x < panel1)
                {
                    int px = TerrainAutotile.Mod(x - panel0, 7);
                    ch = px == 0 ? 'T' : px == 1 ? 'W' : H(x, y / 4, 181) % 13u == 0 ? 'U' : (px == 6 ? 'U' : 'V');
                    if (TerrainAutotile.Mod(y, 40) == 20) ch = 'T';            // travessa
                }
                else ch = Log(x, y, y % 8, 180);
                c.Force(x, y, ch);
            }
        // colunas de pontas de tora: os dois cantos e os dois lados do trecho da porta
        for (int y = 0; y < Hh - 8; y += 8)
        {
            bool odd = (y / 8) % 2 == 1;
            LogEnd(c, odd ? 2 : 0, y, false);
            LogEnd(c, W - 12 - (odd ? 2 : 0), y, (y / 8) % 5 == 3);
            LogEnd(c, panel0 - 8 + (odd ? 1 : 0), y, (y / 8) % 4 == 2);
            LogEnd(c, panel1 - 4 - (odd ? 1 : 0), y, false);
        }
        // alicerce de pedra
        for (int y = Hh - 8; y < Hh; y++)
            for (int x = 0; x < W; x++)
            {
                int r = y - (Hh - 8);
                c.Force(x, y, r == 7 ? 'o' : r == 0 ? 'T' : TerrainAutotile.Mod(x + (r > 3 ? 5 : 0), 10) == 0 || r == 4 ? 'k' : r == 1 || r == 5 ? 'N' : 'M');
            }
        // moldura escura em volta da porta (a porta em si é o sprite porta_casa, 32 × 48)
        for (int y = Hh - 8 - 52; y < Hh - 8; y++)
            for (int x = doorCx - 19; x < doorCx + 19; x++)
            {
                bool frame = x < doorCx - 16 || x >= doorCx + 16 || y < Hh - 8 - 48;
                if (frame) c.Force(x, y, y == Hh - 8 - 52 ? 'W' : 'T');
            }
        // lanterna pendurada à direita da porta
        Stamp(c, doorCx + 21, 52, new[] { "o5o", ".6.", "o6o", "5y5", "6Y5", "6Y5", "5A5", "o5o" });
        // janela acesa (lado esquerdo): moldura, quatro vidros quentes, cruz e peitoril
        int wx = 44, wy = 42;
        for (int y = 0; y < 28; y++)
            for (int x = 0; x < 30; x++)
            {
                char ch;
                if (x == 0 || x == 29 || y == 0 || y == 27) ch = 'T';
                else if (x < 3 || x > 26 || y < 3 || y > 24) ch = x < 3 || y < 3 ? 'W' : 'U';
                else if (x == 14 || x == 15 || y == 13 || y == 14) ch = 'U';
                else ch = (x < 8 && y < 8) || (x > 15 && x < 20 && y < 7) ? 'y' : y > 20 ? 'A' : 'Y';
                c.Force(wx + x, wy + y, ch);
            }
        for (int x = -3; x < 33; x++) { c.Force(wx + x, wy + 28, 'Z'); c.Force(wx + x, wy + 29, 'X'); c.Force(wx + x, wy + 30, 'T'); }
        // musgo subindo do alicerce e uma tábua encostada
        foreach (int mx in new[] { 18, 26, 92, 196, 214 }) Stamp(c, mx, Hh - 11, new[] { ".H.", "GgG" });
        return c;
    }

    /// <summary>
    /// Telhado da casa da Vulpis (272 × 72): telhas cor de ameixa com cumeeira e musgo, beiral de madeira e a sombra
    /// embaixo, e a chaminé de pedra com fumaça. A parte de cima (16 px) cobre a plataforma do layout, que continua sendo
    /// o chão do telhado; os 32 px de baixo cobrem o alto da fachada.
    /// </summary>
    static PixelCanvas CabinRoof()
    {
        const int W = (FacadeTiles + 2) * 16, Hh = 72, top = 24;
        var c = new PixelCanvas(W, Hh);
        // chaminé de pedra (atrás da cumeeira) e a fumaça
        for (int y = 6; y < top + 4; y++)
            for (int x = 40; x < 56; x++)
            {
                char ch = x == 40 || x == 55 ? 'o' : y < 9 ? (y == 6 ? 'o' : 'k') : Stone(x, (y % 10) + 6);
                if (y >= 9 && (x == 41)) ch = 'N';
                c.Force(x, y, ch);
            }
        Stamp(c, 44, 0, new[] { "....1122..", "..1122221.", ".112221...", "..11......" });
        Stamp(c, 50, 0, new[] { "..2", ".22" });
        Stamp(c, 56, 0, new[] { ".122.", "1221.", "11..." });
        for (int y = top; y < Hh; y++)
            for (int x = 0; x < W; x++)
            {
                char ch;
                int r = y - top;
                if (r == 0) ch = 'P';
                else if (r < 4) ch = r == 1 ? 'D' : 'C';                                    // cumeeira
                else if (r == 4) ch = 'P';
                else if (r < 38) ch = Shingle(x, r - 5);
                else if (r < 42) ch = r == 38 ? 'W' : r == 41 ? 'T' : 'V';                  // beiral
                else ch = r < 45 ? 'T' : 'U';                                               // sombra sob o beiral
                if (x < 3 || x >= W - 3) ch = x == 0 || x == W - 1 ? 'P' : r >= 38 && r < 42 ? 'U' : 'Q';  // testeiras
                c.Force(x, y, ch);
            }
        // musgo na cumeeira e escorrendo nas telhas
        for (int x = 2; x < W - 2; x += 7) { c.Force(x, top - 1, H(x, 0, 190) % 2u == 0 ? 'G' : 'H'); if (H(x, 1, 191) % 3u == 0) { c.Force(x + 1, top - 1, 'G'); c.Force(x + 1, top - 2, 'H'); } }
        foreach (int mx in new[] { 30, 110, 178, 236 }) Stamp(c, mx, top + 5, new[] { "gGg", ".g.", ".g." });
        return c;
    }

    /// <summary>Porta da casa (32 × 48, os nomes que o ForestTestSetup carrega): ameixa-escura em tábuas com almofadas,
    /// batente de madeira, maçaneta de ferro; trancada = a fechadura dourada.</summary>
    static PixelCanvas HouseDoor(bool locked)
    {
        var c = new PixelCanvas(32, 48);
        for (int y = 0; y < 48; y++)
            for (int x = 2; x < 30; x++)
            {
                char ch;
                if (y < 4) ch = y == 0 ? 'o' : y == 1 ? 'W' : 'V';                           // verga
                else if (x < 5 || x > 26) ch = x == 2 || x == 29 ? 'o' : x == 3 ? 'W' : 'V'; // batentes
                else if (y >= 46) ch = y == 47 ? 'o' : 'T';                                   // soleira
                else
                {
                    int px = (x - 5) % 6;
                    ch = px == 0 ? 'E' : px == 1 ? 'I' : 'F';                                // tábuas
                    bool panel = (y >= 9 && y <= 21) || (y >= 27 && y <= 41);
                    if (panel && (x == 8 || x == 23) && px != 0) ch = 'E';                  // almofadas
                    if ((y == 9 || y == 27) && x >= 8 && x <= 23) ch = 'E';
                    if ((y == 21 || y == 41) && x >= 8 && x <= 23) ch = 'I';
                }
                c.Force(x, y, ch);
            }
        Stamp(c, 21, 24, new[] { "o6", "67", "o5" });                                         // maçaneta
        if (locked) Stamp(c, 17, 22, new[] { "uu", "do", "uu" });                             // fechadura dourada
        return c;
    }

    /// <summary>Móveis da cabana da Vulpis (caçadora): cama de peles, mesa, arco na parede, peles secando, janela, aljava.</summary>
    static void CabinProps()
    {
        var bed = new PixelCanvas(48, 16);                                                          // cama de peles
        Stamp(bed, 0, 6, new[] {
            "....ooooooooooooooooooooooooooooooooooooooo.....",
            "...oRRRRRRRRRSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSo...",
            "..oRRrrRRRRSSSsSSSSsSSSsSSSSsSSSSsSSSSsSSSSSSo..",
            "..oLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLo.",
            "..oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo.",
            "..oKo..................................oKo......",
            "..oKo..................................oKo......",
            "..ooo..................................ooo......" });
        PropWrite("cabana_cama", bed);
        var table = new PixelCanvas(32, 24);                                                        // mesa com tigela e vela
        Stamp(table, 2, 8, new[] {
            "..........oyo.........oSSo......",
            "..........oYo........oSssSo.....",
            "..........o5o........oSSSSo.....",
            "oooooooooooooooooooooooooooo....",
            "oLLLLLLLLLLLLLLLLLLLLLLLLLLo....",
            "oBBBBBBBBBBBBBBBBBBBBBBBBBBo....",
            ".oKo..................oKo.......",
            ".oKo..................oKo.......",
            ".oKo..................oKo.......",
            ".oKo..................oKo.......",
            ".oKo..................oKo.......",
            ".ooo..................ooo......." });
        PropWrite("cabana_mesa", table);
        var bow = new PixelCanvas(16, 32);                                                          // arco e aljava na parede
        for (int y = 2; y < 30; y++)
        {
            double t = (y - 16) / 14.0;
            int x = 4 + (int)Math.Round(4 * (1 - t * t));
            bow.Force(x, y, 'L'); bow.Force(x + 1, y, 'B');
            bow.Force(4, y, '9');                                                                    // corda
        }
        Stamp(bow, 10, 8, new[] { "o7o", "oLo", "oLo", "oLo", "R8R", "R8R", "R8R", "RRR", "rrr" });     // aljava com flechas
        PropWrite("cabana_arco", bow);
        var pelts = new PixelCanvas(48, 24);                                                        // peles secando no varal
        for (int x = 0; x < 48; x++) pelts.Force(x, 2 + (int)Math.Round(2 * Math.Sin(Math.PI * x / 47.0)), '8');
        foreach (int px in new[] { 6, 20, 34 })
            Stamp(pelts, px, 3, new[] { ".oSSSo.", "oSssSSo", "oSSsSSo", "oSSSSSo", ".oSSSo.", "oS.oSo.", "o...o.." });
        Stamp(pelts, 27, 4, new[] { ".oRRo.", "oRrRRo", "oRRRRo", ".oRRo.", "o..o.." });
        PropWrite("cabana_peles", pelts);
        var window = new PixelCanvas(32, 32);                                                       // janela com a lua
        for (int y = 2; y < 30; y++)
            for (int x = 2; x < 30; x++)
            {
                bool frame = x < 4 || x > 27 || y < 4 || y > 27 || x == 15 || x == 16 || y == 15 || y == 16;
                char c = frame ? ((x < 4 || y < 4) ? 'L' : 'B') : (y < 12 ? 'k' : 'm');
                window.Force(x, y, c);
            }
        Stamp(window, 20, 6, new[] { ".NN", "NNN", ".NN" });
        for (int x = 1; x < 31; x++) { window.Force(x, 1, 'o'); window.Force(x, 30, 'o'); }
        PropWrite("cabana_janela", window);
    }

    static void PropWrite(string name, PixelCanvas c) { Write(name, c); }

    // =============================================================== templo dos Primatas (D6)

    /// <summary>Pedra do templo (a mesma cinza dos tiles pri_templo, um tom abaixo: fica atrás).</summary>
    static void TemplePalette()
    {
        PixelKit.P('a', "#1a1a21"); PixelKit.P('c', "#2e2f38"); PixelKit.P('n', "#43444f"); PixelKit.P('w', "#5a5c68"); PixelKit.P('z', "#747783");
    }

    /// <summary>
    /// Pilastra do templo (24 de largura, <paramref name="h"/> de altura, na parede do fundo): base e capitel de blocos,
    /// fuste com caneluras, luar à esquerda, uma rachadura e musgo subindo da base e escorrendo do capitel.
    /// </summary>
    static PixelCanvas TempleColumn(int h)
    {
        var c = new PixelCanvas(24, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < 24; x++)
            {
                bool cap = y < 14, foot = y >= h - 12;
                char ch;
                if (cap || foot)
                {
                    int r = cap ? y : y - (h - 12);
                    bool band2 = cap ? r >= 8 : r < 4;                                   // degrau mais estreito
                    if (band2 && (x < 2 || x > 21)) continue;
                    ch = x == 0 || x == 23 || (band2 && (x == 2 || x == 21)) ? 'a' : r % 4 == 0 ? 'z' : r % 4 == 3 ? 'c' : x < 4 ? 'w' : 'n';
                }
                else
                {
                    if (x < 3 || x > 20) continue;
                    int fx = (x - 3) % 4;
                    ch = x == 3 || x == 20 ? 'a' : x == 4 ? 'z' : fx == 1 ? 'c' : x < 9 ? 'w' : 'n';  // caneluras
                    if (y % 64 == 40 && x > 3 && x < 20) ch = 'c';                        // junta dos tambores
                }
                c.Force(x, y, ch);
            }
        // rachadura e musgo
        int cx = 13;
        for (int y = h / 3; y < h / 3 + 40; y++) { c.Force(cx, y, 'a'); if (y % 5 == 0) cx += (y / 5) % 2 == 0 ? 1 : -1; }
        for (int x = 1; x < 23; x++)
        {
            int up = 2 + (int)(H(x, 0, 201) % 9u);
            for (int y = h - 1; y > h - 1 - up; y--) if (c.Get(x, y) != '.') c.Force(x, y, y > h - 3 ? 'g' : 'G');
            int down = (int)(H(x, 1, 202) % 7u);
            for (int y = 14; y < 14 + down; y++) if (c.Get(x, y) != '.') c.Force(x, y, 'G');
            if (H(x, 2, 203) % 3u == 0) c.Force(x, 0, 'H');
        }
        return c;
    }

    /// <summary>
    /// Nicho em arco na parede do templo (40 × 56) com a estátua de um primata sentado (cabeça redonda, orelhas, braços
    /// sobre os joelhos, rabo enrolado) no pedestal; musgo na cabeça e nos ombros.
    /// </summary>
    static PixelCanvas TempleNiche()
    {
        var c = new PixelCanvas(40, 56);
        // moldura de blocos e o fundo escuro do nicho em arco
        for (int y = 0; y < 56; y++)
            for (int x = 0; x < 40; x++)
            {
                double dx = x + 0.5 - 20, dy = y + 0.5 - 18;
                bool inArch = y >= 18 ? (x >= 6 && x < 34 && y < 48) : dx * dx + dy * dy < 14 * 14;
                bool outArch = y >= 18 ? (x >= 2 && x < 38) : dx * dx + dy * dy < 18 * 18;
                if (!outArch && y < 48) continue;
                char ch;
                if (y >= 48) ch = y == 48 ? 'z' : y == 55 ? 'a' : x == 0 || x == 39 ? 'a' : 'n';    // pedestal/soleira
                else if (inArch) ch = y < 22 && dx * dx + dy * dy > 12 * 12 ? 'c' : 'a';
                else ch = (dx * dx + dy * dy > 17 * 17 && y < 18) || x == 2 || x == 37 ? 'a' : x < 6 ? 'w' : 'n';
                c.Force(x, y, ch);
            }
        // a estátua do primata (sentado, de frente)
        Stamp(c, 11, 10, new[] {
            "...aaa......aaa...",
            "..anwna....anwna..",
            "..awzwaaaaaawnna..",
            "...aawzzwwnnnaa...",
            "....azwnnnnnna....",
            "....awnaanaana....",
            "....awnnnnnnna....",
            ".....awncccna.....",
            "......annnnna.....",
            "....aawzwnnnnaa...",
            "...awzwnnnnnnnna..",
            "..awzwnnnnnnnnnna.",
            "..awwnnnnnnnnnnna.",
            "..awnacnnnnnacnna.",
            "..awnacnnnnnacnna.",
            "..awnnacnnnacnnna.",
            "..awnnnaaaaannnna.",
            "..awzwnnnnnnnnnna.",
            ".awzwwnnnnnnnnnnna",
            ".awwnnnnnnnnnnnnna",
            ".awnnnnnnnnnnnnnna",
            ".awzwnnaa.aannnnna",
            ".awwnnnna.annnnnna",
            "..aaaaaaa.aaaaaaa.",
        });
        Stamp(c, 29, 28, new[] { ".aa.", "awna", "a.na", "..na", ".ana", "aana" });         // rabo enrolado
        // pedestal da estátua e musgo
        Stamp(c, 9, 34, new[] { "aaaaaaaaaaaaaaaaaaaaaa" });
        for (int x = 0; x < 22; x++) for (int y = 35; y < 48; y++) c.Force(9 + x, y, x == 0 || x == 21 ? 'a' : y == 35 ? 'z' : x < 3 ? 'w' : 'n');
        Stamp(c, 16, 13, new[] { ".GHG", "..g." });                                         // musgo no alto da cabeça
        Stamp(c, 13, 20, new[] { "gG..........Gg" });                                       // e nos ombros
        foreach (int mx in new[] { 4, 12, 27, 33 }) Stamp(c, mx, 46, new[] { ".G.", "GgG" });
        return c;
    }

    // =============================================================== destroços da ponte (no fundo do vale)

    /// <summary>
    /// Pedaços da Ponte do Último Marco caídos no fundo do vale (A1), na pedra da Fortaleza: um trecho de parapeito, um
    /// bloco do tabuleiro inclinado, uma aduela do arco e um monte de cascalho. Caem com o gato na primeira vez.
    /// </summary>
    static void BridgeDebris()
    {
        var a = new PixelCanvas(32, 16);                                                            // trecho de parapeito tombado
        Stamp(a, 0, 6, new[] {
            "..oNNNNNNNNNNNNNNNNNNNNNNNNNo...",
            ".oMMMMMMMMMMMMMMMMMMMMMMMMMMMo..",
            ".okkkkkkkkkkkkkkkkkkkkkkkkkkko..",
            ".omMmo...omMmo...omMmo..omMmo...",
            ".ommmo...ommmo...ommmo..ommmo...",
            ".ommmo...ommmo...ommmo..ommmo...",
            "ooooooooooooooooooooooooooooooo." });
        Write("ponte_destroco_0", a);
        var b = new PixelCanvas(32, 24);                                                            // bloco do tabuleiro, inclinado
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 32; x++)
            {
                double ty = y - (x * 0.25);                                                          // inclinado
                if (ty < 4 || ty > 18) continue;
                char c = ty < 5 ? 'N' : ty < 6 ? 'M' : ((int)ty - 6) % 6 == 5 || (x + ((int)ty / 6) * 8) % 16 == 0 ? 'k' : 'm';
                b.Force(x, y, c);
            }
        Stamp(b, 4, 12, new[] { "G.H" });
        Write("ponte_destroco_1", b);
        var c3 = new PixelCanvas(24, 20);                                                           // aduela do arco
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 24; x++)
            {
                double dx = x - 12, dy = y - 22;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < 12 || d > 20) continue;
                c3.Force(x, y, d > 19 ? 'o' : ((int)(Math.Atan2(dy, dx) * 6)) % 2 == 0 ? 'M' : 'm');
            }
        Write("ponte_destroco_2", c3);
        var d4 = new PixelCanvas(48, 12);                                                           // cascalho
        for (int x = 0; x < 48; x++)
        {
            int h = (int)Math.Round(3 + 4 * Math.Sin(Math.PI * x / 47.0) + ((x * 7) % 5 == 0 ? 1 : 0));
            for (int y = 12 - h; y < 12; y++) d4.Force(x, y, y == 12 - h ? 'N' : ((x + y) % 4 == 0 ? 'k' : (x * 3 + y) % 7 == 0 ? 'M' : 'm'));
        }
        Write("ponte_destroco_3", d4);
    }
    // =============================================================== ponte de pedra

    /// <summary>
    /// Bloco i de n da ponte em arco (32 × 40): parapeito baixo (8 px, atrás do gato), tabuleiro de 16 px (a colisão) com
    /// o calçamento ao luar e duas fiadas de pedra, e o arco embaixo, fundo nas cabeceiras e fino no meio.
    /// </summary>
    static PixelCanvas BridgeBlock(int i, int n)
    {
        var cv = new PixelCanvas(32, 40);
        int span = n * 32;
        for (int x = 0; x < 32; x++)
        {
            int gx = i * 32 + x;
            // parapeito: corrimão de pedra sobre pilaretes a cada 16 px
            bool post = TerrainAutotile.Mod(gx, 16) < 4;
            for (int y = 0; y < 8; y++)
            {
                if (y <= 2) cv.Force(x, y, y == 0 ? 'N' : y == 2 ? 'k' : 'M');
                else if (post) cv.Force(x, y, TerrainAutotile.Mod(gx, 16) == 0 ? 'N' : TerrainAutotile.Mod(gx, 16) == 3 ? 'k' : 'm');
            }
            // tabuleiro
            for (int y = 8; y < 24; y++)
            {
                char c;
                if (y == 8) c = 'N';
                else if (y == 9) c = 'M';
                else
                {
                    int course = (y - 10) / 7, inY = (y - 10) % 7;
                    int off = course % 2 == 0 ? 0 : 8;
                    bool joint = inY == 6 || TerrainAutotile.Mod(gx + off, 16) == 0;
                    c = joint ? 'k' : (inY == 0 ? 'M' : 'm');
                }
                cv.Force(x, y, c);
            }
            // arco: aduelas até a curva (fundo nas cabeceiras, fino no meio)
            double s = Math.Sin(Math.PI * (gx + 0.5) / span);
            int depth = (int)Math.Round(2 + 14 * (1 - s));
            for (int y = 24; y < 24 + depth && y < 40; y++)
            {
                bool voussoir = TerrainAutotile.Mod(gx, 6) == 0;
                cv.Force(x, y, y == 24 + depth - 1 ? 'k' : voussoir ? 'k' : (y == 24 ? 'm' : 'M'));
            }
            if (24 + depth < 40) cv.Force(x, 24 + depth, 'o');
        }
        // musgo de leve no calçamento
        if (i % 3 == 1) Stamp(cv, 5, 7, new[] { "G.HG" });
        return cv;
    }

    // =============================================================== posto abandonado

    /// <summary>
    /// Posto da guarda de Soluki abandonado (8 × 7 tiles; o telhado coincide com a plataforma do layout, topo em y 6):
    /// esteios, parede de tábuas com buracos, telhado com tábuas quebradas e musgo, estandarte vermelho e dourado de
    /// Soluki rasgado (o mesmo da Fortaleza), um suporte de lanças com uma quebrada e tábuas caídas no chão.
    /// </summary>
    static PixelCanvas Outpost()
    {
        var cv = new PixelCanvas(128, 112);
        // parede do fundo (tábuas em pé, escuras), com buracos
        for (int y = 32; y < 106; y++)
            for (int x = 8; x < 120; x++)
            {
                int plank = (x - 8) / 8, px = (x - 8) % 8;
                bool hole = (plank == 3 && y > 60 && y < 84) || (plank == 9 && y > 40 && y < 58) || (plank == 12 && y > 76);
                char c = hole ? 'q' : px == 0 ? 'K' : px == 1 ? 'B' : (y + plank * 7) % 23 == 0 ? 'K' : 'b';
                cv.Force(x, y, c);
            }
        // travessa no meio da parede
        for (int x = 8; x < 120; x++) { cv.Force(x, 66, 'B'); cv.Force(x, 67, 'K'); }
        // esteios
        foreach (int x0 in new[] { 2, 120 })
            for (int y = 26; y < 112; y++)
                for (int x = x0; x < x0 + 6; x++)
                    cv.Force(x, y, x == x0 ? 'o' : x == x0 + 5 ? 'o' : x == x0 + 1 ? 'L' : x == x0 + 4 ? 'K' : 'B');
        // telhado: tábuas deitadas (a plataforma: topo na linha 16), com um buraco e a ponta caída
        for (int y = 16; y < 30; y++)
            for (int x = 0; x < 128; x++)
            {
                bool gap = x >= 70 && x <= 79 && y > 17;
                if (gap) continue;
                char c = y == 16 ? 'l' : y == 17 ? 'L' : y >= 27 ? 'K' : TerrainAutotile.Mod(x, 16) == 0 ? 'K' : (y == 22 ? 'b' : 'B');
                cv.Force(x, y, c);
            }
        Stamp(cv, 69, 17, new[] { "lb", "Lb", ".B", ".bK", "..K" });                     // tábua quebrada pendurada
        Stamp(cv, 80, 17, new[] { "bL", "bL", "K.", "K" });
        for (int x = 4; x < 124; x += 9) { cv.Force(x, 15, 'G'); if (x % 2 == 0) cv.Force(x + 1, 15, 'H'); }  // musgo
        // estandarte de Soluki rasgado na parede
        for (int y = 38; y < 64; y++)
            for (int x = 48; x < 64; x++)
            {
                bool torn = y > 56 && ((x - 48) % 5 == 0 || (y > 60 && (x - 48) % 3 == 1));
                if (torn) continue;
                char c = x == 48 || x == 63 ? 'r' : 'R';
                if (y == 38) c = 'u';
                cv.Force(x, y, c);
            }
        Stamp(cv, 52, 44, new[] { "..u..", ".uuu.", "uuuuu", ".uuu.", "..u.." });     // o sol de Soluki
        Stamp(cv, 46, 37, new[] { "o6666666666666666o" });                             // varão
        // suporte de lanças: uma inteira, uma quebrada
        for (int y = 70; y < 104; y++) { cv.Force(94, y, 'B'); cv.Force(102, y, 'B'); }
        Stamp(cv, 92, 82, new[] { "oLLLLLLLLLLLo" });
        for (int y = 56; y < 104; y++) cv.Force(97, y, y < 60 ? '7' : 'L');
        for (int y = 76; y < 104; y++) cv.Force(100, y, y < 79 ? 'S' : 'L');
        // chão de tábuas e tábuas caídas
        for (int y = 104; y < 112; y++)
            for (int x = 6; x < 122; x++)
                cv.Force(x, y, y == 104 ? 'L' : y == 111 ? 'o' : TerrainAutotile.Mod(x, 12) == 0 ? 'K' : 'B');
        Stamp(cv, 20, 98, new[] { ".....oLLLLo", "...oLBBBBo.", ".oLBBBBo...", "oBBBBo....." });
        return cv;
    }

    // =============================================================== prévia

    static void Preview(string path)
    {
        const int S = 3;
        string[] names = { "tocha_poste_0", "tocha_poste_1", "fogueira_0", "marco_trilha", "tronco_rachado_intacto", "tronco_rachado_lascado", "tronco_lascas", "posto_abandonado" };
        using (var sheet = new Bitmap(520 * S, 200 * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            int x = 4;
            foreach (var n in names)
            {
                string p = Path.Combine(s_dir, n + ".png");
                if (!File.Exists(p)) continue;
                using (var tmp = new Bitmap(p))
                {
                    g.DrawImage(tmp, x * S, (195 - tmp.Height) * S, tmp.Width * S, tmp.Height * S);
                    x += tmp.Width + 6;
                }
            }
            // a ponte inteira (os blocos lado a lado)
            int bx = 4;
            for (int i = 0; ; i++)
            {
                string p = Path.Combine(s_dir, BridgeName(i, 13) + ".png");
                if (!File.Exists(p) || bx > 500) break;
                using (var tmp = new Bitmap(p)) g.DrawImage(tmp, bx * S, 2 * S, tmp.Width * S, tmp.Height * S);
                bx += 32;
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
