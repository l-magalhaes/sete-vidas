using System;
using System.Collections.Generic;

namespace SeteVidas.World
{
    /// <summary>
    /// Os desenhos das vespas da raiz (pedido do usuário, 17/11/2026): a colmeia pendurada onde cada uma dorme e o mel que
    /// ela solta ao morrer (20%). Feitos em código, pixel a pixel (ARGB, linha 0 em cima); C# 5 e sem UnityEngine nesta
    /// parte, para a prévia. Luz da lua do alto à esquerda.
    /// </summary>
    public static class BeeArt
    {
        public const int HiveW = 24, HiveH = 30, HoneyW = 9, HoneyH = 10;
        static readonly uint Outline = C("#24160c"), Stem = C("#3b2a1e"), Dark = C("#6e4519"), Mid = C("#9c6a26"),
            Light = C("#c99238"), Shine = C("#e8bc5c"), Hole = C("#120a06"), HoleRim = C("#4a2e12"), Drip = C("#f0b840"),
            DripLight = C("#ffe08a"), Moss = C("#3c5a3e");

        static uint C(string hex) { return 0xff000000u | Convert.ToUInt32(hex.Substring(1), 16); }

        static uint Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)s * 982451653u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        /// <summary>
        /// A colmeia: uma gota pendurada (o talo em cima), em camadas de papel/cera tortas, a boca escura no terço de baixo,
        /// o mel escorrendo e musgo no talo.
        /// </summary>
        public static uint[] Hive()
        {
            int w = HiveW, h = HiveH;
            var px = new uint[w * h];
            float cx = w / 2f - 0.5f;
            for (int y = 3; y < h - 1; y++)
            {
                float t = (y - 3f) / (h - 4f);
                float half = 10.5f * (float)Math.Sin(Math.PI * Math.Pow(t, 0.75)) + 0.5f;   // gorda embaixo, fina no talo
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    if (Math.Abs(dx) > half) continue;
                    // as camadas: faixas de 4 px, cada uma tortinha (o desenho não repete)
                    int band = (int)Math.Floor((y + 1.5f * Math.Sin(x * 0.5 + y * 0.2)) / 4f);
                    bool edge = ((y + (int)(1.5f * Math.Sin(x * 0.5 + y * 0.2)) + 400) % 4) == 0;
                    float lit = -dx / (half + 0.01f) * 0.6f - (t - 0.4f) * 0.8f;   // luz do alto à esquerda
                    uint c = lit > 0.45f ? Shine : lit > 0.05f ? Light : lit > -0.45f ? Mid : Dark;
                    if (edge) c = c == Shine ? Light : c == Light ? Mid : Dark;
                    if (band % 3 == 1 && Hash(x, band, 3) % 7u == 0u) c = Dark;   // uma falha na cera
                    px[y * w + x] = c;
                }
            }
            // o talo e o musgo
            for (int y = 0; y < 4; y++) for (int x = (int)cx - 1; x <= (int)cx + 1; x++) px[y * w + x] = Stem;
            px[2 * w + (int)cx - 2] = Moss; px[3 * w + (int)cx + 2] = Moss; px[1 * w + (int)cx + 1] = Moss;
            // a boca: oval escura no terço de baixo, com a borda
            int hx = (int)cx + 1, hy = 20;
            for (int y = hy - 3; y <= hy + 3; y++)
                for (int x = hx - 4; x <= hx + 4; x++)
                {
                    float ex = (x - hx) / 3.6f, ey = (y - hy) / 2.6f;
                    float r2 = ex * ex + ey * ey;
                    if (r2 < 0.7f) px[y * w + x] = Hole;
                    else if (r2 < 1.15f && px[y * w + x] != 0u) px[y * w + x] = HoleRim;
                }
            // o mel escorrendo da boca
            for (int i = 0; i < 6; i++) { int y = hy + 3 + i; if (y < h) px[y * w + hx - 1] = i < 4 ? Drip : DripLight; }
            px[(hy + 4) * w + hx] = Drip;
            Outline_(px, w, h);
            return px;
        }

        /// <summary>O mel que a vespa solta: uma gota grossa de mel com o brilho.</summary>
        public static uint[] Honey()
        {
            int w = HoneyW, h = HoneyH;
            var px = new uint[w * h];
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    float dx = (x - 3.5f) / 3.2f, dy = (y - 5.5f) / 3.6f;
                    if (y < 4) dx /= 0.4f + y * 0.2f;   // a ponta da gota em cima
                    if (dx * dx + dy * dy > 1f) continue;
                    uint c = dx < -0.2f && dy < 0.2f ? DripLight : dy > 0.5f ? Mid : Drip;
                    px[y * w + x] = c;
                }
            px[4 * w + 2] = 0xffffffffu;   // o reflexo
            Outline_(px, w, h);
            return px;
        }

        static void Outline_(uint[] px, int w, int h)
        {
            var add = new List<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x] != 0u) continue;
                    if ((x > 0 && px[y * w + x - 1] != 0u && px[y * w + x - 1] != Outline) ||
                        (x < w - 1 && px[y * w + x + 1] != 0u && px[y * w + x + 1] != Outline) ||
                        (y > 0 && px[(y - 1) * w + x] != 0u && px[(y - 1) * w + x] != Outline) ||
                        (y < h - 1 && px[(y + 1) * w + x] != 0u && px[(y + 1) * w + x] != Outline)) add.Add(y * w + x);
                }
            foreach (int i in add) px[i] = Outline;
        }
    }
}
