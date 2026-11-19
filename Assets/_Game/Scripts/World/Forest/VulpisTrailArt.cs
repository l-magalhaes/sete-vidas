using System;

namespace SeteVidas.World
{
    /// <summary>
    /// A trilha da Vulpis (F6, Docs/Floresta/plano-implementacao.md: "trilha de flechas presas nas árvores e criaturas
    /// abatidas"): a flecha dela cravada (haste de madeira, penas verdes da capa, a ponta enterrada) e a vespa abatida, de
    /// costas, com a flecha atravessada. Feitos em código, pixel a pixel (ARGB, linha 0 em cima); C# 5 e sem UnityEngine
    /// nesta parte, para a prévia. Luz da lua do alto à esquerda.
    /// </summary>
    public static class VulpisTrailArt
    {
        public const int ArrowSize = 20, WaspW = 18, WaspH = 12;
        static readonly uint Outline = C("#1c140e"), Shaft = C("#8a6438"), ShaftL = C("#b88a52"), Feather = C("#4e8a46"),
            FeatherL = C("#8ff28a"), FeatherD = C("#2c5430"), Nock = C("#e2ffd8");
        static readonly uint WaspD = C("#5a3a10"), WaspM = C("#9c6a1c"), WaspY = C("#e0b040"), Stripe = C("#2a1c0c"),
            Wing = C("#9aa8b0"), WingL = C("#c8d4dc"), Leg = C("#2a1c0c");

        static uint C(string hex) { return 0xff000000u | Convert.ToUInt32(hex.Substring(1), 16); }

        static void Put(uint[] px, int w, int h, int x, int y, uint c)
        {
            if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
        }

        /// <summary>
        /// A flecha cravada, no quadrado <see cref="ArrowSize"/>: a ponta enterrada no centro do quadrado (o pivô), a haste
        /// saindo na direção <paramref name="degrees"/> (0 = para a direita, 90 = para cima), as penas no fim. Desenhada
        /// pixel a pixel no ângulo (sem girar o sprite, que borraria a pixel art).
        /// </summary>
        public static uint[] Arrow(float degrees)
        {
            int n = ArrowSize;
            var px = new uint[n * n];
            double a = degrees * Math.PI / 180.0;
            float dx = (float)Math.Cos(a), dy = -(float)Math.Sin(a);   // linha 0 em cima: y cresce para baixo
            float cx = n / 2f, cy = n / 2f;
            const int len = 9;
            // a haste: do ponto cravado até as penas, com 1 px de luz do lado de cima
            for (int i = 0; i <= len; i++)
            {
                int x = (int)Math.Round(cx + dx * i), y = (int)Math.Round(cy + dy * i);
                Put(px, n, n, x, y, i < 2 ? Outline : Shaft);
                if (i >= 2 && i < len - 2) Put(px, n, n, x, y - 1, ShaftL);
            }
            // as penas: dois traços abertos para trás, verdes (a cor da capa e da magia dela)
            float bx = -dx, by = -dy;              // para trás (para a ponta)
            float nx = -dy, ny = dx;               // de lado
            for (int k = 0; k < 4; k++)
            {
                float t = len - k;
                for (int s = -1; s <= 1; s += 2)
                {
                    float off = 1f + k * 0.45f;
                    int x = (int)Math.Round(cx + dx * t + nx * off * s + bx * 0.5f), y = (int)Math.Round(cy + dy * t + ny * off * s + by * 0.5f);
                    Put(px, n, n, x, y, k == 0 ? FeatherL : (s < 0 ? Feather : FeatherD));
                }
            }
            // o entalhe na ponta de trás, claro
            Put(px, n, n, (int)Math.Round(cx + dx * (len + 1)), (int)Math.Round(cy + dy * (len + 1)), Nock);
            return px;
        }

        /// <summary>A vespa abatida: de costas no chão, as patas para cima, a asa caída, a flecha atravessada (pivô embaixo no meio).</summary>
        public static uint[] DeadWasp()
        {
            int w = WaspW, h = WaspH;
            var px = new uint[w * h];
            // o corpo: dois ovais (tórax e abdômen listrado) deitados
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float ax = (x - 10.5f) / 5.5f, ay = (y - 8.5f) / 3f;     // abdômen
                    float tx = (x - 4.5f) / 2.8f, ty = (y - 8.5f) / 2.5f;    // tórax e cabeça
                    if (ax * ax + ay * ay <= 1f)
                    {
                        uint c = ay < -0.3f ? WaspY : ay < 0.4f ? WaspM : WaspD;
                        if (x % 3 == 0 && x > 6) c = Stripe;
                        px[y * w + x] = c;
                    }
                    else if (tx * tx + ty * ty <= 1f) px[y * w + x] = ty < 0f ? WaspM : WaspD;
                }
            // o olho fechado (um X) e as patas para cima
            Put(px, w, h, 2, 7, Outline); Put(px, w, h, 3, 8, Outline); Put(px, w, h, 3, 7, Outline); Put(px, w, h, 2, 8, Outline);
            for (int i = 0; i < 3; i++)
            {
                int lx = 6 + i * 3;
                Put(px, w, h, lx, 5, Leg); Put(px, w, h, lx + (i % 2 == 0 ? -1 : 1), 4, Leg);
            }
            // a asa caída por cima
            for (int x = 8; x < 15; x++) { Put(px, w, h, x, 3 + (x - 8) / 3, WingL); Put(px, w, h, x, 4 + (x - 8) / 3, Wing); }
            // a flecha atravessada, de cima, cravada no chão
            for (int y = 0; y < h; y++) { Put(px, w, h, 12 - y / 4, y, y < 2 ? FeatherL : Shaft); }
            Put(px, w, h, 11, 0, Feather); Put(px, w, h, 13, 0, Feather); Put(px, w, h, 11, 1, FeatherD); Put(px, w, h, 13, 1, FeatherD);
            // o contorno
            var copy = (uint[])px.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (copy[y * w + x] != 0u) continue;
                    bool near = false;
                    for (int k = 0; k < 4 && !near; k++)
                    {
                        int xx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), yy = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (xx >= 0 && yy >= 0 && xx < w && yy < h && copy[yy * w + xx] != 0u && copy[yy * w + xx] != WingL) near = true;
                    }
                    if (near) px[y * w + x] = Outline;
                }
            return px;
        }
    }
}
