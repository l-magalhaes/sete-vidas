using System;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// A curva do corpo da serpente sem toca (pedido do usuário, 17/11/2026: "deixe mais orgânica"). Em px, do lado
    /// para onde ela olha: do rabo (tx, ty), rente ao chão, até o pescoço (nx, ny), colado na cabeça. Uma onda corre pelo
    /// corpo, presa nas duas pontas (o rabo no chão e o pescoço na cabeça).
    /// <list type="bullet">
    /// <item>Deitada/rastejando: onda de 2 px a cada 14 px (o desenho de sempre).</item>
    /// <item>Aviso (<c>coil</c> 0→1): enrola em S (onda maior e mais curta), a cabeça recua e levanta: o bote se arma.</item>
    /// <item>Bote (<c>ext</c> 0→1): o corpo estica e a onda quase some; o rabo avança um pouco atrás da cabeça.</item>
    /// </list>
    /// </summary>
    public static class SnakeSpine
    {
        /// <summary>Comprimento do corpo deitado (px), do rabo ao pescoço: o do desenho rastejando.</summary>
        public const float RestLength = 22f;
        /// <summary>Quanto o rabo avança (px) com o bote inteiro: o corpo estica, mas não fica parado no lugar.</summary>
        public const float TailCreep = 24f;
        public const float RestAmplitude = 1.5f, CoilAmplitude = 2.5f, StretchAmplitude = 1f;
        public const float RestWavelength = 16f, CoilWavelength = 13f, StretchWavelength = 22f;
        /// <summary>No aviso, a cabeça recua (tiles) e levanta (tiles).</summary>
        public const float CoilPullback = 0.3f, CoilRaise = 0.125f;

        static float Lerp(float a, float b, float t) { t = Math.Max(0f, Math.Min(1f, t)); return a + (b - a) * t; }

        public static float Amplitude(float ext, float coil) { return Lerp(Lerp(RestAmplitude, CoilAmplitude, coil), StretchAmplitude, ext); }
        public static float Wavelength(float ext, float coil) { return Lerp(Lerp(RestWavelength, CoilWavelength, coil), StretchWavelength, ext); }

        /// <summary>Altura (px) do meio do corpo na coluna x; <paramref name="phase"/> faz a onda correr para o rabo.</summary>
        public static float Y(float x, float tx, float ty, float nx, float ny, float amp, float wave, float phase)
        {
            float len = Math.Max(1f, nx - tx);
            float u = Math.Max(0f, Math.Min(1f, (x - tx) / len));
            float s = u * u * (3f - 2f * u);
            float env = (float)Math.Sin(Math.PI * u);
            return ty + (ny - ty) * s + amp * env * (float)Math.Sin(2.0 * Math.PI * (x - tx) / Math.Max(1f, wave) + phase);
        }

        /// <summary>Meia espessura (px) na coluna x: o rabo afina até a ponta (0, 1, 2 a cada 3 px).</summary>
        public static int Half(float x, float tx) { return Math.Max(0, Math.Min(2, (int)((x - tx) / 3f))); }
    }
}
