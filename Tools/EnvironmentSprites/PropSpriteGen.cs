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

        // Etapa 3 (correções): o que explica as sombras e sustenta as estruturas
        var off = WallTorchOut();
        for (int i = 0; i < off.Count; i++)
            PixelKit.WriteSprite(off[i], Path.Combine(common, "env_com_obj_wall_torch_apagada_" + i + ".png"), "env_com_obj_wall_torch_apagada_" + i, template, false);
        string sol = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Soluki");
        string cid = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Citadel");
        PixelKit.WriteSprite(Tent(), Path.Combine(sol, "env_sol_obj_tenda.png"), "env_sol_obj_tenda", template, false);
        PixelKit.WriteSprite(Awning(), Path.Combine(sol, "env_sol_obj_toldo.png"), "env_sol_obj_toldo", template, false);
        PixelKit.WriteSprite(Post(16), Path.Combine(sol, "env_sol_obj_poste_curto.png"), "env_sol_obj_poste_curto", template, false);
        PixelKit.WriteSprite(Post(52), Path.Combine(sol, "env_sol_obj_poste_toldo.png"), "env_sol_obj_poste_toldo", template, false);
        PixelKit.WriteSprite(GiantStatue(), Path.Combine(cid, "env_cid_cul_estatua_gigante.png"), "env_cid_cul_estatua_gigante", template, false);
    }

    // ------------------------------------------------------ tocha apagada

    /// <summary>
    /// Tocha apagada 12×20 (a ronda passou e apagou): o mesmo corpo da tocha acesa, a ponta queimada e um fio de fumaça
    /// em 2 quadros. É o que explica as sombras da galeria (plano, 4.4).
    /// </summary>
    static List<PixelCanvas> WallTorchOut()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('K', "#24222e"); PixelKit.P('k', "#5a5868"); PixelKit.P('i', "#8e8ca4");
        PixelKit.P('D', "#3d2a22"); PixelKit.P('d', "#6e4a32"); PixelKit.P('L', "#b08a5a"); PixelKit.P('w', "#6b4a2b");
        PixelKit.P('c', "#1c1a24"); PixelKit.P('e', "#7a3a22"); // pavio queimado, uma brasa quase apagada
        PixelKit.Pal['s'] = Color.FromArgb(140, 0x9a, 0x96, 0xa8); PixelKit.Pal['S'] = Color.FromArgb(90, 0x9a, 0x96, 0xa8); // fumaça
        string[] body =
        {
            "....cccc....", //  8 pano queimado
            "...KdcecdK..", //  9
            "...DdDdDdD..", // 10
            "...KkiiikK..", // 11
            "....KkikK...", // 12
            ".....Lw.....", // 13
            ".....Lw.....", // 14
            "....kiiik...", // 15
            "....KkkkK...", // 16
            ".....Lw.....", // 17
            "..KkiiiikK..", // 18
            "..KKKKKKKK..", // 19
        };
        string[][] smoke =
        {
            new[] { "......S.....", ".....s......", ".....S......", "......s.....", ".......S....", "......s.....", ".....s......", "......s....." },
            new[] { ".....S......", "......s.....", "......S.....", ".....s......", ".....S......", "......s.....", ".......s....", "......s....." },
        };
        var frames = new List<PixelCanvas>();
        foreach (var s in smoke)
        {
            var cv = new PixelCanvas(12, 20);
            cv.Stamp(s, 0, 0);
            cv.Stamp(body, 0, 8);
            frames.Add(cv);
        }
        return frames;
    }

    // --------------------------------------------------- tenda, toldo, postes

    static void CanvasPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('R', "#8f2a2a"); PixelKit.P('r', "#5a161c"); PixelKit.P('C', "#e9dcb8"); PixelKit.P('c', "#b8a888");
        PixelKit.P('W', "#9a7044"); PixelKit.P('w', "#6b4a2b"); PixelKit.P('k', "#1c1a24"); PixelKit.P('G', "#d9a441");
    }

    /// <summary>Lona listrada (carmesim da Ordem e cru) com franja recortada; largura em px, altura 16.</summary>
    static void Canopy(PixelCanvas cv, int w, int y0)
    {
        for (int x = 0; x < w; x++)
        {
            bool red = (x / 8) % 2 == 0;
            int fringe = 12 + ((x % 8) < 4 ? 2 : 0);                    // franja em dentes
            for (int y = 0; y <= fringe; y++)
            {
                char c = y == 0 ? 'k' : y == 1 ? (red ? 'R' : 'C') : red ? (y > 9 ? 'r' : 'R') : (y > 9 ? 'c' : 'C');
                if (x == 0 || x == w - 1) c = 'k';
                cv.Force(x, y0 + y, c);
            }
            cv.Force(x, y0 + fringe + 1, 'k');
        }
        for (int x = w / 2 - 2; x <= w / 2 + 2; x++) cv.Force(x, y0 + 5, 'G');        // pata dourada (pontinho) no meio
    }

    /// <summary>
    /// Tenda do pátio 112×88 (7 × 5,5 tiles): lona listrada no alto e dois postes até o chão. É ela que faz a sombra
    /// onde o gato se esconde com o fardo de feno.
    /// </summary>
    static PixelCanvas Tent()
    {
        CanvasPalette();
        var cv = new PixelCanvas(112, 88);
        foreach (int px in new[] { 3, 107 })
            for (int y = 10; y < 88; y++) { cv.Force(px, y, 'k'); cv.Force(px + 1, y, 'W'); cv.Force(px + 2, y, 'w'); cv.Force(px + 3, y, 'k'); }
        Canopy(cv, 112, 2);
        // cordas diagonais dos cantos até o chão
        foreach (var p in PixelKit.Line(1, 16, 0, 40)) cv.Force(p.X, p.Y, 'c');
        foreach (var p in PixelKit.Line(110, 16, 111, 40)) cv.Force(p.X, p.Y, 'c');
        return cv;
    }

    /// <summary>Toldo 112×18 da arquibancada (cobre o lugar do atirador).</summary>
    static PixelCanvas Awning()
    {
        CanvasPalette();
        var cv = new PixelCanvas(112, 18);
        Canopy(cv, 112, 0);
        return cv;
    }

    /// <summary>Poste de madeira 6×h com braçadeira de ferro (sustenta a arquibancada e o toldo).</summary>
    static PixelCanvas Post(int h)
    {
        CanvasPalette();
        var cv = new PixelCanvas(6, h);
        for (int y = 0; y < h; y++)
        {
            cv.Force(0, y, 'k'); cv.Force(1, y, 'W'); cv.Force(2, y, 'W'); cv.Force(3, y, 'w'); cv.Force(4, y, 'w'); cv.Force(5, y, 'k');
            if (y % 9 == 4) { cv.Force(2, y, 'w'); }                    // veio da madeira
        }
        for (int x = 0; x < 6; x++) { cv.Force(x, 1, 'k'); cv.Force(x, h - 2, 'k'); }
        return cv;
    }

    // ---------------------------------------------------------- estátua gigante

    /// <summary>
    /// Estátua gigante de cão paladino 40×72 (2,5 × 4,5 tiles), de pedra da fundação, segurando a marreta à frente.
    /// Fica entre a tocha e a sombra da galeria: é ela que bloqueia a luz. Luz da direita; pata dourada no pedestal.
    /// </summary>
    static PixelCanvas GiantStatue()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('S', "#4a455c"); PixelKit.P('L', "#5e5872"); PixelKit.P('H', "#6e6884"); PixelKit.P('D', "#3a3549");
        PixelKit.P('k', "#1c1a24"); PixelKit.P('G', "#d9a441"); PixelKit.P('g', "#8a621e");
        var cv = new PixelCanvas(40, 72);
        Func<float, float, char> shade = (nx, ny) => nx > 0.45f ? 'L' : nx < -0.5f ? 'D' : 'S';
        // pedestal em degraus
        for (int x = 2; x < 38; x++) for (int y = 62; y < 72; y++) cv.Force(x, y, y == 62 ? 'H' : x > 33 ? 'L' : x < 6 ? 'D' : 'S');
        for (int x = 6; x < 34; x++) for (int y = 56; y < 62; y++) cv.Force(x, y, y == 56 ? 'H' : x > 30 ? 'L' : x < 9 ? 'D' : 'S');
        for (int x = 17; x < 23; x++) for (int y = 64; y < 69; y++) cv.Force(x, y, (x + y) % 3 == 0 ? 'g' : 'G'); // pata dourada
        // corpo (manto/armadura) — trapézio
        for (int y = 24; y < 56; y++)
        {
            float t = (y - 24) / 32f;
            int half = (int)(8 + t * 5);
            for (int x = 20 - half; x <= 20 + half; x++) cv.Force(x, y, shade((x - 20) / (float)half, 0));
        }
        // cabeça de cão com orelhas em pé e focinho para a esquerda
        PixelKit.Ellipse(cv, 20, 17, 7, 7, (nx, ny) => shade(nx, ny));
        PixelKit.Ellipse(cv, 12, 19, 5, 3.2f, (nx, ny) => ny < -0.3f ? 'L' : 'S');           // focinho
        for (int k = 0; k < 7; k++) { cv.Force(16 + k / 2, 9 - k, 'S'); cv.Force(17 + k / 2, 9 - k, 'D'); }  // orelha esquerda
        for (int k = 0; k < 7; k++) { cv.Force(24 - k / 3, 9 - k, 'L'); cv.Force(25 - k / 3, 9 - k, 'S'); } // orelha direita
        cv.Force(16, 15, 'k'); cv.Force(7, 19, 'k');                                          // olho e nariz
        // marreta à frente, com o cabo apoiado no pedestal
        for (int y = 28; y < 56; y++) { cv.Force(9, y, 'D'); cv.Force(10, y, 'S'); }
        for (int x = 4; x < 16; x++) for (int y = 22; y < 29; y++) cv.Force(x, y, y == 22 ? 'H' : x > 13 ? 'L' : 'S');
        // patas segurando o cabo
        PixelKit.Ellipse(cv, 11, 36, 3, 2.5f, (nx, ny) => nx > 0 ? 'L' : 'S');
        PixelKit.Outline(cv, 'k');
        return cv;
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
