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
        [Range(1, 4)] public int ComboSteps = 3;
        [Tooltip("Golpes do combo no ar (o golpe final com avanço é só no chão)")]
        [Range(1, 4)] public int AirComboSteps = 2;
        [Range(0f, 0.5f)] public float Windup = 0.04f;
        [Range(0.02f, 0.3f)] public float Active = 0.08f;
        [Tooltip("Entre golpes do combo: apertar até aqui enfileira o próximo")]
        [Range(0f, 0.5f)] public float Recovery = 0.12f;
        [Tooltip("Depois do último golpe do combo")]
        [Range(0f, 0.8f)] public float FinalRecovery = 0.3f;

        [Header("Golpe final do combo: estocada com avanço (melhoria 4)")]
        [Tooltip("Distância do avanço no último golpe do combo, no chão (tiles). 0 = sem avanço")]
        [Min(0f)] public float FinisherLunge = 1.5f;
        [Tooltip("Duração do avanço (s)")]
        [Min(0.02f)] public float FinisherLungeTime = 0.12f;
        [Tooltip("Multiplica o dano do golpe final")]
        [Min(1f)] public float FinisherDamageMultiplier = 1.5f;

        [Header("Golpe carregado: terremoto (melhoria 4)")]
        [Tooltip("Segurar o ataque carrega o golpe (marreta)")]
        public bool Chargeable;
        [Tooltip("Segurando até aqui, vira carga; soltar antes = golpe normal (s)")]
        [Min(0f)] public float ChargeStart = 0.4f;
        [Tooltip("Carga máxima (s, contados do aperto)")]
        [Min(0.1f)] public float ChargeFull = 1.2f;
        [Tooltip("Do soltar até o impacto no chão (s)")]
        [Min(0f)] public float SlamWindup = 0.1f;
        [Tooltip("Parado depois do impacto (s)")]
        [Min(0f)] public float SlamRecovery = 0.45f;
        [Tooltip("Alcance da onda para cada lado: carga mínima → máxima (tiles)")]
        public Vector2 ShockwaveRange = new Vector2(3f, 5f);
        [Tooltip("Dano da onda: carga mínima → máxima")]
        public Vector2Int ShockwaveDamage = new Vector2Int(3, 5);
        [Tooltip("Altura da faixa da onda acima do chão (tiles): só alvos de pé no chão")]
        [Min(0.1f)] public float ShockwaveHeight = 1.2f;
        [Tooltip("Tempo derrubado (s)")]
        [Min(0f)] public float KnockdownTime = 1f;
        [Tooltip("Barulho do impacto: alcance para a IA (tiles)")]
        [Min(0f)] public float ShockwaveNoiseRadius = 12f;
        [Tooltip("Tremor da câmera no impacto (px, s)")]
        public Vector2 ShockwaveShake = new Vector2(3f, 0.25f);

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

        /// <summary>Tempos no ar: combo mais curto (sem o golpe final com avanço).</summary>
        public AttackTimings AirTimings
        {
            get
            {
                var t = Timings;
                t.Steps = Mathf.Min(ComboSteps, AirComboSteps);
                return t;
            }
        }

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
