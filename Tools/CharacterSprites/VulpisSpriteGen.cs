// A Vulpis: raposa caçadora (feminina), guardiã da Floresta. Sprites, animações e retratos.
// Visual (pedido do usuário, 17/11/2026, com referência): capuz e capa verdes de caçadora com debrum e broche dourados,
// orelhas de raposa saindo do capuz, pelo laranja com bochechas e peito brancos, cílios, túnica e cinto de couro com
// bolsa, botas marrons, cauda grande de ponta branca, o arco nas costas (a lanterna saiu: pedido do usuário, 17/11/2026). A magia dela é verde
// clara (magia da floresta): a flecha carregada brilha.
// Canvas 48×48, pivô no meio da borda de baixo, olhando para a direita (o jogo espelha).
// C# 5. Compilado junto com PixelKit.cs por Tools/CharacterSprites/gerar.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;

public static class VulpisSpriteGen
{
    public const int W = 48, H = 48;
    const int Ground = 46;

    public static void InitPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1310");                                                    // contorno
        PixelKit.P('F', "#e2792c"); PixelKit.P('f', "#b0531c"); PixelKit.P('l', "#f6a552"); // pelo
        PixelKit.P('W', "#f7f0e2"); PixelKit.P('w', "#d8cbb6");                         // branco (bochecha, peito, ponta da cauda)
        PixelKit.P('k', "#3a2016");                                                    // pontas das orelhas, nariz
        PixelKit.P('E', "#24150f"); PixelKit.P('x', "#ffffff"); PixelKit.P('p', "#f08a78"); // olho, brilho do olho, bochecha corada
        PixelKit.P('G', "#3f8a3e"); PixelKit.P('g', "#2b5e2b"); PixelKit.P('h', "#62ad4f"); PixelKit.P('d', "#16301a"); // capuz e capa
        PixelKit.P('Y', "#e8b844"); PixelKit.P('y', "#a87a22");                         // ouro (debrum, broche, fivela)
        PixelKit.P('B', "#7c4a28"); PixelKit.P('b', "#4e2c18"); PixelKit.P('T', "#9a6a3c"); // couro, túnica
        PixelKit.P('P', "#5a3a26");                                                    // calça
        PixelKit.P('I', "#34343e"); PixelKit.P('i', "#5c5c6a");                         // ferro da lanterna
        PixelKit.P('L', "#ffd772"); PixelKit.P('Q', "#fff6c8"); PixelKit.P('O', "#ff9f3a"); // luz da lanterna
        PixelKit.P('R', "#94602f"); PixelKit.P('r', "#5e3a1a"); PixelKit.P('s', "#ece4cc"); // arco e corda
        PixelKit.P('M', "#8ff28a"); PixelKit.P('N', "#e2ffd8"); PixelKit.P('m', "#3cbf5a"); // magia da floresta
        PixelKit.P('v', "#a8a8a8");                                                    // ponta da flecha comum
    }

    public class Pose
    {
        public int Dy;                 // o corpo desce (respiração, agachar)
        public int Tail;               // cauda: -1, 0, 1
        public int Hem;                // barra da capa balançando
        public int CapeLift;           // capa levantada pelo vento (no ar), 0..3
        public string Legs = "stand";  // stand, crouch, tuck, fall, run0..run5
        public string Arm = "lantern"; // lantern, bow, sad
        public float Aim;              // arco: graus (0 = para a frente, negativo = para baixo)
        public float Pull;             // corda puxada 0..1
        public int Magic;              // brilho da flecha 0..3 (4 = o estouro do disparo)
        public bool Arrow;             // flecha na corda
        public int Lantern;            // lanterna balançando -1..1
        public int Flame;              // a chama tremendo 0..2
        public string Face = "n";      // n, blink, talk, focus, sad, smile
        public bool EarsDown;
        public int Ear;                // orelha da frente mexendo (0, 1)
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    static Pose With(Pose b, Action<Pose> e) { var p = b.Clone(); e(p); return p; }

    // ------------------------------------------------------------- o corpo

    static PixelCanvas Body(Pose p, PixelCanvas over)
    {
        var cv = new PixelCanvas(W, H);
        int dy = p.Dy;
        Tail(cv, p);
        if (p.Arm != "bow") BowOnBack(cv, dy);
        Legs(cv, p);
        Cape(cv, p);
        Torso(cv, dy);
        Head(cv, p);
        if (p.Arm == "bow") Bow(cv, over, p);
        else if (p.Arm == "sad") SadArms(cv, p);
        else LanternArm(cv, p);
        return cv;
    }

    /// <summary>Cauda grande e cheia, atrás, de ponta branca, balançando.</summary>
    static void Tail(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy;
        // a curva da base (no quadril) até a ponta, subindo para trás
        float[][] pts = {
            new float[] { 20f, 39f, 2.2f }, new float[] { 17.5f, 38f, 3.0f }, new float[] { 15f, 36f, 3.4f },
            new float[] { 13.2f, 33.2f, 3.4f }, new float[] { 12.3f + p.Tail * 0.5f, 30.2f, 3.0f }, new float[] { 12.4f + p.Tail * 1.0f, 27.6f, 2.2f },
        };
        if (p.Legs == "tuck" || p.Legs == "fall")   // no ar, a cauda fica para trás e para baixo
            pts = new[] {
                new float[] { 20f, 39f, 2.2f }, new float[] { 17f, 39.5f, 3.0f }, new float[] { 14f, 39.6f, 3.3f },
                new float[] { 11.2f, 38.6f, 3.2f }, new float[] { 8.8f, 37.4f + p.Tail * 0.6f, 2.8f }, new float[] { 7f, 36.2f + p.Tail, 2.0f },
            };
        for (int i = 0; i < pts.Length; i++)
        {
            var q = pts[i];
            bool tip = i >= pts.Length - 2;
            int ii = i;
            PixelKit.Ellipse(cv, q[0], q[1] + dy * (i < 3 ? 1 : 0.5f), q[2], q[2] * 0.95f, (a, b) =>
            {
                if (tip && (ii == pts.Length - 1 || a < -0.1f || b < -0.3f)) return b > 0.4f ? 'w' : 'W';
                if (b > 0.45f) return 'f';
                if (b < -0.5f && a > -0.3f) return 'l';
                return 'F';
            });
        }
    }

    static void BowOnBack(PixelCanvas cv, int dy)
    {
        // a ponta de cima do arco aparecendo atrás do ombro
        int[][] bow = { new[] { 21, 27 }, new[] { 20, 28 }, new[] { 19, 29 }, new[] { 19, 30 }, new[] { 19, 31 }, new[] { 19, 32 } };
        foreach (var b in bow) cv.Set(b[0], b[1] + dy, b[1] == 27 ? 'r' : 'R');
        cv.Set(20, 30 + dy, 's'); cv.Set(20, 31 + dy, 's');
    }

    static void Legs(PixelCanvas cv, Pose p)
    {
        // pernas: (x do quadril, x do pé, altura do pé acima do chão, joelho para a frente)
        int bx0 = 22, fx0 = 26;
        int bFoot = 0, fFoot = 0, bLift = 0, fLift = 0, top = 41 + p.Dy;
        switch (p.Legs)
        {
            case "crouch": bFoot = -2; fFoot = 2; break;
            case "tuck": bFoot = -3; fFoot = 2; bLift = 4; fLift = 3; break;
            case "fall": bFoot = -1; fFoot = 2; bLift = 2; fLift = 1; break;
            case "run0": bFoot = -4; fFoot = 3; bLift = 1; break;
            case "run1": bFoot = -2; fFoot = 1; bLift = 3; break;
            case "run2": bFoot = 0; fFoot = -1; fLift = 3; break;
            case "run3": bFoot = 3; fFoot = -4; fLift = 1; break;
            case "run4": bFoot = 1; fFoot = -2; fLift = 3; break;
            case "run5": bFoot = -1; fFoot = 0; bLift = 3; break;
        }
        Leg(cv, bx0, top, bx0 + bFoot, Ground - bLift, true);
        Leg(cv, fx0, top, fx0 + fFoot, Ground - fLift, false);
    }

    static void Leg(PixelCanvas cv, int hipX, int hipY, int footX, int footY, bool back)
    {
        char pants = back ? 'b' : 'P';
        if (footY - hipY < 3) footY = hipY + 3;
        // coxa e canela (uma linha de 2 px), a bota nos últimos 2 px e o bico para a frente
        int kneeX = (hipX + footX) / 2 + 1, kneeY = (hipY + footY) / 2;
        PixelKit.Thick(cv, hipX, hipY, kneeX, kneeY, 2, (t, j) => pants);
        PixelKit.Thick(cv, kneeX, kneeY, footX, footY - 1, 2, (t, j) => t > 0.55f ? (back ? 'b' : 'B') : pants);
        cv.Force(footX, footY, back ? 'b' : 'B'); cv.Force(footX + 1, footY, back ? 'b' : 'B'); cv.Force(footX + 2, footY, 'b');
        cv.Force(footX, footY - 1, back ? 'b' : 'B'); cv.Force(footX + 1, footY - 1, back ? 'b' : 'B');
        if (!back) cv.Force(footX, footY - 2, 'Y');   // fivela da bota
    }

    /// <summary>A capa: sino verde caindo dos ombros até o joelho, de barra dourada; no ar, levanta para trás.</summary>
    static void Cape(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy, top = 30 + dy, bottom = 42 + dy;
        for (int y = top; y <= bottom; y++)
        {
            float k = (y - top) / (float)(bottom - top);
            int back = 20 - (int)(k * 4.2f), front = 27 + (int)(k * 3.2f);
            int hem = y > top + 6 ? p.Hem : 0;
            for (int x = back; x <= front; x++)
            {
                char c = 'G';
                if (x <= back + 1) c = 'g';
                else if (x == front) c = 'h';
                if ((x == back + 3 || x == back + 6) && y > top + 4) c = 'g';     // dobras
                if (y == bottom) c = x % 2 == 0 ? 'Y' : 'y';                        // barra dourada
                cv.Set(x + hem, y, c);
            }
        }
        // no ar: a barra de trás voa para cima e para trás
        for (int i = 0; i < p.CapeLift; i++)
            for (int x = 14 - i * 2; x <= 19; x++)
            {
                int y = bottom - 2 - i * 2 - (19 - x) / 2;
                cv.Set(x, y, x == 14 - i * 2 ? 'y' : (i == p.CapeLift - 1 ? 'h' : 'G'));
                cv.Set(x, y + 1, 'g');
            }
    }

    static void Torso(PixelCanvas cv, int dy)
    {
        // a frente aberta da capa: túnica, cinto e a bolsa
        for (int y = 33; y <= 40; y++)
            for (int x = 24; x <= 27; x++)
                cv.Force(x, y + dy, y >= 37 ? 'T' : (x == 24 ? 'T' : 'W'));               // peito branco por cima da túnica
        for (int x = 23; x <= 28; x++) cv.Force(x, 37 + dy, x == 26 ? 'Y' : 'B');      // cinto com a fivela
        cv.Force(26, 38 + dy, 'y');
        // bolsa de couro no quadril de trás
        cv.Force(21, 37 + dy, 'B'); cv.Force(22, 37 + dy, 'B'); cv.Force(21, 38 + dy, 'B'); cv.Force(22, 38 + dy, 'b'); cv.Force(21, 39 + dy, 'b'); cv.Force(22, 39 + dy, 'b');
        // o broche dourado prendendo a capa no pescoço
        cv.Force(26, 31 + dy, 'Y'); cv.Force(27, 31 + dy, 'Y'); cv.Force(26, 32 + dy, 'y'); cv.Force(27, 32 + dy, 'Y');
    }

    /// <summary>Cabeça no capuz: focinho fino para a direita, olho grande com cílios, orelhas saindo do capuz.</summary>
    static void Head(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy + (p.Face == "sad" ? 1 : 0);
        int fx = p.Face == "sad" ? 0 : 0;
        // capuz: domo com a ponta caindo para trás
        PixelKit.Ellipse(cv, 23.5f, 25.5f + dy, 6.2f, 6.6f, (a, b) => a < -0.55f ? 'g' : (b < -0.6f && a > -0.4f && a < 0.5f ? 'h' : 'G'));
        PixelKit.Thick(cv, 19, 29 + dy, 17, 32 + dy, 2, (t, j) => j == 0 ? 'G' : 'g');   // a ponta do capuz caindo nas costas
        // orelhas de raposa saindo por fendas do capuz
        int earY = p.EarsDown ? 2 : 0;
        Ear(cv, 21 - (p.EarsDown ? 1 : 0), 18 + dy + earY, false, p.EarsDown);
        Ear(cv, 26 + p.Ear - (p.EarsDown ? 1 : 0), 17 + dy + earY + p.Ear, true, p.EarsDown);
        // o rosto dentro do capuz (abertura escura em volta)
        PixelKit.Ellipse(cv, 27.2f, 26f + dy, 4.6f, 4.6f, (a, b) => a < -0.6f || b < -0.75f ? 'd' : 'F');
        PixelKit.Ellipse(cv, 27.6f, 26.3f + dy, 3.6f, 3.6f, (a, b) => b > 0.35f ? 'W' : (b < -0.5f ? 'l' : 'F'));
        // focinho comprido e fino: laranja em cima, branco embaixo
        for (int x = 30; x <= 34; x++)
        {
            int len = 34 - x;
            cv.Force(x + fx, 26 + dy, len >= 1 ? 'F' : 'k');
            cv.Force(x + fx, 27 + dy, x <= 33 ? 'W' : 'k');
            if (x <= 32) cv.Force(x + fx, 28 + dy, 'W');
            if (x <= 31) cv.Force(x + fx, 25 + dy, 'l');
        }
        cv.Force(35 + fx, 26 + dy, 'k');                                                 // a ponta do nariz
        if (p.Face == "talk") { cv.Force(32, 28 + dy, 'k'); cv.Force(31, 29 + dy, 'w'); cv.Force(32, 29 + dy, 'k'); }
        else if (p.Face == "smile") { cv.Force(32, 28 + dy, 'k'); cv.Force(31, 28 + dy, 'W'); }
        else cv.Force(32, 28 + dy, 'w');                                                // a linha da boca
        cv.Force(27, 28 + dy, 'p');                                                      // bochecha corada
        // o olho grande, com brilho e cílios (para cima e para trás)
        int ex = 28, ey = 24 + dy;
        switch (p.Face)
        {
            case "blink":
                cv.Force(ex - 1, ey + 1, 'E'); cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'E'); cv.Force(ex + 2, ey, 'E');
                break;
            case "smile":   // olho fechado de contente (^)
                cv.Force(ex - 1, ey + 1, 'E'); cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey + 1, 'E'); cv.Force(ex + 2, ey, 'E');
                break;
            case "focus":   // a mira: o olho estreito, a sobrancelha baixa
                cv.Force(ex - 1, ey, 'k'); cv.Force(ex, ey, 'k'); cv.Force(ex + 1, ey, 'k');
                cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'x'); cv.Force(ex + 2, ey, 'E');
                break;
            case "sad":     // olhando para baixo, a pálpebra pesada
                cv.Force(ex - 1, ey, 'f'); cv.Force(ex, ey, 'f'); cv.Force(ex + 1, ey, 'f');
                cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'E'); cv.Force(ex + 2, ey + 1, 'E');
                break;
            default:
                cv.Force(ex, ey, 'x'); cv.Force(ex + 1, ey, 'E');
                cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'E');
                cv.Force(ex + 2, ey - 1, 'E'); cv.Force(ex + 2, ey, 'E');                // cílios
                break;
        }
    }

    static void Ear(PixelCanvas cv, int x, int y, bool front, bool down)
    {
        // triângulo de 3 de largura e 4 de altura, ponta escura e o miolo claro
        if (down)
        {
            // deitada para trás
            cv.Force(x - 2, y + 2, 'k'); cv.Force(x - 1, y + 2, 'F'); cv.Force(x, y + 2, 'F'); cv.Force(x + 1, y + 2, 'F');
            cv.Force(x - 1, y + 3, 'F'); cv.Force(x, y + 3, front ? 'l' : 'f'); cv.Force(x + 1, y + 3, 'F'); cv.Force(x + 2, y + 3, 'F');
            return;
        }
        cv.Force(x + 1, y, 'k');
        cv.Force(x, y + 1, 'k'); cv.Force(x + 1, y + 1, 'k'); cv.Force(x + 2, y + 1, front ? 'F' : 'f');
        cv.Force(x, y + 2, 'F'); cv.Force(x + 1, y + 2, front ? 'l' : 'f'); cv.Force(x + 2, y + 2, 'F');
        cv.Force(x - 1, y + 3, 'F'); cv.Force(x, y + 3, 'F'); cv.Force(x + 1, y + 3, front ? 'l' : 'F'); cv.Force(x + 2, y + 3, 'F'); cv.Force(x + 3, y + 3, 'F');
    }

    /// <summary>
    /// O braço da frente solto ao lado do corpo, a mão de luva balançando um pouco (<see cref="Pose.Lantern"/> é o balanço).
    /// Sem lanterna (pedido do usuário, 17/11/2026: "pode remover a lanterninha da mão dela").
    /// </summary>
    static void LanternArm(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy, sw = p.Lantern;
        PixelKit.Thick(cv, 28, 33 + dy, 29 + sw, 37 + dy, 2, (t, j) => j == 0 ? 'h' : 'G');   // manga
        cv.Force(29 + sw, 38 + dy, 'B'); cv.Force(30 + sw, 38 + dy, 'B'); cv.Force(30 + sw, 37 + dy, 'b'); // luva
    }

    static void SadArms(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy;
        // os braços juntos na frente, as mãos paradas
        PixelKit.Thick(cv, 28, 33 + dy, 28, 38 + dy, 2, (t, j) => j == 0 ? 'h' : 'G');
        cv.Force(28, 39 + dy, 'B'); cv.Force(29, 39 + dy, 'B');
    }

    /// <summary>Lanterna de ferro com a luz amarela (alça em <paramref name="x"/>, <paramref name="y"/>).</summary>
    static void Lantern(PixelCanvas cv, int x, int y, int flame, bool lit)
    {
        cv.Force(x, y, 'I');
        cv.Force(x - 1, y + 1, 'I'); cv.Force(x, y + 1, 'i'); cv.Force(x + 1, y + 1, 'I');
        for (int j = 2; j <= 4; j++)
        {
            cv.Force(x - 1, y + j, 'I'); cv.Force(x + 1, y + j, 'I');
            char c = !lit ? (j == 4 ? 'O' : 'y') : (j == 3 - (flame == 1 ? 1 : 0) ? 'Q' : (j == 4 ? 'O' : 'L'));
            cv.Force(x, y + j, c);
        }
        cv.Force(x - 1, y + 5, 'I'); cv.Force(x, y + 5, 'i'); cv.Force(x + 1, y + 5, 'I');
        if (lit && flame == 2) cv.Force(x, y + 2, 'Q');
    }

    static void LanternAtBelt(PixelCanvas cv, Pose p)
    {
        // a lanterna pendurada no cinto, atrás do quadril (as mãos estão no arco)
        int dy = p.Dy, x = 22, y = 38 + dy;
        cv.Set(x, y, 'I');
        cv.Set(x - 1, y + 1, 'I'); cv.Set(x, y + 1, 'L'); cv.Set(x + 1, y + 1, 'I');
        cv.Set(x - 1, y + 2, 'I'); cv.Set(x, y + 2, 'O'); cv.Set(x + 1, y + 2, 'I');
    }

    /// <summary>
    /// O arco na mão da frente, mirando em <see cref="Pose.Aim"/>; a outra mão puxa a corda. Com
    /// <see cref="Pose.Magic"/>, a flecha brilha em verde claro (magia da floresta) e junta luz em volta da ponta.
    /// </summary>
    static void Bow(PixelCanvas body, PixelCanvas cv, Pose p)
    {
        int dy = p.Dy;
        double a = p.Aim * Math.PI / 180.0;
        float dx = (float)Math.Cos(a), dyv = (float)-Math.Sin(a);           // y para baixo
        float px = -dyv, py = dx;                                             // perpendicular (para baixo quando mira em frente)
        float sx = 27f, sy = 32f + dy;                                        // ombro da frente
        float hx = sx + dx * 8f, hy = sy + dyv * 8f;                        // a mão do arco
        float pull = 1.5f + p.Pull * 5f;
        float qx = hx - dx * pull, qy = hy - dyv * pull;                     // a mão da corda
        // o peito por baixo dos braços (sem isso o contorno fechava um buraco preto entre o queixo e o braço)
        PixelKit.Ellipse(body, 28.5f, 32.5f + dy, 2.6f, 2.4f, (u, v) => u < -0.3f ? 'g' : 'G');
        // braço de trás até a corda, e o da frente esticado
        PixelKit.Thick(body, 24, 33 + dy, (int)Math.Round(qx), (int)Math.Round(qy), 2, (t, j) => j == 0 ? 'G' : 'g');
        // o arco: os braços curvam para trás nas pontas
        float bend = 2f + p.Pull * 1.5f;
        var tips = new PointF[2];
        for (int s = -1; s <= 1; s += 2)
        {
            PointF prev = new PointF(hx, hy);
            for (int i = 1; i <= 14; i++)
            {
                float t = i / 14f;
                float x = hx + px * 7f * t * s - dx * bend * t * t;
                float y = hy + py * 7f * t * s - dyv * bend * t * t;
                foreach (var q in PixelKit.Line((int)Math.Round(prev.X), (int)Math.Round(prev.Y), (int)Math.Round(x), (int)Math.Round(y)))
                    cv.Force(q.X, q.Y, t > 0.85f ? 'r' : 'R');
                prev = new PointF(x, y);
            }
            tips[(s + 1) / 2] = prev;
        }
        // a corda
        foreach (var tip in tips)
            foreach (var q in PixelKit.Line((int)Math.Round(tip.X), (int)Math.Round(tip.Y), (int)Math.Round(qx), (int)Math.Round(qy)))
                if (cv.Get(q.X, q.Y) != 'R' && cv.Get(q.X, q.Y) != 'r') cv.Force(q.X, q.Y, 's');
        // a flecha
        if (p.Arrow)
        {
            float ax = hx + dx * 3.5f, ay = hy + dyv * 3.5f;
            var line = PixelKit.Line((int)Math.Round(qx), (int)Math.Round(qy), (int)Math.Round(ax), (int)Math.Round(ay));
            for (int i = 0; i < line.Count; i++)
            {
                bool head = i >= line.Count - 2;
                char c = p.Magic > 0 ? (head ? 'N' : 'M') : (head ? 'v' : 'R');
                cv.Force(line[i].X, line[i].Y, c);
            }
            if (p.Magic > 0)
            {
                // a ponta em folha brilhante e a luz que se junta em volta
                int cx = (int)Math.Round(ax), cy = (int)Math.Round(ay);
                cv.Force(cx + (int)Math.Round(px), cy + (int)Math.Round(py), 'M');
                cv.Force(cx - (int)Math.Round(px), cy - (int)Math.Round(py), 'M');
                // um brilho redondo em volta da ponta (cresce com a carga) e uns pontinhos subindo ao longo da flecha
                int r = p.Magic;
                for (int u = -r; u <= r; u++)
                    for (int v = -r; v <= r; v++)
                    {
                        int d = Math.Abs(u) + Math.Abs(v);
                        if (d > r || (d == r && r > 1 && (u == 0 || v == 0) == false)) continue;   // losango, cantos vazados
                        if (cv.Get(cx + u, cy + v) == '.' || d < r) cv.Force(cx + u, cy + v, d <= r / 2 ? 'N' : (d == r ? 'm' : 'M'));
                    }
                if (r >= 2) { cv.Force(cx + r + 1, cy, 'M'); cv.Force(cx - r - 1, cy, 'm'); cv.Force(cx, cy - r - 1, 'M'); cv.Force(cx, cy + r + 1, 'm'); }
                cv.Force(cx, cy, 'N');
                for (int i = 0; i < p.Magic * 2; i++)
                {
                    float k = 0.25f + 0.25f * i;
                    int x = (int)Math.Round(qx + (ax - qx) * (k % 1f) + px * ((i % 2) * 2 - 1) * 1.5f), y = (int)Math.Round(qy + (ay - qy) * (k % 1f) + py * ((i % 2) * 2 - 1) * 1.5f) - i / 2;
                    if (cv.Get(x, y) == '.') cv.Force(x, y, i % 2 == 0 ? 'M' : 'm');
                }
            }
        }
        if (p.Magic >= 4)
        {
            // o estouro do disparo: anel de luz verde na frente do arco
            int cx = (int)Math.Round(hx + dx * 5f), cy = (int)Math.Round(hy + dyv * 5f);
            PixelKit.Ellipse(cv, cx + 0.5f, cy + 0.5f, 4.2f, 4.2f, (u, v) => { float d = u * u + v * v; return d > 0.55f ? (PixelKit.Noise((int)(u * 9), (int)(v * 9), 2) > 0.35f ? 'M' : '.') : (d > 0.2f ? '.' : 'N'); });
        }
        // a mão do arco (luva) e a da corda
        cv.Force((int)Math.Round(hx), (int)Math.Round(hy), 'B');
        cv.Force((int)Math.Round(qx), (int)Math.Round(qy), 'B');
        // a manga da frente por cima de tudo
        PixelKit.Thick(body, (int)sx, (int)sy, (int)Math.Round(sx + dx * 6f), (int)Math.Round(sy + dyv * 6f), 2, (t, j) => j == 0 ? 'h' : 'G');
    }

    static PixelCanvas Render(Pose p)
    {
        var over = new PixelCanvas(W, H);
        var cv = Body(p, over);
        PixelKit.Outline(cv, 'o', "MNmQL");
        cv.Paste(over);   // o arco, a corda e a flecha por cima, sem contorno (o contorno enchia o vão da corda)
        return cv;
    }

    // ------------------------------------------------------------- as animações

    static readonly List<PixelKit.Clip> Clips = new List<PixelKit.Clip>();

    static void Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new PixelKit.Clip { Name = name, FrameTime = t, Loop = loop };
        foreach (var p in poses) c.Frames.Add(Render(p));
        Clips.Add(c);
    }

    /// <summary>O ângulo da flecha forte na arena: para baixo e para a frente (ela pula da plataforma alta).</summary>
    public const float LeapAim = -32f;

    static void BuildClips()
    {
        Clips.Clear();
        var std = new Pose();

        // parada: respira, a cauda e a capa balançam, a lanterna balança e a chama treme; pisca de vez em quando
        Add("idle", 0.2f, true,
            std,
            With(std, p => { p.Flame = 1; p.Tail = 1; }),
            With(std, p => { p.Dy = 1; p.Tail = 1; p.Lantern = 1; p.Hem = 1; }),
            With(std, p => { p.Dy = 1; p.Flame = 2; p.Lantern = 1; p.Hem = 1; p.Ear = 1; }),
            With(std, p => { p.Tail = 0; p.Flame = 1; }),
            With(std, p => { p.Tail = -1; p.Face = "blink"; }),
            With(std, p => { p.Dy = 1; p.Tail = -1; p.Hem = -1; p.Flame = 2; }),
            With(std, p => { p.Dy = 1; p.Tail = 0; p.Hem = -1; p.Lantern = -1; }));

        // falando
        Add("talk", 0.13f, true,
            With(std, p => { p.Face = "talk"; }),
            With(std, p => { p.Flame = 1; }),
            With(std, p => { p.Face = "talk"; p.Dy = 1; p.Ear = 1; p.Flame = 2; }),
            With(std, p => { p.Dy = 1; p.Tail = 1; }));

        // triste (a guardiã): orelhas baixas, cabeça baixa, a lanterna quase apagando
        Add("sad", 0.4f, true,
            With(std, p => { p.Face = "sad"; p.EarsDown = true; p.Arm = "sad"; p.Tail = -1; }),
            With(std, p => { p.Face = "sad"; p.EarsDown = true; p.Arm = "sad"; p.Tail = -1; p.Dy = 1; }));

        // contente (sorriso de olho fechado)
        Add("smile", 0.3f, true,
            With(std, p => { p.Face = "smile"; p.Ear = 1; }),
            With(std, p => { p.Face = "smile"; p.Dy = 1; p.Tail = 1; p.Flame = 1; }));

        // pronta para atirar (na luta, esperando a hora): o arco baixo com a flecha na corda, a magia verde juntando na
        // ponta; respira, a cauda e a capa balançam, a orelha mexe e ela olha de relance (pedido do usuário, 17/11/2026:
        // "ela está muito parada")
        var ready = With(std, p => { p.Arm = "bow"; p.Aim = -38f; p.Pull = 0.2f; p.Arrow = true; p.Magic = 1; p.Face = "focus"; });
        Add("ready", 0.16f, true,
            ready,
            With(ready, p => { p.Tail = 1; p.Magic = 2; }),
            With(ready, p => { p.Dy = 1; p.Tail = 1; p.Hem = 1; p.Pull = 0.3f; }),
            With(ready, p => { p.Dy = 1; p.Ear = 1; p.Magic = 2; p.Hem = 1; p.Pull = 0.3f; }),
            With(ready, p => { p.Tail = 0; p.Magic = 1; }),
            With(ready, p => { p.Tail = -1; p.Face = "n"; p.Magic = 2; }),
            With(ready, p => { p.Dy = 1; p.Tail = -1; p.Hem = -1; p.Face = "n"; p.Pull = 0.3f; }),
            With(ready, p => { p.Dy = 1; p.Tail = 0; p.Hem = -1; p.Magic = 2; p.Pull = 0.3f; }));

        // correndo
        var run = With(std, p => { p.Face = "focus"; });
        Add("run", 0.08f, true,
            With(run, p => { p.Legs = "run0"; p.Hem = -1; p.Tail = 1; p.Lantern = -1; }),
            With(run, p => { p.Legs = "run1"; p.Dy = -1; p.Hem = -1; p.Tail = 0; }),
            With(run, p => { p.Legs = "run2"; p.Hem = 0; p.Tail = -1; p.Lantern = 1; }),
            With(run, p => { p.Legs = "run3"; p.Hem = -1; p.Tail = 1; p.Lantern = -1; }),
            With(run, p => { p.Legs = "run4"; p.Dy = -1; p.Hem = -1; p.Tail = 0; }),
            With(run, p => { p.Legs = "run5"; p.Hem = 0; p.Tail = -1; p.Lantern = 1; }));

        // atirando do chão (para a frente e um pouco para baixo)
        var aim = With(std, p => { p.Arm = "bow"; p.Aim = -20f; p.Face = "focus"; p.Arrow = true; });
        Add("shoot", 0.09f, false,
            With(aim, p => { p.Pull = 0f; }),
            With(aim, p => { p.Pull = 0.5f; }),
            With(aim, p => { p.Pull = 1f; p.Magic = 1; }),
            With(aim, p => { p.Pull = 1f; p.Magic = 2; p.Dy = 1; }),
            With(aim, p => { p.Pull = 0f; p.Arrow = false; p.Magic = 4; p.Hem = -1; }),
            With(aim, p => { p.Pull = 0f; p.Arrow = false; }));

        // a flecha forte (a cena da arena): agacha, pula, puxa no ar juntando a magia, solta e cai
        Add("crouch", 0.07f, false,
            With(std, p => { p.Legs = "crouch"; p.Dy = 2; p.Face = "focus"; p.Tail = 1; }),
            With(std, p => { p.Legs = "crouch"; p.Dy = 3; p.Face = "focus"; p.Tail = 1; p.Hem = 1; }));
        Add("leap", 0.08f, false,
            With(std, p => { p.Legs = "tuck"; p.Dy = -1; p.CapeLift = 2; p.Face = "focus"; }),
            With(std, p => { p.Legs = "tuck"; p.Dy = -1; p.CapeLift = 3; p.Face = "focus"; p.Tail = 1; }));
        var air = With(std, p => { p.Legs = "tuck"; p.Dy = -1; p.CapeLift = 2; p.Arm = "bow"; p.Aim = LeapAim; p.Face = "focus"; p.Arrow = true; });
        Add("air_draw", 0.11f, false,
            With(air, p => { p.Pull = 0.2f; p.Magic = 1; }),
            With(air, p => { p.Pull = 0.55f; p.Magic = 2; p.CapeLift = 3; }),
            With(air, p => { p.Pull = 0.85f; p.Magic = 3; p.Tail = 1; }),
            With(air, p => { p.Pull = 1f; p.Magic = 3; p.CapeLift = 3; p.Tail = 1; }));
        Add("air_release", 0.07f, false,
            With(air, p => { p.Pull = 0f; p.Arrow = false; p.Magic = 4; p.CapeLift = 3; p.Hem = -1; }),
            With(air, p => { p.Pull = 0f; p.Arrow = false; p.CapeLift = 3; p.Tail = -1; }),
            With(air, p => { p.Pull = 0f; p.Arrow = false; p.CapeLift = 2; p.Face = "n"; }));
        Add("fall", 0.1f, true,
            With(std, p => { p.Legs = "fall"; p.CapeLift = 2; p.Lantern = -1; }),
            With(std, p => { p.Legs = "fall"; p.CapeLift = 3; p.Lantern = -1; p.Tail = 1; }));
        Add("land", 0.08f, false,
            With(std, p => { p.Legs = "crouch"; p.Dy = 3; p.Hem = 1; }),
            With(std, p => { p.Legs = "crouch"; p.Dy = 1; }));
    }

    // ------------------------------------------------------------- retratos

    // 48×48, olhando para a direita (espelhado depois: a Vulpis fala do lado direito da tela).
    static PixelCanvas Portrait(string expr)
    {
        var cv = new PixelCanvas(48, 48);
        bool sad = expr == "triste";
        // ombros da capa e o broche
        PixelKit.Ellipse(cv, 22f, 52f, 24f, 13f, (a, b) => a < -0.5f ? 'g' : (a > 0.65f ? 'h' : 'G'));
        for (int x = 2; x < 44; x++) { int y = 41 + (int)(Math.Abs(x - 22) * 0.12f); if (cv.Get(x, y) != '.') cv.Force(x, y, x % 2 == 0 ? 'Y' : 'y'); }
        PixelKit.Ellipse(cv, 30f, 45f, 2.2f, 2.2f, (a, b) => b < -0.2f ? 'Y' : 'y'); cv.Force(29, 44, 'x');
        // o peito branco na abertura
        PixelKit.Ellipse(cv, 31f, 47f, 5f, 5f, (a, b) => 'W');
        // capuz: domo grande, com a ponta caindo atrás
        PixelKit.Thick(cv, 9, 30, 3, 38, 4, (t, j) => j < 2 ? 'G' : 'g');
        PixelKit.Ellipse(cv, 20f, 24f, 15f, 16f, (a, b) => a < -0.6f ? 'g' : (b < -0.7f && a > -0.4f && a < 0.5f ? 'h' : 'G'));
        // orelhas saindo do capuz
        int ed = sad ? 4 : 0;
        PortraitEar(cv, 14 - (sad ? 2 : 0), 3 + ed, sad, false);
        PortraitEar(cv, 25 - (sad ? 1 : 0), 1 + ed, sad, true);
        // a abertura do capuz e o rosto
        PixelKit.Ellipse(cv, 27f, 26f, 11.5f, 12f, (a, b) => a < -0.65f || b < -0.8f ? 'd' : 'F');
        PixelKit.Ellipse(cv, 28f, 27f, 10f, 10.5f, (a, b) =>
        {
            if (b > 0.25f && a > -0.6f) return 'W';
            if (b < -0.55f && a > -0.3f) return 'l';
            if (a < -0.7f) return 'f';
            return 'F';
        });
        // o focinho fino para a direita: laranja em cima, branco embaixo, o nariz preto
        for (int y = 24; y <= 35; y++)
            for (int x = 33; x <= 45; x++)
            {
                float k = (x - 33) / 12f;
                int topY = 24 + (int)(k * k * 4f + k * 1f), botY = 35 - (int)(k * 4.5f);
                if (y < topY || y > botY) continue;
                cv.Force(x, y, y < (topY + botY) / 2 ? (y == topY ? 'l' : 'F') : 'W');
            }
        for (int y = 28; y <= 30; y++) for (int x = 43; x <= 46; x++) if (!(x == 46 && y != 29) && !(x == 43 && y == 30)) cv.Force(x, y, 'k');
        cv.Force(44, 28, 'w');
        // a boca
        // a boca: uma linha fina saindo do nariz para trás (dentro do branco)
        if (expr == "ironico") { for (int x = 37; x <= 42; x++) cv.Force(x, 31 - (x <= 38 ? 1 : 0), 'w'); cv.Force(36, 29, 'w'); }
        else if (sad) { for (int x = 38; x <= 42; x++) cv.Force(x, 31 + (x < 40 ? 1 : 0), 'w'); }
        else if (expr == "sorriso") { for (int x = 37; x <= 42; x++) cv.Force(x, 31 + (x >= 39 && x <= 41 ? 1 : 0), 'w'); cv.Force(36, 30, 'w'); }
        else for (int x = 38; x <= 42; x++) cv.Force(x, 31, 'w');
        // bochecha corada
        cv.Force(26, 31, 'p'); cv.Force(27, 31, 'p'); cv.Force(28, 31, 'p'); cv.Force(27, 32, 'p');
        PortraitEye(cv, 30, 22, 4, expr);
        // a franja de pelo saindo do capuz
        cv.Force(20, 15, 'F'); cv.Force(21, 16, 'F'); cv.Force(22, 15, 'l'); cv.Force(23, 16, 'F'); cv.Force(24, 15, 'F'); cv.Force(21, 17, 'f'); cv.Force(23, 17, 'f');
        PixelKit.Outline(cv, 'o', "");
        return cv;
    }

    static void PortraitEar(PixelCanvas cv, int x, int y, bool down, bool front)
    {
        if (down)
        {
            for (int j = 3; j < 9; j++)
            {
                int half = j / 2;
                for (int i = -half; i <= half; i++) cv.Force(x + i - (8 - j) / 2, y + j, j < 5 ? 'k' : (Math.Abs(i) < half - 1 && front ? 'l' : (i < 0 ? 'f' : 'F')));
            }
            return;
        }
        for (int j = 0; j < 9; j++)
        {
            int half = j / 2;
            for (int i = -half; i <= half; i++)
            {
                char c = j < 3 ? 'k' : (Math.Abs(i) < half - 1 && front ? 'l' : (i < 0 ? 'f' : 'F'));
                cv.Force(x + i, y + j, c);
            }
        }
    }

    static void PortraitEye(PixelCanvas cv, int x, int y, int w, string expr)
    {
        if (expr == "sorriso")
        {
            for (int i = 0; i < w; i++) cv.Force(x + i, y + (i == 0 || i == w - 1 ? 1 : 0), 'E');
            return;
        }
        if (expr == "triste")
        {
            for (int i = -1; i <= w; i++) cv.Force(x + i, y - 2 + (i < w / 2 ? 0 : -1), 'f');   // sobrancelha subindo no meio
            for (int i = 0; i < w; i++) { cv.Force(x + i, y, 'f'); cv.Force(x + i, y + 1, 'E'); if (w > 2) cv.Force(x + i, y + 2, 'E'); }
            if (w > 2) { cv.Force(x + 1, y + 1, 'x'); cv.Force(x + w - 1, y + 3, 'x'); cv.Force(x + w - 1, y + 4, 'x'); }      // uma lágrima
            return;
        }
        bool narrow = expr == "serio" || expr == "ironico";
        int top = narrow ? 1 : 0;
        for (int yy = y + top; yy <= y + 3; yy++)
            for (int i = 0; i < w; i++) cv.Force(x + i, yy, 'E');
        cv.Force(x + (w > 2 ? 1 : 0), y + top, 'x');
        if (w > 2) cv.Force(x + 1, y + top + 1, 'x');
        // cílios saindo para cima e para trás (no olho de perto)
        if (w > 2) { cv.Force(x - 1, y + top, 'E'); cv.Force(x - 2, y + top - 1, 'E'); cv.Force(x - 1, y + top - 1, 'E'); cv.Force(x + w, y + top, 'E'); }
        if (narrow) for (int i = -1; i <= w; i++) cv.Force(x + i, y + top - 1 - (expr == "ironico" && i > w / 2 ? 1 : 0) + (expr == "serio" && i > w / 2 ? 1 : 0), 'f');
        if (expr == "ironico") for (int i = 0; i < w; i++) cv.Force(x + i, y + 1, 'f');           // a pálpebra meio baixa
    }

    static readonly string[][] PortraitList = {
        new[] { "portrait_vulpis", "neutro" },
        new[] { "portrait_vulpis_serious", "serio" },
        new[] { "portrait_vulpis_ironic", "ironico" },
        new[] { "portrait_vulpis_sad", "triste" },
        new[] { "portrait_vulpis_smile", "sorriso" },
    };

    public const string AssetGuid = "b3f1c9d27a4e4c51a8e6f0d4c2b7a913";

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        BuildClips();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string result = PixelKit.WriteAnimationSet(Clips, System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Characters/Vulpis"), "chr_vulpis",
            System.IO.Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Characters/VulpisAnimationSet.asset"), "VulpisAnimationSet", AssetGuid, template);
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        string pdir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/UI/Portraits");
        foreach (var p in PortraitList)
        {
            var cv = Portrait(p[1]).Mirror();
            PixelKit.WriteSprite(cv, System.IO.Path.Combine(pdir, p[0] + ".png"), "portrait/" + p[0], template, false);
            portraits.Add(cv); names.Add(p[1]);
        }
        if (!string.IsNullOrEmpty(previewDir)) Preview(previewDir);
        return "Vulpis: " + result + ", retratos: " + portraits.Count;
    }

    public static void Preview(string dir)
    {
        InitPalette();
        BuildClips();
        PixelKit.PreviewClips(Clips, System.IO.Path.Combine(dir, "vulpis_animacoes.png"), 6, new Rectangle(0, 0, 48, 48), "#1e2a22");
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        foreach (var p in PortraitList) { portraits.Add(Portrait(p[1]).Mirror()); names.Add(p[1]); }
        PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(dir, "vulpis_retratos.png"), 6);
    }
}
