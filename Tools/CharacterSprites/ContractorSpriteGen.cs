// O Contratante (Capivara Necromante): sprites da cena do tomo e retratos.
// Lore: sobrevivente da Guerra dos Corredores Mortos, infectado por uma praga; uma das mãos é esquelética
// (Lore Oficial §VI; Personagens.md §3). Canvas 48×48, pivot no centro da borda de baixo.
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
        PixelKit.P('o', "#1a1c26");                                                    // contorno
        PixelKit.P('F', "#8a5a3c"); PixelKit.P('f', "#6b4430"); PixelKit.P('l', "#a8764f"); PixelKit.P('k', "#4a2e20"); // pelo
        PixelKit.P('R', "#4a2266"); PixelKit.P('r', "#2e1640"); PixelKit.P('q', "#6a3a8a"); // manto
        PixelKit.P('G', "#8fd14f"); PixelKit.P('Q', "#d6ff8a");                         // praga (runas, olhos)
        PixelKit.P('Y', "#d9a441"); PixelKit.P('y', "#a8782a");                         // ouro
        PixelKit.P('U', "#e6e0d0"); PixelKit.P('u', "#b8b0a0"); PixelKit.P('x', "#7a7468"); // osso
        PixelKit.P('M', "#5b2a6e"); PixelKit.P('m', "#3d1b4a");                         // o tomo
        PixelKit.P('V', "#7a3a9a"); PixelKit.P('v', "#4a1e66");                         // névoa roxa
        PixelKit.P('w', "#ffffff");
    }

    public class Pose
    {
        public int Dy;                              // respiração
        public int Hem;                             // barra do manto balançando (-1, 0, 1)
        public Point Hand = new Point(32, 31);      // mão de osso
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
        // manto: largo e pesado, alarga até o chão; lado de trás na sombra
        for (int y = 22 + dy; y < 48; y++)
        {
            float k = (y - 22 - dy) / 25f;
            int half = (int)(6.5f + k * 6.5f);
            int cx = 23;
            for (int x = cx - half; x <= cx + half; x++)
            {
                char c = x < cx - half + 3 ? 'r' : 'R';
                if ((x == cx - 2 || x == cx + 4) && y > 30 + dy) c = 'r';                // dobras
                if (y >= 45) c = 'Y';                                                    // barra dourada
                if (y == 47) c = (x + p.Hem) % 3 == 0 ? '.' : 'R';                       // barra puída
                cv.Set(x + (y > 40 ? p.Hem : 0), y, c);
            }
        }
        // runas verde-ácido da praga no manto
        int[][] runes = { new[] { 19, 36 }, new[] { 19, 37 }, new[] { 20, 38 }, new[] { 27, 40 }, new[] { 28, 40 }, new[] { 28, 41 }, new[] { 22, 43 } };
        foreach (var r in runes) cv.Force(r[0], r[1] + dy, 'G');
        // capuz caído nos ombros
        PixelKit.Ellipse(cv, 21.5f, 24f + dy, 8f, 3.4f, (a, b) => a < -0.3f ? 'r' : 'q');
        // cabeça de capivara: bloco largo, focinho reto e alto
        PixelKit.Ellipse(cv, 22f, 17f + dy, 7.2f, 6f, (a, b) => a < -0.5f ? 'f' : (b < -0.55f ? 'l' : 'F'));
        for (int y = 13; y <= 21; y++)
            for (int x = 26; x <= 33; x++)
            {
                if (x == 33 && (y == 13 || y == 21)) continue;
                cv.Force(x, y + dy, y <= 14 ? 'l' : (y >= 20 ? 'f' : 'F'));
            }
        cv.Force(32, 15 + dy, 'k'); cv.Force(33, 15 + dy, 'k');                         // narina
        for (int x = 29; x <= 33; x++) cv.Force(x, 19 + dy, 'k');                       // boca
        if (p.Face == "grin") { cv.Force(28, 18 + dy, 'k'); cv.Force(30, 20 + dy, 'U'); cv.Force(31, 20 + dy, 'U'); }  // incisivos à mostra
        // orelha pequena e redonda
        cv.Force(17, 11 + dy, 'f'); cv.Force(18, 11 + dy, 'f'); cv.Force(17, 12 + dy, 'k'); cv.Force(18, 12 + dy, 'f');
        // olho: brilho verde da praga, pálpebra pesada (deboche)
        int ex = 25, ey = 15 + dy;
        if (p.Face == "wide") { cv.Force(ex, ey - 1, 'Q'); cv.Force(ex, ey, 'G'); cv.Force(ex + 1, ey, 'G'); cv.Force(ex + 1, ey - 1, 'G'); }
        else { cv.Force(ex - 1, ey - 1, 'k'); cv.Force(ex, ey - 1, 'k'); cv.Force(ex + 1, ey - 1, 'k'); cv.Force(ex, ey, 'Q'); cv.Force(ex + 1, ey, 'G'); }
        // corrente de ouro com a medalha
        for (int x = 18; x <= 28; x++)
        {
            int yy = 26 + dy + (int)(Math.Abs(x - 23) < 3 ? 1 : 0);
            cv.Force(x, yy, x % 2 == 0 ? 'Y' : 'y');
        }
        cv.Force(23, 28 + dy, 'Y'); cv.Force(24, 28 + dy, 'Y'); cv.Force(23, 29 + dy, 'y'); cv.Force(24, 29 + dy, 'Y');
        // manga e mão de osso
        Arm(cv, new Point(26, 27 + dy), p);
        return cv;
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
        PixelKit.Outline(body, 'o', "GQ");
        if (p.Reveal >= 1f && !p.Mist) return body;
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
        return cv;
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

        // parado: respira, o manto balança, os dedos de osso batem devagar
        Add("idle", 0.25f, true,
            std,
            With(std, p => { p.Fingers = 3; }),
            With(std, p => { p.Dy = 1; p.Hem = 1; }),
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

    // 48×48 olhando para a direita (espelhado depois: o contratante fala do lado direito).
    static PixelCanvas Portrait(string expr)
    {
        var cv = new PixelCanvas(48, 48);
        // manto e capuz caído
        PixelKit.Ellipse(cv, 22f, 50f, 24f, 13f, (a, b) => a < -0.4f ? 'r' : 'R');
        PixelKit.Ellipse(cv, 21f, 37f, 15f, 5f, (a, b) => a < -0.3f ? 'r' : 'q');
        // corrente de ouro e medalha
        for (int x = 8; x <= 34; x++) cv.Force(x, 41 + (Math.Abs(x - 21) < 6 ? 1 : 0), x % 2 == 0 ? 'Y' : 'y');
        PixelKit.Ellipse(cv, 21.5f, 45f, 2.6f, 2.6f, (a, b) => b < 0 ? 'Y' : 'y');
        // cabeça de capivara
        PixelKit.Ellipse(cv, 20f, 22f, 13f, 11.5f, (a, b) => a < -0.55f ? 'f' : (b < -0.6f ? 'l' : 'F'));
        for (int y = 15; y <= 32; y++)
            for (int x = 26; x <= 42; x++)
            {
                if ((x >= 41 && (y <= 16 || y >= 31)) || (x == 42 && (y <= 17 || y >= 30))) continue;
                cv.Force(x, y, y <= 17 ? 'l' : (y >= 29 ? 'f' : 'F'));
            }
        // narinas, boca e queixo
        cv.Force(40, 19, 'k'); cv.Force(41, 19, 'k'); cv.Force(40, 20, 'k');
        for (int x = 33; x <= 42; x++) cv.Force(x, 27, 'k');
        if (expr == "deboche") { cv.Force(32, 26, 'k'); cv.Force(31, 25, 'k'); cv.Force(41, 28, (char)85); cv.Force(42, 28, (char)85); cv.Force(41, 29, (char)117); cv.Force(42, 29, (char)117); }
        // orelha pequena
        PixelKit.Ellipse(cv, 12f, 11.5f, 2.6f, 2.2f, (a, b) => a > 0.2f ? 'k' : 'f');
        // olho: brilho verde, pálpebra pesada
        int ex = 26, ey = 18;
        if (expr == "revelacao")
        {
            // arregalado: íris verde 3×3 com pupila em fenda
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) cv.Force(ex + x, ey + y, 'G');
            cv.Force(ex - 1, ey - 1, 'Q'); cv.Force(ex, ey - 1, 'k'); cv.Force(ex, ey, 'k'); cv.Force(ex, ey + 1, 'k');
            for (int x = ex - 2; x <= ex + 2; x++) cv.Force(x, ey - 2, 'k');
        }
        else
        {
            for (int x = ex - 3; x <= ex + 3; x++) cv.Force(x, ey - 1, 'k');                            // pálpebra pesada
            for (int x = ex - 2; x <= ex + 2; x++) cv.Force(x, ey, x == ex ? 'Q' : 'G');
            for (int x = ex - 2; x <= ex + 2; x++) cv.Force(x, ey + 1, 'k');
        }
        // a mão de osso erguida na frente do peito, sob o focinho, girando a moeda na ponta dos dedos
        PixelKit.Outline(cv, 'o', "GQw");                                                           // contorno da cabeça antes da mão
        var hand = new PixelCanvas(48, 48);
        PixelKit.Thick(hand, 33, 47, 37, 42, 4, (t, j) => j < 2 ? 'q' : 'R');                          // manga
        for (int y = 38; y <= 41; y++) for (int x = 35; x <= 39; x++) hand.Force(x, y, x == 35 ? 'u' : 'U'); // palma
        hand.Force(37, 39, 'x'); hand.Force(36, 40, 'x'); hand.Force(38, 40, 'x');                       // vãos dos ossos
        int[] fxs = { 35, 37, 39 };
        foreach (int fx in fxs)
            for (int y = 37; y >= 33 - (fx == 37 ? 1 : 0); y--) hand.Force(fx, y, y == 35 ? 'x' : 'U');     // dedos para cima, juntas
        hand.Force(34, 39, 'U'); hand.Force(33, 38, 'U'); hand.Force(33, 37, 'x');                       // polegar
        if (expr != "revelacao")
        {
            PixelKit.Ellipse(hand, 37.5f, 30f, 2.4f, 2.4f, (a, b) => b < -0.2f ? 'Y' : 'y'); hand.Force(37, 29, 'w');
        }
        else { hand.Force(36, 31, 'G'); hand.Force(38, 30, 'Q'); hand.Force(40, 32, 'G'); hand.Force(34, 32, 'G'); } // faíscas da praga
        PixelKit.Outline(hand, 'o', "GQw");
        cv.Paste(hand);
        return cv;
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
            PixelKit.PreviewClips(Clips, System.IO.Path.Combine(previewDir, "contratante_animacoes.png"), 7, new Rectangle(4, 6, 42, 42), "#1e1b2c");
            PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(previewDir, "contratante_retratos.png"), 6);
        }
        return "Contratante: " + result + ", retratos: " + portraits.Count;
    }

    public static void Preview(string dir)
    {
        InitPalette();
        BuildClips();
        PixelKit.PreviewClips(Clips, System.IO.Path.Combine(dir, "contratante_animacoes.png"), 7, new Rectangle(4, 6, 42, 42), "#3a3350");
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        foreach (var p in PortraitList) { portraits.Add(Portrait(p[1]).Mirror()); names.Add(p[1]); }
        PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(dir, "contratante_retratos.png"), 6);
    }
}
