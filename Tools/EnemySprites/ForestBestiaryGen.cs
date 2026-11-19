// F4c, arte definitiva do bestiário da Floresta (pedido do usuário, 14/11/2026). Grava em
// Assets/_Game/Art/Environments/Forest/Placeholder com os MESMOS nomes que o ForestTestSetup/ForestGrayboxSetup carregam
// (preservando .meta/GUID) e os quadros novos de animação. Apaga o "<nome>_infectado.png" de cada um: o menu 21 gera de
// novo a variante infectada (troca de paleta do ForestInfectedArt) a partir da arte nova.
//
// Direção de arte (Docs/Arte/floresta-guia-visual.md): noite, lua no alto à esquerda (luz na borda de cima/esquerda),
// contorno escuro, poucas cores por bicho. Turquesa SÓ onde dá pogo (a flor da Planta no recuo); nada de amarelo-dourado
// (é a rachadura do Paladino); o roxo e o verde-ácido são da praga (só na variante infectada).
//
//   Vespas da Raiz (no lugar do Morcego: "não é mais um morcego, e sim insetos voadores"), 24 × 20, olham para a esquerda
//     morcego_dormindo, morcego_dormindo_1   três vespas agarradas de ponta-cabeça na raiz; uma mexe a antena
//     morcego_voo_a.._d                      o enxame voando: asas batendo, as três se mexendo em volta
//   Galho Seco e Broto (com "os mesmos assets das árvores comuns": a casca dos troncos do cenário e a copa das árvores),
//   96 × 104, pés embaixo no meio, olham para a esquerda
//     galho_alto_disfarce, _1, _2            parece uma árvore; a copa balança; de vez em quando um olho se abre na casca
//     galho_alto, galho_alto_1               acordado: dois olhos acesos nos nós, o galho-braço erguido de lado
//     galho_alto_braco, _1                   o braço lá no alto, tremendo (aviso)
//     galho_alto_preso                       o braço batido no chão à frente, lascas
//     broto_*                                os mesmos (as vespas saem da colmeia que o jogo pendura nele)
//   Planta Carnívora (inspirada na planta de cano, com identidade própria: um bulbo vinho com pintas claras, lábios
//   de espinhos e duas folhas-orelha), cabeça 24 × 24 com a boca PARA CIMA (o jogo gira a cabeça para onde ela vai)
//     planta_cabeca, _1                      fechada, respirando
//     planta_cabeca_aberta, _1               aberta: as duas metades da boca abrem em V, espinhos, a língua
//     planta_cabeca_recuo                    murcha voltando, com a flor turquesa no alto (o pogo)
//     planta_pescoco (6 × 6), planta_base (24 × 8)
//   Serpente (refeita pelo bestiário), 32 × 16, olha para a direita
//     serpente_buraco (32 × 8)               a toca CAMUFLADA no chão: um montinho de musgo e terra igual ao topo do chão
//     serpente_olhos, _1, _2                 na toca: só os olhos na fresta; brilho; piscada
//     serpente_aviso, _1                     a cabeça sai da toca, a língua
//     serpente_bote                          a boca aberta, as presas
//     serpente, serpente_1                   rastejando (o corpo ondula)
//     serpente_gomo (16 × 16)                gomo do corpo esticado (emenda dos lados)
// C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public static class ForestBestiaryGen
{
    static string s_dir, s_template;
    static readonly List<string> s_written = new List<string>();

    public static string Run(string projectRoot, string metaTemplatePath, string previewPath)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        s_written.Clear();
        Wasps();
        Branches();
        Plant();
        Snake();
        Preview(previewPath);
        return s_written.Count + " quadros";
    }

    static void Write(string name, PixelCanvas cv)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_dir, name + ".png"), "flo_bicho/" + name, s_template, false);
        string inf = Path.Combine(s_dir, name + "_infectado.png");
        if (File.Exists(inf)) File.Delete(inf);                    // o menu 21 refaz a variante infectada da arte nova
        s_written.Add(name);
    }

    static double N(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    static void Rect(PixelCanvas c, int x0, int y0, int x1, int y1, char ch)
    {
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) c.Force(x, y, ch);
    }

    static void Disc(PixelCanvas c, double cx, double cy, double r, char ch)
    {
        for (int y = (int)Math.Floor(cy - r); y <= (int)Math.Ceiling(cy + r); y++)
            for (int x = (int)Math.Floor(cx - r); x <= (int)Math.Ceiling(cx + r); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                if (dx * dx + dy * dy <= r * r) c.Force(x, y, ch);
            }
    }

    /// <summary>Contorno de 1 px (cor <paramref name="o"/>) em volta de tudo o que já está desenhado.</summary>
    static void Outline(PixelCanvas c, char o)
    {
        var add = new List<int[]>();
        for (int y = 0; y < c.H; y++)
            for (int x = 0; x < c.W; x++)
            {
                if (c.Get(x, y) != '.') continue;
                if (c.Get(x - 1, y) != '.' && c.Get(x - 1, y) != o || c.Get(x + 1, y) != '.' && c.Get(x + 1, y) != o ||
                    c.Get(x, y - 1) != '.' && c.Get(x, y - 1) != o || c.Get(x, y + 1) != '.' && c.Get(x, y + 1) != o)
                    add.Add(new[] { x, y });
            }
        foreach (var p in add) c.Force(p[0], p[1], o);
    }

    // =========================================================== Vespas da Raiz

    static void WaspPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#0e0c10");
        PixelKit.P('a', "#1c1824"); PixelKit.P('b', "#2e2838"); PixelKit.P('c', "#4e4460");          // quitina (luar)
        PixelKit.P('o', "#6e5232"); PixelKit.P('O', "#94703e");                                       // listras ocre, apagadas
        PixelKit.P('r', "#b8462c"); PixelKit.P('R', "#ff8a5a");                                       // olhos
        PixelKit.Pal['w'] = Color.FromArgb(150, 200, 214, 228); PixelKit.Pal['W'] = Color.FromArgb(90, 200, 214, 228); // asas
        PixelKit.P('p', "#3a2a1e"); PixelKit.P('P', "#5a4230");                                       // a raiz do poleiro
    }

    // Vespa desenhada à mão, olhando para a esquerda: cabeça com o olho, tórax ao luar, abdômen listrado e o ferrão.
    static readonly string[] WaspBig =
    {
        "..kk.kk.....",
        ".kbbkcakkkk.",
        "kRbbkacaOoOk",
        ".kbbkaakoOokk",
        "..k..k.k.kk.",
    };
    static readonly string[] WaspSmall =
    {
        "..k.kk...",
        ".kbkcakk.",
        "kRbkaOoOk",
        ".kbkaookk",
        "..k.k.k..",
    };

    /// <summary>Asas (x, y relativos ao canto do corpo; y negativo = acima). 0 em cima, 1 no meio, 2 borrão.</summary>
    static readonly int[][][] WingsBig =
    {
        new[] { new[] { 6, -3 }, new[] { 7, -3 }, new[] { 5, -2 }, new[] { 6, -2 }, new[] { 7, -2 }, new[] { 8, -2 }, new[] { 6, -1 }, new[] { 7, -1 } },
        new[] { new[] { 5, -1 }, new[] { 6, -1 }, new[] { 7, -1 }, new[] { 8, -1 }, new[] { 9, -1 }, new[] { 10, -1 }, new[] { 6, -2 }, new[] { 7, -2 }, new[] { 8, -2 } },
        new[] { new[] { 5, -1 }, new[] { 6, -1 }, new[] { 7, -1 }, new[] { 8, -1 }, new[] { 9, -1 }, new[] { 6, -2 }, new[] { 7, -2 }, new[] { 6, -3 } },
    };
    static readonly int[][][] WingsSmall =
    {
        new[] { new[] { 4, -2 }, new[] { 5, -2 }, new[] { 4, -1 }, new[] { 5, -1 }, new[] { 6, -1 } },
        new[] { new[] { 3, -1 }, new[] { 4, -1 }, new[] { 5, -1 }, new[] { 6, -1 }, new[] { 7, -1 } },
        new[] { new[] { 3, -1 }, new[] { 4, -1 }, new[] { 5, -1 }, new[] { 6, -1 }, new[] { 4, -2 }, new[] { 5, -2 } },
    };

    /// <summary>
    /// Uma vespa com o canto de cima à esquerda do corpo em (x, y). <paramref name="wing"/>: 0 em cima, 1 no meio, 2 borrão.
    /// <paramref name="hang"/>: de ponta-cabeça agarrada ao poleiro, asas fechadas ao longo do corpo.
    /// </summary>
    static void Wasp(PixelCanvas c, int x, int y, bool big, int wing, bool hang, bool twitch)
    {
        var body = big ? WaspBig : WaspSmall;
        int h = body.Length;
        if (!hang)
            foreach (var p in (big ? WingsBig : WingsSmall)[wing]) c.Force(x + p[0], y + p[1], wing == 2 ? 'W' : 'w');
        for (int r = 0; r < h; r++)
        {
            string row = body[hang ? h - 1 - r : r];
            for (int i = 0; i < row.Length; i++) if (row[i] != '.') c.Force(x + i, y + r, row[i]);
        }
        if (hang)
        {
            for (int i = 4; i < (big ? 10 : 8); i++) c.Force(x + i, y + h, 'W');            // asas fechadas
            if (twitch) { c.Force(x + 1, y + h, 'k'); c.Force(x, y + h + 1, 'k'); }          // a antena mexe
            else c.Force(x + 2, y + h, 'k');
        }
        else { c.Force(x + 1, y - 1, 'k'); c.Force(x, y - 2, twitch ? '.' : 'k'); }        // antena
    }

    static void Wasps()
    {
        WaspPalette();
        for (int f = 0; f < 2; f++)
        {
            var c = new PixelCanvas(24, 20);
            for (int x = 0; x < 24; x++) { c.Force(x, 0, x % 5 == 0 ? 'P' : 'p'); c.Force(x, 1, x % 7 == 3 ? 'p' : '.'); }  // a raiz
            Wasp(c, 1, 2, false, 0, true, f == 1);
            Wasp(c, 8, 2, true, 0, true, false);
            Wasp(c, 16, 3, false, 0, true, f == 0);
            Write(f == 0 ? "morcego_dormindo" : "morcego_dormindo_1", c);
        }
        // voando: quatro quadros com as asas fora de fase e as vespas mudando de lugar em volta do meio
        string[] names = { "morcego_voo_a", "morcego_voo_b", "morcego_voo_c", "morcego_voo_d" };
        int[,] jit = { { 0, 0, 0, 0, 0, 0 }, { 1, -1, 0, 1, -1, 0 }, { 0, -1, -1, 0, 0, 1 }, { -1, 0, 0, -1, 1, 1 } };
        for (int f = 0; f < 4; f++)
        {
            var c = new PixelCanvas(24, 20);
            Wasp(c, 1 + jit[f, 0], 4 + jit[f, 1], false, (f + 1) % 3, false, false);
            Wasp(c, 14 + jit[f, 4], 14 + jit[f, 5], false, (f + 2) % 3, false, f % 2 == 1);
            Wasp(c, 6 + jit[f, 2], 9 + jit[f, 3], true, f % 3, false, false);
            Write(names[f], c);
        }
    }

    // =========================================================== Galho Seco e Broto

    const int BW = 96, BH = 104, TrunkX = 42, TrunkW = 12, Shoulder = 46;

    static void BranchPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#0e0c10");
        // casca: a mesma dos troncos do cenário (ForestSpriteGen, coluna fina de chão)
        PixelKit.P('w', "#141012"); PixelKit.P('x', "#2a1e18"); PixelKit.P('y', "#3e2e24"); PixelKit.P('z', "#5a4636");
        // copa: as cores das árvores comuns do cenário (ForestShadowGen.TreePalette), um degrau de luar a mais
        PixelKit.P('a', "#0f1813"); PixelKit.P('b', "#132119"); PixelKit.P('c', "#182a1f"); PixelKit.P('e', "#203628"); PixelKit.P('f', "#2c4634");
        PixelKit.P('r', "#8a3c1e"); PixelKit.P('R', "#e07a3a"); PixelKit.P('S', "#ffc08a");          // olhos (âmbar, nunca dourado)
        PixelKit.P('n', "#b8ac90"); PixelKit.P('N', "#d8ccb0");                                       // casulos do Broto
    }

    /// <summary>A copa (lobos com luar no alto à esquerda), com <paramref name="shift"/> px de balanço.</summary>
    static void Crown(PixelCanvas c, int shift, bool cocoons, int seed)
    {
        double[][] lobes =
        {
            new[] { 48.0, 26, 20 }, new[] { 32.0, 30, 13 }, new[] { 64.0, 29, 14 }, new[] { 40.0, 14, 13 }, new[] { 57.0, 15, 12 },
            new[] { 48.0, 7, 9 }, new[] { 25.0, 38, 8 }, new[] { 71.0, 38, 8 },
        };
        for (int y = 0; y < 52; y++)
            for (int x = 0; x < BW; x++)
            {
                double best = -9;
                foreach (var l in lobes)
                {
                    double lx = l[0] + shift * (1 - l[1] / 52.0), dx = x + 0.5 - lx, dy = y + 0.5 - l[1];
                    double v = 1 - Math.Sqrt(dx * dx + dy * dy) / l[2];
                    if (v > best) best = v;
                }
                if (best < 0) continue;
                double light = -(x - 48 - shift) / 48.0 * 0.6 - (y - 26) / 26.0 * 0.9;   // liso, sem pontilhado (regra do usuário)
                char ch = best < 0.1 ? 'a' : light > 0.8 ? 'f' : light > 0.3 ? 'e' : light > -0.3 ? 'c' : light > -0.8 ? 'b' : 'a';
                c.Force(x, y, ch);
            }
        // franja de folhas embaixo da copa
        for (int x = 18; x < 80; x += 3)
        {
            int b = -1;
            for (int y = 51; y >= 0; y--) if (c.Get(x, y) != '.') { b = y; break; }
            if (b < 0) continue;
            int len = 1 + (int)(N(x + shift, 3, seed) * 4);
            for (int i = 1; i <= len; i++) c.Force(x, b + i, i == len ? 'a' : 'b');
        }
        // (19/11/2026, pedido do usuário: "remova essas manchas brancas"): os casulos claros na copa do Broto saíram; as
        // vespas saem da colmeia pendurada nele (DryBranch.BuildHive)
    }

    /// <summary>O tronco (casca dos troncos do cenário: veio vertical torto, luar à esquerda, raízes abrindo no pé).</summary>
    static void Trunk(PixelCanvas c, int seed)
    {
        for (int y = 28; y < BH; y++)
        {
            int flare = y > BH - 10 ? (y - (BH - 10)) * 2 / 3 : 0;
            for (int dx = -flare; dx < TrunkW + flare; dx++)
            {
                int x = TrunkX + dx;
                int g = ((x + (y / 6) % 2) % 5 + 5) % 5;
                char ch = dx <= -flare ? 'z' : dx >= TrunkW + flare - 1 ? 'w' : g == 0 ? 'w' : g == 2 ? 'y' : 'x';
                if (dx == 0 || dx == 1 && N(x, y, seed) > 0.5) ch = 'z';
                c.Force(x, y, ch);
            }
        }
        // musgo subindo pela casca
        for (int y = 60; y < BH; y++) if (N(TrunkX + 3, y, seed + 1) > 0.4) c.Force(TrunkX + 3 + (y / 4) % 2, y, y % 3 == 0 ? 'c' : 'b');
        // um galho pequeno do outro lado (direita), subindo para a copa
        foreach (var p in PixelKit.Line(TrunkX + TrunkW - 1, 52, TrunkX + TrunkW + 12, 36)) { c.Force(p.X, p.Y, 'x'); c.Force(p.X, p.Y + 1, 'w'); }
    }

    /// <summary>Os nós da casca, à altura dos olhos: 0 fechados (nó), 1 um olho em fresta, 2 os dois acesos.</summary>
    static void Eyes(PixelCanvas c, int state)
    {
        int ey = 56;
        foreach (int ex in new[] { TrunkX + 2, TrunkX + 7 })
        {
            Rect(c, ex, ey, ex + 2, ey + 1, 'w');
            if (state == 2 || (state == 1 && ex == TrunkX + 2))
            {
                c.Force(ex, ey, state == 2 ? 'R' : 'r'); c.Force(ex + 1, ey, 'R'); c.Force(ex + 2, ey, state == 2 ? 'r' : 'w');
                if (state == 2) { c.Force(ex + 1, ey + 1, 'r'); c.Force(ex, ey - 1, 'S'); }
            }
        }
        if (state == 2) for (int x = TrunkX + 3; x < TrunkX + 9; x++) c.Force(x, ey + 5, x % 2 == 0 ? 'w' : 'k');   // a boca: uma fenda
    }

    /// <summary>O galho-braço do ombro até (tx, ty): grosso perto do tronco, afinando, com gravetos-dedos na ponta.</summary>
    static void Arm(PixelCanvas c, int tx, int ty, int bend)
    {
        int sx = TrunkX + 1, sy = Shoulder;
        int mx = (sx + tx) / 2, my = (sy + ty) / 2 + bend;
        var pts = new List<Point>(PixelKit.Line(sx, sy, mx, my));
        pts.AddRange(PixelKit.Line(mx, my, tx, ty));
        for (int i = 0; i < pts.Count; i++)
        {
            double t = i / (double)pts.Count;
            int w = t < 0.3 ? 4 : t < 0.65 ? 3 : 2;
            for (int o = 0; o < w; o++) { c.Force(pts[i].X, pts[i].Y + o, o == 0 ? 'z' : o == w - 1 ? 'w' : 'y'); }
        }
        // gravetos na ponta
        c.Force(tx - 1, ty - 1, 'y'); c.Force(tx - 2, ty - 2, 'x'); c.Force(tx - 1, ty + 1, 'y'); c.Force(tx - 2, ty + 2, 'x');
        c.Force(tx - 1, ty, 'x'); c.Force(tx - 2, ty, 'w');
        // folhinhas no meio do braço (é um galho da árvore)
        c.Force(mx, my - 2, 'c'); c.Force(mx + 1, my - 2, 'e'); c.Force(mx - 6, my - 1, 'b');
    }

    static PixelCanvas Tree(bool sprout, int crownShift, int eyes, int armTx, int armTy, int bend, int seed)
    {
        BranchPalette();
        var c = new PixelCanvas(BW, BH);
        Trunk(c, seed);
        bool awake = eyes == 2;
        if (!awake) Arm(c, armTx, armTy, bend);                 // disfarçado: o braço é só mais um galho, dentro da copa
        Crown(c, crownShift, sprout, seed);
        if (awake) Arm(c, armTx, armTy, bend);                  // acordado: o braço na frente, bem visível
        Eyes(c, eyes);
        Outline(c, 'k');
        return c;
    }

    static void Branches()
    {
        foreach (bool sprout in new[] { false, true })
        {
            string k = sprout ? "broto" : "galho_alto";
            int seed = sprout ? 61 : 41;
            // disfarçado: o braço sobe como um galho comum para dentro da copa
            Write(k + "_disfarce", Tree(sprout, 0, 0, 26, 34, 0, seed));
            Write(k + "_disfarce_1", Tree(sprout, 1, 0, 25, 34, 0, seed));
            Write(k + "_disfarce_2", Tree(sprout, 0, 1, 26, 34, 0, seed));
            // acordado: o braço de lado, os olhos acesos
            Write(k, Tree(sprout, 0, 2, 16, 54, -3, seed));
            Write(k + "_1", Tree(sprout, -1, 2, 16, 56, -2, seed));
            // aviso: o braço lá no alto, tremendo
            Write(k + "_braco", Tree(sprout, 1, 2, 10, 30, -8, seed));
            Write(k + "_braco_1", Tree(sprout, -1, 2, 11, 28, -8, seed));
            // preso: o braço batido no chão à frente, com lascas
            var stuck = Tree(sprout, 0, 2, 6, 102, 2, seed);
            foreach (var p in new[] { new[] { 3, 99 }, new[] { 9, 98 }, new[] { 12, 100 }, new[] { 1, 101 } }) stuck.Force(p[0], p[1], 'z');
            Write(k + "_preso", stuck);
        }
    }

    // =========================================================== Planta Carnívora

    static void PlantPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#120a0e");
        PixelKit.P('m', "#4a1622"); PixelKit.P('M', "#7a2636"); PixelKit.P('N', "#a43c48");          // o bulbo vinho
        PixelKit.P('s', "#d8c8a8"); PixelKit.P('S', "#f0e6cc");                                       // pintas claras
        PixelKit.P('l', "#c89a8a"); PixelKit.P('L', "#e8c4b4");                                       // os lábios
        PixelKit.P('t', "#e8e0c8");                                                                    // espinhos-dentes
        PixelKit.P('q', "#1a080e"); PixelKit.P('u', "#9a3a4a");                                       // dentro da boca e a língua
        PixelKit.P('g', "#22382e"); PixelKit.P('G', "#3c6440"); PixelKit.P('H', "#64905a");          // folhas
        PixelKit.P('T', "#5fd3c7"); PixelKit.P('Y', "#b9f5ec");                                       // o turquesa do pogo
    }

    /// <summary>A cabeça com a boca para cima. <paramref name="open"/> 0 fechada, 1 aberta, 2 bem aberta; <paramref name="squash"/> 0/1.</summary>
    static PixelCanvas PlantHead(int open, int squash, bool tongue, bool droop)
    {
        PlantPalette();
        var c = new PixelCanvas(24, 24);
        double cy = 13 + squash * 0.5 + (droop ? 1 : 0), ry = 7.5 - squash * 0.5 - (droop ? 0.8 : 0), rx = 8.5 + squash * 0.5;
        // as duas folhas-orelha, embaixo
        foreach (int sgn in new[] { -1, 1 })
            for (int i = 0; i < 7; i++)
            {
                int x = 12 + sgn * (5 + i), y = 20 - i / 2 + (droop ? 1 : 0);
                c.Force(x, y, i < 5 ? 'G' : 'g'); c.Force(x, y + 1, 'g'); if (i < 4) c.Force(x, y - 1, 'H');
            }
        Rect(c, 10, 21, 13, 23, 'G'); Rect(c, 10, 21, 10, 23, 'H');
        // o bulbo
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                double dx = (x + 0.5 - 12) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy > 1) continue;
                if (open > 0 && y < cy && Math.Abs(x + 0.5 - 12) < (cy - y) * (open == 2 ? 1.1 : 0.6)) continue;  // a boca abre em V
                double light = -dx * 0.5 - dy * 0.8;
                c.Force(x, y, light > 0.55 ? 'N' : light > -0.2 ? 'M' : 'm');
            }
        // pintas claras (o bulbo é dele, não da planta de cano: pintas pequenas em fileira, quase brilhando)
        foreach (var p in new[] { new[] { 7, 12 }, new[] { 16, 11 }, new[] { 10, 16 }, new[] { 15, 16 }, new[] { 5, 15 }, new[] { 18, 15 } })
        {
            if (c.Get(p[0], p[1] + (int)squash) == '.') continue;
            c.Force(p[0], p[1] + squash, 's'); c.Force(p[0] + 1, p[1] + squash, 'S');
        }
        if (open == 0)
        {
            // fechada: os lábios se encontram numa costura em cima, com os espinhos cruzados
            int top = (int)(cy - ry) + 1;
            for (int x = 5; x <= 18; x++) { int y = top + Math.Abs(x - 12) / 2; c.Force(x, y, 'l'); c.Force(x, y + 1, x % 2 == 0 ? 't' : 'l'); }
            c.Force(12, top - 1, 'L');
        }
        else
        {
            // aberta: dentro escuro, lábios claros nas bordas do V, espinhos virados para dentro, a língua
            for (int y = 0; y < (int)cy; y++)
            {
                double half = (cy - y) * (open == 2 ? 1.1 : 0.6);
                int xl = (int)Math.Round(12 - half), xr = (int)Math.Round(12 + half) - 1;
                double dl = (xl - 1 + 0.5 - 12) / rx, dyy = (y + 0.5 - cy) / ry;
                if (dl * dl + dyy * dyy > 1.05) continue;
                for (int x = xl; x <= xr; x++) c.Force(x, y, 'q');
                c.Force(xl - 1, y, 'L'); c.Force(xr + 1, y, 'l');
                if (y % 2 == 0) { c.Force(xl, y, 't'); c.Force(xr, y, 't'); }
            }
            if (tongue) { Rect(c, 11, (int)cy - 3, 12, (int)cy - 1, 'u'); c.Force(11, (int)cy - 4, 'u'); }
        }
        if (droop)
        {
            // murcha, voltando: a flor turquesa no alto é o único lugar de pogo
            int top = (int)(cy - ry);
            Disc(c, 12, top - 1, 2.6, 'T'); c.Force(11, top - 2, 'Y'); c.Force(12, top - 3, 'Y'); c.Force(13, top - 1, 'Y');
        }
        Outline(c, 'k');
        return c;
    }

    static void Plant()
    {
        Write("planta_cabeca", PlantHead(0, 0, false, false));
        Write("planta_cabeca_1", PlantHead(0, 1, false, false));
        Write("planta_cabeca_aberta", PlantHead(2, 0, true, false));
        Write("planta_cabeca_aberta_1", PlantHead(1, 0, false, false));
        Write("planta_cabeca_recuo", PlantHead(0, 1, false, true));
        PlantPalette();
        var neck = new PixelCanvas(6, 6);
        for (int y = 0; y < 6; y++) { neck.Force(1, y, 'g'); neck.Force(2, y, 'H'); neck.Force(3, y, 'G'); neck.Force(4, y, 'g'); }
        neck.Force(0, 2, 'G'); neck.Force(5, 4, 'G'); Rect(neck, 1, 3, 4, 3, 'g');                   // o nó e duas folhinhas
        Write("planta_pescoco", neck);
        var b = new PixelCanvas(24, 8);
        foreach (int sgn in new[] { -1, 1 })
            for (int i = 0; i < 11; i++)
            {
                int x = 12 + sgn * (1 + i), y = 5 - (int)(Math.Sin(i / 10.0 * Math.PI) * 3);
                b.Force(x, y, 'H'); b.Force(x, y + 1, 'G'); if (i < 9) b.Force(x, y + 2, 'g');
            }
        Rect(b, 10, 2, 13, 7, 'G'); Rect(b, 10, 2, 10, 7, 'H');
        Outline(b, 'k');
        Write("planta_base", b);
    }

    // =========================================================== Serpente

    static void SnakePalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#0e0c10");
        PixelKit.P('o', "#3e3e24"); PixelKit.P('O', "#5e5a32"); PixelKit.P('P', "#847c48");          // escamas oliva (camuflagem)
        PixelKit.P('d', "#221e12");                                                                    // losangos escuros
        PixelKit.P('v', "#a8986a");                                                                    // ventre
        PixelKit.P('e', "#e08a3a"); PixelKit.P('E', "#ffc08a");                                       // olhos laranja
        PixelKit.P('q', "#b04a50"); PixelKit.P('w', "#e8e0c8"); PixelKit.P('m', "#2a0e12");          // língua, presas, boca
        // o chão (o mesmo topo de musgo e terra do ForestSpriteGen): a toca se camufla nele
        PixelKit.P('a', "#16241f"); PixelKit.P('b', "#22382e"); PixelKit.P('c', "#34503f"); PixelKit.P('f', "#5e7e6a");
        PixelKit.P('n', "#231a18"); PixelKit.P('p', "#33261f"); PixelKit.P('l', "#4a3424");
    }

    /// <summary>Faixa do corpo de (x0) a (x1) seguindo y = mid(x), 5 px de altura: luar em cima, ventre embaixo, losangos.</summary>
    static void BodyBand(PixelCanvas c, int x0, int x1, Func<int, double> mid, double taperFrom)
    {
        for (int x = x0; x <= x1; x++)
        {
            int m = (int)Math.Round(mid(x));
            int half = x < taperFrom ? Math.Max(0, (int)((x - x0) / 3.0)) : 2;   // a cauda afina
            half = Math.Min(2, half);
            for (int dy = -half; dy <= half; dy++)
            {
                char ch = dy == -half ? 'P' : dy == half ? 'v' : 'O';
                if (dy == 0 && ((x + 600) % 6 == 0 || (x + 600) % 6 == 5)) ch = 'd';
                if (dy == -1 && (x + 600) % 6 == 2) ch = 'd';
                c.Force(x, m + dy, ch);
            }
        }
    }

    /// <summary>A cabeça olhando para a direita, com o focinho em (x, y). <paramref name="mouth"/>: boca aberta (presas).</summary>
    static void SnakeHead(PixelCanvas c, int x, int y, bool mouth, bool tongue, int eye)
    {
        // cabeça em forma de seta (víbora): larga atrás, afinando no focinho
        for (int i = 0; i < 8; i++)
        {
            int h = i < 2 ? 1 : i < 5 ? 2 : 3;
            int cx = x - i;
            for (int dy = -h; dy <= h; dy++) c.Force(cx, y + dy, dy == -h ? 'P' : dy == h ? 'v' : 'O');
        }
        c.Force(x - 5, y - 2, 'd'); c.Force(x - 6, y - 2, 'd'); c.Force(x - 3, y - 1, 'd');
        // olho
        if (eye == 2) { c.Force(x - 3, y - 1, 'o'); c.Force(x - 4, y - 1, 'o'); }
        else { c.Force(x - 3, y - 1, 'e'); c.Force(x - 4, y - 1, eye == 1 ? 'E' : 'e'); c.Force(x - 3, y - 2, 'k'); }
        if (mouth)
        {
            for (int i = 0; i < 5; i++) c.Force(x - i, y + 1, 'm');
            c.Force(x + 1, y - 1, 'P'); c.Force(x + 1, y + 2, 'v'); c.Force(x, y + 3, 'v');           // a mandíbula abre
            c.Force(x - 1, y + 2, 'w'); c.Force(x - 1, y, 'w');                                      // as presas
        }
        if (tongue) { c.Force(x + 1, y, 'q'); c.Force(x + 2, y, 'q'); c.Force(x + 3, y - 1, 'q'); c.Force(x + 3, y + 1, 'q'); }
    }

    static void Snake()
    {
        SnakePalette();
        // a toca camuflada: um montinho igual ao topo do chão (musgo por cima, terra embaixo), fresta escura no meio
        var hole = new PixelCanvas(32, 8);
        for (int x = 0; x < 32; x++)
        {
            double t = (x - 15.5) / 16.0;
            int h = (int)Math.Round(4 * (1 - t * t));
            for (int y = 7 - h; y < 8; y++) hole.Force(x, y, y == 7 - h ? (N(x, 1, 501) > 0.5 ? 'c' : 'f') : y == 8 - h ? 'b' : y < 6 ? 'a' : (N(x, y, 502) > 0.7 ? 'p' : 'n'));
        }
        for (int x = 11; x <= 20; x++) hole.Force(x, 5 + Math.Abs(x - 15) / 3, 'k');               // a fresta
        hole.Force(6, 3, 'l'); hole.Force(24, 2, 'l'); hole.Force(25, 3, 'l');                       // folhas caídas
        Write("serpente_buraco", hole);

        // na toca: só os olhos na fresta (o desenho fica 0,4 tile à frente da toca, 0,3 acima: fresta na linha 10)
        for (int f = 0; f < 3; f++)
        {
            var c = new PixelCanvas(32, 16);
            foreach (int ex in new[] { 8, 11 })
            {
                if (f == 2) { c.Force(ex, 10, 'd'); continue; }
                c.Force(ex, 10, 'e'); c.Force(ex, 9, f == 1 ? 'E' : 'e');
            }
            Write(f == 0 ? "serpente_olhos" : "serpente_olhos_" + f, c);
        }
        // aviso: a cabeça sai da toca e encara; a língua
        for (int f = 0; f < 2; f++)
        {
            var c = new PixelCanvas(32, 16);
            BodyBand(c, 6, 11, x => 13 - (x - 6) * 0.9, -1);
            SnakeHead(c, 19, 6, false, f == 1, 0);
            Outline(c, 'k');
            Write(f == 0 ? "serpente_aviso" : "serpente_aviso_1", c);
        }
        // bote: a boca aberta, o pescoço esticado atrás
        {
            var c = new PixelCanvas(32, 16);
            BodyBand(c, 0, 22, x => 8 + Math.Sin(x * 0.35) * 1.2, -1);
            SnakeHead(c, 30, 7, true, false, 1);
            Outline(c, 'k');
            Write("serpente_bote", c);
        }
        // rastejando: o corpo inteiro em S, a fase da onda andando
        for (int f = 0; f < 2; f++)
        {
            var c = new PixelCanvas(32, 16);
            int ph = f;
            BodyBand(c, 1, 23, x => 10 + Math.Sin(x * 0.4 + ph * 1.6) * 2, 7);
            SnakeHead(c, 30, (int)Math.Round(10 + Math.Sin(24 * 0.4 + ph * 1.6) * 2), false, f == 1, 0);
            Outline(c, 'k');
            Write(f == 0 ? "serpente" : "serpente_1", c);
        }
        // gomo do corpo esticado (emenda dos lados: losangos a cada 8 px; mesma altura do gomo de antes)
        {
            var c = new PixelCanvas(16, 16);
            BodyBand(c, 0, 15, x => 7, -1);
            for (int x = 0; x < 16; x++) { c.Force(x, 4, 'k'); c.Force(x, 10, 'k'); }
            Write("serpente_gomo", c);
        }
    }

    // =========================================================== prévia

    static void Preview(string path)
    {
        const int S = 3;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var sheet = new Bitmap(640 * S, 560 * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            int x = 4, y = 4, rowH = 0;
            foreach (var n in s_written)
            {
                string p = Path.Combine(s_dir, n + ".png");
                using (var tmp = new Bitmap(p))
                {
                    if (x + tmp.Width > 636) { x = 4; y += rowH + 6; rowH = 0; }
                    g.DrawImage(tmp, x * S, y * S, tmp.Width * S, tmp.Height * S);
                    x += tmp.Width + 6; rowH = Math.Max(rowH, tmp.Height);
                }
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
