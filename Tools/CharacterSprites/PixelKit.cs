// Kit de pixel art por código, compartilhado pelos geradores de Koda e do Contratante.
// Mesmo método do Lion (Tools/LionSprites) e dos templários (Tools/EnemySprites): partes desenhadas em mapas de
// caracteres + partes calculadas (linhas, retângulos girados, elipses), contorno automático de 1 px.
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class PixelCanvas
{
    public readonly int W, H;
    public char[,] Px;
    public PixelCanvas(int w, int h) { W = w; H = h; Px = new char[w, h]; Clear(); }
    public void Clear() { for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) Px[x, y] = '.'; }
    public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
    public void Set(int x, int y, char c) { if (c != '.' && c != ' ' && In(x, y)) Px[x, y] = c; }
    public void Force(int x, int y, char c) { if (In(x, y)) Px[x, y] = c; }
    public char Get(int x, int y) { return In(x, y) ? Px[x, y] : '.'; }
    public void Stamp(string[] map, int x0, int y0)
    {
        for (int r = 0; r < map.Length; r++) for (int c = 0; c < map[r].Length; c++) Set(x0 + c, y0 + r, map[r][c]);
    }
    public PixelCanvas Clone() { var k = new PixelCanvas(W, H); Array.Copy(Px, k.Px, Px.Length); return k; }
    public void Paste(PixelCanvas src) { for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) if (src.Px[x, y] != '.') Px[x, y] = src.Px[x, y]; }
    public PixelCanvas Mirror() { var k = new PixelCanvas(W, H); for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) k.Px[W - 1 - x, y] = Px[x, y]; return k; }
}

public static class PixelKit
{
    public static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>();
    public static void P(char c, string hex) { Pal[c] = ColorTranslator.FromHtml(hex); }

    // Contorno externo de 1 px (vizinhança de 4). "except": cores que não recebem contorno em volta (brilhos, névoa).
    public static void Outline(PixelCanvas cv, char color) { Outline(cv, color, ""); }
    public static void Outline(PixelCanvas cv, char color, string except)
    {
        var add = new List<Point>();
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                if (cv.Px[x, y] != '.') continue;
                if (Solid(cv, x - 1, y, except) || Solid(cv, x + 1, y, except) || Solid(cv, x, y - 1, except) || Solid(cv, x, y + 1, except))
                    add.Add(new Point(x, y));
            }
        foreach (var p in add) cv.Px[p.X, p.Y] = color;
    }
    static bool Solid(PixelCanvas cv, int x, int y, string except)
    {
        char c = cv.Get(x, y);
        return c != '.' && except.IndexOf(c) < 0;
    }

    public static List<Point> Line(int x0, int y0, int x1, int y1)
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

    /// <summary>Linha grossa: um quadrado de lado "w" em cada ponto. color(t) recebe 0..1 ao longo da linha.</summary>
    public static void Thick(PixelCanvas cv, int x0, int y0, int x1, int y1, int w, Func<float, int, char> color)
    {
        var pts = Line(x0, y0, x1, y1);
        for (int i = 0; i < pts.Count; i++)
        {
            float t = pts.Count > 1 ? i / (float)(pts.Count - 1) : 0f;
            for (int k = 0; k < w; k++)
                for (int j = 0; j < w; j++)
                    cv.Set(pts[i].X + k - w / 2, pts[i].Y + j - w / 2, color(t, j));
        }
    }

    // Retângulo girado: u ao longo do eixo, v perpendicular. Ângulo em graus, 0 = direita, 90 = cima.
    public static void FillRotated(PixelCanvas cv, float ox, float oy, float angleDeg, float u0, float u1, float halfV, Func<float, float, char> colorAt)
    {
        double a = angleDeg * Math.PI / 180.0;
        float dx = (float)Math.Cos(a), dy = (float)-Math.Sin(a);
        float px = -dy, py = dx;
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                float rx = x + 0.5f - ox, ry = y + 0.5f - oy;
                float u = rx * dx + ry * dy, v = rx * px + ry * py;
                if (u < u0 || u > u1 || Math.Abs(v) > halfV) continue;
                char c = colorAt(u, v);
                if (c != '.') cv.Set(x, y, c);
            }
    }

    public static void Ellipse(PixelCanvas cv, float cx, float cy, float rx, float ry, Func<float, float, char> colorAt)
    {
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) cv.Set(x, y, colorAt(dx, dy));
            }
    }
    public static void Ellipse(PixelCanvas cv, float cx, float cy, float rx, float ry, char c)
    {
        Ellipse(cv, cx, cy, rx, ry, (a, b) => c);
    }

    /// <summary>Gira o canvas em volta de (cx, cy) (graus, positivo = anti-horário na tela) e desce "lift" px.</summary>
    public static PixelCanvas Rotate(PixelCanvas src, float degrees, float cx, float cy, float shiftX, float shiftY)
    {
        var cv = new PixelCanvas(src.W, src.H);
        double a = -degrees * Math.PI / 180.0;
        float cs = (float)Math.Cos(a), sn = (float)Math.Sin(a);
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                float dx = x + 0.5f - cx - shiftX, dy = y + 0.5f - cy - shiftY;
                float sx = cs * dx + sn * dy + cx, sy = -sn * dx + cs * dy + cy;
                char c = src.Get((int)Math.Floor(sx), (int)Math.Floor(sy));
                if (c != '.') cv.Px[x, y] = c;
            }
        return cv;
    }

    /// <summary>Ruído determinístico 0..1 por pixel (dissolver, névoa).</summary>
    public static float Noise(int x, int y, int seed)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xffff) / 65535f;
    }

    // ------------------------------------------------------------- saída

    public static string Guid(string key)
    {
        using (var md5 = MD5.Create())
        {
            var b = md5.ComputeHash(Encoding.UTF8.GetBytes("setevidas:" + key));
            var sb = new StringBuilder();
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    public static Bitmap ToBitmap(PixelCanvas cv)
    {
        var bmp = new Bitmap(cv.W, cv.H, PixelFormat.Format32bppArgb);
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
                bmp.SetPixel(x, y, cv.Px[x, y] == '.' ? Color.Transparent : Pal[cv.Px[x, y]]);
        return bmp;
    }

    /// <summary>Grava PNG + .meta. Se já existe .meta, mantém o GUID dele. Retorna o GUID.</summary>
    public static string WriteSprite(PixelCanvas cv, string path, string guidKey, string metaTemplate, bool bottomPivot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var bmp = ToBitmap(cv)) bmp.Save(path, ImageFormat.Png);
        string meta = path + ".meta";
        if (File.Exists(meta))
            foreach (var line in File.ReadAllLines(meta))
                if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
        string gu = Guid(guidKey);
        File.WriteAllText(meta, metaTemplate.Replace("__GUID__", gu)
            .Replace("__ALIGN__", bottomPivot ? "9" : "0").Replace("__PIVOTY__", bottomPivot ? "0" : "0.5"));
        return gu;
    }

    public class Clip
    {
        public string Name; public float FrameTime; public bool Loop;
        public List<PixelCanvas> Frames = new List<PixelCanvas>();
    }

    const string AnimationSetScript = "5e7a1c0d9b8f4e2a8c3d6f1b2a4e7c90"; // CharacterAnimationSet.cs

    /// <summary>Grava os frames de cada clipe e o asset CharacterAnimationSet. Retorna "clipes: n, frames: m".</summary>
    public static string WriteAnimationSet(List<Clip> clips, string artDir, string filePrefix, string assetPath, string assetName,
                                           string assetGuid, string metaTemplate)
    {
        var y = new StringBuilder();
        y.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
        y.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n");
        y.Append("  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
        y.Append("  m_Script: {fileID: 11500000, guid: " + AnimationSetScript + ", type: 3}\n  m_Name: " + assetName + "\n  m_EditorClassIdentifier: \n  clips:\n");
        int frames = 0;
        foreach (var c in clips)
        {
            y.Append("  - name: " + c.Name + "\n    frameTime: " + c.FrameTime.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n    loop: " + (c.Loop ? 1 : 0) + "\n    frames:\n");
            for (int i = 0; i < c.Frames.Count; i++)
            {
                string rel = c.Name + "/" + filePrefix + "_" + c.Name + "_" + i + ".png";
                string gu = WriteSprite(c.Frames[i], Path.Combine(artDir, rel), filePrefix + "/" + rel, metaTemplate, true);
                y.Append("    - sprite: {fileID: 21300000, guid: " + gu + ", type: 3}\n      overlay: {fileID: 0}\n");
                y.Append("      tail: {x: 0, y: 0}\n      chest: {x: 0, y: 0}\n      trim: {x: 0, y: 0}\n      body: {x: 0, y: 0}\n      hasTail: 0\n      hasTrim: 0\n");
                frames++;
            }
        }
        File.WriteAllText(assetPath, y.ToString());
        if (!File.Exists(assetPath + ".meta"))
            File.WriteAllText(assetPath + ".meta", "fileFormatVersion: 2\nguid: " + assetGuid + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
        return "clipes: " + clips.Count + ", frames: " + frames;
    }

    /// <summary>Prévia ampliada: uma linha por clipe.</summary>
    public static void PreviewClips(List<Clip> clips, string path, int S, Rectangle crop, string bg)
    {
        int cols = 1; foreach (var c in clips) cols = Math.Max(cols, c.Frames.Count);
        using (var bmp = new Bitmap(cols * (crop.Width * S + 6), clips.Count * (crop.Height * S + 18)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 10))
        {
            g.Clear(ColorTranslator.FromHtml(bg));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int r = 0; r < clips.Count; r++)
            {
                int oy = r * (crop.Height * S + 18);
                g.DrawString(clips[r].Name, font, Brushes.White, 2, oy);
                for (int i = 0; i < clips[r].Frames.Count; i++)
                {
                    int ox = i * (crop.Width * S + 6);
                    using (var b = ToBitmap(clips[r].Frames[i]))
                        g.DrawImage(b, new Rectangle(ox, oy + 16, crop.Width * S, crop.Height * S), crop, GraphicsUnit.Pixel);
                    int gy = oy + 16 + (crop.Bottom - crop.Top) * S;
                    g.DrawLine(Pens.DimGray, ox, gy, ox + crop.Width * S, gy);
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    /// <summary>Prévia dos retratos (48×48) numa grade com moldura dourada.</summary>
    public static void PreviewPortraits(List<PixelCanvas> portraits, List<string> names, string path, int S)
    {
        int cols = Math.Min(4, portraits.Count), rows = (portraits.Count + cols - 1) / cols;
        int cw = 48 * S + 24, ch = 48 * S + 40;
        using (var bmp = new Bitmap(cols * cw, rows * ch))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 12))
        {
            g.Clear(ColorTranslator.FromHtml("#14101e"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < portraits.Count; i++)
            {
                int x = (i % cols) * cw + 12, yy = (i / cols) * ch + 24;
                using (var b = new SolidBrush(ColorTranslator.FromHtml("#2a2238"))) g.FillRectangle(b, x, yy, 48 * S, 48 * S);
                using (var pen = new Pen(ColorTranslator.FromHtml("#c9962e"), 6)) g.DrawRectangle(pen, x - 3, yy - 3, 48 * S + 6, 48 * S + 6);
                using (var b = ToBitmap(portraits[i])) g.DrawImage(b, new Rectangle(x, yy, 48 * S, 48 * S), new Rectangle(0, 0, 48, 48), GraphicsUnit.Pixel);
                g.DrawString(names[i], font, Brushes.White, x, yy - 22);
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
