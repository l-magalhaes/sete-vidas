// Koda, o Dourado (Golden Retriever, Paladino): sprites do chefe/companheiro e retratos.
// Ficha: SeteVidasDocs/Personagens.md §2. Canvas 72×56, pivot no centro da borda de baixo (pés na linha 55).
// C# 5. Compilado junto com PixelKit.cs por Tools/CharacterSprites/gerar.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;

public static class KodaSpriteGen
{
    public const int W = 72, H = 56;

    public static void InitPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1c26");                                                    // contorno
        PixelKit.P('G', "#e0a84e"); PixelKit.P('g', "#b97a2e"); PixelKit.P('L', "#f2cf8a"); PixelKit.P('h', "#8a5a26"); // pelo
        PixelKit.P('K', "#3a2410"); PixelKit.P('w', "#ffffff"); PixelKit.P('N', "#2a1a10"); // olho, brilho, nariz
        PixelKit.P('I', "#d4d9e4"); PixelKit.P('i', "#9aa1b0"); PixelKit.P('J', "#626a7c"); // aço
        PixelKit.P('P', "#ece8de"); PixelKit.P('p', "#bdb6a6");                         // sobreveste
        PixelKit.P('Y', "#e0b04a"); PixelKit.P('y', "#a87a22");                         // ouro da Ordem
        PixelKit.P('B', "#5a3a24");                                                    // couro
        PixelKit.P('W', "#6b4a2b"); PixelKit.P('X', "#8c6640");                         // cabo da marreta
        PixelKit.P('E', "#fff4c2"); PixelKit.P('e', "#ffd34d");                         // luz sagrada
        PixelKit.P('D', "#c9c2b0"); PixelKit.P('d', "#e6e0d0");                         // poeira
        PixelKit.P('n', "#d98a8a");                                                    // língua / boca
        PixelKit.P('R', "#a33a3a");                                                    // arranhão (retrato ferido)
    }

    // ---------------------------------------------------------------- partes

    // Tronco (18×12): ombreiras de aço, sobreveste branca com borda dourada e a pata dourada, cinto, tassetes.
    static readonly string[] Torso = {
        "....JIIIIIIIIJ....",
        "..JIIIIIIIIIIIIJ..",
        ".JIIIYYYYYYYYIIIJ.",
        "JIIIYPPPPPPPPYIIIJ",
        "JiiiYPPYPYPYPYiiiJ",
        ".JiiYPPPYYYPPYiiJ.",
        "..JJYPPPYYYPPYJJ..",
        "....YPPPPPPPPY....",
        "....BBBBYBBBBB....",
        "....IiPPPPPPiI....",
        "....IiPPP.PPiI....",
        "....JJppp.ppJJ....",
    };

    /// <summary>Cabeça de Golden Retriever olhando para a direita, numa caixa 17×12 a partir de (hx, hy).</summary>
    static void Head(PixelCanvas cv, int hx, int hy, string v)
    {
        // crânio
        PixelKit.Ellipse(cv, hx + 7.5f, hy + 6.5f, 6.8f, 6.2f, (dx, dy) => dx < -0.45f ? 'g' : (dy < -0.55f && dx > -0.2f ? 'L' : 'G'));
        // focinho largo e claro
        PixelKit.Ellipse(cv, hx + 13.2f, hy + 8.2f, 3.9f, 2.7f, (dx, dy) => dy > 0.55f ? 'G' : 'L');
        cv.Force(hx + 16, hy + 7, 'N'); cv.Force(hx + 16, hy + 8, 'N'); cv.Force(hx + 15, hy + 7, 'N');
        // boca
        if (v == "hurt") { cv.Force(hx + 13, hy + 10, 'N'); cv.Force(hx + 14, hy + 10, 'n'); cv.Force(hx + 15, hy + 10, 'N'); }
        else if (v == "soft") { cv.Force(hx + 12, hy + 10, 'g'); cv.Force(hx + 13, hy + 10, 'g'); cv.Force(hx + 14, hy + 9, 'g'); }
        else { cv.Force(hx + 12, hy + 10, 'g'); cv.Force(hx + 13, hy + 10, 'g'); cv.Force(hx + 14, hy + 10, 'g'); }
        // orelha caída e franjada, por cima do lado de trás do rosto
        bool up = v == "alert";
        PixelKit.Ellipse(cv, hx + 4.2f, hy + (up ? 6.5f : 8.2f), 2.7f, up ? 3.6f : 4.6f, (dx, dy) => dx < 0.1f ? 'h' : 'g');
        cv.Force(hx + 3, hy + (up ? 10 : 12), 'h'); cv.Force(hx + 5, hy + (up ? 10 : 12), 'g');                  // franja
        // diadema dourado (o "Dourado") com uma pedra de luz
        for (int x = hx + 1; x <= hx + 13; x++)
        {
            if (cv.Get(x, hy + 3) == '.' || x < hx + 3) continue;
            cv.Force(x, hy + 3, 'Y');
            if (cv.Get(x, hy + 4) != '.') cv.Force(x, hy + 4, x == hx + 10 ? 'E' : 'y');
        }
        cv.Force(hx + 10, hy + 3, 'E');
        // olho
        int ex = hx + 10, ey = hy + 6;
        switch (v)
        {
            case "closed": case "hurt":
                cv.Force(ex - 1, ey + 1, 'K'); cv.Force(ex, ey + 1, 'K'); cv.Force(ex + 1, ey + 1, 'K');
                break;
            case "angry":
                cv.Force(ex, ey, 'K'); cv.Force(ex + 1, ey, 'K'); cv.Force(ex, ey + 1, 'K'); cv.Force(ex + 1, ey + 1, 'w');
                cv.Force(ex - 1, ey - 1, 'h'); cv.Force(ex, ey - 1, 'h'); cv.Force(ex + 1, ey, 'K'); cv.Force(ex + 2, ey, 'h');
                break;
            case "soft":
                cv.Force(ex - 1, ey + 1, 'K'); cv.Force(ex, ey, 'K'); cv.Force(ex + 1, ey + 1, 'K');               // olho sorrindo ^
                break;
            default:
                cv.Force(ex, ey, 'K'); cv.Force(ex + 1, ey, 'K'); cv.Force(ex, ey + 1, 'K'); cv.Force(ex + 1, ey + 1, 'K');
                cv.Force(ex, ey, 'w');
                cv.Force(ex - 1, ey - 1, 'g'); cv.Force(ex, ey - 1, 'g'); cv.Force(ex + 1, ey - 1, 'g');             // sobrancelha calma
                break;
        }
    }

    public class Pose
    {
        public int Py = 47, BodyDx, TorsoDy, HeadDx, HeadDy;
        public string HeadV = "n";
        public Point BackFoot = new Point(30, 55), FrontFoot = new Point(39, 55);
        public Point? BackKnee, FrontKnee;
        public Point FrontHand = new Point(45, 38);
        public Point? BackHand;
        public float HammerAngle = -88f, HammerLength = 12f, HammerBack = 3f;
        public bool NoHammer, HammerBehind;
        public int Glow;                // 0 nenhum, 1 brilho na cabeça da marreta, 2 explosão de luz no impacto
        public bool Dust;               // poeira de impacto no chão
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    static Pose With(Pose b, Action<Pose> e) { var p = b.Clone(); e(p); return p; }

    static PixelCanvas Render(Pose p)
    {
        var cv = new PixelCanvas(W, H);
        int bx = 27 + p.BodyDx, ty = p.Py - 11 + p.TorsoDy;
        int fsx = bx + 15, fsy = ty + 3, bsx = bx + 3, bsy = ty + 3;

        if (!p.NoHammer && p.HammerBehind) Hammer(cv, p);
        if (p.BackHand.HasValue) Arm(cv, bsx, bsy, p.BackHand.Value, false);
        Leg(cv, bx + 6, p.Py + 1, p.BackKnee, p.BackFoot, true);
        Leg(cv, bx + 11, p.Py + 1, p.FrontKnee, p.FrontFoot, false);
        // cauda franjada saindo por trás das tassetes
        cv.Set(bx + 3, ty + 9, 'g'); cv.Set(bx + 2, ty + 10, 'G'); cv.Set(bx + 1, ty + 10, 'G'); cv.Set(bx + 1, ty + 11, 'g'); cv.Set(bx, ty + 11, 'L'); cv.Set(bx + 2, ty + 11, 'g');
        cv.Stamp(Torso, bx, ty);
        Head(cv, bx + 1 + p.HeadDx, ty - 11 + p.HeadDy, p.HeadV);
        if (!p.NoHammer && !p.HammerBehind) Hammer(cv, p);
        Arm(cv, fsx, fsy, p.FrontHand, true);
        PixelKit.Outline(cv, 'o', "Ee");
        if (p.Glow > 0) GlowAt(cv, p);
        if (p.Dust) DustAt(cv, p);
        return cv;
    }

    static void Leg(PixelCanvas cv, int hx, int hy, Point? knee, Point foot, bool back)
    {
        char a = back ? 'i' : 'I', b = back ? 'J' : 'i';
        Point k = knee ?? new Point((hx + foot.X) / 2, (hy + foot.Y - 1) / 2);
        PixelKit.Thick(cv, hx, hy, k.X, k.Y, 3, (t, j) => j == 0 ? a : b);
        PixelKit.Thick(cv, k.X, k.Y, foot.X, foot.Y - 1, 3, (t, j) => j == 0 ? a : b);
        cv.Set(k.X, k.Y, 'Y');                                                        // joelheira dourada
        for (int x = -1; x <= 3; x++) cv.Set(foot.X + x, foot.Y, 'J');                 // bota
        cv.Set(foot.X - 1, foot.Y - 1, 'J'); cv.Set(foot.X + 2, foot.Y - 1, 'J');
    }

    static void Arm(PixelCanvas cv, int sx, int sy, Point hand, bool front)
    {
        char a = front ? 'I' : 'i', b = front ? 'i' : 'J';
        PixelKit.Thick(cv, sx, sy, hand.X, hand.Y, 3, (t, j) => j == 0 ? a : b);
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) cv.Set(hand.X + x, hand.Y + y, front ? 'J' : 'J'); // manopla
        cv.Set(hand.X - 1, hand.Y - 1, front ? 'i' : 'J');
    }

    static void HammerHeadCenter(Pose p, out float cx, out float cy)
    {
        double a = p.HammerAngle * Math.PI / 180.0;
        float u = p.HammerLength + 2.5f;
        cx = p.FrontHand.X + 0.5f + (float)Math.Cos(a) * u;
        cy = p.FrontHand.Y + 0.5f - (float)Math.Sin(a) * u;
    }

    // Marreta: cabo de madeira, pomo dourado, cabeça de aço 5×9 com faixa dourada.
    static void Hammer(PixelCanvas cv, Pose p)
    {
        float gx = p.FrontHand.X + 0.5f, gy = p.FrontHand.Y + 0.5f, L = p.HammerLength;
        PixelKit.FillRotated(cv, gx, gy, p.HammerAngle, -p.HammerBack, L, 0.95f, (u, v) => v > 0.2f ? 'X' : 'W');
        PixelKit.FillRotated(cv, gx, gy, p.HammerAngle, -p.HammerBack - 1.6f, -p.HammerBack, 1.3f, (u, v) => 'Y');
        PixelKit.FillRotated(cv, gx, gy, p.HammerAngle, L, L + 5f, 4.4f, (u, v) =>
        {
            if (Math.Abs(v) < 0.9f) return 'Y';
            if (u > L + 4.2f || u < L + 0.7f) return 'J';
            return v > 0 ? 'I' : 'i';
        });
    }

    static void GlowAt(PixelCanvas cv, Pose p)
    {
        float cx, cy; HammerHeadCenter(p, out cx, out cy);
        float r = p.Glow == 1 ? 6.5f : 10f;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                if (cv.Px[x, y] != '.') continue;
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > r || y > 55) continue;
                float n = PixelKit.Noise(x, y, p.Glow);
                if (d < r * 0.55f && n < 0.35f) cv.Px[x, y] = 'E';
                else if (n < 0.18f) cv.Px[x, y] = 'e';
            }
        // raios em cruz
        for (int k = 3; k <= (int)r; k++)
        {
            cv.Set((int)(cx + k), (int)cy, k % 2 == 0 ? 'E' : 'e'); cv.Set((int)(cx - k), (int)cy, k % 2 == 0 ? 'E' : 'e');
            if (cy - k >= 0) cv.Set((int)cx, (int)(cy - k), k % 2 == 0 ? 'E' : 'e');
        }
    }

    static void DustAt(PixelCanvas cv, Pose p)
    {
        float cx, cy; HammerHeadCenter(p, out cx, out cy);
        int x0 = (int)cx;
        int[] dx = { -8, -6, -5, -3, 3, 5, 6, 8, -7, 7 }, dy = { 0, -1, -2, -1, -1, -2, -1, 0, -3, -3 };
        for (int i = 0; i < dx.Length; i++) { cv.Set(x0 + dx[i], 55 + dy[i], i % 3 == 0 ? 'd' : 'D'); }
    }

    // ------------------------------------------------------------- clipes

    static readonly List<PixelKit.Clip> Clips = new List<PixelKit.Clip>();

    static void Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new PixelKit.Clip { Name = name, FrameTime = t, Loop = loop };
        foreach (var p in poses) c.Frames.Add(Render(p));
        Clips.Add(c);
    }

    static void BuildClips()
    {
        Clips.Clear();
        var std = new Pose();

        // guarda: marreta de cabeça para baixo à frente, as duas mãos no pomo; respira
        var guard = With(std, p => { p.FrontHand = new Point(46, 40); p.BackHand = new Point(45, 38); p.HammerAngle = -88f; p.HammerLength = 11f; });
        Add("idle", 0.25f, true, guard, guard, With(guard, p => { p.TorsoDy = 1; }), With(guard, p => { p.TorsoDy = 1; }));

        // caminhada: marreta no ombro, atrás da cabeça
        var shoulder = With(std, p => { p.FrontHand = new Point(43, 41); p.HammerAngle = 128f; p.HammerLength = 12f; p.HammerBehind = true; });
        Add("walk", 0.16f, true,
            With(shoulder, p => { p.BackFoot = new Point(28, 55); p.FrontFoot = new Point(41, 55); }),
            With(shoulder, p => { p.TorsoDy = -1; p.BackFoot = new Point(32, 54); p.FrontFoot = new Point(37, 55); }),
            With(shoulder, p => { p.BackFoot = new Point(40, 55); p.FrontFoot = new Point(29, 55); }),
            With(shoulder, p => { p.TorsoDy = -1; p.BackFoot = new Point(36, 55); p.FrontFoot = new Point(33, 54); }));

        // marretada: ergue acima da cabeça (antecipação longa), desce até o chão, recupera
        var stance = With(std, p => { p.HeadV = "angry"; p.BackFoot = new Point(28, 55); p.FrontFoot = new Point(41, 55); });
        Add("swing_windup", 0.15f, false,
            With(stance, p => { p.FrontHand = new Point(41, 31); p.BackHand = new Point(39, 33); p.HammerAngle = 100f; }),
            With(stance, p => { p.HeadDx = -1; p.BodyDx = -1; p.FrontHand = new Point(38, 29); p.BackHand = new Point(36, 31); p.HammerAngle = 135f; }));
        Add("swing_active", 0.06f, false,
            With(stance, p => { p.HeadDx = 1; p.FrontHand = new Point(48, 36); p.BackHand = new Point(45, 37); p.HammerAngle = 25f; }),
            With(stance, p => { p.HeadDx = 1; p.BodyDx = 1; p.TorsoDy = 1; p.FrontHand = new Point(50, 44); p.BackHand = new Point(47, 44); p.HammerAngle = -38f; p.Dust = true; p.FrontFoot = new Point(43, 55); }));
        Add("swing_recover", 0.15f, false,
            With(stance, p => { p.HeadDx = 1; p.TorsoDy = 1; p.FrontHand = new Point(49, 44); p.BackHand = new Point(46, 44); p.HammerAngle = -42f; }),
            With(stance, p => { p.HeadV = "n"; p.FrontHand = new Point(46, 41); p.BackHand = new Point(44, 40); p.HammerAngle = -70f; }));

        // salto: agacha com a marreta baixa atrás, sobe com ela acima da cabeça, cai com impacto de luz
        var crouch = With(std, p => { p.HeadV = "angry"; p.Py = 50; p.BackKnee = new Point(29, 52); p.FrontKnee = new Point(42, 51); p.BackFoot = new Point(29, 55); p.FrontFoot = new Point(40, 55); });
        Add("leap_windup", 0.12f, false,
            With(crouch, p => { p.FrontHand = new Point(40, 47); p.HammerAngle = 205f; p.HammerLength = 11f; p.HammerBehind = true; }),
            With(crouch, p => { p.Py = 51; p.HeadDy = 1; p.FrontHand = new Point(39, 48); p.HammerAngle = 215f; p.HammerLength = 11f; p.HammerBehind = true; }));
        var air = With(std, p => { p.HeadV = "angry"; p.Py = 44; p.BackKnee = new Point(30, 49); p.FrontKnee = new Point(42, 48); p.BackFoot = new Point(29, 52); p.FrontFoot = new Point(40, 51); });
        Add("leap_air", 0.12f, true,
            With(air, p => { p.FrontHand = new Point(40, 26); p.BackHand = new Point(38, 28); p.HammerAngle = 98f; p.HammerLength = 11f; }),
            With(air, p => { p.FrontHand = new Point(39, 25); p.BackHand = new Point(37, 27); p.HammerAngle = 108f; p.HammerLength = 11f; }));
        var land = With(crouch, p => { p.FrontHand = new Point(50, 46); p.BackHand = new Point(47, 46); p.HammerAngle = -22f; p.HammerLength = 11f; });
        Add("leap_recover", 0.1f, false,
            With(land, p => { p.Glow = 2; p.Dust = true; }),
            With(land, p => { p.Glow = 1; p.Dust = true; }),
            With(land, p => { p.Py = 48; p.BackKnee = null; p.FrontKnee = null; p.BackFoot = new Point(29, 55); p.FrontFoot = new Point(40, 55); p.HeadV = "n"; }));

        // cura: marreta fincada, as duas mãos no pomo, cabeça baixa e olhos fechados (oração)
        var pray = With(guard, p => { p.HeadV = "closed"; p.HeadDy = 1; });
        Add("heal", 0.3f, true, With(pray, p => { p.Glow = 1; }), With(pray, p => { p.Glow = 1; p.TorsoDy = 1; }));

        // atordoado: a marreta pende, o corpo recua
        var dazed = With(std, p => { p.HeadV = "hurt"; p.BodyDx = -1; p.HeadDx = -1; p.HeadDy = 1; p.FrontHand = new Point(43, 45); p.HammerAngle = -62f; p.HammerLength = 11f; });
        Add("stagger", 0.2f, true, dazed, With(dazed, p => { p.HeadDx = 0; p.HeadDy = 0; }));

        // relâmpagos: a marreta erguida para o céu, chamando a chama
        var cast = With(std, p => { p.HeadV = "angry"; p.HeadDy = -1; p.FrontHand = new Point(42, 27); p.BackHand = new Point(40, 29); p.HammerAngle = 90f; p.HammerLength = 11f; });
        Add("cast", 0.12f, true, With(cast, p => { p.Glow = 1; }), With(cast, p => { p.Glow = 2; }));
        Add("lightning", 0.1f, true, With(cast, p => { p.Glow = 2; }), With(cast, p => { p.Glow = 1; p.TorsoDy = 1; }));

        // derrotado: de joelhos, marreta no chão, cabeça baixa; respira
        var kneel = With(std, p =>
        {
            p.HeadV = "closed"; p.HeadDy = 2; p.Py = 51;
            p.BackKnee = new Point(31, 55); p.BackFoot = new Point(25, 55);
            p.FrontKnee = new Point(41, 50); p.FrontFoot = new Point(41, 55);
            p.FrontHand = new Point(46, 48); p.HammerAngle = -8f; p.HammerLength = 11f;
        });
        Add("kneel", 0.4f, true, kneel, With(kneel, p => { p.TorsoDy = 1; }));

        // o golpe do selo: de joelhos, ergue a marreta e a crava no chão (cena do tomo)
        Add("seal_windup", 0.15f, false, With(kneel, p => { p.HeadV = "angry"; p.HeadDy = 0; p.FrontHand = new Point(41, 34); p.BackHand = new Point(39, 36); p.HammerAngle = 102f; }));
        Add("seal_strike", 0.12f, false,
            With(kneel, p => { p.HeadV = "angry"; p.HeadDy = 1; p.FrontHand = new Point(48, 47); p.BackHand = new Point(45, 47); p.HammerAngle = -48f; p.Glow = 2; p.Dust = true; }),
            With(kneel, p => { p.HeadV = "angry"; p.HeadDy = 1; p.FrontHand = new Point(48, 47); p.BackHand = new Point(45, 47); p.HammerAngle = -48f; p.Glow = 1; }));

        // companheiro: de pé, tranquilo, sorriso nos olhos
        var friend = With(guard, p => { p.HeadV = "soft"; });
        Add("companion", 0.3f, true, friend, friend, With(friend, p => { p.TorsoDy = 1; }), With(friend, p => { p.TorsoDy = 1; p.HeadV = "n"; }));
    }

    // ------------------------------------------------------------- retratos

    // Retrato 48×48 olhando para a direita (é espelhado: Koda fala do lado direito, olhando para o Lion).
    static PixelCanvas Portrait(string expr)
    {
        var cv = new PixelCanvas(48, 48);
        // ombreiras e sobreveste
        PixelKit.Ellipse(cv, 24f, 50f, 24f, 12f, (dx, dy) => dx < -0.55f ? 'J' : (dx > 0.55f ? 'i' : 'I'));
        for (int y = 40; y < 48; y++) for (int x = 15; x <= 33; x++) cv.Force(x, y, (x == 15 || x == 33) ? 'Y' : 'P');
        for (int x = 15; x <= 33; x++) cv.Force(x, 40, 'Y');
        // a pata dourada no peito
        cv.Force(22, 43, 'Y'); cv.Force(24, 42, 'Y'); cv.Force(26, 43, 'Y');
        for (int x = 23; x <= 25; x++) { cv.Force(x, 45, 'Y'); cv.Force(x, 46, 'Y'); }
        // pescoço / peito de pelo
        PixelKit.Ellipse(cv, 25f, 35f, 10.5f, 6.5f, (dx, dy) => dy < -0.2f ? (char)76 : (char)71);
        // orelha de trás (lado esquerdo, atrás da cabeça)
        PixelKit.Ellipse(cv, 12.5f, 25f, 5f, 10f, (dx, dy) => dx < 0.2f ? 'h' : 'g');
        // cabeça
        PixelKit.Ellipse(cv, 23f, 20f, 13f, 12f, (dx, dy) => dx < -0.55f ? 'g' : (dy < -0.6f && dx > -0.3f ? 'L' : 'G'));
        // focinho largo
        PixelKit.Ellipse(cv, 33f, 25.5f, 8f, 5.5f, (dx, dy) => dy > 0.55f ? 'G' : 'L');
        // nariz
        PixelKit.Ellipse(cv, 39.5f, 22.5f, 2.6f, 1.9f, 'N'); cv.Force(39, 21, 'g');
        // orelha da frente caída, com franja, sobre a bochecha
        PixelKit.Ellipse(cv, 15f, 27f, 5.2f, 9.5f, (dx, dy) => dx < -0.2f ? 'h' : 'g');
        cv.Force(12, 36, 'h'); cv.Force(14, 37, 'g'); cv.Force(17, 36, 'g');
        if (expr == "ferido") { cv.Force(11, 33, 'h'); cv.Force(19, 34, 'g'); }
        // diadema dourado com a pedra de luz
        for (int x = 11; x <= 33; x++)
        {
            int yy = 11 + (int)(Math.Abs(x - 22) * 0.12f);
            if (cv.Get(x, yy) == '.') continue;
            cv.Force(x, yy, 'Y'); cv.Force(x, yy + 1, 'y');
        }
        cv.Force(28, 11, 'E'); cv.Force(29, 11, 'E'); cv.Force(28, 12, 'e'); cv.Force(29, 12, 'e');

        // olhos (o de trás menor)
        Eye(cv, 21, 19, expr, true);
        Eye(cv, 30, 19, expr, false);

        // boca
        switch (expr)
        {
            case "sorriso":
                for (int x = 29; x <= 37; x++) cv.Force(x, 30 + (x < 33 ? 0 : (x - 33) / 2 > 1 ? -1 : 0), 'N');
                cv.Force(34, 31, 'n'); cv.Force(35, 31, 'n');
                break;
            case "ferido":
                for (int x = 30; x <= 36; x++) cv.Force(x, 30, 'N'); cv.Force(32, 31, 'n'); cv.Force(33, 31, 'n');
                cv.Force(17, 15, 'R'); cv.Force(18, 16, 'R'); cv.Force(19, 17, 'R');                       // arranhão
                break;
            default:
                for (int x = 30; x <= 36; x++) cv.Force(x, 30, 'N');
                if (expr == "neutro") cv.Force(37, 29, 'N');                                              // meio sorriso sereno
                break;
        }
        PixelKit.Outline(cv, 'o', "Ee");
        return cv;
    }

    static void Eye(PixelCanvas cv, int cx, int cy, string expr, bool back)
    {
        int w = back ? 2 : 3;
        if (expr == "sorriso" || expr == "ferido")
        {
            // fechados: arco ^ (sorriso) ou traço baixo (cansado)
            for (int x = cx - w / 2 - 1; x <= cx + w / 2 + 1; x++)
                cv.Force(x, expr == "sorriso" ? cy + (Math.Abs(x - cx) > w / 2 ? 1 : 0) : cy + 1, 'K');
            if (expr == "ferido") for (int x = cx - w / 2 - 1; x <= cx + w / 2 + 1; x++) cv.Force(x, cy - 1, 'g');
            return;
        }
        for (int x = 0; x < w; x++) for (int y = 0; y < 3; y++) cv.Force(cx - 1 + x, cy - 1 + y, 'K');
        cv.Force(cx - 1, cy - 1, 'w');
        if (expr == "serio")
        {
            // sobrancelha firme, descendo para o focinho
            for (int x = -2; x <= w; x++) cv.Force(cx - 1 + x, cy - 3 + (x > w / 2 ? 1 : 0), 'h');
            for (int x = 0; x < w; x++) cv.Force(cx - 1 + x, cy - 1, x == 0 ? 'w' : 'K');
        }
        else
        {
            for (int x = -1; x <= w; x++) cv.Force(cx - 1 + x, cy - 3, 'g');                              // sobrancelha calma
            cv.Force(cx - 1, cy + 2, 'g'); cv.Force(cx, cy + 2, 'g');                                    // olhar caloroso
        }
    }

    static readonly string[][] PortraitList = {
        new[] { "portrait_golden", "neutro" },
        new[] { "portrait_koda_serious", "serio" },
        new[] { "portrait_koda_hurt", "ferido" },
        new[] { "portrait_koda_smile", "sorriso" },
    };

    // ------------------------------------------------------------- saída

    public const string AssetGuid = "d4b2e6a8c1f3459b8e0a2c4d6f8b1e35";

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        BuildClips();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string result = PixelKit.WriteAnimationSet(Clips, System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Characters/Koda"), "chr_koda",
            System.IO.Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Characters/KodaAnimationSet.asset"), "KodaAnimationSet", AssetGuid, template);
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        string pdir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/UI/Portraits");
        foreach (var p in PortraitList)
        {
            var cv = Portrait(p[1]).Mirror();
            PixelKit.WriteSprite(cv, System.IO.Path.Combine(pdir, p[0] + ".png"), "portrait/" + p[0], template, false);
            portraits.Add(cv); names.Add(p[1]);
        }
        if (!string.IsNullOrEmpty(previewDir))
        {
            PixelKit.PreviewClips(Clips, System.IO.Path.Combine(previewDir, "koda_animacoes.png"), 5, new Rectangle(4, 0, 68, 56), "#1e1b2c");
            PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(previewDir, "koda_retratos.png"), 6);
        }
        return "Koda: " + result + ", retratos: " + portraits.Count;
    }

    public static void Preview(string dir)
    {
        InitPalette();
        BuildClips();
        PixelKit.PreviewClips(Clips, System.IO.Path.Combine(dir, "koda_animacoes.png"), 5, new Rectangle(4, 0, 68, 56), "#1e1b2c");
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        foreach (var p in PortraitList) { portraits.Add(Portrait(p[1]).Mirror()); names.Add(p[1]); }
        PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(dir, "koda_retratos.png"), 6);
    }
}
