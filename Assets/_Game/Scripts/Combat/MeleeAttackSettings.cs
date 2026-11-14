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
        [Min(0f)] public float FinisherLunge = 2.25f;
        [Tooltip("Duração do avanço (s)")]
        [Min(0.02f)] public float FinisherLungeTime = 0.15f;
        [Tooltip("Multiplica o dano do golpe final")]
        [Min(1f)] public float FinisherDamageMultiplier = 1.5f;

        [Header("Ataque para baixo no ar: S + ataque (melhoria 5)")]
        [Tooltip("No ar, com baixo segurado, o ataque vira o golpe para baixo")]
        public bool DownAttack = true;
        [Min(0f)] public float DownWindup = 0.05f;
        [Tooltip("Adaga: tempo com a ponta para baixo procurando onde quicar (s)")]
        [Min(0.02f)] public float DownActive = 0.3f;
        [Min(0f)] public float DownRecovery = 0.1f;
        [Tooltip("Caixa da estocada embaixo do gato (tiles): o tamanho do rastro em meia-lua (24×14 px)")]
        public Vector2 DownHitbox = new Vector2(1.5f, 0.875f);
        [Tooltip("Distância do centro da caixa abaixo dos pés (tiles)")]
        public float DownHitboxBelow = 0.35f;
        [Tooltip("Adaga: altura do quique, em fração da altura do pulo")]
        [Range(0f, 3f)] public float PogoHeightFraction = 1.8f; // 3× o quique inicial (pedido do usuário)
        [Tooltip("Adaga: tempo em que a armadilha atingida por cima não machuca, depois do quique (s)")]
        [Min(0f)] public float PogoGrace = 0.25f;
        [Tooltip("Marreta (dano pesado): multiplica a gravidade na queda pesada")]
        [Min(1f)] public float DropGravityMultiplier = 2f;
        [Tooltip("Marreta: dano em quem está embaixo")]
        [Min(0)] public int DropHitDamage = 4;
        [Tooltip("Marreta: mini terremoto ao tocar o chão (alcance para cada lado, tiles)")]
        [Min(0f)] public float DropImpactRange = 1.5f;
        [Min(0)] public int DropImpactDamage = 3;
        [Min(0f)] public float DropRecovery = 0.3f;

        [Header("Ataque para cima: {subir} + ataque (melhorias 14/11/2026)")]
        [Tooltip("Com cima segurado, o ataque vira um golpe só, em cima da cabeça (no chão ou no ar)")]
        public bool UpAttack = true;
        [Tooltip("Caixa do golpe para cima (tiles), com a base no alto da cabeça")]
        public Vector2 UpHitbox = new Vector2(1.4f, 1.3f);

        /// <summary>A queda pesada é da arma de dano pesado (marreta); as outras quicam.</summary>
        public bool HeavyDrop => Type == DamageType.Heavy;

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

            var multipliers = Multipliers;
            if (angle == AttackAngle.Back && target.TryGetComponent(out IBackDamageMultiplier custom)) multipliers.Back = custom.BackMultiplier;
            int amount = DamageCalculator.Apply(BaseDamage, angle, multipliers);
            Vector2 dir = ((Vector2)target.transform.position - attackerPosition);
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;
            return new DamageInfo(amount, dir, Type, angle, source);
        }
    }
}
