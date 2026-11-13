using System;

namespace SeteVidas.Enemies.Zombie
{
    /// <summary>
    /// Regras do Templário Zumbi (spec: Docs/Design/zumbi-templario.md). C# puro, testado em EditMode.
    /// </summary>
    public static class ZombieRules
    {
        /// <summary>Vagando: vira se bateu na parede, se o chão acaba à frente ou se passou do fim da faixa do posto.</summary>
        public static bool ShouldTurn(bool wallAhead, bool groundAhead, float x, int facing, bool hasBounds, float minX, float maxX)
        {
            if (wallAhead || !groundAhead) return true;
            if (!hasBounds) return false;
            return facing > 0 ? x >= maxX : x <= minX;
        }

        /// <summary>
        /// Sente o gato pela visão: à frente até <paramref name="sightRange"/>; de qualquer lado bem perto
        /// (<paramref name="closeRange"/>, o faro). Escondido na sombra, só o faro conta, e pela metade.
        /// Só no mesmo andar (|dy| até <paramref name="maxHeight"/>).
        /// </summary>
        public static bool Senses(float dx, float dy, int facing, float sightRange, float closeRange, float maxHeight, bool hidden)
        {
            if (Math.Abs(dy) > maxHeight) return false;
            float dist = Math.Abs(dx);
            float close = hidden ? closeRange * 0.5f : closeRange;
            if (dist <= close) return true;
            if (hidden) return false;
            bool inFront = dx * facing >= 0f;
            return inFront && dist <= sightRange;
        }

        /// <summary>Ouve um barulho: dentro do raio dele (ajustado pelo ouvido do zumbi).</summary>
        public static bool Hears(float distance, float noiseRadius, float hearingMultiplier)
        {
            return noiseRadius > 0f && distance <= noiseRadius * hearingMultiplier;
        }

        /// <summary>Para que lado correr atrás do gato (0 = já está em cima dele).</summary>
        public static int ChaseDirection(float selfX, float targetX, float deadZone)
        {
            float d = targetX - selfX;
            if (Math.Abs(d) <= deadZone) return 0;
            return d > 0f ? 1 : -1;
        }
    }
}
