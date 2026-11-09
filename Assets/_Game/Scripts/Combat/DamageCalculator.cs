using System;

namespace SeteVidas.Combat
{
    /// <summary>De que lado o golpe pegou o alvo (GDD §5.2).</summary>
    public enum AttackAngle
    {
        /// <summary>O atacante está do lado para onde o alvo olha.</summary>
        Front,
        /// <summary>O atacante está do lado oposto ao que o alvo olha.</summary>
        Back,
        /// <summary>Pelas costas e o alvo ainda não está em combate.</summary>
        StealthBack
    }

    /// <summary>Multiplicadores de dano por ângulo. Provisórios no GDD §5.2.</summary>
    [Serializable]
    public struct DamageMultipliers
    {
        public float Front;
        public float Back;
        public float StealthBack;

        public static DamageMultipliers Default => new DamageMultipliers { Front = 1f, Back = 1.5f, StealthBack = 2.5f };

        public float For(AttackAngle angle) => angle switch
        {
            AttackAngle.Back => Back,
            AttackAngle.StealthBack => StealthBack,
            _ => Front,
        };
    }

    /// <summary>
    /// Decide o ângulo do golpe e o dano final. C# puro (sem Unity), testado em EditMode.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>Distância horizontal abaixo da qual atacante e alvo contam como alinhados (golpe de frente).</summary>
        public const float AlignedTolerance = 0.05f;

        /// <param name="attackerX">Posição X do atacante.</param>
        /// <param name="targetX">Posição X do alvo.</param>
        /// <param name="targetFacing">1 = alvo olhando para a direita, -1 = para a esquerda.</param>
        /// <param name="targetInCombat">O alvo já está em combate (não pode ser surpreendido).</param>
        public static AttackAngle Classify(float attackerX, float targetX, int targetFacing, bool targetInCombat)
        {
            float dx = attackerX - targetX;
            if (Math.Abs(dx) <= AlignedTolerance || targetFacing == 0) return AttackAngle.Front;

            int attackerSide = dx > 0f ? 1 : -1;
            bool behind = attackerSide == -Math.Sign(targetFacing);
            if (!behind) return AttackAngle.Front;
            return targetInCombat ? AttackAngle.Back : AttackAngle.StealthBack;
        }

        /// <summary>Dano base × multiplicador, arredondado (0,5 para cima), nunca menor que 1.</summary>
        public static int Apply(int baseDamage, AttackAngle angle, DamageMultipliers multipliers)
        {
            if (baseDamage <= 0) return 0;
            double value = baseDamage * (double)multipliers.For(angle);
            return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>Quantos golpes iguais derrubam um alvo com esta vida.</summary>
        public static int HitsToDefeat(int health, int damagePerHit) =>
            damagePerHit <= 0 ? int.MaxValue : (health + damagePerHit - 1) / damagePerHit;
    }
}
