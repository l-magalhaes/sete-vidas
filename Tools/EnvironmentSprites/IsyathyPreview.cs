// Prévia da Isyathy do Ninho (o desenho é feito em código: Assets/_Game/Scripts/World/Forest/IsyathyArt.cs e
// SerpentRaster.cs). Gera um PNG com a cabeça (dormindo, acordada, mordendo), a cratera e o corpo numa pose de bote.
// Uso: powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/previa_isyathy.ps1 (C# 5, sem Unity)
using System;
using System.Drawing;
using System.Drawing.Imaging;
using SeteVidas.World;

public static class IsyathyPreview
{
    static Color Col(uint c) { return Color.FromArgb((int)(c >> 24), (int)((c >> 16) & 255), (int)((c >> 8) & 255), (int)(c & 255)); }

    static void Blit(Bitmap bmp, uint[] px, int w, int h, int ox, int oy, bool bottomUp, int S)
    {
        using (var g = Graphics.FromImage(bmp))
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    uint c = px[y * w + x];
                    if (c == 0u) continue;
                    int row = bottomUp ? h - 1 - y : y;
                    using (var b = new SolidBrush(Col(c))) g.FillRectangle(b, (ox + x) * S, (oy + row) * S, S, S);
                }
    }

    /// <summary>O corpo num trilho em S com a cabeça na ponta (como o SerpentRig faz no jogo).</summary>
    static SerpentRaster Pose(float[] path, float headS, float amp, float phase, int frame, bool clip, float holeX, float holeY)
    {
        var r = new SerpentRaster(360, 200);
        r.Begin(0f, 0f);
        r.SetClip(clip, (holeX - 1.7f) * 16f, (holeX + 1.7f) * 16f, holeY * 16f);
        int n = (int)(22f / (3f / 16f));
        var discs = new SerpentDisc[n];
        var pts = new float[n * 2];
        for (int k = 0; k < n; k++)
        {
            float d = k * 3f / 16f;
            float px, py, dx, dy;
            At(path, headS - d, out px, out py, out dx, out dy);
            float pin = Math.Min(1f, d / 2.5f), w = amp * pin * (float)Math.Sin(2 * Math.PI * d / 5f - phase);
            pts[k * 2] = px - dy * w; pts[k * 2 + 1] = py + dx * w;
        }
        int m = 0;
        for (int k = n - 1; k >= 0; k--)
        {
            int a = Math.Max(0, k - 1), b = Math.Min(n - 1, k + 1);
            float dx = pts[a * 2] - pts[b * 2], dy = pts[a * 2 + 1] - pts[b * 2 + 1], l = (float)Math.Sqrt(dx * dx + dy * dy);
            float d = k * 3f / 16f;
            float rad = d < 6f ? 12f + 3f * d / 6f : Math.Max(2.5f, 15f - 12f * (float)Math.Pow(Math.Min(1f, (d - 6f) / 16f), 1.4));
            discs[m++] = new SerpentDisc { X = pts[k * 2] * 16f, Y = pts[k * 2 + 1] * 16f, DirX = dx / l, DirY = dy / l, R = rad, U = d * 16f };
        }
        r.Body(discs, m);
        float hx, hy, hdx, hdy;
        At(path, headS, out hx, out hy, out hdx, out hdy);
        float ang = (float)Math.Atan2(hdy, hdx);
        bool flip = hdx < 0f;
        if (flip) ang = (float)Math.Atan2(hdy, hdx);
        r.Head(IsyathyArt.Head(frame, true), IsyathyArt.HeadW, IsyathyArt.HeadH, IsyathyArt.NeckX, IsyathyArt.NeckY, hx * 16f, hy * 16f, ang, flip);
        r.Finish();
        return r;
    }

    static void At(float[] p, float s, out float x, out float y, out float dx, out float dy)
    {
        int n = p.Length / 2;
        if (s <= 0f)
        {
            dx = p[2] - p[0]; dy = p[3] - p[1]; float l = (float)Math.Sqrt(dx * dx + dy * dy); dx /= l; dy /= l;
            x = p[0] + dx * s; y = p[1] + dy * s; return;
        }
        for (int i = 1; i < n; i++)
        {
            float ax = p[i * 2 - 2], ay = p[i * 2 - 1], bx = p[i * 2], by = p[i * 2 + 1];
            float seg = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            if (s <= seg || i == n - 1) { dx = (bx - ax) / seg; dy = (by - ay) / seg; x = ax + dx * s; y = ay + dy * s; return; }
            s -= seg;
        }
        x = p[0]; y = p[1]; dx = 1; dy = 0;
    }

    public static void Run(string outPath)
    {
        const int S = 3;
        var bmp = new Bitmap(760 * S, 520 * S);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.FromArgb(255, 22, 18, 30));
        // as cabeças
        for (int f = 0; f < 3; f++) Blit(bmp, IsyathyArt.Head(f, true), IsyathyArt.HeadW, IsyathyArt.HeadH, 10 + f * 80, 8, false, S);
        // a cratera (o buraco e a borda da frente)
        Blit(bmp, IsyathyArt.CraterBack(), IsyathyArt.CraterW, IsyathyArt.CraterBackH, 260, 20, false, S);
        Blit(bmp, IsyathyArt.CraterLip(), IsyathyArt.CraterW, IsyathyArt.CraterLipH, 260, 20 + IsyathyArt.CraterBackSurface - IsyathyArt.CraterLipSurface, false, S);
        // deitada (dormindo), em S
        var rest = new float[] { 20, 2, 16, 3, 12, 2.2f, 8, 3, 4, 2.4f };
        var r1 = Pose(rest, 18f, 0.15f, 0f, IsyathyArt.FrameSleep, false, 0, 0);
        Blit(bmp, r1.Px, r1.W, r1.H, 0, 70, true, S);
        // o bote: sai do buraco (x 8, chão 2), armada, e avança para a direita
        var strike = new float[] { 8, -8, 8, 2, 7.6f, 3.9f, 7.1f, 5.2f, 13.5f, 3.4f };
        var r2 = Pose(strike, 10f + 1.95f + 1.45f + 6.6f, 0.12f, 1f, IsyathyArt.FrameBite, true, 8, 2);
        Blit(bmp, r2.Px, r2.W, r2.H, 370, 70, true, S);
        // armada (antes do bote)
        var r3 = Pose(strike, 10f + 1.95f + 1.45f, 0.12f, 2f, IsyathyArt.FrameAwake, true, 8, 2);
        Blit(bmp, r3.Px, r3.W, r3.H, 0, 290, true, S);
        bmp.Save(outPath, ImageFormat.Png);
    }
}
