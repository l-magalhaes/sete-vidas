// Arquitetura da Cidadela (plano de assets, Etapa 3): uma alvenaria por época, a madeira da arquibancada e a
// terra batida do pátio. Nomes e variações vêm de Scripts/World/Scenery/TerrainAutotile.cs (a regra que o Unity usa).
//  - Fundação de Golden: blocos 32×16 em fiadas alternadas, juntas largas, escuros e lisos.
//  - Soluki: tijolo militar 16×8, juntas retas.
//  - Reforma: tijolos novos e claros com argamassa fresca, misturados a tijolos antigos (os remendos contam a reforma).
//  - Abandono: tijolos de Soluki desbotados, alguns faltando, musgo e rachaduras escuras (nunca douradas).
//  - Madeira: tábuas de 4 px, algumas novas (mais claras), pregos.
// Luz de cima e da direita. Lados expostos com contorno; topo exposto com pedra de cobertura.
// Também: parede do fundo (versão escura, sem bordas), parede escalável por época (ranhuras + hera seca reservadas)
// e as gárgulas que ficam em cima de cada vinha.
// C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.World.Scenery;

public static class ArchitectureSpriteGen
{
    static string s_template;
    static string s_dir;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Citadel/Architecture");
        foreach (var m in TerrainAutotile.Masonry)
        {
            Palette(m, Normal);
            foreach (var name in TerrainAutotile.AllTileNames(m))
                if (!name.Contains("_interno_") && !name.Contains("_meio_")) Write(Tile(m, name), name);
            int count = TerrainAutotile.VariantCount(m, "miolo");
            for (int v = 0; v < count; v++)
                foreach (int side in new[] { 0, TerrainAutotile.Left, TerrainAutotile.Right, TerrainAutotile.Left | TerrainAutotile.Right })
                    Write(Climbable(m, side, v), TerrainAutotile.ClimbTileName(m, side, v));
            if (m != TerrainAutotile.Wood)
            {
                // parede quebrável (gate do Paladino) na pedra da época
                Write(BreakableWall(m, false), TerrainAutotile.BreakableWallName(m, "intacta"));
                Write(BreakableWall(m, true), TerrainAutotile.BreakableWallName(m, "danificada"));
                Write(Rubble(), TerrainAutotile.BreakableWallName(m, "entulho"));
            }
            Palette(m, Back); // parede do fundo: mesmo desenho, roxo escuro
            for (int v = 0; v < count; v++) Write(Body(m, 0, v), TerrainAutotile.BackTileName(m, v));
            Palette(m, Mid); // 3º tile a partir do ar: meio-tom (degrau entre a borda e o interior)
            for (int v = 0; v < count; v++) Write(Body(m, 0, v), TerrainAutotile.TileName(m, "meio", 0, v));
            Palette(m, Inside); // interior do sólido: mais escuro e neutro que o fundo
            for (int v = 0; v < count; v++) Write(Body(m, 0, v), TerrainAutotile.TileName(m, "interno", 0, v));
        }
        Palette(TerrainAutotile.Soluki, Back); // acontecimentos da parede do fundo
        foreach (var ev in TerrainAutotile.WallEvents) Write(WallEvent(ev), TerrainAutotile.WallEventName(ev));
        Write(BackBattlement(), "env_cid_arq_ameias_fundo");
        Palette(TerrainAutotile.PackedEarth, Normal);
        foreach (var name in TerrainAutotile.AllTileNames(TerrainAutotile.PackedEarth)) Write(PackedEarth(name), name);
        GargoylePalette();
        Write(Gargoyle(), "env_sol_arq_gargula_l");
        Write(Gargoyle().Mirror(), "env_sol_arq_gargula_r");
        Write(Gutter(), "env_sol_arq_calha_l");
        Write(Gutter().Mirror(), "env_sol_arq_calha_r");
        var drip = Drip();
        for (int i = 0; i < drip.Count; i++) Write(drip[i], TerrainAutotile.DripFrameName(i));

        // alcovas: o recuo na parede que explica cada sombra jogável de interior (largura × altura da sombra, em tiles)
        Palette(TerrainAutotile.Soluki, Back);
        foreach (var size in new[] { new[] { 10, 5 }, new[] { 8, 5 } })
            Write(Alcove(size[0], size[1]), "env_cid_arq_alcova_" + size[0] + "x" + size[1]);
    }

    static void Write(PixelCanvas cv, string name)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_dir, name + ".png"), name, s_template, false);
    }

    static float N(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    // --------------------------------------------------------------- paletas

    /// <summary>
    /// S corpo, L luz, D sombra, H brilho, j junta, A tijolo alternativo (reforma: antigo; madeira: tábua nova),
    /// k tinta, v musgo, g/G hera seca (reservada à parede escalável).
    /// </summary>
    const int Normal = 0, Back = 1, Inside = 2, Mid = 3;

    static void Palette(string m, int dark)
    {
        PixelKit.Pal.Clear();
        string[] c; // S, L, D, H, j, A
        if (m == TerrainAutotile.Foundation) c = new[] { "#4a455c", "#5e5872", "#3a3549", "#6e6884", "#2a2638", "#444056" };
        else if (m == TerrainAutotile.Soluki) c = new[] { "#7a748e", "#8e88a2", "#625c76", "#a29cb6", "#4a455c", "#6e6882" };
        else if (m == TerrainAutotile.Reform) c = new[] { "#9a94aa", "#b0aac0", "#807a92", "#c4bed2", "#c8c2b8", "#7a748e" };
        else if (m == TerrainAutotile.Ruin) c = new[] { "#5e5a6e", "#6e6a7e", "#46425a", "#7e7a8c", "#33303f", "#545066" };
        else if (m == TerrainAutotile.Wood) c = new[] { "#6b4a2b", "#9a7044", "#4a321e", "#8e8ca4", "#2e2018", "#b08a5a" };
        else c = new[] { "#6a4c3e", "#8a6a52", "#4e3830", "#a08f98", "#33241f", "#574c62" }; // terra batida
        string[] keys = { "S", "L", "D", "H", "j", "A" };
        for (int i = 0; i < keys.Length; i++) Set(keys[i][0], c[i], dark);
        Set('k', "#1c1a24", Normal);
        Set('v', "#36524a", dark);
        Set('g', "#4f5a2e", Normal); Set('G', "#8a9a4e", Normal); // hera seca: nunca escurece (é o sinal do gate)
        Set('m', "#33241f", dark); Set('n', "#4e3830", dark); Set('o', "#6a4c3e", dark); Set('s', "#574c62", dark);
    }

    /// <summary>
    /// Back (parede do fundo): 45% da cor + roxo escuro do interior (#1a1626). Inside (interior do sólido): 38% da cor +
    /// quase preto neutro (#0b0a10): mais escuro e menos roxo que o fundo, para não parecer passagem; as bordas a até
    /// 2 tiles do ar ficam com a cor cheia e desenham as salas.
    /// </summary>
    static void Set(char key, string hex, int mode)
    {
        var col = ColorTranslator.FromHtml(hex);
        if (mode != Normal)
        {
            var bg = ColorTranslator.FromHtml(mode == Back ? "#1a1626" : "#0b0a10");
            float a = mode == Back ? 0.45f : mode == Mid ? 0.66f : 0.38f;
            col = Color.FromArgb((int)(col.R * a + bg.R * (1 - a)), (int)(col.G * a + bg.G * (1 - a)), (int)(col.B * a + bg.B * (1 - a)));
        }
        PixelKit.Pal[key] = col;
    }

    // ---------------------------------------------------------------- tiles

    /// <summary>tile_&lt;material&gt;_&lt;faixa&gt;_&lt;máscara&gt;_&lt;variação&gt;</summary>
    static PixelCanvas Tile(string m, string name)
    {
        string[] p = name.Split('_');
        string band = p[3];
        int mask = Convert.ToInt32(p[4], 16), v = int.Parse(p[5]);
        var cv = Body(m, mask, v);
        Edges(cv, m, mask, band == "topo");
        return cv;
    }

    /// <summary>A alvenaria sem bordas (miolo). mask só influencia o desgaste perto dos lados.</summary>
    static PixelCanvas Body(string m, int mask, int v)
    {
        var cv = new PixelCanvas(16, 16);
        if (m == TerrainAutotile.Foundation) Foundation(cv, v / 2, v % 2);
        else if (m == TerrainAutotile.Wood) Planks(cv, v);
        else Bricks(cv, m, v);
        return cv;
    }

    /// <summary>Blocos 32×16 com juntas largas (2 px). half = 0: metade esquerda do bloco, 1: metade direita.</summary>
    static void Foundation(PixelCanvas cv, int half, int wear)
    {
        int seed = 900 + half * 7 + wear * 31;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int bx = x + half * 16; // 0..31 dentro do bloco
                char c;
                if (y >= 14 || bx >= 30) c = 'j';                       // junta larga
                else if (y == 0 || bx == 29) c = 'L';                   // luz de cima e da direita
                else if (y == 13 || bx == 0) c = 'D';
                else c = N(bx, y, seed) > 0.86f ? 'L' : N(bx, y, seed + 1) < 0.1f ? 'D' : 'S';
                cv.Force(x, y, c);
            }
        if (wear == 1)
        {
            // canto lascado e uma mancha de desgaste (5 séculos). Sem rachadura: rachadura é só da parede quebrável
            // lasca triangular no canto de cima do bloco (esquerdo na metade 0, direito, junto à junta, na metade 1)
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3 - i; j++)
                    cv.Force(half == 0 ? i : 13 - i, j, 'j');
            for (int y = 6; y < 11; y++) for (int x = 6 + half; x < 11 + half; x++) if (N(x, y, seed + 5) > 0.45f) cv.Force(x, y, 'D');
        }
    }

    /// <summary>Tijolo 16×8 em amarração corrida (fiada de cima começa em 0, a de baixo deslocada 8 px).</summary>
    static void Bricks(PixelCanvas cv, string m, int wear)
    {
        int seed = (m == TerrainAutotile.Soluki ? 300 : m == TerrainAutotile.Reform ? 400 : 500) + wear * 17;
        bool reform = m == TerrainAutotile.Reform, ruin = m == TerrainAutotile.Ruin;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int row = y / 8, ly = y % 8;
                int off = row == 0 ? 0 : 8;
                int bx = (x + off) % 16;
                int brick = row * 2 + (x + off) / 16; // tijolo dentro do tile (para tom e falhas)
                float tone = N(brick, wear, seed);
                char body = reform ? (tone < 0.45f ? 'A' : 'S') : tone < 0.25f ? 'A' : 'S';
                bool missing = ruin && wear == 1 && brick == 1;             // variação 1: um tijolo caído (só um)
                char c;
                if (ly == 7 || bx == 15) c = 'j';
                else if (missing) c = ly == 0 || bx == 0 ? 'D' : 'k';    // buraco onde caiu um tijolo
                else if (ly == 0 || bx == 14) c = 'L';
                else if (ly == 6 || bx == 0) c = 'D';
                else c = N(x, y, seed + 2) > 0.9f ? 'L' : N(x, y, seed + 3) < 0.07f ? 'D' : body;
                cv.Force(x, y, c);
            }
        if (ruin)
        {
            // 0 limpa (só um pouco de musgo), 1 tijolo caído, 2 tijolos gastos pela erosão, 3 musgo pesado escorrendo.
            // Sem rachadura: rachadura é só da parede quebrável.
            float mossChance = wear == 3 ? 0.35f : 0.8f;
            for (int x = 0; x < 16; x++)
                if (N(x, 0, seed + 8) > mossChance)
                {
                    cv.Force(x, 7, 'v');
                    if (wear == 3 && N(x, 1, seed + 9) > 0.4f) { cv.Force(x, 6, 'v'); if (N(x, 2, seed + 9) > 0.6f) cv.Force(x, 8, 'v'); }
                }
            if (wear == 2)
            {
                // erosão: cantos dos tijolos arredondados e a face picada (sem linha contínua)
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        int ly = y % 8, bx = (x + (y / 8 == 0 ? 0 : 8)) % 16;
                        bool corner = (ly == 0 || ly == 6) && (bx == 0 || bx == 14);
                        if (corner) cv.Force(x, y, 'j');
                        else if (cv.Get(x, y) == 'S' && N(x, y, seed + 6) > 0.78f) cv.Force(x, y, 'D');
                    }
            }
        }
        if (reform && wear == 1)
            for (int x = 2; x < 9; x++) cv.Force(x, 15, 'H'); // argamassa nova escorrida
    }

    /// <summary>Tábuas de 4 px de altura, emendas desencontradas, pregos; a variação 1 tem uma tábua nova (clara).</summary>
    static void Planks(PixelCanvas cv, int v)
    {
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int row = y / 4, ly = y % 4;
                int joint = (row * 5 + v * 3) % 16;
                bool fresh = v == 1 && row == 2;
                char c;
                if (ly == 3) c = 'j';
                else if (x == joint) c = 'j';
                else if (ly == 0) c = fresh ? 'H' : 'L';
                else c = fresh ? 'A' : N(x, y, 700 + v) > 0.85f ? 'D' : 'S';
                cv.Force(x, y, c);
                if (ly == 1 && (x == (joint + 2) % 16 || x == (joint + 14) % 16)) cv.Force(x, y, 'H'); // pregos
            }
        if (v == 1) for (int x = 0; x < 16; x++) if (cv.Get(x, 8) == 'H' && x % 3 != 0) cv.Force(x, 8, 'A');
    }

    /// <summary>Lados expostos: contorno; topo com pedra de cobertura (brilho + sombra), direita com luz, cantos chanfrados.</summary>
    static void Edges(PixelCanvas cv, string m, int mask, bool top)
    {
        bool up = (mask & TerrainAutotile.Up) != 0, right = (mask & TerrainAutotile.Right) != 0;
        bool down = (mask & TerrainAutotile.Down) != 0, left = (mask & TerrainAutotile.Left) != 0;
        if (up)
            for (int x = 0; x < 16; x++)
            {
                cv.Force(x, 0, 'H'); cv.Force(x, 1, 'L'); cv.Force(x, 2, m == TerrainAutotile.Wood ? 'S' : 'L');
                cv.Force(x, 3, 'D');
            }
        for (int i = 0; i < 16; i++)
        {
            if (left) { cv.Force(0, i, 'k'); if (i > 3 || !up) cv.Force(1, i, 'D'); }
            if (right) { cv.Force(15, i, 'k'); if (i > 3 || !up) cv.Force(14, i, 'L'); }
            if (down) { cv.Force(i, 15, 'k'); cv.Force(i, 14, 'D'); }
        }
        if (up && left) { cv.Force(0, 0, '.'); cv.Force(1, 0, 'k'); cv.Force(0, 1, 'k'); }
        if (up && right) { cv.Force(15, 0, '.'); cv.Force(14, 0, 'k'); cv.Force(15, 1, 'k'); }
        if (down && left) { cv.Force(0, 15, '.'); }
        if (down && right) { cv.Force(15, 15, '.'); }
    }

    /// <summary>Parede escalável: a alvenaria da época + ranhuras de apoio + dois ramos de hera seca (linguagem reservada).</summary>
    static PixelCanvas Climbable(string m, int side, int v)
    {
        var cv = Body(m, side, v);
        Edges(cv, m, side, false);
        // ranhuras: onde as patas apoiam (sombra + brilho)
        for (int x = 4; x <= 8; x++) { cv.Force(x, 4, 'j'); cv.Force(x, 5, 'H'); }
        for (int x = 8; x <= 12; x++) { cv.Force(x, 11, 'j'); cv.Force(x, 12, 'H'); }
        // hera seca: ramos que ondulam de cima a baixo, com folhas alternadas
        for (int y = 0; y < 16; y++)
        {
            int wobble = (y / 3) % 2;
            int l = 2 + wobble, r = 13 - wobble;
            cv.Force(l, y, 'g'); cv.Force(r, y, 'g');
            if (y % 4 == 1) cv.Force(l + 1, y, 'G');
            if (y % 4 == 3) cv.Force(r - 1, y, 'G');
        }
        return cv;
    }

    /// <summary>Terra batida do pátio: topo compactado e claro, pedrinhas, sem grama; embaixo, terra.</summary>
    static PixelCanvas PackedEarth(string name)
    {
        string[] p = name.Split('_');
        int mask = Convert.ToInt32(p[5], 16), v = int.Parse(p[6]);
        var cv = new PixelCanvas(16, 16);
        int seed = 1200 + v * 13;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char c;
                if (y == 0) c = N(x, 0, seed) > 0.7f ? 'H' : 'L';
                else if (y < 3) c = N(x, y, seed) > 0.8f ? 's' : 'L';
                else if (y < 5) c = N(x, y, seed + 1) > 0.5f ? 'o' : 'S';
                else c = N(x, y, seed + 2) < 0.15f ? 'm' : N(x, y, seed + 3) > 0.9f ? 'o' : 'n';
                cv.Force(x, y, c);
            }
        bool right = (mask & TerrainAutotile.Right) != 0, left = (mask & TerrainAutotile.Left) != 0, down = (mask & TerrainAutotile.Down) != 0;
        for (int i = 0; i < 16; i++)
        {
            if (left) cv.Force(0, i, 'k');
            if (right) { cv.Force(15, i, 'k'); if (i > 2) cv.Force(14, i, 'o'); }
            if (down) cv.Force(i, 15, 'k');
        }
        if (left) { cv.Force(0, 0, '.'); cv.Force(1, 0, 'k'); }
        if (right) { cv.Force(15, 0, '.'); cv.Force(14, 0, 'k'); }
        return cv;
    }

    // ------------------------------------------------ acontecimentos de parede

    /// <summary>
    /// Acontecimentos 16×24 na parede do fundo (paleta escura de Soluki): janela tapada com tijolo de outra época,
    /// nicho escuro em arco, remendo de pedra mais clara, mancha de água escorrendo, rachadura escura. Nada dourado
    /// (rachadura dourada = parede quebrável) e nada com cara de passagem.
    /// </summary>
    static PixelCanvas WallEvent(string ev)
    {
        var cv = new PixelCanvas(16, 24);
        Func<int, int, bool> arch = (x, y) =>
        {
            // arco: retângulo 10×16 (x 3..12, y 6..21) com topo redondo (y 1..6)
            if (x < 3 || x > 12 || y > 21) return false;
            if (y >= 6) return true;
            float dx = (x + 0.5f - 8f) / 5f, dy = (y + 0.5f - 6.5f) / 5.5f;
            return dx * dx + dy * dy <= 1f;
        };
        if (ev == "janela_tapada" || ev == "nicho")
        {
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 16; x++)
                {
                    if (!arch(x, y)) continue;
                    bool rim = !arch(x - 1, y) || !arch(x + 1, y) || !arch(x, y - 1) || !arch(x, y + 1);
                    char c;
                    if (rim) c = 'H';                                                   // moldura de pedra clara
                    else if (ev == "nicho") c = y > 18 ? 'D' : 'k';                     // fundo escuro, base com luz
                    else if (y < 12)
                        c = (x == 8 || y == 7) ? 'D' : 'k';                             // metade de cima: ainda aberta, com grade
                    else
                        c = ((y - 12) % 3 == 2 || (x + ((y - 12) / 3 % 2) * 2) % 4 == 3) ? 'j' : 'A'; // metade de baixo: tapada com tijolinhos de outra cor
                    cv.Force(x, y, c);
                }
            cv.Force(8, 1, 'L'); cv.Force(7, 1, 'L'); cv.Force(8, 0, 'H');               // pedra-chave no topo do arco
            for (int x = 2; x <= 13; x++) { cv.Force(x, 22, 'H'); cv.Force(x, 23, 'D'); } // peitoril saliente
            if (ev == "nicho") { cv.Force(7, 20, 'A'); cv.Force(8, 20, 'A'); cv.Force(7, 19, 'L'); cv.Force(8, 19, 'A'); } // pote de barro
        }
        else if (ev == "remendo")
        {
            // tijolos mais claros encaixados de qualquer jeito (uma reforma antiga)
            for (int y = 6; y < 20; y++)
                for (int x = 1; x < 15; x++)
                {
                    if ((x == 1 || x == 14) && (y < 8 || y > 17)) continue;
                    int row = (y - 6) / 4, off = row % 2 == 0 ? 0 : 3;
                    char c = (y - 6) % 4 == 3 || (x + off) % 7 == 6 ? 'j' : 'L';
                    if (N(x, y, 77) > 0.85f) c = 'H';
                    cv.Force(x, y, c);
                }
        }
        else // mancha: água escorrendo de uma junta, faixa escura que afina para baixo
        {
            for (int y = 0; y < 24; y++)
            {
                int w = Math.Max(1, 5 - y / 5);
                int cx = 8 + (int)Math.Round(Math.Sin(y * 0.4) * 1.2);
                for (int x = cx - w / 2; x <= cx + w / 2; x++) if (N(x, y, 88) > 0.15f) cv.Force(x, y, y % 5 == 0 ? 'k' : 'D');
            }
        }
        return cv;
    }

    // --------------------------------------------------------- parede quebrável

    /// <summary>
    /// Parede quebrável 16×64 (o gate do Paladino) na pedra da época: uma porta antiga fechada com alvenaria que está
    /// cedendo. Sem brilho dourado: o sinal é a rede GRANDE de rachaduras escuras de cima a baixo (com o lado de cá
    /// iluminado), tijolos saltados e entulho no pé. As outras paredes não têm rachaduras, só lascas e erosão.
    /// damaged = depois do 1º golpe: mais rachaduras, um buraco e tijolos caídos.
    /// </summary>
    static PixelCanvas BreakableWall(string m, bool damaged)
    {
        var cv = new PixelCanvas(16, 64);
        int count = TerrainAutotile.VariantCount(m, "miolo");
        for (int b = 0; b < 4; b++)
        {
            var block = Body(m, 0, (b * 3 + 1) % count);
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) cv.Force(x, b * 16 + y, block.Px[x, y]);
        }
        for (int y = 0; y < 64; y++) { cv.Force(0, y, 'k'); cv.Force(15, y, 'k'); cv.Force(1, y, 'D'); } // é um enchimento: contorno próprio

        int seed = damaged ? 1700 : 1600;
        Action<int, int, int, int> crack = (sx, sy, len, drift) =>
        {
            int x = sx;
            for (int i = 0; i < len; i++)
            {
                int y = sy + i;
                if (y < 1 || y > 62) break;
                cv.Force(x, y, 'k');
                if (x + 1 < 15 && cv.Get(x + 1, y) != 'k') cv.Force(x + 1, y, 'L');   // borda iluminada (luz da direita)
                if (x - 1 > 1 && cv.Get(x - 1, y) != 'k') cv.Force(x - 1, y, 'D');
                float n = N(x, y, seed + sx);
                if (n > 0.62f - drift * 0.1f) x = Math.Min(12, x + 1); else if (n < 0.3f + drift * 0.1f) x = Math.Max(3, x - 1);
            }
        };
        crack(8, 2, 60, 0);                                        // a rachadura principal, de cima a baixo
        crack(8, 14, 9, -2); crack(7, 30, 8, 2); crack(9, 44, 10, -2); // galhos
        if (damaged)
        {
            crack(4, 6, 20, 1); crack(11, 36, 22, -1);
            for (int y = 24; y < 30; y++) for (int x = 5; x < 10; x++) cv.Force(x, y, y == 24 || x == 5 ? 'D' : 'k'); // buraco
        }
        // tijolos saltados: um bloco com a borda de cima clara, deslocado 1 px
        for (int x = 3; x < 7; x++) { cv.Force(x, 19, 'H'); cv.Force(x, 20, 'L'); }
        for (int x = 10; x < 13; x++) { cv.Force(x, 51, 'H'); }
        // entulho no pé
        int[] pebbles = damaged ? new[] { 2, 4, 6, 9, 11, 13 } : new[] { 3, 9, 12 };
        foreach (int px in pebbles) { cv.Force(px, 62, 'L'); cv.Force(px, 63, 'D'); cv.Force(px + 1, 63, 'D'); }
        return cv;
    }

    /// <summary>Entulho 20×6 da parede quebrada, na pedra da época.</summary>
    static PixelCanvas Rubble()
    {
        var cv = new PixelCanvas(20, 6);
        int[,] chunks = { { 0, 2, 6, 5 }, { 5, 0, 12, 5 }, { 12, 3, 19, 5 }, { 9, 1, 13, 3 } };
        for (int i = 0; i < chunks.GetLength(0); i++)
            for (int x = chunks[i, 0]; x <= chunks[i, 2]; x++)
                for (int y = chunks[i, 1]; y <= chunks[i, 3]; y++)
                {
                    bool edge = x == chunks[i, 0] || x == chunks[i, 2] || y == chunks[i, 3];
                    cv.Force(x, y, y == chunks[i, 1] ? 'L' : edge ? 'D' : 'S');
                }
        return cv;
    }

    /// <summary>Ameias para o alto da parede do fundo (16×8, paleta do fundo): o topo da torre de escalada.</summary>
    static PixelCanvas BackBattlement()
    {
        var cv = new PixelCanvas(16, 8);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 8; y++)
            {
                bool merlon = x % 8 < 5 || y >= 4; // dentes de 5 px com vão de 3
                if (!merlon) continue;
                cv.Force(x, y, y == 0 || (y == 4 && x % 8 >= 5) ? 'L' : x % 8 == 4 && y < 4 ? 'D' : 'S');
            }
        return cv;
    }

    // --------------------------------------------------------------- gárgula

    static void GargoylePalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('S', "#6e6882"); PixelKit.P('L', "#8e88a2"); PixelKit.P('D', "#4a455c"); PixelKit.P('k', "#1c1a24");
        PixelKit.P('j', "#2a2638"); PixelKit.P('w', "#7fd0f0"); PixelKit.P('W', "#c8f4ff");
        PixelKit.P('T', "#9a7044"); PixelKit.P('t', "#6b4a2b"); // madeira da calha
    }

    /// <summary>
    /// Gárgula 24×18: uma bica de pedra em forma de cabeça de cão saindo de uma mísula presa na parede (à direita),
    /// olhando para a esquerda. Orelha em pé, olho, focinho e a boca aberta de onde sai um fio d'água contínuo
    /// (é por ela que a chuva escorre e alimenta a vinha embaixo). Maior que a versão anterior para ler de longe.
    /// </summary>
    static PixelCanvas Gargoyle()
    {
        string[] map =
        {
            "........kk........kkkkkk",
            ".......kLLk.......kLLLLk",
            "......kLSSLk......kSSSDk",
            ".....kLSSSSLkkkkkkkSSSDk",
            "....kLSSSSSSSSSSSSSSSSDk",
            "...kLSSSkkSSSSSSSSSSSSDk",
            "..kLSSSSkkSSSSSSSSSSSSDk",
            ".kLSSSSSSSSSSSSSSSSSSSDk",
            "kLLSSSSSSSSSSSSSSSSSSSDk",
            "kSSSjjjjjjSSSSSSSSSSSDDk",
            "kSjjWWWWjjjDSSSSSSSSDDDk",
            ".kkjWWjjkkkDDSSSSSSDDDk.",
            "...kWwk...kkDDDDDDDDkk..",
            "....Ww......kkkkkkkk....",
            "....wW..................",
            "....Ww..................",
            "....wW..................",
            "....Ww..................",
        };
        var cv = new PixelCanvas(24, map.Length);
        cv.Stamp(map, 0, 0);
        return cv;
    }

    /// <summary>
    /// Gota da gárgula 7×48 (3 tiles), 14 quadros a 12 fps: a gota se forma na boca (0–1), cai acelerando ao longo da
    /// vinha (2–8), espirra embaixo (9) e há uma pausa (10–13). Cada gárgula começa num ponto diferente do ciclo.
    /// </summary>
    static List<PixelCanvas> Drip()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('W', "#c8f4ff"); PixelKit.P('w', "#7fd0f0");
        PixelKit.Pal['s'] = Color.FromArgb(120, 0xc8, 0xf4, 0xff); // rastro leve
        var frames = new List<PixelCanvas>();
        int[] fall = { 4, 7, 12, 19, 27, 36, 44 };                  // y da gota nos quadros 2..8 (acelera)
        for (int f = 0; f < 14; f++)
        {
            var cv = new PixelCanvas(7, 48);
            if (f == 0) cv.Force(3, 0, 'w');
            else if (f == 1) { cv.Force(3, 0, 'w'); cv.Force(3, 1, 'W'); cv.Force(3, 2, 'w'); }   // a gota inchando
            else if (f <= 8)
            {
                int y = fall[f - 2];
                cv.Force(3, y, 'W'); cv.Force(3, y + 1, 'W'); cv.Force(3, y - 1, 'w');
                for (int t = 2; t <= 4; t++) if (y - t >= 0) cv.Force(3, y - t, 's');                // rastro
            }
            else if (f == 9)
            {
                // respingo: dois pingos saltando para os lados e um anel
                cv.Force(1, 45, 'w'); cv.Force(5, 45, 'w'); cv.Force(0, 44, 's'); cv.Force(6, 44, 's');
                cv.Force(2, 47, 'W'); cv.Force(3, 47, 'W'); cv.Force(4, 47, 'W');
            }
            frames.Add(cv);
        }
        return frames;
    }

    /// <summary>
    /// Alcova (recuo na parede do fundo) para uma sombra de w × h tiles: dois pilares de 8 px nas laterais (fora da
    /// sombra), um arco abatido de aduelas por cima e o fundo recuado mais escuro, com tijolos quase apagados. A luz das
    /// tochas não entra no recuo: é ISSO que explica a sombra jogável (que tem a mesma largura e altura).
    /// Paleta escura da parede do fundo (Soluki).
    /// </summary>
    static PixelCanvas Alcove(int wTiles, int hTiles)
    {
        int inner = wTiles * 16, W = inner + 16, archH = 20, H = hTiles * 16 + archH;
        var cv = new PixelCanvas(W, H);
        int springY = archH;                                      // onde o arco encontra os pilares
        for (int x = 0; x < W; x++)
        {
            bool pillar = x < 8 || x >= W - 8;
            // altura do intradorso (parte de baixo do arco) nesta coluna: arco abatido entre os pilares
            float t = (x - 8 + 0.5f) / inner * 2f - 1f;            // -1..1 entre os pilares
            int intrados = pillar ? 0 : springY - (int)Math.Round((1f - t * t) * (archH - 6));
            for (int y = 0; y < H; y++)
            {
                char c;
                if (pillar)
                {
                    if (y < springY - 4) continue;                    // acima do capitel, a parede continua (não desenha)
                    bool capital = y < springY;
                    int px = x < 8 ? x : W - 1 - x;                    // 0 = borda de fora
                    c = capital ? (y == springY - 4 ? 'H' : 'L') : px == 7 ? 'D' : px == 0 ? 'D' : x >= W - 8 && px == 1 ? 'L' : 'S';
                    if (!capital && (y - springY) % 12 == 11) c = 'j';  // juntas dos blocos do pilar
                }
                else if (y < intrados - 5) continue;                  // acima do arco: a parede normal
                else if (y < intrados)
                {
                    // aduelas do arco (5 px de espessura), com juntas radiais
                    c = ((x - 8) / 10) % 2 == 0 ? 'L' : 'S';
                    if ((x - 8) % 10 == 0) c = 'j';
                    if (y == intrados - 5) c = 'H';
                }
                else
                {
                    // fundo recuado: escuro, com tijolos quase sumindo
                    int ly = (y - intrados) % 8, bx = (x + ((y - intrados) / 8 % 2) * 8) % 16;
                    c = ly == 7 || bx == 15 ? 'k' : 'j';
                    if (y >= H - 2) c = 'D';                          // degrau da base
                }
                cv.Force(x, y, c);
            }
        }
        return cv;
    }

    /// <summary>Calha de madeira 14×8 (na arquibancada): canaleta de tábuas com a ponta pingando, olhando para a esquerda.</summary>
    static PixelCanvas Gutter()
    {
        string[] map =
        {
            "kkkkkkkkkkkkkk",
            "kTTTTTTTTTTTTk",
            "kttttttttttttk",
            ".kWkkkkkkkkkk.",
            "..w...........",
            "..W...........",
            "..w...........",
            "..W...........",
        };
        var cv = new PixelCanvas(14, map.Length);
        cv.Stamp(map, 0, 0);
        return cv;
    }
}
