using System;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// O Golden, guardião do tomo (spec: Docs/Design/chefe-golden.md). Golpes:
    /// marretada de perto; salto com marretada (passar por baixo); cura concentrada (interromper batendo);
    /// chuva de relâmpagos (ficar onde não há aviso). Na 2ª fase (metade da vida) fica mais rápido e solta
    /// mais raios. Derrotado, ajoelha; depois da maldição do tomo, levanta como companheiro.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class GoldenBoss : MonoBehaviour, IDamageable, IFacingTarget
    {
        public enum State
        {
            Dormant, Idle, Walk,
            SwingWindup, SwingActive, SwingRecover,
            LeapWindup, LeapAir, LeapRecover,
            Heal, Stagger,
            LightningCast, Lightning,
            Defeated, Companion
        }

        [SerializeField] GoldenBossSettings settings;
        [Tooltip("Limites da arena (X no mundo): o salto e os raios ficam dentro deles")]
        [SerializeField] float arenaMinX = 180f;
        [SerializeField] float arenaMaxX = 211f;
        [Tooltip("Altura da coluna dos relâmpagos (do chão ao teto, tiles)")]
        [SerializeField] float lightningHeight = 9f;
        [SerializeField] LightningStrike lightningPrefab;
        [SerializeField] LayerMask playerLayers;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite idle;
        [SerializeField] Sprite[] walk;
        [SerializeField] Sprite swingWindup;
        [SerializeField] Sprite swing;
        [SerializeField] Sprite leapCrouch;
        [SerializeField] Sprite leapAir;
        [SerializeField] Sprite heal;
        [SerializeField] Sprite cast;
        [SerializeField] Sprite kneel;
        [Tooltip("Brilho dourado da cura")]
        [SerializeField] SpriteRenderer healAura;

        Rigidbody2D _body;
        BoxCollider2D _box;
        Vector2 _spawn;
        int _facing = -1;
        float _timer;
        float _sinceHeal = 999f, _sinceLightning;
        float _healAccum;
        int _healDamageTaken;
        Vector2 _leapFrom;
        float _leapTargetX;
        int _wavesLeft;
        float _blinkUntil;
        bool _swingHit;
        readonly List<LightningStrike> _strikes = new List<LightningStrike>();

        public static GoldenBoss Active { get; private set; }

        public State Current { get; private set; } = State.Dormant;
        public int Health { get; private set; }
        public int MaxHealth => settings != null ? settings.MaxHealth : 1;
        public bool IsFighting => Current != State.Dormant && Current != State.Defeated && Current != State.Companion;
        public bool IsDefeated => Current == State.Defeated || Current == State.Companion;
        public bool InSecondPhase => Health <= MaxHealth * settings.SecondPhaseAt;
        public GoldenBossSettings Settings => settings;
        public int FacingDirection => _facing;
        public bool InCombat => true; // guardião sempre alerta: pelas costas vale 1,5×, nunca furtivo

        /// <summary>Vida chegou a zero.</summary>
        public event Action Defeated;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _spawn = _body.position;
            Health = MaxHealth;
            if (playerLayers == 0) playerLayers = LayerMask.GetMask(GameConstants.Layers.Player);
            if (healAura != null) healAura.enabled = false;
            _box.enabled = false; // só sólido para golpes durante a luta
        }

        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        // ------------------------------------------------------------ API

        /// <summary>Começa a luta (o BossArena chama depois da conversa de entrada).</summary>
        public void BeginFight()
        {
            if (IsDefeated) return;
            Health = MaxHealth;
            _sinceHeal = 999f;
            _sinceLightning = 0f;
            Enter(State.Idle, 1f);
        }

        /// <summary>O gato morreu ou descansou: volta ao início, vida cheia, esperando.</summary>
        public void ResetToStart()
        {
            if (IsDefeated) return;
            ClearStrikes();
            Health = MaxHealth;
            _body.position = _spawn;
            transform.position = _spawn;
            _facing = -1;
            Enter(State.Dormant, 0f);
        }

        /// <summary>Já derrotado (save carregado): ajoelhado no lugar de início.</summary>
        public void SetDefeatedPose()
        {
            ClearStrikes();
            Health = 0;
            _body.position = _spawn;
            transform.position = _spawn;
            Enter(State.Defeated, 0f);
        }

        /// <summary>Levanta como companheiro em <paramref name="feet"/>, olhando para <paramref name="facing"/>.</summary>
        public void StandAsCompanion(Vector2 feet, int facing)
        {
            ClearStrikes();
            _body.position = feet;
            transform.position = feet;
            _facing = facing >= 0 ? 1 : -1;
            Enter(State.Companion, 0f);
        }

        // --------------------------------------------------------- máquina

        void FixedUpdate()
        {
            if (settings == null) return;
            float dt = Time.fixedDeltaTime;
            _sinceHeal += dt;
            _sinceLightning += dt;
            _timer -= dt;
            _strikes.RemoveAll(s => s == null);

            switch (Current)
            {
                case State.Idle:
                    FacePlayer();
                    if (_timer <= 0f) ChooseMove();
                    break;

                case State.Walk:
                    FacePlayer();
                    if (DistanceToPlayer() <= settings.SwingRange) { Enter(State.SwingWindup, settings.SwingWindup); break; }
                    MoveX(_facing * settings.WalkSpeed * Speed * dt);
                    if (_timer <= 0f) Enter(State.Idle, 0f);
                    break;

                case State.SwingWindup:
                    if (_timer <= 0f) { _swingHit = false; Enter(State.SwingActive, settings.SwingActive); }
                    break;
                case State.SwingActive:
                    if (!_swingHit) _swingHit = HitPlayer(SwingCenter(), settings.SwingHitbox);
                    if (_timer <= 0f) Enter(State.SwingRecover, settings.SwingRecovery / Speed);
                    break;
                case State.SwingRecover:
                    if (_timer <= 0f) Enter(State.Idle, IdleTime);
                    break;

                case State.LeapWindup:
                    FacePlayer();
                    if (_timer <= 0f)
                    {
                        _leapFrom = _body.position;
                        _leapTargetX = Mathf.Clamp(PlayerX(), arenaMinX + settings.BodySize.x, arenaMaxX - settings.BodySize.x);
                        Enter(State.LeapAir, settings.LeapAirTime);
                    }
                    break;
                case State.LeapAir:
                    StepLeap();
                    break;
                case State.LeapRecover:
                    if (_timer <= 0f) Enter(State.Idle, IdleTime);
                    break;

                case State.Heal:
                    _healAccum += settings.HealPerSecond * dt;
                    while (_healAccum >= 1f && Health < MaxHealth) { Health++; _healAccum -= 1f; }
                    if (_timer <= 0f || Health >= MaxHealth) { _sinceHeal = 0f; Enter(State.Idle, IdleTime); }
                    break;
                case State.Stagger:
                    if (_timer <= 0f) Enter(State.Idle, IdleTime);
                    break;

                case State.LightningCast:
                    if (_timer <= 0f)
                    {
                        _wavesLeft = settings.LightningWaves + (InSecondPhase ? 1 : 0);
                        Enter(State.Lightning, 0f);
                    }
                    break;
                case State.Lightning:
                    if (_timer <= 0f)
                    {
                        if (_wavesLeft <= 0) { _sinceLightning = 0f; Enter(State.Idle, IdleTime); break; }
                        SpawnWave();
                        _wavesLeft--;
                        _timer = settings.LightningTelegraph + settings.LightningStrikeTime + settings.LightningWaveGap;
                    }
                    break;
            }
        }

        float Speed => InSecondPhase ? 1.35f : 1f;
        float IdleTime => settings.IdleBetween * (InSecondPhase ? 0.6f : 1f);

        void ChooseMove()
        {
            var s = new BossSituation
            {
                DistanceToPlayer = DistanceToPlayer(),
                HealthFraction = Health / (float)MaxHealth,
                HealReady = _sinceHeal >= settings.HealCooldown,
                LightningReady = _sinceLightning >= settings.LightningCooldown,
                SwingRange = settings.SwingRange,
                HealBelowFraction = settings.HealBelow,
            };
            ForceMove(BossRules.Pick(s, UnityEngine.Random.value));
        }

        /// <summary>Começa este golpe agora (a escolha normal é sorteada). Usado pelos testes e pelo menu de depuração.</summary>
        public void ForceMove(BossMove move)
        {
            if (!IsFighting) return;
            switch (move)
            {
                case BossMove.Swing: Enter(State.SwingWindup, settings.SwingWindup / Speed); break;
                case BossMove.Leap: Enter(State.LeapWindup, settings.LeapWindup / Speed); break;
                case BossMove.Heal:
                    _healAccum = 0f;
                    _healDamageTaken = 0;
                    Enter(State.Heal, settings.HealDuration);
                    break;
                case BossMove.Lightning: Enter(State.LightningCast, settings.LightningCastTime); break;
                default: Enter(State.Walk, settings.WalkTime); break;
            }
        }

        void StepLeap()
        {
            float t = 1f - Mathf.Clamp01(_timer / settings.LeapAirTime);
            float x = Mathf.Lerp(_leapFrom.x, _leapTargetX, t);
            float y = _spawn.y + 4f * settings.LeapHeight * t * (1f - t); // parábola: topo no meio
            _body.MovePosition(new Vector2(x, y));
            if (t < 1f) return;

            _body.position = new Vector2(_leapTargetX, _spawn.y);
            var center = new Vector2(_leapTargetX, _spawn.y + 0.75f);
            HitPlayer(center, new Vector2(settings.LeapImpactHalfWidth * 2f, 1.5f));
            NoiseSystem.Emit(center, 8f, 0.6f, gameObject, "marretada");
            Enter(State.LeapRecover, settings.LeapRecovery / Speed);
        }

        void SpawnWave()
        {
            if (lightningPrefab == null) return;
            int count = settings.StrikesPerWave + (InSecondPhase ? 1 : 0);
            var columns = BossRules.LightningColumns(arenaMinX, arenaMaxX, count, settings.LightningWidth,
                settings.LightningSafeGap, PlayerX(), () => UnityEngine.Random.value);
            foreach (float x in columns)
            {
                var strike = Instantiate(lightningPrefab);
                strike.Init(new Vector2(x, _spawn.y), settings.LightningWidth, lightningHeight,
                    settings.LightningTelegraph, settings.LightningStrikeTime, settings.Damage, gameObject);
                _strikes.Add(strike);
            }
        }

        void ClearStrikes()
        {
            foreach (var s in _strikes) if (s != null) Destroy(s.gameObject);
            _strikes.Clear();
        }

        void Enter(State state, float time)
        {
            Current = state;
            _timer = time;
            if (_box != null) _box.enabled = state != State.Dormant && state != State.Defeated && state != State.Companion;
            if (healAura != null) healAura.enabled = state == State.Heal;
        }

        // ------------------------------------------------------------ dano

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsFighting || info.Amount <= 0) return;
            Health = Mathf.Max(0, Health - info.Amount);
            _blinkUntil = Time.time + 0.15f;
            DamagePopup.Spawn(transform.position + Vector3.up * 2.4f, DamagePopup.DamageLabel(info), DamagePopup.ColorFor(info.Angle));

            if (Health <= 0)
            {
                ClearStrikes();
                Enter(State.Defeated, 0f);
                _body.position = new Vector2(_body.position.x, _spawn.y); // cai do salto, se estiver no ar
                Defeated?.Invoke();
                return;
            }

            if (Current == State.Heal)
            {
                _healDamageTaken += info.Amount;
                if (_healDamageTaken >= settings.HealInterruptDamage)
                {
                    _sinceHeal = 0f;
                    Enter(State.Stagger, settings.StaggerTime);
                    DamagePopup.Spawn(transform.position + Vector3.up * 3f, "interrompido!", new Color32(0xff, 0xd3, 0x4d, 0xff));
                }
            }
        }

        // ---------------------------------------------------------- ajudas

        float PlayerX()
        {
            var p = PlayerHealth.Active;
            return p != null ? p.transform.position.x : _body.position.x;
        }

        float DistanceToPlayer() => Mathf.Abs(PlayerX() - _body.position.x);

        void FacePlayer()
        {
            float dx = PlayerX() - _body.position.x;
            if (Mathf.Abs(dx) > 0.1f) _facing = dx > 0f ? 1 : -1;
        }

        void MoveX(float dx)
        {
            float half = settings.BodySize.x * 0.5f;
            float x = Mathf.Clamp(_body.position.x + dx, arenaMinX + half, arenaMaxX - half);
            _body.MovePosition(new Vector2(x, _spawn.y));
        }

        Vector2 SwingCenter() =>
            new Vector2(_body.position.x + _facing * (settings.BodySize.x * 0.5f + settings.SwingHitbox.x * 0.5f), _spawn.y + 0.8f);

        bool HitPlayer(Vector2 center, Vector2 size)
        {
            var col = Physics2D.OverlapBox(center, size, 0f, playerLayers);
            if (col == null) return false;
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null) return false;
            float dir = col.transform.position.x >= _body.position.x ? 1f : -1f;
            target.TakeDamage(new DamageInfo(settings.Damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, gameObject));
            return true;
        }

        // ---------------------------------------------------------- visual

        void LateUpdate()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.flipX = _facing < 0;
            spriteRenderer.sprite = Current switch
            {
                State.Walk => walk != null && walk.Length > 0 ? walk[(int)(Time.time * 6f) % walk.Length] : idle,
                State.SwingWindup => swingWindup,
                State.SwingActive or State.SwingRecover => swing,
                State.LeapWindup => leapCrouch,
                State.LeapAir => leapAir,
                State.LeapRecover => swing,
                State.Heal or State.Stagger => heal,
                State.LightningCast or State.Lightning => cast,
                State.Defeated => kneel,
                _ => idle,
            } ?? idle;
            spriteRenderer.enabled = Time.time >= _blinkUntil || ((int)(Time.time * 30f) & 1) == 0;
            if (Current == State.Stagger) spriteRenderer.color = ((int)(Time.time * 8f) & 1) == 0 ? Color.white : new Color(1f, 0.85f, 0.6f);
            else spriteRenderer.color = Color.white;

            if (healAura != null && healAura.enabled)
            {
                float pulse = 0.35f + 0.25f * Mathf.Sin(Time.time * 6f);
                var c = healAura.color; c.a = pulse; healAura.color = c;
            }
        }

        [ContextMenu("Depuração: salto")] void DebugLeap() => ForceMove(BossMove.Leap);
        [ContextMenu("Depuração: cura")] void DebugHeal() => ForceMove(BossMove.Heal);
        [ContextMenu("Depuração: relâmpagos")] void DebugLightning() => ForceMove(BossMove.Lightning);
        [ContextMenu("Depuração: marretada")] void DebugSwing() => ForceMove(BossMove.Swing);

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y), new Vector3(arenaMinX, transform.position.y + lightningHeight));
            Gizmos.DrawLine(new Vector3(arenaMaxX, transform.position.y), new Vector3(arenaMaxX, transform.position.y + lightningHeight));
        }
#endif
    }
}
