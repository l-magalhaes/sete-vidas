using System;

namespace SeteVidas.World
{
    /// <summary>Uma fatia do corpo (px do mundo): o meio, a direção para a cabeça, o raio e a distância até o pescoço.</summary>
    public struct SerpentDisc
    {
        public float X, Y, DirX, DirY, R, U;
    }

    /// <summary>
    /// Monta o desenho da Isyathy num quadro de pixels (ARGB, linha 0 embaixo, como a textura do Unity): o corpo é um tubo
    /// contínuo de fatias (do rabo para a cabeça, a da frente cobre a de trás), pintado por <see cref="IsyathyArt.BodyColor"/>
    /// com as coordenadas do próprio corpo (o desenho anda com ela); a cabeça gira para onde ela vai; o contorno é só o
    /// da silhueta. Embaixo do chão, na boca do buraco (<see cref="SetClip"/>), nada aparece: ela entra na terra.
    /// <para>Duas passadas: a 1ª só decide qual fatia fica em cada pixel (barato); a 2ª pinta cada pixel uma vez.</para>
    /// C# 5 e sem UnityEngine (a prévia usa o mesmo código).
    /// </summary>
    public sealed class SerpentRaster
    {
        public readonly int W, H;
        /// <summary>O quadro (ARGB, W × H, linha 0 embaixo).</summary>
        public readonly uint[] Px;
        /// <summary>O estágio da corrupção do corpo (<see cref="IsyathyArt.BlightThreshold"/>).</summary>
        public int Stage;
        readonly float[] _u, _v, _lit, _r;
        readonly bool[] _body;
        int _x0, _y0, _x1 = -1, _y1 = -1;   // a área suja (para limpar só ela)
        float _ox, _oy;
        // as bocas de buraco: abaixo do chão, ali dentro, nada aparece (várias: a varredura entra de um lado e sai do outro)
        readonly System.Collections.Generic.List<float[]> _clips = new System.Collections.Generic.List<float[]>();
        // luz da lua: do alto à esquerda
        const float Lx = -0.45f, Ly = 0.89f;

        public SerpentRaster(int w, int h)
        {
            W = w; H = h;
            Px = new uint[w * h];
            _u = new float[w * h]; _v = new float[w * h]; _lit = new float[w * h]; _r = new float[w * h];
            _body = new bool[w * h];
        }

        /// <summary>Começa um quadro novo com o canto de baixo à esquerda em (<paramref name="originX"/>, <paramref name="originY"/>) px do mundo.</summary>
        public void Begin(float originX, float originY)
        {
            for (int y = _y0; y <= _y1; y++)
                for (int x = _x0; x <= _x1; x++) { int i = y * W + x; Px[i] = 0u; _body[i] = false; }
            _x0 = W; _y0 = H; _x1 = -1; _y1 = -1;
            _ox = originX; _oy = originY;
        }

        /// <summary>A boca do buraco: abaixo de <paramref name="surfaceY"/>, entre <paramref name="x0"/> e <paramref name="x1"/> (px do mundo), não desenha.</summary>
        public void SetClip(bool on, float x0, float x1, float surfaceY)
        {
            _clips.Clear();
            if (on) AddClip(x0, x1, surfaceY);
        }

        /// <summary>Mais uma boca de buraco (px do mundo).</summary>
        public void AddClip(float x0, float x1, float surfaceY) { _clips.Add(new[] { x0, x1, surfaceY }); }

        bool _room;
        float _rx0, _ry0, _rx1, _ry1;

        /// <summary>
        /// O interior da sala (px do mundo): fora dele (no chão, nas paredes, no teto) o corpo não aparece. A luta com a
        /// Isyathy usa isto: ela anda dentro da terra pelas quatro bordas e sai de qualquer lado.
        /// </summary>
        public void SetRoom(bool on, float x0, float y0, float x1, float y1) { _room = on; _rx0 = x0; _ry0 = y0; _rx1 = x1; _ry1 = y1; }

        public bool Clipped(float wx, float wy)
        {
            if (_room && (wx < _rx0 || wx > _rx1 || wy < _ry0 || wy > _ry1)) return true;
            for (int i = 0; i < _clips.Count; i++)
            {
                var c = _clips[i];
                if (wy < c[2] && wx >= c[0] && wx <= c[1]) return true;
            }
            return false;
        }

        void Touch(int x, int y)
        {
            if (x < _x0) _x0 = x; if (x > _x1) _x1 = x;
            if (y < _y0) _y0 = y; if (y > _y1) _y1 = y;
        }

        /// <summary>As fatias do corpo, do rabo para a cabeça (a da frente cobre a de trás).</summary>
        public void Body(SerpentDisc[] discs, int count)
        {
            for (int k = 0; k < count; k++)
            {
                var d = discs[k];
                float nx = -d.DirY, ny = d.DirX;
                if (ny < 0f || (Math.Abs(ny) < 1e-4f && nx > 0f)) { nx = -nx; ny = -ny; }   // o lado de cima é o dorso
                float nL = nx * Lx + ny * Ly;
                float bx = d.X - _ox, by = d.Y - _oy, r = d.R, r2 = r * r;
                int ix0 = Math.Max(0, (int)Math.Floor(bx - r)), ix1 = Math.Min(W - 1, (int)Math.Ceiling(bx + r));
                int iy0 = Math.Max(0, (int)Math.Floor(by - r)), iy1 = Math.Min(H - 1, (int)Math.Ceiling(by + r));
                for (int y = iy0; y <= iy1; y++)
                    for (int x = ix0; x <= ix1; x++)
                    {
                        float dx = x + 0.5f - bx, dy = y + 0.5f - by;
                        if (dx * dx + dy * dy > r2) continue;
                        if (Clipped(x + 0.5f + _ox, y + 0.5f + _oy)) continue;
                        float along = dx * d.DirX + dy * d.DirY, across = dx * nx + dy * ny;
                        float v = Math.Max(-1f, Math.Min(1f, across / r));
                        int i = y * W + x;
                        _u[i] = d.U - along;
                        _v[i] = v;
                        _lit[i] = v * nL + 0.35f * (float)Math.Sqrt(1f - v * v) - 0.15f;
                        _r[i] = r;
                        _body[i] = true;
                        Touch(x, y);
                    }
            }
            // 2ª passada: a cor de cada pixel uma vez
            for (int y = _y0; y <= _y1; y++)
                for (int x = _x0; x <= _x1; x++)
                {
                    int i = y * W + x;
                    if (_body[i]) Px[i] = IsyathyArt.BodyColor(_u[i], _v[i], _lit[i], _r[i], Stage);
                }
        }

        /// <summary>
        /// A cabeça (<paramref name="head"/>, linha 0 em cima, olhando para a direita) com o pescoço em (<paramref name="neckX"/>,
        /// <paramref name="neckY"/>) px do mundo, girada <paramref name="angle"/> (rad). <paramref name="flipY"/>: virada
        /// para a esquerda, o alto da cabeça continua para cima.
        /// </summary>
        public void Head(uint[] head, int hw, int hh, int neckCol, int neckRow, float neckX, float neckY, float angle, bool flipY)
        {
            float cs = (float)Math.Cos(angle), sn = (float)Math.Sin(angle);
            float reach = (float)Math.Sqrt(hw * hw + hh * hh) + 2f;
            float bx = neckX - _ox, by = neckY - _oy;
            int ix0 = Math.Max(0, (int)Math.Floor(bx - reach)), ix1 = Math.Min(W - 1, (int)Math.Ceiling(bx + reach));
            int iy0 = Math.Max(0, (int)Math.Floor(by - reach)), iy1 = Math.Min(H - 1, (int)Math.Ceiling(by + reach));
            for (int y = iy0; y <= iy1; y++)
                for (int x = ix0; x <= ix1; x++)
                {
                    float lx = x + 0.5f - bx, ly = y + 0.5f - by;
                    float sx = lx * cs + ly * sn, sy = -lx * sn + ly * cs;   // desfaz a rotação
                    if (flipY) sy = -sy;
                    int col = (int)Math.Floor(neckCol + sx), row = (int)Math.Floor(neckRow - sy);
                    if (col < 0 || row < 0 || col >= hw || row >= hh) continue;
                    uint c = head[row * hw + col];
                    if (c == 0u) continue;
                    if (Clipped(x + 0.5f + _ox, y + 0.5f + _oy)) continue;
                    int i = y * W + x;
                    Px[i] = c;
                    _body[i] = false;
                    Touch(x, y);
                }
        }

        /// <summary>O contorno da silhueta (1 px, fora do que está embaixo do chão).</summary>
        public void Finish()
        {
            if (_x1 < _x0) return;
            int x0 = Math.Max(0, _x0 - 1), x1 = Math.Min(W - 1, _x1 + 1), y0 = Math.Max(0, _y0 - 1), y1 = Math.Min(H - 1, _y1 + 1);
            var add = new System.Collections.Generic.List<int>();
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int i = y * W + x;
                    if (Px[i] != 0u) continue;
                    bool edge = (x > 0 && Px[i - 1] != 0u && Px[i - 1] != IsyathyArt.Outline) ||
                                (x < W - 1 && Px[i + 1] != 0u && Px[i + 1] != IsyathyArt.Outline) ||
                                (y > 0 && Px[i - W] != 0u && Px[i - W] != IsyathyArt.Outline) ||
                                (y < H - 1 && Px[i + W] != 0u && Px[i + W] != IsyathyArt.Outline);
                    if (edge && !Clipped(x + 0.5f + _ox, y + 0.5f + _oy)) add.Add(i);
                }
            foreach (int i in add) Px[i] = IsyathyArt.Outline;
            _x0 = x0; _x1 = x1; _y0 = y0; _y1 = y1;
        }

        public bool Empty { get { return _x1 < _x0; } }
    }
}
