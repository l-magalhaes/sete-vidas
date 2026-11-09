using System;

namespace SeteVidas.Player
{
    /// <summary>Conta da visibilidade do gato (GDD §5.1). C# puro, testado em EditMode.</summary>
    public static class StealthMath
    {
        /// <summary>
        /// Visibilidade de 0 (invisível) a 1 (totalmente exposto) = exposição da luz × multiplicador do agachar
        /// × esconderijo. O esconderijo (matinho) só conta agachado: de pé, a cabeça aparece por cima.
        /// </summary>
        public static float Visibility(float lightExposure, bool crouching, float crouchMultiplier, float concealment = 1f)
        {
            float exposure = Clamp01(lightExposure);
            float crouch = crouching ? Clamp01(crouchMultiplier) * Clamp01(concealment) : 1f;
            return exposure * crouch;
        }

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));

        /// <summary>A queda faz barulho? Um pulo normal (altura do pulo) não deve fazer.</summary>
        public static bool LandingIsLoud(float fallHeight, float loudHeight) => fallHeight >= loudHeight;
    }
}
