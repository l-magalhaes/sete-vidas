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
        PixelKit.WriteSprite(DogStatue(26, 48), Path.Combine(cid, "env_cid_cul_estatua_paladino.png"), "env_cid_cul_estatua_paladino", template, false);
        PixelKit.WriteSprite(Sanctuary(false, 0), Path.Combine(common, "env_com_obj_santuario_seco.png"), "env_com_obj_santuario_seco", template, false);
        var sanct = new List<PixelCanvas> { Sanctuary(false, 0) };
        for (int i = 0; i < 4; i++)
        {
            var f = Sanctuary(true, i); sanct.Add(f);
            PixelKit.WriteSprite(f, Path.Combine(common, "env_com_obj_santuario_ativo_" + i + ".png"), "env_com_obj_santuario_ativo_" + i, template, false);
        }
        // borda da frente da caixinha de areia (mesmo quadro 64×32): fica na frente do gato enquanto ele cochila nela
        var front = Sanctuary(false, 0);
        for (int x = 0; x < front.W; x++) for (int y = 0; y < front.H; y++) if (x < 46 || y < 30) front.Px[x, y] = '.';
        PixelKit.WriteSprite(front, Path.Combine(common, "env_com_obj_santuario_frente.png"), "env_com_obj_santuario_frente", template, false);
        sanct.Add(front);
        Preview(sanct, Path.Combine(previewDir, "santuario.png"), 6, "#2a2438");
        // versão compacta (sem a cesta de frutas: 4 tiles visíveis) para lugares apertados, como a câmara (entre a parede
        // de fora e a chaminé). Mesmo quadro de 80 px: só a cesta some, então nada muda de posição
        Func<PixelCanvas, PixelCanvas> compact = c => { for (int x = 0; x <= 16; x++) for (int y = 0; y < c.H; y++) c.Px[x, y] = '.'; return c; };
        PixelKit.WriteSprite(compact(Sanctuary(false, 0)), Path.Combine(common, "env_com_obj_santuario_compacto_seco.png"), "env_com_obj_santuario_compacto_seco", template, false);
        for (int i = 0; i < 4; i++)
            PixelKit.WriteSprite(compact(Sanctuary(true, i)), Path.Combine(common, "env_com_obj_santuario_compacto_ativo_" + i + ".png"), "env_com_obj_santuario_compacto_ativo_" + i, template, false);
        PixelKit.WriteSprite(Crates(), Path.Combine(sol, "env_sol_obj_esconderijo_caixotes.png"), "env_sol_obj_esconderijo_caixotes", template, false);
        PixelKit.WriteSprite(HayBale(), Path.Combine(sol, "env_sol_obj_esconderijo_feno.png"), "env_sol_obj_esconderijo_feno", template, false);
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
    static PixelCanvas GiantStatue() { return DogStatue(40, 72); }

    /// <summary>
    /// Estátua de cão paladino em qualquer tamanho (o desenho é feito em proporções de 40×72): pedestal em degraus
    /// com a pata dourada, corpo de manto/armadura, cabeça com orelhas em pé e focinho para a esquerda, e a marreta
    /// à frente com o cabo apoiado no pedestal. Pedra da fundação, luz da direita. A gigante (40×72) e a comum (26×48)
    /// saem do mesmo desenho: o pátio, a galeria e a câmara ficam coerentes.
    /// </summary>
    static PixelCanvas DogStatue(int W, int H)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('S', "#4a455c"); PixelKit.P('L', "#5e5872"); PixelKit.P('H', "#6e6884"); PixelKit.P('D', "#3a3549");
        PixelKit.P('k', "#1c1a24"); PixelKit.P('G', "#d9a441"); PixelKit.P('g', "#8a621e");
        var cv = new PixelCanvas(W, H);
        float sx = W / 40f, sy = H / 72f;
        Func<float, int> X = v => (int)Math.Round(v * sx);
        Func<float, int> Y = v => (int)Math.Round(v * sy);
        Func<float, float, char> shade = (nx, ny) => nx > 0.45f ? 'L' : nx < -0.5f ? 'D' : 'S';
        int cx = X(20);
        // pedestal em degraus
        for (int x = X(2); x < X(38); x++) for (int y = Y(62); y < H; y++) cv.Force(x, y, y == Y(62) ? 'H' : x > X(33) ? 'L' : x < X(6) ? 'D' : 'S');
        for (int x = X(6); x < X(34); x++) for (int y = Y(56); y < Y(62); y++) cv.Force(x, y, y == Y(56) ? 'H' : x > X(30) ? 'L' : x < X(9) ? 'D' : 'S');
        // pata dourada da Ordem entalhada no pedestal: 3 dedos + almofada (5×5; 7×6 na gigante)
        string[] paw = W >= 36
            ? new[] { ".G.G.G.", ".g.g.g.", ".......", "..GGG..", ".GGGGG.", "..ggg.." }
            : new[] { "G.G.G", ".....", ".GGG.", "GGGGG", ".ggg." };
        int px0 = cx - paw[0].Length / 2, py0 = Y(62) + Math.Max(1, (H - Y(62) - paw.Length) / 2);
        for (int r = 0; r < paw.Length; r++)
            for (int c = 0; c < paw[r].Length; c++)
                if (paw[r][c] != '.') cv.Force(px0 + c, py0 + r, paw[r][c]);
        // corpo (manto/armadura) — trapézio

        for (int y = Y(24); y < Y(56); y++)
        {
            float t = (y - Y(24)) / (float)Math.Max(1, Y(56) - Y(24));
            int half = (int)((8 + t * 5) * sx);
            for (int x = cx - half; x <= cx + half; x++) cv.Force(x, y, shade((x - cx) / (float)Math.Max(1, half), 0));
        }
        // cabeça de cão com orelhas em pé e focinho para a esquerda
        PixelKit.Ellipse(cv, 20 * sx, 17 * sy, 7 * sx, 7 * sy, (nx, ny) => shade(nx, ny));
        PixelKit.Ellipse(cv, 12 * sx, 19 * sy, 5 * sx, Math.Max(1.6f, 3.2f * sy), (nx, ny) => ny < -0.3f ? 'L' : 'S'); // focinho
        int ears = Math.Max(4, Y(7));
        for (int k = 0; k < ears; k++) { cv.Force(X(16) + k / 2, Y(9) - k, 'S'); cv.Force(X(17) + k / 2, Y(9) - k, 'D'); } // orelha esquerda
        for (int k = 0; k < ears; k++) { cv.Force(X(24) - k / 3, Y(9) - k, 'L'); cv.Force(X(25) - k / 3, Y(9) - k, 'S'); } // orelha direita
        cv.Force(X(16), Y(15), 'k'); cv.Force(X(7), Y(19), 'k');                                   // olho e nariz
        // marreta à frente, com o cabo apoiado no pedestal
        for (int y = Y(28); y < Y(56); y++) { cv.Force(X(9), y, 'D'); cv.Force(X(9) + 1, y, 'S'); }
        for (int x = X(4); x < X(16); x++) for (int y = Y(22); y < Y(29); y++) cv.Force(x, y, y == Y(22) ? 'H' : x > X(13) ? 'L' : 'S');
        // patas segurando o cabo
        PixelKit.Ellipse(cv, 11 * sx, 36 * sy, Math.Max(1.5f, 3 * sx), Math.Max(1.5f, 2.5f * sy), (nx, ny) => nx > 0 ? 'L' : 'S');
        PixelKit.Outline(cv, 'k');
        return cv;
    }

    // ------------------------------------------------------ santuário

    /// <summary>
    /// Santuário felino 80×32 na pedra da fundação: estátua de gato sentado sobre um pilar com uma bica de peixe,
    /// a bacia da fonte embaixo, uma cesta de frutas à esquerda e uma caixinha de areia à direita (o lugar onde o
    /// gato descansa). Seco = ainda não descansou aqui. Ativo = a água corre (frame 0..3) e os olhos acendem.
    /// </summary>
    static PixelCanvas Sanctuary(bool active, int frame)
    {
        PixelKit.Pal.Clear();
        PixelKit.P('S', "#4a455c"); PixelKit.P('L', "#5e5872"); PixelKit.P('H', "#6e6884"); PixelKit.P('D', "#3a3549");
        PixelKit.P('k', "#1c1a24"); PixelKit.P('m', "#2a2434");                                   // contorno, fundo seco
        PixelKit.P('O', "#f0c060"); PixelKit.P('o', "#8a621e");                                   // olhos acesos / apagados
        PixelKit.P('E', "#c8f4ff"); PixelKit.P('e', "#7fd0f0"); PixelKit.P('F', "#f0fcff");      // água (mesma da cachoeira)
        PixelKit.Pal['W'] = Color.FromArgb(220, 0x3a, 0x78, 0xc0); PixelKit.Pal['w'] = Color.FromArgb(230, 0x5a, 0xa0, 0xe0);
        PixelKit.P('B', "#9a7044"); PixelKit.P('b', "#6b4a2b"); PixelKit.P('n', "#4a321e");      // vime e madeira
        PixelKit.P('A', "#9e4a32"); PixelKit.P('a', "#c86a4a");                                   // maçã
        PixelKit.P('R', "#d88a3a"); PixelKit.P('r', "#f0b060");                                   // laranja
        PixelKit.P('P', "#c8b04a"); PixelKit.P('p', "#e0d070");                                   // pera
        PixelKit.P('Y', "#d8c08a"); PixelKit.P('y', "#b89c66"); PixelKit.P('h', "#ecdcae");      // areia
        PixelKit.P('i', "#8e8ca4");                                                               // pazinha de metal
        var cv = new PixelCanvas(80, 32);

        // pilar com a estátua de gato sentado (de frente, cauda enrolada à direita)
        for (int x = 28; x <= 36; x++) for (int y = 13; y <= 23; y++) cv.Force(x, y, x == 28 ? 'D' : x >= 35 ? 'L' : y == 13 ? 'H' : 'S');
        PixelKit.Ellipse(cv, 32f, 9.5f, 4.2f, 3.6f, (nx, ny) => nx > 0.4f ? 'L' : nx < -0.55f ? 'D' : 'S');   // corpo
        PixelKit.Ellipse(cv, 32f, 4.5f, 3.2f, 2.6f, (nx, ny) => nx > 0.4f ? 'L' : ny < -0.5f ? 'H' : 'S');   // cabeça
        cv.Force(29, 1, 'S'); cv.Force(29, 2, 'S'); cv.Force(30, 2, 'S');                                     // orelhas
        cv.Force(35, 1, 'L'); cv.Force(35, 2, 'L'); cv.Force(34, 2, 'L');
        cv.Force(37, 10, 'L'); cv.Force(37, 11, 'L'); cv.Force(36, 12, 'L'); cv.Force(38, 9, 'L');             // cauda
        char eye = active ? 'O' : 'o';
        cv.Force(31, 4, eye); cv.Force(33, 4, eye);
        // bica de peixe na frente do pilar
        cv.Force(31, 16, 'H'); cv.Force(32, 16, 'H'); cv.Force(33, 16, 'L'); cv.Force(30, 15, 'L'); cv.Force(30, 17, 'L');
        cv.Force(32, 17, 'k');                                                                                 // boca da bica

        // bacia: borda com luz, frente em blocos
        for (int x = 19; x <= 45; x++)
            for (int y = 22; y <= 31; y++)
            {
                if ((x == 19 || x == 45) && y < 24) continue;                                                // cantos
                char c = y == 22 ? 'H' : y == 23 ? 'L' : x >= 43 ? 'L' : x <= 20 ? 'D' : 'S';
                if (y > 24 && (y == 28 || (x - 19 + (y > 28 ? 5 : 0)) % 9 == 0)) c = 'D';                    // juntas
                cv.Force(x, y, c);
            }
        // dentro da bacia (linha de cima): água ou fundo seco
        for (int x = 21; x <= 43; x++)
        {
            if (active) cv.Force(x, 22, (x + frame * 2) % 7 == 0 ? 'E' : (x + frame) % 5 == 0 ? 'e' : 'w');
            else cv.Force(x, 22, x % 6 == 2 ? 'D' : 'm');
        }
        if (active)
        {
            // filete caindo da bica (faixas descem um pixel por quadro) e o respingo na água
            for (int y = 18; y <= 21; y++) cv.Force(32, y, (y + 4 - frame) % 4 == 0 ? 'E' : (y + 4 - frame) % 4 == 2 ? 'e' : 'W');
            cv.Force(31, 18, 'W');
            int s = frame % 2;
            cv.Force(30 - s, 21, 'F'); cv.Force(34 + s, 21, 'F'); cv.Force(32, 21, 'F');
            if (frame == 1 || frame == 3) { cv.Force(29, 20, 'E'); cv.Force(35, 20, 'E'); }
        }

        // cesta de frutas (esquerda): vime em trama, alça e frutas por cima
        for (int x = 4; x <= 15; x++)
            for (int y = 24; y <= 31; y++)
            {
                if (y > 28 && (x < 4 + (y - 28) || x > 15 - (y - 28))) continue;                             // fundo afunilado
                cv.Force(x, y, y == 24 ? 'B' : (x + y) % 3 == 0 ? 'n' : (x + y) % 3 == 1 ? 'b' : 'B');
            }
        foreach (var p in PixelKit.Line(5, 24, 7, 18)) cv.Force(p.X, p.Y, 'b');                              // alça
        foreach (var p in PixelKit.Line(7, 18, 12, 18)) cv.Force(p.X, p.Y, 'b');
        foreach (var p in PixelKit.Line(12, 18, 14, 24)) cv.Force(p.X, p.Y, 'b');
        PixelKit.Ellipse(cv, 7f, 22.5f, 2.2f, 2f, (nx, ny) => nx > 0.2f && ny < 0 ? 'a' : 'A');              // maçã
        PixelKit.Ellipse(cv, 11.5f, 22f, 2.3f, 2.2f, (nx, ny) => nx > 0.2f && ny < 0 ? 'r' : 'R');           // laranja
        PixelKit.Ellipse(cv, 14f, 22.8f, 1.6f, 1.8f, (nx, ny) => ny < -0.2f ? 'p' : 'P');                    // pera
        cv.Force(14, 20, 'n'); cv.Force(7, 20, 'n');                                                          // cabinhos

        // caixinha de areia (direita): bandeja larga e baixa do tamanho do gato enrolado, cantoneiras um pouco mais
        // altas, areia lisa com uma covinha no meio (onde ele cochila), patinhas de quem passou e a pazinha no canto
        for (int x = 47; x <= 78; x++)
            for (int y = 28; y <= 31; y++)
            {
                bool post = x <= 48 || x >= 77;
                if (y == 28 && !post) continue;
                char c = y == 31 ? 'n' : post ? (x == 47 || x == 78 ? 'n' : 'B') : y == 29 ? 'B' : (x - 47) % 8 == 0 ? 'n' : 'b';
                cv.Force(x, y, c);
            }
        for (int x = 49; x <= 76; x++)
        {
            bool hollow = x >= 56 && x <= 70;                                                             // covinha
            int top = x < 51 || x > 74 ? 28 : hollow ? 28 : 27;
            for (int y = top; y <= 28; y++)
            {
                char c = y == top && !hollow ? (x % 5 == 2 ? 'h' : 'Y') : hollow ? (x % 4 == 0 ? 'Y' : 'y') : 'Y';
                cv.Force(x, y, c);
            }
        }
        cv.Force(52, 27, 'y'); cv.Force(54, 28, 'y'); cv.Force(73, 27, 'y');                                // patinhas
        foreach (var p in PixelKit.Line(75, 26, 77, 22)) cv.Force(p.X, p.Y, 'n');                           // cabo da pazinha
        cv.Force(74, 27, 'i'); cv.Force(75, 27, 'i');

        PixelKit.Outline(cv, 'k', "EeFWw");
        return cv;
    }

    // ------------------------------------------------------ esconderijos

    /// <summary>
    /// Desenhos do esconderijo (o mesmo HidingSpot do matinho: 32×16, 15 px de altura, na frente do gato — agachado,
    /// ele some; de pé, a cabeça aparece). Caixotes dentro do castelo; fardo de feno no pátio (embaixo da tenda).
    /// </summary>
    static PixelCanvas Crates()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('W', "#9a7044"); PixelKit.P('w', "#6b4a2b"); PixelKit.P('d', "#4a321e"); PixelKit.P('k', "#1c1a24"); PixelKit.P('i', "#8e8ca4");
        var cv = new PixelCanvas(32, 16);
        // caixote grande (esquerda), um menor ao lado e um em cima, desencontrados
        Box(cv, 1, 4, 17, 15); Box(cv, 17, 8, 30, 15); Box(cv, 4, 1, 14, 4);
        return cv;
    }

    static void Box(PixelCanvas cv, int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                bool edge = x == x0 || x == x1 || y == y0 || y == y1;
                char c = edge ? 'k' : y == y0 + 1 ? 'W' : x == x1 - 1 ? 'W' : x == x0 + 1 ? 'd' : (y - y0) % 3 == 0 ? 'd' : 'w';
                cv.Force(x, y, c);
            }
        foreach (var p in PixelKit.Line(x0 + 1, y0 + 1, x1 - 1, y1 - 1)) cv.Force(p.X, p.Y, 'd');   // travessa em X
        cv.Force(x0 + 2, y0 + 2, 'i'); cv.Force(x1 - 2, y0 + 2, 'i');                               // pregos
    }

    static PixelCanvas HayBale()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('Y', "#c8a85a"); PixelKit.P('y', "#a8883e"); PixelKit.P('o', "#7a6430"); PixelKit.P('h', "#e0c878");
        PixelKit.P('r', "#6b4a2b"); PixelKit.P('k', "#3a2a18");
        var cv = new PixelCanvas(32, 16);
        for (int x = 1; x < 31; x++)
            for (int y = 2; y < 16; y++)
            {
                if ((x < 3 || x > 28) && (y < 4 || y > 14)) continue;           // cantos arredondados
                bool edge = x == 1 || x == 30 || y == 2 || y == 15;
                float n = PixelKit.Noise(x, y, 950);
                char c = edge ? 'o' : y < 5 ? (n > 0.6f ? 'h' : 'Y') : n > 0.8f ? 'h' : n < 0.25f ? 'y' : (x % 4 == 0 ? 'y' : 'Y');
                cv.Force(x, y, c);
            }
        foreach (int rx in new[] { 9, 22 }) for (int y = 2; y < 16; y++) cv.Force(rx, y, 'r');     // amarras
        for (int x = 0; x < 32; x += 5) cv.Force(x, 1, 'Y');                                       // palhas soltas
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
