// Desenha o mapa da Fortaleza (Assets/_Game/Editor/FortressLayout.cs) num PNG, sem abrir o Unity:
// chão, vinhas, fundo do interior, céu, e cada objeto com um símbolo e uma cor.
// C# 5. Uso: Tools/LevelPreview/gerar_mapa.ps1
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using SeteVidas.EditorTools;

public static class FortressPreview
{
    public static string Render(string path, int S)
    {
        int minX = FortressLayout.MinX, maxX = FortressLayout.MaxX, minY = FortressLayout.MinY, maxY = FortressLayout.MaxY;
        int w = maxX - minX + 1, h = maxY - minY + 1;
        var ground = new bool[w, h]; var climb = new bool[w, h]; var bg = new bool[w, h];
        foreach (var r in FortressLayout.Tiles())
            for (int x = Math.Max(minX, r.X0); x <= Math.Min(maxX, r.X1); x++)
                for (int y = Math.Max(minY, r.Y0); y <= Math.Min(maxY, r.Y1); y++)
                {
                    int i = x - minX, j = y - minY;
                    if (r.Map == FortressLayout.Map.Ground) ground[i, j] = !r.Clear;
                    else if (r.Map == FortressLayout.Map.Climbable) climb[i, j] = true;
                    else bg[i, j] = true;
                }

        using (var bmp = new Bitmap(w * S, h * S + 40))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 8))
        using (var bold = new Font("Consolas", 9, FontStyle.Bold))
        {
            for (int j = 0; j < h; j++)
            {
                // céu do pôr do sol: roxo em cima, laranja embaixo
                float k = j / (float)h;
                var sky = Lerp(ColorTranslator.FromHtml("#f0a050"), ColorTranslator.FromHtml("#2b1d3f"), k);
                for (int i = 0; i < w; i++)
                {
                    Color c = sky;
                    if (bg[i, j]) c = ColorTranslator.FromHtml("#242233");
                    if (ground[i, j]) c = ColorTranslator.FromHtml("#5a556e");
                    if (climb[i, j]) c = ColorTranslator.FromHtml("#4e8a3a");
                    using (var b = new SolidBrush(c)) g.FillRectangle(b, i * S, (h - 1 - j) * S, S, S);
                }
            }
            // grade a cada 10 tiles
            using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255)))
            {
                for (int x = (minX / 10) * 10; x <= maxX; x += 10) g.DrawLine(pen, (x - minX) * S, 0, (x - minX) * S, h * S);
                for (int y = (minY / 10) * 10; y <= maxY; y += 10) g.DrawLine(pen, 0, (maxY - y + 1) * S, w * S, (maxY - y + 1) * S);
            }
            Func<float, int> PX = x => (int)((x - minX) * S);
            Func<float, int> PY = y => (int)((maxY + 1 - y) * S);

            // rampas (triângulos de pedra)
            using (var slopeBrush = new SolidBrush(ColorTranslator.FromHtml("#5a556e")))
                foreach (var r in FortressLayout.Slopes())
                {
                    int highX = r.RisesRight ? r.X0 + r.W : r.X0;
                    g.FillPolygon(slopeBrush, new[]
                    {
                        new Point(PX(r.X0), PY(r.Y0)), new Point(PX(r.X0 + r.W), PY(r.Y0)), new Point(PX(highX), PY(r.Y0 + r.H))
                    });
                }

            // personagem no início
            Mark(g, PX(FortressLayout.SpawnX), PY(FortressLayout.SpawnY), S, Color.White, "▲", bold, "início");

            foreach (var o in FortressLayout.Objects())
            {
                int x = PX(o.X), y = PY(o.Y);
                switch (o.Kind)
                {
                    case "sentinel":
                        using (var pen = new Pen(Color.Red, 2)) g.DrawLine(pen, PX(o.A), y - S, PX(o.B), y - S);
                        Box(g, x - S / 2, y - 2 * S, S, 2 * S, Color.Red);
                        break;
                    case "archer":
                        Box(g, x - S / 2, y - 2 * S, S, 2 * S, Color.Crimson);
                        using (var pen = new Pen(Color.FromArgb(160, 255, 80, 80), 1)) { g.DrawLine(pen, x, y - S, x + o.Facing * 8 * S, y + 5 * S); g.DrawLine(pen, x, y - S, x + o.Facing * 10 * S, y); }
                        Label(g, x - 3 * S, y - 4 * S, "besta", font, Color.Crimson);
                        break;
                    case "coin": Dot(g, x, y, S, Color.Gold); break;
                    case "wall": Box(g, x - S / 2, y - 4 * S, S, 4 * S, Color.Orange); Label(g, x + 2, y - 5 * S, "rachada", font, Color.Orange); break;
                    case "sanctuary": Box(g, x - S, y - 2 * S, 2 * S, 2 * S, Color.Cyan); Label(g, x - 3 * S, y - 4 * S, "S: " + o.Text, bold, Color.Cyan); break;
                    case "lore": Box(g, x - S / 2, y - S, S, S, Color.Violet); break;
                    case "spikes": Box(g, (int)(x - o.W * S / 2), y - S / 2, (int)(o.W * S), S / 2, Color.Magenta); break;
                    case "flame": Box(g, x - S / 2, y - 2 * S, S, 2 * S, Color.OrangeRed); break;
                    case "saw": using (var pen = new Pen(Color.Silver, 2)) g.DrawLine(pen, PX(o.A), y, PX(o.B), y); Dot(g, x, y, S, Color.Silver); break;
                    case "crumble": Box(g, x - S, y - S / 3, 2 * S, S / 2, Color.SandyBrown); break;
                    case "launcher": Box(g, x - S / 2, y - S / 2, S, S, Color.Brown); using (var pen = new Pen(Color.Brown, 1)) g.DrawLine(pen, x, y, x + o.Facing * 6 * S, y); break;
                    case "tripwire": case "plate": Box(g, x - S, y - S / 3, 2 * S, S / 3, Color.Yellow); break;
                    case "spears": Box(g, x - S / 2, y - S, S, S, Color.LightPink); break;
                    case "shadow": using (var b = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillRectangle(b, (int)(x - o.W * S / 2), (int)(y - o.H * S / 2), (int)(o.W * S), (int)(o.H * S)); break;
                    case "bush": Box(g, x - S, y - S, 2 * S, S, Color.DarkGreen); break;
                    case "trigger": using (var pen = new Pen(Color.LightGreen, 1)) g.DrawRectangle(pen, (int)(x - o.W * S / 2), (int)(y - o.H * S / 2), (int)(o.W * S), (int)(o.H * S)); Label(g, x - 2 * S, (int)(y - o.H * S / 2) - 12, o.Id, font, Color.LightGreen); break;
                    case "waterfall": using (var b = new SolidBrush(Color.FromArgb(160, 150, 200, 255))) g.FillRectangle(b, x - S, PY(o.Y + o.H), 2 * S, (int)(o.H * S)); break;
                    case "pool": using (var b = new SolidBrush(Color.FromArgb(150, 80, 140, 220))) g.FillRectangle(b, (int)(x - o.W * S / 2), y - S, (int)(o.W * S), S); break;
                    case "dummy": Box(g, x - S / 3, y - 2 * S, S, 2 * S, Color.Tan); break;
                    case "sign": Label(g, x - 10, y - 6, "»", font, Color.White); break;
                    default: break;
                }
            }
            // a câmara do selo (o chefe e o tomo)
            int ay = PY(FortressLayout.ArenaFloor);
            Box(g, PX(FortressLayout.ArenaEntryGateX) - S / 2, ay - 4 * S, S, 4 * S, Color.SlateGray);
            Box(g, PX(FortressLayout.ArenaExitGateX) - S / 2, ay - 4 * S, S, 4 * S, Color.SlateGray);
            Box(g, PX(FortressLayout.BossX) - S, ay - 3 * S, 2 * S, 3 * S, Color.Goldenrod); Label(g, PX(FortressLayout.BossX) - 2 * S, ay - 5 * S, "KODA", bold, Color.Goldenrod);
            Box(g, PX(FortressLayout.TomeX) - S / 2, ay - 2 * S, S, S, Color.MediumPurple); Label(g, PX(FortressLayout.TomeX) - 2 * S, ay - 4 * S, "tomo", bold, Color.MediumPurple);
            Box(g, PX(FortressLayout.SealSanctuaryX) - S, ay - 2 * S, 2 * S, 2 * S, Color.Cyan);

            g.DrawString("Fortaleza — 1 quadrado = 1 tile; grade a cada 10. Vermelho: sentinela e patrulha · dourado: moeda · laranja: parede rachada · ciano: santuário · violeta: placa de lore",
                font, Brushes.White, 4, h * S + 4);
            g.DrawString("magenta: espinhos · vermelho-laranja: fogo · prata: serra · marrom: seteira · amarelo: fio/placa de pressão · verde: vinha · contorno verde: conversa",
                font, Brushes.White, 4, h * S + 20);
            bmp.Save(path, ImageFormat.Png);
        }
        return "mapa: " + w + " × " + h + " tiles";
    }

    static Color Lerp(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
    static void Box(Graphics g, int x, int y, int w, int h, Color c) { using (var b = new SolidBrush(c)) g.FillRectangle(b, x, y, Math.Max(1, w), Math.Max(1, h)); }
    static void Dot(Graphics g, int x, int y, int S, Color c) { using (var b = new SolidBrush(c)) g.FillEllipse(b, x - S / 2, y - S / 2, S, S); }
    static void Label(Graphics g, int x, int y, string text, Font f, Color c) { using (var b = new SolidBrush(c)) g.DrawString(text, f, b, x, y); }
    static void Mark(Graphics g, int x, int y, int S, Color c, string glyph, Font f, string text) { Label(g, x - 4, y - 2 * S, glyph + " " + text, f, c); }
}
