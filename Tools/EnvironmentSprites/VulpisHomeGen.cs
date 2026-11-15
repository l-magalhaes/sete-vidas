// A casa da Vulpis por dentro (pedido do usuário, 14/11/2026: "deixe mais orgânica e detalhada, parecendo que alguém
// mora lá e com mais definição nos objetos"). Grava em Assets/_Game/Art/Environments/Forest/Placeholder (móveis) e
// Forest/Terrain (tiles), que o ForestPainter.DressCabin carrega. Contorno escuro em todo móvel, luz da lareira e da vela
// (toda luz tem quem a faça), madeira quente, a raposa nos detalhes (manta ruiva, mapa com a trilha dela). C# 5.
//   tile_flo_casa_parede_0..2   toras deitadas por dentro, com barro entre elas (2: um gancho de ferro)
//   tile_flo_casa_piso_0..1     tábuas do assoalho;  tile_flo_casa_alicerce_0..1  pedra embaixo
//   casa_lareira (48 × 144)     lareira de pedra até o teto, a boca escura; casa_fogo_0..2 (24 × 20) a chama
//   casa_cama (56 × 28)         cama de tábuas, colchão de palha, manta de pele ruiva, travesseiro
//   casa_mesa (40 × 30)         mesa com o mapa aberto, a caneca, a tigela de amoras e a vela acesa
//   casa_banco (14 × 14)        toco de banco
//   casa_prateleira (64 × 32)   tábua em mãos-francesas com potes, livros e um maço de ervas (o meio fica livre: moeda)
//   casa_arco (24 × 44)         o arco no suporte de dois pinos e a aljava cheia
//   casa_ervas (48 × 26)        ervas e cebolas secando penduradas numa vara do teto
//   casa_pele (32 × 36)         pele esticada num bastidor de galhos
//   casa_tapete (72 × 6)        tapete trançado no chão
//   casa_lenha (24 × 20)        lenha empilhada
//   casa_barril (18 × 26)       barril com flechas e uma lança
//   casa_janela (36 × 40)       janela com cortina, a lua lá fora e um vaso no peitoril
//   casa_mapa (30 × 24)         mapa da floresta pregado na parede, trilhas marcadas e um círculo vermelho no ninho
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.World.Scenery;

public static class VulpisHomeGen
{
    static string s_dir, s_terrain, s_template;
    static readonly List<string> s_props = new List<string>();

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        s_terrain = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Terrain");
        s_props.Clear();
        Palette();
        for (int v = 0; v < 3; v++) Tile("tile_flo_casa_parede_" + v, Wall(v));
        for (int v = 0; v < 2; v++) { Tile("tile_flo_casa_piso_" + v, Floor(v)); Tile("tile_flo_casa_alicerce_" + v, Footing(v)); }
        Prop("casa_lareira", Fireplace());
        for (int f = 0; f < 3; f++) Prop("casa_fogo_" + f, Fire(f));
        Prop("casa_cama", Bed());
        Prop("casa_mesa", Table());
        Prop("casa_banco", Stool());
        Prop("casa_prateleira", Shelf());
        Prop("casa_arco", BowRack());
        Prop("casa_ervas", Herbs());
        Prop("casa_pele", Pelt());
        Prop("casa_tapete", Rug());
        Prop("casa_lenha", Firewood());
        Prop("casa_barril", Barrel());
        Prop("casa_janela", Window());
        Prop("casa_mapa", Map());
        Preview(Path.Combine(previewDir, "casa_vulpis.png"));
        return s_props.Count + " móveis e 7 tiles";
    }

    /// <summary>Nomes dos móveis (o teste confere os PNG).</summary>
    public static readonly string[] Names =
    {
        "casa_lareira", "casa_fogo_0", "casa_fogo_1", "casa_fogo_2", "casa_cama", "casa_mesa", "casa_banco", "casa_prateleira",
        "casa_arco", "casa_ervas", "casa_pele", "casa_tapete", "casa_lenha", "casa_barril", "casa_janela", "casa_mapa",
    };

    static void Prop(string name, PixelCanvas c)
    {
        PixelKit.WriteSprite(c, Path.Combine(s_dir, name + ".png"), "flo_casa/" + name, s_template, false);
        s_props.Add(name);
    }

    static void Tile(string name, PixelCanvas c)
    {
        PixelKit.WriteSprite(c, Path.Combine(s_terrain, name + ".png"), name, s_template, false);
    }

    static uint H(int x, int y, int s) { return TerrainAutotile.Hash(x, y, s); }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#140c0a");                                                                    // contorno
        PixelKit.P('a', "#2e1a12"); PixelKit.P('b', "#4a2c1c"); PixelKit.P('c', "#6a4228"); PixelKit.P('d', "#8c5a34"); PixelKit.P('e', "#b07a48"); // madeira
        PixelKit.P('m', "#5a4632"); PixelKit.P('n', "#7a6448");                                       // barro entre as toras
        PixelKit.P('s', "#3a363e"); PixelKit.P('t', "#56505a"); PixelKit.P('u', "#787078"); PixelKit.P('v', "#9a9298"); // pedra
        PixelKit.P('r', "#a8481e"); PixelKit.P('R', "#d06a2a"); PixelKit.P('S', "#ec9a52");          // ruivo (raposa)
        PixelKit.P('w', "#e8d8b8"); PixelKit.P('W', "#c8b494");                                       // creme
        PixelKit.P('q', "#7a2a2a"); PixelKit.P('Q', "#a83a36");                                       // tecido vermelho
        PixelKit.P('g', "#2e4a2c"); PixelKit.P('G', "#4e7444"); PixelKit.P('H', "#7aa060");          // ervas
        PixelKit.P('p', "#8a5a3a"); PixelKit.P('P', "#b47a4e");                                       // barro (potes, caneca)
        PixelKit.P('y', "#c8b070"); PixelKit.P('Y', "#e6d498");                                       // palha
        PixelKit.P('f', "#b8401e"); PixelKit.P('F', "#f08a3c"); PixelKit.P('z', "#ffd88a"); PixelKit.P('Z', "#fff6d8"); // fogo
        PixelKit.P('o', "#121018"); PixelKit.P('O', "#2a2632");                                       // boca da lareira, fuligem
        PixelKit.P('i', "#1a2440"); PixelKit.P('j', "#2c3a62"); PixelKit.P('l', "#dfe6f0");          // noite lá fora, lua
        PixelKit.P('x', "#5a3a6a"); PixelKit.P('X', "#7a52a0");                                       // amoras
        PixelKit.P('h', "#3a5a7a"); PixelKit.P('M', "#6a88a8");                                       // pote azul
    }

    static void Rect(PixelCanvas c, int x0, int y0, int x1, int y1, char ch)
    {
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) c.Force(x, y, ch);
    }

    static void Stamp(PixelCanvas c, int x0, int y0, string[] rows)
    {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '.') c.Force(x0 + x, y0 + y, rows[y][x]);
    }

    /// <summary>Contorno de 1 px em volta do que já está desenhado (definição: todo móvel tem borda).</summary>
    static void Outline(PixelCanvas c)
    {
        var add = new List<Point>();
        for (int y = 0; y < c.H; y++)
            for (int x = 0; x < c.W; x++)
            {
                if (c.Get(x, y) != '.') continue;
                if (c.Get(x - 1, y) != '.' && c.Get(x - 1, y) != 'k' || c.Get(x + 1, y) != '.' && c.Get(x + 1, y) != 'k' ||
                    c.Get(x, y - 1) != '.' && c.Get(x, y - 1) != 'k' || c.Get(x, y + 1) != '.' && c.Get(x, y + 1) != 'k')
                    add.Add(new Point(x, y));
            }
        foreach (var p in add) c.Force(p.X, p.Y, 'k');
    }

    /// <summary>Tábua horizontal (luz em cima, sombra embaixo, veio e pregos), de x0 a x1, y0 a y1.</summary>
    static void Plank(PixelCanvas c, int x0, int y0, int x1, int y1, int seed)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                char ch = y == y0 ? 'e' : y == y1 ? 'b' : (H(x / 3, y, seed) % 9u == 0 ? 'c' : 'd');
                c.Force(x, y, ch);
            }
        c.Force(x0 + 1, y0 + 1, 'a'); c.Force(x1 - 1, y0 + 1, 'a');
    }

    // ============================================================ tiles

    /// <summary>Toras deitadas por dentro (8 px cada), barro claro entre elas; luz de cima (a lareira está embaixo).</summary>
    static PixelCanvas Wall(int v)
    {
        var c = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int r = y % 8;
                char ch = r == 0 ? 'm' : r == 1 ? 'n' : r == 2 ? 'd' : r == 7 ? 'a' : r == 6 ? 'b' : (H((x + v * 16) / 4, y, 801) % 7u == 0 ? 'b' : 'c');
                c.Force(x, y, ch);
            }
        if (v == 1) { Rect(c, 9, 3, 11, 5, 'b'); c.Force(10, 4, 'a'); }                              // um nó na tora
        if (v == 2) { c.Force(6, 10, 'u'); c.Force(6, 11, 'u'); c.Force(7, 12, 'u'); c.Force(8, 12, 's'); }   // gancho de ferro
        return c;
    }

    static PixelCanvas Floor(int v)
    {
        var c = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int seam = (x + v * 9) % 16 == 0 || (y > 7 && (x + v * 9 + 8) % 16 == 0) ? 1 : 0;
                char ch = y == 0 ? 'e' : y == 7 || y == 15 || seam == 1 ? 'a' : y == 1 || y == 8 ? 'd' : (H(x / 4, y, 811 + v) % 6u == 0 ? 'b' : 'c');
                c.Force(x, y, ch);
            }
        c.Force(3 + v * 5, 4, 'a'); c.Force(11 - v * 4, 11, 'a');                                    // pregos
        return c;
    }

    static PixelCanvas Footing(int v)
    {
        var c = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int bx = (x + (y / 8 % 2) * 8 + v * 4) % 16;
                char ch = y % 8 == 7 || bx == 0 ? 's' : y % 8 == 0 ? 'u' : (H(x, y, 821) % 5u == 0 ? 's' : 't');
                c.Force(x, y, ch);
            }
        return c;
    }

    // ============================================================ móveis

    /// <summary>Lareira de pedra até o teto: chaminé de pedras irregulares, cornija de madeira com potes, boca escura com fuligem.</summary>
    static PixelCanvas Fireplace()
    {
        var c = new PixelCanvas(48, 144);
        // chaminé (mais estreita) e o corpo da lareira (mais largo, embaixo)
        for (int y = 0; y < 144; y++)
        {
            int x0 = y < 80 ? 8 : 0, x1 = y < 80 ? 39 : 47;
            for (int x = x0; x <= x1; x++)
            {
                int row = y / 7, off = (row % 2) * 5;
                int bx = (x + off + (int)(H(row, 0, 831) % 4u)) % 11;
                char ch = y % 7 == 6 || bx == 0 ? 's' : y % 7 == 0 || bx == 1 ? 'v' : (H(x / 2, y / 2, 832) % 6u == 0 ? 't' : 'u');
                c.Force(x, y, ch);
            }
        }
        // cornija de madeira
        Plank(c, 0, 76, 47, 81, 833);
        // a boca da lareira (arco escuro) com fuligem subindo
        for (int y = 96; y < 144; y++)
            for (int x = 8; x <= 39; x++)
            {
                double dx = (x + 0.5 - 24) / 16.0, dy = (y + 0.5 - 112) / 16.0;
                if (y < 112 && dx * dx + dy * dy > 1) continue;
                c.Force(x, y, y > 140 ? 'O' : 'o');
            }
        for (int y = 84; y < 100; y++) for (int x = 18; x <= 30; x++) if (H(x, y, 834) % 3u == 0) c.Force(x, y, 'O');
        // pedra da soleira
        Rect(c, 4, 140, 43, 143, 't'); Rect(c, 4, 140, 43, 140, 'v');
        // na cornija: dois potes de barro e um ramo de ervas
        Stamp(c, 6, 66, new[] { ".kkk.", "kPPpk", "kPppk", "kpppk", ".kkk." });
        Stamp(c, 36, 64, new[] { "..kk..", ".kPPk.", "kPPppk", "kPpppk", "kppppk", ".kkkk." });
        Stamp(c, 18, 68, new[] { "..H.G..", ".GgHgG.", "..ggg..", "...b...", "...b..." });
        Outline(c);
        return c;
    }

    /// <summary>A chama da lareira em 3 quadros: lenha cruzada embaixo, fogo em línguas (borda vermelha, miolo claro).</summary>
    static PixelCanvas Fire(int f)
    {
        var c = new PixelCanvas(24, 20);
        Rect(c, 3, 16, 20, 18, 'b'); Rect(c, 3, 16, 20, 16, 'd'); Rect(c, 6, 15, 17, 15, 'c');          // lenha
        c.Force(5, 17, 'r'); c.Force(12, 17, 'F'); c.Force(18, 17, 'r');                              // brasas
        int[] tips = f == 0 ? new[] { 4, 11, 5 } : f == 1 ? new[] { 6, 9, 3 } : new[] { 3, 12, 6 };
        int[] xs = { 7, 12, 17 };
        for (int i = 0; i < 3; i++)
            for (int y = tips[i]; y < 16; y++)
            {
                double t = (y - tips[i]) / (double)(16 - tips[i]);
                int half = (int)Math.Round(1 + t * 3);
                int sway = (int)Math.Round(Math.Sin((y + f * 3 + i * 2) * 0.6) * (1 - t));
                for (int dx = -half; dx <= half; dx++)
                {
                    int x = xs[i] + dx + sway;
                    char ch = Math.Abs(dx) == half ? 'f' : Math.Abs(dx) >= half - 1 ? 'F' : t > 0.5 ? 'z' : 'F';
                    if (t > 0.65 && Math.Abs(dx) <= 1) ch = 'Z';
                    if (c.Get(x, y) == '.' || ch == 'Z' || ch == 'z') c.Force(x, y, ch);
                }
            }
        return c;
    }

    static PixelCanvas Bed()
    {
        var c = new PixelCanvas(56, 28);
        // cabeceira (à esquerda) e pés da cama
        Rect(c, 1, 2, 5, 27, 'c'); Rect(c, 1, 2, 1, 27, 'e'); Rect(c, 1, 2, 5, 3, 'd');
        Rect(c, 50, 10, 53, 27, 'c'); Rect(c, 50, 10, 50, 27, 'e');
        Plank(c, 4, 18, 53, 22, 841);                                                                   // a armação
        // colchão de palha
        for (int y = 12; y <= 17; y++) for (int x = 6; x <= 50; x++) c.Force(x, y, y == 12 ? 'Y' : (H(x, y, 842) % 4u == 0 ? 'y' : 'Y'));
        // travesseiro
        Stamp(c, 6, 7, new[] { ".wwwwwwww.", "wwwwwwwwwW", "wWwwwwwwWW", ".WWWWWWWW." });
        // a manta de pele ruiva com a barra creme, dobrada em cima
        for (int y = 9; y <= 19; y++)
            for (int x = 17; x <= 49; x++)
            {
                if (y < 11 && x > 47) continue;
                char ch = y == 9 ? 'S' : y >= 18 ? 'w' : (x + y) % 7 == 0 ? 'r' : y < 12 ? 'S' : 'R';
                c.Force(x, y, ch);
            }
        Rect(c, 17, 9, 19, 19, 'w');                                                                    // a dobra creme
        for (int x = 20; x <= 48; x += 4) c.Force(x, 20, 'W');                                         // franja
        Outline(c);
        return c;
    }

    static PixelCanvas Table()
    {
        var c = new PixelCanvas(40, 30);
        Plank(c, 0, 14, 39, 17, 851);
        Rect(c, 3, 18, 5, 29, 'c'); Rect(c, 3, 18, 3, 29, 'd'); Rect(c, 34, 18, 36, 29, 'c'); Rect(c, 34, 18, 34, 29, 'd');
        Rect(c, 5, 24, 34, 25, 'b');                                                                    // a travessa
        // o mapa aberto, com a trilha
        Rect(c, 12, 11, 27, 13, 'W'); Rect(c, 12, 11, 27, 11, 'w');
        c.Force(14, 12, 'q'); c.Force(16, 12, 'q'); c.Force(18, 12, 'q'); c.Force(22, 12, 'Q'); c.Force(25, 12, 'q');
        // caneca de barro
        Stamp(c, 3, 7, new[] { "kkkk.", "kPpkk", "kPpk.k", "kPpkk", "kkkk." });
        // tigela de amoras
        Stamp(c, 28, 9, new[] { ".xXxX.", "XxXxXx", "pPPPPp", ".pppp." });
        // a vela acesa num pires
        Stamp(c, 34, 0, new[] { ".z.", "zZz", ".F.", ".w.", ".w.", ".w.", ".W.", ".W.", "kkkk", "tvtk" });
        Outline(c);
        return c;
    }

    static PixelCanvas Stool()
    {
        var c = new PixelCanvas(14, 14);
        for (int y = 2; y < 14; y++) for (int x = 2; x < 12; x++) c.Force(x, y, x == 2 ? 'd' : x == 11 ? 'b' : 'c');
        Rect(c, 1, 1, 12, 3, 'e'); Rect(c, 3, 2, 10, 2, 'd');
        c.Force(6, 2, 'c'); c.Force(7, 2, 'b'); c.Force(5, 2, 'c');                                    // anéis do toco
        Outline(c);
        return c;
    }

    /// <summary>Prateleira: tábua (topo na linha 20) em duas mãos-francesas; objetos em cima, o meio livre (a moeda).</summary>
    static PixelCanvas Shelf()
    {
        var c = new PixelCanvas(64, 32);
        Plank(c, 0, 20, 63, 23, 861);
        foreach (int bx in new[] { 6, 54 })
        {
            Rect(c, bx, 24, bx + 2, 31, 'c');
            for (int i = 0; i < 6; i++) c.Force(bx + 3 + i, 24 + i, 'b');
        }
        // potes (barro, azul), livros e as ervas, deixando px 24..40 livres
        Stamp(c, 2, 9, new[] { ".kkk..", "kPPpk.", "kPpppk", "kPpppk", "kPpppk", "kpppkk", ".kkk.." });
        Stamp(c, 9, 12, new[] { ".kk.", "khMk", "khhk", "khhk", ".kk." });
        Stamp(c, 14, 7, new[] { "kkkkkkk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kQqkcdk", "kkkkkkk" });
        Stamp(c, 43, 4, new[] { "..H.G.H..", ".GgHGgHG.", "..GgggG..", "...ggg...", "....b....", "....b....", "...bbb...", "..kPPPk..", "..kPppk..", "..kPppk..", "..kpppk..", "..kpppk..", "..kpppk..", "...kkk..." });
        Stamp(c, 54, 13, new[] { ".kkkkk", "kwWwWk", "kWwWwk", "kwWwWk", "kWwWwk", ".kkkkk" });   // um rolo de pano
        Outline(c);
        return c;
    }

    static PixelCanvas BowRack()
    {
        var c = new PixelCanvas(24, 44);
        // dois pinos na parede e o arco deitado neles (de pé, curvado para a esquerda), com a corda
        Rect(c, 9, 6, 12, 7, 'b'); Rect(c, 9, 34, 12, 35, 'b');
        for (int y = 1; y < 41; y++)
        {
            double t = (y - 21) / 20.0;
            int x = 4 + (int)Math.Round(5 * (t * t));
            c.Force(x, y, 'd'); c.Force(x + 1, y, 'c');
            if (y == 21 || y == 20 || y == 22) { c.Force(x, y, 'Q'); c.Force(x + 1, y, 'q'); }         // a empunhadura
            c.Force(9, y, 'W');                                                                         // a corda
        }
        // aljava de couro com as flechas (penas ruivas)
        Stamp(c, 15, 10, new[] {
            ".S.S.S", ".R.r.R", ".e.e.e", ".e.e.e", "kkkkkk.", "kcdddck", "kcddcck", "kcddcck", "kcddcck", "kcdqcck",
            "kcddcck", "kcddcck", "kcddcck", "kccccck", ".kkkkk." });
        Outline(c);
        return c;
    }

    static PixelCanvas Herbs()
    {
        var c = new PixelCanvas(48, 26);
        Rect(c, 0, 0, 1, 3, 'b'); Rect(c, 46, 0, 47, 3, 'b');                                           // as cordinhas do teto
        Plank(c, 0, 3, 47, 5, 871);                                                                      // a vara
        int[] xs = { 5, 13, 21, 29, 37, 43 };
        for (int i = 0; i < xs.Length; i++)
        {
            int x = xs[i];
            c.Force(x, 6, 'W'); c.Force(x, 7, 'W');
            if (i % 3 == 2)   // cebolas
                Stamp(c, x - 2, 8, new[] { "..W..", ".wWw.", "wwWww", "wWwWw", ".wWw.", "..W.." });
            else
            {
                int len = 9 + (int)(H(i, 0, 872) % 6u);
                for (int y = 8; y < 8 + len; y++)
                {
                    int w = 1 + (y - 8) / 4;
                    for (int dx = -w; dx <= w; dx++) c.Force(x + dx, y, (dx + y) % 3 == 0 ? 'g' : (i % 2 == 0 ? 'G' : 'H'));
                }
            }
        }
        Outline(c);
        return c;
    }

    static PixelCanvas Pelt()
    {
        var c = new PixelCanvas(32, 36);
        // bastidor de galhos em losango e a pele esticada com cordinhas
        foreach (var p in PixelKit.Line(16, 1, 30, 18)) { c.Force(p.X, p.Y, 'c'); c.Force(p.X, p.Y + 1, 'b'); }
        foreach (var p in PixelKit.Line(30, 18, 16, 34)) { c.Force(p.X, p.Y, 'c'); c.Force(p.X, p.Y + 1, 'b'); }
        foreach (var p in PixelKit.Line(16, 34, 2, 18)) { c.Force(p.X, p.Y, 'c'); c.Force(p.X, p.Y + 1, 'b'); }
        foreach (var p in PixelKit.Line(2, 18, 16, 1)) { c.Force(p.X, p.Y, 'c'); c.Force(p.X, p.Y + 1, 'b'); }
        for (int y = 7; y < 30; y++)
        {
            int half = (int)Math.Round(9 * Math.Sin(Math.PI * (y - 6) / 24.0));
            for (int dx = -half; dx <= half; dx++) c.Force(16 + dx, y, Math.Abs(dx) == half ? 'W' : dx < -half / 2 ? 'w' : 'W');
        }
        foreach (var p in new[] { new[] { 9, 9 }, new[] { 23, 9 }, new[] { 7, 18 }, new[] { 25, 18 }, new[] { 10, 27 }, new[] { 22, 27 } })
        { c.Force(p[0], p[1], 'b'); }
        Outline(c);
        return c;
    }

    static PixelCanvas Rug()
    {
        var c = new PixelCanvas(72, 6);
        for (int y = 1; y < 5; y++)
            for (int x = 2; x < 70; x++)
            {
                int band = (x / 6) % 3;
                char ch = y == 1 ? 'S' : y == 4 ? 'q' : band == 0 ? 'R' : band == 1 ? 'w' : 'Q';
                if ((x + y) % 6 == 0) ch = 'r';
                c.Force(x, y, ch);
            }
        for (int x = 0; x < 72; x += 2) { c.Force(x, 5, 'W'); }
        c.Force(0, 2, 'W'); c.Force(0, 3, 'W'); c.Force(71, 2, 'W'); c.Force(71, 3, 'W');               // franjas
        return c;
    }

    static PixelCanvas Firewood()
    {
        var c = new PixelCanvas(24, 20);
        int[][] logs = { new[] { 1, 14 }, new[] { 9, 14 }, new[] { 17, 14 }, new[] { 5, 8 }, new[] { 13, 8 }, new[] { 9, 2 } };
        foreach (var l in logs)
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 7; x++)
                {
                    double dx = (x + 0.5 - 3.5) / 3.5, dy = (y + 0.5 - 3) / 3.0, d = dx * dx + dy * dy;
                    if (d > 1) continue;
                    c.Force(l[0] + x, l[1] + y, d > 0.6 ? 'c' : d > 0.25 ? 'e' : 'd');
                }
        Outline(c);
        return c;
    }

    static PixelCanvas Barrel()
    {
        var c = new PixelCanvas(18, 26);
        // flechas e uma lança saindo do barril
        for (int i = 0; i < 4; i++) { int x = 4 + i * 3; Rect(c, x, 2 + i % 2 * 2, x, 10, 'd'); c.Force(x, 1 + i % 2 * 2, 'R'); c.Force(x - 1, 2 + i % 2 * 2, 'S'); }
        Rect(c, 13, 0, 13, 10, 'c'); c.Force(13, 0, 'v'); c.Force(12, 1, 'u'); c.Force(14, 1, 'u');   // a lança
        for (int y = 10; y < 26; y++)
        {
            int bulge = (int)Math.Round(1.5 * Math.Sin(Math.PI * (y - 10) / 15.0));
            for (int x = 2 - bulge; x <= 15 + bulge; x++)
                c.Force(x, y, y == 13 || y == 22 ? 't' : x <= 3 - bulge ? 'd' : x >= 14 + bulge ? 'b' : ((x + 1) % 4 == 0 ? 'b' : 'c'));
        }
        Rect(c, 2, 10, 15, 10, 'e');
        Outline(c);
        return c;
    }

    static PixelCanvas Window()
    {
        var c = new PixelCanvas(36, 40);
        Rect(c, 4, 4, 31, 33, 'c');
        // vidros: a noite e a lua lá fora, com copas escuras embaixo
        for (int y = 7; y <= 30; y++)
            for (int x = 7; x <= 28; x++) c.Force(x, y, y < 22 ? (y < 13 ? 'i' : 'j') : 'g');
        Stamp(c, 20, 9, new[] { ".ll.", "llll", "lll.", ".l.." });
        for (int x = 7; x <= 28; x++) if (H(x, 0, 881) % 2u == 0) c.Force(x, 21, 'g');
        Rect(c, 17, 7, 18, 30, 'd'); Rect(c, 7, 18, 28, 19, 'd');                                      // a cruz
        // cortinas presas dos lados e a vara
        Rect(c, 1, 2, 34, 3, 'b');
        for (int y = 4; y < 34; y++)
        {
            int wl = y < 20 ? 5 : 3 + (y - 20) / 4, wr = wl;
            for (int x = 1; x < 1 + wl; x++) c.Force(x, y, (x + y / 3) % 3 == 0 ? 'q' : 'Q');
            for (int x = 34 - wr; x < 35; x++) c.Force(x, y, (x + y / 3) % 3 == 0 ? 'q' : 'Q');
        }
        // peitoril com o vaso de planta
        Plank(c, 2, 33, 33, 35, 882);
        Stamp(c, 11, 25, new[] { "..H.G...", ".GgHgGH.", "..ggGg..", "...gg...", "..kkkk..", "..kPpk..", "..kPpk..", "...kk..." });
        Outline(c);
        return c;
    }

    static PixelCanvas Map()
    {
        var c = new PixelCanvas(30, 24);
        Rect(c, 1, 1, 28, 22, 'W'); Rect(c, 1, 1, 28, 1, 'w'); Rect(c, 2, 2, 3, 21, 'w');
        // a floresta (manchas verdes), a trilha tracejada e o círculo vermelho no ninho, lá em cima
        foreach (var p in new[] { new[] { 6, 16 }, new[] { 10, 9 }, new[] { 19, 15 }, new[] { 22, 6 } })
            for (int y = -2; y <= 2; y++) for (int x = -3; x <= 3; x++) if (x * x + y * y * 2 <= 9) c.Force(p[0] + x, p[1] + y, 'G');
        int[,] trail = { { 4, 20 }, { 7, 18 }, { 10, 17 }, { 13, 15 }, { 15, 12 }, { 17, 10 }, { 19, 8 }, { 21, 6 } };
        for (int i = 0; i < trail.GetLength(0); i++) c.Force(trail[i, 0], trail[i, 1], 'q');
        Stamp(c, 21, 3, new[] { ".QQQ.", "Q...Q", "Q.k.Q", "Q...Q", ".QQQ." });
        c.Force(14, 1, 'u'); c.Force(14, 0, 's');                                                     // o prego
        Outline(c);
        return c;
    }

    // ============================================================ prévia

    static void Preview(string path)
    {
        const int S = 3;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var sheet = new Bitmap(560 * S, 200 * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#5a4030"));
            int x = 4, y = 4, rowH = 0;
            foreach (var n in s_props)
            {
                using (var tmp = new Bitmap(Path.Combine(s_dir, n + ".png")))
                {
                    if (x + tmp.Width > 556) { x = 4; y += rowH + 6; rowH = 0; }
                    g.DrawImage(tmp, x * S, y * S, tmp.Width * S, tmp.Height * S);
                    x += tmp.Width + 6; rowH = Math.Max(rowH, tmp.Height);
                }
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
