// Objetos de cenário gerados por código (biblioteca de cenário). Usa o PixelKit (Tools/CharacterSprites).
// Cada asset grava os PNGs (+ .meta com pivot central) e uma prévia ampliada em SeteVidasDocs/Referencias.
// C# 5 (compilado pelo PowerShell): sem interpolação de string, sem "?.", sem membros "=>".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class PropSpriteGen
{
    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        string template = File.ReadAllText(metaTemplatePath);
        string common = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Common");
        var torch = WallTorch();
        for (int i = 0; i < torch.Count; i++)
            PixelKit.WriteSprite(torch[i], Path.Combine(common, "env_com_obj_wall_torch_" + i + ".png"),
                "env_com_obj_wall_torch_" + i, template, false);
        Preview(torch, Path.Combine(previewDir, "tocha_parede.png"), 8, "#242233");
    }

    // ------------------------------------------------------------ tocha de parede

    /// <summary>
    /// Tocha de parede 12×20, 4 quadros de chama (10 fps). Copo de ferro com o topo iluminado pela chama,
    /// cabo de madeira com o lado da luz mais claro, anel e placa presos na parede. As peças de metal e madeira
    /// são claras o bastante para aparecer contra a parede escura (#242233); a chama tem um halo leve.
    /// Sem o vermelho das armadilhas: a ponta da chama é laranja #e0702a.
    /// </summary>
    static List<PixelCanvas> WallTorch()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('W', "#fff2b0"); // miolo da chama
        PixelKit.P('Y', "#ffc86a"); // chama clara
        PixelKit.P('O', "#ffa83a"); // chama
        PixelKit.P('R', "#e0702a"); // ponta (laranja, longe de #c83a3a)
        PixelKit.Pal['h'] = Color.FromArgb(80, 0xff, 0xa8, 0x3a); // halo
        PixelKit.P('K', "#24222e"); // ferro na sombra / contorno
        PixelKit.P('k', "#5a5868"); // ferro
        PixelKit.P('i', "#8e8ca4"); // ferro na luz
        PixelKit.P('g', "#c8874a"); // ferro iluminado pela chama
        PixelKit.P('D', "#3d2a22"); // pano escuro
        PixelKit.P('d', "#6e4a32"); // pano
        PixelKit.P('L', "#b08a5a"); // madeira na luz
        PixelKit.P('w', "#6b4a2b"); // madeira

        string[][] flames =
        {
            new[] { // 0: reta
                "......R.....",
                ".....RO.....",
                ".....OOR....",
                "....ROYO....",
                "....OYYOR...",
                "...ROYWYO...",
                "...OYWWYO...",
                "...OYWWYO...",
                "....OYYO....",
            },
            new[] { // 1: inclina para a esquerda
                ".....R......",
                "....RO......",
                "....OOR.....",
                "...ROYO.....",
                "...OYYOR....",
                "...OYWYOR...",
                "...OYWWYO...",
                "...OYWWYO...",
                "....OYYO....",
            },
            new[] { // 2: alta
                "......R.....",
                "......O.....",
                ".....RO.....",
                ".....OYR....",
                "....ROYO....",
                "....OYYOR...",
                "...OYWWYO...",
                "...OYWWYO...",
                "....OYYO....",
            },
            new[] { // 3: inclina para a direita
                "......R.....",
                "......OR....",
                ".....ROO....",
                ".....OYOR...",
                "....ROYYO...",
                "...ROYWYO...",
                "...OYWWYO...",
                "...OYWWYO...",
                "....OYYO....",
            },
        };

        string[] body =
        {
            "...KgggggK..", //  9 borda do copo, iluminada pela chama
            "...DdDdDdD..", // 10 pano enrolado no copo
            "...KkiiikK..", // 11 copo de ferro
            "....KkikK...", // 12
            ".....Lw.....", // 13 cabo
            ".....Lw.....", // 14
            "....kiiik...", // 15 anel que prende o cabo
            "....KkkkK...", // 16
            ".....Lw.....", // 17
            "..KkiiiikK..", // 18 placa na parede
            "..KKKKKKKK..", // 19
        };

        var frames = new List<PixelCanvas>();
        foreach (var f in flames)
        {
            var cv = new PixelCanvas(12, 20);
            cv.Stamp(f, 0, 0);
            PixelKit.Outline(cv, 'h');     // halo só em volta da chama (o corpo vem depois)
            cv.Stamp(body, 0, 9);
            frames.Add(cv);
        }
        return frames;
    }

    // ------------------------------------------------------------------ prévia

    /// <summary>Quadros lado a lado, ampliados S vezes, sobre a cor de parede informada.</summary>
    static void Preview(List<PixelCanvas> frames, string path, int S, string bg)
    {
        int w = frames[0].W, h = frames[0].H, gap = 4;
        using (var bmp = new Bitmap((w + gap) * S * frames.Count + gap * S, (h + 2 * gap) * S))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(ColorTranslator.FromHtml(bg));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < frames.Count; i++)
                using (var one = PixelKit.ToBitmap(frames[i]))
                    g.DrawImage(one, new Rectangle((gap + i * (w + gap)) * S, gap * S, w * S, h * S));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
