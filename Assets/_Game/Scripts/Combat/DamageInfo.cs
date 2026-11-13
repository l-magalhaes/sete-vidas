using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>Normal = adagas e golpes comuns. Heavy = marreta do Paladino (única que quebra parede rachada).</summary>
    public enum DamageType { Normal, Heavy }

    /// <summary>Um golpe que chegou ao alvo, já com o dano final calculado.</summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        /// <summary>Direção do atacante para o alvo (normalizada). Usada para o recuo.</summary>
        public readonly Vector2 Direction;
        public readonly DamageType Type;
        public readonly AttackAngle Angle;
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector2 direction, DamageType type, AttackAngle angle, GameObject source)
        {
            Amount = amount;
            Direction = direction;
            Type = type;
            Angle = angle;
            Source = source;
        }
    }

    /// <summary>Qualquer coisa que toma dano: inimigos, boneco de treino, parede rachada e, na Fase 4, o gato.</summary>
    public interface IDamageable
    {
        void TakeDamage(in DamageInfo info);
    }

    /// <summary>
    /// Alvo com o próprio multiplicador de golpe pelas costas (Templário Zumbi: nunca é surpreendido, mas leva
    /// dano dobrado por trás, pedido do usuário de 12/11/2026). Vale no lugar do <see cref="DamageMultipliers.Back"/>.
    /// </summary>
    public interface IBackDamageMultiplier
    {
        float BackMultiplier { get; }
    }

    /// <summary>Alvo que tem frente e costas. Sem esta interface, todo golpe conta como de frente.</summary>
    public interface IFacingTarget
    {
        /// <summary>1 = olhando para a direita, -1 = para a esquerda.</summary>
        int FacingDirection { get; }
        /// <summary>Já está em combate: não pode ser surpreendido (sem bônus furtivo).</summary>
        bool InCombat { get; }
    }
}
