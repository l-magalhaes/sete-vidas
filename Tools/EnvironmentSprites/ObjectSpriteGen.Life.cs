// Etapa 7 do plano de cenário (mundo vivo): partículas de ambiente (poeira, folha, vaga-lume, poeira da praga),
// bichos que fogem (pássaro, rato) e o buraco do rato, velas com a chama inclinada e insetos mortos (corrupção, 6.3).
// Nada usa o vermelho das armadilhas; os bichos são pequenos e escuros (nunca parecem inimigos).
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.IO;

public static partial class ObjectSpriteGen
{
    static void Run7(string env)
    {
        string life = Path.Combine(env, "Common/Life"), nec = Path.Combine(env, "Necro/Plague"), cul = Path.Combine(env, "Citadel/Culture");
        // partículas (sem contorno)
        Write(life, "env_com_fx_poeira_0", Dot("#d0c8e0", 1));
        Write(life, "env_com_fx_poeira_1", Dot("#a8a0c0", 1));
        Write(life, "env_com_fx_folha_0", Leaf(false));
        Write(life, "env_com_fx_folha_1", Leaf(true));
        Write(life, "env_com_fx_vagalume", Dot("#e6f2c8", 1));      // branco-esverdeado, nunca amarelo (não parece moeda)
        Write(nec, "env_nec_fx_poeira", Dot("#8a5aa0", 1));
        // bichos
        Write(life, "env_ext_ani_passaro_0", Bird(0));
        Write(life, "env_ext_ani_passaro_voo_0", Bird(1));
        Write(life, "env_ext_ani_passaro_voo_1", Bird(2));
        Write(life, "env_com_ani_rato_0", Rat(0));
        Write(life, "env_com_ani_rato_corre_0", Rat(1));
        Write(life, "env_com_ani_rato_corre_1", Rat(2));
        Write(life, "env_com_obj_buraco_rato", RatHole());
        // velas: a chama inclinada quando o gato passa
        Write(cul, "env_cid_cul_vela_inclinada", CandlesLeaning());
        // insetos mortos (galeria das armadilhas: a praga já chegou)
        Write(nec, "env_nec_obj_inseto_morto_0", DeadBug(0));
        Write(nec, "env_nec_obj_inseto_morto_1", DeadBug(1));
    }

    static PixelCanvas Dot(string hex, int size)
    {
        PixelKit.Pal.Clear(); PixelKit.P('x', hex);
        var cv = Blank(size, size);
        Fill(cv, 0, 0, size - 1, size - 1, 'x');
        return cv;
    }

    static PixelCanvas Leaf(bool turned)
    {
        PixelKit.Pal.Clear(); PixelKit.P('g', "#3e5a48"); PixelKit.P('l', "#a0582e");
        var cv = Blank(2, 2);
        if (turned) { cv.Force(0, 0, 'l'); cv.Force(1, 1, 'g'); }
        else { cv.Force(0, 1, 'g'); cv.Force(1, 0, 'g'); cv.Force(1, 1, 'l'); }
        return cv;
    }

    /// <summary>Pássaro 9×7 olhando para a direita: pousado (0) e voando com as asas para cima (1) e para baixo (2).</summary>
    static PixelCanvas Bird(int f)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#1c1a24"); PixelKit.P('b', "#3a3049"); PixelKit.P('B', "#4e4460"); PixelKit.P('p', "#8a7a90"); PixelKit.P('y', "#b08a3a");
        string[] map;
        if (f == 0) map = new[] { ".......  ", "....bbB. ", "...bbbBby", "bbbbbBB. ", ".bbppb.. ", "...k.k.. " };
        else if (f == 1) map = new[] { "..b...b..", "...b.b...", "...bbbBby", "bbbbbBB..", ".bbppb...", "........." };
        else map = new[] { ".........", "....bbB..", "...bbbBby", "bbbbbBB..", "..b...b..", ".b.....b." };
        var cv = Map(map, true);
        return cv;
    }

    /// <summary>Rato 10×5 olhando para a direita: parado (0) e correndo (1, 2).</summary>
    static PixelCanvas Rat(int f)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('r', "#4a4448"); PixelKit.P('R', "#5e5862"); PixelKit.P('k', "#1c1a24"); PixelKit.P('n', "#a08a8a");
        string[] map;
        if (f == 0) map = new[] { "......rR.", "..rrrrrRn", "rrrrrrrr.", "...r..r.." };
        else if (f == 1) map = new[] { "......rR.", ".rrrrrrRn", "r.rrrrrr.", "..r....r." };
        else map = new[] { "......rR.", "..rrrrrRn", "rrrrrrrr.", "....rr..." };
        return Map(map, true);
    }

    static PixelCanvas RatHole()
    {
        PixelKit.Pal.Clear(); PixelKit.P('k', "#141018"); PixelKit.P('D', "#3a3549"); PixelKit.P('S', "#4a455c");
        return Map(new[] { "..DDD..", ".DkkkD.", "DkkkkkD", "SkkkkkS" }, false);
    }

    /// <summary>O castiçal do salão com as três chamas deitadas para a direita (espelha para a esquerda).</summary>
    static PixelCanvas CandlesLeaning()
    {
        var cv = Candles(0, false);
        // move cada pixel de chama 1 px para a direita e um a mais na ponta (a chama deita com o vento do gato)
        var copy = cv.Clone();
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                char c = copy.Px[x, y];
                if (c != 'O' && c != 'F' && c != 'R') continue;
                cv.Px[x, y] = '.';
            }
        for (int x = cv.W - 1; x >= 0; x--)
            for (int y = 0; y < cv.H; y++)
            {
                char c = copy.Px[x, y];
                if (c != 'O' && c != 'F' && c != 'R') continue;
                int shift = (c == 'R') ? 1 : 2;
                if (x + shift < cv.W) cv.Px[x + shift, y + (c == 'R' ? 0 : 1)] = c;
            }
        return cv;
    }

    static PixelCanvas DeadBug(int v)
    {
        PixelKit.Pal.Clear(); PixelKit.P('k', "#1c1a24"); PixelKit.P('b', "#3a3040"); PixelKit.P('Q', "#5b2a6e");
        if (v == 0) return Map(new[] { "k.k.k", ".bQb.", "k.k.k" }, false);
        return Map(new[] { ".k.k.", "kbbQk", ".k.k." }, false);
    }
}
