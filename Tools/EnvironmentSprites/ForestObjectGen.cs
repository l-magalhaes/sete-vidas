// Floresta Silenciosa, F5: arte das plataformas e dos pontos de pogo (no lugar dos desenhos provisórios da F1).
// Grava POR CIMA dos PNGs de Assets/_Game/Art/Environments/Forest/Placeholder (mesmo nome, mesmo .meta e GUID): toda cena
// que já usa o desenho troca sozinha, inclusive as Forest_Test.
//   galho           48 × 12  galho de mão única (e o que balança): fatiado (borda 12 px) para esticar na largura do galho
//   folha_larga     48 × 12  folha gigante que afunda
//   galho_podre     32 × 12  casca cinza com liquens (e _rachando: rachaduras abertas, lascas caindo)
//   cogumelo        32 × 16  cogumelo saltador: chapéu turquesa (a cor do pogo) com a marca de uma pata de gato
//   cacto_flutuante 24 × 24  cacto-barril arrancado do chão: espinhos pelos lados e por baixo, topo achatado com a flor
//                            turquesa (só o topo aceita pogo) e as raízes soltas embaixo
//   lamina          32 × 12  o pêndulo: tora de espinheiro amarrada no meio pelo cipó (no lugar da lâmina de aço)
//   haste            4 × 16  o cipó torcido que pendura a tora (repete na altura)
// Linha de pisar: as plataformas de mão única (galho, folha) têm o topo em 2 px abaixo do alto do sprite; o galho podre,
// no alto do sprite. A luz vem da lua, no alto à esquerda. C# 5 (compilado pelo PowerShell).
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using SeteVidas.World.Scenery;

public static class ForestObjectGen
{
    static string s_dir, s_template;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        Palette();
        Write("galho", Branch(), 12, 12);
        Write("folha_larga", Leaf(), 0, 0);
        Write("galho_podre", Rotten(false), 0, 0);
        Write("galho_podre_rachando", Rotten(true), 0, 0);
        Write("cogumelo", Mushroom(), 0, 0);
        Write("cacto_flutuante", Cactus(), 0, 0);
        Write("gancho", Hook(), 0, 0);
        Write("cipo_atalho", VineShortcut(), 0, 0);
        Write("cipo_enrolado", VineRolled(), 0, 0);
        Write("bau_fechado", Chest(false), 0, 0);
        Write("bau_aberto", Chest(true), 0, 0);
        Write("atalho_estaca", Stake(false), 0, 0);
        Write("atalho_estaca_solta", Stake(true), 0, 0);
        Write("alcapao_argola", TrapdoorRing(), 0, 0);
        Write("alcapao_aberto", TrapdoorOpen(), 0, 0);
        Write("tampa_raizes", RootLid(), 0, 0);
        Write("lamina", ThornLog(), 0, 0);       // o pêndulo: tora de espinheiro (15/11/2026)
        Write("haste", PendulumVine(), 0, 0);    // e o cipó que a pendura
        Preview(Path.Combine(previewDir, "floresta_objetos_f5.png"));
    }

    /// <summary>Grava o PNG (preserva o .meta) e acerta a borda do fatiado (esquerda/direita, em px).</summary>
    static void Write(string name, PixelCanvas cv, int left, int right)
    {
        string path = Path.Combine(s_dir, name + ".png");
        PixelKit.WriteSprite(cv, path, "flo_obj/" + name, s_template, false);
        string meta = path + ".meta";
        string text = File.ReadAllText(meta);
        string border = "spriteBorder: {x: " + left + ", y: 0, z: " + right + ", w: 0}";
        string fixedText = Regex.Replace(text, @"spriteBorder: \{[^}]*\}", border);
        if (fixedText != text) File.WriteAllText(meta, fixedText);
    }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1210");                                                                   // contorno
        PixelKit.P('K', "#2c1e17"); PixelKit.P('b', "#45301f"); PixelKit.P('B', "#644630");          // casca
        PixelKit.P('L', "#8c6844"); PixelKit.P('l', "#a8845a");                                       // casca ao luar
        PixelKit.P('g', "#24402e"); PixelKit.P('G', "#3c6440"); PixelKit.P('H', "#64905a");          // folhas
        PixelKit.P('q', "#2c2520"); PixelKit.P('r', "#4c4239"); PixelKit.P('R', "#6a5d50");          // madeira podre
        PixelKit.P('S', "#8d8273"); PixelKit.P('s', "#a3ab86");                                       // líquen
        PixelKit.P('f', "#2e5a30"); PixelKit.P('F', "#4f8a44"); PixelKit.P('E', "#7cb862"); PixelKit.P('e', "#a8d888"); // folha larga
        PixelKit.P('T', "#5fd3c7"); PixelKit.P('U', "#b9f5ec"); PixelKit.P('Z', "#8ee4da");          // turquesa do pogo
        PixelKit.P('V', "#2f9a9a"); PixelKit.P('W', "#1c5a62");                                       // aba e lamelas
        PixelKit.P('C', "#e4e2dc"); PixelKit.P('c', "#aaa69a");                                       // pé do cogumelo
        PixelKit.P('k', "#1c4632"); PixelKit.P('j', "#2c6a48"); PixelKit.P('J', "#46905a"); PixelKit.P('I', "#78bc82"); // cacto
        PixelKit.P('X', "#ece4cc"); PixelKit.P('x', "#8c7c5c");                                       // espinhos
        PixelKit.P('1', "#3a4422"); PixelKit.P('2', "#687538"); PixelKit.P('3', "#8c9852"); PixelKit.P('4', "#161a0c"); // hera (a da vinha)
        PixelKit.P('5', "#34383f"); PixelKit.P('6', "#646a75"); PixelKit.P('7', "#a4aab4");          // ferro
        PixelKit.P('8', "#7a5a32"); PixelKit.P('9', "#b8945a");                                       // corda do Caçador
        PixelKit.P('Y', "#e0b04a"); PixelKit.P('y', "#ffe7a0");                                       // dourado da fechadura
    }

    // =============================================================== galho de mão única

    /// <summary>
    /// Galho deitado: casca de 6 px com o topo reto ao luar (a linha de pisar), fendas compridas no veio, um nó, placas de
    /// musgo por cima e uma barriga irregular por baixo, com folhas penduradas. As pontas (12 px de cada lado) afinam por
    /// baixo e soltam um broto; o meio (24 px) repete sem costura.
    /// </summary>
    static PixelCanvas Branch()
    {
        var cv = new PixelCanvas(48, 12);
        for (int x = 0; x < 48; x++)
        {
            int de = Math.Min(x, 47 - x);                         // distância até a ponta
            int p = TerrainAutotile.Mod(x, 24);
            bool mid = de >= 12;
            int top = de == 0 ? 3 : 2;
            int bottom = de == 0 ? 4 : de == 1 ? 5 : de == 2 ? 6 : 7;
            if (mid && p >= 5 && p <= 10) bottom = 8;              // barriga irregular
            for (int y = top; y <= bottom; y++)
            {
                char c;
                if (y == top) c = 'L';
                else if (y == bottom) c = 'K';
                else if (y == bottom - 1) c = 'b';
                else if (y == 3 && ((p >= 10 && p <= 15))) c = 'b';
                else if (y == 4 && ((p >= 1 && p <= 6) || (p >= 17 && p <= 21))) c = 'b';
                else if (y == 5 && p >= 8 && p <= 13) c = 'b';
                else if (y == 3 && (p == 2 || p == 19)) c = 'l';
                else c = 'B';
                cv.Force(x, y, c);
            }
            cv.Force(x, bottom + 1, 'o');
            // nó
            if (de >= 3 && p == 22) { cv.Force(x, 4, 'K'); cv.Force(x, 5, 'K'); cv.Force(x + 1, 4, 'b'); cv.Force(x - 1, 5, 'b'); cv.Force(x, 3, 'l'); }
            // placas de musgo por cima (a linha de pisar continua reta: o musgo é a casca)
            if (de >= 3 && ((p >= 0 && p <= 3) || (p >= 14 && p <= 16))) { cv.Force(x, 2, 'G'); if (p == 1 || p == 15) cv.Force(x, 1, 'H'); else if (p != 3) cv.Force(x, 1, 'G'); }
        }
        // pontas: broto com duas folhas
        Stamp(cv, 0, 0, new[] { "H.G", ".BG" });
        Stamp(cv, 44, 0, new[] { "G.H.", "GB.." });
        // folhas penduradas: na ponta esquerda, na barriga do meio e um raminho na ponta direita
        Stamp(cv, 4, 7, new[] { "gG.", "GHg", ".Gg", ".g." });
        for (int t = 12; t < 36; t += 24) Stamp(cv, t + 7, 9, new[] { "GH", "gG", ".g" });
        Stamp(cv, 40, 8, new[] { "K..", ".b.", ".bG", "..H" });
        return cv;
    }

    // =============================================================== folha larga

    /// <summary>
    /// Folha gigante vista um pouco de cima: ponta fina à esquerda, base redonda e talo à direita; a metade de cima pega
    /// o luar (borda clara = a linha de pisar), a de baixo é mais escura; nervura central e nervuras laterais em espinha de
    /// peixe apontando para a ponta; borda de baixo serrilhada.
    /// </summary>
    static PixelCanvas Leaf()
    {
        var cv = new PixelCanvas(48, 12);
        const double c = 5.3;
        for (int x = 0; x <= 43; x++)
        {
            double s = x / 43.0;
            double w = 3.3 * Math.Sqrt(Math.Max(0, Math.Sin(Math.PI * Math.Pow(s, 0.75))));
            int top = Math.Max(2, (int)Math.Round(c - w));
            int bottom = (int)Math.Round(c + w * 0.8);
            if (x <= 1) { top = 1; bottom = 2; }                   // a ponta vira um pouco para cima
            for (int y = top; y <= bottom; y++)
            {
                char ch;
                int dy = y - 5;
                if (y == top) ch = 'e';
                else if (y == 5 && x > 2 && x < 42) ch = 'e';                                   // nervura central
                else if (dy < 0) ch = TerrainAutotile.Mod(x - dy, 5) == 0 ? 'F' : 'E';           // face de cima
                else ch = y == bottom ? 'f' : TerrainAutotile.Mod(x + dy, 5) == 0 ? 'E' : 'F';  // face de baixo
                cv.Force(x, y, ch);
            }
            cv.Force(x, bottom + 1, TerrainAutotile.Mod(x, 3) == 0 && x > 3 && x < 41 ? 'f' : 'o');
        }
        Stamp(cv, 42, 4, new[] { "eF....", ".FFf..", "...Ff.", "....f." });                  // base e talo
        return cv;
    }

    // =============================================================== galho podre

    /// <summary>
    /// Tronco caído e podre: madeira cinza-amarronzada com fendas compridas no veio, um buraco de nó, líquen esverdeado
    /// claro em cima, um orelha-de-pau pálido embaixo, pontas lascadas e farelo caindo. Rachando: as fendas abrem de cima a
    /// baixo, os pedaços cedem um pixel e cai mais farelo.
    /// </summary>
    static PixelCanvas Rotten(bool cracking)
    {
        var cv = new PixelCanvas(32, 12);
        for (int x = 0; x < 32; x++)
        {
            int top = x == 0 || x == 31 ? 1 : 0;
            int bottom = x == 0 ? 4 : x == 31 ? 5 : x == 1 ? 5 : 6;
            for (int y = top; y <= bottom; y++)
            {
                char c;
                if (y == top) c = TerrainAutotile.Mod(x, 7) == 3 || TerrainAutotile.Mod(x, 7) == 4 ? 's' : 'S';
                else if (y == bottom) c = 'r';
                else if (y == 2 && TerrainAutotile.Mod(x, 11) >= 2 && TerrainAutotile.Mod(x, 11) <= 7) c = 'r';
                else if (y == 4 && TerrainAutotile.Mod(x + 5, 9) <= 4) c = 'r';
                else c = 'R';
                cv.Force(x, y, c);
            }
            cv.Force(x, bottom + 1, 'o');
        }
        // pontas lascadas
        Stamp(cv, 0, 0, new[] { "S", ".", ".", ".", ".", "r" });
        Stamp(cv, 30, 0, new[] { ".S", "RR", "R.", "RR" });
        // buraco de nó e líquen na lateral
        Stamp(cv, 15, 2, new[] { "rqr", "rqq", ".r." });
        Stamp(cv, 5, 3, new[] { "ss", ".s" });
        Stamp(cv, 24, 1, new[] { "s." });
        // orelha-de-pau (pálido, sem brilho) e farelo
        Stamp(cv, 9, 7, new[] { "sSSs", ".SS." });
        Stamp(cv, 21, 8, new[] { "R.", "..", ".r" });
        if (cracking)
        {
            foreach (int cx in new[] { 11, 22 })
                for (int y = 0; y <= 6; y++)
                {
                    int zx = cx + ((y / 2) % 2);
                    cv.Force(zx, y, (y >= 1 && y <= 5) ? '.' : 'q');
                    cv.Force(zx - 1, y, 'q');
                    cv.Force(zx + 1, y, cv.Get(zx + 1, y) == '.' ? '.' : 'r');
                }
            // o pedaço do meio cede um pixel
            for (int x = 13; x <= 20; x++) { for (int y = 7; y >= 1; y--) cv.Force(x, y, cv.Get(x, y - 1)); cv.Force(x, 0, '.'); }
            Stamp(cv, 10, 9, new[] { "R..", "..r", ".S." });
            Stamp(cv, 22, 9, new[] { ".R", "r.", "..", ".S" });
            Stamp(cv, 3, 9, new[] { "S", ".", "r" });
        }
        return cv;
    }

    // =============================================================== cogumelo saltador

    /// <summary>
    /// Chapéu turquesa (a cor do pogo: "pode quicar aqui") com contorno, brilho ao luar em cima à esquerda, aba e lamelas
    /// escuras por baixo; no chapéu, a marca clara de uma pata de gato. Pé creme com anel e musgo na base.
    /// </summary>
    static PixelCanvas Mushroom()
    {
        var cv = new PixelCanvas(32, 16);
        const double cx = 15.5, cy = 9.5, rx = 15.5, ry = 8.6;
        for (int y = 0; y <= 9; y++)
            for (int x = 0; x < 32; x++)
            {
                double dx = (x - cx) / rx, dy = (y - cy) / ry, d = dx * dx + dy * dy;
                if (d > 1.0) continue;
                char c = 'T';
                if (d > 0.80 && y < 8) c = 'o';
                else if (y >= 8) c = y == 9 ? 'W' : 'V';
                else if (dx < -0.35 && dy < -0.35 && d < 0.65) c = 'Z';
                cv.Force(x, y, c);
            }
        // pata de gato: almofada e quatro dedos
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 32; x++)
            {
                double px = (x - 16.5) / 3.0, py = (y - 6.0) / 1.6;
                if (px * px + py * py <= 1.0) cv.Force(x, y, 'U');
            }
        foreach (var t in new[] { new[] { 11, 4 }, new[] { 14, 2 }, new[] { 18, 2 }, new[] { 21, 4 } })   // dedos 2 × 2
        { cv.Force(t[0], t[1], 'U'); cv.Force(t[0] + 1, t[1], 'U'); cv.Force(t[0], t[1] + 1, 'U'); cv.Force(t[0] + 1, t[1] + 1, 'U'); }
        // lamelas por baixo da aba
        for (int x = 5; x <= 26; x++) cv.Force(x, 10, TerrainAutotile.Mod(x, 3) == 0 ? 'V' : 'W');
        for (int x = 2; x <= 29; x++) if (cv.Get(x, 9) == '.') cv.Force(x, 9, 'o');
        // pé com anel
        for (int y = 10; y <= 15; y++)
            for (int x = 11; x <= 20; x++)
            {
                int half = y == 15 ? 6 : y >= 12 && y <= 13 ? 5 : 4;
                if (Math.Abs(x - 15.5) > half) continue;
                bool edge = Math.Abs(x - 15.5) > half - 1;
                char c = edge ? 'o' : x >= 18 ? 'c' : 'C';
                if (y == 13 && !edge) c = 'c';                                                // sombra do anel
                cv.Force(x, y, c);
            }
        for (int x = 9; x <= 22; x++) cv.Force(x, 15, Math.Abs(x - 15.5) > 5 ? 'o' : (x >= 18 ? 'c' : 'C'));
        Stamp(cv, 8, 14, new[] { ".G..", "GHG." });
        Stamp(cv, 21, 14, new[] { "..G.", ".GGH" });
        return cv;
    }

    // =============================================================== cacto flutuante

    /// <summary>
    /// Cacto-bola flutuando com o torrão de terra preso embaixo: gomos curvos (claros no alto à esquerda, ao luar), pontos
    /// de espinho brancos sobre os gomos, tufos de espinhos curtos pelos lados e por baixo (eles ferem) e o topo achatado
    /// com a flor turquesa aberta (o único lugar que aceita pogo).
    /// </summary>
    static PixelCanvas Cactus()
    {
        var cv = new PixelCanvas(24, 24);
        const double cx = 11.5, cy = 13.0, rx = 7.8, ry = 7.0;
        for (int y = 6; y < 22; y++)
            for (int x = 0; x < 24; x++)
            {
                double dx = (x - cx) / rx, dy = (y - cy) / ry, d = dx * dx + dy * dy;
                if (d > 1.0 || y < 7) continue;
                if (d > 0.80) { cv.Force(x, y, 'o'); continue; }
                double u = dx / Math.Sqrt(Math.Max(0.08, 1 - dy * dy));             // posição no gomo (curva com a bola)
                double rib = (u + 1) * 3.0, fr = rib - Math.Floor(rib);
                double light = -dx * 0.7 - dy * 0.5;
                int shade = light > 0.35 ? 3 : light > -0.05 ? 2 : light > -0.45 ? 1 : 0;
                if (fr < 0.25) shade = Math.Max(0, shade - 1);                        // sulco
                char c = "kjJI"[shade];
                if (fr < 0.25 && shade == 0) c = 'o';
                if (fr > 0.45 && fr < 0.75 && TerrainAutotile.Mod(y + (int)Math.Floor(rib) * 2, 4) == 1 && d < 0.55) c = 'X'; // pontos de espinho
                cv.Force(x, y, c);
            }
        // topo achatado
        for (int x = 7; x <= 16; x++) cv.Force(x, 7, x <= 9 ? 'I' : 'J');
        // flor turquesa aberta, deitada no topo
        Stamp(cv, 6, 3, new[] {
            "...T..T...",
            ".T.TZZT.T.",
            "TTZZUUZZTT",
            ".TTZUUZTT.",
            "..JVTTVJ.." });
        // tufos de espinhos curtos: lados e baixo (não no topo)
        int[] angles = { 150, 175, 200, 225, 250, 290, 315, 340, 5, 30 };
        foreach (int a in angles)
        {
            double r = a * Math.PI / 180.0, ux = Math.Cos(r), uy = -Math.Sin(r);
            double ex = cx + ux * (rx + 0.3), ey = cy + uy * (ry + 0.3);
            cv.Force((int)Math.Round(ex), (int)Math.Round(ey), 'x');
            cv.Force((int)Math.Round(ex + ux * 1.4), (int)Math.Round(ey + uy * 1.4), 'X');
            // espinho vizinho, abrindo para o lado
            double r2 = r + 0.45, vx = Math.Cos(r2), vy = -Math.Sin(r2);
            cv.Force((int)Math.Round(ex + vx * 1.3), (int)Math.Round(ey + vy * 1.3), 'X');
        }
        // torrão de terra preso embaixo (veio do chão com ele)
        Stamp(cv, 9, 19, new[] { ".bBBb.", "bBbbKb", ".bKKb.", "..KK.." });
        return cv;
    }

    // =============================================================== gancho, cipó e baú

    /// <summary>
    /// Gancho do Caçador: chapa de ferro cravada (dois rebites), haste e uma argola grossa com o brilho do luar em cima à
    /// esquerda; uma volta da corda do Caçador amarrada na argola diz "a flecha com corda prende aqui".
    /// </summary>
    static PixelCanvas Hook()
    {
        var cv = new PixelCanvas(16, 16);
        Stamp(cv, 4, 0, new[] { "o5555555o", "56766676", "55555555" });          // chapa com rebites
        cv.Force(5, 1, '7'); cv.Force(10, 1, '7');
        Stamp(cv, 7, 3, new[] { "67", "56" });                                    // haste
        for (int y = 4; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                double dx = x - 7.5, dy = y - 10.0, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 5.6 || d < 2.6) continue;
                char c = d > 4.9 || d < 3.1 ? 'o' : (dx + dy < -2 ? '7' : dx + dy > 2 ? '5' : '6');
                cv.Force(x, y, c);
            }
        Stamp(cv, 1, 8, new[] { ".98", "989", "898", ".8." });                     // a corda amarrada
        Stamp(cv, 0, 12, new[] { "8", "9" });                                     // a ponta solta
        return cv;
    }

    /// <summary>
    /// Cipó do atalho (aberto, repete na vertical): dois cipós trançados com o luar na borda esquerda e folhas da hera de
    /// escalar (a mesma linguagem da vinha: dá para subir).
    /// </summary>
    static PixelCanvas VineShortcut()
    {
        var cv = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
        {
            double a = 2 * Math.PI * y / 16.0;
            int xa = (int)Math.Round(7 + 2.5 * Math.Sin(a)), xb = (int)Math.Round(7 - 2.5 * Math.Sin(a));
            bool aFront = Math.Cos(a) > 0;
            foreach (int pass in new[] { 0, 1 })
            {
                int x = (pass == 0) == aFront ? xb : xa;                          // o de trás primeiro
                cv.Force(x - 1, y, 'o'); cv.Force(x, y, 'L'); cv.Force(x + 1, y, 'B'); cv.Force(x + 2, y, 'K'); cv.Force(x + 3, y, 'o');
            }
        }
        Stamp(cv, 1, 2, new[] { "4224", "23324", ".4214" });                       // folha à esquerda
        Stamp(cv, 10, 10, new[] { "4224", "42332", "4124." });                     // folha à direita
        return cv;
    }

    /// <summary>Cipó enrolado (atalho fechado): rolo de cipó amarrado lá no alto, com folhas e a ponta pendurada.</summary>
    static PixelCanvas VineRolled()
    {
        var cv = new PixelCanvas(16, 16);
        for (int y = 0; y < 14; y++)
            for (int x = 0; x < 16; x++)
            {
                double dx = (x - 7.5) / 7.0, dy = (y - 6.5) / 6.0, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 1.0) continue;
                int ring = (int)(d * 6.5);
                char c = d > 0.88 ? 'o' : ring % 2 == 0 ? (dx + dy < -0.3 ? 'L' : 'B') : 'K';
                if (d < 0.18) c = 'K';
                cv.Force(x, y, c);
            }
        Stamp(cv, 9, 12, new[] { ".oBo", "..BK", "..oB", "...o" });                   // a ponta pendurada
        Stamp(cv, 0, 1, new[] { "4224", "23324", ".44" });                          // folhas
        Stamp(cv, 11, 0, new[] { ".422", "42334" });
        return cv;
    }

    /// <summary>
    /// Baú da floresta: madeira com duas cintas de ferro, musgo na tampa e o cadeado dourado (fechadura = dourado, a
    /// linguagem das portas) com a marca de uma pata de gato. Aberto: a tampa para trás e o brilho do que tem dentro.
    /// </summary>
    static PixelCanvas Chest(bool open)
    {
        var cv = new PixelCanvas(16, 16);
        // corpo (linhas 8..15)
        for (int y = 8; y < 16; y++)
            for (int x = 1; x < 15; x++)
            {
                char c = x == 1 || x == 14 || y == 15 ? 'o' : y == 8 ? 'L' : (y == 11 ? 'b' : 'B');
                if ((x == 3 || x == 12) && y > 8 && y < 15) c = y == 9 ? '7' : '6';      // cintas
                cv.Force(x, y, c);
            }
        if (!open)
        {
            // tampa arredondada (linhas 2..7)
            int[] l0 = { 4, 2, 1, 1, 1, 1 }, l1 = { 11, 13, 14, 14, 14, 14 };
            for (int r = 0; r < 6; r++)
                for (int x = l0[r]; x <= l1[r]; x++)
                {
                    int y = 2 + r;
                    char c = x == l0[r] || x == l1[r] || r == 0 ? 'o' : r == 1 ? 'L' : r == 5 ? 'K' : 'B';
                    if ((x == 3 || x == 12) && r > 0 && r < 5) c = r == 1 ? '7' : '6';
                    cv.Force(x, y, c);
                }
            Stamp(cv, 4, 2, new[] { "GHG..", ".G..." });                            // musgo na tampa
            Stamp(cv, 6, 6, new[] { "oYYo", "YyyY", "YoyY", "oYYo" });              // cadeado com a pata
            cv.Force(6, 10, 'Y'); cv.Force(9, 10, 'Y');
        }
        else
        {
            // tampa aberta, para trás (linhas 1..5), e o brilho dourado lá dentro
            for (int y = 1; y <= 5; y++)
                for (int x = 2; x <= 13; x++)
                {
                    char c = x == 2 || x == 13 || y == 1 ? 'o' : y == 5 ? 'K' : 'b';
                    if (x == 3 || x == 12) c = '5';
                    cv.Force(x, y, c);
                }
            for (int x = 2; x <= 13; x++) { cv.Force(x, 6, 'K'); cv.Force(x, 7, 'K'); }
            Stamp(cv, 4, 6, new[] { ".y..Yy..", "YyYYyYYy" });                      // o que brilha dentro
            cv.Force(2, 7, 'o'); cv.Force(13, 7, 'o');
            Stamp(cv, 6, 9, new[] { "oYYo", "oooo" });                              // cadeado aberto, pendurado
        }
        return cv;
    }
    // =============================================================== atalhos (no lugar do clarão: a ação que acontece ali)

    /// <summary>
    /// "Soltar o cipó": estaca cravada na borda, com o cipó enrolado amarrado nela. Fechado: o nó com a laçada à mostra
    /// (puxar a ponta solta o rolo) e a corda curta indo para a borda. Solto: o cipó dá duas voltas na estaca e desce pela
    /// borda (para a direita; o Unity espelha quando o cipó fica à esquerda). 16 × 24, pés na base.
    /// </summary>
    static PixelCanvas Stake(bool released)
    {
        var cv = new PixelCanvas(16, 24);
        // estaca: topo cortado ao luar, veio, base enterrada
        for (int y = 5; y < 24; y++)
            for (int x = 6; x <= 9; x++)
                cv.Force(x, y, x == 6 ? 's' : x == 9 ? 'r' : (y + x) % 5 == 0 ? 'R' : 'S');   // madeira clara, descascada
        Stamp(cv, 6, 4, new[] { "ssSR" });
        Stamp(cv, 5, 3, new[] { ".oooo." });
        for (int y = 5; y < 24; y++) { cv.Force(5, y, 'o'); cv.Force(10, y, 'o'); }
        if (!released)
        {
            Stamp(cv, 4, 12, new[] { "oLBBBBo", "oKBLBKo", "oLBBBBo" });             // voltas do nó (cipó escuro na estaca clara)
            Stamp(cv, 0, 6, new[] { ".ooo..", "oLBKo.", "oB..oo", "oB...B", ".oBBLo" }); // a laçada grande (puxa daqui)
            Stamp(cv, 11, 13, new[] { "BLo", "KBL", ".KBo", "..KB", "...KB" });     // a corda indo para o rolo
            Stamp(cv, 2, 20, new[] { "G.H", "GGG" });                                // folhinhas no pé
        }
        else
        {
            Stamp(cv, 4, 11, new[] { "oLBBBBo", "oKBLBKo", "oLBBBBo", "oKBLBKo" }); // duas voltas apertadas
            for (int y = 12; y < 24; y++)                                            // o cipó desce pela borda
            {
                int x = 11 + Math.Min(3, (y - 12) / 2);
                cv.Force(x, y, 'L'); cv.Force(x + 1, y, 'B'); if (x + 2 < 16) cv.Force(x + 2, y, 'K');
            }
        }
        return cv;
    }

    /// <summary>"Abrir o alçapão": a argola de ferro de puxar, presa por uma chapa na tampa de raízes. 16 × 16, pés na base.</summary>
    static PixelCanvas TrapdoorRing()
    {
        var cv = new PixelCanvas(16, 16);
        for (int y = 3; y < 15; y++)
            for (int x = 0; x < 16; x++)
            {
                double dx = x - 7.5, dy = y - 8.5, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 5.2 || d < 2.8) continue;
                cv.Force(x, y, d > 4.6 || d < 3.3 ? 'o' : (dx + dy < -2 ? '7' : dx + dy > 2 ? '5' : '6'));
            }
        Stamp(cv, 3, 13, new[] { "o55555555o", "56777777665", "o55555555o" });
        return cv;
    }

    /// <summary>O alçapão aberto: a tampa de raízes trançadas em pé, apoiada na dobradiça de ferro. 16 × 40, pés na base.</summary>
    static PixelCanvas TrapdoorOpen()
    {
        var cv = new PixelCanvas(16, 40);
        for (int y = 2; y < 40; y++)
        {
            const int lean = 0;
            for (int x = 4; x <= 11; x++)
            {
                int xx = x - lean;
                char c = x == 4 || x == 11 || y == 2 ? 'o' : Weave(x, y);
                cv.Force(xx, y, c);
            }
        }
        Stamp(cv, 3, 36, new[] { "o5555555o", "56677665", "o5555555o" });           // dobradiça
        Stamp(cv, 3, 14, new[] { ".o6o", "o7.6", ".o5o" });                         // a argola, agora de lado
        return cv;
    }

    /// <summary>Tampa de raízes (repete): raízes trançadas por cima e por baixo, luar nas de cima.</summary>
    static PixelCanvas RootLid()
    {
        var cv = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                cv.Force(x, y, y == 15 ? 'o' : Weave(x, y));
        return cv;
    }

    /// <summary>Trama de raízes 8 × 8: faixas horizontais e verticais alternando por cima e por baixo.</summary>
    static char Weave(int x, int y)
    {
        int cx = TerrainAutotile.Mod(x, 8), cy = TerrainAutotile.Mod(y, 8);
        bool hBand = cy >= 1 && cy <= 3, vBand = cx >= 5 && cx <= 7;
        bool cell = ((x / 8) + (y / 8)) % 2 == 0;
        if (hBand && (!vBand || cell)) return cy == 1 ? 'L' : cy == 3 ? 'b' : 'B';  // raiz deitada por cima
        if (vBand) return cx == 5 ? 'L' : cx == 7 ? 'K' : 'B';                     // raiz em pé
        return 'q';                                                                  // o vão escuro entre elas
    }
    // =============================================================== pêndulo da Floresta (no lugar da lâmina de aço)

    /// <summary>
    /// Tora de espinheiro (pedido do usuário, 15/11/2026: a lâmina de aço não combinava com a Floresta): um pedaço de
    /// tronco deitado, amarrado no meio pelo cipó que o pendura, cheio de espinhos claros para baixo e para os lados (a
    /// mesma cor dos espinhos do cacto). Mesmo tamanho da lâmina (32 × 12) e o mesmo dano/pogo.
    /// </summary>
    static PixelCanvas ThornLog()
    {
        var cv = new PixelCanvas(32, 12);
        // a tora: x 3..28, y 3..8, pontas com o anel do corte
        for (int x = 3; x <= 28; x++)
            for (int y = 3; y <= 8; y++)
            {
                char c = y == 3 ? 'L' : y == 8 ? 'K' : y == 7 ? 'b' : (TerrainAutotile.Mod(x * 3 + y, 7) == 0 ? 'b' : 'B');
                if (x == 3 || x == 28) c = 'o';
                cv.Force(x, y, c);
            }
        for (int y = 4; y <= 7; y++) { cv.Force(4, y, y == 4 || y == 7 ? 'L' : 'l'); cv.Force(27, y, y == 4 || y == 7 ? 'b' : 'L'); }
        cv.Force(4, 5, 'b'); cv.Force(27, 6, 'K');                                          // o miolo dos anéis
        for (int x = 3; x <= 28; x++) cv.Force(x, 2, 'o');
        for (int x = 3; x <= 28; x++) cv.Force(x, 9, 'o');
        // espinhos para baixo (pares, um claro e a base escura), nas pontas para os lados e uns poucos em cima
        for (int x = 5; x <= 26; x += 3) { cv.Force(x, 10, 'x'); cv.Force(x, 11, 'X'); }
        Stamp(cv, 0, 4, new[] { "Xx.", ".Xx", "Xx." });
        Stamp(cv, 29, 4, new[] { ".xX", "xX.", ".xX" });
        foreach (int x in new[] { 6, 10, 21, 25 }) { cv.Force(x, 1, 'x'); cv.Force(x, 0, 'X'); }
        // o cipó amarrado no meio (laço por cima, voltas na tora)
        Stamp(cv, 13, 0, new[] { ".2332.", "21..12", "2....2" });
        for (int y = 3; y <= 8; y++) { cv.Force(14, y, (y & 1) == 0 ? '2' : '3'); cv.Force(17, y, (y & 1) == 0 ? '3' : '2'); }
        cv.Force(15, 9, '1'); cv.Force(16, 10, '2');                                          // a ponta que sobra
        return cv;
    }

    /// <summary>Cipó do pêndulo (4 × 16, repete na altura): dois fios de hera torcidos, com uma folhinha.</summary>
    static PixelCanvas PendulumVine()
    {
        var cv = new PixelCanvas(4, 16);
        for (int y = 0; y < 16; y++)
        {
            int ph = TerrainAutotile.Mod(y, 8);
            bool leftFront = ph < 4;
            cv.Force(0, y, '4'); cv.Force(3, y, '4');
            cv.Force(1, y, leftFront ? '3' : '1');
            cv.Force(2, y, leftFront ? '1' : '2');
        }
        cv.Force(0, 5, '2'); cv.Force(3, 12, '3');                                           // folhinhas
        return cv;
    }

    // =============================================================== util

    static void Stamp(PixelCanvas cv, int x0, int y0, string[] rows)
    {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '.') cv.Force(x0 + x, y0 + y, rows[y][x]);
    }

    /// <summary>Folha de prévia (4×): galho esticado a 3, 4 e 6 tiles (como o Unity fatia), folha, galho podre, cogumelo e cacto.</summary>
    static void Preview(string path)
    {
        const int S = 4;
        using (var sheet = new Bitmap(240 * S, 72 * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            using (var branch = Bmp("galho"))
            {
                int x = 4, y = 4;
                foreach (int tiles in new[] { 3, 4, 6 })
                {
                    int w = tiles * 16;
                    g.DrawImage(branch, new Rectangle(x * S, y * S, 12 * S, 12 * S), new Rectangle(0, 0, 12, 12), GraphicsUnit.Pixel);
                    for (int m = 12; m < w - 12; m += 24)
                    {
                        int cw = Math.Min(24, w - 12 - m);
                        g.DrawImage(branch, new Rectangle((x + m) * S, y * S, cw * S, 12 * S), new Rectangle(12, 0, cw, 12), GraphicsUnit.Pixel);
                    }
                    g.DrawImage(branch, new Rectangle((x + w - 12) * S, y * S, 12 * S, 12 * S), new Rectangle(36, 0, 12, 12), GraphicsUnit.Pixel);
                    x += w + 6;
                }
            }
            int[][] at = { new[] { 4, 24 }, new[] { 58, 24 }, new[] { 4, 44 }, new[] { 40, 44 }, new[] { 80, 44 }, new[] { 110, 44 }, new[] { 132, 44 }, new[] { 152, 44 }, new[] { 172, 44 }, new[] { 194, 44 }, new[] { 132, 28 }, new[] { 160, 30 }, new[] { 172, 14 } };
            string[] names = { "folha_larga", "galho_podre", "galho_podre_rachando", "cogumelo", "cacto_flutuante", "gancho", "cipo_atalho", "cipo_enrolado", "bau_fechado", "bau_aberto", "cipo_atalho", "lamina", "haste" };
            for (int i = 0; i < names.Length; i++)
                using (var b = Bmp(names[i])) g.DrawImage(b, at[i][0] * S, at[i][1] * S, b.Width * S, b.Height * S);
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    static Bitmap Bmp(string name)
    {
        using (var tmp = new Bitmap(Path.Combine(s_dir, name + ".png"))) return new Bitmap(tmp);
    }
}
