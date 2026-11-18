using System;

namespace SeteVidas.World
{
    /// <summary>
    /// O desenho da Isyathy (refeito em 18/11/2026, pedido do usuário: "mais cartunesca e com o design do jogo, a cor um
    /// pouco mais clara, sombras roxas tomando o corpo, sem ferimentos; presas de cobra gigante ao abrir a boca; adornos
    /// como uma deidade maia"). Feito em código, pixel a pixel, para o corpo ser um tubo contínuo que se dobra
    /// (<see cref="SerpentRaster"/>) e a cabeça girar para onde ela vai.
    /// <list type="bullet">
    /// <item>Corpo: verde de serpente em 3 tons chapados (luz do alto à esquerda), o ventre creme em placas, losangos
    /// grandes no dorso (de tamanhos diferentes, sem grade). As <b>sombras roxas da praga</b> avançam pelo corpo em manchas
    /// de borda clara, como tinta se espalhando. Os <b>anéis de ouro</b> com a pedra de jade no dorso, em distâncias
    /// irregulares: os adornos de deidade.</item>
    /// <item>Cabeça (68 × 56, olhando para a direita, o pescoço em <see cref="NeckX"/>, <see cref="NeckY"/>): o penacho de
    /// plumas de quetzal (verdes, com as pontas de ouro e vermelhas) para trás, a tiara de ouro com o degrau maia e o
    /// medalhão de jade na testa, o brinco de jade na bochecha, o olho grande verde com a fenda e o halo roxo, a sombra da
    /// praga subindo da nuca. Quadros: dormindo, acordada e mordendo (a boca escancarada com as <b>presas grandes</b>).</item>
    /// <item>Cratera: o buraco (atrás do corpo) e a borda de terra da frente (na frente dele).</item>
    /// </list>
    /// C# 5 e sem UnityEngine: a prévia (Tools/EnvironmentSprites/previa_isyathy.ps1) usa o mesmo código. Cores em ARGB.
    /// As linhas dos desenhos vão de cima para baixo (linha 0 = topo).
    /// </summary>
    public static class IsyathyArt
    {
        public const uint Clear = 0u;
        public static readonly uint Outline = C("#14101a");
        // escamas: verde de serpente, mais claro e chapado (escuro, sombra, meio, luz, brilho)
        static readonly uint DK = C("#25402c"), S = C("#3a6a3e"), M = C("#58955a"), L = C("#82bd6c"), HL = C("#b4e08c");
        // ventre
        static readonly uint Belly = C("#d8c88a"), BellyL = C("#f0e2a8"), BellyLine = C("#b09c62");
        // os losangos do dorso
        static readonly uint Dia = C("#2e5a36"), DiaRim = C("#a6d67e");
        // a praga: as sombras roxas que tomam o corpo
        static readonly uint PDark = C("#3e2458"), PMid = C("#5c3680"), PRim = C("#9a68c8");
        static readonly uint G = C("#c47bff"), GG = C("#f0c9ff");
        // os adornos de deidade: ouro e jade
        static readonly uint Gold = C("#f0c040"), GoldL = C("#fff0a0"), GoldD = C("#b07a1c"), Jade = C("#36c8a0"), JadeL = C("#a8fff0"), JadeD = C("#1c7c68");
        // as plumas de quetzal
        static readonly uint FeatherG = C("#3ec070"), FeatherT = C("#1e9a8a"), FeatherD = C("#166a5c"), FeatherR = C("#e04848");
        // boca, presas e olho
        static readonly uint Mouth = C("#3a1430"), Throat = C("#7a2c78"), Gum = C("#c25a8a"), Tooth = C("#fbf6ea"), ToothShade = C("#cfc4aa");
        static readonly uint Iris = C("#7ae05a"), IrisLight = C("#e8ffd0"), IrisDark = C("#3a9a38");
        static readonly uint P = PDark, Q = PMid;
        // terra do Ninho (escura, puxada para o roxo, como o chão da E)
        static readonly uint Dirt0 = C("#120d12"), Dirt1 = C("#22191f"), Dirt2 = C("#352834"), Dirt3 = C("#4e3c4c"), Rock = C("#6b5c6c"), Root = C("#3b2a1e");

        public static uint C(string hex)
        {
            return 0xff000000u | Convert.ToUInt32(hex.Substring(1), 16);
        }

        static uint Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)s * 982451653u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        static float Rand(int x, int y, int s) { return (Hash(x, y, s) % 10000u) / 10000f; }

        /// <summary>Ruído suave de 1 dimensão (0..1), para as sombras da praga ao longo do corpo.</summary>
        static float Smooth1(float x, int seed)
        {
            int i = (int)Math.Floor(x);
            float f = x - i, a = Rand(i, 0, seed), b = Rand(i + 1, 0, seed);
            f = f * f * (3f - 2f * f);
            return a + (b - a) * f;
        }

        // ------------------------------------------------------------ corpo

        /// <summary>
        /// A cor de um ponto do corpo. <paramref name="u"/>: px ao longo do corpo, do pescoço para o rabo (o desenho anda
        /// com ela); <paramref name="v"/>: −1 (barriga) a 1 (dorso), de lado a lado; <paramref name="lit"/>: −1..1, a luz
        /// naquele ponto; <paramref name="radius"/>: o raio ali (px).
        /// </summary>
        public static uint BodyColor(float u, float v, float lit, float radius)
        {
            int ui = (int)Math.Floor(u);
            // a base: 3 tons chapados e o ventre creme em placas
            uint c;
            if (v < -0.58f) c = (ui % 6 + 6) % 6 == 0 ? BellyLine : (lit > 0.15f ? BellyL : Belly);
            else if (lit > 0.7f && v > 0.2f) c = HL;
            else if (lit > 0.3f) c = L;
            else if (lit > -0.25f) c = M;
            else if (lit > -0.65f) c = S;
            else c = DK;

            // os losangos grandes no dorso: de tamanhos e distâncias diferentes (nada de grade)
            int k = (int)Math.Floor(u / 24f);
            for (int dk = -1; dk <= 1 && v > -0.2f; dk++)
            {
                int kk = k + dk;
                uint h = Hash(kk, 0, 31);
                float cu = kk * 24f + 12f + (h % 7u) - 3f, half = 6f + (h >> 4) % 4u;
                float d = Math.Abs(u - cu) / half + Math.Abs(v - 0.62f) / 0.3f;
                if (d < 0.78f) { c = Dia; break; }
                if (d < 1f) { c = DiaRim; break; }
            }

            // as sombras roxas da praga: manchas que avançam pelo corpo (mais no dorso), com a borda clara
            float s = Smooth1(u / 38f, 11) * 0.85f + Smooth1(u / 9f, 13) * 0.18f + 0.28f * v;
            const float thr = 0.62f;
            if (s > thr + 0.05f) c = lit > 0.35f ? PMid : PDark;
            else if (s > thr) c = PRim;
            if (s > thr + 0.12f && Rand(ui, (int)(v * radius), 41) < 0.012f) c = Rand(ui, 1, 42) < 0.4f ? GG : G;   // faíscas

            // os anéis de ouro (adornos de deidade), em distâncias irregulares, com a pedra de jade no dorso
            int b = (int)Math.Floor(u / 46f);
            for (int db = -1; db <= 1; db++)
            {
                int bb = b + db;
                uint h = Hash(bb, 2, 77);
                if (h % 10u < 3u || bb < 0) continue;
                float cu = bb * 46f + 23f + (h % 11u) - 5f, du = Math.Abs(u - cu);
                if (du > 2.6f) continue;
                if (du > 1.9f) return GoldD;
                if (v > 0.38f && v < 0.92f && du < 1.6f)
                {
                    float jv = Math.Abs(v - 0.65f) / 0.25f;
                    if (jv < 1f) return jv < 0.4f && du < 0.6f ? JadeL : (lit > 0f ? Jade : JadeD);
                }
                return lit > 0.45f ? GoldL : (lit > -0.3f ? Gold : GoldD);
            }
            return c;
        }

        // ------------------------------------------------------------ cabeça

        public const int HeadW = 68, HeadH = 66;
        /// <summary>O espaço a mais em cima da cabeça (px): o penacho de plumas.</summary>
        const int OY = 10;
        /// <summary>Onde o pescoço encosta no corpo (px, linha a partir do topo).</summary>
        public const int NeckX = 3, NeckY = 26 + OY;
        /// <summary>A ponta do focinho (px): por onde ela morde.</summary>
        public const int SnoutX = 64, SnoutY = 24 + OY;
        /// <summary>O meio do olho (px).</summary>
        public const int EyeX = 46, EyeY = 16 + OY;
        public const int FrameSleep = 0, FrameAwake = 1, FrameBite = 2;

        static float Top(float x)
        {
            if (x <= 30f) return OY + 14f - 4f * (float)Math.Sin(Math.PI * 0.5 * x / 30f);   // o crânio abaulado
            if (x <= 46f) return OY + 10f + (x - 30f) * 0.06f;                                // a testa até o olho
            return OY + 11f + (float)Math.Pow((x - 46f) / 18f, 1.7f) * 9f;                    // o focinho descendo
        }

        static float Bottom(float x)
        {
            if (x <= 26f) return OY + 38f + 3f * (float)Math.Sin(Math.PI * 0.5 * x / 26f);   // as bochechas
            if (x <= 40f) return OY + 41f - (x - 26f) * 0.15f;
            return OY + 38.9f - (float)Math.Pow((x - 40f) / 24f, 1.3f) * 12f;                  // o queixo afinando
        }

        /// <summary>A linha da boca (y) na coluna x: da dobra (x 22) até o focinho.</summary>
        static float MouthLine(float x) { return OY + 32f - (x - 24f) * 0.2f; }

        const float HingeX = 24f, HingeY = 32f + OY;

        /// <summary>
        /// A cabeça (ARGB, <see cref="HeadW"/> × <see cref="HeadH"/>, linha 0 em cima). <paramref name="glow"/>: o halo roxo
        /// do olho aceso (pisca entre os dois).
        /// </summary>
        public static uint[] Head(int frame, bool glow)
        {
            var closed = new uint[HeadW * HeadH];
            var lower = new bool[HeadW * HeadH];
            PaintHead(closed, lower, frame, glow);
            if (frame != FrameBite) { OutlineAll(closed, HeadW, HeadH); return closed; }

            // mordendo: o queixo desce (gira em volta da dobra), o alto da cabeça sobe um pouco; a boca fica aberta
            var open = new uint[HeadW * HeadH];
            double down = 32.0 * Math.PI / 180.0, up = -10.0 * Math.PI / 180.0;
            for (int y = 0; y < HeadH; y++)
                for (int x = 0; x < HeadW; x++)
                {
                    float px = x + 0.5f - HingeX, py = y + 0.5f - HingeY;
                    float lx = (float)(px * Math.Cos(-down) - py * Math.Sin(-down)) + HingeX;
                    float ly = (float)(px * Math.Sin(-down) + py * Math.Cos(-down)) + HingeY;
                    float ux = (float)(px * Math.Cos(-up) - py * Math.Sin(-up)) + HingeX;
                    float uy = (float)(px * Math.Sin(-up) + py * Math.Cos(-up)) + HingeY;
                    uint c = Clear;
                    int ix = (int)Math.Floor(ux), iy = (int)Math.Floor(uy);
                    if (x < HingeX - 2) { ix = x; iy = y; }   // atrás da dobra não gira (o pescoço)
                    if (ix >= 0 && iy >= 0 && ix < HeadW && iy < HeadH && !lower[iy * HeadW + ix]) c = closed[iy * HeadW + ix];
                    if (c == Clear && x >= HingeX - 2)
                    {
                        int jx = (int)Math.Floor(lx), jy = (int)Math.Floor(ly);
                        if (jx >= 0 && jy >= 0 && jx < HeadW && jy < HeadH && lower[jy * HeadW + jx]) c = closed[jy * HeadW + jx];
                    }
                    if (c == Clear && x < HingeX - 2 && lower[y * HeadW + x]) c = closed[y * HeadW + x];
                    open[y * HeadW + x] = c;
                }
            // o vão da boca: o triângulo entre a dobra, a ponta de cima e a ponta do queixo (abertos)
            float ax = HingeX, ay = HingeY;
            float ux0 = SnoutX - HingeX, uy0 = MouthLine(SnoutX) - HingeY;
            float bx = HingeX + (float)(ux0 * Math.Cos(up) - uy0 * Math.Sin(up)), by = HingeY + (float)(ux0 * Math.Sin(up) + uy0 * Math.Cos(up));
            float lx0 = SnoutX - 3 - HingeX, ly0 = MouthLine(SnoutX - 3) + 1f - HingeY;
            float cx = HingeX + (float)(lx0 * Math.Cos(down) - ly0 * Math.Sin(down)), cy = HingeY + (float)(lx0 * Math.Sin(down) + ly0 * Math.Cos(down));
            for (int y = 0; y < HeadH; y++)
                for (int x = (int)HingeX - 1; x < HeadW; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float d1 = (bx - ax) * (py - ay) - (by - ay) * (px - ax);
                    float d2 = (cx - bx) * (py - by) - (cy - by) * (px - bx);
                    float d3 = (ax - cx) * (py - cy) - (ay - cy) * (px - cx);
                    bool inside = (d1 >= 0 && d2 >= 0 && d3 >= 0) || (d1 <= 0 && d2 <= 0 && d3 <= 0);
                    if (!inside) continue;
                    int i = y * HeadW + x;
                    if (open[i] != Clear) continue;
                    float deep = 1f - (px - HingeX) / (SnoutX - HingeX);
                    open[i] = deep > 0.55f ? Throat : Mouth;
                    if (deep > 0.78f && Rand(x, y, 72) < 0.25f) open[i] = G;              // a garganta roxa acesa
                }
            // as gengivas: uma faixa rosada logo dentro das duas bordas
            for (int k = 1; k < 30; k++)
            {
                float t = k / 30f;
                PutIfMouth(open, (int)(ax + (bx - ax) * t), (int)(ay + (by - ay) * t) + 1, Gum);
                PutIfMouth(open, (int)(ax + (cx - ax) * t), (int)(ay + (cy - ay) * t) - 1, Gum);
            }
            // as presas: duas grandes e curvas no alto (na frente), duas menores embaixo, e dentinhos entre elas
            Fang(open, ax, ay, bx, by, 0.80f, 12, 1);
            Fang(open, ax, ay, bx, by, 0.58f, 7, 1);
            Fang(open, ax, ay, cx, cy, 0.74f, 5, -1);
            Fang(open, ax, ay, cx, cy, 0.52f, 4, -1);
            for (int k = 3; k < 12; k++)
            {
                float t = k / 14f;
                if (Math.Abs(t - 0.58f) < 0.06f || Math.Abs(t - 0.52f) < 0.06f) continue;
                if (k % 2 == 0) PutIfMouth(open, (int)(ax + (bx - ax) * t), (int)(ay + (by - ay) * t) + 2, Tooth);
                else PutIfMouth(open, (int)(ax + (cx - ax) * t), (int)(ay + (cy - ay) * t) - 2, Tooth);
            }
            OutlineAll(open, HeadW, HeadH);
            return open;
        }

        /// <summary>
        /// Uma presa de cobra: nasce na borda (de (x0, y0) a (x1, y1), na fração <paramref name="t"/>) e desce para dentro
        /// da boca (<paramref name="dir"/> 1 = para baixo, −1 = para cima), larga na base, fina e curva na ponta.
        /// </summary>
        static void Fang(uint[] px, float x0, float y0, float x1, float y1, float t, int length, int dir)
        {
            float bx = x0 + (x1 - x0) * t, by = y0 + (y1 - y0) * t;
            // a direção da borda e a perpendicular (para dentro da boca)
            float ex = x1 - x0, ey = y1 - y0, el = (float)Math.Sqrt(ex * ex + ey * ey);
            ex /= el; ey /= el;
            float nx = -ey * dir, ny = ex * dir;
            if (ny * dir < 0) { nx = -nx; ny = -ny; }
            for (int s = 0; s < length; s++)
            {
                float k = s / (float)length;
                float w = 2.2f * (1f - k) + 0.4f;                       // afina até a ponta
                float curve = k * k * 2.2f;                              // curva para trás (para dentro)
                float cxp = bx + nx * s - ex * curve, cyp = by + ny * s - ey * curve;
                for (float o = -w; o <= w; o += 0.5f)
                {
                    int x = (int)Math.Round(cxp + ex * o), y = (int)Math.Round(cyp + ey * o);
                    if (x < 0 || y < 0 || x >= HeadW || y >= HeadH) continue;
                    uint cur = px[y * HeadW + x];
                    if (cur != Mouth && cur != Throat && cur != G && cur != Gum && cur != Tooth && cur != ToothShade) continue;
                    px[y * HeadW + x] = o > w * 0.35f ? ToothShade : Tooth;
                }
            }
        }

        static void PutIfMouth(uint[] px, int x, int y, uint c)
        {
            if (x < 0 || y < 0 || x >= HeadW || y >= HeadH) return;
            uint o = px[y * HeadW + x];
            if (o == Mouth || o == Throat || o == G || o == Gum) px[y * HeadW + x] = c;
        }

        static void Put(uint[] px, int x, int y, uint c)
        {
            if (x >= 0 && y >= 0 && x < HeadW && y < HeadH) px[y * HeadW + x] = c;
        }

        static void PutOn(uint[] px, int x, int y, uint c)
        {
            if (x >= 0 && y >= 0 && x < HeadW && y < HeadH && px[y * HeadW + x] != Clear) px[y * HeadW + x] = c;
        }

        static void PaintHead(uint[] px, bool[] lower, int frame, bool glow)
        {
            // o penacho atrás (desenhado primeiro: a cabeça fica na frente dele)
            Plumes(px);
            for (int x = 0; x < HeadW - 2; x++)
            {
                float top = Top(x), bot = Bottom(x);
                if (bot - top < 1f) continue;
                if (x > SnoutX - 3) { float k = (x - (SnoutX - 3)) / 3f; top += k * k * 4f; bot -= k * k * 4f; if (bot - top < 1f) continue; }
                float mouth = MouthLine(x);
                for (int y = (int)Math.Ceiling(top); y <= (int)Math.Floor(bot); y++)
                {
                    float t = (y - top) / (bot - top);
                    bool jaw = x >= HingeX - 2 && y > mouth;
                    lower[y * HeadW + x] = jaw;
                    uint c;
                    if (jaw) c = t > 0.9f ? BellyLine : (y - mouth < 2f ? Belly : BellyL);
                    else if (t < 0.12f) c = x < 52 ? HL : L;
                    else if (t < 0.34f) c = L;
                    else if (t < 0.62f) c = M;
                    else c = S;
                    if (jaw && x % 5 == 0 && t < 0.88f) c = BellyLine;   // as placas do queixo
                    px[y * HeadW + x] = c;
                }
                if (x >= HingeX - 2 && x < SnoutX) Put(px, x, (int)Math.Round(mouth), Outline);   // a boca fechada
            }
            // a sombra da praga subindo da nuca (com a borda clara), e uma língua dela indo para o olho
            for (int y = OY + 8; y < OY + 44; y++)
                for (int x = 0; x < 30; x++)
                {
                    int yy = y - OY;
                    if (px[y * HeadW + x] == Clear || lower[y * HeadW + x]) continue;
                    float edge = 9f + 4f * (float)Math.Sin(yy * 0.45f) + 2f * (float)Math.Sin(yy * 1.3f);
                    if (yy > 18 && yy < 24) edge += (yy - 18) < 3 ? (yy - 18) * 3f : (24 - yy) * 3f;   // a língua da sombra
                    if (x < edge - 1.5f) px[y * HeadW + x] = yy < 22 ? PMid : PDark;
                    else if (x < edge) px[y * HeadW + x] = PRim;
                }
            // o desenho do dorso: um losango na nuca
            for (int y = OY + 10; y < OY + 20; y++) for (int x = 16; x < 30; x++)
                { float d = Math.Abs(x - 23f) / 6f + Math.Abs(y - OY - 14.5f) / 3.2f; if (d < 0.8f) PutOn(px, x, y, Dia); else if (d < 1f) PutOn(px, x, y, DiaRim); }
            // a tiara de ouro com o degrau maia, da nuca até a testa
            for (int x = 14; x <= 42; x++)
            {
                int y0 = (int)Math.Round(Top(x)) + 1;
                for (int dy = 0; dy < 3; dy++)
                {
                    bool step = ((x / 2) % 4 == 0 && dy == 1) || ((x / 2) % 4 == 2 && dy != 1);   // o degrau (greca)
                    PutOn(px, x, y0 + dy, dy == 0 ? GoldL : (step ? GoldD : Gold));
                }
            }
            // o medalhão de jade na testa, com aro de ouro
            Disc(px, 37, OY + 11, 3.2f, Gold, GoldD); Disc(px, 37, OY + 11, 2f, Jade, JadeD); Put(px, 36, OY + 10, JadeL);
            // o brinco de jade na bochecha (um carretel de ouro com jade e o furo no meio)
            Disc(px, 18, OY + 29, 4.2f, Gold, GoldD); Disc(px, 18, OY + 29, 2.8f, Jade, JadeD); Put(px, 17, OY + 28, JadeL); Put(px, 18, OY + 29, Outline); Put(px, 19, OY + 29, JadeD);
            // as contas de ouro penduradas do brinco
            for (int i = 0; i < 3; i++) { Put(px, 18, OY + 34 + i * 2, i % 2 == 0 ? Gold : GoldD); Put(px, 19, OY + 34 + i * 2, GoldD); }
            // a sobrancelha (a crista sobre o olho) e a narina
            for (int x = EyeX - 5; x <= EyeX + 4; x++) PutOn(px, x, EyeY - 5 + (x > EyeX ? 1 : 0), S);
            Put(px, 59, OY + 18, Outline); Put(px, 60, OY + 18, Outline);

            // o olho
            if (frame == FrameSleep)
            {
                for (int x = EyeX - 4; x <= EyeX + 4; x++) Put(px, x, EyeY + (Math.Abs(x - EyeX) < 3 ? 1 : 0), Outline);
                return;
            }
            if (glow)
            {
                for (int y = EyeY - 8; y <= EyeY + 8; y++)
                    for (int x = EyeX - 11; x <= EyeX + 11; x++)
                    {
                        if (y < 0 || y >= HeadH || x < 0 || x >= HeadW) continue;
                        float dx = (x - EyeX) / 1.25f, dy = y - EyeY;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d < 4.6f || d > 8.2f) continue;
                        if (px[y * HeadW + x] == Clear && d > 6.8f) continue;   // fora da cabeça, só o halo de perto
                        if (Rand(x, y, 81) < (8.2f - d) / 4f * 0.9f) Put(px, x, y, d < 5.6f ? GG : d < 6.8f ? G : Q);
                    }
            }
            // grande e cartunesco: o aro escuro, o verde claro em cima, a fenda e o brilho branco
            for (int y = EyeY - 4; y <= EyeY + 4; y++)
                for (int x = EyeX - 5; x <= EyeX + 5; x++)
                {
                    float dx = (x - EyeX) / 4.8f, dy = (y - EyeY) / 3.8f;
                    float r2 = dx * dx + dy * dy;
                    if (r2 > 1f) continue;
                    uint c = r2 > 0.66f ? Outline : (y < EyeY ? Iris : IrisDark);
                    if (r2 <= 0.66f && (x == EyeX || (x == EyeX + 1 && Math.Abs(y - EyeY) < 2))) c = Outline;   // a fenda
                    px[y * HeadW + x] = c;
                }
            Put(px, EyeX - 2, EyeY - 2, IrisLight); Put(px, EyeX - 3, EyeY - 1, IrisLight); Put(px, EyeX - 2, EyeY - 1, IrisLight);
            Put(px, EyeX + 2, EyeY + 1, glow ? GG : Iris);
        }

        /// <summary>
        /// O penacho de plumas de quetzal saindo da nuca para trás e para cima: compridas, verdes e turquesa, com as pontas
        /// de ouro e vermelhas (o adorno de deidade).
        /// </summary>
        static void Plumes(uint[] px)
        {
            // (base x, base y, ângulo em graus para trás/cima, comprimento, cor)
            int[][] plumes =
            {
                new[] { 28, OY + 10, 96, 14, 0 }, new[] { 25, OY + 10, 110, 18, 1 }, new[] { 21, OY + 11, 124, 19, 0 },
                new[] { 17, OY + 12, 138, 18, 1 }, new[] { 13, OY + 13, 152, 15, 0 }, new[] { 10, OY + 14, 158, 6, 1 },
            };
            foreach (var p in plumes)
            {
                double a = p[2] * Math.PI / 180.0;
                float dx = (float)Math.Cos(a), dy = (float)-Math.Sin(a);
                for (int s = 0; s <= p[3]; s++)
                {
                    float k = s / (float)p[3];
                    float w = 1.6f + 1.4f * (float)Math.Sin(k * Math.PI);   // larga no meio da pluma
                    float bend = k * k * 3f;                                // a pluma cai para trás na ponta
                    float cxp = p[0] + dx * s - bend * 0.6f, cyp = p[1] + dy * s + bend * 0.4f;
                    for (float o = -w; o <= w; o += 0.5f)
                    {
                        int x = (int)Math.Round(cxp - dy * o), y = (int)Math.Round(cyp + dx * o);
                        if (x < 0 || y < 0 || x >= HeadW || y >= HeadH) continue;
                        uint c = k > 0.82f ? (p[4] == 0 ? Gold : FeatherR) : (Math.Abs(o) < 0.6f ? FeatherD : (p[4] == 0 ? FeatherG : FeatherT));
                        px[y * HeadW + x] = c;
                    }
                }
            }
        }

        static void Disc(uint[] px, int cx, int cy, float r, uint inside, uint rim)
        {
            for (int y = cy - (int)r - 1; y <= cy + (int)r + 1; y++)
                for (int x = cx - (int)r - 1; x <= cx + (int)r + 1; x++)
                {
                    float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d > r) continue;
                    Put(px, x, y, d > r - 1f ? rim : inside);
                }
        }

        /// <summary>Contorno de 1 px em volta de tudo o que tem cor (vizinhança de 4).</summary>
        public static void OutlineAll(uint[] px, int w, int h)
        {
            var add = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x] != Clear) continue;
                    if ((x > 0 && px[y * w + x - 1] != Clear) || (x < w - 1 && px[y * w + x + 1] != Clear) ||
                        (y > 0 && px[(y - 1) * w + x] != Clear) || (y < h - 1 && px[(y + 1) * w + x] != Clear)) add[y * w + x] = true;
                }
            for (int i = 0; i < add.Length; i++) if (add[i]) px[i] = Outline;
        }

        // ------------------------------------------------------------ cratera

        public const int CraterW = 84, CraterBackH = 28, CraterLipH = 14;
        /// <summary>Linha (a partir do topo) do desenho de trás que fica na altura do chão.</summary>
        public const int CraterBackSurface = 18;
        /// <summary>Linha (a partir do topo) da borda da frente que fica na altura do chão.</summary>
        public const int CraterLipSurface = 6;

        /// <summary>O buraco: a boca escura (oval) com a parede de trás e a terra jogada para cima e para os lados.</summary>
        public static uint[] CraterBack()
        {
            int w = CraterW, h = CraterBackH;
            var px = new uint[w * h];
            float cx = w / 2f, cy = CraterBackSurface;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - cx) / 31f, dy = (y + 0.5f - cy) / 7.5f;
                    float r2 = dx * dx + dy * dy;
                    float n = Rand(x, y, 91);
                    if (r2 < 0.75f) px[y * w + x] = r2 < 0.35f ? Dirt0 : (dy < 0f ? Dirt2 : Dirt1);         // o fundo e a parede de trás
                    else if (r2 < 1f + n * 0.25f && dy < 0.6f) px[y * w + x] = dy < -0.4f ? Dirt3 : Dirt2;    // a borda de trás
                    // a terra jogada: um monte irregular atrás da boca
                    float mound = 9f * (1f - dx * dx * 0.75f) + (n - 0.5f) * 3f + 2f * (float)Math.Sin(x * 0.45f);
                    if (r2 >= 0.75f && y < cy && cy - y < mound && Math.Abs(dx) < 1.15f && px[y * w + x] == Clear)
                        px[y * w + x] = cy - y > mound - 1.2f ? Dirt3 : (n < 0.15f ? Rock : Dirt2);
                }
            // raízes arrebentadas e a praga na terra
            for (int i = 0; i < 6; i++)
            {
                int x0 = 10 + (int)(Hash(i, 0, 93) % 64u), y0 = (int)cy - 2;
                for (int s = 0; s < 4; s++) { int x = x0 + s * (i % 2 == 0 ? 1 : -1), y = y0 - s / 2; if (x >= 0 && x < w && y >= 0) px[y * w + x] = Root; }
            }
            for (int i = 0; i < 12; i++)
            {
                int x = 8 + (int)(Hash(i, 2, 95) % 68u), y = (int)cy - 1 - (int)(Hash(i, 3, 95) % 7u);
                if (px[y * w + x] != Clear) px[y * w + x] = i % 3 == 0 ? G : P;
            }
            OutlineAll(px, w, h);
            return px;
        }

        /// <summary>A borda de terra da frente: cobre a parte de baixo da boca (o corpo sai de trás dela).</summary>
        public static uint[] CraterLip()
        {
            int w = CraterW, h = CraterLipH;
            var px = new uint[w * h];
            float cx = w / 2f, surf = CraterLipSurface;
            for (int x = 1; x < w - 1; x++)
            {
                float dx = (x + 0.5f - cx) / 36f;
                if (Math.Abs(dx) > 1f) continue;
                // um arco de torrões: mais alto nas pontas, cobrindo a frente da boca
                float topY = surf - 1.5f - 2.5f * (1f - (float)Math.Cos(dx * Math.PI * 0.5)) + (Rand(x / 3, 0, 97) - 0.5f) * 2.5f;
                float botY = surf + 3f + (1f - Math.Abs(dx)) * 2.5f;
                for (int y = (int)Math.Ceiling(topY); y <= (int)Math.Floor(botY) && y < h; y++)
                {
                    if (y < 0) continue;
                    float t = (y - topY) / Math.Max(1f, botY - topY);
                    float n = Rand(x, y, 98);
                    uint c = t < 0.2f ? Dirt3 : t < 0.55f ? Dirt2 : Dirt1;
                    if (n < 0.08f) c = Rock;
                    if (t < 0.25f && n > 0.93f) c = P;
                    px[y * w + x] = c;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int x0 = 12 + (int)(Hash(i, 4, 99) % 58u);
                for (int s = 0; s < 3; s++) { int y = (int)surf - 2 - s; if (y >= 0) px[y * w + x0 + s] = Root; }
            }
            OutlineAll(px, w, h);
            return px;
        }
    }
}
