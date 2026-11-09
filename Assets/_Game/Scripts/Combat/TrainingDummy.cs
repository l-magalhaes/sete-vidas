using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Boneco de treino da Fase 2: tem vida, um lado para onde olha e mostra o dano de cada golpe.
    /// Fica "em combate" por alguns segundos depois de apanhar (sem bônus furtivo nesse tempo).
    /// Ao chegar a 0, some e volta com a vida cheia.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable, IFacingTarget
    {
        public enum Side { Right = 1, Left = -1 }

        [SerializeField, Min(1)] int maxHealth = 10;
        [Tooltip("Para onde o boneco olha. As costas ficam do lado oposto")]
        [SerializeField] Side facing = Side.Right;
        [Tooltip("Depois de apanhar, fica em combate por este tempo (s)")]
        [SerializeField, Min(0f)] float combatMemory = 3f;
        [Tooltip("Tempo até voltar depois de derrotado (s)")]
        [SerializeField, Min(0f)] float respawnDelay = 2f;

        [Header("Impacto")]
        [SerializeField] float recoilDistance = 0.15f;
        [SerializeField] float recoilTime = 0.1f;
        [SerializeField] float blinkTime = 0.2f;

        [Header("Referências")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Collider2D hurtbox;
        [SerializeField] TextMesh statusLabel;

        float _combatUntil = -1f;
        float _recoilUntil, _blinkUntil, _respawnAt;
        float _recoilDir;
        Vector3 _spriteRest;

        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDefeated => Health <= 0;
        public int FacingDirection => (int)facing;
        public bool InCombat => Time.time < _combatUntil;

        void Awake()
        {
            Health = maxHealth;
            if (spriteRenderer != null) _spriteRest = spriteRenderer.transform.localPosition;
            ApplyFacing();
        }

        void OnValidate() => ApplyFacing();

        /// <summary>Muda o lado para onde o boneco olha (também pelo Inspector).</summary>
        public void SetFacing(Side side)
        {
            facing = side;
            ApplyFacing();
        }

        void ApplyFacing()
        {
            if (spriteRenderer != null) spriteRenderer.flipX = facing == Side.Left;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDefeated) return;

            Health = Mathf.Max(0, Health - info.Amount);
            _combatUntil = Time.time + combatMemory;
            _recoilDir = Mathf.Sign(info.Direction.x == 0f ? 1f : info.Direction.x);
            _recoilUntil = Time.time + recoilTime;
            _blinkUntil = Time.time + blinkTime;

            DamagePopup.Spawn(transform.position + Vector3.up * 1.9f, Label(info), ColorFor(info.Angle));

            if (IsDefeated)
            {
                _respawnAt = Time.time + respawnDelay;
                SetVisible(false);
            }
        }

        static string Label(in DamageInfo info) => info.Angle switch
        {
            AttackAngle.StealthBack => $"{info.Amount} FURTIVO",
            AttackAngle.Back => $"{info.Amount} COSTAS",
            _ => info.Amount.ToString(),
        };

        static Color ColorFor(AttackAngle angle) => angle switch
        {
            AttackAngle.StealthBack => new Color32(0xff, 0x4d, 0x4d, 0xff),
            AttackAngle.Back => new Color32(0xff, 0xa8, 0x3a, 0xff),
            _ => Color.white,
        };

        void Update()
        {
            if (IsDefeated && Time.time >= _respawnAt)
            {
                Health = maxHealth;
                _combatUntil = -1f;
                SetVisible(true);
            }

            if (spriteRenderer != null)
            {
                bool recoiling = Time.time < _recoilUntil;
                spriteRenderer.transform.localPosition = _spriteRest + (recoiling ? Vector3.right * (_recoilDir * recoilDistance) : Vector3.zero);
                bool blinking = Time.time < _blinkUntil;
                spriteRenderer.enabled = !IsDefeated && (!blinking || ((int)(Time.time * 30f) & 1) == 0);
            }

            if (statusLabel != null)
            {
                string state = IsDefeated ? "derrotado" : InCombat ? "em combate" : "calmo";
                string arrow = facing == Side.Right ? "olha >" : "< olha";
                statusLabel.text = $"{Health}/{maxHealth} {arrow}\n{state}";
            }
        }

        void SetVisible(bool visible)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = visible;
            if (hurtbox != null) hurtbox.enabled = visible;
        }
    }
}
