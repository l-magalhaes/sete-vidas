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
            case "hurt": // olho fechado, orelhas para trás, boca aberta
                rows[0] = "...............";
                rows[1] = ".KKK..KKK......";
                set(6, 7, 'a'); set(6, 8, 'a'); set(9, 10, 'o'); set(9, 11, 'o');
                break;
        }
        return rows;
    }

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

        // braço de trás (atrás de tudo)
        if (p.BackHand.HasValue) Arm(cv, bsx, bsy, p.BackHand.Value, 'J', 'J');
        // pernas
        Leg(cv, bx + 3, p.Py + 1, p.BackFoot, true);
        Leg(cv, bx + 7, p.Py + 1, p.FrontFoot, false);
        // tronco e cabeça
        cv.Stamp(Torso, bx, ty);
        cv.Stamp(Head(p.HeadV), bx + 1 + p.HeadDx, ty - 10 + p.HeadDy);
        // lança e braço da frente (a mão por cima da haste)
        if (!p.NoSpear) Spear(cv, p.FrontHand.X + 0.5f, p.FrontHand.Y + 0.5f, p.SpearAngle, p.SpearBack);
        Arm(cv, fsx, fsy, p.FrontHand, 'I', 'J');
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
    }

    // Corpo girado para trás em volta dos pés (cai de costas), lança largada no chão.
    static Canvas Fallen(float degrees)
    {
        var body = Render(new Pose { HeadV = "hurt", NoSpear = true, FrontHand = new Point(36, 35) }, false);
        var cv = new Canvas();
        double a = -degrees * Math.PI / 180.0;                       // negativo: tomba para trás (cabeça para a esquerda)
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
        if (degrees >= 89f) Spear(cv, 36.5f, 42.5f, 0f, 10f);         // lança no chão
        else Spear(cv, 38.5f, 38.5f, 160f, 8f);
        Outline(cv);
        return cv;
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

    public static string Run(string projectRoot, string metaTemplatePath, string previewPath)
    {
        InitPalette();
        BuildClips();
        string template = File.ReadAllText(metaTemplatePath);
        string art = Path.Combine(projectRoot, "Assets/_Game/Art/Enemies/Templar");
        var y = new StringBuilder();
        y.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
        y.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n");
        y.Append("  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
        y.Append("  m_Script: {fileID: 11500000, guid: " + ScriptGuid + ", type: 3}\n  m_Name: TemplarAnimationSet\n  m_EditorClassIdentifier: \n  clips:\n");
        int frames = 0;
        foreach (var c in Clips)
        {
            y.Append("  - name: " + c.Name + "\n    frameTime: " + c.FrameTime.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n    loop: " + (c.Loop ? 1 : 0) + "\n    frames:\n");
            for (int i = 0; i < c.Frames.Count; i++)
            {
                string rel = c.Name + "/enm_templar_" + c.Name + "_" + i + ".png";
                string path = Path.Combine(art, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var bmp = ToBitmap(c.Frames[i].Px, W, H)) bmp.Save(path, ImageFormat.Png);
                string gu = Guid(rel);
                File.WriteAllText(path + ".meta", template.Replace("__GUID__", gu).Replace("__ALIGN__", "9").Replace("__PIVOTY__", "0"));
                y.Append("    - sprite: {fileID: 21300000, guid: " + gu + ", type: 3}\n      overlay: {fileID: 0}\n");
                y.Append("      tail: {x: 0, y: 0}\n      chest: {x: 0, y: 14}\n      trim: {x: 0, y: 0}\n      body: {x: 0, y: 0}\n      hasTail: 0\n      hasTrim: 0\n");
                frames++;
            }
        }
        string assetPath = Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Enemies/TemplarAnimationSet.asset");
        File.WriteAllText(assetPath, y.ToString());
        if (!File.Exists(assetPath + ".meta"))
            File.WriteAllText(assetPath + ".meta", "fileFormatVersion: 2\nguid: " + AssetGuid + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
        if (!string.IsNullOrEmpty(previewPath)) Preview(previewPath, 6);
        return "clipes: " + Clips.Count + ", frames: " + frames;
    }

    public static void Preview(string path, int S)
    {
        if (Pal.Count == 0) InitPalette();
        if (Clips.Count == 0) BuildClips();
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
