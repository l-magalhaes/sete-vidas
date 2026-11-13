// Gerador dos sprites do Cão Templário (inimigo da Fortaleza em Sete Vidas).
// Mesmo método do Lion (Tools/LionSprites): cada frame é montado a partir de partes (cabeça, tronco, pernas, braços,
// lança) num esqueleto em pixel, com contorno automático de 1 px na cor do contorno do Lion.
// Grava PNG + .meta e o asset TemplarAnimationSet (CharacterAnimationSet) para o Unity.
// C# 5 (compilado pelo PowerShell). Uso: Tools/EnemySprites/gerar.ps1
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class TemplarSpriteGen
{
    // ------------------------------------------------------------------ paleta

    static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>();
    static void P(char c, string hex) { Pal[c] = ColorTranslator.FromHtml(hex); }
    static void InitPalette()
    {
        Pal.Clear();
        P('o', "#1a1c26");                                       // contorno (o mesmo do Lion)
        P('K', "#2e2a33"); P('k', "#45404b");                    // pelo preto (sela do pastor) e luz
        P('A', "#b06a3b"); P('a', "#7f4526"); P('Q', "#d9a26a"); // pelo castanho, sombra, focinho claro
        P('I', "#c3c9d6"); P('i', "#8a91a0"); P('J', "#5a6173"); // aço: luz, base, sombra
        P('P', "#e8e4da"); P('p', "#b3ada0");                    // sobreveste branca
        P('Y', "#d9a441"); P('y', "#a8782a");                    // ouro da Ordem (pata)
        P('B', "#5a3a24");                                       // couro do cinto
        P('W', "#6b4a2b"); P('w', "#8c6640");                    // haste da lança
        P('D', "#a9bcd6"); P('d', "#f4f7fb");                    // lâmina
        P('E', "#f4f7fb");                                       // brilho do olho
        // Templário Zumbi (a praga do tomo): pelo morto, sobreveste suja, olho verde-ácido e veias roxas
        P('Z', "#7d7058"); P('z', "#544a3c"); P('q', "#a89a7a"); // pelo morto, sombra, focinho
        P('U', "#b8b09a"); P('u', "#7e7868");                    // sobreveste suja e rasgos
        P('j', "#8a8690"); P('r', "#6a5048");                    // aço sem brilho, ferrugem
        P('G', "#8fd14f"); P('V', "#5b2a6e");                    // olho e veias (o par da praga)
    }

    // ------------------------------------------------------------ canvas

    // 64×44, pivot no centro da borda de baixo (pés na linha 43). A lança cabe esticada para a frente.
    public const int W = 64, H = 44;

    public class Canvas
    {
        public char[,] Px = new char[W, H];
        public Canvas() { for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) Px[x, y] = '.'; }
        public void Set(int x, int y, char c) { if (c != '.' && c != ' ' && x >= 0 && y >= 0 && x < W && y < H) Px[x, y] = c; }
        public char Get(int x, int y) { return (x >= 0 && y >= 0 && x < W && y < H) ? Px[x, y] : '.'; }
        public void Stamp(string[] map, int x0, int y0)
        {
            for (int r = 0; r < map.Length; r++) for (int c = 0; c < map[r].Length; c++) Set(x0 + c, y0 + r, map[r][c]);
        }
    }

    static void Outline(Canvas cv)
    {
        var add = new List<Point>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                if (cv.Px[x, y] != '.') continue;
                if (cv.Get(x - 1, y) != '.' || cv.Get(x + 1, y) != '.' || cv.Get(x, y - 1) != '.' || cv.Get(x, y + 1) != '.')
                    add.Add(new Point(x, y));
            }
        foreach (var p in add) cv.Px[p.X, p.Y] = 'o';
    }

    static List<Point> Line(int x0, int y0, int x1, int y1)
    {
        var pts = new List<Point>();
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            pts.Add(new Point(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return pts;
    }

    // Retângulo girado (u ao longo do eixo, v perpendicular). Ângulo em graus, 0 = direita, 90 = cima.
    static void FillRotated(Canvas cv, float ox, float oy, float angleDeg, float u0, float u1, float halfV, Func<float, float, char> colorAt)
    {
        double a = angleDeg * Math.PI / 180.0;
        float dx = (float)Math.Cos(a), dy = (float)-Math.Sin(a);
        float px = -dy, py = dx;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                float rx = x + 0.5f - ox, ry = y + 0.5f - oy;
                float u = rx * dx + ry * dy, v = rx * px + ry * py;
                if (u < u0 || u > u1 || Math.Abs(v) > halfV) continue;
                char c = colorAt(u, v);
                if (c != '.') cv.Set(x, y, c);
            }
    }

    // ---------------------------------------------------------------- partes

    // Cabeça de pastor olhando para a direita (15×11): orelhas pontudas furando o elmo, protetor de nariz,
    // focinho comprido castanho-claro.
    static readonly string[] HeadBase = {
        "...K....K......",
        "..KAK..KAK.....",
        "..JIIIIIIJ.....",
        ".JIIIIIIIIJ....",
        ".JIIIIIIIIIJ...",
        ".JYiiiiiiiiiJ..",
        ".KKAAAAoEiAAA..",
        ".KKAAAAAAiQQQQK",
        "..KKAAAAAQQQQQ.",
        "..KKKAAaaQQQa..",
        "...KKKaa.......",
    };

    static string[] Head(string variant)
    {
        var rows = (string[])HeadBase.Clone();
        Action<int, int, char> set = (r, c, ch) => { var a = rows[r].ToCharArray(); a[c] = ch; rows[r] = new string(a); };
        switch (variant)
        {
            case "alert": // orelhas mais altas, olho arregalado
                rows[0] = "..K.....K......"; rows[1] = "..KK....KK.....";
                set(6, 6, 'E'); set(6, 7, 'o'); set(6, 8, 'o'); set(5, 7, 'o');
                break;
            case "angry": // olho estreito, dentes à mostra
                set(6, 8, 'o'); set(5, 8, 'o'); set(9, 10, 'E'); set(9, 11, 'E'); set(9, 12, 'o');
                break;
            case "zombie": // orelhas caídas, olho aberto (vira verde-ácido no ZombieSkin), boca aberta e torta
                rows[0] = "...............";
                rows[1] = ".KKK...KK......";
                set(9, 10, 'o'); set(9, 11, 'o'); set(10, 10, 'o');
                break;
            case "hurt": // olho fechado, orelhas para trás, boca aberta
                rows[0] = "...............";
                rows[1] = ".KKK..KKK......";
                set(6, 7, 'a'); set(6, 8, 'a'); set(9, 10, 'o'); set(9, 11, 'o');
                break;
        }
        if (Archer)
        {
            // atirador: capuz de couro no lugar do elmo de aço (e sem o protetor de nariz)
            for (int r = 0; r < rows.Length; r++)
            {
                var a = rows[r].ToCharArray();
                for (int c = 0; c < a.Length; c++)
                {
                    if (r <= 5) { if (a[c] == 'I') a[c] = 'w'; else if (a[c] == 'i') a[c] = 'W'; else if (a[c] == 'J') a[c] = 'B'; }
                    else if (a[c] == 'i') a[c] = 'A';
                }
                rows[r] = new string(a);
            }
        }
        return rows;
    }

    /// <summary>Gera a variante do atirador (capuz de couro, aljava, besta).</summary>
    static bool Archer;
    /// <summary>Gera o Templário Zumbi (depois do tomo): mesmo corpo, sem arma, pelo morto, olho verde-ácido e veias roxas.</summary>
    static bool Zombie;

    // Tronco (12×10): ombreiras de aço, sobreveste branca com a pata dourada da Ordem, cinto, saia em duas abas.
    static readonly string[] Torso = {
        "..JJIIIIJJ..",
        ".JIIIIIIIIJ.",
        "JIIPPPPPPIIJ",
        "JIPPYPYPYPIJ",
        "JiPPPYYYPPiJ",
        ".iPPPYYYPPi.",
        ".iPPPPPPPPi.",
        ".BBBBYBBBBB.",
        ".pPPPP.PPPP.",
        ".ppPP...PPp.",
    };

    public class Pose
    {
        public int Py = 36;                    // linha de baixo da saia (quadril)
        public int TorsoDy, HeadDx, HeadDy, BodyDx;
        public string HeadV = "n";
        public Point BackFoot = new Point(29, 43), FrontFoot = new Point(35, 43);
        public Point FrontHand = new Point(39, 31);
        public Point? BackHand;                 // mão de trás na lança (estocada)
        public float SpearAngle = 90f;          // 90 = de pé
        public float SpearBack = 12f;           // quanto da haste fica atrás da mão
        public bool NoSpear;
        // besta (Cão Sentinela Atirador)
        public bool Crossbow;
        public float BowAngle;                  // 0 = para a frente, negativo = mirando para baixo
        public bool Loaded = true;              // virote no trilho
        public bool Released;                   // corda solta (acabou de atirar)
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    static Pose With(Pose b, Action<Pose> e) { var p = b.Clone(); e(p); return p; }

    static Canvas Render(Pose p, bool outline)
    {
        var cv = new Canvas();
        int bx = 26 + p.BodyDx;                                   // borda esquerda do tronco
        int ty = p.Py - 9 + p.TorsoDy;                            // topo do tronco
        int fsx = bx + 10, fsy = ty + 2;                          // ombro da frente
        int bsx = bx + 2, bsy = ty + 2;                           // ombro de trás

        // cauda curta saindo de baixo da sobreveste
        cv.Set(bx, ty + 7, 'K'); cv.Set(bx - 1, ty + 7, 'K'); cv.Set(bx - 1, ty + 8, 'K'); cv.Set(bx - 2, ty + 8, 'k');
        cv.Set(bx - 2, ty + 9, 'K'); cv.Set(bx - 3, ty + 9, 'K'); cv.Set(bx - 3, ty + 10, 'A');

        // aljava nas costas (atirador): couro com as penas dos virotes para fora
        if (Archer)
        {
            for (int k = 0; k < 8; k++) { cv.Set(bx - 1 + k / 4, ty - 1 + k, 'B'); cv.Set(bx + k / 4, ty - 1 + k, 'W'); }
            cv.Set(bx - 2, ty - 2, 'P'); cv.Set(bx - 1, ty - 3, 'P'); cv.Set(bx, ty - 2, 'p'); cv.Set(bx - 1, ty - 2, 'Y');
        }
        // braço de trás (atrás de tudo)
        if (p.BackHand.HasValue) Arm(cv, bsx, bsy, p.BackHand.Value, 'J', 'J');
        // pernas
        Leg(cv, bx + 3, p.Py + 1, p.BackFoot, true);
        Leg(cv, bx + 7, p.Py + 1, p.FrontFoot, false);
        // tronco e cabeça
        cv.Stamp(Torso, bx, ty);
        cv.Stamp(Head(p.HeadV), bx + 1 + p.HeadDx, ty - 10 + p.HeadDy);
        // lança e braço da frente (a mão por cima da haste)
        if (p.Crossbow) CrossbowAt(cv, p.FrontHand.X + 0.5f, p.FrontHand.Y + 0.5f, p.BowAngle, p.Loaded, p.Released);
        else if (!p.NoSpear) Spear(cv, p.FrontHand.X + 0.5f, p.FrontHand.Y + 0.5f, p.SpearAngle, p.SpearBack);
        Arm(cv, fsx, fsy, p.FrontHand, 'I', 'J');
        if (Zombie) ZombieSkin(cv);
        if (outline) Outline(cv);
        return cv;
    }

    static void Leg(Canvas cv, int hx, int hy, Point foot, bool back)
    {
        char steel = back ? 'i' : 'I';
        var pts = Line(hx, hy, foot.X, foot.Y - 1);
        foreach (var q in pts) { cv.Set(q.X, q.Y, steel); cv.Set(q.X + 1, q.Y, back ? 'J' : 'i'); }
        // bota: 3 px, ponta para a frente
        cv.Set(foot.X, foot.Y, 'J'); cv.Set(foot.X + 1, foot.Y, 'J'); cv.Set(foot.X + 2, foot.Y, 'J');
    }

    static void Arm(Canvas cv, int sx, int sy, Point hand, char steel, char glove)
    {
        var pts = Line(sx, sy, hand.X, hand.Y);
        for (int i = 0; i < pts.Count; i++)
        {
            var q = pts[i];
            cv.Set(q.X, q.Y, steel); cv.Set(q.X, q.Y + 1, steel == 'I' ? 'i' : 'J');
        }
        cv.Set(hand.X, hand.Y, glove); cv.Set(hand.X + 1, hand.Y, glove); cv.Set(hand.X, hand.Y + 1, glove); cv.Set(hand.X + 1, hand.Y + 1, glove);
    }

    // Lança: haste de madeira (2 tons), anel dourado e ponta em folha com fio claro.
    static void Spear(Canvas cv, float gx, float gy, float angle, float back)
    {
        FillRotated(cv, gx, gy, angle, -back, 14f, 0.55f, (u, v) => v > 0.1f ? 'w' : 'W');
        FillRotated(cv, gx, gy, angle, 13.5f, 14.6f, 0.9f, (u, v) => 'Y');
        FillRotated(cv, gx, gy, angle, 14.6f, 19.5f, 1.6f, (u, v) =>
        {
            float half = u < 16.5f ? 0.6f + (u - 14.6f) * 0.55f : 1.6f - (u - 16.5f) * 0.5f; // folha
            if (Math.Abs(v) > half) return '.';
            return v > 0.2f ? 'd' : 'D';
        });
    }

    // ------------------------------------------------------------- clipes

    public class Clip
    {
        public string Name; public float FrameTime; public bool Loop;
        public List<Canvas> Frames = new List<Canvas>();
    }
    static readonly List<Clip> Clips = new List<Clip>();

    static void Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new Clip { Name = name, FrameTime = t, Loop = loop };
        foreach (var p in poses) c.Frames.Add(Render(p, true));
        Clips.Add(c);
    }
    static void AddCanvas(string name, float t, bool loop, params Canvas[] frames)
    {
        var c = new Clip { Name = name, FrameTime = t, Loop = loop };
        c.Frames.AddRange(frames);
        Clips.Add(c);
    }

    static void BuildClips()
    {
        Clips.Clear();
        var std = new Pose();

        // parado de guarda: lança de pé com o cabo no chão; respira (o tronco desce 1 px)
        Add("idle", 0.25f, true, std, std, With(std, p => { p.TorsoDy = 1; p.FrontHand = new Point(39, 32); p.SpearBack = 11f; }),
                                           With(std, p => { p.TorsoDy = 1; p.FrontHand = new Point(39, 32); p.SpearBack = 11f; }));

        // alerta ("?" e "!"): orelhas em pé, lança empunhada à frente
        var look = With(std, p => { p.HeadV = "alert"; p.SpearAngle = 70f; p.SpearBack = 9f; p.FrontHand = new Point(39, 32); });
        Add("alert", 0.3f, true, look, With(look, p => { p.HeadDx = -1; }));

        // patrulha: lança no ombro, passos curtos
        var shoulder = With(std, p => { p.SpearAngle = 62f; p.SpearBack = 8f; p.FrontHand = new Point(38, 33); });
        Add("walk", 0.15f, true,
            With(shoulder, p => { p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); }),
            With(shoulder, p => { p.TorsoDy = -1; p.BackFoot = new Point(30, 42); p.FrontFoot = new Point(34, 43); }),
            With(shoulder, p => { p.BackFoot = new Point(36, 43); p.FrontFoot = new Point(28, 43); }),
            With(shoulder, p => { p.TorsoDy = -1; p.BackFoot = new Point(33, 43); p.FrontFoot = new Point(31, 42); }));

        // perseguição: lança baixada para a frente, as duas mãos, inclinado
        var charge = With(std, p => { p.HeadV = "angry"; p.HeadDx = 1; p.SpearAngle = 8f; p.SpearBack = 10f; p.FrontHand = new Point(40, 33); p.BackHand = new Point(33, 34); });
        Add("run", 0.1f, true,
            With(charge, p => { p.BackFoot = new Point(25, 43); p.FrontFoot = new Point(39, 43); }),
            With(charge, p => { p.TorsoDy = -1; p.BackFoot = new Point(29, 41); p.FrontFoot = new Point(35, 43); }),
            With(charge, p => { p.BackFoot = new Point(38, 43); p.FrontFoot = new Point(26, 43); }),
            With(charge, p => { p.TorsoDy = -1; p.BackFoot = new Point(34, 43); p.FrontFoot = new Point(30, 41); }));

        // estocada: recua a lança (antecipação longa e legível), estica tudo, volta
        var guard = With(std, p => { p.HeadV = "angry"; p.SpearAngle = 4f; p.SpearBack = 10f; p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); });
        Add("attack_windup", 0.1f, false,
            With(guard, p => { p.HeadDx = -1; p.FrontHand = new Point(35, 32); p.BackHand = new Point(29, 33); }),
            With(guard, p => { p.HeadDx = -1; p.BodyDx = -1; p.TorsoDy = 1; p.FrontHand = new Point(31, 32); p.BackHand = new Point(25, 33); p.SpearAngle = 6f; }));
        Add("attack_active", 0.06f, false,
            With(guard, p => { p.HeadDx = 1; p.BodyDx = 1; p.FrontHand = new Point(44, 32); p.BackHand = new Point(38, 32); p.FrontFoot = new Point(39, 43); p.BackFoot = new Point(25, 43); p.SpearAngle = 0f; p.SpearBack = 8f; }),
            With(guard, p => { p.HeadDx = 1; p.BodyDx = 1; p.FrontHand = new Point(45, 32); p.BackHand = new Point(39, 32); p.FrontFoot = new Point(39, 43); p.BackFoot = new Point(25, 43); p.SpearAngle = 0f; p.SpearBack = 8f; }));
        Add("attack_recovery", 0.12f, false,
            With(guard, p => { p.HeadDx = 1; p.FrontHand = new Point(41, 32); p.BackHand = new Point(35, 33); p.FrontFoot = new Point(38, 43); p.SpearAngle = -4f; }),
            With(guard, p => { p.FrontHand = new Point(38, 32); p.BackHand = new Point(32, 33); p.SpearAngle = 20f; }));

        // dano: recua com a lança balançando
        var hit = With(std, p => { p.HeadV = "hurt"; p.HeadDx = -1; p.BodyDx = -1; p.SpearAngle = 104f; p.FrontHand = new Point(37, 32); });
        Add("hurt", 0.1f, false, hit, With(hit, p => { p.HeadDx = 0; p.BodyDx = 0; p.SpearAngle = 96f; }));

        // morte: recua, cai de joelhos, tomba de costas e a lança cai no chão
        var d0 = With(hit, p => { p.SpearAngle = 115f; });
        var d1 = With(std, p => { p.HeadV = "hurt"; p.Py = 39; p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); p.FrontHand = new Point(38, 36); p.SpearAngle = 140f; p.SpearBack = 6f; });
        AddCanvas("death", 0.12f, false, Render(d0, true), Render(d1, true), Fallen(45f), Fallen(90f), Fallen(90f));

        // abate furtivo: trava de susto (olho arregalado), a lança escorrega, os joelhos cedem e tomba de cara para
        // a frente, sem recuar (a adaga veio de trás). O primeiro quadro segura mais (o "congela" do golpe).
        var s0 = With(std, p => { p.HeadV = "alert"; p.HeadDy = -1; p.SpearAngle = 96f; p.FrontHand = new Point(39, 32); });
        var s1 = With(std, p => { p.HeadV = "hurt"; p.HeadDx = 1; p.TorsoDy = 1; p.Py = 37; p.SpearAngle = 118f; p.SpearBack = 9f; p.FrontHand = new Point(38, 35); });
        var s2 = With(std, p => { p.HeadV = "hurt"; p.HeadDx = 1; p.HeadDy = 1; p.Py = 39; p.BodyDx = 1; p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); p.NoSpear = true; p.FrontHand = new Point(39, 37); });
        AddCanvas("death_stealth", 0.12f, false, Render(s0, true), Render(s0, true), Render(s1, true), Render(s2, true), Fallen(45f, true), Fallen(90f, true), Fallen(90f, true));
    }

    // Besta: coronha de madeira (a mão segura no meio), arco de aço na frente, corda e o virote no trilho.
    static void CrossbowAt(Canvas cv, float gx, float gy, float angle, bool loaded, bool released)
    {
        double a = angle * Math.PI / 180.0;
        float dx = (float)Math.Cos(a), dy = (float)-Math.Sin(a);   // para a frente (y para baixo)
        float px = -dy, py = dx;                                    // perpendicular
        // coronha (de 6 px atrás da mão até 3 na frente)
        FillRotated(cv, gx, gy, angle, -6f, 3f, 0.6f, (u, v) => u < -4.5f ? 'B' : (v > 0.1f ? 'w' : 'W'));
        // arco (limbos) atravessado na ponta: puxado para trás quando armado
        float bx = gx + dx * 3.5f, by = gy + dy * 3.5f;
        float pull = released ? 0f : 1.2f;
        for (int s = -1; s <= 1; s += 2)
            foreach (var q in Line((int)Math.Round(bx), (int)Math.Round(by),
                                   (int)Math.Round(bx + px * 4f * s - dx * pull), (int)Math.Round(by + py * 4f * s - dy * pull)))
                cv.Set(q.X, q.Y, 'J');
        // corda: dos limbos até a trava (armada) ou reta (solta)
        float nx = released ? bx : gx - dx * 1.5f, ny = released ? by : gy - dy * 1.5f;
        for (int s = -1; s <= 1; s += 2)
            foreach (var q in Line((int)Math.Round(bx + px * 4f * s - dx * pull), (int)Math.Round(by + py * 4f * s - dy * pull),
                                   (int)Math.Round(nx), (int)Math.Round(ny)))
                if (cv.Get(q.X, q.Y) == '.') cv.Set(q.X, q.Y, 'p');
        // virote no trilho, com a ponta clara passando do arco
        if (loaded)
            FillRotated(cv, gx, gy, angle, -1.5f, 6f, 0.45f, (u, v) => u > 5f ? 'd' : (u < -0.5f ? 'P' : 'D'));
    }

    // Corpo girado para trás em volta dos pés (cai de costas), lança largada no chão.
    // forward = true: tomba de cara para a frente (abate furtivo: morte silenciosa, "death_stealth")
    static Canvas Fallen(float degrees, bool forward = false)
    {
        var body = Render(new Pose { HeadV = Zombie ? "zombie" : "hurt", NoSpear = true, FrontHand = new Point(36, 35) }, false);
        var cv = new Canvas();
        double a = (forward ? degrees : -degrees) * Math.PI / 180.0;  // negativo: tomba para trás (cabeça para a esquerda)
        float cs = (float)Math.Cos(a), sn = (float)Math.Sin(a);
        float cx = 32f, cy = 43.5f;
        float lift = degrees >= 89f ? 7f : 2f;                       // deitado: o corpo assenta no chão
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                // rotação inversa: para cada pixel do destino, de onde ele vem
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy + lift;
                float sx = cs * dx + sn * dy + cx, sy = -sn * dx + cs * dy + cy;
                char c = body.Get((int)Math.Floor(sx), (int)Math.Floor(sy));
                if (c != '.') cv.Px[x, y] = c;
            }
        if (Zombie) { }                                              // zumbi: sem arma
        else if (forward)
        {
            // a arma escorrega da mão e fica para trás, no chão
            if (Archer) CrossbowAt(cv, degrees >= 89f ? 22.5f : 25.5f, 42.5f, 180f, false, true);
            else if (degrees >= 89f) Spear(cv, 30.5f, 42.5f, 180f, 10f);
            else Spear(cv, 27.5f, 40.5f, 200f, 8f);
        }
        else if (Archer)
        {
            if (degrees >= 89f) CrossbowAt(cv, 40.5f, 42.5f, 0f, false, true); // besta largada no chão
            else CrossbowAt(cv, 39.5f, 39.5f, -30f, false, true);
        }
        else if (degrees >= 89f) Spear(cv, 36.5f, 42.5f, 0f, 10f);   // lança no chão
        else Spear(cv, 38.5f, 38.5f, 160f, 8f);
        Outline(cv);
        return cv;
    }

    // ---------------------------------------------------- o Templário Zumbi

    /// <summary>
    /// Troca as cores do templário pelas do zumbi (pedido do usuário, 12/11/2026): pelo morto, aço sem brilho com
    /// ferrugem, sobreveste suja e rasgada, o olho verde-ácido e veias roxas no pelo. Determinístico pela posição.
    /// </summary>
    static void ZombieSkin(Canvas cv)
    {
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = cv.Px[x, y];
                int h = (x * 7 + y * 13) % 17;
                switch (c)
                {
                    case 'A': cv.Px[x, y] = h == 0 ? 'V' : 'Z'; break;
                    case 'a': cv.Px[x, y] = 'z'; break;
                    case 'Q': cv.Px[x, y] = h == 3 ? 'V' : 'q'; break;
                    case 'K': cv.Px[x, y] = h == 5 ? 'V' : 'K'; break;
                    case 'P': cv.Px[x, y] = h % 5 == 0 ? 'u' : 'U'; break;
                    case 'p': cv.Px[x, y] = 'u'; break;
                    case 'I': cv.Px[x, y] = h == 7 ? 'r' : 'j'; break;
                    case 'i': cv.Px[x, y] = h % 6 == 1 ? 'r' : 'i'; break;
                    case 'Y': cv.Px[x, y] = 'y'; break;
                    case 'E': cv.Px[x, y] = 'G'; break;
                }
            }
        // rasgos na barra da sobreveste: alguns pixels somem
        for (int x = 0; x < W; x++)
            for (int y = 1; y < H; y++)
                if ((cv.Px[x, y] == 'U' || cv.Px[x, y] == 'u') && cv.Get(x, y + 1) == '.' && (x * 3 + y) % 4 == 0) cv.Px[x, y] = '.';
    }

    /// <summary>O corpo deitado se desfazendo em fiapos roxos (morte do zumbi): tira pixels e tinge alguns de roxo.</summary>
    static Canvas Dissolve(Canvas src, int level)
    {
        var cv = new Canvas();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = src.Px[x, y];
                if (c == '.') continue;
                int h = (x * 11 + y * 5 + level * 3) % 7;
                if (h < level * 2) continue;                       // some
                cv.Px[x, y] = h == level * 2 ? 'V' : c;            // borda roxa onde está sumindo
            }
        // fiapos subindo
        for (int k = 0; k < 6; k++)
        {
            int fx = 22 + k * 4 + level, fy = 40 - level * 3 - (k % 3) * 2;
            cv.Set(fx, fy, 'V'); if (level > 1) cv.Set(fx + 1, fy - 2, 'V');
        }
        return cv;
    }

    static void BuildZombieClips()
    {
        Clips.Clear();
        // curvado, cabeça para a frente e para baixo, os dois braços estendidos à frente (sem arma)
        var slump = new Pose { HeadV = "zombie", NoSpear = true, HeadDx = 1, HeadDy = 1, TorsoDy = 1,
                               FrontHand = new Point(42, 32), BackHand = new Point(40, 34) };
        Add("idle", 0.3f, true, slump,
            With(slump, p => { p.HeadDx = 2; p.FrontHand = new Point(42, 33); p.BackHand = new Point(40, 35); }),
            With(slump, p => { p.HeadDx = 2; p.HeadDy = 2; p.FrontHand = new Point(42, 34); }),
            With(slump, p => { p.FrontHand = new Point(42, 33); }));
        // arrastando os pés: passos curtos, o corpo balança de um lado para o outro
        Add("walk", 0.2f, true,
            With(slump, p => { p.BackFoot = new Point(28, 43); p.FrontFoot = new Point(35, 43); }),
            With(slump, p => { p.TorsoDy = 2; p.HeadDx = 2; p.BackFoot = new Point(30, 43); p.FrontFoot = new Point(34, 42); p.FrontHand = new Point(42, 33); }),
            With(slump, p => { p.BackFoot = new Point(34, 43); p.FrontFoot = new Point(29, 43); }),
            With(slump, p => { p.TorsoDy = 2; p.HeadDx = 0; p.BackFoot = new Point(32, 42); p.FrontFoot = new Point(31, 43); p.BackHand = new Point(40, 35); }));
        // perseguição: inclinado, braços esticados para pegar, passadas largas e tortas
        var lunge = With(slump, p => { p.HeadDx = 2; p.BodyDx = 1; p.TorsoDy = 0; p.FrontHand = new Point(44, 30); p.BackHand = new Point(42, 32); });
        Add("run", 0.1f, true,
            With(lunge, p => { p.BackFoot = new Point(25, 43); p.FrontFoot = new Point(38, 43); }),
            With(lunge, p => { p.TorsoDy = 1; p.BackFoot = new Point(29, 41); p.FrontFoot = new Point(35, 43); p.FrontHand = new Point(44, 31); }),
            With(lunge, p => { p.BackFoot = new Point(37, 43); p.FrontFoot = new Point(27, 43); }),
            With(lunge, p => { p.TorsoDy = 1; p.BackFoot = new Point(33, 43); p.FrontFoot = new Point(30, 41); p.BackHand = new Point(42, 33); }));
        // sentiu o gato: a cabeça dá um tranco para cima e volta
        Add("alert", 0.08f, false,
            With(slump, p => { p.HeadDy = -1; p.HeadDx = 0; p.TorsoDy = 0; p.FrontHand = new Point(43, 30); }),
            With(slump, p => { p.HeadDy = 0; p.HeadDx = 2; p.FrontHand = new Point(43, 31); }),
            slump);
        var hit = With(slump, p => { p.HeadDx = -1; p.BodyDx = -1; p.FrontHand = new Point(40, 30); p.BackHand = new Point(38, 31); });
        Add("hurt", 0.1f, false, hit, With(hit, p => { p.HeadDx = 0; p.BodyDx = 0; }));
        // morte: cai de costas e se desfaz em fiapos roxos
        var lying = Fallen(90f);
        AddCanvas("death", 0.12f, false, Render(hit, true), Fallen(45f), lying, lying, Dissolve(lying, 1), Dissolve(lying, 2), Dissolve(lying, 3));
        // golpe pelas costas que mata: tomba de cara para a frente e se desfaz
        var down = Fallen(90f, true);
        AddCanvas("death_stealth", 0.12f, false, Render(slump, true), Fallen(45f, true), down, down, Dissolve(down, 1), Dissolve(down, 2), Dissolve(down, 3));
    }

    // ------------------------------------------------ o Cão Sentinela Atirador

    static void BuildCrossbowClips()
    {
        Clips.Clear();
        // de guarda: a besta atravessada no corpo, apontando para o chão à frente
        var rest = new Pose { Crossbow = true, NoSpear = true, BowAngle = -40f, FrontHand = new Point(38, 33), BackHand = new Point(33, 31) };
        Add("idle", 0.25f, true, rest, rest, With(rest, p => { p.TorsoDy = 1; p.FrontHand = new Point(38, 34); p.BackHand = new Point(33, 32); }),
                                            With(rest, p => { p.TorsoDy = 1; p.FrontHand = new Point(38, 34); p.BackHand = new Point(33, 32); }));
        Add("walk", 0.15f, true,
            With(rest, p => { p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); }),
            With(rest, p => { p.TorsoDy = -1; p.BackFoot = new Point(30, 42); p.FrontFoot = new Point(34, 43); }),
            With(rest, p => { p.BackFoot = new Point(36, 43); p.FrontFoot = new Point(28, 43); }),
            With(rest, p => { p.TorsoDy = -1; p.BackFoot = new Point(33, 43); p.FrontFoot = new Point(31, 42); }));
        Clips.Add(new Clip { Name = "run", FrameTime = 0.1f, Loop = true, Frames = Clips[Clips.Count - 1].Frames });
        // alerta: ergue a besta, orelhas em pé
        var up = With(rest, p => { p.HeadV = "alert"; p.BowAngle = -12f; p.FrontHand = new Point(39, 31); p.BackHand = new Point(34, 30); });
        Add("alert", 0.3f, true, up, With(up, p => { p.HeadDx = -1; }));

        // mira em três ângulos: a besta no ombro, as duas mãos nela
        var aim0 = With(rest, p => { p.HeadV = "angry"; p.BowAngle = 0f; p.FrontHand = new Point(40, 29); p.BackHand = new Point(35, 29); p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); });
        var aim30 = With(aim0, p => { p.BowAngle = -30f; p.HeadDy = 1; p.FrontHand = new Point(39, 31); p.BackHand = new Point(35, 29); });
        var aim60 = With(aim0, p => { p.BowAngle = -60f; p.HeadDy = 1; p.HeadDx = 1; p.TorsoDy = 1; p.FrontHand = new Point(38, 33); p.BackHand = new Point(36, 30); });
        Add("aim_0", 0.2f, true, aim0, With(aim0, p => { p.TorsoDy = 1; p.FrontHand = new Point(40, 30); p.BackHand = new Point(35, 30); }));
        Add("aim_30", 0.2f, true, aim30, With(aim30, p => { p.TorsoDy = 1; p.FrontHand = new Point(39, 32); p.BackHand = new Point(35, 30); }));
        Add("aim_60", 0.2f, true, aim60, With(aim60, p => { p.TorsoDy = 2; p.FrontHand = new Point(38, 34); p.BackHand = new Point(36, 31); }));
        // disparo: o virote sai, a corda bate e a besta dá um coice para trás
        Add("shoot_0", 0.06f, false, With(aim0, p => { p.Loaded = false; p.Released = true; p.FrontHand = new Point(39, 29); p.BackHand = new Point(34, 29); p.HeadDx = -1; }),
                                     With(aim0, p => { p.Loaded = false; p.Released = true; }));
        Add("shoot_30", 0.06f, false, With(aim30, p => { p.Loaded = false; p.Released = true; p.FrontHand = new Point(38, 30); p.HeadDx = -1; }),
                                      With(aim30, p => { p.Loaded = false; p.Released = true; }));
        Add("shoot_60", 0.06f, false, With(aim60, p => { p.Loaded = false; p.Released = true; p.FrontHand = new Point(37, 32); }),
                                      With(aim60, p => { p.Loaded = false; p.Released = true; }));
        // recarga: baixa a besta, puxa a corda e põe outro virote
        var low = With(rest, p => { p.BowAngle = -70f; p.Loaded = false; p.Released = true; p.FrontHand = new Point(37, 35); p.BackHand = new Point(34, 33); p.HeadDy = 1; });
        Add("reload", 0.15f, false,
            low,
            With(low, p => { p.Released = false; p.BackHand = new Point(35, 31); }),
            With(low, p => { p.Released = false; p.BackHand = new Point(33, 30); p.HeadV = "angry"; }),
            With(low, p => { p.Released = false; p.Loaded = true; p.BackHand = new Point(36, 33); }));

        var hit = With(rest, p => { p.HeadV = "hurt"; p.HeadDx = -1; p.BodyDx = -1; p.BowAngle = -60f; });
        Add("hurt", 0.1f, false, hit, With(hit, p => { p.HeadDx = 0; p.BodyDx = 0; }));
        var d1 = With(rest, p => { p.HeadV = "hurt"; p.Py = 39; p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); p.FrontHand = new Point(38, 37); p.BowAngle = -80f; p.Loaded = false; p.Released = true; });
        AddCanvas("death", 0.12f, false, Render(hit, true), Render(d1, true), Fallen(45f), Fallen(90f), Fallen(90f));
        var s0 = With(rest, p => { p.HeadV = "alert"; p.HeadDy = -1; p.BowAngle = -50f; });
        var s1 = With(rest, p => { p.HeadV = "hurt"; p.HeadDx = 1; p.TorsoDy = 1; p.Py = 37; p.BowAngle = -80f; p.FrontHand = new Point(38, 35); p.BackHand = new Point(35, 34); });
        var s2 = With(rest, p => { p.HeadV = "hurt"; p.HeadDx = 1; p.HeadDy = 1; p.Py = 39; p.BodyDx = 1; p.BackFoot = new Point(27, 43); p.FrontFoot = new Point(37, 43); p.Crossbow = false; p.FrontHand = new Point(39, 37); p.BackHand = null; });
        AddCanvas("death_stealth", 0.12f, false, Render(s0, true), Render(s0, true), Render(s1, true), Render(s2, true), Fallen(45f, true), Fallen(90f, true), Fallen(90f, true));
    }

    /// <summary>Virote 10×3 apontando para a direita (o jogo gira conforme a mira).</summary>
    static char[,] BoltPixels(out int w, out int h)
    {
        w = 12; h = 5;
        var px = new char[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) px[x, y] = '.';
        for (int x = 1; x <= 8; x++) px[x, 2] = 'W';                  // haste
        px[9, 2] = 'D'; px[10, 2] = 'd'; px[9, 1] = 'D'; px[9, 3] = 'D'; // ponta
        px[1, 1] = 'P'; px[2, 1] = 'P'; px[1, 3] = 'P'; px[2, 3] = 'P'; // penas
        // contorno
        var copy = (char[,])px.Clone();
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (copy[x, y] != '.') continue;
                bool n = (x > 0 && copy[x - 1, y] != '.') || (x < w - 1 && copy[x + 1, y] != '.') || (y > 0 && copy[x, y - 1] != '.') || (y < h - 1 && copy[x, y + 1] != '.');
                if (n) px[x, y] = 'o';
            }
        return px;
    }

    // ------------------------------------------------------------- saída

    const string ScriptGuid = "5e7a1c0d9b8f4e2a8c3d6f1b2a4e7c90"; // CharacterAnimationSet.cs
    public const string AssetGuid = "c7e3a1f05b9d4c2e8a6f0d1b3c5e7a92";

    static string Guid(string key)
    {
        using (var md5 = MD5.Create())
        {
            var b = md5.ComputeHash(Encoding.UTF8.GetBytes("setevidas-templar:" + key));
            var sb = new StringBuilder();
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    static Bitmap ToBitmap(char[,] px, int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) bmp.SetPixel(x, y, px[x, y] == '.' ? Color.Transparent : Pal[px[x, y]]);
        return bmp;
    }

    public const string CrossbowAssetGuid = "a2f4c6e8b0d24f6a9c1e3b5d7f9a1c46";
    public const string ZombieAssetGuid = "d4b8f2a6c0e14b7d9f3a5c7e1b9d2f48";
    public const string BoltSpriteGuidKey = "bolt/enm_bolt.png";

    public static string Run(string projectRoot, string metaTemplatePath, string previewPath)
    {
        InitPalette();
        string template = File.ReadAllText(metaTemplatePath);

        // lanceiro (Cão Templário)
        Archer = false;
        BuildClips();
        int spear = WriteSet(projectRoot, template, "Templar", "enm_templar_", "TemplarAnimationSet", AssetGuid);
        if (!string.IsNullOrEmpty(previewPath)) Preview(previewPath, 6);

        // atirador (Cão Sentinela Atirador)
        Archer = true;
        BuildCrossbowClips();
        int bow = WriteSet(projectRoot, template, "TemplarCrossbow", "enm_archer_", "CrossbowTemplarAnimationSet", CrossbowAssetGuid);
        if (!string.IsNullOrEmpty(previewPath)) Preview(Path.Combine(Path.GetDirectoryName(previewPath), "atirador_animacoes.png"), 6);

        // virote
        int bw, bh; var bolt = BoltPixels(out bw, out bh);
        string boltPath = Path.Combine(projectRoot, "Assets/_Game/Art/Enemies/TemplarCrossbow/enm_bolt.png");
        Directory.CreateDirectory(Path.GetDirectoryName(boltPath));
        using (var bmp = ToBitmap(bolt, bw, bh)) bmp.Save(boltPath, ImageFormat.Png);
        if (!File.Exists(boltPath + ".meta"))
            File.WriteAllText(boltPath + ".meta", template.Replace("__GUID__", Guid(BoltSpriteGuidKey)).Replace("__ALIGN__", "0").Replace("__PIVOTY__", "0.5"));
        Archer = false;

        // Templário Zumbi (depois do tomo)
        Zombie = true;
        BuildZombieClips();
        int zombie = WriteSet(projectRoot, template, "TemplarZombie", "enm_zombie_", "ZombieTemplarAnimationSet", ZombieAssetGuid);
        if (!string.IsNullOrEmpty(previewPath)) Preview(Path.Combine(Path.GetDirectoryName(previewPath), "zumbi_animacoes.png"), 6);
        Zombie = false;
        return "lanceiro: " + spear + " frames, atirador: " + bow + " frames, zumbi: " + zombie + " frames";
    }

    /// <summary>Grava os frames de <see cref="Clips"/> e o asset CharacterAnimationSet. Retorna o número de frames.</summary>
    static int WriteSet(string projectRoot, string template, string folder, string filePrefix, string assetName, string assetGuid)
    {
        string art = Path.Combine(projectRoot, "Assets/_Game/Art/Enemies/" + folder);
        var y = new StringBuilder();
        y.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
        y.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n");
        y.Append("  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
        y.Append("  m_Script: {fileID: 11500000, guid: " + ScriptGuid + ", type: 3}\n  m_Name: " + assetName + "\n  m_EditorClassIdentifier: \n  clips:\n");
        int frames = 0;
        foreach (var c in Clips)
        {
            y.Append("  - name: " + c.Name + "\n    frameTime: " + c.FrameTime.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n    loop: " + (c.Loop ? 1 : 0) + "\n    frames:\n");
            for (int i = 0; i < c.Frames.Count; i++)
            {
                // "run" do atirador reaproveita os frames de "walk": mesmo arquivo
                string clipFolder = c.Name == "run" && filePrefix == "enm_archer_" ? "walk" : c.Name;
                string rel = clipFolder + "/" + filePrefix + clipFolder + "_" + i + ".png";
                string path = Path.Combine(art, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var bmp = ToBitmap(c.Frames[i].Px, W, H)) bmp.Save(path, ImageFormat.Png);
                string gu = Guid(filePrefix == "enm_templar_" ? rel : filePrefix == "enm_zombie_" ? "zombie/" + rel : "archer/" + rel);
                File.WriteAllText(path + ".meta", template.Replace("__GUID__", gu).Replace("__ALIGN__", "9").Replace("__PIVOTY__", "0"));
                y.Append("    - sprite: {fileID: 21300000, guid: " + gu + ", type: 3}\n      overlay: {fileID: 0}\n");
                y.Append("      tail: {x: 0, y: 0}\n      chest: {x: 0, y: 14}\n      trim: {x: 0, y: 0}\n      body: {x: 0, y: 0}\n      hasTail: 0\n      hasTrim: 0\n");
                frames++;
            }
        }
        string assetPath = Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Enemies/" + assetName + ".asset");
        File.WriteAllText(assetPath, y.ToString());
        if (!File.Exists(assetPath + ".meta"))
            File.WriteAllText(assetPath + ".meta", "fileFormatVersion: 2\nguid: " + assetGuid + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
        return frames;
    }

    public static void Preview(string path, int S)
    {
        if (Pal.Count == 0) InitPalette();
        if (Clips.Count == 0) { if (Zombie) BuildZombieClips(); else if (Archer) BuildCrossbowClips(); else BuildClips(); }
        int cols = 1; foreach (var c in Clips) cols = Math.Max(cols, c.Frames.Count);
        const int x0 = 4, y0 = 8, cw = 60, ch = 36;
        using (var bmp = new Bitmap(cols * (cw * S + 6), Clips.Count * (ch * S + 18)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 10))
        {
            g.Clear(ColorTranslator.FromHtml("#1e1b2c"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < Clips.Count; r++)
            {
                int oy = r * (ch * S + 18);
                g.DrawString(Clips[r].Name, font, Brushes.White, 2, oy);
                for (int i = 0; i < Clips[r].Frames.Count; i++)
                {
                    int ox = i * (cw * S + 6);
                    using (var b = ToBitmap(Clips[r].Frames[i].Px, W, H))
                        g.DrawImage(b, new Rectangle(ox, oy + 16, cw * S, ch * S), new Rectangle(x0, y0, cw, ch), GraphicsUnit.Pixel);
                    g.DrawLine(Pens.DimGray, ox, oy + 16 + (H - y0) * S, ox + cw * S, oy + 16 + (H - y0) * S);
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
