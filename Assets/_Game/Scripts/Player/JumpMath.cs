using System;

namespace SeteVidas.Player
{
    /// <summary>
    /// Conta do pulo a partir de "altura em tiles" e "tempo até o topo". C# puro, testado em EditMode.
    /// </summary>
    public static class JumpMath
    {
        /// <summary>Gravidade constante que leva ao topo em <paramref name="timeToApex"/> segundos.</summary>
        public static float Gravity(float height, float timeToApex) =>
            2f * height / (timeToApex * timeToApex);

        /// <summary>
        /// Velocidade de saída que faz a física em passos fixos (gravidade aplicada antes de mover)
        /// chegar exatamente a <paramref name="height"/>. A fórmula contínua (2h/t) erra por
        /// v·dt/2, cerca de 0,2 tile a 50 Hz, o que estragaria as métricas de level design.
        /// Resolve: v²/(2g) − v·dt/2 = h.
        /// </summary>
        public static float LaunchVelocity(float height, float timeToApex, float fixedDeltaTime)
        {
            float g = Gravity(height, timeToApex);
            return LaunchVelocityForGravity(g, height, fixedDeltaTime);
        }

        /// <summary>Mesma conta, para uma gravidade já conhecida (ex.: pulo da parede usa a gravidade do pulo normal).</summary>
        public static float LaunchVelocityForGravity(float gravity, float height, float fixedDeltaTime)
        {
            if (height <= 0f) return 0f;
            float gdt = gravity * fixedDeltaTime;
            return (gdt + (float)Math.Sqrt(gdt * gdt + 8f * gravity * height)) / 2f;
        }
    }
}
