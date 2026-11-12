// Água do exterior (cachoeira e lagoa), inspiração de leitura em Celeste: contorno ciano claro, corpo azul
// translúcido com faixas de correnteza e espuma branca. Tudo animado em 4 quadros que fecham o ciclo.
//  - env_ext_ani_cachoeira_{0..3}: pedaço de 24×32 que se repete na vertical (as faixas descem 8 px por quadro).
//  - env_ext_ani_cachoeira_nascente_{0..3}: 32×24, a fenda na face do penhasco de onde a água sai e faz a curva.
//  - env_ext_ani_cachoeira_espuma_{0..3}: 40×14, espuma e respingos onde a cachoeira bate na lagoa.
//  - env_ext_ani_lagoa_{0..3}: tile 16×16 da lagoa: superfície ondulada a 2 px do topo e corpo translúcido.
// C# 5 (compilado pelo PowerShell).
using System;
using System.Drawing;
using System.IO;

public static class WaterSpriteGen
{
    public const int Frames = 4;
    public const int FallWidth = 24;

    public static void Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        string template = File.ReadAllText(metaTemplatePath);
        string dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Exterior/Water");
        Palette();
        for (int f = 0; f < Frames; f++)
        {
            Write(Fall(f), dir, "env_ext_ani_cachoeira_" + f, template);
            Write(Spring(f), dir, "env_ext_ani_cachoeira_nascente_" + f, template);
            Write(Splash(f), dir, "env_ext_ani_cachoeira_espuma_" + f, template);
            Write(Pool(f), dir, "env_ext_ani_lagoa_" + f, template);
        }
        // melhoria 2: respingos do gato na lagoa (mesma paleta da cachoeira e da lagoa)
        for (int f = 0; f < 6; f++) Write(BigSplash(f), dir, "env_ext_ani_respingo_grande_" + f, template);
        for (int f = 0; f < 4; f++) Write(SmallSplash(f), dir, "env_ext_ani_respingo_pequeno_" + f, template);
        for (int f = 0; f < 3; f++) Write(FootRipple(f), dir, "env_ext_ani_onda_pe_" + f, template);
        for (int f = 0; f < 3; f++) Write(Ring(f), dir, "env_ext_ani_anel_" + f, template);
    }

    // ------------------------------------------------------------ respingos

    /// <summary>Coluna de água de meia largura <paramref name="half"/> e altura <paramref name="h"/>, subindo da base, com espuma no topo.</summary>
    static void Column(PixelCanvas cv, float cx, float half, int h)
    {
        int bottom = cv.H - 1;
        for (int x = 0; x < cv.W; x++)
        {
            float d = Math.Abs(x + 0.5f - cx);
            if (d > half) continue;
            // mais alta no meio (cúpula), com a borda irregular
            int top = bottom - (int)Math.Round(h * (1f - (d / half) * (d / half) * 0.55f)) + (int)(H(x, 7) % 2);
            for (int y = Math.Max(0, top); y <= bottom; y++)
            {
                char c = y == top ? 'F' : y == top + 1 ? 'E' : d > half - 1.2f ? 'e' : ((x + y) % 5 == 0 ? 'V' : 'W');
                cv.Force(x, y, c);
            }
        }
    }

    static void Drop(PixelCanvas cv, int x, int y, bool big)
    {
        cv.Force(x, y, 'F');
        cv.Force(x, y + 1, 'e');
        if (big) { cv.Force(x + 1, y, 'E'); cv.Force(x + 1, y + 1, 'e'); }
    }

    /// <summary>
    /// Respingo grande 32×40 (queda forte, ex.: o salto de fé), 6 quadros: a coroa abre (0), a coluna sobe (1–2), quebra
    /// em gotas (3), as gotas caem para os lados (4) e só sobram pingos rentes à água (5). A base fica na superfície.
    /// </summary>
    static PixelCanvas BigSplash(int f)
    {
        var cv = new PixelCanvas(32, 40);
        float cx = 16f;
        switch (f)
        {
            case 0: Column(cv, cx, 11f, 7); Drop(cv, 5, 30, false); Drop(cv, 25, 29, false); break;
            case 1: Column(cv, cx, 4f, 24); Column(cv, cx, 10f, 9); Drop(cv, 8, 20, true); Drop(cv, 23, 18, true); break;
            case 2: Column(cv, cx, 3.5f, 34); Column(cv, cx, 9f, 8); Drop(cv, 12, 2, true); Drop(cv, 19, 0, false); Drop(cv, 6, 14, true); Drop(cv, 25, 12, false); break;
            case 3:
                Column(cv, cx, 3f, 20); Column(cv, cx, 8f, 6);
                Drop(cv, 10, 8, true); Drop(cv, 21, 6, true); Drop(cv, 14, 3, false); Drop(cv, 4, 16, false); Drop(cv, 27, 15, true);
                break;
            case 4:
                Column(cv, cx, 5f, 6);
                Drop(cv, 6, 22, true); Drop(cv, 25, 20, true); Drop(cv, 2, 30, false); Drop(cv, 29, 28, false); Drop(cv, 12, 18, false); Drop(cv, 19, 17, false);
                break;
            default:
                Column(cv, cx, 7f, 2);
                Drop(cv, 3, 35, false); Drop(cv, 27, 34, false); Drop(cv, 9, 33, false); Drop(cv, 22, 33, false);
                break;
        }
        return cv;
    }

    /// <summary>Respingo pequeno 16×16 (queda curta ou pulo), 4 quadros: coroa, gotas subindo, gotas caindo, pingos.</summary>
    static PixelCanvas SmallSplash(int f)
    {
        var cv = new PixelCanvas(16, 16);
        switch (f)
        {
            case 0: Column(cv, 8f, 6f, 4); break;
            case 1: Column(cv, 8f, 2.5f, 9); Column(cv, 8f, 5f, 3); Drop(cv, 3, 7, false); Drop(cv, 12, 6, false); break;
            case 2: Column(cv, 8f, 2f, 4); Drop(cv, 2, 9, false); Drop(cv, 13, 8, false); Drop(cv, 6, 3, false); Drop(cv, 9, 2, false); break;
            default: Drop(cv, 1, 13, false); Drop(cv, 14, 13, false); Drop(cv, 5, 12, false); Drop(cv, 10, 12, false); break;
        }
        return cv;
    }

    /// <summary>Ondinha nos pés 12×3, 3 quadros: um arco claro que se abre e some (andando dentro da água).</summary>
    static PixelCanvas FootRipple(int f)
    {
        var cv = new PixelCanvas(12, 3);
        int half = 2 + f * 2;
        for (int x = 6 - half; x < 6 + half; x++)
        {
            if (x < 0 || x >= 12) continue;
            bool edge = x == 6 - half || x == 6 + half - 1;
            cv.Force(x, edge ? 1 : 0, f == 2 ? 's' : 'E');
            if (edge) cv.Force(x, 2, 's');
        }
        return cv;
    }

    /// <summary>Anel na superfície 24×3, 3 quadros: o círculo que se abre depois do respingo, cada vez mais fraco.</summary>
    static PixelCanvas Ring(int f)
    {
        var cv = new PixelCanvas(24, 3);
        int half = 4 + f * 4;
        char c = f == 0 ? 'E' : f == 1 ? 'e' : 's';
        for (int x = 12 - half; x < 12 + half; x++)
        {
            if (x < 0 || x >= 24) continue;
            bool edge = x <= 12 - half + 1 || x >= 12 + half - 2;
            cv.Force(x, edge ? 1 : 2, c);
        }
        return cv;
    }

    static void Write(PixelCanvas cv, string dir, string name, string template)
    {
        PixelKit.WriteSprite(cv, Path.Combine(dir, name + ".png"), name, template, false);
    }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('E', "#c8f4ff");                                   // contorno claro
        PixelKit.P('e', "#7fd0f0");                                   // contorno de dentro
        PixelKit.Pal['W'] = Color.FromArgb(185, 0x3a, 0x78, 0xc0);    // corpo translúcido
        PixelKit.Pal['D'] = Color.FromArgb(200, 0x2a, 0x5a, 0x9a);    // corpo fundo
        PixelKit.Pal['w'] = Color.FromArgb(205, 0x5a, 0xa0, 0xe0);    // faixa
        PixelKit.Pal['V'] = Color.FromArgb(225, 0xa8, 0xe4, 0xff);    // brilho da faixa
        PixelKit.P('F', "#f0fcff");                                   // espuma
        PixelKit.Pal['f'] = Color.FromArgb(220, 0xb8, 0xe8, 0xf8);    // espuma na sombra
        PixelKit.Pal['s'] = Color.FromArgb(150, 0xd8, 0xf6, 0xff);    // respingo leve
        PixelKit.P('k', "#1c1a24");                                   // fundo da fenda
        PixelKit.P('r', "#2a2434");                                   // borda da fenda (rocha escura)
        PixelKit.P('t', "#574c62");                                   // lábio de rocha iluminado
    }

    static uint H(int x, int salt) { unchecked { uint h = (uint)x * 2654435761u + (uint)salt * 40503u; h ^= h >> 15; h *= 2246822519u; return h ^ (h >> 13); } }

    /// <summary>Correnteza: faixas verticais de comprimentos variados que descem 8 px por quadro (ciclo de 32 px).</summary>
    static char Current(int x, int y, int frame)
    {
        int offset = (int)(H(x, 1) % 32), len = 3 + (int)(H(x, 2) % 9);
        int s = ((y - frame * 8 - offset) % 32 + 32) % 32;
        if (s < len) return s == 0 ? 'V' : 'w';
        if (H(x, 3) % 5 == 0) return 'D'; // colunas mais fundas
        return 'W';
    }

    static PixelCanvas Fall(int frame)
    {
        var cv = new PixelCanvas(FallWidth, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < FallWidth; x++)
            {
                char c = x == 0 || x == FallWidth - 1 ? 'E' : x == 1 || x == FallWidth - 2 ? 'e' : Current(x, y, frame);
                cv.Force(x, y, c);
            }
        return cv;
    }

    /// <summary>
    /// Nascente 32×24: fenda escura na rocha à esquerda (x 0..10, fica sobre a face do penhasco) e a água saindo
    /// para a direita, fazendo a curva e virando a coluna de 24 px (x 8..31) nas últimas linhas.
    /// </summary>
    static PixelCanvas Spring(int frame)
    {
        var cv = new PixelCanvas(32, 24);
        // fenda: elipse escura com borda de rocha e o lábio de cima iluminado, toda à esquerda de x 8
        // (a face do penhasco fica em x 8 do sprite): nada dela aparece por cima do céu
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 8; x++)
            {
                float dx = (x + 0.5f - 4f) / 3.6f, dy = (y + 0.5f - 8f) / 3.6f;
                float d = dx * dx + dy * dy;
                if (d <= 1f) cv.Force(x, y, 'k');
                else if (d <= 1.5f) cv.Force(x, y, dy < 0 ? 't' : 'r');
            }
        for (int y = 6; y < 24; y++)
        {
            // limites da água em cada linha: sai da fenda (x 3..) para fora da face e se alarga até a coluna (x 8..31)
            int l, r;
            if (y < 10) { l = 3; r = 9 + (y - 6) * 4; }
            else if (y < 17) { l = 3 + (y - 10) * 5 / 7; r = Math.Min(31, 25 + (y - 10)); }
            else { l = 8; r = 31; }
            for (int x = l; x <= r; x++)
            {
                char c = x == l || x == r ? 'E' : x == l + 1 || x == r - 1 ? 'e' : Current(x, y, frame);
                if (y == 6 && x > l && x < r) c = 'E';            // borda de cima da água saindo
                cv.Force(x, y, c);
            }
        }
        return cv;
    }

    /// <summary>Espuma 40×14 onde a água bate: bolhas brancas em volta da linha da água e respingos subindo.</summary>
    static PixelCanvas Splash(int frame)
    {
        var cv = new PixelCanvas(40, 14);
        for (int x = 0; x < 40; x++)
        {
            float dist = Math.Abs(x - 19.5f) / 20f;                  // 0 no meio, 1 nas pontas
            int height = (int)Math.Round((1f - dist * dist) * 6f) + (int)((H(x + frame * 7, 4) % 3));
            for (int y = 0; y < height; y++)
            {
                int py = 9 - y;
                cv.Force(x, py, y == height - 1 ? 'F' : (H(x * 3 + y, 5 + frame) % 3 == 0 ? 'f' : 'F'));
            }
            for (int y = 10; y < 14; y++) if (H(x, y + frame) % 3 != 0 && dist < 0.8f) cv.Force(x, y, 'f'); // espuma sob a linha
            // respingos soltos: gotas de 1 px que sobem e mudam a cada quadro
            if (H(x, 30 + frame) % 9 == 0) cv.Force(x, (int)(H(x, 31 + frame) % 4), 's');
        }
        return cv;
    }

    /// <summary>Tile da lagoa 16×16: linha da superfície ondulada (linha 2), brilho logo abaixo e corpo com reflexos diagonais.</summary>
    static PixelCanvas Pool(int frame)
    {
        var cv = new PixelCanvas(16, 16);
        for (int x = 0; x < 16; x++)
        {
            double t = 2 * Math.PI * (x + frame * 4) / 16.0;
            int surface = 2 + (int)Math.Round(Math.Sin(t) * 0.6);   // 1 px de onda; fecha o ciclo em 4 quadros e entre tiles
            for (int y = surface; y < 16; y++)
            {
                char c;
                if (y == surface) c = 'E';
                else if (y == surface + 1) c = 'e';
                else if (((x + y + frame * 2) % 8) == 0 && y < 12) c = 'w';   // reflexo diagonal andando
                else if (y >= 13) c = 'D';
                else c = 'W';
                if (y == surface + 2 && (x + frame * 3) % 7 == 0) c = 'V';
                cv.Force(x, y, c);
            }
        }
        return cv;
    }
}
