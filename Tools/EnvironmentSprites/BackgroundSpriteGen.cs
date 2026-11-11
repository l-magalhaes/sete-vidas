// Etapa 6A do plano de cenário: céus por altura (crepúsculo, borda do sol, meio sol, último raio, noite) e as camadas
// exteriores dos conjuntos de fundo (seção 7): cordilheira de 960 px (dia e noite), floresta do vale, copas vistas de
// cima, a Cidadela de perto, a montanha do início (perto e longe) e Miaravéu no horizonte.
// Regras (7.6): três escalas de forma em toda silhueta, nada reto nem simétrico, árvores em bosques com clareiras,
// camadas repetidas com 960 px e emenda sem costura, uma luz só por conjunto (borda do lado do sol), névoa na base.
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class BackgroundSpriteGen
{
    static string s_template;
    static readonly List<KeyValuePair<string, Bitmap>> s_sheet = new List<KeyValuePair<string, Bitmap>>();

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        string bg = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Backgrounds");
        s_sheet.Clear();

        { var c_ = Sky(SkyKind.Twilight); Write(bg, "Sky", "bg_ceu_crepusculo", c_); }
        { var c_ = Sky(SkyKind.SunEdge); Write(bg, "Sky", "bg_ceu_borda_do_sol", c_); }
        { var c_ = Sky(SkyKind.HalfSun); Write(bg, "Sky", "bg_ceu_meio_sol", c_); }
        { var c_ = Sky(SkyKind.LastRay); Write(bg, "Sky", "bg_ceu_ultimo_raio", c_); }
        { var c_ = Sky(SkyKind.Night); Write(bg, "Sky", "bg_ceu_noite", c_); }
        { var c_ = Range(false); Write(bg, "Ext", "bg_ext_cordilheira_a", c_); }
        { var c_ = Range(true); Write(bg, "Ext", "bg_ext_cordilheira_noite", c_); }
        { var c_ = Range(false, false); Write(bg, "Ext", "bg_ext_cordilheira", c_); }
        { var c_ = ValleyForest(); Write(bg, "Ext", "bg_ext_floresta_vale", c_); }
        { var c_ = CanopyFromAbove(); Write(bg, "Ext", "bg_ext_copas_de_cima", c_); }
        { var c_ = StartMountain(1f); Write(bg, "Ext", "bg_ext_montanha_inicio", c_); }
        { var c_ = StartMountain(0.45f); Write(bg, "Ext", "bg_ext_montanha_inicio_longe", c_); }
        { var c_ = CitadelNear(); Write(bg, "Cid", "bg_cid_fachada_perto", c_); }
        { var c_ = Miaraveu(); Write(bg, "Mia", "bg_mia_telhados_horizonte", c_); }

        // 6B: janelas dos interiores (moldura + máscara da abertura) e silhuetas de primeiro plano
        foreach (var kind in new[] { WindowKind.Slit, WindowKind.Window, WindowKind.Big })
        {
            string name = kind == WindowKind.Slit ? "bg_int_seteira" : kind == WindowKind.Window ? "bg_int_janela" : "bg_int_janela_grande";
            PixelCanvas frame, mask;
            WindowArt(kind, out frame, out mask);
            Write(bg, "Interior", name, frame);
            Write(bg, "Interior", name + "_mascara", mask);
        }
        { var c_ = FgChain(); Write(bg, "Interior", "fg_corrente", c_); }

        Sheet(Path.Combine(previewDir, "fundos_etapa6.png"));
    }

    // ------------------------------------------------------------ janelas (6B)

    enum WindowKind { Slit, Window, Big }

    /// <summary>
    /// Janela na parede do fundo (7.5): a moldura de pedra (desenhada por cima da parede) e a máscara da abertura (branca),
    /// que recorta a parede para o fundo daquela altura aparecer. Seteira 4×18 (só céu), janela 20×34 com uma travessa,
    /// janela grande 36×52 com mainel (a câmara: o último raio). Abertura em arco, peitoril embaixo.
    /// </summary>
    static void WindowArt(WindowKind kind, out PixelCanvas frame, out PixelCanvas mask)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('L', "#5e5872"); PixelKit.P('H', "#6e6884"); PixelKit.P('S', "#4a455c"); PixelKit.P('D', "#3a3549");
        PixelKit.P('k', "#1c1a24"); PixelKit.P('i', "#5a5868"); PixelKit.P('w', "#ffffff");
        int ow = kind == WindowKind.Slit ? 4 : kind == WindowKind.Window ? 20 : 36;
        int oh = kind == WindowKind.Slit ? 18 : kind == WindowKind.Window ? 34 : 52;
        int border = kind == WindowKind.Slit ? 3 : 5;
        int W = ow + border * 2, H = oh + border + 4;                      // + peitoril
        frame = new PixelCanvas(W, H);
        mask = new PixelCanvas(W, H);
        float cx = W / 2f, r = ow / 2f, archY = border + r;                // centro do arco
        Func<float, float, float, bool> inside = (x, y, grow) =>
        {
            float px = x + 0.5f, py = y + 0.5f;
            if (py >= archY) return Math.Abs(px - cx) <= r + grow && py <= border + oh + grow;
            float dx = px - cx, dy = py - archY;
            return dx * dx + dy * dy <= (r + grow) * (r + grow);
        };
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                bool open = inside(x, y, 0f), outer = inside(x, y, border);
                if (open) { mask.Px[x, y] = 'w'; continue; }
                if (outer) frame.Px[x, y] = x > cx + r ? 'H' : x < cx - r ? 'S' : (y < archY ? 'L' : 'S');
            }
        // juntas das aduelas e o peitoril
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (frame.Px[x, y] != '.' && (x + y * 3) % 11 == 0) frame.Px[x, y] = 'D';
        for (int x = 0; x < W; x++) { frame.Px[x, H - 4] = 'H'; frame.Px[x, H - 3] = 'L'; frame.Px[x, H - 2] = 'S'; frame.Px[x, H - 1] = 'D'; }
        // travessa e mainel de ferro (por cima da abertura, na moldura)
        if (kind != WindowKind.Slit)
            for (int x = (int)(cx - r); x < (int)(cx + r); x++) frame.Px[x, border + oh / 2] = 'i';
        if (kind == WindowKind.Big)
            for (int y = (int)archY; y < border + oh; y++) frame.Px[(int)cx, y] = 'i';
        PixelKit.Outline(frame, 'k');
    }

    /// <summary>Corrente de primeiro plano (fg): silhueta quase preta, desfocada pela distância (sem detalhe).</summary>
    static PixelCanvas FgChain()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#120e18"); PixelKit.P('K', "#1c1624");
        var cv = new PixelCanvas(10, 64);
        for (int y = 0; y < 64; y++)
        {
            int k = y % 10;
            if ((y / 10) % 2 == 0) { if (k < 2 || k > 7) for (int x = 3; x < 7; x++) cv.Px[x, y] = 'k'; else { cv.Px[2, y] = 'k'; cv.Px[3, y] = 'K'; cv.Px[6, y] = 'K'; cv.Px[7, y] = 'k'; } }
            else for (int x = 4; x < 6; x++) cv.Px[x, y] = 'k';
        }
        return cv;
    }

    /// <summary>Viga de primeiro plano (fg) cruzando o alto da tela, com as mãos-francesas: silhueta escura.</summary>
    static PixelCanvas FgBeam()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#120e18"); PixelKit.P('K', "#1c1624");
        var cv = new PixelCanvas(96, 22);
        for (int x = 0; x < 96; x++) for (int y = 0; y < 10; y++) cv.Px[x, y] = y == 9 ? 'K' : 'k';
        foreach (int bx in new[] { 6, 89 })
            for (int k = 0; k < 12; k++) for (int w = 0; w < 3; w++)
            {
                int x = bx + (bx < 48 ? k : -k) + w, y = 10 + k;
                if (x >= 0 && x < 96 && y < 22) cv.Px[x, y] = 'k';
            }
        return cv;
    }

    static void Write(string bg, string sub, string name, PixelCanvas cv)
    {
        foreach (char ch in cv.Px) if (ch != '.' && !PixelKit.Pal.ContainsKey(ch)) throw new Exception(name + ": cor sem paleta '" + ch + "'");
        PixelKit.WriteSprite(cv, Path.Combine(Path.Combine(bg, sub), name + ".png"), name, s_template, false);
        s_sheet.Add(new KeyValuePair<string, Bitmap>(name, PixelKit.ToBitmap(cv)));   // a paleta muda a cada asset
    }

    static void Sheet(string path)
    {
        // cada asset numa faixa, na escala 1 (os céus de 480 lado a lado)
        int W = 960 * 2, y = 0;
        var placed = new List<Rectangle>();
        int x = 0, rowH = 0;
        foreach (var kv in s_sheet)
        {
            var cv = kv.Value;
            if (x + cv.Width > W) { x = 0; y += rowH + 6; rowH = 0; }
            placed.Add(new Rectangle(x, y, cv.Width, cv.Height));
            x += cv.Width + 6; rowH = Math.Max(rowH, cv.Height);
        }
        int H = y + rowH;
        using (var bmp = new Bitmap(W, H))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(ColorTranslator.FromHtml("#101018"));
            for (int i = 0; i < s_sheet.Count; i++)
                g.DrawImage(s_sheet[i].Value, placed[i]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // ------------------------------------------------------------------ céus

    enum SkyKind { Twilight, SunEdge, HalfSun, LastRay, Night }

    static float Hash(int x, int y, int seed) { return PixelKit.Noise(x, y, seed); }

    /// <summary>
    /// Céu 480×270 preso à câmera: gradiente em faixas com pontilhado de 2 passos, sol ou lua do lado da luz e nuvens
    /// com contorno suave. A luz depende da altura (7.2): embaixo o sol já se pôs; no alto ele ainda aparece.
    /// </summary>
    static PixelCanvas Sky(SkyKind kind)
    {
        PixelKit.Pal.Clear();
        string[] bands;
        switch (kind)
        {
            case SkyKind.Twilight:  // crepúsculo frio: a luz laranja só pega as nuvens altas, embaixo é roxo
                bands = new[] { "#2a1f40", "#3a2550", "#4a2c58", "#4e3060", "#47305e", "#3e2c58", "#352850", "#2e2448", "#282040" }; break;
            case SkyKind.SunEdge:   // a borda do sol volta a aparecer no horizonte
                bands = new[] { "#24183a", "#33204a", "#462656", "#5c2e5c", "#7a385c", "#9a4458", "#b85450", "#d06a48", "#e08a48" }; break;
            case SkyKind.HalfSun:   // meio sol: mais quente e claro
                bands = new[] { "#2e1a42", "#45224e", "#62305a", "#8a3e5a", "#b0505a", "#d06a4e", "#e88a48", "#f4a850", "#f8c868" }; break;
            case SkyKind.LastRay:   // o último raio: sol inteiro acima do horizonte, luz dourada horizontal
                bands = new[] { "#3a2450", "#56305c", "#7a3e60", "#a0525e", "#c46a58", "#e08a50", "#f0a850", "#f8c460", "#fcd878" }; break;
            default:                // noite depois da maldição: azul-arroxeado, um roxo leve (a praga solta)
                bands = new[] { "#0c0a1a", "#100e22", "#14122a", "#181632", "#1e1838", "#241a3e", "#2a1c44", "#2e1e48", "#34204c" }; break;
        }
        for (int i = 0; i < bands.Length; i++) PixelKit.P((char)('0' + i), bands[i]);
        PixelKit.P('S', "#fff0b8"); PixelKit.P('s', "#ffd070"); PixelKit.P('r', "#ffb860");
        PixelKit.P('c', "#d07a6a"); PixelKit.P('C', "#e8a08a"); PixelKit.P('v', "#5a3a62"); PixelKit.P('V', "#6e4a6e");
        PixelKit.P('*', "#c8c8e0"); PixelKit.P('+', "#8a8aa8"); PixelKit.P('M', "#e8e4d0"); PixelKit.P('m', "#b8b4a8");

        int W = 480, H = 270;
        var cv = new PixelCanvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float t = y / (float)(H - 1) * 8f;
                int band = (int)t; float f = t - band;
                bool next = f > 0.5f && ((x + y) % 2 == 0 || f > 0.8f);
                cv.Px[x, y] = (char)('0' + Math.Min(8, band + (next ? 1 : 0)));
            }

        if (kind == SkyKind.Night)
        {
            // estrelas (mais no alto) e a lua crescente à esquerda: a luz de borda das camadas fica à esquerda
            for (int y = 0; y < 190; y++)
                for (int x = 0; x < W; x++)
                {
                    float h = Hash(x, y, 71);
                    if (h > 0.9965f - y * 0.00001f) cv.Px[x, y] = h > 0.9991f ? '*' : '+';
                }
            PixelKit.Ellipse(cv, 110f, 58f, 13f, 13f, (nx, ny) => nx < 0.2f && ny < 0.3f ? 'M' : 'm');
            // a sombra que faz o crescente: repinta o céu por cima de parte da lua
            for (int y = 40; y < 66; y++)
                for (int x = 104; x < 130; x++)
                {
                    float ddx = x + 0.5f - 117f, ddy = y + 0.5f - 53f;
                    if (ddx * ddx + ddy * ddy > 11f * 11f) continue;
                    float t = y / (float)(H - 1) * 8f; int band = (int)t;
                    cv.Px[x, y] = (char)('0' + Math.Min(8, band + ((t - band > 0.5f && (x + y) % 2 == 0) ? 1 : 0)));
                }
            // um véu roxo bem fraco no meio do céu (a praga solta), só pontilhado
            for (int x = 0; x < W; x++)
                for (int y = 120; y < 150; y++)
                    if ((x + y) % 3 == 0 && Hash(x, y, 9) > 0.55f) cv.Px[x, y] = 'v';
            return cv;
        }

        // sol do lado direito; altura conforme o estado
        float scx = 330f, scy, r = 26f;
        switch (kind)
        {
            case SkyKind.SunEdge: scy = 282f; break;  // só a borda aparece acima da linha de baixo
            case SkyKind.HalfSun: scy = 258f; break;
            case SkyKind.LastRay: scy = 214f; r = 22f; break;
            default: scy = 400f; break;              // abaixo do horizonte
        }
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = (float)Math.Sqrt((x - scx) * (x - scx) + (y - scy) * (y - scy));
                if (d <= r) cv.Px[x, y] = d < r - 5 ? 'S' : 's';
                else if (d <= r + 4 && (x + y) % 2 == 0) cv.Px[x, y] = 'r';
            }
        // (os raios em faixas pareciam riscos na tela: o último raio fica só no sol alto e na luz dourada das camadas)

        // nuvens: na noite não; no crepúsculo só as altas pegam luz laranja
        if (kind == SkyKind.Twilight)
        {
            Cloud(cv, 30, 34, 150, 'c', 'C'); Cloud(cv, 230, 22, 110, 'C', 'c'); Cloud(cv, 360, 48, 90, 'c', 'C');
            Cloud(cv, 80, 140, 140, 'v', 'V'); Cloud(cv, 300, 168, 120, 'V', 'v');
        }
        else
        {
            Cloud(cv, 40, 120, 120, 'c', 'C'); Cloud(cv, 250, 104, 90, 'C', 'c'); Cloud(cv, 370, 150, 70, 'c', 'C'); Cloud(cv, 120, 176, 80, 'C', 'c');
        }
        return cv;
    }

    /// <summary>Nuvem em faixa: corpo mais grosso no meio, borda de cima clara, ponta pontilhada (contorno suave).</summary>
    static void Cloud(PixelCanvas cv, int x0, int y0, int len, char body, char rim)
    {
        for (int i = 0; i < len; i++)
        {
            double t = i / (double)len;
            int thick = 1 + (int)(3.2 * Math.Sin(t * Math.PI) + Math.Sin(t * 17) * 0.6);
            bool tip = i < 6 || i > len - 6;
            for (int k = 0; k < thick; k++)
            {
                if (tip && (i + k) % 2 == 0) continue;
                cv.Set(x0 + i, y0 + k + (int)(Math.Sin(t * 5) * 1.5), k == 0 ? rim : body);
            }
        }
    }

    // ---------------------------------------------------------------- silhuetas

    /// <summary>Cumeeira repetível em W: três escalas de forma (grande, média, pequena) por soma de senos inteiros.</summary>
    static float Ridge(int x, int W, float baseY, float amp, int seed)
    {
        double t = x / (double)W * Math.PI * 2;
        double big = 0.55 * Math.Sin(t * 2 + seed) + 0.25 * Math.Sin(t * 3 + seed * 1.7);
        double mid = 0.14 * Math.Sin(t * 9 + seed * 2.3) + 0.08 * Math.Sin(t * 14 + seed * 0.7);
        double small = 0.03 * Math.Sin(t * 41 + seed) + 0.02 * Math.Sin(t * 67 + seed * 3);
        return baseY - (float)(amp * (big + mid + small));
    }

    /// <summary>
    /// Cordilheira 960×200 (bg_longe): picos de alturas diferentes, neve nos mais altos, luz de borda nas encostas
    /// voltadas para o sol (direita de dia, esquerda de noite, com a lua). Acontecimentos únicos: o fio de uma
    /// cachoeira distante e uma torre em ruína; de dia, a Cidadela pequena com janelas acesas.
    /// </summary>
    static PixelCanvas Range(bool night) { return Range(night, !night); }

    /// <summary>Cordilheira com ou sem a Cidadela pequena (sem: vista de dentro dela, conjuntos C, F e G).</summary>
    static PixelCanvas Range(bool night, bool withCitadel)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('M', night ? "#100e1e" : "#3a2247");   // de noite o corpo fica bem mais escuro que o céu (senão só a borda aparece)
        PixelKit.P('m', night ? "#34305a" : "#6a3a6a");
        PixelKit.P('n', night ? "#3e3a5c" : "#8a5a7a");
        PixelKit.P('e', night ? "#1a1630" : "#4a2a52");   // névoa na base
        PixelKit.P('Q', night ? "#16122a" : "#301c3d");
        PixelKit.P('k', night ? "#c88a4a" : "#ffd070");
        PixelKit.P('w', night ? "#4a4a6a" : "#9a7a9a");
        int W = 960, H = 200;
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
        {
            float top = Ridge(x, W, 96f, 62f, 1);
            float nxt = Ridge((x + 1) % W, W, 96f, 62f, 1), prv = Ridge((x + W - 1) % W, W, 96f, 62f, 1);
            bool lit = night ? prv > top : nxt > top;      // encosta voltada para a luz
            for (int y = Math.Max(0, (int)top); y < H; y++)
            {
                char c = 'M';
                if (lit && y < top + 3) c = 'm';
                if (y < top + 2 && top < 52) c = 'n';
                if (y > H - 26 && (x + y) % 2 == 0 && y > H - 26 + (int)(Hash(x, 0, 3) * 10)) c = 'e';
                cv.Px[x, y] = c;
            }
        }
        // cachoeira distante (fio de 1 px) e torre em ruína
        int fx = 212; int fTop = (int)Ridge(fx, W, 96f, 62f, 1) + 10;
        for (int y = fTop; y < fTop + 40; y++) if (y % 4 != 0) cv.Force(fx, y, 'w');
        int rx = 470, rTop = (int)Ridge(rx, W, 96f, 62f, 1);
        for (int y = rTop - 12; y < rTop + 2; y++) { cv.Force(rx, y, 'Q'); cv.Force(rx + 1, y, 'Q'); cv.Force(rx + 2, y, y < rTop - 9 ? '.' : 'Q'); }
        cv.Force(rx, rTop - 13, 'Q');
        if (withCitadel) Citadel(cv, W, 760);
        return cv;
    }

    /// <summary>A Cidadela pequena ao longe (como a do fundo antigo, agora numa cordilheira de 960).</summary>
    static void Citadel(PixelCanvas cv, int W, int cx)
    {
        Func<int, int> ridge = x => (int)Ridge(x, W, 96f, 62f, 1);
        for (int seg = cx - 42; seg <= cx + 42; seg += 7)
        {
            int segTop = int.MaxValue;
            for (int x = seg; x < seg + 7; x++) segTop = Math.Min(segTop, ridge(x));
            int wallH = 6 + (Math.Abs(seg - cx) < 20 ? 3 : 0) - (Math.Abs(seg - cx) > 34 ? 2 : 0);
            int top = segTop - wallH;
            for (int x = seg; x < seg + 7; x++)
            {
                for (int y = top; y <= ridge(x); y++) cv.Force(x, y, 'Q');
                if ((x - seg) % 3 == 0) cv.Force(x, top - 1, 'Q');
            }
            cv.Force(seg + 6, top, 'm');
        }
        int[,] towers = { { -33, 5, 12 }, { -21, 7, 22 }, { -9, 5, 15 }, { 0, 8, 40 }, { 11, 6, 18 }, { 22, 7, 26 }, { 33, 5, 11 } };
        for (int i = 0; i < towers.GetLength(0); i++)
        {
            int tx = cx + towers[i, 0], tw = towers[i, 1], th = towers[i, 2];
            int ground = int.MaxValue;
            for (int x = tx - tw / 2; x <= tx + tw / 2; x++) ground = Math.Min(ground, ridge(x));
            int top = ground - 8 - th;
            for (int x = tx - tw / 2; x <= tx + tw / 2; x++)
                for (int y = top; y <= ridge(x); y++) cv.Force(x, y, x == tx + tw / 2 && y < ground - 4 ? 'm' : 'Q');
            if (th >= 18)
                for (int k = 1; k <= tw; k++)
                    for (int dx = -(tw - k) / 2; dx <= (tw - k) / 2; dx++) cv.Force(tx + dx, top - k, 'Q');
            else for (int x = tx - tw / 2; x <= tx + tw / 2; x += 2) cv.Force(x, top - 1, 'Q');
            if (i % 2 == 1 || i == 3) cv.Force(tx, top + th / 3, 'k');
            if (i == 3) { cv.Force(tx, top + 6, 'k'); cv.Force(tx - 1, top + 18, 'k'); }
        }
    }

    /// <summary>Árvore de fundo simplificada (mesmas espécies da Etapa 4): pinheiro ou copa redonda.</summary>
    static void BgTree(PixelCanvas cv, int x, int baseY, int h, bool round, char body, char rim, bool rimRight)
    {
        if (round)
        {
            int r = Math.Max(3, h / 3);
            PixelKit.Ellipse(cv, x, baseY - h + r, r, r * 0.85f, (nx, ny) => (rimRight ? nx > 0.45f : nx < -0.45f) && ny < 0.2f ? rim : body);
            for (int y = baseY - h + 2 * r - 1; y <= baseY; y++) cv.Set(x, y, body);
        }
        else
            for (int k = 0; k < h; k++)
            {
                int half = (h - k) / 3 + ((k % 4 == 0) ? 1 : 0);
                for (int dx = -half; dx <= half; dx++) cv.Set(x + dx, baseY - k, (rimRight ? dx == half : dx == -half) && k > 1 ? rim : body);
            }
    }

    /// <summary>
    /// Floresta do vale 960×150 (bg_medio do conjunto A): morros baixos, bosques de 3–7 árvores com clareiras, a trilha
    /// clara serpenteando para a direita (até o castelo). Acontecimento único: uma clareira grande com um lago.
    /// </summary>
    static PixelCanvas ValleyForest()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('H', "#2c1c3a"); PixelKit.P('h', "#3e2848"); PixelKit.P('T', "#22162e"); PixelKit.P('t', "#4a2e4e");
        PixelKit.P('p', "#4e3650"); PixelKit.P('l', "#6a5a7a"); PixelKit.P('e', "#352240");
        int W = 960, H = 150;
        var cv = new PixelCanvas(W, H);
        Func<int, float> ridge = x => Ridge(((x % W) + W) % W, W, 74f, 24f, 4);
        for (int x = 0; x < W; x++)
        {
            float top = ridge(x);
            for (int y = Math.Max(0, (int)top); y < H; y++) cv.Px[x, y] = y < top + 2 ? 'h' : ((y > H - 20 && (x + y) % 2 == 0) ? 'e' : 'H');
        }
        // trilha: faixa clara que serpenteia descendo para a direita
        for (int x = 0; x < W; x++)
        {
            int y = (int)(ridge(x) + 18 + 10 * Math.Sin(x / 60.0) + x * 0.02);
            if (y < H - 2) { cv.Force(x, y, 'p'); if (x % 3 != 0) cv.Force(x, y + 1, 'p'); }
        }
        // lago da clareira (acontecimento único)
        for (int x = 600; x < 660; x++) for (int y = (int)ridge(x) + 22; y < (int)ridge(x) + 26; y++) cv.Force(x, y, (x + y) % 5 == 0 ? 'l' : 'p');
        // bosques: grupos de 3–7, alturas ±30%, com clareiras entre eles (nada perto do lago)
        int gx = 8;
        int seed = 0;
        while (gx < W - 8)
        {
            seed++;
            int n = 3 + (int)(Hash(seed, 1, 5) * 5);
            int baseH = 12 + (int)(Hash(seed, 2, 5) * 8);
            for (int i = 0; i < n && gx < W - 8; i++)
            {
                if (gx > 585 && gx < 675) { gx += 6; continue; }
                int h = (int)(baseH * (0.7f + Hash(seed, 10 + i, 5) * 0.6f));
                BgTree(cv, gx, (int)ridge(gx) + 3, h, Hash(seed, 20 + i, 5) > 0.6f, 'T', 't', true);
                gx += 4 + (int)(Hash(seed, 30 + i, 5) * 4);
            }
            gx += 14 + (int)(Hash(seed, 3, 5) * 30);   // clareira
        }
        return cv;
    }

    /// <summary>
    /// Copas vistas de cima 960×140 (bg_medio dos conjuntos C, F, G): um mar de copas em tufos redondos, buracos escuros
    /// entre eles, luz no alto à direita de cada tufo e uma faixa de névoa na borda de cima.
    /// </summary>
    static PixelCanvas CanopyFromAbove()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('G', "#2a2440"); PixelKit.P('g', "#3a3054"); PixelKit.P('d', "#1e1a30"); PixelKit.P('f', "#4a3a5e");
        int W = 960, H = 140;
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++) for (int y = 30; y < H; y++) cv.Px[x, y] = (Hash(x, y, 4) > 0.85f) ? 'd' : 'G';
        // tufos em fileiras irregulares; repete em W (tufos perto das bordas aparecem dos dois lados)
        for (int row = 0; row < 6; row++)
        {
            int cy = 22 + row * 18;
            for (int i = 0; i < 70; i++)
            {
                float cx = i * 14f + Hash(i, row, 8) * 12f + (row % 2) * 7f;
                float r = 7f + Hash(i, row, 9) * 7f - row * 0.3f;
                for (int rep = -1; rep <= 1; rep++)
                    PixelKit.Ellipse(cv, cx + rep * W, cy + Hash(i, row, 10) * 6f, r, r * 0.7f,
                        (nx, ny) => nx > 0.25f && ny < -0.2f ? 'g' : (ny > 0.6f ? 'd' : 'G'));
            }
        }
        // névoa na borda de cima (pontilhada), separa do céu sem contorno
        for (int x = 0; x < W; x++)
            for (int y = 0; y < 40; y++)
                if (cv.Px[x, y] != '.' && y < 24 + (int)(Hash(x, 1, 2) * 6) && (x + y) % 2 == 0) cv.Px[x, y] = 'f';
        return cv;
    }

    /// <summary>
    /// A montanha do início vista de longe (sem repetir): pico arredondado com mata no alto, o penhasco à direita com o
    /// fio da cachoeira descendo até a lagoa, e serras baixas dos lados. Escala 1 = conjunto B (perto); 0,45 = G (longe).
    /// </summary>
    static PixelCanvas StartMountain(float scale)
    {
        PixelKit.Pal.Clear();
        bool far = scale < 0.8f;
        PixelKit.P('M', far ? "#4a2a52" : "#34203f"); PixelKit.P('m', far ? "#7a4a6e" : "#5e3a5e");
        PixelKit.P('c', far ? "#3e2448" : "#2a1834"); PixelKit.P('T', far ? "#3a2246" : "#24162e");
        PixelKit.P('w', far ? "#c8b8d8" : "#9ab0d0"); PixelKit.P('W', far ? "#e8e0f0" : "#c8dcf0"); PixelKit.P('e', far ? "#5a3a5e" : "#3a2444");
        int W = far ? 480 : 960, H = far ? 110 : 220;
        var cv = new PixelCanvas(W, H);
        int peak = (int)(W * 0.35f);
        float peakTop = H * 0.18f, foot = H * 0.62f;
        for (int x = 0; x < W; x++)
        {
            // serra baixa dos lados (repete) + o morro do início (pico arredondado, penhasco reto à direita)
            float side = Ridge(x, W, foot, H * 0.12f, 6);
            float dx = (x - peak) / (W * 0.16f);
            float hill = dx < 0 ? peakTop + (foot - peakTop) * (float)Math.Pow(Math.Min(1.0, -dx), 1.6) + (float)Math.Sin(x * 0.3) * 1.5f
                        : dx < 0.5f ? peakTop + dx * 6f : float.MaxValue;    // topo; depois de 0,5 vem o penhasco
            float top = Math.Min(side, hill);
            bool cliff = false;
            for (int y = Math.Max(0, (int)top); y < H; y++)
            {
                char c = 'M';
                if (y < top + 2 && dx > -0.6f && dx < 0.5f) c = 'm';          // borda iluminada do alto (sol à direita)
                if (cliff && y < foot) c = 'c';
                if (y > H - (int)(H * 0.1f) && (x + y) % 2 == 0) c = 'e';
                cv.Px[x, y] = c;
            }
        }
        // face do penhasco: borda irregular (pedras salientes e reentrâncias) e uma faixa de sombra de largura variável
        int edge0 = peak + (int)(W * 0.16f * 0.5f);
        var edgeAt = new int[H];
        for (int y = (int)peakTop; y < (int)foot; y++)
        {
            int jag = (int)Math.Round((Math.Sin(y * 0.35) * 1.6 + Math.Sin(y * 0.11 + 1) * 2.2 + (Hash(0, y, 17) - 0.5f) * 2.5) * scale);
            edgeAt[y] = edge0 + jag;
            int face = (int)((4 + Hash(1, y, 17) * 5) * scale) + 1;
            for (int x = edge0 - 8; x <= edgeAt[y]; x++)
            {
                if (x < 0 || x >= W) continue;
                if (x > edge0) cv.Px[x, y] = 'c';                                          // pedra saliente
                else if (x >= edgeAt[y] - face && cv.Px[x, y] != '.') cv.Px[x, y] = 'c'; // sombra da face
            }
            for (int x = edgeAt[y] + 1; x <= edge0 + 8 && x < W; x++)
                if (x <= edge0 && y < foot - 2 && (int)Ridge(x, W, foot, H * 0.12f, 6) > y) cv.Px[x, y] = '.';   // reentrância
        }
        // cachoeira: o fio claro descendo colado na face do penhasco
        // a cachoeira só na versão distante (do topo): no vale a cachoeira de verdade está ao lado e apareceria duas vezes
        for (int y = (int)(peakTop + H * 0.06f); y < foot && far; y++)
        {
            int fx = edgeAt[y] + 1;                                                 // colada na face do penhasco
            cv.Force(fx, y, y % 5 == 0 ? 'W' : 'w'); if (!far && y % 3 != 0) cv.Force(fx + 1, y, 'w');
        }
        // mata no alto (pinheiros pequenos), em grupos
        for (int x = peak - (int)(W * 0.12f); x < peak + (int)(W * 0.07f); x += far ? 4 : 7)
        {
            if (Hash(x, 0, 12) < 0.35f) continue;
            float dx = (x - peak) / (W * 0.16f);
            int baseY = (int)(dx < 0 ? peakTop + (foot - peakTop) * Math.Pow(-dx, 1.6) : peakTop + dx * 6f) + 2;
            BgTree(cv, x, baseY, far ? 4 + (int)(Hash(x, 1, 12) * 3) : 8 + (int)(Hash(x, 1, 12) * 6), false, 'T', 'm', true);
        }
        return cv;
    }

    /// <summary>
    /// A Cidadela de perto (conjunto B, sem repetir, 960×320): a rocha-base subindo para a direita, a muralha de fora em
    /// degraus, torres de alturas diferentes saindo pelo alto do quadro, a Torre do Selo mais alta no meio-direito,
    /// janelas acesas (o padrão bate com os andares de dentro: 43, 72, 101) e estandartes. Valor 15–28%.
    /// </summary>
    static PixelCanvas CitadelNear()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('B', "#2a2038"); PixelKit.P('b', "#3a2c4c"); PixelKit.P('D', "#201828");   // corpo, borda iluminada, sombra
        PixelKit.P('R', "#241c30"); PixelKit.P('r', "#30263e");                               // rocha-base
        PixelKit.P('k', "#9a6a3e"); PixelKit.P('K', "#b88450");                               // janelas acesas (fundo: sem competir com o jogo)
        PixelKit.P('C', "#4a2430"); PixelKit.P('j', "#2e2438"); PixelKit.P('x', "#16101e");  // estandarte, juntas, fresta
        int W = 960, H = 320;
        var cv = new PixelCanvas(W, H);
        // rocha-base: começa baixa à esquerda e sobe para a direita (alturas guardadas antes de pôr a construção em cima)
        var rock = new int[W];
        for (int x = 0; x < W; x++)
        {
            rock[x] = (int)(H - 30 - Math.Max(0, (x - 260) * 0.18f) + Math.Sin(x * 0.07) * 4 + Math.Sin(x * 0.23) * 2);
            for (int y = Math.Max(0, rock[x]); y < H; y++) cv.Px[x, y] = (Hash(x, y, 13) > 0.8f) ? 'r' : 'R';
        }
        Func<int, int> rockTop = x => rock[Math.Max(0, Math.Min(W - 1, x))];
        // muralha de fora em degraus (acompanha a rocha), com ameias e frestas
        for (int seg = 300; seg < W; seg += 24)
        {
            int segTop = int.MaxValue;
            for (int x = seg; x < Math.Min(W, seg + 24); x++) segTop = Math.Min(segTop, rockTop(x));
            int top = segTop - 34;
            for (int x = seg; x < Math.Min(W, seg + 24); x++)
            {
                for (int y = top; y < rockTop(x) + 1; y++) cv.Force(x, y, x >= seg + 22 ? 'b' : (y == top + 2 ? 'D' : 'B'));
                if ((x - seg) % 6 < 3) for (int y = top - 4; y < top; y++) cv.Force(x, y, 'B');
            }
            for (int y = top + 10; y < top + 18; y++) cv.Force(seg + 11, y, 'x');
        }
        // torres: (x, largura na base, topo) — as altas saem pelo alto do quadro. Base mais larga (talude), frisos nos
        // andares, guaritas nos cantos das torres de topo plano, telhado cônico nas outras, janelas em alguns andares
        int[,] towers = { { 360, 40, 120 }, { 470, 54, 40 }, { 560, 46, 90 }, { 650, 76, -10 }, { 760, 52, 60 }, { 860, 60, 20 } };
        for (int i = 0; i < towers.GetLength(0); i++)
        {
            int tx = towers[i, 0], tw = towers[i, 1], top = towers[i, 2];
            int bottom = rockTop(tx) - 30;
            for (int y = Math.Max(0, top); y < rockTop(tx) + 2; y++)
            {
                int half = tw / 2 - (y < bottom - 24 ? 3 : 0) - (y < top + 8 ? 0 : 0);
                if (y > bottom - 24) half = tw / 2 + Math.Min(4, (y - (bottom - 24)) / 6);   // talude na base
                for (int x = tx - half; x <= tx + half; x++)
                {
                    bool rimLit = x >= tx + half - 1, shade = x <= tx - half + 2;
                    bool ledge = (y - top) % 34 == 33;
                    cv.Force(x, y, ledge ? (rimLit ? 'b' : 'D') : rimLit ? 'b' : shade ? 'D' : ((y + x / 9) % 13 == 0 && x % 5 == 0 ? 'j' : 'B'));
                }
            }
            int topHalf = tw / 2 - 3;
            if (i == 1 || i == 4)
            {
                // telhado cônico
                for (int k = 1; k < topHalf + 6 && top - k >= 0; k++)
                    for (int dx = -(topHalf + 2) + k; dx <= topHalf + 2 - k; dx++) cv.Force(tx + dx, top - k, dx > topHalf - k ? 'b' : 'D');
            }
            else if (top > 4)
            {
                // topo plano com ameias e duas guaritas nos cantos
                for (int x = tx - topHalf; x <= tx + topHalf; x++) if ((x - tx) % 6 < 3) for (int y = top - 4; y < top; y++) cv.Force(x, y, 'B');
                foreach (int side in new[] { -1, 1 })
                {
                    int gx = tx + side * (topHalf + 2);
                    // guarita: corpo cilíndrico de 7 px, mísula embaixo e telhadinho cônico
                    for (int y = top - 10; y < top + 2; y++) for (int x = gx - 3; x <= gx + 3; x++) cv.Force(x, y, side > 0 && x == gx + 3 ? 'b' : 'D');
                    for (int k = 0; k < 3; k++) for (int x = gx - 2 + k; x <= gx + 2 - k; x++) cv.Force(x, top + 2 + k, 'D');
                    for (int k = 1; k < 5; k++) for (int x = gx - 4 + k; x <= gx + 4 - k; x++) cv.Force(x, top - 10 - k, 'D');
                    cv.Force(gx, top - 6, 'x'); cv.Force(gx, top - 5, 'x');
                }
            }
            // janelas: um andar a cada 34 px, metade acesas
            for (int wy = Math.Max(top + 12, 16); wy < bottom - 30; wy += 34)
                for (int wx = tx - topHalf + 6; wx < tx + topHalf - 4; wx += 12)
                {
                    bool on = Hash(wx, wy, 21) > 0.72f;
                    for (int y = wy; y < wy + 8; y++) { cv.Force(wx, y, on ? 'k' : 'x'); cv.Force(wx + 1, y, on ? (y < wy + 2 ? 'K' : 'k') : 'x'); }
                    cv.Force(wx, wy - 1, 'x'); cv.Force(wx + 1, wy - 1, 'x');
                }
            // estandarte desbotado nas torres de telhado
            if (i == 1 || i == 4)
                for (int y = top + 6; y < top + 34; y++) for (int x = tx - 3; x < tx + 3; x++) if (y < top + 30 || (x + y) % 2 == 0) cv.Force(x, y, 'C');
        }
        return cv;
    }

    /// <summary>
    /// Miaravéu no horizonte de noite (conjunto H, 960×120, sem repetir): morros escuros, a estrada clara descendo até a
    /// cidade, telhados pontudos com janelas acesas e fumaça das chaminés. O gancho para a próxima fase.
    /// </summary>
    static PixelCanvas Miaraveu()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('H', "#16142a"); PixelKit.P('h', "#24223c"); PixelKit.P('p', "#2e2c48"); PixelKit.P('T', "#1e1a34");
        PixelKit.P('k', "#e0a050"); PixelKit.P('K', "#f0c070"); PixelKit.P('s', "#2c2a44"); PixelKit.P('t', "#2a2440");
        int W = 960, H = 120;
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
        {
            float top = Ridge(x, W, 64f, 14f, 9);
            for (int y = Math.Max(0, (int)top); y < H; y++) cv.Px[x, y] = y < top + 1 ? 'h' : 'H';
        }
        // estrada: da esquerda (perto, embaixo) até a cidade (direita, no alto do morro)
        for (int x = 0; x < 640; x++)
        {
            int y = (int)(H - 8 - x * 0.07 - 4 * Math.Sin(x / 50.0));
            cv.Force(x, y, 'p'); if (x < 300) cv.Force(x, y + 1, 'p');
        }
        // cidade: casas de telhado pontudo, uma torre do sino, janelas acesas, fumaça
        int cx0 = 600, cx1 = 820;
        for (int hx = cx0; hx < cx1; hx += 9 + (int)(Hash(hx, 0, 31) * 8))
        {
            int baseY = (int)Ridge(hx, W, 64f, 14f, 9) + 4;
            int h = 8 + (int)(Hash(hx, 1, 31) * 8), w = 6 + (int)(Hash(hx, 2, 31) * 5);
            for (int x = hx; x < hx + w; x++) for (int y = baseY - h; y <= baseY; y++) cv.Force(x, y, 'T');
            for (int k = 0; k <= w / 2 + 1; k++) for (int x = hx - 1 + k; x <= hx + w - k; x++) cv.Force(x, baseY - h - k, 't');
            if (Hash(hx, 3, 31) > 0.35f) cv.Force(hx + w / 2, baseY - h / 2, Hash(hx, 4, 31) > 0.6f ? 'K' : 'k');
            if (Hash(hx, 5, 31) > 0.6f)
                for (int s = 0; s < 10; s++) cv.Force(hx + w - 2 + (s / 4) % 2, baseY - h - 4 - s, s % 3 == 2 ? '.' : 's');
        }
        int bt = 712, bty = (int)Ridge(bt, W, 64f, 14f, 9) + 4;
        for (int y = bty - 28; y <= bty; y++) for (int x = bt; x < bt + 6; x++) cv.Force(x, y, 'T');
        for (int k = 0; k < 6; k++) for (int x = bt + k / 2; x < bt + 6 - k / 2; x++) cv.Force(x, bty - 28 - k, 't');
        cv.Force(bt + 2, bty - 22, 'K'); cv.Force(bt + 3, bty - 22, 'K');
        return cv;
    }
}
