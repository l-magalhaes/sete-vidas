// Gerador dos sprites do Lion (protagonista de Sete Vidas).
// Monta cada frame a partir de partes (cabeça, tronco, pernas, braços, armas) num esqueleto em pixel,
// desenha o contorno automaticamente e grava PNG + .meta + o asset LionAnimationSet para o Unity.
// Compilado pelo PowerShell (C# 5): sem interpolação de string, sem "?.", sem membros "=>".
// Uso: Tools/LionSprites/gerar.ps1
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static partial class LionSpriteGen
{
    // ------------------------------------------------------------------ paleta

    static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>();
    static void P(char c, string hex) { Pal[c] = ColorTranslator.FromHtml(hex); }
    static void InitPalette()
    {
        Pal.Clear();
        P('o', "#1a1c26"); // contorno
        P('L', "#f4d6a3"); P('F', "#e3b77d"); P('f', "#b98552"); // pelo
        P('G', "#6a7080"); P('H', "#4a4f5c"); P('h', "#33363f"); // capuz e capa
        P('V', "#9a5b36"); P('v', "#6e3c22");                    // couro
        P('P', "#e4e2dc"); P('p', "#b3b1ad");                    // calças
        P('E', "#f2a53a"); P('N', "#d98a8a");                    // olho, nariz/língua
        P('B', "#5a3a24"); P('k', "#c2a35a");                    // cinto/bolsa, chaves
        P('T', "#2b3a67"); P('t', "#1c2645");                    // faixa da capa (classe Ladino) e sombra
        P('D', "#a9bcd6"); P('d', "#f4f7fb");                    // lâmina
        P('W', "#6b4a2b"); P('I', "#b8bfcc"); P('i', "#8a91a0"); P('Y', "#d9a441"); // marreta
        P('R', "#5b2a6e"); P('g', "#8fd14f");                    // tomo (praga)
        P('r', "#9a5ab8");                                       // veia da praga sobre pelo claro
        P('w', "#ffffff"); P('c', "#c9c2b0"); P('C', "#e6e0d0"); // branco, poeira
    }

    // ------------------------------------------------------------ geometria

    // Coordenadas de desenho ("mockup"): x para a direita, y para baixo; patas no chão na linha 20.
    // Canvas 48x48: canvas = mockup + (OX, OY). O pivot fica na borda de baixo da linha 39 (8 linhas livres embaixo).
    public const int W = 48, H = 48, OX = 14, OY = 18;
    public const float PivotY = 8f / 48f;

    public class Canvas
    {
        public char[,] Px = new char[W, H];
        public Canvas() { Clear(); }
        public void Clear() { for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) Px[x, y] = '.'; }
        public void Set(int mx, int my, char c)
        {
            if (c == '.' || c == ' ') return;
            int x = mx + OX, y = my + OY;
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[x, y] = c;
        }
        public char Get(int mx, int my)
        {
            int x = mx + OX, y = my + OY;
            if (x < 0 || y < 0 || x >= W || y >= H) return '.';
            return Px[x, y];
        }
        public void Stamp(string[] map, int mx, int my)
        {
            for (int r = 0; r < map.Length; r++)
                for (int c = 0; c < map[r].Length; c++)
                    Set(mx + c, my + r, map[r][c]);
        }
        public Canvas Clone()
        {
            var k = new Canvas();
            Array.Copy(Px, k.Px, Px.Length);
            return k;
        }
    }

    // Contorno externo de 1 px (vizinhança de 4) em volta de tudo que não é transparente.
    static void Outline(Canvas cv, char color)
    {
        var add = new List<int>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                if (cv.Px[x, y] != '.') continue;
                if (Solid(cv, x - 1, y) || Solid(cv, x + 1, y) || Solid(cv, x, y - 1) || Solid(cv, x, y + 1))
                    add.Add(x * H + y);
            }
        foreach (int i in add) cv.Px[i / H, i % H] = color;
    }
    static bool Solid(Canvas cv, int x, int y)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return false;
        return cv.Px[x, y] != '.';
    }

    // Linha (Bresenham) com espessura horizontal.
    static List<Point> Line(int x0, int y0, int x1, int y1)
    {
        var pts = new List<Point>();
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
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

    // Retângulo girado: preenche os pixels cujo centro cai dentro (u = ao longo do eixo, v = perpendicular).
    static void FillRotated(Canvas cv, float ox, float oy, float angleDeg, float u0, float u1, float halfV, Func<float, float, char> colorAt)
    {
        double a = angleDeg * Math.PI / 180.0;
        float dx = (float)Math.Cos(a), dy = (float)-Math.Sin(a); // y para baixo
        float px = -dy, py = dx;
        int minX = (int)Math.Floor(ox - 20), maxX = (int)Math.Ceiling(ox + 20);
        int minY = (int)Math.Floor(oy - 20), maxY = (int)Math.Ceiling(oy + 20);
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                float rx = x - ox, ry = y - oy;
                float u = rx * dx + ry * dy;
                float v = rx * px + ry * py;
                if (u < u0 - 0.5f || u > u1 + 0.5f || Math.Abs(v) > halfV) continue;
                char c = colorAt(u, v);
                if (c != '.') cv.Set(x, y, c);
            }
    }

    // ---------------------------------------------------------------- partes

    static readonly string[] HeadBase = {
        "..L....L....",
        ".LFf..LFf...",
        "HGGHHHHHHHH.",
        "HGHHHHHHHHHH",
        "HHHHLFFFFFFF",
        "HHHLFFEFFFEF",
        "HHHLFFoFFFoF",
        "hHHFFFFFNFFF",
        ".hhLFFFfFFF.",
        "..hhfffffff.",
    };

    static string[] Head(string variant)
    {
        var rows = (string[])HeadBase.Clone();
        Func<int, int, char, int> set = (r, c, ch) =>
        {
            var a = rows[r].ToCharArray(); a[c] = ch; rows[r] = new string(a); return 0;
        };
        switch (variant)
        {
            case "blink": set(5, 6, 'F'); set(5, 10, 'F'); break;
            case "hurt":
                set(5, 6, 'o'); set(5, 10, 'o'); set(6, 6, 'F'); set(6, 10, 'F');
                rows[0] = "............"; rows[1] = "LFf..LFf....";
                break;
            case "wide": set(4, 6, 'E'); set(4, 10, 'E'); break;
            case "earflick": rows[0] = ".......L...."; rows[1] = "..Ff..LFf..."; break;
            case "flat": rows[0] = "............"; rows[1] = "LFf..LFf...."; break;
            case "flatblink":
                rows[0] = "............"; rows[1] = "LFf..LFf....";
                set(5, 6, 'F'); set(5, 10, 'F'); break;
            case "lick": set(5, 6, 'F'); set(5, 10, 'F'); set(8, 9, 'N'); break;
            case "down": set(5, 6, 'F'); set(5, 10, 'F'); set(6, 6, 'E'); set(6, 10, 'E'); set(7, 6, 'o'); set(7, 10, 'o'); break;
            case "up": set(4, 6, 'o'); set(4, 10, 'o'); set(5, 6, 'E'); set(5, 10, 'E'); set(6, 6, 'F'); set(6, 10, 'F'); break;
            case "cursed":   // orelhas baixas, olhos acesos de verde-praga
                rows[0] = "............"; rows[1] = "LFf..LFf....";
                set(5, 6, 'g'); set(5, 10, 'g'); break;
            case "cursedup": // olhando o tomo lá em cima, olhos verdes
                set(4, 6, 'o'); set(4, 10, 'o'); set(5, 6, 'g'); set(5, 10, 'g'); set(6, 6, 'F'); set(6, 10, 'F'); break;
        }
        return rows;
    }

    static readonly Dictionary<string, string[]> Torsos = new Dictionary<string, string[]>
    {
        { "n", new[] {
            "...hTVVVVVVV",
            "..hHTvVVVVVV",
            "...hHvVVVVVV",
            "...hhvVVVVV.",
            "...hhBBkBBB.",
            "...hh.......",
            "....h.......",
        } },
        { "run", new[] {
            "...hTVVVVVVV",
            ".hhHTvVVVVVV",
            "hhHHHvVVVVVV",
            ".hhhhvVVVVV.",
            "..hh.BBkBBB.",
            "............",
            "............",
        } },
        { "up", new[] {
            ".hhhTVVVVVVV",
            "hhHHTvVVVVVV",
            ".hhHhvVVVVVV",
            "...hhvVVVVV.",
            "....hBBkBBB.",
            "............",
            "............",
        } },
    };

    // Corpo agachado (cabeça na frente, corpo baixo atrás). Desenhado em (3, 17).
    static readonly string[] CrouchBody = {
        "hhTVVVVVVVFF",
        "hhTBBkBBBBF.",
        ".hPPPPPPPP..",
        "...FFF.FFFFF",
    };

    // ----------------------------------------------------------------- pose

    public class Item { public string Kind; public float Angle; public Item(string k, float a) { Kind = k; Angle = a; } }

    public class Pose
    {
        public int Px = 7, Py = 16;                 // pelve (canto superior esquerdo do bloco de 6x2)
        public string HeadV = "n"; public int HeadDx, HeadDy;
        public string TorsoV = "n"; public int TorsoDy;
        public Point BackFoot = new Point(7, 20), FrontFoot = new Point(10, 20);
        public bool BackGround = true, FrontGround = true;
        public Point? FrontHand = new Point(14, 14), BackHand = null;
        public bool ArmsBehindHead;                 // braços atrás da cabeça (pendurado, marreta erguida)
        public bool BackArmInFront;                 // braço de trás desenhado na frente (golpe cruzado)
        public Item FrontItem, BackItem;
        public bool Crouch; public int CrouchHeadDy; public string CrouchFeet;
        public bool HideFrontPaw;                   // agachado atacando: a pata sai do mapa
        public float HammerAngle = float.NaN; public Point HammerGrip;
        public List<Action<Canvas>> Extras = new List<Action<Canvas>>();
        public PointF? TailOverride, ChestOverride; public Point? BodyOffsetOverride;
        public bool NoTail;
        public int WallX = int.MaxValue;            // escalada: a lâmina some a partir desta coluna (entra na parede)

        public Pose Clone()
        {
            var p = (Pose)MemberwiseClone();
            p.Extras = new List<Action<Canvas>>(Extras);
            return p;
        }
    }

    static Pose Std() { return new Pose(); }

    // Desenha a pose. Retorna o corpo (com contorno); a marreta vai para "overlay".
    static Canvas RenderBody(Pose p, out Canvas overlay)
    {
        var cv = new Canvas();
        DaggerClipX = p.WallX;
        overlay = null;
        int sx = p.Px + 6, sy = p.Py - 4 + p.TorsoDy;   // ombro da frente
        int bsx = p.Px + 1, bsy = sy;                    // ombro de trás

        if (p.Crouch)
        {
            cv.Stamp(Head(p.HeadV), 5 + p.HeadDx, 9 + p.CrouchHeadDy + p.HeadDy);
            var body = (string[])CrouchBody.Clone();
            if (p.CrouchFeet != null) body[3] = p.CrouchFeet;
            if (p.HideFrontPaw) { body[0] = body[0].Substring(0, 10) + ".."; body[1] = body[1].Substring(0, 10) + ".."; }
            cv.Stamp(body, 3, 17);
            sx = 12; sy = 17;
            if (p.FrontHand.HasValue) DrawArm(cv, sx, sy, p.FrontHand.Value, 'F', 'L', p.FrontItem);
        }
        else
        {
            // 1. braço de trás (atrás de tudo)
            if (p.BackHand.HasValue && !p.BackArmInFront) DrawArm(cv, bsx, bsy, p.BackHand.Value, 'f', 'F', p.BackItem);
            if (p.ArmsBehindHead && p.FrontHand.HasValue) DrawArm(cv, sx, sy, p.FrontHand.Value, 'F', 'L', p.FrontItem);
            // 2. perna de trás, pelve, perna da frente
            DrawLeg(cv, p.Px, p.Py + 2, p.BackFoot, p.BackGround, true);
            for (int x = 0; x < 6; x++) { cv.Set(p.Px + x, p.Py, 'P'); cv.Set(p.Px + x, p.Py + 1, x == 2 ? 'p' : 'P'); }
            DrawLeg(cv, p.Px + 3, p.Py + 2, p.FrontFoot, p.FrontGround, false);
            // 3. tronco e cabeça
            cv.Stamp(Torsos[p.TorsoV], p.Px - 5, p.Py - 5 + p.TorsoDy);
            cv.Stamp(Head(p.HeadV), p.Px - 3 + p.HeadDx, p.Py - 15 + p.TorsoDy + p.HeadDy);
            // 4. braço da frente
            if (p.BackHand.HasValue && p.BackArmInFront) DrawArm(cv, bsx + 3, bsy, p.BackHand.Value, 'f', 'F', p.BackItem);
            if (!p.ArmsBehindHead && p.FrontHand.HasValue) DrawArm(cv, sx, sy, p.FrontHand.Value, 'F', 'L', p.FrontItem);
        }
        DaggerClipX = int.MaxValue;
        foreach (var e in p.Extras) e(cv);
        Outline(cv, 'o');

        if (!float.IsNaN(p.HammerAngle))
        {
            overlay = new Canvas();
            DrawHammer(overlay, p.HammerGrip.X, p.HammerGrip.Y, p.HammerAngle, true);
            Outline(overlay, 'o');
        }
        return cv;
    }

    static void DrawLeg(Canvas cv, int hx, int hy, Point foot, bool grounded, bool back)
    {
        char pants = back ? 'p' : 'P', fur = back ? 'f' : 'F';
        var pts = Line(hx, hy, foot.X, foot.Y);
        for (int i = 0; i < pts.Count; i++)
        {
            bool last = i == pts.Count - 1;
            char c = last ? fur : pants;
            cv.Set(pts[i].X, pts[i].Y, c);
            cv.Set(pts[i].X + 1, pts[i].Y, c);
        }
        if (grounded && !back) cv.Set(foot.X + 2, foot.Y, fur); // pata da frente aponta para a frente
    }

    static void DrawArm(Canvas cv, int sx, int sy, Point hand, char fur, char paw, Item item)
    {
        var pts = Line(sx, sy, hand.X, hand.Y);
        for (int i = 0; i < pts.Count; i++) cv.Set(pts[i].X, pts[i].Y, i == pts.Count - 1 ? paw : fur);
        if (item != null && item.Kind == "dagger") DrawDagger(cv, hand.X, hand.Y, item.Angle);
        if (item != null && item.Kind == "key") DrawKey(cv, hand.X, hand.Y, item.Angle);
        if (item != null && item.Kind == "tome") DrawTome(cv, hand.X, hand.Y);
    }

    static int DaggerClipX = int.MaxValue;

    static void DrawDagger(Canvas cv, int hx, int hy, float angle)
    {
        if (DaggerClipX == int.MaxValue)
        {
            FillRotated(cv, hx, hy, angle, 1, 6, 0.55f, (u, v) => u < 1.5f ? 'B' : (u > 5.5f ? 'd' : 'D'));
            return;
        }
        // cravada na parede: só o cabo e o começo da lâmina aparecem; o brilho fica na borda da pedra
        var tmp = new Canvas();
        FillRotated(tmp, hx, hy, angle, 1, 6, 0.55f, (u, v) => u < 1.5f ? 'B' : 'D');
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = tmp.Px[x, y];
                if (c == '.') continue;
                int mx = x - OX;
                if (mx >= DaggerClipX) continue;
                cv.Px[x, y] = (mx == DaggerClipX - 1 && c == 'D') ? 'd' : c;
            }
    }

    static void DrawKey(Canvas cv, int hx, int hy, float angle)
    {
        FillRotated(cv, hx, hy, angle, 1, 3, 0.55f, (u, v) => 'k');
    }

    static void DrawTome(Canvas cv, int hx, int hy)
    {
        for (int x = -1; x <= 2; x++) for (int y = -3; y <= 0; y++) cv.Set(hx + x, hy + y, 'R');
        cv.Set(hx, hy - 2, 'g'); cv.Set(hx + 1, hy - 1, 'g');
    }

    // Marreta: cabo de 9 px a partir da pega, cabeça de 7x4 com faixa dourada, pomo de 2 px.
    static void DrawHammer(Canvas cv, int gx, int gy, float angle, bool hands)
    {
        FillRotated(cv, gx, gy, angle, 0, 9, 0.55f, (u, v) => 'W');
        double a = angle * Math.PI / 180.0;
        float hx = gx + (float)Math.Cos(a) * 9f, hy = gy - (float)Math.Sin(a) * 9f;
        FillRotated(cv, hx, hy, angle, 0, 3, 3.5f, (u, v) =>
        {
            if (Math.Abs(v) < 0.6f) return 'Y';
            if (Math.Abs(v) > 2.6f) return 'i';
            return u > 2.4f ? 'i' : 'I';
        });
        if (hands) { cv.Set(gx, gy, 'L'); }
    }

    // ------------------------------------------------------------- âncoras

    // Posição em pixels relativa ao pivot (x para a direita, y para cima), do centro do pixel de desenho (mx, my).
    static PointF Rel(float mx, float my) { return new PointF(mx + OX + 0.5f - 24f, 40f - (my + OY + 0.5f)); }

    public class Frame
    {
        public string Name; public Canvas Body; public Canvas Overlay;
        public PointF Tail, Chest, Trim; public bool HasTrim, HasTail; public Point BodyOffset;
    }

    static Frame MakeFrame(string name, Pose p)
    {
        Canvas ov;
        var f = new Frame { Name = name, Body = RenderBody(p, out ov), Overlay = ov };
        f.HasTail = !p.NoTail;
        if (p.Crouch)
        {
            f.Tail = p.TailOverride ?? Rel(2, 18);
            f.Chest = p.ChestOverride ?? Rel(8, 18);
            f.BodyOffset = p.BodyOffsetOverride ?? new Point(-3, 3);
        }
        else
        {
            f.Tail = p.TailOverride ?? Rel(p.Px - 3, p.Py - 1 + p.TorsoDy);
            f.Chest = p.ChestOverride ?? Rel(p.Px + 3, p.Py - 4 + p.TorsoDy);
            f.BodyOffset = p.BodyOffsetOverride ?? new Point(p.Px - 7, p.Py - 16 + p.TorsoDy);
        }
        // faixa da capa: o pixel 'T' mais alto
        f.HasTrim = false;
        for (int y = 0; y < H && !f.HasTrim; y++)
            for (int x = 0; x < W; x++)
                if (f.Body.Px[x, y] == 'T') { f.HasTrim = true; f.Trim = new PointF(x + 0.5f - 24f, 40f - (y + 1f)); break; }
        return f;
    }

    // ------------------------------------------------------------- clipes

    public class Clip
    {
        public string Name; public float FrameTime; public bool Loop; public bool Overlay;
        public List<Frame> Frames = new List<Frame>();
        public Clip(string n, float t, bool loop) { Name = n; FrameTime = t; Loop = loop; }
    }

    static readonly List<Clip> Clips = new List<Clip>();
    static Clip Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new Clip(name, t, loop);
        for (int i = 0; i < poses.Length; i++) c.Frames.Add(MakeFrame(name + "_" + i, poses[i]));
        Clips.Add(c);
        PosesByClip[name] = new List<Pose>(poses);
        return c;
    }

    static Pose With(Pose b, Action<Pose> edit) { var p = b.Clone(); edit(p); return p; }

    static Pose CrouchPose() { var p = Std(); p.Crouch = true; p.FrontHand = null; return p; }

    static Pose LandLow()
    {
        return With(Std(), p => { p.Py = 18; p.BackFoot = new Point(6, 20); p.FrontFoot = new Point(11, 20); p.FrontHand = new Point(15, 16); });
    }
    static Pose LandMid()
    {
        return With(Std(), p => { p.Py = 17; p.FrontHand = new Point(14, 15); });
    }

    static void BuildClips()
    {
        Clips.Clear();
        var std = Std();

        // ---------------- parado
        Add("idle", 0.16f, true,
            std,
            std,
            With(std, p => { p.TorsoDy = 1; p.FrontHand = new Point(14, 15); }),
            With(std, p => { p.TorsoDy = 1; p.HeadV = "earflick"; p.FrontHand = new Point(14, 15); }));
        Add("idle_blink", 0.1f, false, With(std, p => p.HeadV = "blink"));
        Add("idle_groom", 0.1f, false,
            With(std, p => p.FrontHand = new Point(15, 11)),
            With(std, p => { p.FrontHand = new Point(14, 9); p.HeadV = "blink"; }),
            With(std, p => { p.FrontHand = new Point(14, 9); p.HeadV = "lick"; p.HeadDy = 1; }),
            With(std, p => { p.FrontHand = new Point(14, 8); p.HeadV = "blink"; p.HeadDy = 1; }),
            With(std, p => { p.FrontHand = new Point(14, 9); p.HeadV = "lick"; p.HeadDy = 1; }),
            With(std, p => { p.FrontHand = new Point(14, 8); p.HeadV = "blink"; }),
            With(std, p => { p.FrontHand = new Point(15, 7); p.HeadV = "blink"; }),
            With(std, p => { p.FrontHand = new Point(14, 6); p.HeadV = "flatblink"; }),
            With(std, p => { p.FrontHand = new Point(15, 7); p.HeadV = "blink"; }),
            With(std, p => p.FrontHand = new Point(14, 13)));
        float[] keyAngles = { 90, 45, 0, -45, -90, -135 };
        var keyPoses = new List<Pose>();
        keyPoses.Add(With(std, p => p.FrontHand = new Point(15, 12)));
        foreach (float ka in keyAngles)
        {
            float a = ka;
            keyPoses.Add(With(std, p => { p.FrontHand = new Point(15, 11); p.FrontItem = new Item("key", a); p.HeadV = "down"; }));
        }
        keyPoses.Add(With(std, p => p.FrontHand = new Point(14, 13)));
        Add("idle_key", 0.09f, false, keyPoses.ToArray());

        // ---------------- corrida
        var runBase = With(std, p => { p.Px = 8; p.HeadDx = 1; p.TorsoV = "run"; });
        Add("run", 0.08f, true,
            With(runBase, p => { p.FrontFoot = new Point(14, 20); p.BackFoot = new Point(4, 19); p.BackGround = false; p.FrontHand = new Point(12, 15); }),
            With(runBase, p => { p.Py = 17; p.FrontFoot = new Point(11, 20); p.BackFoot = new Point(6, 18); p.BackGround = false; p.FrontHand = new Point(13, 15); }),
            With(runBase, p => { p.FrontFoot = new Point(9, 20); p.BackFoot = new Point(10, 18); p.BackGround = false; p.FrontHand = new Point(14, 14); }),
            With(runBase, p => { p.BackFoot = new Point(14, 20); p.FrontFoot = new Point(4, 19); p.FrontGround = false; p.FrontHand = new Point(16, 13); }),
            With(runBase, p => { p.Py = 17; p.BackFoot = new Point(11, 20); p.FrontFoot = new Point(6, 18); p.FrontGround = false; p.FrontHand = new Point(15, 14); }),
            With(runBase, p => { p.BackFoot = new Point(9, 20); p.FrontFoot = new Point(10, 18); p.FrontGround = false; p.FrontHand = new Point(14, 15); }));

        // ---------------- pulo
        var air = With(std, p => { p.BackGround = false; p.FrontGround = false; });
        Add("jump_up", 0.06f, false,
            With(air, p => { p.Py = 15; p.BackFoot = new Point(7, 21); p.FrontFoot = new Point(10, 21); p.FrontHand = new Point(15, 8); }),
            With(air, p => { p.TorsoV = "run"; p.BackFoot = new Point(6, 19); p.FrontFoot = new Point(11, 19); p.FrontHand = new Point(15, 9); }));
        Add("jump_apex", 0.1f, false,
            With(air, p => { p.BackFoot = new Point(5, 19); p.FrontFoot = new Point(12, 19); p.FrontHand = new Point(16, 11); }));
        Add("fall", 0.1f, true,
            With(air, p => { p.TorsoV = "up"; p.BackFoot = new Point(7, 20); p.FrontFoot = new Point(11, 20); p.FrontHand = new Point(15, 8); }),
            With(air, p => { p.TorsoV = "up"; p.BackFoot = new Point(6, 20); p.FrontFoot = new Point(11, 21); p.FrontHand = new Point(14, 9); }));
        Add("land", 0.06f, false, LandLow(), LandMid());
        var crouchPaws = With(CrouchPose(), p => { p.HeadV = "flat"; });
        Add("land_heavy", 0.06f, false, crouchPaws, crouchPaws, LandLow(), LandMid());

        // ---------------- agachado
        Add("crouch", 0.05f, false, LandLow(), CrouchPose());
        Add("crouch_walk", 0.12f, true,
            With(CrouchPose(), p => p.CrouchFeet = "..FFF..FFFF."),
            With(CrouchPose(), p => { p.CrouchFeet = "...FFF.FFFFF"; p.CrouchHeadDy = 1; }),
            With(CrouchPose(), p => p.CrouchFeet = "....FFFFFF.."),
            With(CrouchPose(), p => { p.CrouchFeet = "...FFF.FFFFF"; p.CrouchHeadDy = 1; }));

        // ---------------- borda e parede
        var hang = With(std, p =>
        {
            p.Py = 19; p.ArmsBehindHead = true; p.FrontHand = new Point(17, 4); p.BackHand = new Point(16, 3);
            p.BackFoot = new Point(7, 23); p.FrontFoot = new Point(10, 23); p.BackGround = false; p.FrontGround = false;
        });
        Add("ledge_hang", 0.3f, true, hang, With(hang, p => { p.BackFoot = new Point(8, 23); p.FrontFoot = new Point(11, 23); }));
        Add("ledge_climb", 0.05f, false,
            With(hang, p => { p.Py = 17; p.FrontHand = new Point(17, 6); p.BackHand = new Point(16, 5); p.BackFoot = new Point(7, 21); p.FrontFoot = new Point(10, 21); }),
            With(std, p => { p.FrontHand = new Point(16, 10); p.BackFoot = new Point(6, 19); p.FrontFoot = new Point(11, 19); p.BackGround = false; p.FrontGround = false; }),
            CrouchPose(),
            LandMid());
        // escalada com as adagas: uma crava no alto enquanto a outra segura embaixo; o corpo sobe puxando.
        // A parede começa na coluna 16 (meia largura do colisor = 6 px à frente do pivot); a lâmina entra nela.
        // O corpo fica 3 px afastado, braços esticados: as patas e os cabos aparecem no vão entre o rosto e a pedra.
        var wall = With(std, p =>
        {
            p.Px = 4; p.BackGround = false; p.FrontGround = false; p.WallX = 16;
            p.FrontItem = new Item("dagger", -30); p.BackItem = new Item("dagger", -30);
        });
        Add("wall_climb", 0.1f, true,
            // 0: a adaga da frente crava no alto (a de trás segura na altura do peito)
            With(wall, p => { p.FrontHand = new Point(13, 4); p.BackHand = new Point(13, 12); p.FrontFoot = new Point(13, 18); p.BackFoot = new Point(8, 20); }),
            // 1: puxa: o corpo sobe, as mãos descem em relação a ele, o pé de trás empurra
            With(wall, p => { p.Py = 15; p.FrontHand = new Point(13, 6); p.BackHand = new Point(13, 14); p.FrontFoot = new Point(13, 19); p.BackFoot = new Point(10, 19); }),
            // 2: a adaga de trás solta e crava no alto
            With(wall, p => { p.FrontHand = new Point(13, 9); p.BackHand = new Point(13, 2); p.FrontFoot = new Point(9, 20); p.BackFoot = new Point(13, 17); }),
            // 3: puxa de novo
            With(wall, p => { p.Py = 15; p.FrontHand = new Point(13, 11); p.BackHand = new Point(13, 4); p.FrontFoot = new Point(10, 19); p.BackFoot = new Point(13, 18); }));
        Add("wall_jump", 0.06f, false,
            With(air, p => { p.TorsoV = "run"; p.BackFoot = new Point(4, 19); p.FrontFoot = new Point(5, 18); p.FrontHand = new Point(16, 10); }),
            With(air, p => { p.TorsoV = "run"; p.BackFoot = new Point(6, 19); p.FrontFoot = new Point(11, 19); p.FrontHand = new Point(15, 9); }));

        // ---------------- adagas (Ladino): golpe 1 = adaga da frente, golpe 2 = adaga de trás de cima para baixo
        var stance = With(std, p => { p.BackFoot = new Point(6, 20); p.FrontFoot = new Point(11, 20); });
        var lunge = With(stance, p => { p.Px = 8; p.HeadDx = 1; p.FrontFoot = new Point(13, 20); p.TorsoV = "run"; });
        Add("dagger1_windup", 0.04f, false, With(stance, p => { p.FrontHand = new Point(11, 13); p.FrontItem = new Item("dagger", 160); }));
        Add("dagger1_active", 0.04f, false,
            With(lunge, p => { p.FrontHand = new Point(16, 13); p.FrontItem = new Item("dagger", 0); }),
            With(lunge, p => { p.FrontHand = new Point(16, 14); p.FrontItem = new Item("dagger", -25); }));
        Add("dagger1_recovery", 0.1f, false, With(lunge, p => { p.FrontHand = new Point(15, 15); p.FrontItem = new Item("dagger", -60); }));
        Add("dagger2_windup", 0.04f, false, With(stance, p =>
        {
            p.Py = 15; p.BackFoot = new Point(7, 20); p.FrontFoot = new Point(10, 20);
            p.BackHand = new Point(13, 5); p.BackArmInFront = true; p.BackItem = new Item("dagger", 110); p.FrontHand = new Point(13, 14);
        }));
        Add("dagger2_active", 0.04f, false,
            With(lunge, p => { p.BackHand = new Point(16, 10); p.BackArmInFront = true; p.BackItem = new Item("dagger", -30); p.FrontHand = new Point(13, 15); }),
            With(lunge, p => { p.Py = 17; p.BackHand = new Point(16, 15); p.BackArmInFront = true; p.BackItem = new Item("dagger", -70); p.FrontHand = new Point(13, 16); }));
        Add("dagger2_recovery", 0.1f, false,
            With(stance, p =>
            {
                p.FrontHand = new Point(14, 13); p.FrontItem = new Item("dagger", 45);
                p.BackHand = new Point(15, 13); p.BackArmInFront = true; p.BackItem = new Item("dagger", 135);
            }),
            With(stance, p => { p.FrontHand = new Point(14, 14); p.FrontItem = new Item("dagger", -45); }));
        // golpe 3 (melhoria 4): estocada com as duas adagas e avanço. Recolhe as duas atrás do corpo, se joga para
        // a frente com as pernas abertas e as lâminas retas, e volta à guarda. Só no chão (no ar o combo tem 2 golpes).
        var deep = With(stance, p => { p.Px = 9; p.HeadDx = 1; p.TorsoV = "run"; p.BackFoot = new Point(5, 20); p.FrontFoot = new Point(15, 20); });
        Add("dagger3_windup", 0.04f, false,
            With(stance, p =>
            {
                p.Px = 6; p.Py = 17; p.HeadDx = -1; p.BackFoot = new Point(5, 20); p.FrontFoot = new Point(11, 20);
                p.FrontHand = new Point(10, 14); p.FrontItem = new Item("dagger", 180);
                p.BackHand = new Point(8, 13); p.BackItem = new Item("dagger", 170);
            }));
        Add("dagger3_active", 0.04f, false,
            With(deep, p =>
            {
                p.FrontHand = new Point(18, 13); p.FrontItem = new Item("dagger", 0);
                p.BackHand = new Point(17, 15); p.BackArmInFront = true; p.BackItem = new Item("dagger", 5);
            }),
            With(deep, p =>
            {
                p.Px = 10; p.FrontFoot = new Point(16, 20);
                p.FrontHand = new Point(19, 13); p.FrontItem = new Item("dagger", 0);
                p.BackHand = new Point(18, 15); p.BackArmInFront = true; p.BackItem = new Item("dagger", 0);
            }));
        Add("dagger3_recovery", 0.1f, false,
            With(deep, p =>
            {
                p.Px = 10; p.Py = 17; p.FrontFoot = new Point(16, 20);
                p.FrontHand = new Point(17, 15); p.FrontItem = new Item("dagger", -35);
                p.BackHand = new Point(16, 16); p.BackArmInFront = true; p.BackItem = new Item("dagger", -50);
            }),
            With(lunge, p => { p.FrontHand = new Point(15, 14); p.FrontItem = new Item("dagger", -45); }),
            With(stance, p => { p.FrontHand = new Point(14, 14); p.FrontItem = new Item("dagger", -45); }));
        // no ar: mesmos braços, pernas encolhidas
        foreach (string nm in new[] { "dagger1_windup", "dagger1_active", "dagger1_recovery", "dagger2_windup", "dagger2_active", "dagger2_recovery" })
            AddAirVariant(nm);
        // agachado
        var cAtk = With(CrouchPose(), p => p.HideFrontPaw = true);
        Add("dagger_crouch_windup", 0.04f, false, With(cAtk, p => { p.FrontHand = new Point(11, 18); p.FrontItem = new Item("dagger", 170); }));
        Add("dagger_crouch_active", 0.04f, false,
            With(cAtk, p => { p.FrontHand = new Point(17, 18); p.FrontItem = new Item("dagger", 0); }),
            With(cAtk, p => { p.FrontHand = new Point(17, 19); p.FrontItem = new Item("dagger", -15); }));
        Add("dagger_crouch_recovery", 0.1f, false, With(cAtk, p => { p.FrontHand = new Point(15, 19); p.FrontItem = new Item("dagger", -30); }));

        // ---------------- marreta (Paladino): corpo + camada da marreta, mesmo canvas e pivot
        Add("hammer1_windup", 0.1f, false,
            With(stance, p => { p.Py = 17; p.FrontHand = new Point(14, 12); p.HammerGrip = new Point(14, 12); p.HammerAngle = 60; }),
            With(stance, p => { p.ArmsBehindHead = true; p.FrontHand = new Point(11, 0); p.BackHand = new Point(10, 1); p.HammerGrip = new Point(11, 0); p.HammerAngle = 95; }),
            With(stance, p => { p.Px = 6; p.HeadDx = -1; p.TorsoV = "up"; p.ArmsBehindHead = true; p.FrontHand = new Point(9, 0); p.BackHand = new Point(8, 1); p.HammerGrip = new Point(9, 0); p.HammerAngle = 140; }));
        Add("hammer1_active", 0.06f, false,
            With(lunge, p => { p.FrontHand = new Point(17, 9); p.HammerGrip = new Point(17, 9); p.HammerAngle = 25; }),
            With(lunge, p => { p.Px = 9; p.Py = 17; p.HeadDx = 1; p.HeadDy = 1; p.FrontFoot = new Point(14, 20); p.FrontHand = new Point(16, 13); p.HammerGrip = new Point(16, 13); p.HammerAngle = -55; }));
        Add("hammer1_recovery", 0.12f, false,
            With(lunge, p => { p.Px = 9; p.Py = 17; p.HeadDx = 1; p.HeadDy = 1; p.FrontFoot = new Point(14, 20); p.FrontHand = new Point(16, 13); p.HammerGrip = new Point(16, 13); p.HammerAngle = -55; }),
            With(stance, p => { p.Px = 8; p.FrontHand = new Point(15, 12); p.HammerGrip = new Point(15, 12); p.HammerAngle = 35; }));
        foreach (string nm in new[] { "hammer1_windup", "hammer1_active", "hammer1_recovery" }) AddAirVariant(nm);
        // ataque para baixo no ar (melhoria 5): estocada da adaga (prepara, crava embaixo dos pés, quica) e
        // queda pesada da marreta (ergue, despenca com a cabeça para baixo, crava no chão agachado)
        var tuck = With(std, p => { p.Py = 15; p.BackGround = false; p.FrontGround = false; p.BackFoot = new Point(7, 19); p.FrontFoot = new Point(10, 19); });
        Add("dagger_down", 0.1f, false,
            With(tuck, p => { p.FrontHand = new Point(15, 6); p.FrontItem = new Item("dagger", 75); }),
            With(tuck, p => { p.HeadV = "down"; p.FrontHand = new Point(11, 19); p.FrontItem = new Item("dagger", -90); p.BackHand = new Point(9, 17); }),
            With(tuck, p => { p.Py = 14; p.BackFoot = new Point(6, 20); p.FrontFoot = new Point(11, 19); p.FrontHand = new Point(13, 16); p.FrontItem = new Item("dagger", -60); }));
        Add("hammer_down", 0.1f, false,
            With(tuck, p => { p.ArmsBehindHead = true; p.FrontHand = new Point(11, 0); p.BackHand = new Point(10, 1); p.HammerGrip = new Point(11, 0); p.HammerAngle = 95; }),
            With(tuck, p =>
            {
                p.HeadV = "down"; p.BackFoot = new Point(7, 17); p.FrontFoot = new Point(10, 17);
                p.FrontHand = new Point(12, 15); p.BackHand = new Point(11, 15); p.BackArmInFront = true;
                p.HammerGrip = new Point(12, 15); p.HammerAngle = -90;
            }),
            With(stance, p =>
            {
                p.Py = 19; p.HeadDy = 1; p.BackFoot = new Point(5, 20); p.FrontFoot = new Point(12, 20);
                p.FrontHand = new Point(13, 13); p.BackHand = new Point(12, 13); p.BackArmInFront = true;
                p.HammerGrip = new Point(13, 13); p.HammerAngle = -88;
            }));
        // carregando o terremoto (melhoria 4): agachado, pernas abertas, marreta erguida atrás da cabeça; respira
        // (o corpo desce 1 px) e a cabeça da marreta treme. O brilho dourado é do jogo (cor da camada + faíscas).
        var charge = With(stance, p =>
        {
            p.Px = 6; p.Py = 18; p.HeadDx = -1; p.TorsoV = "up"; p.ArmsBehindHead = true;
            p.BackFoot = new Point(4, 20); p.FrontFoot = new Point(12, 20);
            p.FrontHand = new Point(9, 2); p.BackHand = new Point(8, 3); p.HammerGrip = new Point(9, 2); p.HammerAngle = 140;
        });
        Add("hammer_charge", 0.12f, true,
            charge,
            With(charge, p => { p.Py = 19; p.FrontHand = new Point(9, 3); p.BackHand = new Point(8, 4); p.HammerGrip = new Point(9, 3); p.HammerAngle = 146; }));
        // terremoto: sobe na ponta dos pés, desce a marreta com tudo e crava a cabeça no chão à frente, agachado
        var impact = With(stance, p =>
        {
            p.Px = 9; p.Py = 19; p.HeadDx = 1; p.HeadDy = 1; p.TorsoV = "run";
            p.BackFoot = new Point(4, 20); p.FrontFoot = new Point(14, 20);
            p.FrontHand = new Point(15, 13); p.BackHand = new Point(14, 13); p.BackArmInFront = true;
            p.HammerGrip = new Point(15, 13); p.HammerAngle = -62;
        });
        Add("hammer_slam", 0.08f, false,
            With(stance, p =>
            {
                p.Px = 7; p.Py = 15; p.TorsoV = "up"; p.ArmsBehindHead = true;
                p.FrontHand = new Point(10, 0); p.BackHand = new Point(9, 1); p.HammerGrip = new Point(10, 0); p.HammerAngle = 100;
            }),
            With(lunge, p => { p.Py = 17; p.FrontHand = new Point(17, 8); p.HammerGrip = new Point(17, 8); p.HammerAngle = 20; }),
            impact,
            impact,
            With(impact, p => { p.Py = 18; p.HeadDy = 0; }),
            With(stance, p => { p.Px = 8; p.FrontHand = new Point(15, 12); p.HammerGrip = new Point(15, 12); p.HammerAngle = -30; }));

        // ---------------- dano, morte, renascimento
        Add("hurt", 0.08f, false,
            With(air, p => { p.Px = 6; p.HeadV = "hurt"; p.TorsoV = "run"; p.BackFoot = new Point(5, 20); p.FrontFoot = new Point(9, 19); p.FrontHand = new Point(15, 9); }),
            With(std, p => { p.HeadV = "flat"; p.FrontHand = new Point(14, 13); }));
        var deathPose = With(std, p => { p.Px = 6; p.HeadV = "wide"; p.TorsoV = "up"; p.FrontHand = new Point(16, 10); p.BackFoot = new Point(6, 20); p.FrontFoot = new Point(10, 20); });
        Add("death", 0.1f, false, deathPose);
        var crouchFrame = MakeFrame("tmp", CrouchPose());
        var respawn = new Clip("respawn", 0.06f, false);
        respawn.Frames.Add(Silhouette("respawn_0", crouchFrame, 'w', 0));
        respawn.Frames.Add(Silhouette("respawn_1", crouchFrame, 'w', 1));
        respawn.Frames.Add(Silhouette("respawn_2", crouchFrame, 'w', 2));
        respawn.Frames.Add(Silhouette("respawn_3", crouchFrame, 'w', 3));
        respawn.Frames.Add(MakeFrame("respawn_4", CrouchPose()));
        respawn.Frames.Add(MakeFrame("respawn_5", LandMid()));
        Clips.Add(respawn);
        var hurtFlash = new Clip("hurt_flash", 0.05f, false);
        hurtFlash.Frames.Add(Silhouette("hurt_flash_0", Clips.Find(c => c.Name == "hurt").Frames[0], 'w', 2));
        Clips.Add(hurtFlash);

        // ---------------- cenas: maldição, caído, levantar, pegar o tomo, olhar
        var lying = Lying("lying_0", "blink");
        var lyingClip = new Clip("lying", 0.5f, true); lyingClip.Frames.Add(lying); Clips.Add(lyingClip);
        // o selo rompe: o tomo explode na pata, ele é jogado para trás, leva as patas ao peito, cai de joelhos e tomba.
        // As veias roxas sobem pelo pelo a cada frame (Corrupt). O tomo sai da pata no frame 1 (o jogo o faz flutuar).
        var kneel = new Clip("curse_kneel", 0.13f, false);
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_0", With(std, p => { p.HeadV = "wide"; p.FrontHand = new Point(16, 8); p.FrontItem = new Item("tome", 0); })), 0, 0));
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_1", With(std, p => { p.Px = 6; p.HeadDx = -1; p.TorsoV = "up"; p.HeadV = "hurt"; p.FrontHand = new Point(17, 9); p.BackFoot = new Point(5, 20); p.FrontFoot = new Point(10, 20); })), 1, 1));
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_2", With(std, p => { p.Py = 17; p.HeadV = "cursed"; p.FrontHand = new Point(13, 12); p.BackHand = new Point(12, 12); p.BackArmInFront = true; })), 2, 2));
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_3", With(LandLow(), p => { p.HeadV = "cursed"; p.HeadDy = 1; p.FrontHand = new Point(13, 14); p.BackHand = new Point(12, 14); p.BackArmInFront = true; })), 2, 3));
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_4", With(CrouchPose(), p => p.HeadV = "cursed")), 3, 4));
        kneel.Frames.Add(Corrupt(MakeFrame("curse_kneel_5", With(CrouchPose(), p => { p.HeadV = "flatblink"; p.CrouchHeadDy = 1; })), 3, 5));
        kneel.Frames.Add(Corrupt(Lying("curse_kneel_6", "hurt", new Point(4, 0), 0, 0), 3, 6));
        Clips.Add(kneel);
        // no chão, enquanto o tomo suga as vidas: se contorce, ergue a cabeça e estende a pata para o tomo (loop)
        var writhe = new Clip("curse_writhe", 0.16f, true);
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_0", "hurt", new Point(4, 0), 0, 0), 3, 10));
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_1", "cursed", new Point(5, 3), 1, 1), 4, 11));
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_2", "cursedup", new Point(6, 6), 2, 0), 4, 12));
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_3", "cursedup", new Point(6, 9), 2, 1), 4, 13));
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_4", "cursed", new Point(5, 4), 1, 0), 4, 14));
        writhe.Frames.Add(Corrupt(Lying("curse_writhe_5", "flatblink", new Point(5, 1), 0, 0), 3, 15));
        Clips.Add(writhe);
        var getUp = new Clip("get_up", 0.12f, false);
        getUp.Frames.Add(Lying("get_up_0", "n"));
        getUp.Frames.Add(MakeFrame("get_up_1", With(CrouchPose(), p => { p.HeadV = "flatblink"; p.CrouchHeadDy = 1; })));
        getUp.Frames.Add(MakeFrame("get_up_2", With(CrouchPose(), p => p.HeadV = "flat")));
        getUp.Frames.Add(MakeFrame("get_up_3", LandLow()));
        getUp.Frames.Add(MakeFrame("get_up_4", LandMid()));
        getUp.Frames.Add(MakeFrame("get_up_5", std));
        Clips.Add(getUp);
        // cochilo de gatinho na caixinha de areia do santuário: enrolado, respira devagar, mexe uma orelha de vez em quando
        var nap = new Clip("nap", 0.3f, true);
        nap.Frames.Add(Nap("nap_0", 0, "sleep"));
        nap.Frames.Add(Nap("nap_1", 0, "sleep"));
        nap.Frames.Add(Nap("nap_2", 1, "sleep"));
        nap.Frames.Add(Nap("nap_3", 1, "sleep"));
        nap.Frames.Add(Nap("nap_4", 0, "sleep"));
        nap.Frames.Add(Nap("nap_5", 0, "sleep"));
        nap.Frames.Add(Nap("nap_6", 0, "sleepflick"));
        nap.Frames.Add(Nap("nap_7", 0, "sleep"));
        Clips.Add(nap);
        // acorda: abre os olhos, boceja, espreguiça com as patas lá na frente e levanta
        var wake = new Clip("nap_wake", 0.12f, false);
        wake.Frames.Add(Nap("nap_wake_0", 0, "n"));
        wake.Frames.Add(Nap("nap_wake_1", 1, "yawn"));
        wake.Frames.Add(Nap("nap_wake_2", 1, "yawn"));
        wake.Frames.Add(Lying("nap_wake_3", "blink", new Point(7, 0), 0, 0));
        wake.Frames.Add(Lying("nap_wake_4", "n", new Point(5, 1), 1, 0));
        wake.Frames.Add(MakeFrame("nap_wake_5", With(CrouchPose(), p => p.HeadV = "blink")));
        wake.Frames.Add(MakeFrame("nap_wake_6", LandLow()));
        wake.Frames.Add(MakeFrame("nap_wake_7", LandMid()));
        wake.Frames.Add(MakeFrame("nap_wake_8", std));
        Clips.Add(wake);
        Add("grab_tome", 0.12f, false,
            With(std, p => p.FrontHand = new Point(16, 13)),
            With(std, p => { p.FrontHand = new Point(16, 12); p.HeadV = "down"; }),
            With(std, p => { p.FrontHand = new Point(15, 12); p.FrontItem = new Item("tome", 0); p.HeadV = "down"; }),
            With(std, p => { p.FrontHand = new Point(15, 8); p.FrontItem = new Item("tome", 0); }),
            With(std, p => { p.FrontHand = new Point(15, 7); p.FrontItem = new Item("tome", 0); p.HeadV = "wide"; }));
        Add("look_up", 0.1f, false, With(std, p => p.HeadV = "up"));
        Add("look_down", 0.1f, false, With(std, p => p.HeadV = "down"));
        // magia de cura (aprendida com o Koda): patas juntas no peito, olhos fechados; o código acende a luz dourada
        Add("heal", 0.12f, false,
            With(std, p => { p.FrontHand = new Point(13, 12); p.BackHand = new Point(12, 12); p.BackArmInFront = true; }),
            With(std, p => { p.FrontHand = new Point(12, 11); p.BackHand = new Point(12, 11); p.BackArmInFront = true; p.HeadV = "blink"; }),
            With(std, p => { p.TorsoDy = 1; p.FrontHand = new Point(12, 12); p.BackHand = new Point(11, 12); p.BackArmInFront = true; p.HeadV = "blink"; }));

        // ---------------- camada da marreta nas costas (classe Paladino, locomoção)
        var back = new Clip("hammer_back", 1f, false) { Overlay = true };
        var bf = new Frame { Name = "hammer_back_0", Body = new Canvas() };
        DrawHammer(bf.Body, 6, 16, 100, false);
        Outline(bf.Body, 'o');
        back.Frames.Add(bf);
        Clips.Add(back);
    }

    static void AddAirVariant(string groundName)
    {
        var src = PosesByClip[groundName];
        var poses = new List<Pose>();
        foreach (var s in src)
            poses.Add(With(s, p =>
            {
                p.BackGround = false; p.FrontGround = false;
                p.BackFoot = new Point(p.Px - 1, p.Py + 3); p.FrontFoot = new Point(p.Px + 4, p.Py + 3);
            }));
        var c = Clips.Find(k => k.Name == groundName);
        // dagger1_windup → dagger1_air_windup
        int u = groundName.IndexOf('_');
        string airName = groundName.Substring(0, u) + "_air" + groundName.Substring(u);
        Add(airName, c.FrameTime, c.Loop, poses.ToArray());
    }

    // As poses de cada clipe (para gerar as variantes no ar).
    static readonly Dictionary<string, List<Pose>> PosesByClip = new Dictionary<string, List<Pose>>();

    static Frame Silhouette(string name, Frame src, char color, int stage)
    {
        var f = new Frame { Name = name, Body = new Canvas(), Tail = src.Tail, Chest = src.Chest, BodyOffset = src.BodyOffset, HasTail = stage >= 3 };
        // 0: só uma centelha no peito; 1: silhueta branca pela metade (xadrez); 2: silhueta branca; 3: cores com contorno branco
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = src.Body.Px[x, y];
                if (c == '.') continue;
                bool edge = c == 'o';
                switch (stage)
                {
                    case 1: if (!edge && ((x + y) & 1) == 0) f.Body.Px[x, y] = color; break;
                    case 2: f.Body.Px[x, y] = edge ? 'C' : color; break;
                    case 3: f.Body.Px[x, y] = edge ? color : c; break;
                }
            }
        if (stage == 0)
        {
            int cx = (int)Math.Round(src.Chest.X + 24f - 0.5f), cy = (int)Math.Round(40f - src.Chest.Y - 0.5f);
            for (int d = -1; d <= 1; d++) { SetC(f.Body, cx + d, cy, color); SetC(f.Body, cx, cy + d, color); }
        }
        return f;
    }
    static void SetC(Canvas cv, int x, int y, char c) { if (x >= 0 && y >= 0 && x < W && y < H) cv.Px[x, y] = c; }

    // Veias roxas da praga sobre pelo, capuz e calças: <paramref name="level"/> 0..4 (mais veias), <paramref name="seed"/>
    // muda o desenho para as veias "correrem" de um frame para o outro. Algumas viram brasas verdes.
    static Frame Corrupt(Frame f, int level, int seed)
    {
        if (level <= 0) return f;
        var cv = f.Body;
        for (int x = 1; x < W - 1; x++)
            for (int y = 1; y < H - 1; y++)
            {
                char c = cv.Px[x, y];
                if (c != 'L' && c != 'F' && c != 'f' && c != 'G' && c != 'H' && c != 'P' && c != 'p') continue;
                int h = (x * 73856093) ^ (y * 19349663) ^ (seed * 83492791);
                h = (h ^ (h >> 13)) & 1023;
                bool vein = ((x + 2 * y + seed) % 7 == 0 && h < 260 + level * 110) || h < level * 22;
                if (!vein) continue;
                cv.Px[x, y] = (h & 31) == 0 && level >= 3 ? 'g' : (c == 'L' || c == 'P' ? 'r' : 'R');
            }
        return f;
    }

    // Deitado de lado: o corpo em pé (sem contorno) girado 90° (cabeça para a direita), apoiado no chão.
    static Frame Lying(string name, string head) { return Lying(name, head, new Point(4, 0), 0, 0); }

    // Enrolado como um gatinho (cabeça para a direita, deitada nas patinhas): o corpo é uma bolinha com a capa por
    // cima, a calça no quadril e a pata de trás dobrada embaixo. A cauda (SoftBody) sai de trás e se deita no chão.
    // <paramref name="breathe"/>: as costas sobem 1 px (inspira). <paramref name="head"/>: "sleep" (olhos fechados),
    // "sleepflick" (dormindo, orelha mexe), "n" (acordou) ou "yawn" (bocejo, cabeça 1 px mais alta).
    static Frame Nap(string name, int breathe, string head)
    {
        var fin = new Canvas();
        const int ground = 39;
        // bolinha do corpo
        // costas em domo (mais alto no meio, caindo para o quadril), faixas da capa curvas seguindo a bolinha
        float cx = 18f, cy = 37.5f, rx = 8.5f, ry = 7f + breathe;
        for (int x = 9; x <= 27; x++)
            for (int y = 28; y <= ground; y++)
            {
                float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                if (y + 0.5f > cy) ny = (y + 0.5f - cy) / 2.2f;              // embaixo é achatado (apoiado no chão)
                float d = (float)Math.Sqrt(nx * nx + ny * ny);
                if (d > 1f) continue;
                char c;
                if (ny < 0 && d > 0.72f) c = nx > 0.1f && ny < -0.6f ? 'G' : 'H';   // capa nas costas, luz em cima
                else if (ny < 0 && d > 0.58f) c = 'T';                               // faixa da capa (cor da classe)
                else if (ny < 0 && d > 0.5f) c = 't';
                else if (nx < -0.35f) c = ny > 0.4f ? 'f' : 'F';                     // quadril de pelo (pata de trás dobrada)
                else c = ny > 0.4f ? 'v' : 'V';                                      // colete de couro
                fin.Px[x, y] = c;
            }
        for (int x = 12; x <= 14; x++) fin.Px[x, ground - 3] = 'f';                   // dobra da coxa
        // pata de trás dobrada embaixo do corpo
        for (int x = 10; x <= 13; x++) fin.Px[x, ground] = x == 10 ? 'L' : 'F';
        fin.Px[11, ground - 1] = 'f';
        // cabeça deitada nas patinhas
        string variant = head == "sleep" ? "blink" : head == "sleepflick" ? "earflick" : head == "yawn" ? "blink" : "n";
        var hd = Head(variant);
        if (head == "sleepflick")
            for (int r = 0; r < hd.Length; r++) hd[r] = hd[r].Replace('E', 'F');      // dormindo: olhos fechados
        if (head == "yawn")
        {
            var a = hd[8].ToCharArray(); a[7] = 'o'; a[8] = 'N'; a[9] = 'o'; hd[8] = new string(a);    // boca bem aberta
            var b = hd[9].ToCharArray(); b[7] = 'o'; b[8] = 'N'; b[9] = 'o'; hd[9] = new string(b);
            hd[0] = "...L....L..."; hd[1] = "..LFf..LFf..";                                  // orelhas esticadas para trás
        }
        int lift = head == "yawn" ? 2 : 0;
        int hx = 22, hy = ground - lift - (hd.Length - 1);
        for (int r = 0; r < hd.Length; r++)
            for (int c = 0; c < hd[r].Length; c++)
            {
                char ch = hd[r][c]; if (ch == '.' || ch == ' ') continue;
                int x = hx + c, y = hy + r;
                if (x >= 0 && y >= 0 && x < W && y < H) fin.Px[x, y] = ch;
            }
        // patinhas da frente aparecendo embaixo do queixo
        for (int x = 31; x <= 35; x++) if (fin.Px[x, ground] == '.' || x >= 33) fin.Px[x, ground] = x == 35 ? 'L' : (x % 2 == 0 ? 'L' : 'F');
        fin.Px[34, ground - 1] = 'F';
        Outline(fin, 'o');
        Func<int, int, PointF> at = (x, y) => new PointF(x + 0.5f - 24f, 40f - (y + 0.5f));
        return new Frame { Name = name, Body = fin, Tail = at(10, 36), Chest = at(18, 35), BodyOffset = new Point(0, 0), HasTail = true };
    }

    // Caído de bruços (cabeça para a direita): o corpo em pé (sem cabeça e sem braço) girado 90°, de barriga no
    // chão; a cabeça é carimbada em pé na ponta, para o rosto ficar legível.
    // <paramref name="reach"/>: onde a pata da frente vai, em px a partir do ombro (x para a frente, y para cima).
    // <paramref name="headLift"/>: quanto ele ergue a cabeça. <paramref name="spasm"/>: o tronco treme 1 px para cima.
    static Frame Lying(string name, string head, Point reach, int headLift, int spasm)
    {
        var p = Std();
        var cv = new Canvas();
        DrawLeg(cv, p.Px, p.Py + 2, p.BackFoot, false, true);
        for (int x = 0; x < 6; x++) { cv.Set(p.Px + x, p.Py, 'P'); cv.Set(p.Px + x, p.Py + 1, 'P'); }
        DrawLeg(cv, p.Px + 3, p.Py + 2, p.FrontFoot, false, false);
        cv.Stamp(Torsos["n"], p.Px - 5, p.Py - 5);
        // gira no sentido horário: (x, y) → (H-1-y, x): a frente (barriga) fica para baixo, as costas para cima
        var rot = new Canvas();
        int minX = W, maxX = -1, maxY = -1;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = cv.Px[x, y]; if (c == '.') continue;
                int nx = H - 1 - y, ny = x;
                rot.Px[nx, ny] = c;
                minX = Math.Min(minX, nx); maxX = Math.Max(maxX, nx); maxY = Math.Max(maxY, ny);
            }
        int shiftX = 20 - (minX + maxX + 1) / 2, shiftY = 38 - maxY;
        Func<int, int, Point> mapPx = (mx, my) => new Point(H - 1 - (my + OY) + shiftX, mx + OX + shiftY);
        var fin = new Canvas();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                char c = rot.Px[x, y]; if (c == '.') continue;
                int nx = x + shiftX, ny = y + shiftY;
                if (spasm > 0 && ny < 37) ny -= spasm;
                if (nx >= 0 && ny >= 0 && nx < W && ny < H) fin.Px[nx, ny] = c;
                if (spasm > 0 && y + shiftY == 37 && ny >= 0 && ny < H) fin.Px[nx, 36] = c; // sem buraco
            }
        // cabeça em pé na ponta do tronco (o capuz cobre o pescoço)
        var neck = mapPx(p.Px + 2, p.Py - 5);
        var hd = Head(head);
        int hx = neck.X - 3, hy = 38 - headLift - spasm - (hd.Length - 1);
        for (int r = 0; r < hd.Length; r++)
            for (int c = 0; c < hd[r].Length; c++)
            {
                char ch = hd[r][c]; if (ch == '.' || ch == ' ') continue;
                int x = hx + c, y = hy + r;
                if (x >= 0 && y >= 0 && x < W && y < H) fin.Px[x, y] = ch;
            }
        // pata da frente: sai debaixo do queixo e vai para a frente (ou sobe, estendida para o tomo)
        var sh = new Point(hx + 10, 37 - spasm);
        var hand = new Point(sh.X + reach.X, sh.Y - reach.Y);
        var pts = Line(sh.X, sh.Y, hand.X, hand.Y);
        for (int i = 0; i < pts.Count; i++)
        {
            var q = pts[i];
            if (q.X < 0 || q.Y < 0 || q.X >= W || q.Y >= H) continue;
            fin.Px[q.X, q.Y] = i >= pts.Count - 2 ? 'L' : 'F';
            if (i == pts.Count - 1 && q.Y + 1 < H && fin.Px[q.X, q.Y + 1] == '.') fin.Px[q.X, q.Y + 1] = 'L'; // patinha de 2 px
        }
        Outline(fin, 'o');
        Func<int, int, PointF> map = (mx, my) => { var q = mapPx(mx, my); return new PointF(q.X + 0.5f - 24f, 40f - (q.Y + 0.5f)); };
        return new Frame { Name = name, Body = fin, Tail = map(p.Px - 3, p.Py - 1), Chest = map(p.Px + 3, p.Py - 4), BodyOffset = new Point(0, 0), HasTail = true };
    }

    // ------------------------------------------------------------------ FX

    public class Fx
    {
        public string Name; public int Size; public List<Canvas> Frames = new List<Canvas>();
        public int SW, SH; // tamanho real do PNG
    }
    static readonly List<Fx> Effects = new List<Fx>();

    static char[,] NewGrid(int w, int h) { var g = new char[w, h]; for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) g[x, y] = '.'; return g; }

    class Grid { public int W, H; public char[,] Px; public Grid(int w, int h) { W = w; H = h; Px = NewGrid(w, h); } }
    static readonly Dictionary<string, List<Grid>> FxGrids = new Dictionary<string, List<Grid>>();

    static void Disc(Grid g, float cx, float cy, float r, char c, char top)
    {
        for (int x = 0; x < g.W; x++)
            for (int y = 0; y < g.H; y++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) g.Px[x, y] = (dy < -r * 0.3f && top != '.') ? top : c;
            }
    }
    static void Ring(Grid g, float cx, float cy, float r, float t, char c)
    {
        for (int x = 0; x < g.W; x++)
            for (int y = 0; y < g.H; y++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d <= r && d >= r - t) g.Px[x, y] = c;
            }
    }
    static void GridOutline(Grid g, char c)
    {
        var add = new List<Point>();
        for (int x = 0; x < g.W; x++)
            for (int y = 0; y < g.H; y++)
            {
                if (g.Px[x, y] != '.') continue;
                bool n = (x > 0 && g.Px[x - 1, y] != '.') || (x < g.W - 1 && g.Px[x + 1, y] != '.') ||
                         (y > 0 && g.Px[x, y - 1] != '.') || (y < g.H - 1 && g.Px[x, y + 1] != '.');
                if (n) add.Add(new Point(x, y));
            }
        foreach (var p in add) g.Px[p.X, p.Y] = c;
    }

    static void BuildFx()
    {
        FxGrids.Clear();
        // poeira do pulo: duas nuvens subindo pelos lados (16x8)
        var jump = new List<Grid>();
        for (int i = 0; i < 5; i++)
        {
            var g = new Grid(16, 8); float r = 2.6f - i * 0.45f, spread = 2.5f + i * 1.2f, y = 6f - i * 0.6f;
            Disc(g, 8 - spread, y, r, 'c', 'C'); Disc(g, 8 + spread, y, r, 'c', 'C'); jump.Add(g);
        }
        FxGrids["fx_dust_jump"] = jump;
        // poeira da aterrissagem: nuvens saindo para os lados (24x8)
        var land = new List<Grid>();
        for (int i = 0; i < 6; i++)
        {
            var g = new Grid(24, 8); float r = 3f - i * 0.45f, spread = 3f + i * 2f, y = 5.5f - i * 0.3f;
            Disc(g, 12 - spread, y, r, 'c', 'C'); Disc(g, 12 + spread, y, r, 'c', 'C');
            if (i < 2) Disc(g, 12, 6.5f, 2f - i, 'c', 'C');
            land.Add(g);
        }
        FxGrids["fx_dust_land"] = land;
        // passo: nuvenzinha atrás do pé (6x6)
        var step = new List<Grid>();
        for (int i = 0; i < 3; i++) { var g = new Grid(6, 6); Disc(g, 3f - i * 0.6f, 4f - i * 0.7f, 2f - i * 0.5f, 'c', 'C'); step.Add(g); }
        FxGrids["fx_dust_step"] = step;
        // parede: três bolinhas caindo (8x8)
        var wallG = new List<Grid>();
        for (int i = 0; i < 3; i++)
        {
            var g = new Grid(8, 8);
            Disc(g, 3, 2 + i * 2f, 1.6f - i * 0.3f, 'c', 'C'); Disc(g, 5, 3 + i * 1.6f, 1.2f - i * 0.25f, 'c', '.');
            wallG.Add(g);
        }
        FxGrids["fx_dust_wall"] = wallG;
        // ataque furtivo: estrela de 4 pontas (24x24)
        var star = new List<Grid>();
        int[] arm = { 3, 10, 11, 6 }; int[] thick = { 1, 2, 1, 1 };
        for (int i = 0; i < 4; i++)
        {
            var g = new Grid(24, 24);
            for (int t = -arm[i]; t <= arm[i]; t++)
            {
                int w = Math.Abs(t) < arm[i] / 2 ? thick[i] : 0;
                for (int k = -w; k <= w; k++) { g.Px[12 + t, 12 + k] = 'w'; g.Px[12 + k, 12 + t] = 'w'; }
                int dd = (int)(Math.Abs(t) * 0.55f);
                if (i == 1 && Math.Abs(t) <= arm[i] / 2) { g.Px[12 + Math.Sign(t) * dd, 12 + Math.Sign(t) * dd] = 'w'; g.Px[12 + Math.Sign(t) * dd, 12 - Math.Sign(t) * dd] = 'w'; }
            }
            if (i < 3) GridOutline(g, 'T');
            star.Add(g);
        }
        FxGrids["fx_backstab"] = star;
        // centelha (5x5): branca, tingida em jogo pela cor da classe
        var spark = new List<Grid>();
        var s0 = new Grid(5, 5); for (int d = -2; d <= 2; d++) { s0.Px[2 + d, 2] = 'w'; s0.Px[2, 2 + d] = 'w'; } spark.Add(s0);
        var s1 = new Grid(5, 5); for (int d = -1; d <= 1; d++) { s1.Px[2 + d, 2] = 'w'; s1.Px[2, 2 + d] = 'w'; } spark.Add(s1);
        FxGrids["fx_spark"] = spark;
        // anel da troca de classe (32x32)
        var ring = new List<Grid>();
        float[] rr = { 5, 8, 11, 13.5f, 15.5f }; float[] tt = { 2, 2, 2, 1, 1 };
        for (int i = 0; i < 5; i++) { var g = new Grid(32, 32); Ring(g, 16, 16, rr[i], tt[i], 'w'); ring.Add(g); }
        FxGrids["fx_class_switch"] = ring;
        // pixels de estado: faixa da capa (1x2) e centelha do peito (3x3)
        var trim = new Grid(1, 2); trim.Px[0, 0] = 'w'; trim.Px[0, 1] = 'w';
        FxGrids["chr_lion_trim"] = new List<Grid> { trim };
        var chest = new Grid(3, 3); chest.Px[1, 0] = 'w'; chest.Px[0, 1] = 'w'; chest.Px[1, 1] = 'w'; chest.Px[2, 1] = 'w'; chest.Px[1, 2] = 'w';
        FxGrids["chr_lion_chest_spark"] = new List<Grid> { chest };
    }

    // ------------------------------------------------------------- saída

    static string Guid(string key)
    {
        using (var md5 = MD5.Create())
        {
            var b = md5.ComputeHash(Encoding.UTF8.GetBytes("setevidas-lion:" + key));
            var sb = new StringBuilder();
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    static Bitmap ToBitmap(char[,] px, int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                char c = px[x, y];
                bmp.SetPixel(x, y, c == '.' ? Color.Transparent : Pal[c]);
            }
        return bmp;
    }

    static string MetaTemplate;

    static void WritePng(char[,] px, int w, int h, string path, string guid, bool characterPivot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var bmp = ToBitmap(px, w, h)) bmp.Save(path, ImageFormat.Png);
        string meta = MetaTemplate.Replace("__GUID__", guid)
            .Replace("__ALIGN__", characterPivot ? "9" : "0")
            .Replace("__PIVOTY__", characterPivot ? PivotY.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture) : "0.5");
        File.WriteAllText(path + ".meta", meta);
    }

    static string F(float v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }
    static string SpriteRef(string guid) { return "{fileID: 21300000, guid: " + guid + ", type: 3}"; }

    public static string Run(string projectRoot, string metaTemplatePath, string previewPath)
    {
        InitPalette();
        MetaTemplate = File.ReadAllText(metaTemplatePath);
        PosesByClip.Clear();
        BuildClips();
        BuildFx();

        string art = Path.Combine(projectRoot, "Assets/_Game/Art/Characters/Cat/Lion");
        var log = new StringBuilder();
        var guids = new Dictionary<string, string>();

        foreach (var c in Clips)
        {
            for (int i = 0; i < c.Frames.Count; i++)
            {
                var fr = c.Frames[i];
                string bodyRel = "Body/" + c.Name + "/chr_lion_" + c.Name + "_" + i + ".png";
                if (c.Overlay) bodyRel = "Overlay/chr_lion_" + c.Name + "_" + i + ".png";
                string g = Guid(bodyRel);
                guids[bodyRel] = g;
                WritePng(fr.Body.Px, W, H, Path.Combine(art, bodyRel), g, true);
                if (fr.Overlay != null)
                {
                    string ovRel = "Overlay/" + c.Name + "/chr_lion_" + c.Name + "_ovl_" + i + ".png";
                    string og = Guid(ovRel);
                    guids[ovRel] = og;
                    WritePng(fr.Overlay.Px, W, H, Path.Combine(art, ovRel), og, true);
                }
            }
        }
        foreach (var kv in FxGrids)
        {
            for (int i = 0; i < kv.Value.Count; i++)
            {
                var g = kv.Value[i];
                string rel = "FX/" + kv.Key + (kv.Value.Count > 1 ? "_" + i : "") + ".png";
                string gu = Guid(rel);
                guids[rel] = gu;
                WritePng(g.Px, g.W, g.H, Path.Combine(art, rel), gu, false);
            }
        }

        // ---------- asset LionAnimationSet
        var y = new StringBuilder();
        y.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
        y.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n");
        y.Append("  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
        y.Append("  m_Script: {fileID: 11500000, guid: " + ScriptGuid + ", type: 3}\n");
        y.Append("  m_Name: LionAnimationSet\n  m_EditorClassIdentifier: \n");
        y.Append("  clips:\n");
        foreach (var c in Clips)
        {
            y.Append("  - name: " + c.Name + "\n");
            y.Append("    frameTime: " + F(c.FrameTime) + "\n");
            y.Append("    loop: " + (c.Loop ? 1 : 0) + "\n");
            y.Append("    frames:\n");
            for (int i = 0; i < c.Frames.Count; i++)
            {
                var fr = c.Frames[i];
                string bodyRel = c.Overlay ? "Overlay/chr_lion_" + c.Name + "_" + i + ".png" : "Body/" + c.Name + "/chr_lion_" + c.Name + "_" + i + ".png";
                string ovRel = "Overlay/" + c.Name + "/chr_lion_" + c.Name + "_ovl_" + i + ".png";
                y.Append("    - sprite: " + SpriteRef(guids[bodyRel]) + "\n");
                y.Append("      overlay: " + (fr.Overlay != null ? SpriteRef(guids[ovRel]) : "{fileID: 0}") + "\n");
                y.Append("      tail: {x: " + F(fr.Tail.X) + ", y: " + F(fr.Tail.Y) + "}\n");
                y.Append("      chest: {x: " + F(fr.Chest.X) + ", y: " + F(fr.Chest.Y) + "}\n");
                y.Append("      trim: {x: " + F(fr.Trim.X) + ", y: " + F(fr.Trim.Y) + "}\n");
                y.Append("      body: {x: " + fr.BodyOffset.X + ", y: " + (-fr.BodyOffset.Y) + "}\n");
                y.Append("      hasTail: " + (fr.HasTail ? 1 : 0) + "\n");
                y.Append("      hasTrim: " + (fr.HasTrim ? 1 : 0) + "\n");
            }
        }
        Func<string, string> fxList = key =>
        {
            var sb = new StringBuilder();
            var list = FxGrids[key];
            for (int i = 0; i < list.Count; i++)
            {
                string rel = "FX/" + key + (list.Count > 1 ? "_" + i : "") + ".png";
                sb.Append("  - " + SpriteRef(guids[rel]) + "\n");
            }
            return sb.ToString();
        };
        Func<string, string> fxOne = key => SpriteRef(guids["FX/" + key + ".png"]);
        y.Append("  dustJump:\n" + fxList("fx_dust_jump"));
        y.Append("  dustLand:\n" + fxList("fx_dust_land"));
        y.Append("  dustStep:\n" + fxList("fx_dust_step"));
        y.Append("  dustWall:\n" + fxList("fx_dust_wall"));
        y.Append("  backstab:\n" + fxList("fx_backstab"));
        y.Append("  spark:\n" + fxList("fx_spark"));
        y.Append("  classSwitch:\n" + fxList("fx_class_switch"));
        y.Append("  trimPixel: " + fxOne("chr_lion_trim") + "\n");
        y.Append("  chestSpark: " + fxOne("chr_lion_chest_spark") + "\n");

        string assetDir = Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Player");
        File.WriteAllText(Path.Combine(assetDir, "LionAnimationSet.asset"), y.ToString());
        File.WriteAllText(Path.Combine(assetDir, "LionAnimationSet.asset.meta"),
            "fileFormatVersion: 2\nguid: " + AssetGuid + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");

        // ---------- prévia: todos os clipes, um por linha, escala x3
        if (previewPath != null) WritePreview(previewPath);

        int frames = 0; foreach (var c in Clips) frames += c.Frames.Count;
        log.Append("clipes: " + Clips.Count + ", frames: " + frames + ", efeitos: " + FxGrids.Count + "\n");
        return log.ToString();
    }

    public const string ScriptGuid = "5e7a1c0d9b8f4e2a8c3d6f1b2a4e7c90";
    public const string AssetGuid = "b2d4f6a8c0e2446688aaccee11335577";


    // Prévia ampliada de alguns clipes (recorte de 44x44 em volta do gato), com a linha do chão.
    public static void Zoom(string path, string clipCsv, int S)
    {
        var names = clipCsv.Split(',');
        var list = new List<Clip>();
        foreach (var n in names) { var c = Clips.Find(k => k.Name == n.Trim()); if (c != null) list.Add(c); }
        int cols = 1; foreach (var c in list) cols = Math.Max(cols, c.Frames.Count);
        const int x0 = 2, y0 = 2, cw = 44, ch = 44;
        using (var bmp = new Bitmap(cols * (cw * S + 6), list.Count * (ch * S + 18)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 10))
        {
            g.Clear(ColorTranslator.FromHtml("#26324a"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < list.Count; r++)
            {
                int oy = r * (ch * S + 18);
                g.DrawString(list[r].Name, font, Brushes.White, 2, oy);
                for (int i = 0; i < list[r].Frames.Count; i++)
                {
                    var fr = list[r].Frames[i];
                    int ox = i * (cw * S + 6);
                    var dst = new Rectangle(ox, oy + 16, cw * S, ch * S);
                    var src = new Rectangle(x0, y0, cw, ch);
                    using (var b = ToBitmap(fr.Body.Px, W, H)) g.DrawImage(b, dst, src, GraphicsUnit.Pixel);
                    if (fr.Overlay != null) using (var b = ToBitmap(fr.Overlay.Px, W, H)) g.DrawImage(b, dst, src, GraphicsUnit.Pixel);
                    int gy = oy + 16 + (40 - y0) * S;
                    g.DrawLine(Pens.DimGray, ox, gy, ox + cw * S, gy);
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static void WritePreview(string path)
    {
        const int S = 3, cell = 48;
        int cols = 0; foreach (var c in Clips) cols = Math.Max(cols, c.Frames.Count);
        int labelW = 120;
        int wpx = labelW + cols * cell * S, hpx = Clips.Count * cell * S;
        using (var bmp = new Bitmap(wpx, hpx))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 9))
        {
            g.Clear(ColorTranslator.FromHtml("#26324a"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < Clips.Count; r++)
            {
                var c = Clips[r];
                g.DrawString(c.Name, font, Brushes.White, 4, r * cell * S + 4);
                using (var pen = new Pen(ColorTranslator.FromHtml("#3d3a4a")))
                    g.DrawLine(pen, labelW, r * cell * S + 40 * S, wpx, r * cell * S + 40 * S);
                for (int i = 0; i < c.Frames.Count; i++)
                {
                    var fr = c.Frames[i];
                    int ox = labelW + i * cell * S, oy = r * cell * S;
                    using (var b = ToBitmap(fr.Body.Px, W, H)) g.DrawImage(b, ox, oy, W * S, H * S);
                    if (fr.Overlay != null) using (var b = ToBitmap(fr.Overlay.Px, W, H)) g.DrawImage(b, ox, oy, W * S, H * S);
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // ------------------------------------------------- prévia da cauda e da capa

    // Roda a física de Assets/_Game/Scripts/Player/SoftBody.cs sobre frames reais e desenha cada estado em 4 instantes.
    // Compile junto com SoftBody.cs (ver gerar_cauda.ps1).
    public static void SoftPreview(string path, int S)
    {
        InitPalette();
        PosesByClip.Clear();
        BuildClips();
        var rows = new[] {
            new object[] { "idle", "idle", 0, SeteVidas.Player.BodyMood.Idle, 0f },
            new object[] { "run", "run", 1, SeteVidas.Player.BodyMood.Run, 1f },
            new object[] { "jump_up", "jump_up", 1, SeteVidas.Player.BodyMood.Jump, 0.3f },
            new object[] { "fall", "fall", 0, SeteVidas.Player.BodyMood.Fall, 0.3f },
            new object[] { "crouch_walk", "crouch_walk", 0, SeteVidas.Player.BodyMood.Sneak, 0.5f },
            new object[] { "ledge_hang", "ledge_hang", 0, SeteVidas.Player.BodyMood.Hang, 0f },
            new object[] { "hurt", "hurt", 0, SeteVidas.Player.BodyMood.Hurt, 0f },
        };
        char[] map = { '.', 'o', 'L', 'F', 'f', 'G', 'H', 'h', 'T', 't' };
        const int cols = 4, x0 = 2, y0 = 2, cw = 44, ch = 44, B = 32;
        using (var bmp = new Bitmap(cols * (cw * S + 6), rows.Length * (ch * S + 18)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 10))
        {
            g.Clear(ColorTranslator.FromHtml("#26324a"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < rows.Length; r++)
            {
                var clip = Clips.Find(k => k.Name == (string)rows[r][1]);
                var fr = clip.Frames[Math.Min((int)rows[r][2], clip.Frames.Count - 1)];
                var mood = (SeteVidas.Player.BodyMood)rows[r][3];
                float speed = (float)rows[r][4];
                var tail = new SeteVidas.Player.SoftChain(SeteVidas.Player.SoftPoses.TailSegments);
                var cape = new SeteVidas.Player.SoftChain(SeteVidas.Player.SoftPoses.CapeSegments);
                var ta = new float[SeteVidas.Player.SoftPoses.TailSegments.Length];
                var ca = new float[SeteVidas.Player.SoftPoses.CapeSegments.Length];
                float tax = fr.Tail.X, tay = fr.Tail.Y, cax = fr.Trim.X - 1.5f, cay = fr.Trim.Y + 0.5f;
                int oy = r * (ch * S + 18);
                g.DrawString((string)rows[r][0], font, Brushes.White, 2, oy);
                float t = 0f; int sample = 0;
                for (int step = 0; step < 60 * 4 && sample < cols; step++, t += 1f / 60f)
                {
                    SeteVidas.Player.SoftPoses.Tail(mood, 1, t, speed, ta);
                    SeteVidas.Player.SoftPoses.Cape(mood, 1, t, speed, ca);
                    tail.Step(tax, tay, ta, 1f / 60f);
                    cape.Step(cax, cay, ca, 1f / 60f);
                    if (step < 120 || (step - 120) % 12 != 0) continue;
                    var cv = new char[W, H];
                    for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) cv[x, y] = '.';
                    var buf = new byte[B * B];
                    if (fr.HasTrim)
                    {
                        Array.Clear(buf, 0, buf.Length);
                        int cox = (int)Math.Floor(cax) - B / 2, coy = (int)Math.Floor(cay) - B / 2;
                        SeteVidas.Player.SoftRaster.Cape(buf, B, cox, coy, cape.X, cape.Y, SeteVidas.Player.SoftPoses.CapeHalfWidth, 1);
                        SeteVidas.Player.SoftRaster.Outline(buf, B);
                        Blit(cv, buf, B, cox, coy, map);
                    }
                    Array.Clear(buf, 0, buf.Length);
                    int tox = (int)Math.Floor(tax) - B / 2, toy = (int)Math.Floor(tay) - B / 2;
                    SeteVidas.Player.SoftRaster.Tail(buf, B, tox, toy, tail.X, tail.Y, SeteVidas.Player.SoftPoses.TailRadius, mood == SeteVidas.Player.BodyMood.Hurt ? 1.3f : 1f);
                    SeteVidas.Player.SoftRaster.Outline(buf, B);
                    Blit(cv, buf, B, tox, toy, map);
                    for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) if (fr.Body.Px[x, y] != '.') cv[x, y] = fr.Body.Px[x, y];
                    int ox = sample * (cw * S + 6);
                    var dst = new Rectangle(ox, oy + 16, cw * S, ch * S);
                    using (var b = ToBitmap(cv, W, H)) g.DrawImage(b, dst, new Rectangle(x0, y0, cw, ch), GraphicsUnit.Pixel);
                    int gy = oy + 16 + (40 - y0) * S;
                    g.DrawLine(Pens.DimGray, ox, gy, ox + cw * S, gy);
                    sample++;
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static void Blit(char[,] cv, byte[] buf, int B, int ox, int oy, char[] map)
    {
        for (int py = 0; py < B; py++)
            for (int px = 0; px < B; px++)
            {
                byte c = buf[py * B + px];
                if (c == 0) continue;
                int x = 24 + ox + px, y = 39 - (oy + py);
                if (x >= 0 && y >= 0 && x < W && y < H) cv[x, y] = map[c];
            }
    }
}
