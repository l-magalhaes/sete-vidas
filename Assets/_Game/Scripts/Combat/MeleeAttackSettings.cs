using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Valores de um ataque corpo a corpo (spec: Docs/Design/combate-adagas.md).
    /// O asset RogueDaggers é o das adagas do Ladino; a marreta do Paladino (Fase 5) usará outro asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Melee Attack Settings", fileName = "MeleeAttackSettings")]
    public class MeleeAttackSettings : ScriptableObject
    {
        [Header("Dano")]
        [Min(0)] public int BaseDamage = 2;
        public DamageType Type = DamageType.Normal;
        [Tooltip("Aplica os multiplicadores de costas/furtivo. O GDD §5.2 dá esse bônus ao Ladino")]
        public bool BackstabBonus = true;
        public DamageMultipliers Multipliers = DamageMultipliers.Default;

        [Header("Tempos (s)")]
        [Range(1, 4)] public int ComboSteps = 2;
        [Range(0f, 0.5f)] public float Windup = 0.04f;
        [Range(0.02f, 0.3f)] public float Active = 0.08f;
        [Tooltip("Entre golpes do combo: apertar até aqui enfileira o próximo")]
        [Range(0f, 0.5f)] public float Recovery = 0.12f;
        [Tooltip("Depois do último golpe do combo")]
        [Range(0f, 0.8f)] public float FinalRecovery = 0.22f;

        [Header("Movimento e barulho")]
        [Tooltip("Velocidade de andar durante o ataque (1 = normal). Marreta: lenta")]
        [Range(0f, 1f)] public float MoveMultiplierWhileAttacking = 1f;
        [Tooltip("Multiplica o barulho do golpe (raio e intensidade do StealthSettings)")]
        [Min(0f)] public float NoiseMultiplier = 1f;

        [Header("Hitbox (tiles)")]
        public Vector2 HitboxSize = new Vector2(1.1f, 0.9f);
        [Tooltip("Distância do centro da hitbox à borda da frente do gato")]
        public float HitboxForward = 0.5f;

        [Header("Impacto")]
        [Range(0f, 0.3f)] public float HitstopFront = 0.05f;
        [Range(0f, 0.3f)] public float HitstopBack = 0.07f;
        [Range(0f, 0.3f)] public float HitstopStealth = 0.12f;

        public AttackTimings Timings => new AttackTimings
        {
            Steps = ComboSteps, Windup = Windup, Active = Active, Recovery = Recovery, FinalRecovery = FinalRecovery
        };

        public float HitstopFor(AttackAngle angle) => angle switch
        {
            AttackAngle.Back => HitstopBack,
            AttackAngle.StealthBack => HitstopStealth,
            _ => HitstopFront,
        };

        /// <summary>Monta o golpe contra um alvo: ângulo, dano final e direção.</summary>
        public DamageInfo ResolveHit(Vector2 attackerPosition, Component target, GameObject source)
        {
            var angle = AttackAngle.Front;
            if (BackstabBonus && target.TryGetComponent(out IFacingTarget facing))
                angle = DamageCalculator.Classify(attackerPosition.x, target.transform.position.x,
                    facing.FacingDirection, facing.InCombat);

            int amount = DamageCalculator.Apply(BaseDamage, angle, Multipliers);
            Vector2 dir = ((Vector2)target.transform.position - attackerPosition);
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;
            return new DamageInfo(amount, dir, Type, angle, source);
        }
    }
}
