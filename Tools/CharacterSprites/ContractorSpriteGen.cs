// O Contratante (Capivara Necromante): sprites da cena do tomo e retratos.
// Lore: sobrevivente da Guerra dos Corredores Mortos, infectado por uma praga; uma das mãos é esquelética
// (Lore Oficial §VI; Personagens.md §3). Canvas 48×48, pivot no centro da borda de baixo.
// Visual (refeito em 12/11/2026, pedido do usuário com duas referências): capivara roliça de cara emburrada,
// focinho largo com a mancha do nariz, dentro de um capuz alto de ponta dobrada com debrum dourado; manto de barra
// rasgada, cajado com garra de osso e orbe da praga flutuando, pingente verde e sombra no chão.
// C# 5. Compilado junto com PixelKit.cs por Tools/CharacterSprites/gerar.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;

public static class ContractorSpriteGen
{
    public const int W = 48, H = 48;

    public static void InitPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1420");                                                    // contorno
        PixelKit.P('F', "#b98256"); PixelKit.P('f', "#93603f"); PixelKit.P('l', "#d4a26e"); PixelKit.P('k', "#4e2f24"); // pelo
        PixelKit.P('N', "#7e4a3a"); PixelKit.P('n', "#9a5e48"); PixelKit.P('p', "#c97c68");                            // nariz, bochecha
        PixelKit.P('R', "#3e2b55"); PixelKit.P('r', "#291c3b"); PixelKit.P('q', "#5a4176"); PixelKit.P('h', "#140e1e"); // manto, fundo do capuz
        PixelKit.P('G', "#8fd14f"); PixelKit.P('Q', "#d6ff8a");                         // praga (orbe, pingente, olhos)
        PixelKit.P('Y', "#d9a441"); PixelKit.P('y', "#a8782a");                         // ouro
        PixelKit.P('U', "#e6e0d0"); PixelKit.P('u', "#b8b0a0"); PixelKit.P('x', "#7a7468"); // osso
        PixelKit.P('W', "#6b4430"); PixelKit.P('D', "#3e271c");                         // madeira do cajado
        PixelKit.P('M', "#5b2a6e"); PixelKit.P('m', "#3d1b4a");                         // o tomo
        PixelKit.P('V', "#7a3a9a"); PixelKit.P('v', "#4a1e66");                         // névoa roxa
        PixelKit.P('s', "#0c0910");                                                    // sombra no chão
        PixelKit.P('w', "#ffffff");
    }

    public class Pose
    {
        public int Dy;                              // respiração
        public int Hem;                             // barra do manto balançando (-1, 0, 1)
        public int Orb;                             // o orbe flutuando (sobe e desce)
        public Point Hand = new Point(32, 32);      // mão de osso
        public int Fingers;                         // 0 = relaxados, 1 = abertos, 2 = fechados (segurando), 3 = batendo
        public bool Tome;                           // segurando o tomo com a mão de osso
        public bool Coin;                           // moeda entre os dedos
        public string Face = "n";                   // n, grin, wide
        public float Reveal = 1f;                   // 0..1: surgindo/sumindo na névoa
        public bool Mist;                           // névoa nos pés
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    static Pose With(Pose b, Action<Pose> e) { var p = b.Clone(); e(p); return p; }

    static PixelCanvas Body(Pose p)
    {
        var cv = new PixelCanvas(W, H);
        int dy = p.Dy;
        Staff(cv, p);                                                                    // atrás de tudo
        // manto: em sino, pesado, barra rasgada em pontas; costas na sombra, frente com uma aresta de luz
        const int cx = 23;
        for (int y = 24 + dy; y <= 46; y++)
        {
            float k = (y - 24 - dy) / 22f;
            int back = (int)(6f + k * k * 9f + k * 1f), front = (int)(6f + k * 5f);   // abre mais para trás (cauda do manto)
            for (int x = cx - back; x <= cx + front; x++)
            {
                int tear = (int)(PixelKit.Noise(x, 3, 11) * 2.99f);                   // 0..2: pontas da barra
                if (x < cx - back + 4) tear += (cx - back + 4 - x);                    // a cauda sobe na ponta de trás
                if (y > 46 - tear) continue;
                char c = 'R';
                if (x < cx - back + 3 + (int)(k * 3f)) c = 'r';
                else if (x == cx + front) c = 'q';
                if ((x == cx - 4 || x == cx + 1) && y > 31 + dy && y < 44) c = 'r';   // dobras
                if (x == cx - 3 && y > 33 + dy && y < 42) c = 'q';
                if (x >= cx + 3 && x <= cx + 3 + (int)(k * 3f) && x < cx + front && y > 29 + dy) c = 'h'; // abertura do manto
                cv.Set(x + (y > 38 ? p.Hem : 0), y, c);
            }
        }
        // runas da praga brilhando na abertura do manto
        int[][] runes = { new[] { 26, 33 }, new[] { 27, 36 }, new[] { 26, 37 }, new[] { 27, 40 }, new[] { 28, 42 }, new[] { 14, 40 }, new[] { 15, 41 } };
        foreach (var r in runes) cv.Force(r[0] + (r[1] > 38 ? p.Hem : 0), r[1] + (r[1] > 38 ? 0 : dy), 'G');
        // capuz: domo redondo com a ponta dobrada para trás, caindo nos ombros
        PixelKit.Ellipse(cv, 21.5f, 25f + dy, 9.5f, 3.4f, (a, b) => a < -0.4f ? 'r' : 'R');
        PixelKit.Ellipse(cv, 22f, 16f + dy, 8.2f, 8.6f, (a, b) => a < -0.55f ? 'r' : (b < -0.6f && a < 0.4f && a > -0.5f ? 'q' : 'R'));
        PixelKit.Thick(cv, 17, 9 + dy, 13, 6 + dy, 2, (t, j) => j == 0 ? 'q' : 'R');       // ponta
        cv.Force(12, 6 + dy, 'R'); cv.Force(11, 7 + dy, 'R'); cv.Force(11, 8 + dy, 'r'); cv.Force(12, 7 + dy, 'R');
        // orelha redonda escapando por um rasgo no alto do capuz
        cv.Force(24, 7 + dy, 'f'); cv.Force(25, 7 + dy, 'f'); cv.Force(24, 8 + dy, 'k'); cv.Force(25, 8 + dy, 'f');
        // abertura do capuz (fundo escuro) com debrum dourado fino
        PixelKit.Ellipse(cv, 27f, 17f + dy, 6.6f, 7.4f, (a, b) =>
        {
            float d = (float)Math.Sqrt(a * a + b * b);
            if (d > 0.87f && a < 0.3f) return b < 0f ? 'Y' : 'y';
            return 'h';
        });
        // cabeça: bochechas roliças dentro do capuz
        PixelKit.Ellipse(cv, 28f, 17.5f + dy, 5.2f, 5.6f, (a, b) => a < -0.55f ? 'f' : (b < -0.55f ? 'l' : 'F'));
        // focinho: bloco largo e alto, cantos redondos, para fora do capuz
        for (int y = 13; y <= 23; y++)
            for (int x = 29; x <= 37; x++)
            {
                bool corner = (x >= 36 && (y == 13 || y == 23)) || (x == 37 && (y == 14 || y == 22));
                if (corner) continue;
                char c = y <= 14 ? 'l' : (y >= 21 ? 'f' : 'F');
                cv.Force(x, y + dy, c);
            }
        // mancha do nariz na frente do focinho, narina e o "bico" da boca
        for (int y = 14; y <= 18; y++)
            for (int x = 33; x <= 37; x++)
                if (!(x == 37 && y == 14) && !(x == 33 && (y == 14 || y == 18))) cv.Force(x, y + dy, y == 14 ? 'n' : 'N');
        cv.Force(35, 15 + dy, 'k'); cv.Force(36, 16 + dy, 'k');
        cv.Force(35, 19 + dy, 'k'); cv.Force(34, 20 + dy, 'k'); cv.Force(36, 20 + dy, 'k');
        cv.Force(29, 19 + dy, 'p'); cv.Force(30, 19 + dy, 'p');                         // bochecha
        if (p.Face == "grin")
        {
            for (int x = 30; x <= 35; x++) cv.Force(x, 20 + dy, 'k');
            cv.Force(36, 21 + dy, 'U'); cv.Force(37, 21 + dy, 'U'); cv.Force(36, 22 + dy, 'u');        // incisivos à mostra
        }
        // olho: brilho verde da praga sob a pálpebra pesada (cara emburrada)
        int ex = 30, ey = 15 + dy;
        if (p.Face == "wide")
        {
            cv.Force(ex - 1, ey - 2, 'k'); cv.Force(ex, ey - 2, 'k'); cv.Force(ex + 1, ey - 2, 'k');
            cv.Force(ex, ey - 1, 'Q'); cv.Force(ex + 1, ey - 1, 'G'); cv.Force(ex, ey, 'G'); cv.Force(ex + 1, ey, 'k');
        }
        else
        {
            cv.Force(ex - 1, ey - 2, 'k'); cv.Force(ex, ey - 1, 'k'); cv.Force(ex + 1, ey - 1, 'k'); cv.Force(ex + 2, ey - 1, 'k');
            cv.Force(ex, ey, 'Q'); cv.Force(ex + 1, ey, 'G');
        }
        // corrente de ouro com o pingente da praga
        for (int x = 18; x <= 29; x++) cv.Force(x, 26 + dy + (Math.Abs(x - 23) < 3 ? 1 : 0), x % 2 == 0 ? 'Y' : 'y');
        cv.Force(23, 28 + dy, 'Y'); cv.Force(24, 28 + dy, 'Y');
        cv.Force(22, 29 + dy, 'Y'); cv.Force(23, 29 + dy, 'Q'); cv.Force(24, 29 + dy, 'G'); cv.Force(25, 29 + dy, 'y');
        cv.Force(23, 30 + dy, 'G'); cv.Force(24, 30 + dy, 'G'); cv.Force(23, 31 + dy, 'y'); cv.Force(24, 31 + dy, 'y');
        // manga e mão de osso
        Arm(cv, new Point(28, 28 + dy), p);
        return cv;
    }

    /// <summary>Cajado na mão de trás: madeira, garra de osso no alto e o orbe da praga flutuando dentro dela.</summary>
    static void Staff(PixelCanvas cv, Pose p)
    {
        int dy = p.Dy, sx = 9;
        for (int y = 9 + dy; y <= 46; y++)
        {
            cv.Set(sx, y, 'W'); cv.Set(sx - 1, y, 'D');
            if (y == 20 + dy || y == 37) cv.Set(sx + 1, y, 'W');                         // nós da madeira
        }
        // braço de trás (manga na sombra) estendido até o cajado
        PixelKit.Thick(cv, 17, 27 + dy, 12, 29 + dy, 3, (t, j) => j == 0 ? 'R' : 'r');
        // garra de osso: dois dedos curvos que seguram o orbe
        int oy = 5 + dy;
        int[][] claw = { new[] { -3, 3 }, new[] { -4, 2 }, new[] { -4, 1 }, new[] { -4, 0 }, new[] { -3, -1 },
                         new[] { 2, 3 }, new[] { 3, 2 }, new[] { 3, 1 }, new[] { 3, 0 }, new[] { 2, -1 },
                         new[] { -2, 4 }, new[] { -1, 4 }, new[] { 0, 4 }, new[] { 1, 4 } };
        foreach (var c in claw) cv.Set(sx + c[0], oy + c[1], c[1] == 4 || c[0] > 0 ? 'u' : 'U');
        // a mão de pelo (de trás) segurando o cajado
        cv.Set(sx, 29 + dy, 'F'); cv.Set(sx + 1, 29 + dy, 'F'); cv.Set(sx, 30 + dy, 'f'); cv.Set(sx + 1, 30 + dy, 'F'); cv.Set(sx - 1, 30 + dy, 'f');
        // orbe flutuando
        int ocx = sx, ocy = oy + 1 - p.Orb;
        PixelKit.Ellipse(cv, ocx + 0.5f, ocy + 0.5f, 2.6f, 2.6f, (a, b) => (a < -0.1f && b < -0.1f) ? 'Q' : 'G');
        cv.Force(ocx - 1, ocy - 1, 'w');
    }

    static void Arm(PixelCanvas cv, Point shoulder, Pose p)
    {
        var h = p.Hand;
        PixelKit.Thick(cv, shoulder.X, shoulder.Y, h.X - 1, h.Y, 3, (t, j) => t > 0.8f ? 'q' : (j == 0 ? 'q' : 'R'));   // manga
        // punho escuro da manga e palma de osso 3×3 (vãos entre os ossos)
        cv.Force(h.X - 1, h.Y - 1, 'r'); cv.Force(h.X - 1, h.Y, 'r'); cv.Force(h.X - 1, h.Y + 1, 'r');
        for (int x = 0; x < 3; x++) for (int y = -1; y <= 1; y++) cv.Force(h.X + x, h.Y + y, x == 0 ? 'u' : 'U');
        cv.Force(h.X + 1, h.Y, 'x');
        // três dedos longos, falanges com juntas escuras
        switch (p.Fingers)
        {
            case 1: // abertos, para a frente (o do meio mais longo)
                for (int i = -1; i <= 1; i += 2)
                    for (int k = 0; k < 4; k++) cv.Force(h.X + 3 + k, h.Y + i, k == 1 ? 'x' : 'U');
                cv.Force(h.X + 3, h.Y, 'U');
                cv.Force(h.X + 3, h.Y - 2, 'u');                                                        // polegar
                break;
            case 2: // fechados (segurando)
                for (int k = 0; k < 3; k++) cv.Force(h.X + k, h.Y + 2, k == 1 ? 'x' : 'U');
                cv.Force(h.X + 3, h.Y, 'U'); cv.Force(h.X + 3, h.Y + 1, 'x'); cv.Force(h.X + 3, h.Y - 1, 'U');
                break;
            case 3: // batendo: o dedo do meio levantado
                for (int k = 0; k < 3; k++) { cv.Force(h.X, h.Y + 2 + k, k == 1 ? 'x' : 'U'); cv.Force(h.X + 2, h.Y + 2 + k, k == 1 ? 'x' : 'U'); }
                cv.Force(h.X + 3, h.Y + 1, 'U'); cv.Force(h.X + 4, h.Y + 1, 'U');
                break;
            default: // relaxados, pendendo como garras
                for (int k = 0; k < 3; k++) { cv.Force(h.X, h.Y + 2 + k, k == 1 ? 'x' : 'U'); cv.Force(h.X + 2, h.Y + 2 + k, k == 1 ? 'x' : 'U'); }
                break;
        }
        if (p.Tome)
        {
            // o tomo preso contra o peito pela mão de osso
            for (int x = -3; x <= 1; x++) for (int y = -4; y <= 1; y++) cv.Force(h.X + x, h.Y + y, x == -3 || y == 1 ? 'm' : 'M');
            cv.Force(h.X - 1, h.Y - 2, 'G'); cv.Force(h.X, h.Y - 3, 'G');
            cv.Force(h.X + 2, h.Y - 1, 'U'); cv.Force(h.X + 2, h.Y, 'x');
        }
        if (p.Coin) { cv.Force(h.X + 3, h.Y - 2, 'Y'); cv.Force(h.X + 4, h.Y - 2, 'Y'); cv.Force(h.X + 3, h.Y - 3, 'y'); cv.Force(h.X + 4, h.Y - 3, 'Y'); }
    }

    /// <summary>Corpo com contorno; surgindo/sumindo, os pixels aparecem por ruído e o resto vira névoa roxa.</summary>
    static PixelCanvas Render(Pose p, int seed)
    {
        var body = Body(p);
        PixelKit.Outline(body, 'o', "GQw");
        if (p.Reveal >= 1f && !p.Mist) { Shadow(body, 1f); return body; }
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = body.Px[x, y];
                if (c == '.') continue;
                // revela de baixo para cima, com ruído
                float n = PixelKit.Noise(x, y, 7) * 0.6f + (1f - y / 48f) * 0.4f;
                if (n < p.Reveal) cv.Px[x, y] = c;
                else if (n < p.Reveal + 0.25f) cv.Px[x, y] = PixelKit.Noise(x, y, seed) < 0.5f ? 'V' : (PixelKit.Noise(x, y, seed + 1) < 0.4f ? 'v' : '.');
            }
        if (p.Mist)
            for (int x = 8; x < 40; x++)
                for (int y = 40; y < 48; y++)
                {
                    float d = Math.Abs(x - 23.5f) / 16f + (47 - y) / 9f;
                    float n = PixelKit.Noise(x, y, seed + 3);
                    if (cv.Px[x, y] == '.' && n > d) cv.Px[x, y] = n > d + 0.35f ? 'V' : 'v';
                }
        Shadow(cv, p.Reveal);
        return cv;
    }

    /// <summary>Sombra oval no chão, embaixo do manto (encolhe enquanto ele surge ou some).</summary>
    static void Shadow(PixelCanvas cv, float size)
    {
        if (size < 0.2f) return;
        float rx = 6f + 9f * size;
        for (int x = 0; x < W; x++)
            for (int y = 45; y < 48; y++)
            {
                float a = (x + 0.5f - 23.5f) / rx, b = (y + 0.5f - 46.5f) / 1.6f;
                if (a * a + b * b <= 1f && cv.Px[x, y] == '.') cv.Px[x, y] = 's';
            }
    }

    static readonly List<PixelKit.Clip> Clips = new List<PixelKit.Clip>();

    static void Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new PixelKit.Clip { Name = name, FrameTime = t, Loop = loop };
        for (int i = 0; i < poses.Length; i++) c.Frames.Add(Render(poses[i], i * 13 + name.Length));
        Clips.Add(c);
    }

    static void BuildClips()
    {
        Clips.Clear();
        var std = new Pose();

        // surge numa névoa roxa, de baixo para cima
        Add("appear", 0.07f, false,
            With(std, p => { p.Reveal = 0.05f; p.Mist = true; }),
            With(std, p => { p.Reveal = 0.3f; p.Mist = true; }),
            With(std, p => { p.Reveal = 0.55f; p.Mist = true; }),
            With(std, p => { p.Reveal = 0.8f; p.Mist = true; }),
            With(std, p => { p.Reveal = 1f; p.Mist = true; }));

        // parado: respira, o manto balança, os dedos de osso batem devagar e o orbe flutua
        Add("idle", 0.25f, true,
            std,
            With(std, p => { p.Fingers = 3; p.Orb = 1; }),
            With(std, p => { p.Dy = 1; p.Hem = 1; p.Orb = 1; }),
            With(std, p => { p.Dy = 1; p.Hem = 1; p.Fingers = 3; }));

        // sorriso de deboche (falas da revelação)
        Add("gloat", 0.3f, true,
            With(std, p => { p.Face = "grin"; p.Hand = new Point(31, 24); p.Fingers = 1; }),
            With(std, p => { p.Face = "grin"; p.Hand = new Point(31, 25); p.Fingers = 1; p.Dy = 1; }));

        // estende a mão de osso, agarra o tomo e o puxa contra o peito
        Add("take", 0.12f, false,
            With(std, p => { p.Hand = new Point(36, 33); p.Fingers = 1; p.Face = "wide"; }),
            With(std, p => { p.Hand = new Point(39, 36); p.Fingers = 1; p.Face = "wide"; }),
            With(std, p => { p.Hand = new Point(37, 34); p.Fingers = 2; p.Tome = true; p.Face = "grin"; }),
            With(std, p => { p.Hand = new Point(30, 31); p.Fingers = 2; p.Tome = true; p.Face = "grin"; }));

        // com o tomo
        var hold = With(std, p => { p.Hand = new Point(30, 31); p.Fingers = 2; p.Tome = true; });
        Add("hold", 0.25f, true, hold, hold, With(hold, p => { p.Dy = 1; p.Hem = 1; }), With(hold, p => { p.Dy = 1; p.Hem = 1; }));

        // joga a moeda: o tomo no braço, a moeda aparece entre os dedos e sai num peteleco
        Add("toss", 0.09f, false,
            With(hold, p => { p.Face = "grin"; }),
            With(std, p => { p.Hand = new Point(33, 27); p.Fingers = 2; p.Coin = true; p.Face = "grin"; }),
            With(std, p => { p.Hand = new Point(36, 24); p.Fingers = 1; p.Face = "grin"; }),
            With(hold, p => { p.Face = "grin"; }));

        // some na névoa, levando o tomo
        Add("vanish", 0.07f, false,
            With(hold, p => { p.Reveal = 1f; p.Mist = true; }),
            With(hold, p => { p.Reveal = 0.75f; p.Mist = true; }),
            With(hold, p => { p.Reveal = 0.5f; p.Mist = true; }),
            With(hold, p => { p.Reveal = 0.25f; p.Mist = true; }),
            With(hold, p => { p.Reveal = 0f; p.Mist = true; }));
    }

    // ------------------------------------------------------------- retratos

    // 48×48 em três quartos, olhando para a direita (espelhado depois: o contratante fala do lado direito).
    // Capuz alto de ponta dobrada e debrum dourado emoldurando a cara emburrada; mancha larga do nariz com a boca em "人".
    static PixelCanvas Portrait(string expr)
    {
        var cv = new PixelCanvas(48, 48);
        // ombros do manto
        PixelKit.Ellipse(cv, 22f, 53f, 25f, 13f, (a, b) => a < -0.45f ? 'r' : (a > 0.7f ? 'q' : 'R'));
        // capuz: domo grande, ponta caindo para trás
        PixelKit.Thick(cv, 11, 9, 4, 3, 3, (t, j) => j == 0 ? 'q' : 'R');
        cv.Force(2, 3, 'R'); cv.Force(2, 4, 'r'); cv.Force(3, 2, 'R');
        PixelKit.Ellipse(cv, 21f, 23f, 17f, 19f, (a, b) => a < -0.6f ? 'r' : (b < -0.75f && a < 0.4f && a > -0.55f ? 'q' : 'R'));
        // orelha redonda escapando por um rasgo no alto do capuz
        PixelKit.Ellipse(cv, 28f, 5f, 2.6f, 2.2f, (a, b) => (a > -0.3f && b > -0.1f) ? 'k' : 'f');
        // abertura do capuz (fundo escuro) com debrum dourado fino
        PixelKit.Ellipse(cv, 29f, 25f, 13.5f, 15.5f, (a, b) =>
        {
            float d = (float)Math.Sqrt(a * a + b * b);
            if (d > 0.91f && a < 0.35f) return b < 0f ? 'Y' : 'y';
            return 'h';
        });
        // cabeça roliça
        PixelKit.Ellipse(cv, 29f, 24f, 11f, 11f, (a, b) => a < -0.6f ? 'f' : (b < -0.6f ? 'l' : 'F'));
        // focinho largo e alto, cantos redondos
        for (int y = 20; y <= 34; y++)
            for (int x = 28; x <= 45; x++)
            {
                int cut = (y == 20 || y == 34) ? 2 : ((y == 21 || y == 33) ? 1 : 0);
                if (x > 45 - cut) continue;
                cv.Force(x, y, y <= 21 ? 'l' : (y >= 32 ? 'f' : 'F'));
            }
        // mancha do nariz e narinas
        for (int y = 21; y <= 27; y++)
            for (int x = 36; x <= 45; x++)
            {
                bool corner = (y == 21 && (x == 36 || x >= 44)) || (y == 27 && (x == 36 || x == 45));
                if (!corner) cv.Force(x, y, y == 21 ? 'n' : 'N');
            }
        cv.Force(38, 24, 'k'); cv.Force(38, 25, 'k'); cv.Force(43, 24, 'k'); cv.Force(43, 25, 'k');
        cv.Force(39, 22, 'n'); cv.Force(40, 22, 'n');                                               // brilho do nariz
        // boca em "人"
        cv.Force(40, 28, 'k'); cv.Force(40, 29, 'k');
        if (expr == "deboche")
        {
            cv.Force(39, 30, 'k'); cv.Force(41, 30, 'k'); cv.Force(38, 30, 'k'); cv.Force(42, 30, 'k');
            cv.Force(37, 29, 'k'); cv.Force(43, 29, 'k');                                          // cantos subindo: sorriso torto
            cv.Force(39, 31, 'U'); cv.Force(40, 31, 'U'); cv.Force(41, 31, 'U'); cv.Force(39, 32, 'u'); cv.Force(41, 32, 'u'); // incisivos
            cv.Force(40, 32, 'x');
        }
        else if (expr == "revelacao")
        {
            for (int x = 38; x <= 42; x++) cv.Force(x, 31, 'k');
            cv.Force(39, 30, 'k'); cv.Force(41, 30, 'k'); cv.Force(39, 32, 'h'); cv.Force(40, 32, 'h'); cv.Force(41, 32, 'h'); // boca aberta
        }
        else { cv.Force(39, 30, 'k'); cv.Force(41, 30, 'k'); cv.Force(38, 31, 'k'); cv.Force(42, 31, 'k'); }
        // bochecha
        cv.Force(23, 28, 'p'); cv.Force(24, 28, 'p'); cv.Force(25, 28, 'p'); cv.Force(24, 29, 'p');
        // olhos: o de perto grande, o de longe pequeno junto da ponte do focinho
        Eye(cv, 25, 20, 4, expr);
        Eye(cv, 35, 18, 2, expr);
        // corrente de ouro e pingente da praga
        for (int x = 8; x <= 36; x++) cv.Force(x, 42 + (Math.Abs(x - 22) < 6 ? 1 : 0), x % 2 == 0 ? 'Y' : 'y');
        PixelKit.Ellipse(cv, 22.5f, 45.5f, 2.6f, 2.6f, (a, b) => (a * a + b * b > 0.5f) ? (b < 0 ? 'Y' : 'y') : 'G');
        cv.Force(22, 45, 'Q');
        PixelKit.Outline(cv, 'o', "GQw");
        // a mão de osso erguida, girando a moeda na ponta dos dedos (ou faíscas da praga na revelação)
        var hand = new PixelCanvas(48, 48);
        PixelKit.Thick(hand, 39, 49, 42, 47, 4, (t, j) => j < 2 ? 'q' : 'R');                       // manga
        for (int y = 43; y <= 46; y++) for (int x = 40; x <= 44; x++) hand.Force(x, y, x == 40 ? 'u' : 'U'); // palma
        hand.Force(42, 44, 'x'); hand.Force(41, 45, 'x'); hand.Force(43, 45, 'x');
        int[] fxs = { 40, 42, 44 };
        foreach (int fx in fxs)
            for (int y = 42; y >= 39 - (fx == 42 ? 1 : 0); y--) hand.Force(fx, y, y == 40 ? 'x' : 'U');
        hand.Force(39, 44, 'U'); hand.Force(38, 43, 'U'); hand.Force(38, 42, 'x');                   // polegar
        if (expr != "revelacao")
        {
            PixelKit.Ellipse(hand, 42.5f, 36f, 2.4f, 2.4f, (a, b) => b < -0.2f ? 'Y' : 'y'); hand.Force(42, 35, 'w');
        }
        else { hand.Force(41, 37, 'G'); hand.Force(43, 36, 'Q'); hand.Force(45, 38, 'G'); hand.Force(39, 38, 'G'); hand.Force(43, 34, 'G'); }
        PixelKit.Outline(hand, 'o', "GQw");
        cv.Paste(hand);
        return cv;
    }

    /// <summary>Olho do retrato: brilho verde da praga sob a pálpebra pesada (ou arregalado, na revelação).</summary>
    static void Eye(PixelCanvas cv, int x, int y, int w, string expr)
    {
        if (expr == "revelacao")
        {
            for (int i = 0; i < w; i++) { cv.Force(x + i, y - 2, 'k'); cv.Force(x + i, y - 1, 'G'); cv.Force(x + i, y, 'G'); cv.Force(x + i, y + 1, 'G'); }
            cv.Force(x, y - 1, 'Q');
            int px = x + w / 2;
            cv.Force(px, y - 1, 'k'); cv.Force(px, y, 'k'); cv.Force(px, y + 1, 'k');                  // pupila em fenda
            for (int i = 0; i < w; i++) cv.Force(x + i, y + 2, 'k');
            return;
        }
        // sobrancelha franzida: desce em direção ao focinho
        for (int i = -1; i <= w; i++) cv.Force(x + i, y - 2 + (i >= w / 2 ? 1 : 0), 'k');
        bool narrow = expr == "deboche";
        for (int i = 0; i < w; i++) cv.Force(x + i, y, i == 0 ? 'Q' : 'G');
        if (!narrow && w > 2) for (int i = 1; i < w; i++) cv.Force(x + i, y + 1, 'G');
        for (int i = 0; i < w; i++) cv.Force(x + i, y + (narrow || w <= 2 ? 1 : 2), 'k');
    }

    static readonly string[][] PortraitList = {
        new[] { "portrait_contractor", "neutro" },
        new[] { "portrait_contractor_grin", "deboche" },
        new[] { "portrait_contractor_reveal", "revelacao" },
    };

    public const string AssetGuid = "e7c3a9f1b5d2468a9c0e4b6d8f1a3c57";

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        BuildClips();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string result = PixelKit.WriteAnimationSet(Clips, System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Characters/Contractor"), "chr_contractor",
            System.IO.Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Characters/ContractorAnimationSet.asset"), "ContractorAnimationSet", AssetGuid, template);
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
            PixelKit.PreviewClips(Clips, System.IO.Path.Combine(previewDir, "contratante_animacoes.png"), 7, new Rectangle(0, 0, 48, 48), "#1e1b2c");
            PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(previewDir, "contratante_retratos.png"), 6);
        }
        return "Contratante: " + result + ", retratos: " + portraits.Count;
    }

    public static void Preview(string dir)
    {
        InitPalette();
        BuildClips();
        PixelKit.PreviewClips(Clips, System.IO.Path.Combine(dir, "contratante_animacoes.png"), 7, new Rectangle(0, 0, 48, 48), "#3a3350");
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        foreach (var p in PortraitList) { portraits.Add(Portrait(p[1]).Mirror()); names.Add(p[1]); }
        PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(dir, "contratante_retratos.png"), 6);
    }
}
