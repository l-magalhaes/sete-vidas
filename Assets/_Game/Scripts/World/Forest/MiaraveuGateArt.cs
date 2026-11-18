using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O portão de Miaravéu na saída da fase (R5, 18/11/2026, pedido do usuário: "uma entrada 'bem-vindo a Miaravéu'; a
    /// entrada da cidade não parece estar preservada"): o arco de pedra quebrado no meio, o pilar da direita partido com
    /// os escombros no chão, musgo nas pedras e a placa de madeira pendurada, rachada, com a tinta gasta. W × H px, linha 0
    /// embaixo (os pés no chão).
    /// </summary>
    public static class MiaraveuGateArt
    {
        public const int W = 160, H = 176;

        static readonly Color32 Ink = new Color32(0x1a, 0x14, 0x18, 0xff);
        static readonly Color32 Stone = new Color32(0x6a, 0x64, 0x78, 0xff);
        static readonly Color32 StoneL = new Color32(0x8a, 0x84, 0x9a, 0xff);
        static readonly Color32 StoneD = new Color32(0x4a, 0x44, 0x58, 0xff);
        static readonly Color32 Moss = new Color32(0x4a, 0x6a, 0x3a, 0xff);
        static readonly Color32 MossL = new Color32(0x6a, 0x8c, 0x4a, 0xff);
        static readonly Color32 Wood = new Color32(0x7a, 0x4e, 0x2a, 0xff);
        static readonly Color32 WoodL = new Color32(0x9a, 0x68, 0x38, 0xff);
        static readonly Color32 WoodD = new Color32(0x4e, 0x30, 0x1a, 0xff);
        static readonly Color32 Rope = new Color32(0xc8, 0xb0, 0x8a, 0xff);
        static readonly Color32 Paint = new Color32(0xf0, 0xe0, 0xb0, 0xff);
        static readonly Color32 PaintFaded = new Color32(0xb8, 0xa4, 0x80, 0xff);

        public const string Line1 = "BEM-VINDO A", Line2 = "MIARAVEU";

        public static Color32[] Pixels()
        {
            var px = new Color32[W * H];
            // o pilar da esquerda, inteiro, com o capitel
            Blocks(px, 8, 0, 31, 149, 0);
            Box(px, 4, 150, 35, 159, StoneL, Stone, StoneD);
            // o pilar da direita, partido (o topo em dentes)
            for (int x = 128; x <= 151; x++)
            {
                int top = 92 + ((x / 3) * 7 % 9) + ((x / 3) % 3 == 0 ? 3 : 0);
                for (int y = 0; y <= top; y++) Set(px, x, y, Shade(x, y, 128, 151, top));
            }
            MortarLines(px, 128, 0, 151, 100);
            // o arco: sai do capitel da esquerda e quebra no meio
            for (int x = 30; x <= 100; x++)
            {
                int bottom = 140 - (int)(Mathf.Sin((x - 30) / 140f * Mathf.PI) * 6f);
                int top = 158 - (x > 92 ? (x - 92) * 2 : 0);   // a ponta quebrada desce em dente
                for (int y = bottom; y <= top; y++) Set(px, x, y, y >= top - 1 ? StoneL : y <= bottom + 1 ? StoneD : Stone);
            }
            MortarLines(px, 30, 138, 100, 158);
            // os escombros no chão (o pedaço do arco caído e pedras)
            Box(px, 96, 0, 124, 11, StoneL, Stone, StoneD);
            Box(px, 140, 0, 158, 7, StoneL, Stone, StoneD);
            Box(px, 118, 0, 132, 5, StoneL, Stone, StoneD);
            Box(px, 86, 0, 95, 4, StoneL, Stone, StoneD);
            // o musgo: escorrendo do alto das pedras
            for (int x = 6; x <= 150; x++)
            {
                int topLeft = x <= 35 ? 159 : x <= 100 ? 158 : -1;
                if (topLeft > 0 && (x * 13 % 7) < 4)
                {
                    int len = 1 + (x * 5 % 6);
                    for (int k = 0; k < len; k++) Set(px, x, topLeft - k, k == 0 ? MossL : Moss);
                }
            }
            for (int y = 0; y < 40; y++) if ((y * 11 % 5) < 3) { Set(px, 8, y, Moss); Set(px, 9, y, MossL); }   // no pé do pilar
            // as cordas e a placa (pendurada no arco, rachada no canto)
            for (int y = 128; y <= 139; y++) { Set(px, 42, y, Rope); Set(px, 86 + (y % 2), y, Rope); }
            int sx0 = 30, sx1 = 98, sy0 = 102, sy1 = 128;
            for (int x = sx0; x <= sx1; x++)
                for (int y = sy0; y <= sy1; y++)
                {
                    if (x > sx1 - 8 && y < sy0 + (x - (sx1 - 8))) continue;   // o canto quebrado
                    bool edge = x == sx0 || x == sx1 || y == sy0 || y == sy1;
                    Set(px, x, y, edge ? WoodD : (y - sy0) % 9 == 0 ? WoodD : (x * 3 + y) % 17 == 0 ? WoodL : Wood);
                }
            for (int k = 0; k < 10; k++) Set(px, 60 + k / 2, sy1 - 1 - k, WoodD);   // a rachadura
            // o texto, gasto (uns pontos da tinta caíram)
            int w1 = PixelFont.Measure(Line1), w2 = PixelFont.Measure(Line2);
            var paint = new Color32[W * H];
            PixelFont.Stamp(paint, W, H, Line1, (sx0 + sx1 - w1) / 2 + 1, 117, Paint);
            PixelFont.Stamp(paint, W, H, Line2, (sx0 + sx1 - w2) / 2 + 1, 106, Paint);
            // o acento do É (MIARAVÉU): o 7º caractere de "MIARAVEU"
            int ex = (sx0 + sx1 - w2) / 2 + 1 + PixelFont.Measure("MIARAV") + 1;
            Set(paint, ex + 3, 115, Paint); Set(paint, ex + 2, 114, Paint);
            for (int i = 0; i < paint.Length; i++)
            {
                if (paint[i].a == 0 || px[i].a == 0) continue;
                int x = i % W, y = i / W;
                px[i] = (x * 7 + y * 3) % 23 == 0 ? Wood : (x + y) % 7 == 0 ? PaintFaded : Paint;
            }
            // o contorno do jogo (escuro, por fora)
            var outline = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (px[y * W + x].a != 0) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && nx < W && ny >= 0 && ny < H && px[ny * W + nx].a != 0 && px[ny * W + nx].r != Rope.r) { outline[y * W + x] = true; break; }
                    }
                }
            for (int i = 0; i < px.Length; i++) if (outline[i]) px[i] = Ink;
            return px;
        }

        static Color32 Shade(int x, int y, int x0, int x1, int top)
        {
            if (x <= x0 + 1) return StoneL;
            if (x >= x1 - 1 || y >= top - 1) return x >= x1 - 1 ? StoneD : StoneL;
            return Stone;
        }

        /// <summary>Um pilar de blocos de pedra (a luz na esquerda, a sombra na direita), com as juntas.</summary>
        static void Blocks(Color32[] px, int x0, int y0, int x1, int y1, int seed)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Set(px, x, y, x <= x0 + 1 ? StoneL : x >= x1 - 1 ? StoneD : Stone);
            MortarLines(px, x0, y0, x1, y1);
        }

        /// <summary>As juntas dos blocos: linhas a cada 12 px, com as verticais alternadas, e umas rachaduras.</summary>
        static void MortarLines(Color32[] px, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++)
            {
                int row = (y - y0) / 12;
                bool joint = (y - y0) % 12 == 0;
                for (int x = x0 + 1; x < x1; x++)
                {
                    if (Get(px, x, y).a == 0) continue;
                    bool vertical = ((x - x0) + (row % 2) * 8) % 16 == 0;
                    if (joint || vertical) Set(px, x, y, StoneD);
                    else if ((x * 31 + y * 17) % 97 == 0) Set(px, x, y, StoneD);   // rachadurinha
                }
            }
        }

        static void Box(Color32[] px, int x0, int y0, int x1, int y1, Color32 light, Color32 mid, Color32 dark)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Set(px, x, y, y == y1 || x == x0 ? light : x == x1 || y == y0 ? dark : mid);
        }

        static Color32 Get(Color32[] px, int x, int y) => x < 0 || x >= W || y < 0 || y >= H ? default : px[y * W + x];

        static void Set(Color32[] px, int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            px[y * W + x] = c;
        }
    }
}
