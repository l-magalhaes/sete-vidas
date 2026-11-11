using System;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>Peso do golpe para a reação do alvo (melhoria 3).</summary>
    public enum HitWeight { Light, Strong, Heavy }

    /// <summary>Como o corpo do alvo reage a um golpe de um peso.</summary>
    [Serializable]
    public struct HitReaction
    {
        [Tooltip("Velocidade do recuo, para trás do golpe (tiles/s)")]
        public float Knockback;
        [Tooltip("Velocidade para cima do recuo (tiles/s)")]
        public float Lift;
        [Tooltip("Clarão branco (s): 0,05 = 1 quadro a 20 fps")]
        public float Flash;
        [Tooltip("Pose de dor: não anda nem ataca (s)")]
        public float Stun;
        [Tooltip("Achatar e esticar (px)")]
        public int SquashPx;
        [Tooltip("Faíscas na armadura")]
        public int Sparks;
        [Tooltip("Poeira de impacto (golpe pesado)")]
        public bool Dust;
        [Tooltip("Tremor da câmera (px); 0 = sem tremor")]
        public float ShakePx;
        [Tooltip("Duração do tremor (s)")]
        public float ShakeTime;
    }

    /// <summary>Tabela de reações por peso do golpe (vive no SentinelSettings e no GoldenBossSettings).</summary>
    [Serializable]
    public class HitReactionTable
    {
        public HitReaction Light = new HitReaction { Knockback = 3f, Flash = 0.05f, Stun = 0.2f, SquashPx = 1, Sparks = 3 };
        public HitReaction Strong = new HitReaction { Knockback = 5f, Flash = 0.1f, Stun = 0.3f, SquashPx = 1, Sparks = 5 };
        public HitReaction Heavy = new HitReaction { Knockback = 7f, Lift = 1.5f, Flash = 0.1f, Stun = 0.45f, SquashPx = 2, Sparks = 5, Dust = true, ShakePx = 2f, ShakeTime = 0.15f };
        [Tooltip("Golpe leve só interrompe o ataque se a preparação ainda não passou desta fração")]
        [Range(0f, 1f)] public float LightInterruptBefore = 0.5f;
        [Tooltip("Multiplica o recuo (chefe: 0,5)")]
        [Min(0f)] public float KnockbackScale = 1f;

        public HitReaction For(HitWeight weight) => weight switch
        {
            HitWeight.Heavy => Heavy,
            HitWeight.Strong => Strong,
            _ => Light,
        };

        public static HitReactionTable Boss() => new HitReactionTable
        {
            Light = new HitReaction { Knockback = 3f, Flash = 0.05f, Sparks = 3 },
            Strong = new HitReaction { Knockback = 5f, Flash = 0.05f, Sparks = 3 },
            Heavy = new HitReaction { Knockback = 7f, Flash = 0.05f, Sparks = 3, Dust = true, ShakePx = 2f, ShakeTime = 0.15f },
            KnockbackScale = 0.5f,
        };
    }

    /// <summary>Regras puras da reação ao dano (EditMode).</summary>
    public static class HitReactionRules
    {
        /// <summary>Marreta = pesado; pelas costas ou furtivo = forte; o resto = leve.</summary>
        public static HitWeight WeightOf(DamageType type, AttackAngle angle)
        {
            if (type == DamageType.Heavy) return HitWeight.Heavy;
            return angle == AttackAngle.Back || angle == AttackAngle.StealthBack ? HitWeight.Strong : HitWeight.Light;
        }

        /// <summary>
        /// O golpe interrompe o ataque do alvo? O chefe nunca (só troca de fase). Forte e pesado sempre.
        /// Leve só durante a preparação e antes de <paramref name="lightLimit"/> dela.
        /// </summary>
        public static bool Interrupts(HitWeight weight, bool inWindup, float windupProgress01, bool isBoss, float lightLimit)
        {
            if (isBoss) return false;
            if (weight != HitWeight.Light) return true;
            return inWindup && windupProgress01 < lightLimit;
        }

        /// <summary>Lado do recuo: para onde o golpe empurra (−1 ou 1). Sem direção, para trás do alvo.</summary>
        public static int KnockbackSide(Vector2 hitDirection, int targetFacing) =>
            Mathf.Abs(hitDirection.x) > 0.01f ? (hitDirection.x > 0f ? 1 : -1) : (targetFacing >= 0 ? -1 : 1);

        /// <summary>Escala do achatar: mais largo e mais baixo em <paramref name="px"/> pixels.</summary>
        public static Vector2 SquashScale(int px, float widthPx, float heightPx)
        {
            if (px <= 0 || widthPx <= 0f || heightPx <= 0f) return Vector2.one;
            return new Vector2(1f + px / widthPx, 1f - px / heightPx);
        }
    }
}
