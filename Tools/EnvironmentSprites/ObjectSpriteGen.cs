// Etapa 5A do plano de cenário: objetos do dia a dia por zona (treino, defesa, alojamento, armadilhas, serviço,
// exterior) e os objetos barulhentos com os quadros de reação. Usa o PixelKit (Tools/CharacterSprites).
// Cada objeto tem contorno #1c1a24 em cima e dos lados, nunca embaixo: a base do PNG é o chão (apoio "chao").
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static partial class ObjectSpriteGen
{
    static string s_template, s_sol, s_com, s_cid, s_ext;
    static readonly List<KeyValuePair<string, PixelCanvas>> s_sheet = new List<KeyValuePair<string, PixelCanvas>>();

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        string env = Path.Combine(projectRoot, "Assets/_Game/Art/Environments");
        s_sol = Path.Combine(env, "Soluki/Objects");
        s_com = Path.Combine(env, "Common/Noisy");
        s_cid = Path.Combine(env, "Citadel/Traps");
        s_ext = Path.Combine(env, "Exterior/Objects");
        s_sheet.Clear();

        // treino
        Write(s_sol, "env_sol_obj_banco", Bench());
        Write(s_sol, "env_sol_obj_balde_agua", WaterBucket());
        Write(s_sol, "env_sol_obj_quadro_treino", TrainingBoard());
        Write(s_sol, "env_sol_obj_amolador", Grindstone());
        // defesa
        Write(s_sol, "env_sol_obj_aljava", Quiver());
        Write(s_sol, "env_sol_obj_virotes", BoltCrate());
        Write(s_sol, "env_sol_obj_corda", RopeCoil());
        for (int i = 0; i < 4; i++) Write(s_sol, "env_sol_obj_braseiro_" + i, Brazier(i));
        // alojamento
        Write(s_sol, "env_sol_obj_beliche", BunkBed());
        Write(s_sol, "env_sol_obj_bau", Chest());
        Write(s_sol, "env_sol_obj_mesa_dados", DiceTable());
        for (int i = 0; i < 2; i++) Write(s_sol, "env_sol_obj_varal_" + i, Clothesline(i));
        for (int i = 0; i < 3; i++) Write(s_sol, "env_sol_obj_panela_" + i, Pot(i));
        Write(s_sol, "env_sol_obj_vassoura", Broom());
        Write(s_sol, "env_sol_obj_escala", DutyRoster());
        // serviço
        Write(s_sol, "env_sol_obj_prateleira", Shelf());
        Write(s_sol, "env_sol_obj_barril", Barrel());
        Write(s_sol, "env_sol_obj_caixote", Crate());
        Write(s_sol, "env_sol_obj_sacos", Sacks());
        Write(s_sol, "env_com_obj_teia", Cobweb());
        // portas fechadas: corredores que o gato não usa
        for (int i = 0; i < 2; i++) Write(s_sol, "env_sol_obj_porta_fechada_" + i, Door(i));
        // armadilhas
        Write(s_cid, "env_cid_obj_tubo_oleo", PipeH());
        Write(s_cid, "env_cid_obj_tubo_descida", PipeV());
        Write(s_cid, "env_cid_obj_alavanca", Lever());
        Write(s_cid, "env_cid_obj_engrenagem", Gear());
        Write(s_cid, "env_cid_obj_registro", MaintenanceLog());
        Write(s_cid, "env_cid_obj_elmo_amassado", DentedHelm());
        Write(s_cid, "env_cid_obj_escudo_amassado", DentedShield());
        // exterior
        Write(s_ext, "env_ext_obj_fogueira_apagada", DeadCampfire());
        Write(s_ext, "env_ext_obj_mochila", Backpack());
        // barulhentos: repouso (_0) e reação (_1.._n)
        for (int i = 0; i < 4; i++) Write(s_com, "env_com_obj_cacos_" + i, Glass(i));
        for (int i = 0; i < 4; i++) Write(s_com, "env_com_obj_folhas_secas_" + i, Leaves(i, false));
        for (int i = 0; i < 4; i++) Write(s_com, "env_com_obj_palha_" + i, Leaves(i, true));
        for (int i = 0; i < 5; i++) Write(s_com, "env_com_obj_sininhos_" + i, Chimes(i));
        for (int i = 0; i < 5; i++) Write(s_com, "env_com_obj_balde_" + i, Bucket(i));

        Sheet(Path.Combine(previewDir, "objetos_etapa5.png"), 5);
        s_sheet.Clear();
        Run5B(env);
        Sheet(Path.Combine(previewDir, "objetos_etapa5b.png"), 5);
    }

    // ------------------------------------------------------------------ base

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#1c1a24");
        PixelKit.P('W', "#9a7044"); PixelKit.P('w', "#6b4a2b"); PixelKit.P('d', "#4a321e");      // madeira
        PixelKit.P('i', "#8e8ca4"); PixelKit.P('I', "#5a5868"); PixelKit.P('j', "#b8b6c8");      // ferro
        PixelKit.P('S', "#4a455c"); PixelKit.P('L', "#5e5872"); PixelKit.P('H', "#6e6884"); PixelKit.P('D', "#3a3549"); // pedra
        PixelKit.P('C', "#8f2a2a"); PixelKit.P('c', "#5a161c");                                   // carmesim da Ordem
        PixelKit.P('G', "#d9a441"); PixelKit.P('g', "#8a621e");                                   // dourado
        PixelKit.P('P', "#d8ccae"); PixelKit.P('p', "#a89c80");                                   // linho, papel
        PixelKit.P('Y', "#c8a85a"); PixelKit.P('y', "#a8883e"); PixelKit.P('h', "#e0c878");      // palha
        PixelKit.P('B', "#3a78c0"); PixelKit.P('b', "#5aa0e0"); PixelKit.P('E', "#c8f4ff");      // água
        PixelKit.P('F', "#fff2b0"); PixelKit.P('O', "#ffc86a"); PixelKit.P('R', "#ffa83a"); PixelKit.P('r', "#e0702a"); // fogo
        PixelKit.P('A', "#2a2428"); PixelKit.P('a', "#4a4448");                                   // carvão, cinza
        PixelKit.Pal['s'] = Color.FromArgb(140, 0xc8, 0xc8, 0xd8);                               // vapor
        PixelKit.Pal['e'] = Color.FromArgb(170, 0xb8, 0xb6, 0xc8);                               // teia
        PixelKit.P('x', "#e8f0ff"); PixelKit.P('X', "#a8b8d0");                                   // vidro
        PixelKit.P('l', "#a0582e"); PixelKit.P('m', "#6e3a22");                                   // folha seca
        PixelKit.P('n', "#b08a3a"); PixelKit.P('N', "#7a5e26");                                   // latão
        PixelKit.P('u', "#6a7080"); PixelKit.P('U', "#4a4f5c");                                   // pano cinza
        PixelKit.P('V', "#9a5b36"); PixelKit.P('v', "#6e3c22");                                   // couro
    }

    /// <summary>Mapa de caracteres → canvas com 1 px de folga em cima e dos lados (para o contorno); a base é a última linha.</summary>
    static PixelCanvas Map(string[] rows, bool outline)
    {
        int w = 0; foreach (var r in rows) w = Math.Max(w, r.Length);
        var cv = new PixelCanvas(w + 2, rows.Length + 1);
        cv.Stamp(rows, 1, 1);
        if (outline) OutlineNoBottom(cv);
        return cv;
    }

    static void OutlineNoBottom(PixelCanvas cv) { OutlineNoBottom(cv, ""); }
    static void OutlineNoBottom(PixelCanvas cv, string except)
    {
        // contorno de 1 px, menos na última linha (o objeto encosta no chão sem risco embaixo)
        var keepBottom = new char[cv.W];
        for (int x = 0; x < cv.W; x++) keepBottom[x] = cv.Px[x, cv.H - 1];
        PixelKit.Outline(cv, 'k', except + "sex");
        for (int x = 0; x < cv.W; x++) if (keepBottom[x] == '.' && cv.Px[x, cv.H - 1] == 'k' && cv.Get(x, cv.H - 2) == '.') cv.Px[x, cv.H - 1] = '.';
    }

    static PixelCanvas Blank(int w, int h) { return new PixelCanvas(w, h); }
    static void Fill(PixelCanvas cv, int x0, int y0, int x1, int y1, char c) { for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) cv.Force(x, y, c); }
    static void Line(PixelCanvas cv, int x0, int y0, int x1, int y1, char c) { foreach (var p in PixelKit.Line(x0, y0, x1, y1)) cv.Force(p.X, p.Y, c); }

    static void Write(string dir, string name, PixelCanvas cv)
    {
        PixelKit.WriteSprite(cv, Path.Combine(dir, name + ".png"), name, s_template, false);
        s_sheet.Add(new KeyValuePair<string, PixelCanvas>(name, cv));
    }

    static void Sheet(string path, int S)
    {
        int cols = 8, cell = 46;
        int rows = (s_sheet.Count + cols - 1) / cols;
        using (var bmp = new Bitmap(cols * cell * S, rows * cell * S))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(ColorTranslator.FromHtml("#2a2438"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < s_sheet.Count; i++)
            {
                var cv = s_sheet[i].Value;
                int cx = (i % cols) * cell, cy = (i / cols) * cell;
                using (var one = PixelKit.ToBitmap(cv))
                    g.DrawImage(one, new Rectangle((cx + (cell - cv.W) / 2) * S, (cy + cell - 2 - cv.H) * S, cv.W * S, cv.H * S));
                using (var pen = new Pen(Color.FromArgb(90, 120, 110, 140)))
                    g.DrawLine(pen, cx * S, (cy + cell - 2) * S, (cx + cell) * S, (cy + cell - 2) * S);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // ---------------------------------------------------------------- treino

    static PixelCanvas Bench()
    {
        Palette();
        return Map(new[] {
            "WWWWWWWWWWWWWWWWWW",
            "wwwwwwwwwwwwwwwwww",
            "dddddddddddddddddd",
            ".wd............wd.",
            ".wd............wd.",
            ".wd............wd.",
        }, true);
    }

    static PixelCanvas WaterBucket()
    {
        Palette();
        // balde de água com a esponja na borda (o recruta lava o boneco)
        return Map(new[] {
            "......hY..",
            "WEbbbbBhY.",
            "wBbBBbBBYy",
            "IiiiiiiiiI",
            "wWwWwWwWwW",
            "wWwWwWwWww",
            "IiiiiiiiiI",
            "dwWwWwWwwd",
            ".dddddddd.",
        }, true);
    }

    static PixelCanvas TrainingBoard()
    {
        Palette();
        // quadro de giz da escala de treino num poste: tracinhos dos recrutas e uma pata desenhada
        var cv = Map(new[] {
            "WWWWWWWWWWWWWWWW",
            "wDDDDDDDDDDDDDDw",
            "wDHDHDHDDDDPDDDw",
            "wDHDHDHDDDPPPDDw",
            "wDDDDDDDDDDPDDDw",
            "wDPPPDPPDDDDDDDw",
            "wDDDDDDDDLLLLDDw",
            "wDHDHDDDDDDDDDDw",
            "wwwwwwwwwwwwwwww",
            ".......wd.......",
            ".......wd.......",
            ".......wd.......",
            ".......wd.......",
            ".......wd.......",
            ".......wd.......",
            ".......wd.......",
            "......dwdd......",
        }, true);
        return cv;
    }

    static PixelCanvas Grindstone()
    {
        Palette();
        var cv = Blank(20, 17);
        // cavalete
        Line(cv, 3, 16, 7, 8, 'w'); Line(cv, 4, 16, 8, 8, 'd');
        Line(cv, 15, 16, 11, 8, 'w'); Line(cv, 16, 16, 12, 8, 'd');
        Fill(cv, 2, 12, 17, 12, 'w');
        // rebolo de pedra
        PixelKit.Ellipse(cv, 9.5f, 6.5f, 5.6f, 5.6f, (nx, ny) => nx > 0.35f && ny < 0.2f ? 'H' : nx < -0.5f ? 'D' : (nx * nx + ny * ny < 0.12f ? 'I' : 'L'));
        cv.Force(9, 6, 'j'); cv.Force(10, 6, 'i');
        // manivela
        Line(cv, 10, 6, 15, 3, 'I'); cv.Force(16, 3, 'W'); cv.Force(16, 2, 'W');
        // pedal
        Fill(cv, 6, 15, 12, 15, 'W');
        OutlineNoBottom(cv);
        return cv;
    }

    // ---------------------------------------------------------------- defesa

    static PixelCanvas Quiver()
    {
        Palette();
        return Map(new[] {
            ".P.C.P",
            ".PCPCP",
            "..WWW.",
            "..WWW.",
            ".VVVVv",
            ".VvVVv",
            ".VVVVv",
            ".VnnVv",
            ".VVVVv",
            ".VvVVv",
            ".VVVVv",
            ".VVVVv",
            "..vvv.",
        }, true);
    }

    static PixelCanvas BoltCrate()
    {
        Palette();
        return Map(new[] {
            ".j.j..j.j..j.",
            ".W.W..W.W..W.",
            "WWWWWWWWWWWWW",
            "wdwwwwwwwwwdw",
            "wwdwwwwwwwdww",
            "wwwdwwwwwdwww",
            "wwwwdwwwdwwww",
            "wwwdwwwwwdwww",
            "dddddddddddd.",
        }, true);
    }

    static PixelCanvas RopeCoil()
    {
        Palette();
        return Map(new[] {
            "..YyYyYy..",
            ".yY....Yy.",
            "Yy.YyYy.yY",
            "yY.y..Y.Yy",
            ".YyYyYyYy.",
        }, true);
    }

    static PixelCanvas Brazier(int f)
    {
        Palette();
        var cv = Blank(14, 19);
        // tripé de ferro
        Line(cv, 3, 18, 5, 11, 'I'); Line(cv, 10, 18, 8, 11, 'I'); Line(cv, 7, 18, 7, 11, 'i');
        // bacia
        Fill(cv, 2, 8, 11, 8, 'i'); Fill(cv, 3, 9, 10, 9, 'I'); Fill(cv, 4, 10, 9, 10, 'I');
        // brasas e chama (4 quadros)
        Fill(cv, 3, 7, 10, 7, 'r');
        for (int x = 3; x <= 10; x++) if ((x + f) % 3 == 0) cv.Force(x, 7, 'R');
        int[] h = { 5, 3, 6, 4 };
        for (int k = 0; k < 4; k++)
        {
            int x = 4 + k * 2, top = 7 - h[(k + f) % 4];
            for (int y = top; y < 7; y++) cv.Force(x, y, y == top ? 'O' : y < top + 2 ? 'R' : 'r');
            if (k % 2 == 0) cv.Force(x + 1, Math.Max(top + 1, 2), 'F');
        }
        cv.Force(6 + (f % 2), 1 + (f % 2), 'O');
        OutlineNoBottom(cv, "FORr");
        return cv;
    }

    // ------------------------------------------------------------ alojamento

    static PixelCanvas BunkBed()
    {
        Palette();
        var cv = Blank(30, 27);
        for (int y = 0; y < 27; y++) { cv.Force(1, y, 'w'); cv.Force(2, y, 'd'); cv.Force(27, y, 'W'); cv.Force(28, y, 'w'); }
        // cama de cima: estrado, colchão, travesseiro e o cobertor dobrado
        Fill(cv, 1, 9, 28, 10, 'w'); Fill(cv, 1, 10, 28, 10, 'd');
        Fill(cv, 3, 6, 26, 8, 'P'); Fill(cv, 3, 8, 26, 8, 'p');
        Fill(cv, 4, 4, 8, 6, 'P'); cv.Force(4, 6, 'p');
        Fill(cv, 14, 5, 26, 7, 'u'); Fill(cv, 14, 7, 26, 7, 'U');
        // cama de baixo: cobertor carmesim da Ordem jogado, com a pata dourada
        Fill(cv, 1, 22, 28, 23, 'w'); Fill(cv, 1, 23, 28, 23, 'd');
        Fill(cv, 3, 19, 26, 21, 'P'); Fill(cv, 3, 21, 26, 21, 'p');
        Fill(cv, 4, 17, 8, 19, 'P');
        Fill(cv, 11, 18, 26, 21, 'C'); Fill(cv, 11, 21, 26, 21, 'c'); Line(cv, 11, 18, 14, 21, 'c');
        cv.Force(19, 19, 'G'); cv.Force(21, 19, 'G'); cv.Force(20, 20, 'G'); cv.Force(19, 20, 'g'); cv.Force(21, 20, 'g');
        // escadinha
        for (int y = 11; y <= 21; y += 3) Fill(cv, 23, y, 26, y, 'W');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Chest()
    {
        Palette();
        return Map(new[] {
            ".wWWWWWWWWWWWWw.",
            "wWWWWWWWWWWWWWWw",
            "wwwwwwwwwwwwwwww",
            "IiiiiiiiiiiiiiiI",
            "wwwwwwwnGnwwwwww",
            "wwwwwwwnnnwwwwww",
            "dwwwwwwwwwwwwwwd",
            "dwwwwwwwwwwwwwwd",
            "IiiiiiiiiiiiiiiI",
            "dddddddddddddddd",
        }, true);
    }

    static PixelCanvas DiceTable()
    {
        Palette();
        // a partida de dados largada no meio: caneca, dois dados, cartas e uma moeda de aposta
        return Map(new[] {
            "..nnN.........PpP......",
            "..nNnN..Pk.kP.pPp...g..",
            "WWWWWWWWWWWWWWWWWWWWWWW",
            "wwwwwwwwwwwwwwwwwwwwwww",
            "ddddddddddddddddddddddd",
            ".wd.................wd.",
            ".wd.................wd.",
            ".wd.................wd.",
            ".wd.................wd.",
            ".wd.................wd.",
        }, true);
    }

    static PixelCanvas Clothesline(int f)
    {
        Palette();
        var cv = Blank(42, 23);
        for (int y = 0; y < 23; y++) { cv.Force(1, y, 'w'); cv.Force(2, y, 'd'); cv.Force(39, y, 'W'); cv.Force(40, y, 'w'); }
        Fill(cv, 0, 1, 3, 1, 'w'); Fill(cv, 38, 1, 41, 1, 'w');
        // corda que cede no meio
        Func<int, int> rope = x => 2 + (int)Math.Round(3.0 * Math.Sin(Math.PI * (x - 3) / 35.0));
        for (int x = 3; x <= 38; x++) cv.Force(x, rope(x), 'p');
        int sw = f;  // a barra das roupas balança 1 px
        // camisa de linho
        int t = rope(9) + 1;
        Fill(cv, 6, t, 13, t + 2, 'P'); Fill(cv, 7 + sw, t + 3, 12 + sw, t + 7, 'P'); Fill(cv, 7 + sw, t + 7, 12 + sw, t + 7, 'p');
        cv.Force(9, t, 'p'); cv.Force(10, t, 'p');
        // tabardo carmesim com a pata dourada
        t = rope(19) + 1;
        Fill(cv, 16, t, 22, t + 1, 'C'); Fill(cv, 16 + sw, t + 2, 22 + sw, t + 9, 'C'); Fill(cv, 16 + sw, t + 9, 22 + sw, t + 9, 'c');
        cv.Force(18 + sw, t + 4, 'G'); cv.Force(20 + sw, t + 4, 'G'); cv.Force(19 + sw, t + 5, 'G'); cv.Force(19 + sw, t + 6, 'g');
        // meia e toalha
        t = rope(27) + 1;
        Fill(cv, 26, t, 27, t + 4, 'u'); Fill(cv, 26 + sw, t + 4, 29 + sw, t + 5, 'u'); Fill(cv, 26 + sw, t + 5, 29 + sw, t + 5, 'U');
        t = rope(33) + 1;
        Fill(cv, 31, t, 36, t + 3, 'p'); Fill(cv, 31 + sw, t + 4, 36 + sw, t + 6, 'p'); Fill(cv, 31 + sw, t + 6, 36 + sw, t + 6, 'P');
        // prendedores
        cv.Force(7, rope(7), 'W'); cv.Force(12, rope(12), 'W'); cv.Force(17, rope(17), 'W'); cv.Force(21, rope(21), 'W'); cv.Force(32, rope(32), 'W');
        OutlineNoBottom(cv, "p");
        return cv;
    }

    static PixelCanvas Pot(int f)
    {
        Palette();
        var cv = Blank(14, 19);
        // pedras do fogareiro e o fogo baixo
        Fill(cv, 1, 16, 12, 18, 'S'); Fill(cv, 1, 16, 12, 16, 'L'); cv.Force(4, 17, 'D'); cv.Force(9, 17, 'D');
        for (int x = 3; x <= 10; x++) cv.Force(x, 15, (x + f) % 3 == 0 ? 'O' : 'r');
        cv.Force(5 + f % 2, 14, 'R'); cv.Force(8 - f % 2, 14, 'R');
        // panela de ferro com tampa e cabo
        Fill(cv, 2, 9, 11, 13, 'I'); Fill(cv, 3, 13, 10, 13, 'D'); Fill(cv, 9, 9, 11, 12, 'i');
        Fill(cv, 1, 8, 12, 8, 'i'); Fill(cv, 5, 7, 8, 7, 'I'); cv.Force(6, 6, 'i'); cv.Force(7, 6, 'i');
        cv.Force(0, 10, 'I'); cv.Force(13, 10, 'I');
        // vapor subindo (3 quadros)
        int[,] puffs = { { 4, 4 }, { 8, 3 }, { 6, 1 } };
        for (int p = 0; p < 3; p++)
        {
            int px = puffs[p, 0] + ((p + f) % 2 == 0 ? 0 : 1), py = puffs[p, 1] + (f == 0 ? 1 : 0) - (f == 2 ? 1 : 0);
            cv.Force(px, Math.Max(0, py), 's'); cv.Force(px + 1, Math.Max(0, py), 's');
        }
        OutlineNoBottom(cv, "Rr");
        return cv;
    }

    static PixelCanvas Broom()
    {
        Palette();
        var cv = Blank(9, 21);
        Line(cv, 7, 1, 3, 13, 'W'); Line(cv, 8, 1, 4, 13, 'w');
        for (int y = 13; y <= 20; y++)
        {
            int half = 1 + (y - 13) / 2;
            for (int x = 3 - half + 1; x <= 4 + half; x++) cv.Force(x, y, (x + y) % 3 == 0 ? 'y' : 'Y');
        }
        Fill(cv, 2, 14, 5, 14, 'd');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas DutyRoster()
    {
        Palette();
        // escala da guarda pregada na parede: nomes, horários e o selo carmesim da Ordem
        return Map(new[] {
            ".....i.....",
            "PPPPPPPPPPP",
            "PppPpppPpPP",
            "PPPPPPPPPPP",
            "PpppPppPPpP",
            "PPPPPPPPPPP",
            "PppPPpppPpP",
            "PPPPPPPPPPP",
            "PpPpppPPpPP",
            "PPPPPPPPCCP",
            "pPPPPPPPCcp",
            ".ppppppppp.",
        }, true);
    }

    // ---------------------------------------------------------------- serviço

    static PixelCanvas Shelf()
    {
        Palette();
        var cv = Blank(26, 31);
        for (int y = 0; y < 31; y++) { cv.Force(1, y, 'w'); cv.Force(2, y, 'd'); cv.Force(23, y, 'W'); cv.Force(24, y, 'w'); }
        Fill(cv, 1, 0, 24, 0, 'W');
        int[] shelves = { 10, 20, 30 };
        char[] books = { 'C', 'u', 'V', 'g', 'U', 'c', 'v' };
        int seed = 0;
        foreach (int sy in shelves)
        {
            Fill(cv, 1, sy, 24, sy, 'w');
            int x = 3;
            while (x < 23)
            {
                seed++;
                int kind = (seed * 7 + sy) % 5;
                if (kind == 0 && x < 20)
                {
                    // rolos de pergaminho deitados
                    for (int k = 0; k < 2; k++) { Fill(cv, x, sy - 2 - k * 2, x + 3, sy - 1 - k * 2, 'P'); cv.Force(x, sy - 2 - k * 2, 'p'); }
                    x += 5;
                }
                else if (kind == 1 && x < 19)
                {
                    // caixa de arquivo
                    Fill(cv, x, sy - 5, x + 4, sy - 1, 'w'); Fill(cv, x + 1, sy - 4, x + 3, sy - 4, 'P'); x += 6;
                }
                else
                {
                    int h = 6 + (seed * 3 + sy) % 3;
                    char c = books[(seed * 5 + sy) % books.Length];
                    Fill(cv, x, sy - h, x + 1, sy - 1, c); cv.Force(x, sy - h + 2, 'G');
                    x += 2;
                }
            }
        }
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Barrel()
    {
        Palette();
        return Map(new[] {
            "..wwwwwwww..",
            ".wdWWWWWWdw.",
            "IiiiiiiiiiiI",
            "wWWwWWWwWWWw",
            "wWWwWWWwWWdw",
            "wWWwWWWwWWdw",
            "IiiiiiiiiiiI",
            "wWWwWWWwWWdw",
            "wWWwWWWwWWdw",
            "wWWwWWWwWWdw",
            "IiiiiiiiiiiI",
            ".wwwwwwwwww.",
        }, true);
    }

    static PixelCanvas Crate()
    {
        Palette();
        return Map(new[] {
            "WWWWWWWWWWWW",
            "WdwwwwwwwwdW",
            "wwdwwwwwwdww",
            "wwwdwwwwdwww",
            "wwwwdwwdwwww",
            "wwwwwddwwwww",
            "wwwwdwwdwwww",
            "wwwdwwwwdwww",
            "wwdwwwwwwdww",
            "ddddddddddddd",
        }, true);
    }

    static PixelCanvas Sacks()
    {
        Palette();
        var cv = Blank(18, 12);
        PixelKit.Ellipse(cv, 5.5f, 7.5f, 4.8f, 4.5f, (nx, ny) => nx > 0.3f && ny < 0 ? 'P' : ny > 0.5f ? 'p' : 'P');
        PixelKit.Ellipse(cv, 12f, 8f, 4.6f, 4f, (nx, ny) => nx > 0.3f && ny < 0 ? 'P' : ny > 0.4f ? 'p' : 'p');
        Fill(cv, 4, 2, 6, 3, 'p'); cv.Force(5, 1, 'P'); Fill(cv, 4, 3, 6, 3, 'w');                 // amarrado
        Fill(cv, 11, 3, 13, 4, 'p'); Fill(cv, 11, 4, 13, 4, 'w');
        cv.Force(5, 8, 'C'); cv.Force(6, 8, 'C'); cv.Force(5, 9, 'c');                                // carimbo da Ordem
        Fill(cv, 0, 11, 17, 11, '.');
        for (int x = 1; x < 17; x++) if (cv.Get(x, 10) != '.') cv.Force(x, 11, 'p');
        OutlineNoBottom(cv);
        return cv;
    }

    static PixelCanvas Cobweb()
    {
        Palette();
        // teia de canto de teto (apoio "teto", canto de cima à esquerda; espelhe para a direita)
        var cv = Blank(13, 13);
        Line(cv, 0, 0, 12, 0, 'e'); Line(cv, 0, 0, 0, 12, 'e');
        Line(cv, 0, 0, 10, 10, 'e'); Line(cv, 0, 0, 12, 5, 'e'); Line(cv, 0, 0, 5, 12, 'e');
        for (int r = 4; r <= 10; r += 3)
            for (int a = 0; a <= 90; a += 4)
            {
                double t = a * Math.PI / 180.0;
                int x = (int)Math.Round(r * Math.Cos(t)), y = (int)Math.Round(r * Math.Sin(t));
                cv.Force(x, y, 'e');
            }
        return cv;
    }

    // ------------------------------------------------------------ armadilhas

    static PixelCanvas PipeH()
    {
        Palette();
        // cano de óleo (emenda a cada 16 px): ferro com fuligem embaixo
        return Map(new[] {
            "IjIiiiiiiiiiiiIj",
            "IiIiiiiiiiiiiiIi",
            "IIIIIIIIIIIIIIII",
            "IaIaaIaaIaaaaIaI",
        }, true);
    }

    static PixelCanvas PipeV()
    {
        Palette();
        var rows = new List<string>();
        for (int y = 0; y < 16; y++) rows.Add(y % 8 == 0 ? "jIIj" : "iIIa");
        return Map(rows.ToArray(), true);
    }

    static PixelCanvas Lever()
    {
        Palette();
        return Map(new[] {
            "....CC",
            "....Cc",
            "...W..",
            "...W..",
            "..W...",
            "..W...",
            ".IIIII",
            ".IiiiI",
            ".IijiI",
            ".IiiiI",
            ".IIIII",
        }, true);
    }

    static PixelCanvas Gear()
    {
        Palette();
        var cv = Blank(18, 18);
        for (int x = 0; x < 18; x++)
            for (int y = 0; y < 18; y++)
            {
                float dx = x + 0.5f - 9f, dy = y + 0.5f - 9f;
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx);
                bool tooth = Math.Cos(ang * 8) > 0.3;
                if (r < 2f) cv.Force(x, y, 'k');
                else if (r < 3.2f) cv.Force(x, y, 'j');
                else if (r < 6.2f) cv.Force(x, y, dx + dy < -2 ? 'i' : 'I');
                else if (r < 8f && tooth) cv.Force(x, y, dx + dy < -2 ? 'i' : 'I');
            }
        PixelKit.Outline(cv, 'k');
        return cv;
    }

    static PixelCanvas MaintenanceLog()
    {
        Palette();
        // registro de manutenção dos jatos: papel pregado, com uma mancha de fuligem
        return Map(new[] {
            "....i....",
            "PPPPPPPPP",
            "PpppPppPP",
            "PPPPPPPPP",
            "PppPpppPa",
            "PPPPPPPaa",
            "PpppPPpaP",
            "PPPPPPPPP",
            "PpPppPPPP",
            ".ppppppp.",
        }, true);
    }

    static PixelCanvas DentedHelm()
    {
        Palette();
        return Map(new[] {
            "...iiii...",
            "..iijiIi..",
            ".iIi.IiIi.",
            ".iIkkkIIi.",
            ".IIi.iII..",
            "IIIIIIIIIa",
        }, true);
    }

    static PixelCanvas DentedShield()
    {
        Palette();
        return Map(new[] {
            "gGGGGGGGGGg",
            "GCCCCCCCcCG",
            "GCCGCGCCcCG",
            "GCCCGCCcCCG",
            "GCCCGCcCCCG",
            ".GCCCcCCCG.",
            ".GCCcCCCCG.",
            "..GCCCCCG..",
            "...gGGGg...",
        }, true);
    }

    // ------------------------------------------------------------------ portas

    /// <summary>
    /// Porta de madeira fechada num arco de pedra (22×34): corredores que existem mas o gato não usa.
    /// Precisa ler como TRANCADA, nunca como passagem nem como parede quebrável (nada de rachadura nem dourado):
    /// v0 = tranca de ferro atravessada com cadeado; v1 = tábuas pregadas em X (abandonada). Madeira escura, pouco contraste.
    /// </summary>
    static PixelCanvas Door(int v)
    {
        Palette();
        var cv = Blank(24, 35);
        // arco de pedra
        for (int x = 0; x < 24; x++)
            for (int y = 0; y < 35; y++)
            {
                float dx = x + 0.5f - 12f, dy = y + 0.5f - 12f;
                bool inArch = y >= 12 ? Math.Abs(dx) <= 11.5f : dx * dx + dy * dy <= 11.5f * 11.5f;
                if (!inArch) continue;
                cv.Force(x, y, x > 18 ? 'H' : x < 5 ? 'D' : 'L');
            }
        // folha da porta (tábuas escuras) dentro do arco
        for (int x = 3; x < 21; x++)
            for (int y = 3; y < 35; y++)
            {
                float dx = x + 0.5f - 12f, dy = y + 0.5f - 12f;
                bool inLeaf = y >= 12 ? Math.Abs(dx) <= 8.5f : dx * dx + dy * dy <= 8.5f * 8.5f;
                if (!inLeaf) continue;
                cv.Force(x, y, (x - 3) % 4 == 0 ? 'd' : (x - 3) % 4 == 3 ? 'd' : 'w');
            }
        // juntas do arco
        foreach (var p in new[] { new Point(2, 9), new Point(6, 2), new Point(12, 0), new Point(18, 2), new Point(21, 9), new Point(1, 20), new Point(22, 20), new Point(1, 29), new Point(22, 29) })
            cv.Force(p.X, p.Y, 'S');
        // dobradiças e argola
        Fill(cv, 4, 14, 9, 14, 'I'); Fill(cv, 4, 27, 9, 27, 'I');
        cv.Force(16, 21, 'N'); cv.Force(17, 22, 'N'); cv.Force(16, 23, 'N'); cv.Force(15, 22, 'N');
        if (v == 0)
        {
            // tranca de ferro atravessada nos suportes, com cadeado
            Fill(cv, 2, 19, 21, 20, 'I'); Fill(cv, 2, 19, 21, 19, 'i');
            Fill(cv, 2, 18, 3, 21, 'I'); Fill(cv, 20, 18, 21, 21, 'I');
            Fill(cv, 11, 21, 13, 23, 'I'); cv.Force(12, 22, 'k'); cv.Force(11, 20, 'i'); cv.Force(13, 20, 'i');
        }
        else
        {
            // tábuas pregadas em X: ninguém passa por aqui há tempos
            for (int k = -1; k <= 0; k++)
            {
                Line(cv, 4, 12 + k, 19, 31 + k, 'W'); Line(cv, 19, 12 + k, 4, 31 + k, 'W');
            }
            Line(cv, 4, 13, 19, 32, 'w');
            cv.Force(5, 13, 'i'); cv.Force(18, 13, 'i'); cv.Force(5, 30, 'i'); cv.Force(18, 30, 'i');
        }
        OutlineNoBottom(cv);
        return cv;
    }

    // --------------------------------------------------------------- exterior

    static PixelCanvas DeadCampfire()
    {
        Palette();
        // fogueira apagada de quem passou antes: pedras em roda, carvão, cinza e um tição queimado
        return Map(new[] {
            "......wA.......",
            "....AwwA.......",
            "..LS.AAaaA.SL..",
            ".SLDaAaAaaASLD.",
            "SSDaaaaaaaaaDSS",
        }, true);
    }

    static PixelCanvas Backpack()
    {
        Palette();
        return Map(new[] {
            ".PPPPPP.",
            "PpPPPPpP",
            ".pppppp.",
            ".VVVVVv.",
            "VVvVVVVv",
            "VVnVVVVv",
            "VVVVVVVv",
            "VvVVVVvv",
            ".vvvvvv.",
        }, true);
    }

    // ------------------------------------------------------- barulhentos

    /// <summary>Cacos de vidro: 16×4 por tile, brilhos de 1 px. Reação: os cacos tilintam (pulam 1 px e piscam).</summary>
    static PixelCanvas Glass(int f)
    {
        Palette();
        var cv = Blank(16, 4);
        int[,] shards = { { 1, 3, 3 }, { 4, 2, 2 }, { 6, 3, 1 }, { 9, 3, 3 }, { 11, 2, 2 }, { 13, 3, 2 }, { 15, 3, 1 } };
        for (int s = 0; s < shards.GetLength(0); s++)
        {
            int x = shards[s, 0], y = shards[s, 1], len = shards[s, 2];
            int hop = f > 0 && (s + f) % 3 == 0 ? 1 : 0;
            for (int k = 0; k < len; k++) cv.Force(x - k, y - hop - (k == len - 1 ? 1 : 0), k == 0 ? 'x' : 'X');
            if (f > 0 && (s + f) % 2 == 0) cv.Force(x, Math.Max(0, y - hop - 2), 'x');                // brilho do tilintar
        }
        return cv;
    }

    /// <summary>Folhas secas (exterior) ou palha (pátio, estábulo): 16×4. Reação: pulam e assentam.</summary>
    static PixelCanvas Leaves(int f, bool straw)
    {
        Palette();
        var cv = Blank(16, 5);
        char a = straw ? 'Y' : 'l', b = straw ? 'y' : 'm', c2 = straw ? 'h' : 'l';
        for (int x = 0; x < 16; x++)
        {
            float n = PixelKit.Noise(x, 0, straw ? 77 : 33);
            int hop = f == 1 ? ((x % 3 == 0) ? 2 : 1) : f == 2 ? (x % 3 == 1 ? 1 : 0) : 0;
            cv.Force(x, 4 - hop, n > 0.5f ? a : b);
            if (n > 0.35f) cv.Force(x, 3 - hop, n > 0.8f ? c2 : a);
            if (straw && x % 4 == 1) cv.Force(x + (f == 1 ? 1 : 0), 2 - hop, 'h');
            if (!straw && n > 0.85f) cv.Force(x, 2 - hop, 'm');
        }
        return cv;
    }

    /// <summary>
    /// Sininhos do alojamento: travessa de madeira de 28 px sobre a passagem, com 3 sininhos de latão a 17–21 px do
    /// chão (batem na cabeça do gato em pé; agachado ele passa por baixo). Reação: balançam (quadros 1..4).
    /// </summary>
    static PixelCanvas Chimes(int f)
    {
        Palette();
        var cv = Blank(20, 29);
        for (int y = 0; y < 29; y++) { cv.Force(1, y, 'w'); cv.Force(2, y, 'd'); cv.Force(17, y, 'W'); cv.Force(18, y, 'w'); }
        Fill(cv, 0, 1, 19, 2, 'w'); Fill(cv, 0, 2, 19, 2, 'd');
        int[] sway = { 0, 1, 0, -1, 0 };
        int[] bx = { 6, 10, 14 };
        for (int b = 0; b < 3; b++)
        {
            int s = f == 0 ? 0 : sway[(f + b) % 5];
            int len = 4 + b % 2;
            for (int y = 3; y < 3 + len; y++) cv.Force(bx[b] + (y > 4 ? s : 0), y, 'p');
            int top = 3 + len, x0 = bx[b] + s;
            cv.Force(x0, top, 'n'); Fill(cv, x0 - 1, top + 1, x0 + 1, top + 2, 'n'); cv.Force(x0 + 1, top + 1, 'N');
            Fill(cv, x0 - 1, top + 3, x0 + 1, top + 3, 'N'); cv.Force(x0 + (s > 0 ? 1 : 0), top + 4, 'N');
        }
        OutlineNoBottom(cv, "p");
        return cv;
    }

    /// <summary>Balde de serviço: em pé (0) e caindo até ficar de lado com a água espalhada (1..4). Fica caído.</summary>
    static PixelCanvas Bucket(int f)
    {
        Palette();
        var up = Blank(16, 12);
        up.Stamp(new[] {
            ".IiiiiiiiI",
            ".wBbBBbBBw",
            ".IiiiiiiiI",
            ".wWwWwWwWw",
            ".wWwWwWwww",
            ".IiiiiiiiI",
            ".dwWwWwWwd",
            "..dddddddd",
        }, 3, 3);
        Line(up, 4, 2, 7, 0, 'i'); Line(up, 7, 0, 10, 0, 'i'); Line(up, 10, 0, 12, 2, 'i');       // alça
        PixelCanvas cv;
        if (f == 0) cv = up;
        else if (f <= 2)
        {
            // inclinando para a direita: cada linha anda para o lado conforme a altura (1/4 e 1/2 px por linha)
            float k = f == 1 ? 0.25f : 0.55f;
            cv = Blank(16, 12);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 12; y++)
                {
                    char c = up.Px[x, y]; if (c == '.') continue;
                    int nx = x + (int)Math.Round((11 - y) * k);
                    cv.Force(nx, y + (f == 2 ? 1 : 0), c);
                }
        }
        else
        {
            // caído de lado, boca para a esquerda; no último quadro a água já se espalhou
            cv = Blank(16, 12);
            cv.Stamp(new[] {
                ".IwIwwIwI.",
                "EIWIWWIWId",
                "bIwIwwIwId",
                "BIWIWWIWId",
                ".IwIwwIwI.",
            }, 4, 7);
            Line(cv, 14, 8, 15, 10, 'i');
            if (f == 4) { Fill(cv, 0, 11, 4, 11, 'B'); cv.Force(1, 11, 'b'); cv.Force(3, 11, 'b'); cv.Force(2, 10, 'E'); }
            else { cv.Force(3, 9, 'b'); cv.Force(3, 10, 'B'); }
        }
        // ancorar no chão: desce até a última linha
        int bottom = -1;
        for (int y = cv.H - 1; y >= 0 && bottom < 0; y--) for (int x = 0; x < cv.W; x++) if (cv.Px[x, y] != '.') { bottom = y; break; }
        if (bottom >= 0 && bottom < cv.H - 1)
        {
            var moved = Blank(cv.W, cv.H); int dy = cv.H - 1 - bottom;
            for (int x = 0; x < cv.W; x++) for (int y = 0; y < cv.H - dy; y++) moved.Px[x, y + dy] = cv.Px[x, y];
            cv = moved;
        }
        OutlineNoBottom(cv, "bBE");
        return cv;
    }
}
