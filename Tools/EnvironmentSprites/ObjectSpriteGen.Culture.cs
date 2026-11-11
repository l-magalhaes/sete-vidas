// Etapa 5B do plano de cenário: cultura da Ordem, posto de comércio estrangeiro, marcas da Guilda das Nove Sombras,
// vestígios Primatas, magia (runas, selos, correntes), veias da praga, as sete portas (selos em 7 estados) e a
// arquitetura vinda da Etapa 3 (seteiras, arcos, portão, vigas, colunas, escada de fundo), mais o sino e o pombal.
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public static partial class ObjectSpriteGen
{
    static void Palette5B()
    {
        Palette();
        PixelKit.P('Z', "#6e3a3a"); PixelKit.P('z', "#4a2a2e"); PixelKit.P('K', "#9a8250");      // carmesim e ouro desbotados
        PixelKit.P('T', "#e8e0c8"); PixelKit.P('t', "#b8b0a0");                                   // cera de vela
        PixelKit.P('1', "#7fd8e0"); PixelKit.P('2', "#c8f8ff"); PixelKit.P('3', "#3a4a5a");      // cristal Primata aceso / apagado
        PixelKit.P('4', "#2a3440"); PixelKit.P('5', "#4a6a7a");
        PixelKit.P('6', "#2a2a30"); PixelKit.P('7', "#3a3a44"); PixelKit.P('8', "#5a5a68");      // pedra negra Primata
        PixelKit.P('9', "#3a5a7a"); PixelKit.P('0', "#2a4058");                                   // lona azul estrangeira
        PixelKit.P('!', "#d0d0dc");                                                               // giz da Guilda
        PixelKit.P('Q', "#5b2a6e"); PixelKit.P('q', "#8fd14f");                                   // praga (reservadas)
        PixelKit.P('J', "#c07a4a"); PixelKit.P('o', "#9a5a3a");                                   // barro dos potes
    }

    static void Run5B(string env)
    {
        string cul = Path.Combine(env, "Citadel/Culture"), mag = Path.Combine(env, "Citadel/Magic");
        string str = Path.Combine(env, "Citadel/Structures"), pri = Path.Combine(env, "Primates/Objects");
        string plague = Path.Combine(env, "Necro/Plague"), guild = Path.Combine(env, "Common/Guild");

        // cultura da Ordem
        Write(cul, "env_cid_cul_estandarte_desbotado", FadedBanner());
        for (int v = 0; v < 3; v++) Write(cul, "env_cid_cul_escudo_nome_" + v, NamedShield(v));
        for (int f = 0; f < 3; f++) Write(cul, "env_cid_cul_vela_" + f, Candles(f, false));
        for (int f = 0; f < 3; f++) Write(cul, "env_cid_cul_vela_apagada_" + f, Candles(f, true));
        Write(cul, "env_cid_cul_tapete", Carpet());
        Write(cul, "env_cid_cul_placa_juramento", OathPlaque());
        Write(cul, "env_cid_cul_emblema", Emblem());
        Write(cul, "env_cid_cul_sino", Bell());
        Write(cul, "env_cid_cul_pombal", Dovecote());
        Write(cul, "env_cid_cul_penas", Feathers());
        // estrangeiros e ladrões
        Write(s_ext, "env_ext_obj_barraca_comercio", TradeStall());
        for (int v = 0; v < 3; v++) Write(guild, "env_com_cul_marca_guilda_" + v, GuildMark(v));
        Write(guild, "env_com_obj_corda_cortada", CutRope());
        // Primatas
        Write(pri, "env_pri_obj_cristal_apagado", Crystal(-1));
        for (int f = 0; f < 3; f++) Write(pri, "env_pri_obj_cristal_aceso_" + f, Crystal(f));
        Write(pri, "env_pri_obj_marco", PrimateMarker());
        for (int v = 0; v < 3; v++) Write(pri, "env_pri_obj_reliquia_" + v, Relic(v));
        // magia
        for (int v = 0; v < 3; v++) Write(mag, "env_mag_runa_" + v, Rune(v));
        Write(mag, "env_mag_selo_cera_0", WaxSeal(false));
        Write(mag, "env_mag_selo_cera_1", WaxSeal(true));
        Write(mag, "env_mag_corrente", Chain(48));
        for (int s = 1; s <= 7; s++) Write(mag, "env_mag_selo_porta_" + (s - 1), DoorSeal(s));   // variação = porta - 1
        // praga
        for (int i = 1; i <= 3; i++) Write(plague, "env_nec_veia_" + (i - 1), Veins(i));        // variação = intensidade - 1
        Write(plague, "env_nec_fissura_vitral", GlassCrack());
        // arquitetura (Etapa 3 → 5)
        Write(str, "env_cid_arq_seteira", ArrowSlit());
        Write(str, "env_cid_arq_arco_passagem", Archway());
        Write(str, "env_cid_arq_portao_levantado", Portcullis());
        Write(str, "env_cid_arq_viga", Beam());
        Write(str, "env_cid_arq_coluna", Column(160));
        Write(str, "env_cid_arq_escada_fundo", BgStairs());
    }

    // ------------------------------------------------------------ cultura

    static PixelCanvas FadedBanner()
    {
        Palette5B();
        // estandarte velho: carmesim e ouro desbotados, um furo e a barra rasgada
        var cv = Blank(18, 42);
        Fill(cv, 0, 1, 17, 2, 'w'); cv.Force(0, 0, 'W'); cv.Force(17, 0, 'W');
        for (int y = 3; y < 40; y++)
            for (int x = 2; x < 16; x++)
            {
                bool torn = y > 33 && ((x * 7 + y * 3) % 5 == 0 || (y > 36 && x % 3 != 1));
                if (torn) continue;
                cv.Force(x, y, x == 2 || x == 15 ? 'K' : (x + y) % 9 == 0 ? 'z' : 'Z');
            }
        // pata desbotada
        cv.Force(7, 14, 'K'); cv.Force(9, 13, 'K'); cv.Force(11, 14, 'K'); Fill(cv, 7, 16, 11, 18, 'K'); cv.Force(7, 18, 'Z'); cv.Force(11, 18, 'Z');
        cv.Force(10, 24, '.'); cv.Force(11, 24, '.'); cv.Force(10, 25, '.');   // furo
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas NamedShield(int v)
    {
        Palette5B();
        char red = v == 1 ? 'Z' : 'C', dark = v == 1 ? 'z' : 'c', gold = v == 1 ? 'K' : 'G';
        var cv = Blank(18, 24);
        // escudo
        for (int y = 1; y < 16; y++)
        {
            int half = y < 10 ? 7 : 7 - (y - 9);
            for (int x = 9 - half; x <= 8 + half; x++)
                cv.Force(x, y, x == 9 - half || x == 8 + half || y == 1 ? gold : (x < 9 ? red : dark));
        }
        cv.Force(7, 5, gold); cv.Force(10, 5, gold); cv.Force(8, 4, gold); cv.Force(9, 4, gold); Fill(cv, 7, 7, 10, 9, gold);
        if (v == 2) Line(cv, 12, 2, 7, 13, 'k');                              // escudo rachado
        // plaquinha do nome embaixo (letras em tinta)
        Fill(cv, 3, 18, 14, 22, 'P'); Fill(cv, 3, 22, 14, 22, 'p');
        for (int x = 4; x < 14; x++) if ((x * 5 + v) % 3 != 0) cv.Force(x, 20, 'k');
        cv.Force(8, 17, 'i');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Candles(int f, bool out_)
    {
        Palette5B();
        // castiçal de chão com 3 velas; apagadas = a primeira vela da praga (fumacinha de 1 px)
        var cv = Blank(14, 22);
        Fill(cv, 6, 12, 7, 20, 'n'); Fill(cv, 3, 21, 10, 21, 'N'); Fill(cv, 4, 20, 9, 20, 'n');
        Fill(cv, 1, 11, 12, 11, 'n'); cv.Force(1, 10, 'N'); cv.Force(12, 10, 'N');
        int[] cx = { 2, 6, 11 }; int[] h = { 4, 6, 4 };
        for (int k = 0; k < 3; k++)
        {
            int x = cx[k], top = 11 - h[k];
            Fill(cv, x, top, x + (k == 1 ? 1 : 0), 10, 'T'); cv.Force(x, 10, 't');
            if (!out_)
            {
                int sway = (f + k) % 3 == 0 ? 0 : ((f + k) % 3 == 1 ? 1 : -1);
                cv.Force(x + Math.Max(0, sway), top - 1, 'O'); cv.Force(x, top - 2, (f + k) % 2 == 0 ? 'F' : 'O'); cv.Force(x, top - 1, 'R');
            }
            else
            {
                cv.Force(x, top - 1, 'k');
                cv.Force(x + ((f + k) % 2), top - 3 - f % 2, 's');
            }
        }
        OutlineNoBottom(cv, out_ ? "s" : "FOR");
        return cv;
    }

    static PixelCanvas Carpet()
    {
        Palette5B();
        // tapete do salão (repete a cada 16 px): carmesim com barra dourada, gasto no meio do caminho
        var cv = Blank(16, 3);
        for (int x = 0; x < 16; x++)
        {
            // carmesim escuro e sem barra dourada: uma linha clara no chão parecia perigo (fogo, laser)
            cv.Force(x, 0, (x % 4 == 1) ? 'Z' : 'C'); cv.Force(x, 1, 'c'); cv.Force(x, 2, 'z');
        }
        return cv;
    }

    static PixelCanvas OathPlaque()
    {
        Palette5B();
        return Map(new[] {
            "HHHHHHHHHHHHHHHHHHHH",
            "HLLLLLLLLLLLLLLLLLLS",
            "HLLLLLLLGLGLLLLLLLLS",
            "HLLLLLLGLLLGLLLLLLLS",
            "HLLLLLLLGGGLLLLLLLLS",
            "HLLLLLLLGGGLLLLLLLLS",
            "HLLLLLLLLLLLLLLLLLLS",
            "HLgGgLggGgLgGgggLLLS",
            "HLLLLLLLLLLLLLLLLLLS",
            "HLggGgLgGggLggGgLLLS",
            "HLLLLLLLLLLLLLLLLLLS",
            "HLgGLggGgLgggLLLLLLS",
            "HLLLLLLLLLLLLLLLLLLS",
            "SSSSSSSSSSSSSSSSSSSS",
        }, true);
    }

    static PixelCanvas Emblem()
    {
        Palette5B();
        var cv = Blank(16, 16);
        PixelKit.Ellipse(cv, 8f, 8f, 7f, 7f, (nx, ny) => nx * nx + ny * ny > 0.72f ? (nx > 0 ? 'H' : 'S') : 'L');
        cv.Force(5, 5, 'G'); cv.Force(7, 4, 'G'); cv.Force(9, 4, 'G'); cv.Force(11, 5, 'G');
        Fill(cv, 6, 7, 10, 10, 'G'); cv.Force(6, 10, 'g'); cv.Force(10, 10, 'g'); cv.Force(10, 7, 'g');
        PixelKit.Outline(cv, 'k');
        return cv;
    }

    static PixelCanvas Bell()
    {
        Palette5B();
        // sino de bronze no cepo, com a corda descendo (apoio "teto": o topo encosta no teto)
        var cv = Blank(26, 64);
        Fill(cv, 2, 0, 23, 2, 'w'); Fill(cv, 2, 2, 23, 2, 'd'); Fill(cv, 11, 3, 14, 5, 'I');
        for (int y = 6; y < 24; y++)
        {
            float t = (y - 6) / 17f;
            int half = (int)(4 + t * t * 7);
            for (int x = 13 - half; x < 13 + half; x++) cv.Force(x, y, x < 13 - half + 2 ? 'N' : x > 13 + half - 4 ? 'N' : (x < 11 ? 'n' : 'G'));
        }
        Fill(cv, 3, 23, 22, 24, 'N');
        cv.Force(12, 25, 'I'); cv.Force(13, 25, 'I'); cv.Force(12, 26, 'i'); cv.Force(13, 26, 'i');   // badalo
        for (int y = 27; y < 63; y++) cv.Force(13 + ((y / 6) % 2), y, (y % 3 == 0) ? 'y' : 'Y');     // corda
        cv.Force(13, 63, 'y'); cv.Force(14, 63, 'Y');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Dovecote()
    {
        Palette5B();
        // pombal de madeira preso na parede, três buracos e dois pombos
        var cv = Map(new[] {
            "...........WW...........",
            "........WWWwwWWW........",
            ".....WWWwwwwwwwwWWW.....",
            "..WWWwwwwwwwwwwwwwwWWW..",
            "dddddddddddddddddddddddd",
            ".wwwwwwwwwwwwwwwwwwwwww.",
            ".wwkkkwwwwwkkkwwwwwkkkw.",
            ".wkkkkkwwwkkkkkwwwkkkkkw",
            ".wkkkkkwwwkkkkkwwwkkkkkw",
            ".wwwwwwwwwwwwwwwwwwwwww.",
            "WWWWWWWWWWWWWWWWWWWWWWWW",
            ".dwwwwwwwwwwwwwwwwwwwwd.",
            ".dwwwwwwwwwwwwwwwwwwwwd.",
            ".dddddddddddddddddddddd.",
        }, true);
        // pombos (cinza com peito claro)
        cv.Stamp(new[] { ".uU", "uPuU", "UuuU" }, 3, 7);
        cv.Stamp(new[] { "Uu.", "UuPu", "UuuU" }, 18, 7);
        cv.Force(5, 7, 'k'); cv.Force(19, 7, 'k');
        cv.Force(8, 12, 'P'); cv.Force(15, 13, 'P');                          // sujeira de pombo
        // em cima de um poste que sai do chão (preso no ar, parecia um baú flutuando)
        var full = Blank(cv.W, cv.H + 30);
        for (int x = 0; x < cv.W; x++) for (int y = 0; y < cv.H; y++) full.Px[x, y] = cv.Px[x, y];
        for (int y = cv.H - 2; y < full.H; y++) { full.Px[12, y] = 'w'; full.Px[13, y] = 'd'; }
        Line(full, 9, full.H - 1, 12, full.H - 4, 'w'); Line(full, 16, full.H - 1, 13, full.H - 4, 'd');
        Line(full, 12, cv.H + 3, 7, cv.H - 2, 'w'); Line(full, 13, cv.H + 3, 18, cv.H - 2, 'd');   // mãos-francesas
        OutlineNoBottom(full);
        return full;
    }

    static PixelCanvas Feathers()
    {
        Palette5B();
        var cv = Blank(12, 4);
        Line(cv, 0, 3, 3, 1, 'P'); cv.Force(1, 3, 'u');
        Line(cv, 6, 3, 9, 2, 'u'); cv.Force(9, 1, 'P');
        cv.Force(11, 3, 'P');
        return cv;
    }

    // ------------------------------------------------------------ estrangeiros

    static PixelCanvas TradeStall()
    {
        Palette5B();
        // posto de comércio de fora dos muros (Répteis e Aves): lona azul listrada, balcão, potes de ervas,
        // balança, placa com várias escritas e uma pena de mensageiro presa no poste. Abandonado: lona caída num canto.
        var cv = Blank(42, 32);
        for (int y = 2; y < 32; y++) { cv.Force(2, y, 'w'); cv.Force(3, y, 'd'); cv.Force(38, y, 'W'); cv.Force(39, y, 'w'); }
        for (int x = 0; x < 42; x++)
        {
            int sag = x > 30 ? (x - 30) / 3 : 0;                               // lona caída à direita
            for (int y = 0; y < 5 + sag; y++) cv.Force(x, y, ((x / 4) % 2 == 0 ? '9' : 'P'));
            cv.Force(x, 5 + sag, ((x / 4) % 2 == 0 ? '0' : 'p'));
            if (x % 4 == 2) cv.Force(x, 6 + sag, ((x / 4) % 2 == 0 ? '0' : 'p'));
        }
        // balcão
        Fill(cv, 1, 21, 40, 22, 'W'); Fill(cv, 1, 23, 40, 23, 'd');
        Fill(cv, 4, 24, 37, 31, 'w'); for (int x = 6; x < 37; x += 5) Fill(cv, x, 24, x, 31, 'd');
        // potes de ervas e a balança
        cv.Stamp(new[] { ".JJ.", "JJJo", "JooJ", ".oo." }, 6, 17);
        cv.Stamp(new[] { ".J.", "JJo", "Joo" }, 11, 18);
        cv.Force(12, 17, 'Y');                                                   // folhinha de erva seca
        Fill(cv, 22, 12, 22, 20, 'N'); Fill(cv, 18, 12, 26, 12, 'n');
        cv.Stamp(new[] { "nNn" }, 17, 15); cv.Stamp(new[] { "nNn" }, 25, 16);
        Line(cv, 18, 12, 18, 14, 'N'); Line(cv, 26, 12, 26, 15, 'N');
        // placa em várias escritas pendurada no poste da esquerda, e a pena
        Fill(cv, 4, 9, 13, 14, 'P'); Fill(cv, 4, 14, 13, 14, 'p');
        cv.Force(5, 10, 'k'); cv.Force(7, 10, 'k'); cv.Force(6, 10, 'k'); cv.Force(9, 10, '9'); cv.Force(10, 10, '9'); cv.Force(12, 10, 'C');
        cv.Force(5, 12, 'g'); cv.Force(6, 12, 'g'); cv.Force(8, 12, 'k'); cv.Force(10, 12, 'k'); cv.Force(11, 12, 'k');
        Line(cv, 36, 8, 34, 14, 'P'); cv.Force(35, 9, 'u'); cv.Force(35, 11, 'u');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas GuildMark(int v)
    {
        Palette5B();
        // marca de giz das Nove Sombras (perto de sombras e passagens): olho, seta com 9 pontos, nove riscos
        var cv = Blank(12, 10);
        if (v == 0)
        {
            Line(cv, 0, 5, 5, 2, '!'); Line(cv, 5, 2, 11, 5, '!'); Line(cv, 0, 5, 5, 8, '!'); Line(cv, 5, 8, 11, 5, '!');
            cv.Force(5, 4, '!'); cv.Force(5, 5, '!'); cv.Force(5, 6, '!');
        }
        else if (v == 1)
        {
            Line(cv, 0, 4, 9, 4, '!'); cv.Force(8, 3, '!'); cv.Force(8, 5, '!'); cv.Force(7, 2, '!'); cv.Force(7, 6, '!');
            for (int k = 0; k < 9; k++) cv.Force(1 + k, 8, k % 2 == 0 ? '!' : '.');
            for (int k = 0; k < 4; k++) cv.Force(2 + k * 2, 9, '!');
        }
        else
        {
            // nove riscos contados em grupos de cinco (4 + traço), como numa parede de cela
            cv = Blank(20, 10);
            for (int g = 0; g < 2; g++)
            {
                int x0 = g * 9;
                for (int k = 0; k < 4; k++) Line(cv, x0 + k * 2, 2, x0 + k * 2, 8, '!');
                if (g == 0) Line(cv, x0, 7, x0 + 7, 3, '!');
            }
            Line(cv, 17, 2, 17, 8, '!');
        }
        return cv;
    }

    static PixelCanvas CutRope()
    {
        Palette5B();
        // gancho enferrujado preso no teto e a corda velha cortada (alguém da Guilda já tentou)
        var cv = Blank(10, 26);
        Fill(cv, 2, 0, 7, 0, 'r'); cv.Force(1, 1, 'r'); cv.Force(8, 1, 'r'); cv.Force(4, 1, 'o'); cv.Force(5, 1, 'o');
        cv.Force(4, 2, 'o'); cv.Force(5, 2, 'o'); cv.Force(4, 3, 'r');
        for (int y = 4; y < 22; y++) cv.Force(4 + ((y / 5) % 2), y, y % 3 == 0 ? 'y' : 'Y');
        cv.Force(3, 22, 'y'); cv.Force(5, 23, 'Y'); cv.Force(6, 22, 'y'); cv.Force(4, 24, 'h');
        OutlineNoBottom(cv);
        return cv;
    }

    // ------------------------------------------------------------ Primatas

    static PixelCanvas Crystal(int f)
    {
        Palette5B();
        // cristal num pé de pedra negra: apagado (f = -1) ou aceso (3 quadros de pulso)
        var cv = Blank(14, 18);
        Fill(cv, 2, 14, 11, 17, '7'); Fill(cv, 2, 14, 11, 14, '8'); cv.Force(4, 16, '6'); cv.Force(9, 15, '6');
        char a = f < 0 ? '3' : '1', b = f < 0 ? '4' : '5', c = f < 0 ? '5' : '2';
        for (int y = 2; y < 14; y++)
        {
            int half = y < 5 ? (y - 1) : (y < 11 ? 3 : 2);
            for (int x = 7 - half; x <= 6 + half; x++) cv.Force(x, y, x < 7 ? a : b);
        }
        Line(cv, 6, 3, 6, 12, c);
        if (f >= 0) { cv.Force(5 + f, 5 + f * 2, '2'); if (f == 1) { cv.Force(1, 6, '1'); cv.Force(12, 9, '1'); } }
        OutlineNoBottom(cv, "1");
        return cv;
    }

    static PixelCanvas PrimateMarker()
    {
        Palette5B();
        // marco da estrada negra: pedra lisa demais para ser natural, com sulcos que já brilharam
        var rows = new List<string>();
        rows.Add("...7777...");
        rows.Add("..788887..");
        for (int y = 0; y < 16; y++)
        {
            string r = ".7" + (y % 5 == 2 ? "3335" : y % 5 == 4 ? "6336" : "6886") + "667.";
            rows.Add(r);
        }
        rows.Add("6777777776");
        rows.Add("6666666666");
        return Map(rows.ToArray(), true);
    }

    static PixelCanvas Relic(int v)
    {
        Palette5B();
        // relíquia confiscada (uma em cada cofre): disco com glifos, máscara, tabuleta
        if (v == 0) return Map(new[] { "..7777..", ".788887.", "78353387", "78833887", "78353387", ".788887.", "..6666.." }, true);
        if (v == 1) return Map(new[] { ".777777.", "78888887", "7k38k387", "78888887", "7883 887".Replace(' ', '8'), ".786687.", "..6666.." }, true);
        return Map(new[] { "7777777", "7838387", "7888887", "7833387", "7888887", "7383837", "6666666" }, true);
    }

    // ------------------------------------------------------------ magia

    static PixelCanvas Rune(int v)
    {
        Palette5B();
        string[][] maps = {
            new[] { "..G..", ".GgG.", "G.g.G", "..g..", ".G.G.", "G...G", "GGGGG" },
            new[] { "GGGGG", "G...G", "..G..", ".GgG.", "..G..", "G...G", "GGGGG" },
            new[] { "G...G", ".G.G.", "..G..", "GgGgG", "..G..", ".G.G.", "G...G" },
        };
        var cv = Blank(9, 11);
        PixelKit.Ellipse(cv, 4.5f, 5.5f, 4.4f, 5.4f, (nx, ny) => 'S');
        cv.Stamp(maps[v], 2, 2);
        return cv;
    }

    static PixelCanvas WaxSeal(bool broken)
    {
        Palette5B();
        // selo de cera num pergaminho (o que prende o Tomo): faixa de papel com o lacre carmesim e a pata dourada.
        // Rompido: o papel rasgado e o lacre partido. Nada de fitas soltas (pareciam perninhas de um bicho).
        var cv = Blank(18, 10);
        Fill(cv, 0, 2, 17, 7, 'P'); Fill(cv, 0, 7, 17, 7, 'p'); for (int x = 2; x < 16; x += 3) cv.Force(x, 4, 'p');
        PixelKit.Ellipse(cv, 9f, 5f, 3.6f, 3.6f, (nx, ny) => nx > 0.3f && ny < 0 ? 'C' : 'c');
        cv.Force(8, 4, 'G'); cv.Force(10, 4, 'G'); cv.Force(9, 5, 'G');
        if (broken)
        {
            for (int y = 2; y < 8; y++) { cv.Force(9 + (y % 2), y, '.'); }
            Line(cv, 14, 2, 17, 5, '.'); Line(cv, 15, 2, 17, 4, '.');
        }
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Chain(int len)
    {
        Palette5B();
        // corrente pendurada do teto (elos alternando de frente e de lado)
        var cv = Blank(6, len);
        Fill(cv, 0, 0, 5, 0, 'I');
        for (int y = 1; y < len; y++)
        {
            int k = (y - 1) % 6;
            if ((y - 1) / 6 % 2 == 0) { if (k == 0 || k == 5) Fill(cv, 2, y, 3, y, 'i'); else { cv.Force(1, y, 'i'); cv.Force(4, y, 'I'); } }
            else Fill(cv, 2, y, 3, y, k == 0 || k == 5 ? 'I' : 'j');
        }
        return cv;
    }

    /// <summary>
    /// Selo das sete portas (1.3 do plano): pedra de fecho de arco com a pata dourada, um estado por porta.
    /// 1 intacto e limpo · 2 intacto · 3 com correntes · 4 fissura roxa fina · 5 veias roxas nas bordas ·
    /// 6 runas falhando (ouro apagado e roxo) · 7 rompido (partido ao meio, um pedaço caído).
    /// </summary>
    static PixelCanvas DoorSeal(int s)
    {
        Palette5B();
        var cv = Blank(18, 20);
        for (int y = 1; y < 19; y++)
        {
            int half = 8 - Math.Max(0, (y - 12) / 2);
            for (int x = 9 - half; x < 9 + half; x++) cv.Force(x, y, y == 1 ? 'H' : x > 9 + half - 3 ? 'H' : x < 9 - half + 2 ? 'S' : 'L');
        }
        char gold = s == 6 ? 'K' : 'G', goldDark = s == 6 ? 'z' : 'g';
        PixelKit.Ellipse(cv, 9f, 9f, 5f, 5f, (nx, ny) => nx * nx + ny * ny > 0.7f ? goldDark : (s == 1 ? 'G' : 'L'));
        cv.Force(6, 6, gold); cv.Force(8, 5, gold); cv.Force(10, 5, gold); cv.Force(12, 6, gold); Fill(cv, 7, 8, 11, 11, gold);
        if (s >= 3 && s <= 3) { Line(cv, 0, 3, 17, 15, 'I'); Line(cv, 0, 15, 17, 3, 'I'); for (int x = 0; x < 18; x += 3) { cv.Force(x, 3 + x * 12 / 17, 'i'); } }
        if (s == 4) { Line(cv, 9, 1, 8, 9, 'Q'); Line(cv, 8, 9, 10, 15, 'Q'); }
        if (s == 5) { Line(cv, 1, 18, 4, 12, 'Q'); Line(cv, 4, 12, 3, 7, 'Q'); Line(cv, 16, 18, 14, 11, 'Q'); Line(cv, 14, 11, 15, 5, 'Q'); cv.Force(4, 12, 'q'); }
        if (s == 6) { Line(cv, 2, 18, 6, 10, 'Q'); Line(cv, 6, 10, 9, 9, 'Q'); Line(cv, 15, 18, 12, 11, 'Q'); cv.Force(9, 9, 'q'); cv.Force(6, 10, 'q'); }
        if (s == 7)
        {
            // rompido: uma rachadura escura atravessa, o pedaço de cima à direita caiu
            Line(cv, 3, 18, 9, 9, 'k'); Line(cv, 9, 9, 16, 4, 'k');
            for (int x = 10; x < 18; x++) for (int y = 0; y < 9; y++) if (y < 9 - (x - 10) / 2) cv.Force(x, y, '.');
            Line(cv, 5, 15, 9, 9, 'Q'); cv.Force(9, 9, 'q');
        }
        OutlineNoBottom(cv);
        return cv;
    }

    // ------------------------------------------------------------ praga

    /// <summary>Veias roxas na pedra (16×16, por cima da parede): 1 finas, 2 com galhos, 3 grossas com pontos verdes.</summary>
    static PixelCanvas Veins(int level)
    {
        Palette5B();
        var cv = Blank(16, 16);
        Line(cv, 2, 15, 6, 8, 'Q'); Line(cv, 6, 8, 5, 0, 'Q');
        if (level >= 2) { Line(cv, 6, 8, 12, 5, 'Q'); Line(cv, 12, 5, 15, 7, 'Q'); Line(cv, 4, 11, 0, 9, 'Q'); }
        if (level >= 3)
        {
            Line(cv, 3, 15, 7, 8, 'Q'); Line(cv, 7, 8, 6, 0, 'Q'); Line(cv, 12, 5, 13, 0, 'Q'); Line(cv, 9, 15, 10, 9, 'Q'); Line(cv, 10, 9, 12, 5, 'Q');
            cv.Force(6, 8, 'q'); cv.Force(12, 5, 'q'); cv.Force(10, 10, 'q');
        }
        return cv;
    }

    static PixelCanvas GlassCrack()
    {
        Palette5B();
        // fissura roxa fina por cima do vitral do salão (primeiro sinal da praga)
        var cv = Blank(10, 26);
        Line(cv, 6, 0, 4, 9, 'Q'); Line(cv, 4, 9, 6, 16, 'Q'); Line(cv, 6, 16, 3, 25, 'Q'); Line(cv, 4, 9, 1, 12, 'Q'); Line(cv, 6, 16, 9, 19, 'Q');
        return cv;
    }

    // ------------------------------------------------------------ arquitetura

    static PixelCanvas ArrowSlit()
    {
        Palette5B();
        // seteira em cruz na pedra (atrás de cada lançador de flechas: explica de onde vêm as flechas)
        return Map(new[] {
            "HLLLLLS",
            "LLDkDLS",
            "LLkkkLS",
            "LLkkkLS",
            "LDkkkDS",
            "kkkkkkk",
            "LDkkkDS",
            "LLkkkLS",
            "LLkkkLS",
            "LLkkkLS",
            "LLkkkLS",
            "LLDkDLS",
            "SSSSSSS",
        }, true);
    }

    static PixelCanvas Archway()
    {
        Palette5B();
        // arco de passagem: dois pilares e o arco por cima de uma abertura de 3 tiles; o meio fica vazio
        int W = 64, H = 72;
        var cv = Blank(W, H);
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                float dx = x + 0.5f - 32f, dy = y + 0.5f - 24f;
                bool outer = y >= 24 ? Math.Abs(dx) < 32f : dx * dx + dy * dy < 32f * 32f;
                bool inner = y >= 24 ? Math.Abs(dx) < 24f : dx * dx + dy * dy < 24f * 24f;
                if (!outer || inner) continue;
                bool joint = (y % 8 == 0) || (y < 24 && ((int)(Math.Atan2(dy, dx) * 6) % 2 == 0) && ((x + y) % 7 == 0));
                cv.Force(x, y, joint ? 'S' : dx > 26 ? 'H' : dx < -26 ? 'D' : 'L');
            }
        // pedra de fecho (onde vai o selo das portas)
        Fill(cv, 28, 0, 35, 8, 'H'); Fill(cv, 29, 1, 34, 7, 'L');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Portcullis()
    {
        Palette5B();
        // grade levantada (apoio "teto"): barras de ferro e a barra de baixo reta (sem pontas, para não parecer perigo)
        var cv = Blank(48, 16);
        for (int x = 1; x < 48; x += 5) Fill(cv, x, 0, x + 1, 13, 'I');
        for (int y = 2; y < 13; y += 4) Fill(cv, 0, y, 47, y, 'i');
        Fill(cv, 0, 13, 47, 15, 'I'); Fill(cv, 0, 13, 47, 13, 'i');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Beam()
    {
        Palette5B();
        // ponta de viga do teto vista de frente, com as mãos-francesas
        return Map(new[] {
            "WWWWWWWWWWWW",
            "WwwwwwwwwwwW",
            "WwddwwwwddwW",
            "WwwwwwwwwwwW",
            "dddddddddddd",
            "dw........wd",
            ".dw......wd.",
            "..dw....wd..",
            "...d....d...",
        }, true);
    }

    static PixelCanvas Column(int h)
    {
        Palette5B();
        // coluna do Salão dos Votos (base, fuste canelado e capitel), do chão ao teto
        var cv = Blank(22, h);
        for (int y = 0; y < h; y++)
        {
            bool cap = y < 10, bas = y >= h - 10;
            int x0 = cap || bas ? 1 : 4, x1 = cap || bas ? 20 : 17;
            for (int x = x0; x <= x1; x++)
            {
                char c;
                if (cap) c = y < 3 ? 'H' : y < 6 ? 'L' : (x == x0 || x == x1 ? 'S' : 'L');
                else if (bas) c = y >= h - 3 ? 'S' : y >= h - 6 ? 'L' : 'H';
                else c = (x - x0) % 4 == 3 ? 'S' : x > x1 - 3 ? 'H' : x < x0 + 2 ? 'D' : 'L';
                cv.Force(x, y, c);
            }
        }
        Fill(cv, 6, 3, 15, 4, 'G');                                            // faixa dourada no capitel
        PixelKit.Outline(cv, 'k');
        for (int x = 0; x < cv.W; x++) if (cv.Px[x, h - 1] == 'k' && cv.Get(x, h - 2) == '.') cv.Px[x, h - 1] = '.';
        return cv;
    }

    static PixelCanvas BgStairs()
    {
        Palette5B();
        // escada que sobe para dentro da parede, escura e sem contorno: fica no fundo (parede_fundo), nunca no plano
        // do jogo, para não parecer degrau
        var cv = Blank(48, 48);
        for (int s = 0; s < 6; s++)
        {
            int x0 = s * 8, top = 48 - (s + 1) * 8;
            for (int x = x0; x < 48; x++) for (int y = top; y < top + 8; y++) cv.Force(x, y, y == top ? 'S' : 'D');
        }
        return cv;
    }
}
