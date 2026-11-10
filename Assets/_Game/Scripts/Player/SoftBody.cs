using System;

// Cauda e capa do Lion: física de corrente com mola e desenho pixel a pixel.
// C# puro (sem UnityEngine) e compatível com C# 5: o mesmo arquivo gera a prévia em Tools/LionSprites.
// Unidades: pixels de arte (1 px = 1/16 de unidade Unity), y para cima.
namespace SeteVidas.Player
{
    /// <summary>Estado do corpo que muda a pose da cauda e da capa.</summary>
    public enum BodyMood { Idle, Run, Jump, Sneak, Fall, Hang, Hurt, Lying }

    /// <summary>Índices da paleta usados no buffer (0 = vazio).</summary>
    public static class SoftPal
    {
        public const byte Empty = 0, Outline = 1, FurLight = 2, Fur = 3, FurDark = 4,
                          ClothLight = 5, Cloth = 6, ClothDark = 7, Trim = 8, TrimDark = 9;
        public const int Count = 10;
    }

    /// <summary>
    /// Corrente de nós presa numa âncora. Cada nó persegue, com mola amortecida, a posição que a pose pede
    /// a partir do nó anterior: a ponta atrasa e chicoteia quando o corpo muda de direção.
    /// </summary>
    public sealed class SoftChain
    {
        public readonly float[] X, Y;
        readonly float[] _vx, _vy, _len;
        bool _init;

        public float Stiffness = 420f;
        public float Damping = 15f;

        public SoftChain(float[] segmentLengths)
        {
            int n = segmentLengths.Length + 1;
            X = new float[n]; Y = new float[n]; _vx = new float[n]; _vy = new float[n];
            _len = (float[])segmentLengths.Clone();
        }

        public int Count { get { return X.Length; } }
        public float SegmentLength(int i) { return _len[i]; }
        public void Reset() { _init = false; }

        /// <summary><paramref name="angles"/>: direção de cada segmento no mundo (radianos, 0 = +x, y para cima).</summary>
        public void Step(float ax, float ay, float[] angles, float dt)
        {
            int n = X.Length;
            float jump = (X[0] - ax) * (X[0] - ax) + (Y[0] - ay) * (Y[0] - ay);
            if (!_init || jump > 24f * 24f)
            {
                // primeira vez (ou teleporte): já nasce na pose, parado
                X[0] = ax; Y[0] = ay;
                for (int i = 1; i < n; i++)
                {
                    X[i] = X[i - 1] + (float)Math.Cos(angles[i - 1]) * _len[i - 1];
                    Y[i] = Y[i - 1] + (float)Math.Sin(angles[i - 1]) * _len[i - 1];
                    _vx[i] = 0f; _vy[i] = 0f;
                }
                _init = true;
                return;
            }
            X[0] = ax; Y[0] = ay;
            if (dt <= 0f) return; // hitstop: congela junto com o jogo
            if (dt > 0.05f) dt = 0.05f;
            const int Sub = 3;
            float h = dt / Sub;
            float damp = (float)Math.Exp(-Damping * h);
            for (int s = 0; s < Sub; s++)
            {
                for (int i = 1; i < n; i++)
                {
                    float len = _len[i - 1];
                    float tx = X[i - 1] + (float)Math.Cos(angles[i - 1]) * len;
                    float ty = Y[i - 1] + (float)Math.Sin(angles[i - 1]) * len;
                    _vx[i] = (_vx[i] + (tx - X[i]) * Stiffness * h) * damp;
                    _vy[i] = (_vy[i] + (ty - Y[i]) * Stiffness * h) * damp;
                    X[i] += _vx[i] * h;
                    Y[i] += _vy[i] * h;
                    // o pano e o pelo esticam pouco: segura o comprimento do segmento
                    float dx = X[i] - X[i - 1], dy = Y[i] - Y[i - 1];
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float min = len * 0.75f, max = len * 1.15f;
                    if (d > 0.0001f && (d > max || d < min))
                    {
                        float k = (d > max ? max : min) / d;
                        X[i] = X[i - 1] + dx * k;
                        Y[i] = Y[i - 1] + dy * k;
                    }
                }
            }
        }
    }

    /// <summary>Desenho em pixel (buffer de índices de paleta, linha 0 embaixo).</summary>
    public static class SoftRaster
    {
        public static void Clear(byte[] buf) { Array.Clear(buf, 0, buf.Length); }

        /// <summary>
        /// Cauda felpuda: cápsulas com raio variável ao longo da corrente. Lado de cima mais claro, de baixo
        /// mais escuro, base escura (sai de baixo da capa) e ponta clara (pelo de persa).
        /// </summary>
        public static void Tail(byte[] buf, int size, float ox, float oy, float[] xs, float[] ys, float[] radius, float bristle)
        {
            int n = xs.Length;
            float total = 0f;
            float[] acc = new float[n];
            for (int i = 1; i < n; i++) { total += Dist(xs[i - 1], ys[i - 1], xs[i], ys[i]); acc[i] = total; }
            if (total < 0.01f) total = 0.01f;

            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float cx = ox + px + 0.5f, cy = oy + py + 0.5f;
                    float best = float.MaxValue, bestS = 0f, bestOff = 0f, bestR = 1f;
                    for (int j = 0; j < n - 1; j++)
                    {
                        float ax = xs[j], ay = ys[j], dx = xs[j + 1] - ax, dy = ys[j + 1] - ay;
                        float l2 = dx * dx + dy * dy;
                        float t = l2 > 0f ? ((cx - ax) * dx + (cy - ay) * dy) / l2 : 0f;
                        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                        float qx = ax + dx * t, qy = ay + dy * t;
                        float d = Dist(cx, cy, qx, qy);
                        float r = (radius[j] + (radius[j + 1] - radius[j]) * t) * bristle;
                        float score = d - r;
                        if (score < best)
                        {
                            best = score; bestR = r;
                            bestS = (acc[j] + (acc[j + 1] - acc[j]) * t) / total;
                            // deslocamento para o lado "de cima" do segmento
                            float l = (float)Math.Sqrt(l2); if (l < 0.0001f) l = 1f;
                            float nx = -dy / l, ny = dx / l;
                            if (ny < 0f || (ny == 0f && nx < 0f)) { nx = -nx; ny = -ny; }
                            bestOff = (cx - qx) * nx + (cy - qy) * ny;
                        }
                    }
                    if (best > 0f) continue;
                    byte c;
                    if (bestS > 0.78f) c = bestOff < -bestR * 0.5f ? SoftPal.Fur : SoftPal.FurLight; // ponta clara
                    else if (bestS < 0.18f) c = SoftPal.FurDark;                                       // base, à sombra da capa
                    else if (bestOff > bestR * 0.35f) c = SoftPal.FurLight;
                    else if (bestOff < -bestR * 0.4f) c = SoftPal.FurDark;
                    else c = SoftPal.Fur;
                    buf[py * size + px] = c;
                }
        }

        /// <summary>
        /// Aba da capa: faixa que alarga do ombro até a barra. Barra reta na cor da classe, borda de fora e uma dobra
        /// mais escuras. <paramref name="facing"/>: +1 olhando para a direita (o corpo fica do lado +x da capa).
        /// </summary>
        public static void Cape(byte[] buf, int size, float ox, float oy, float[] xs, float[] ys, float[] halfWidth, int facing)
        {
            int n = xs.Length;
            float total = 0f;
            float[] acc = new float[n];
            for (int i = 1; i < n; i++) { total += Dist(xs[i - 1], ys[i - 1], xs[i], ys[i]); acc[i] = total; }
            if (total < 0.01f) total = 0.01f;

            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float cx = ox + px + 0.5f, cy = oy + py + 0.5f;
                    bool inside = false;
                    float bestRatio = float.MaxValue, alongPx = 0f, side = 0f, hw = 1f;
                    for (int j = 0; j < n - 1; j++)
                    {
                        float ax = xs[j], ay = ys[j], dx = xs[j + 1] - ax, dy = ys[j + 1] - ay;
                        float l2 = dx * dx + dy * dy;
                        if (l2 < 0.0001f) continue;
                        float l = (float)Math.Sqrt(l2);
                        float t = ((cx - ax) * dx + (cy - ay) * dy) / l2;
                        bool last = j == n - 2;
                        if (t > 1f && last) continue;          // barra reta, sem ponta redonda
                        float tc = t < 0f ? 0f : (t > 1f ? 1f : t);
                        float qx = ax + dx * tc, qy = ay + dy * tc;
                        float d = Dist(cx, cy, qx, qy);
                        float w = halfWidth[j] + (halfWidth[j + 1] - halfWidth[j]) * tc;
                        if (d > w) continue;
                        float ratio = d / w;
                        if (ratio < bestRatio)
                        {
                            bestRatio = ratio; inside = true; hw = w;
                            alongPx = acc[j] + (acc[j + 1] - acc[j]) * tc;
                            // > 0 = lado do corpo; < 0 = lado de fora
                            side = ((dx / l) * (cy - ay) - (dy / l) * (cx - ax)) * facing;
                        }
                    }
                    if (!inside) continue;
                    float fromHem = total - alongPx;
                    byte c;
                    if (fromHem < 1.1f) c = side < -hw * 0.45f ? SoftPal.TrimDark : SoftPal.Trim;        // barra
                    else if (side < -(hw - 1.0f)) c = SoftPal.ClothDark;                                 // borda de fora
                    else if (alongPx > total * 0.4f && Math.Abs(side - hw * 0.25f) < 0.5f) c = SoftPal.ClothDark; // dobra
                    else if (alongPx < 1.2f && side > 0f) c = SoftPal.ClothLight;                        // luz no ombro
                    else c = SoftPal.Cloth;
                    buf[py * size + px] = c;
                }
        }

        /// <summary>Contorno externo de 1 px (vizinhança de 4), igual ao dos sprites.</summary>
        public static void Outline(byte[] buf, int size)
        {
            byte[] src = (byte[])buf.Clone();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (src[y * size + x] != SoftPal.Empty) continue;
                    if ((x > 0 && src[y * size + x - 1] != 0) || (x < size - 1 && src[y * size + x + 1] != 0) ||
                        (y > 0 && src[(y - 1) * size + x] != 0) || (y < size - 1 && src[(y + 1) * size + x] != 0))
                        buf[y * size + x] = SoftPal.Outline;
                }
        }

        static float Dist(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx, dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>Poses da cauda e da capa por estado (ângulos e ritmo). Compartilhado com a prévia.</summary>
    public static class SoftPoses
    {
        const float Deg = (float)(Math.PI / 180.0);

        public static readonly float[] TailSegments = { 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f };
        public static readonly float[] TailRadius = { 0.8f, 1.1f, 1.35f, 1.55f, 1.65f, 1.55f, 1.15f };
        public static readonly float[] CapeSegments = { 2.6f, 2.6f, 2.6f };
        public static readonly float[] CapeHalfWidth = { 1.3f, 2.0f, 2.6f, 3.1f };

        /// <summary>
        /// Cauda: ângulo de cada segmento em graus "do gato" (0 = para trás, 90 = para cima), mais curva para
        /// cima na ponta (ponto de interrogação) e uma onda que corre da base à ponta.
        /// </summary>
        public static void Tail(BodyMood mood, int facing, float time, float speed01, float[] outAngles)
        {
            float baseA, curl, amp, freq, lag;
            switch (mood)
            {
                case BodyMood.Run:   baseA = 2f - 8f * speed01; curl = 6f; amp = 6f; freq = 10f; lag = 0.9f; break;
                case BodyMood.Jump:  baseA = -8f; curl = 10f; amp = 4f; freq = 6f; lag = 0.7f; break;
                case BodyMood.Sneak: baseA = -4f; curl = 15f; amp = 5f; freq = 2.2f; lag = 0.8f; break;  // baixa, ponta em gancho
                case BodyMood.Fall:  baseA = 40f; curl = 8f; amp = 9f; freq = 12f; lag = 1.0f; break;
                case BodyMood.Hang:  baseA = -82f; curl = 7f; amp = 6f; freq = 1.6f; lag = 0.6f; break;
                case BodyMood.Hurt:  baseA = 70f; curl = 0f; amp = 3f; freq = 20f; lag = 0.4f; break;   // eriçada, reta
                case BodyMood.Lying: baseA = -14f; curl = 5f; amp = 6f; freq = 2.4f; lag = 0.9f; break;  // caído: deitada no chão, ponta tremendo
                default:             baseA = -6f; curl = 17f; amp = 8f; freq = 2.3f; lag = 0.75f; break;
            }
            for (int i = 0; i < outAngles.Length; i++)
            {
                float a = baseA + curl * i + amp * (float)Math.Sin(time * freq - i * lag) * (0.4f + 0.6f * i / outAngles.Length);
                outAngles[i] = FromCat(a, facing);
            }
        }

        /// <summary>
        /// Capa: ângulo de cada segmento em graus a partir de "para baixo" (0) rumo às costas (90 = horizontal
        /// para trás, &gt; 90 = sobe). Correr e cair levantam a capa; a barra tremula com o vento.
        /// </summary>
        public static void Cape(BodyMood mood, int facing, float time, float speed01, float[] outAngles)
        {
            float baseA, curl, amp, freq, lag;
            switch (mood)
            {
                case BodyMood.Run:   baseA = 30f + 45f * speed01; curl = 6f; amp = 5f + 5f * speed01; freq = 9f; lag = 1.1f; break;
                case BodyMood.Jump:  baseA = 22f; curl = 5f; amp = 3f; freq = 7f; lag = 1f; break;
                case BodyMood.Sneak: baseA = 72f; curl = 6f; amp = 3f; freq = 3f; lag = 1f; break;   // deitada nas costas
                case BodyMood.Fall:  baseA = 118f; curl = 10f; amp = 9f; freq = 13f; lag = 1.2f; break; // sobe com o vento
                case BodyMood.Hang:  baseA = 12f; curl = 3f; amp = 3f; freq = 1.4f; lag = 0.8f; break;
                case BodyMood.Hurt:  baseA = 85f; curl = 12f; amp = 6f; freq = 18f; lag = 1f; break;
                case BodyMood.Lying: baseA = 80f; curl = 4f; amp = 2f; freq = 2f; lag = 1f; break;
                default:             baseA = 16f; curl = 5f; amp = 3f; freq = 1.7f; lag = 0.9f; break;     // respira
            }
            for (int i = 0; i < outAngles.Length; i++)
            {
                float a = baseA + curl * i + amp * (float)Math.Sin(time * freq - i * lag) * (0.3f + 0.7f * (i + 1f) / outAngles.Length);
                // 0 = baixo; gira para as costas (-facing em x)
                float r = a * Deg;
                float dx = -facing * (float)Math.Sin(r), dy = -(float)Math.Cos(r);
                outAngles[i] = (float)Math.Atan2(dy, dx);
            }
        }

        static float FromCat(float deg, int facing)
        {
            float r = deg * Deg;
            return (float)Math.Atan2(Math.Sin(r), -facing * Math.Cos(r));
        }
    }
}
