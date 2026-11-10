// Itens em pixel art: a moeda comum (girando), a moeda do Contratante e o ícone de moeda do HUD.
// Mesmo contorno e mesmo ouro da Ordem do resto do jogo. C# 5; compilado com PixelKit.cs por gerar.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;

public static class ItemSpriteGen
{
    static void InitPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1c26");                                              // contorno
        PixelKit.P('Y', "#e0b04a"); PixelKit.P('y', "#a87a22"); PixelKit.P('L', "#fff4c2"); PixelKit.P('k', "#7a5418"); // ouro
        PixelKit.P('M', "#5b2a6e"); PixelKit.P('m', "#3d1b4a"); PixelKit.P('G', "#8fd14f"); // selo roxo do Contratante
    }

    /// <summary>Moeda vista de frente (w = meia largura visível: girando, ela afina até virar o fio).</summary>
    static PixelCanvas Coin(int size, float halfWidth, bool seal, int glint)
    {
        var cv = new PixelCanvas(size + 2, size + 2);
        float c = (size + 2) / 2f, r = size / 2f - 0.1f;
        for (int x = 0; x < cv.W; x++)
            for (int y = 0; y < cv.H; y++)
            {
                float dx = (x + 0.5f - c) / Math.Max(0.6f, halfWidth), dy = (y + 0.5f - c) / r;
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;
                char ch = 'Y';
                if (halfWidth < 1.2f) ch = (y + 0.5f - c) < 0 ? 'Y' : 'y';          // de lado: só o fio
                else if (d > 0.62f) ch = (x + 0.5f - c) > 0 ? 'y' : 'Y';            // borda em relevo
                else if (seal && d < 0.2f) ch = 'M';                                // miolo com o selo
                else ch = (x + 0.5f - c) > 0.5f ? 'y' : 'Y';
                cv.Px[x, y] = ch;
            }
        if (seal && halfWidth >= 2.5f)
        {
            // selo desconhecido: um olho com fenda, no roxo da praga
            int cx = (int)c, cy = (int)c;
            cv.Force(cx - 1, cy, 'm'); cv.Force(cx, cy, 'G'); cv.Force(cx + 1, cy, 'm'); cv.Force(cx, cy - 1, 'M'); cv.Force(cx, cy + 1, 'M');
        }
        else if (!seal && halfWidth >= 2.5f)
        {
            // cunho comum: uma pata pequena em baixo-relevo
            int cx = (int)c, cy = (int)c;
            cv.Force(cx - 1, cy, 'k'); cv.Force(cx, cy, 'k'); cv.Force(cx - 1, cy - 1, 'k');
        }
        if (glint > 0 && halfWidth >= 1.2f)
        {
            int gx = (int)(c - halfWidth * 0.45f), gy = (int)(c - r * 0.5f);
            cv.Force(gx, gy, 'L');
            if (glint > 1) { cv.Force(gx + 1, gy, 'L'); cv.Force(gx, gy - 1 < 0 ? 0 : gy - 1, 'L'); }
        }
        PixelKit.Outline(cv, 'o', "L");
        return cv;
    }

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string dir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Items/Coins");
        var sheet = new List<PixelCanvas>(); var names = new List<string>();
        // moeda comum 7×7: frente, afinando, de lado, afinando (o giro); brilho na frente
        float[] w = { 3.5f, 2.2f, 0.8f, 2.2f };
        var log = new System.Text.StringBuilder();
        for (int i = 0; i < 4; i++)
        {
            var cv = Coin(7, w[i], false, i == 0 ? 2 : 1);
            string gu = PixelKit.WriteSprite(cv, System.IO.Path.Combine(dir, "item_coin_" + i + ".png"), "items/item_coin_" + i, template, false);
            log.Append("item_coin_" + i + " " + gu + "\n"); sheet.Add(cv); names.Add("coin_" + i);
        }
        // moeda do Contratante 10×10, com o selo
        float[] wb = { 5f, 3.2f, 1f, 3.2f };
        for (int i = 0; i < 4; i++)
        {
            var cv = Coin(10, wb[i], true, i == 0 ? 2 : 1);
            string gu = PixelKit.WriteSprite(cv, System.IO.Path.Combine(dir, "item_coin_contractor_" + i + ".png"), "items/item_coin_contractor_" + i, template, false);
            log.Append("item_coin_contractor_" + i + " " + gu + "\n"); sheet.Add(cv); names.Add("contratante_" + i);
        }
        // ícone do HUD (9×9 com contorno)
        var icon = Coin(7, 3.5f, false, 2);
        string ig = PixelKit.WriteSprite(icon, System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/UI/Hearts/ui_coin.png"), "ui/ui_coin", template, false);
        log.Append("ui_coin " + ig + "\n");
        if (!string.IsNullOrEmpty(previewDir)) Preview(sheet, names, System.IO.Path.Combine(previewDir, "moedas.png"), 10);
        return log.ToString();
    }

    static void Preview(List<PixelCanvas> items, List<string> names, string path, int S)
    {
        int cell = 14 * S;
        using (var bmp = new Bitmap(4 * cell, 2 * (cell + 18)))
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 9))
        {
            g.Clear(ColorTranslator.FromHtml("#2a2238"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < items.Count; i++)
            {
                int x = (i % 4) * cell, y = (i / 4) * (cell + 18);
                g.DrawString(names[i], font, Brushes.White, x + 2, y);
                using (var b = PixelKit.ToBitmap(items[i]))
                    g.DrawImage(b, new Rectangle(x + 10, y + 18, items[i].W * S, items[i].H * S), new Rectangle(0, 0, items[i].W, items[i].H), GraphicsUnit.Pixel);
            }
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
