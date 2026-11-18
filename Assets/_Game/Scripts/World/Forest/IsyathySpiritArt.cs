using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O espírito da Isyathy (18/11/2026, pedido do usuário: "um asset novo para a forma de espírito, que não pareça uma
    /// cabeça flutuante"): a serpente inteira, translúcida e azul-clara, subindo em S. O rabo se desfaz em fiapos de luz
    /// embaixo; a cabeça, no alto, olha para a direita, calma, com as plumas de quetzal e a tiara (agora de luz, não de
    /// ouro) da deidade. Desenhado em código (o mesmo traço cartunesco do corpo: contorno, luz do alto à esquerda, os
    /// losangos do dorso). O retrato do diálogo é um recorte da cabeça (<see cref="Portrait"/>).
    /// </summary>
    public static class IsyathySpiritArt
    {
        public const int W = 80, H = 112;

        static readonly Color32 Rim = new Color32(0xf0, 0xfc, 0xff, 0xff);    // o contorno de luz
        static readonly Color32 Light = new Color32(0xc4, 0xf0, 0xff, 0xff);
        static readonly Color32 Mid = new Color32(0x84, 0xd4, 0xf4, 0xff);
        static readonly Color32 Deep = new Color32(0x4a, 0x96, 0xd0, 0xff);
        static readonly Color32 Belly = new Color32(0xe4, 0xfa, 0xff, 0xff);
        static readonly Color32 Plume = new Color32(0x9c, 0xf4, 0xdc, 0xff);
        static readonly Color32 PlumeTip = new Color32(0xe0, 0xff, 0xf4, 0xff);
        static readonly Color32 Tiara = new Color32(0xff, 0xf4, 0xc4, 0xff);
        static readonly Color32 Eye = new Color32(0x1e, 0x4a, 0x7a, 0xff);

        /// <summary>O centro do trilho (px, de baixo para cima) em <paramref name="t"/> 0 (o rabo) .. 1 (o pescoço).</summary>
        public static Vector2 Spine(float t) => new Vector2(40f + 16f * Mathf.Sin(t * 2.2f * Mathf.PI + 0.3f) * (0.45f + 0.55f * t), 8f + t * 80f);

        /// <summary>A grossura (px de raio): fio no rabo, grosso no meio, um pouco mais fino no pescoço.</summary>
        public static float Radius(float t) => t < 0.85f ? 1f + 7f * Mathf.Pow(t / 0.85f, 0.6f) : 8f - 1.5f * (t - 0.85f) / 0.15f;

        /// <summary>A figura inteira (W × H, linha 0 embaixo). Transparência: o corpo é um pouco translúcido, o contorno não.</summary>
        public static Color32[] Pixels()
        {
            var px = new Color32[W * H];
            // 1. o corpo, do rabo ao pescoço (o de cima cobre o de baixo)
            float along = 0f;
            Vector2 prev = Spine(0f);
            for (float t = 0f; t <= 1f; t += 0.0025f)
            {
                Vector2 c = Spine(t);
                along += Vector2.Distance(prev, c);
                prev = c;
                Vector2 dir = (Spine(Mathf.Min(1f, t + 0.01f)) - Spine(Mathf.Max(0f, t - 0.01f))).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x);
                float r = Radius(t);
                byte alpha = (byte)Mathf.Lerp(90f, 205f, Mathf.Clamp01(t * 2.2f));   // o rabo some aos poucos
                for (int y = Mathf.FloorToInt(c.y - r); y <= Mathf.CeilToInt(c.y + r); y++)
                    for (int x = Mathf.FloorToInt(c.x - r); x <= Mathf.CeilToInt(c.x + r); x++)
                    {
                        Vector2 d = new Vector2(x + 0.5f - c.x, y + 0.5f - c.y);
                        if (d.magnitude > r) continue;
                        Vector2 n = d / Mathf.Max(r, 0.01f);
                        float shade = Vector2.Dot(n, new Vector2(-0.55f, 0.65f));
                        float s = Vector2.Dot(n, side);                                // de um lado a barriga clara
                        Color32 col = s < -0.45f ? Belly : shade > 0.35f ? Light : shade < -0.35f ? Deep : Mid;
                        // os losangos do dorso (como no corpo de carne), a cada 9 px do trilho
                        float k = Mathf.Repeat(along, 9f);
                        if (r > 3f && s > 0.05f && s < 0.55f && Mathf.Abs(k - 4.5f) + Mathf.Abs(s - 0.3f) * 6f < 1.6f) col = Deep;
                        Set(px, x, y, new Color32(col.r, col.g, col.b, alpha));
                    }
            }
            // 2. a cabeça, olhando para a direita (o pescoço no alto do trilho)
            Vector2 neck = Spine(1f);
            Vector2 head = neck + new Vector2(6f, 3f);
            Fill(px, head, 12f, 8.5f, Mid);
            Fill(px, head + new Vector2(10f, -1.5f), 7.5f, 5.5f, Mid);                  // o focinho
            Fill(px, head + new Vector2(-1f, 3f), 8.5f, 4f, Light);                     // a luz no alto da cabeça
            Fill(px, head + new Vector2(7f, -4.5f), 10f, 2.4f, Belly);                  // o queixo claro
            for (int x = -2; x <= 16; x++) Set(px, (int)(head.x + x), (int)(head.y - 2f - x * 0.08f), Deep);   // a boca fechada, calma
            Fill(px, head + new Vector2(16f, 0.5f), 0.8f, 0.8f, Deep);                  // a narina
            // a tiara de luz (o degrau maia) e o medalhão
            for (int x = -6; x <= 6; x++)
            {
                int yy = (int)(head.y + 7.5f) + ((x + 5) % 4 < 2 ? 0 : 1);
                Set(px, (int)head.x + x, yy, Tiara);
                Set(px, (int)head.x + x, yy - 1, Tiara);
            }
            Fill(px, head + new Vector2(0f, 10f), 1.8f, 1.8f, Rim);
            // o olho: grande, calmo (pálpebra meio baixa)
            Vector2 eye = head + new Vector2(6f, 3f);
            Fill(px, eye, 3.2f, 2.6f, Rim);
            Fill(px, eye + new Vector2(0.8f, -0.4f), 1.5f, 1.6f, Eye);
            Set(px, (int)eye.x + 1, (int)eye.y + 1, Rim);                               // o brilho no olho
            for (int x = -4; x <= 3; x++) Set(px, (int)eye.x + x, (int)(eye.y + 2.6f), Deep);   // a pálpebra
            // 3. as plumas de quetzal, abertas em leque atrás da cabeça
            for (int i = 0; i < 6; i++)
            {
                float ang = (112f + i * 15f) * Mathf.Deg2Rad, len = 13f + (i % 3) * 4f;
                Vector2 root = head + new Vector2(-5f, 5f);
                for (float d = 0f; d < len; d += 0.4f)
                {
                    Vector2 p = root + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d + new Vector2(0f, Mathf.Sin(d * 0.25f) * 0.8f);
                    float rr = Mathf.Lerp(1.8f, 0.6f, d / len);
                    Fill(px, p, rr, rr, d > len * 0.7f ? PlumeTip : Plume, 190);
                }
            }
            // 4. os fiapos de luz soltos embaixo do rabo (ela se desfazendo em floresta)
            Vector2 tail = Spine(0f);
            Fill(px, tail + new Vector2(-3f, -4f), 1.2f, 1.2f, Light, 95);
            Fill(px, tail + new Vector2(2f, -6f), 0.9f, 0.9f, Light, 90);
            Fill(px, tail + new Vector2(-1f, -7.5f), 0.7f, 0.7f, Rim, 70);
            // 5. o contorno de luz em volta de tudo (o traço do jogo, aqui claro)
            var outline = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (px[y * W + x].a != 0) continue;
                    bool near = false;
                    for (int k = 0; k < 4 && !near; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && nx < W && ny >= 0 && ny < H && px[ny * W + nx].a > 100) near = true;
                    }
                    outline[y * W + x] = near;
                }
            for (int i = 0; i < px.Length; i++) if (outline[i]) px[i] = new Color32(Rim.r, Rim.g, Rim.b, 230);
            // 6. o halo: uma luz fraca em volta (2 px), ela brilha de leve no escuro da floresta
            var halo = new Color32[px.Length];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (px[y * W + x].a != 0) continue;
                    int best = 9;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= W || ny < 0 || ny >= H || px[ny * W + nx].a == 0) continue;
                            best = Mathf.Min(best, dx * dx + dy * dy);
                        }
                    if (best <= 2) halo[y * W + x] = new Color32(Light.r, Light.g, Light.b, 70);
                    else if (best <= 8) halo[y * W + x] = new Color32(Light.r, Light.g, Light.b, 35);
                }
            for (int i = 0; i < px.Length; i++) if (px[i].a == 0 && halo[i].a != 0) px[i] = halo[i];
            return px;
        }

        /// <summary>Onde fica a cabeça na figura (px): o centro dela (para o retrato e para os balões).</summary>
        public static Vector2 HeadCenter => Spine(1f) + new Vector2(11f, 6f);

        /// <summary>
        /// O retrato do diálogo (<paramref name="size"/>², olhando para a esquerda, como os outros retratos do lado direito):
        /// o recorte da cabeça com as plumas, mais opaco para ler bem na caixa.
        /// </summary>
        public static Color32[] Portrait(int size = 48)
        {
            var src = Pixels();
            var dst = new Color32[size * size];
            Vector2 c = HeadCenter;
            int ox = Mathf.RoundToInt(c.x) - 29, oy = Mathf.RoundToInt(c.y) - size / 2 - 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int sx = ox + (size - 1 - x), sy = oy + y;   // espelhado: olha para a esquerda
                    if (sx < 0 || sx >= W || sy < 0 || sy >= H) continue;
                    var p = src[sy * W + sx];
                    if (p.a == 0) continue;
                    dst[y * size + x] = new Color32(p.r, p.g, p.b, (byte)Mathf.Max(p.a, (byte)235));
                }
            return dst;
        }

        static void Set(Color32[] px, int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            px[y * W + x] = c;
        }

        static void Fill(Color32[] px, Vector2 c, float rx, float ry, Color32 col, byte alpha = 215)
        {
            for (int y = Mathf.FloorToInt(c.y - ry); y <= Mathf.CeilToInt(c.y + ry); y++)
                for (int x = Mathf.FloorToInt(c.x - rx); x <= Mathf.CeilToInt(c.x + rx); x++)
                {
                    float dx = (x + 0.5f - c.x) / Mathf.Max(rx, 0.01f), dy = (y + 0.5f - c.y) / Mathf.Max(ry, 0.01f);
                    if (dx * dx + dy * dy > 1f) continue;
                    Set(px, x, y, new Color32(col.r, col.g, col.b, alpha));
                }
        }
    }
}
