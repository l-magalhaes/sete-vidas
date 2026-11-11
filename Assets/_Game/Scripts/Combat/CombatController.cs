using System.Collections.Generic;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Ataques corpo a corpo do gato (spec: Docs/Design/combate-adagas.md).
    /// A hitbox fica à frente do gato durante a fase ativa e acerta cada alvo uma vez por golpe.
    /// Melhoria 4: o último golpe do combo no chão avança (estocada) e a marreta, segurada, carrega o terremoto.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class CombatController : MonoBehaviour
    {
        [SerializeField] MeleeAttackSettings weapon;
        [SerializeField] PlayerInputReader input;
        [Tooltip("Layers que a hitbox acerta")]
        [SerializeField] LayerMask targetLayers;

        PlayerController _player;
        PlayerHealth _health;
        readonly AttackSequence _sequence = new AttackSequence();
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();
        readonly List<Collider2D> _overlaps = new List<Collider2D>();
        int _lastSwingId;
        int _comboSteps;          // golpes do combo atual (no ar é mais curto)
        ContactFilter2D _filter;

        // golpe carregado
        enum Slam { None, Windup, Recovery }
        bool _pressHeld;          // o aperto que começou este golpe continua segurado
        float _holdTime;
        bool _charging;
        Slam _slam;
        float _slamTimer;
        float _slamCharge;
        float _glintTimer;

        public bool IsAttacking => _sequence.IsAttacking || _charging || _slam != Slam.None;
        public bool IsHitActive => _sequence.IsHitActive;
        public int ComboStep => _sequence.Step;
        /// <summary>Lado do golpe atual, travado no início do golpe.</summary>
        public int AttackDirection { get; private set; } = 1;
        public MeleeAttackSettings Weapon => weapon;
        /// <summary>Na preparação do golpe (a marreta mostra a arma erguida).</summary>
        public bool IsWindingUp => _sequence.Current == AttackSequence.Phase.Windup || _charging;
        /// <summary>Segurando a marreta erguida, carregando o terremoto.</summary>
        public bool IsCharging => _charging;
        /// <summary>Carga atual (0 a 1) enquanto carrega; a do último terremoto depois.</summary>
        public float Charge01 => _charging ? ChargeRules.Charge01(_holdTime, weapon.ChargeStart, weapon.ChargeFull) : _slamCharge;
        /// <summary>Soltou a carga: descendo a marreta ou parado depois do impacto.</summary>
        public bool IsSlamming => _slam != Slam.None;
        /// <summary>
        /// Progresso do terremoto para a animação <c>hammer_slam</c> (6 quadros): descer a marreta ocupa o primeiro
        /// terço (quadros 0–1); o impacto cai no quadro 2, junto com o dano; o resto é a recuperação.
        /// </summary>
        public float SlamProgress
        {
            get
            {
                if (_slam == Slam.None || weapon == null) return 0f;
                const float impactAt = 2f / 6f;
                if (_slam == Slam.Windup)
                    return weapon.SlamWindup <= 0f ? impactAt : impactAt * Mathf.Clamp01(1f - _slamTimer / weapon.SlamWindup);
                float r = weapon.SlamRecovery <= 0f ? 1f : Mathf.Clamp01(1f - _slamTimer / weapon.SlamRecovery);
                return impactAt + (1f - impactAt) * r;
            }
        }
        /// <summary>O golpe atual é o golpe final com avanço.</summary>
        public bool IsFinisher => _sequence.IsAttacking && ComboRules.IsFinisher(_sequence.Step, _comboSteps) && _comboSteps == weapon.ComboSteps;

        /// <summary>Troca a arma (troca de classe). Cancela um golpe em andamento.</summary>
        public void Equip(MeleeAttackSettings newWeapon)
        {
            if (newWeapon == null || newWeapon == weapon) return;
            CancelAll();
            weapon = newWeapon;
            if (!enabled && _player != null) enabled = true; // começou sem arma
        }

        /// <summary>Disparado a cada alvo atingido (para efeitos e testes).</summary>
        public event System.Action<IDamageable, DamageInfo> Hit;
        /// <summary>Um golpe entrou na fase ativa (o StealthController faz barulho com isto).</summary>
        public event System.Action SwingStarted;
        /// <summary>O terremoto bateu no chão (efeitos e testes).</summary>
        public event System.Action<float> ShockwaveReleased;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            _filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = targetLayers };
            if (weapon == null)
            {
                Debug.LogError("[Sete Vidas] CombatController sem MeleeAttackSettings.", this);
                enabled = false;
            }
        }

        void OnEnable() { if (_health != null) _health.Damaged += OnDamaged; }

        void OnDisable()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
            CancelAll();
            if (_player != null) _player.SpeedMultiplier = 1f;
        }

        /// <summary>Tomar dano cancela a carga (o golpe já solto continua).</summary>
        void OnDamaged(DamageInfo _) => _charging = false;

        bool CanAttack => !_player.IsHanging && !_player.IsClimbingLedge && _player.ActiveAbility == null;

        void CancelAll()
        {
            _sequence.Cancel();
            _charging = false;
            _slam = Slam.None;
            _pressHeld = false;
            if (_player != null) _player.StopLunge();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!CanAttack)
            {
                if (IsAttacking) CancelAll();
                _player.SpeedMultiplier = 1f;
                return;
            }

            bool grounded = _player.IsGrounded;
            if (_slam != Slam.None) { StepSlam(dt); return; }
            if (_charging) { StepCharge(dt, grounded); return; }

            var timings = _sequence.IsAttacking ? CurrentTimings() : (grounded ? weapon.Timings : weapon.AirTimings);
            if (input != null && input.AttackPressedThisFrame)
            {
                bool wasIdle = !_sequence.IsAttacking;
                // virou o analógico e atacou no mesmo instante: o golpe sai para o lado novo
                if (wasIdle && input.FirmDirection != 0) _player.Face(input.FirmDirection);
                if (_sequence.Press(timings) && wasIdle)
                {
                    AttackDirection = _player.FacingDirection;
                    _comboSteps = timings.Steps;
                    _pressHeld = true;
                    _holdTime = 0f;
                }
            }

            // marreta: segurando, a marreta fica erguida no alto da preparação e, depois de ChargeStart, vira carga
            float seqDt = dt;
            if (weapon.Chargeable && _pressHeld && _sequence.Current == AttackSequence.Phase.Windup && _sequence.Step == 0)
            {
                if (input == null || !input.AttackHeld) _pressHeld = false; // soltou: segue o golpe normal
                else
                {
                    _holdTime += dt;
                    if (grounded && ChargeRules.IsCharge(_holdTime, weapon.ChargeStart))
                    {
                        _sequence.Cancel();
                        _charging = true;
                        _glintTimer = 0f;
                        _player.SpeedMultiplier = 0f;
                        return;
                    }
                    seqDt = Mathf.Min(dt, Mathf.Max(0f, _sequence.PhaseTimeLeft - 0.0001f)); // segura no alto
                }
            }
            else if (_sequence.Current != AttackSequence.Phase.Windup) _pressHeld = false;

            int stepBefore = _sequence.Step;
            _sequence.Tick(seqDt, timings);
            if (_sequence.Step != stepBefore) AttackDirection = _player.FacingDirection; // o golpe seguinte pode virar

            if (_sequence.SwingId != _lastSwingId)
            {
                _lastSwingId = _sequence.SwingId;
                _hitThisSwing.Clear();
                SwingStarted?.Invoke();
                // golpe final no chão: estocada com avanço
                if (IsFinisher && weapon.FinisherLunge > 0f && grounded && !_player.IsCrouching)
                    _player.StartLunge(AttackDirection, weapon.FinisherLunge, weapon.FinisherLungeTime);
            }
            if (_sequence.IsHitActive) CheckHits();

            // Armas pesadas deixam o gato lento enquanto golpeia
            _player.SpeedMultiplier = _sequence.IsAttacking ? weapon.MoveMultiplierWhileAttacking : 1f;
        }

        AttackTimings CurrentTimings()
        {
            var t = weapon.Timings;
            t.Steps = _comboSteps > 0 ? _comboSteps : t.Steps;
            return t;
        }

        // ------------------------------------------------------- terremoto

        void StepCharge(float dt, bool grounded)
        {
            _player.SpeedMultiplier = 0f;
            if (!grounded) { _charging = false; _player.SpeedMultiplier = 1f; return; } // caiu de uma borda: perde a carga
            _holdTime += dt;
            // a marreta brilha: faíscas douradas subindo da cabeça da marreta, mais rápidas com a carga
            _glintTimer -= dt;
            if (_glintTimer <= 0f)
            {
                float c = Charge01;
                _glintTimer = Mathf.Lerp(0.18f, 0.06f, c);
                Vector2 feet = _player.Body.position;
                HitSparks.Glint(feet + new Vector2(-AttackDirection * 0.45f + Random.Range(-0.2f, 0.2f), 2.05f + Random.Range(-0.1f, 0.15f)));
            }
            if (input == null || !input.AttackHeld)
            {
                _slamCharge = Charge01;
                _charging = false;
                _slam = Slam.Windup;
                _slamTimer = weapon.SlamWindup;
                if (_slamTimer <= 0f) Impact();
            }
        }

        void StepSlam(float dt)
        {
            _player.SpeedMultiplier = 0f;
            _slamTimer -= dt;
            if (_slamTimer > 0f) return;
            if (_slam == Slam.Windup) { Impact(); return; }
            _slam = Slam.None;
            _player.SpeedMultiplier = 1f;
        }

        void Impact()
        {
            _slam = Slam.Recovery;
            _slamTimer = weapon.SlamRecovery;
            Shockwave(_slamCharge);
        }

        /// <summary>A onda corre pelo chão para os dois lados: dano, derruba, faz muito barulho. Não quebra parede rachada.</summary>
        public void Shockwave(float charge01)
        {
            Vector2 feet = _player.Body.position;
            float range = ChargeRules.Range(charge01, weapon.ShockwaveRange);
            int damage = ChargeRules.Damage(charge01, weapon.ShockwaveDamage);

            _overlaps.Clear();
            Physics2D.OverlapBox(feet + new Vector2(0f, weapon.ShockwaveHeight * 0.5f), new Vector2(range * 2f, weapon.ShockwaveHeight),
                0f, _filter, _overlaps);
            _hitThisSwing.Clear();
            foreach (var col in _overlaps)
            {
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target is SeteVidas.World.BreakableWall || !_hitThisSwing.Add(target)) continue;
                var b = col.bounds;
                if (!ChargeRules.InShockwave(feet, range, new Rect(b.min.x, b.min.y, b.size.x, b.size.y))) continue;
                int side = b.center.x >= feet.x ? 1 : -1;
                var info = new DamageInfo(damage, new Vector2(side, 0f), DamageType.Heavy, AttackAngle.Front, gameObject);
                target.TakeDamage(info);
                if (target is IKnockdown down) down.Knockdown(weapon.KnockdownTime);
                Hit?.Invoke(target, info);
            }

            NoiseSystem.Emit(feet, weapon.ShockwaveNoiseRadius, 1f, gameObject, "terremoto");
            CameraShake.Shake(weapon.ShockwaveShake.x, weapon.ShockwaveShake.y);
            HitSparks.Wave(feet, range, 0.25f);
            Hitstop.Trigger(0.08f);
            ShockwaveReleased?.Invoke(charge01);
        }

        // ------------------------------------------------------------ golpes

        /// <summary>Centro e tamanho da hitbox no mundo, para o lado <see cref="AttackDirection"/>.</summary>
        public void GetHitbox(out Vector2 center, out Vector2 size)
        {
            Vector2 feet = _player != null && _player.Body != null ? _player.Body.position : (Vector2)transform.position;
            float height = _player != null ? _player.CurrentHeight : 1.375f;
            float halfWidth = _player != null && _player.Settings != null ? _player.Settings.ColliderWidth * 0.5f : 0.375f;
            size = weapon.HitboxSize;
            center = new Vector2(feet.x + AttackDirection * (halfWidth + weapon.HitboxForward),
                                 feet.y + height * 0.5f);
        }

        void CheckHits()
        {
            GetHitbox(out var center, out var size);
            _overlaps.Clear();
            Physics2D.OverlapBox(center, size, 0f, _filter, _overlaps);

            bool finisher = IsFinisher;
            float stop = 0f;
            foreach (var col in _overlaps)
            {
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !_hitThisSwing.Add(target)) continue;

                var info = weapon.ResolveHit(_player.Body.position, (Component)target, gameObject);
                if (finisher)
                    info = new DamageInfo(ComboRules.FinisherDamage(info.Amount, weapon.FinisherDamageMultiplier),
                        info.Direction, info.Type, info.Angle, info.Source);
                target.TakeDamage(info);
                Hit?.Invoke(target, info);
                stop = Mathf.Max(stop, weapon.HitstopFor(info.Angle) * (finisher ? 1.5f : 1f));
            }
            if (stop > 0f) Hitstop.Trigger(stop);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (weapon == null) return;
            if (_player == null) _player = GetComponent<PlayerController>();
            GetHitbox(out var c, out var s);
            Gizmos.color = Application.isPlaying && IsHitActive ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(c, s);
        }
#endif
    }
}
