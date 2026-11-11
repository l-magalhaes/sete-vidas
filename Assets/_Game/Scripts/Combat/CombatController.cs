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

        public bool IsAttacking => _sequence.IsAttacking || _charging || _slam != Slam.None || _down != Down.None;
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
        void OnDamaged(DamageInfo _)
        {
            _charging = false;
            bool heavy = IsHeavyDrop;
            EndDown(); // cancela o ataque para baixo
            // o recuo do dano já travou o controle; o fim da queda pesada soltou a trava: devolve a do recuo
            if (heavy && _player != null) _player.LockHorizontalControl(_player.Settings.KnockbackControlLock);
        }

        bool CanAttack => !_player.IsHanging && !_player.IsClimbingLedge && _player.ActiveAbility == null;

        void CancelAll()
        {
            EndDown();
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
            if (_down != Down.None) { StepDown(dt, grounded); return; }
            // no ar com baixo segurado: estocada para baixo (adaga) ou queda pesada (marreta)
            if (input != null && input.AttackPressedThisFrame && !_sequence.IsAttacking && weapon.DownAttack &&
                DownAttackRules.WantsDownAttack(grounded, input.MoveY, input.CrouchHeld))
            {
                StartDown();
                return;
            }
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
            float range = ChargeRules.Range(charge01, weapon.ShockwaveRange);
            Quake(range, ChargeRules.Damage(charge01, weapon.ShockwaveDamage), weapon.KnockdownTime,
                weapon.ShockwaveNoiseRadius, weapon.ShockwaveShake.x, weapon.ShockwaveShake.y, 0.25f);
            ShockwaveReleased?.Invoke(charge01);
        }

        /// <summary>Tremor no chão em volta do gato: só pega quem está de pé no mesmo chão; não quebra parede rachada.</summary>
        void Quake(float range, int damage, float knockdown, float noiseRadius, float shakePx, float shakeTime, float waveTime)
        {
            Vector2 feet = _player.Body.position;
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
                if (knockdown > 0f && target is IKnockdown down) down.Knockdown(knockdown);
                Hit?.Invoke(target, info);
            }

            NoiseSystem.Emit(feet, noiseRadius, 1f, gameObject, "terremoto");
            CameraShake.Shake(shakePx, shakeTime);
            HitSparks.Wave(feet, range, waveTime);
            Hitstop.Trigger(0.08f);
        }

        // ------------------------------------------------ ataque para baixo (melhoria 5)

        enum Down { None, Windup, Active, Recovery }
        Down _down;
        float _downTimer;
        bool _downHeavy;

        /// <summary>No ataque para baixo no ar (estocada da adaga ou queda pesada da marreta).</summary>
        public bool IsDownAttacking => _down != Down.None;
        /// <summary>Queda pesada da marreta (e não a estocada da adaga).</summary>
        public bool IsHeavyDrop => _down != Down.None && _downHeavy;
        /// <summary>Progresso para a animação de 3 quadros: preparar (0), golpe ou queda (1), impacto ou quique (2).</summary>
        public float DownProgress => _down switch
        {
            Down.Windup => 0.1f,
            Down.Active => 0.5f,
            Down.Recovery => 0.9f,
            _ => 0f,
        };
        /// <summary>O rastro do golpe para baixo está no ar: na estocada e na queda (e um instante no impacto da marreta).</summary>
        public bool IsDownHitActive => _down == Down.Active || (_downHeavy && _down == Down.Recovery && _downTimer > weapon.DropRecovery - 0.1f);

        /// <summary>Caixa do golpe para baixo no mundo (é também onde o rastro é desenhado).</summary>
        public Rect DownHitbox => DownAttackRules.StabBox(_player.Body.position, weapon.DownHitbox, weapon.DownHitboxBelow);

        /// <summary>A estocada quicou (testes e efeitos).</summary>
        public event System.Action Bounced;

        /// <summary>Começa o ataque para baixo sem o controle (testes e cenas). Só no ar e com a arma que o tem.</summary>
        public bool TryDownAttack()
        {
            if (!enabled || weapon == null || !weapon.DownAttack || _player.IsGrounded || IsAttacking || !CanAttack) return false;
            StartDown();
            return true;
        }

        void StartDown()
        {
            _down = Down.Windup;
            _downTimer = weapon.DownWindup;
            _downHeavy = weapon.HeavyDrop;
            AttackDirection = _player.FacingDirection;
            _hitThisSwing.Clear();
            if (_downHeavy)
            {
                // a marreta segura o gato no ar um instante e depois despenca reto, sem controle
                _player.Body.linearVelocity = new Vector2(0f, Mathf.Max(0f, _player.Body.linearVelocity.y * 0.3f));
                _player.LockHorizontalControl(10f);
            }
        }

        void EndDown()
        {
            if (_down == Down.None) return;
            _down = Down.None;
            if (_player == null) return;
            _player.GravityMultiplier = 1f;
            if (_downHeavy) _player.ReleaseHorizontalLock();
        }

        void StepDown(float dt, bool grounded)
        {
            _downTimer -= dt;
            switch (_down)
            {
                case Down.Windup:
                    if (_downHeavy) _player.Body.linearVelocity = new Vector2(0f, _player.Body.linearVelocity.y);
                    if (_downTimer > 0f) return;
                    _down = Down.Active;
                    _downTimer = _downHeavy ? 3f : weapon.DownActive; // a queda pesada vai até tocar algo (3 s no máximo)
                    if (_downHeavy) _player.GravityMultiplier = weapon.DropGravityMultiplier;
                    SwingStarted?.Invoke();
                    break;

                case Down.Active:
                    if (_downHeavy)
                    {
                        _player.Body.linearVelocity = new Vector2(0f, _player.Body.linearVelocity.y);
                        if (DownHits(weapon.DropHitDamage, DamageType.Heavy) > 0)
                        {
                            // caiu em cima de alguém: pancada forte e um pulinho para fora
                            _player.Bounce(0.8f);
                            EnterDownRecovery(weapon.DropRecovery);
                            return;
                        }
                        if (grounded) { HeavyLanding(); return; }
                    }
                    else
                    {
                        if (grounded) { EndDown(); return; } // tocou o chão sem acertar nada
                        int hits = DownHits(-1, weapon.Type);
                        bool pogo = PogoSurfaces.Touches(DownAttackRules.StabBox(_player.Body.position, weapon.DownHitbox, weapon.DownHitboxBelow));
                        if (DownAttackRules.Bounces(true, hits > 0, pogo))
                        {
                            if (pogo && _health != null) _health.GrantPogoGrace(weapon.PogoGrace);
                            _player.Bounce(_player.Settings.JumpHeight * weapon.PogoHeightFraction);
                            Bounced?.Invoke();
                            if (pogo) HitSparks.Spawn(_player.Body.position + Vector2.down * 0.2f, 3, AttackDirection, false, _player.Body.position);
                            EndDown(); // pode emendar outra estocada logo em seguida
                            return;
                        }
                    }
                    if (_downTimer <= 0f) EnterDownRecovery(weapon.DownRecovery);
                    break;

                case Down.Recovery:
                    if (_downTimer <= 0f) EndDown();
                    break;
            }
        }

        void EnterDownRecovery(float seconds)
        {
            _player.GravityMultiplier = 1f;
            _down = Down.Recovery;
            _downTimer = seconds;
        }

        /// <summary>Acerta quem está na caixa embaixo dos pés. Dano negativo = o dano normal da arma.</summary>
        int DownHits(int damage, DamageType type)
        {
            var box = DownAttackRules.StabBox(_player.Body.position, weapon.DownHitbox, weapon.DownHitboxBelow);
            _overlaps.Clear();
            Physics2D.OverlapBox(box.center, box.size, 0f, _filter, _overlaps);
            int count = 0;
            foreach (var col in _overlaps)
            {
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !_hitThisSwing.Add(target)) continue;
                var resolved = weapon.ResolveHit(_player.Body.position, (Component)target, gameObject);
                var info = new DamageInfo(damage >= 0 ? damage : resolved.Amount, Vector2.down, type, resolved.Angle, gameObject);
                target.TakeDamage(info);
                Hit?.Invoke(target, info);
                count++;
            }
            if (count > 0) Hitstop.Trigger(weapon.HitstopFront);
            return count;
        }

        /// <summary>A marreta tocou o chão: mini terremoto, quebra a plataforma frágil embaixo, poeira.</summary>
        void HeavyLanding()
        {
            Vector2 feet = _player.Body.position;
            var under = Physics2D.OverlapBox(feet + Vector2.down * 0.1f, new Vector2(_player.Settings.ColliderWidth, 0.2f), 0f,
                LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable));
            var platform = under != null ? under.GetComponentInParent<SeteVidas.World.Traps.CrumblingPlatform>() : null;
            if (platform != null && !platform.IsFallen) platform.Fall();
            Quake(weapon.DropImpactRange, weapon.DropImpactDamage, 0f, weapon.ShockwaveNoiseRadius * 0.6f, 2f, 0.15f, 0.12f);
            EnterDownRecovery(weapon.DropRecovery);
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
