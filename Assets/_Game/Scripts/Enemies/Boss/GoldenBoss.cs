using System;
using System.Collections;
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
    public class GoldenBoss : MonoBehaviour, IDamageable, IFacingTarget, ISolidBody
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
        [Tooltip("Animações do Koda (KodaAnimationSet, gerado por Tools/CharacterSprites). Vazio = sprites acima")]
        [SerializeField] CharacterAnimationSet animations;
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
        Sprite _poseOverride;
        string _poseClip;
        float _poseTime, _stateTime, _stateDuration;
        int _combo;               // marretadas seguidas neste combo
        SpriteRenderer _shield;   // bolha dourada dos relâmpagos
        float _shieldFlashUntil, _nextShieldPopup;
        readonly List<LightningStrike> _strikes = new List<LightningStrike>();

        public static GoldenBoss Active { get; private set; }

        // o dourado do Paladino (o mesmo da marreta do gato e do HUD)
        internal static readonly Color32 Gold = new Color32(0xf2, 0xc9, 0x6b, 0xff);
        internal static readonly Color32 ShieldLight = new Color32(0xff, 0xf4, 0xc2, 0xff);

        // ---- para os efeitos (BossFx) e os testes
        /// <summary>Pés do Koda (centro da base do corpo).</summary>
        public Vector2 Feet => _body != null ? _body.position : (Vector2)transform.position;
        /// <summary>Altura do chão da arena.</summary>
        public float GroundY => _spawn.y;
        /// <summary>Centro da área da marretada, à frente do corpo.</summary>
        public Vector2 SwingPoint => settings != null ? SwingCenter() : Feet;
        /// <summary>Onde o salto vai cair (vale a partir do fim da preparação).</summary>
        public float LeapTargetX => _leapTargetX;
        /// <summary>Tempo no estado atual e quanto ele dura (0 = sem fim marcado).</summary>
        public float StateTime => _stateTime;
        public float StateDuration => _stateDuration;

        public State Current { get; private set; } = State.Dormant;
        public int Health { get; private set; }
        public int MaxHealth => settings != null ? settings.MaxHealth : 1;
        public bool IsFighting => Current != State.Dormant && Current != State.Defeated && Current != State.Companion;
        public bool IsDefeated => Current == State.Defeated || Current == State.Companion;
        public bool InSecondPhase => Health <= MaxHealth * settings.SecondPhaseAt;
        public GoldenBossSettings Settings => settings;
        public int FacingDirection => _facing;
        public bool InCombat => true; // guardião sempre alerta: pelas costas vale 1,5×, nunca furtivo

        /// <summary>Escudo sagrado: enquanto chama e lança os relâmpagos, nenhum golpe passa (de nenhum ângulo).</summary>
        public bool IsShielded => settings != null && settings.ShieldDuringLightning
                                  && (Current == State.LightningCast || Current == State.Lightning);

        /// <summary>Vida chegou a zero.</summary>
        public event Action Defeated;
        /// <summary>Mudou de estado (anterior, novo). Usado pelos sons.</summary>
        public event Action<State, State> StateChanged;

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
            _shield = BuildShield();
            if (GetComponent<SeteVidas.Audio.BossAudio>() == null) gameObject.AddComponent<SeteVidas.Audio.BossAudio>(); // sons (só observa)
            if (GetComponent<BossFx>() == null) gameObject.AddComponent<BossFx>(); // efeitos dos golpes (só observa)
        }

        void OnEnable() { Active = this; SolidBodies.Register(this); }
        void OnDisable() { if (Active == this) Active = null; SolidBodies.Unregister(this); }

        // corpo sólido na luta (melhoria 6): encostar nele só para o gato, sem dano; o Koda não reage ao esbarrão
        public Rect Bounds => SolidBodies.RectOf(_box);
        public bool IsSolid => IsFighting && _box != null && _box.enabled;
        public void OnBumped(Vector2 playerPosition) { }

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

        /// <summary>
        /// Derrotado, Koda ergue a marreta e a crava no chão para segurar a última vida do gato (cena do tomo).
        /// Só muda a pose por um instante; ele continua de joelhos depois.
        /// </summary>
        public IEnumerator SealStrike(float windup = 0.35f, float hold = 0.45f)
        {
            if (Current != State.Defeated) yield break;
            _poseOverride = swingWindup; _poseClip = "seal_windup"; _poseTime = 0f;
            yield return new WaitForSeconds(windup);
            _poseOverride = swing; _poseClip = "seal_strike"; _poseTime = 0f;
            SeteVidas.Audio.Sfx.Play("selo_golpe");
            if (healAura != null) healAura.enabled = true;   // brilho dourado do golpe
            yield return new WaitForSeconds(hold);
            if (healAura != null) healAura.enabled = false;
            _poseOverride = null; _poseClip = null;
        }

        /// <summary>Animação mostrada neste quadro (testes e depuração; vazio sem <c>animations</c>).</summary>
        public string CurrentClipName { get; private set; } = "";

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
                    if (DistanceToPlayer() <= settings.SwingRange) { StartSwing(false); break; }
                    MoveX(_facing * settings.WalkSpeed * Speed * dt);
                    if (_timer <= 0f) Enter(State.Idle, 0f);
                    break;

                case State.SwingWindup:
                    if (_timer <= 0f) { _swingHit = false; Enter(State.SwingActive, settings.SwingActive); }
                    break;
                case State.SwingActive:
                    if (!_swingHit) _swingHit = HitPlayer(SwingCenter(), settings.SwingHitbox, settings.SwingDamage);
                    if (_timer <= 0f) Enter(State.SwingRecover, settings.SwingRecovery / Speed);
                    break;
                case State.SwingRecover:
                    if (_timer > 0f) break;
                    // agressivo: se o gato ficou perto, emenda outra marretada (até 3 seguidas)
                    if (_combo < 3 && DistanceToPlayer() <= settings.SwingRange + 0.4f
                        && UnityEngine.Random.value < settings.ComboChance) { FacePlayer(); StartSwing(true); }
                    else Enter(State.Idle, IdleTime);
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
                    if (_timer > 0f) break;
                    // caiu do lado do gato: já levanta batendo
                    if (DistanceToPlayer() <= settings.SwingRange) { FacePlayer(); StartSwing(false); }
                    else Enter(State.Idle, IdleTime);
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

        float Speed => InSecondPhase ? settings.SecondPhaseSpeed : 1f;
        float IdleTime => settings.IdleBetween * (InSecondPhase ? 0.6f : 1f);

        /// <summary>Marretada. No combo, a preparação é mais curta (o gato já viu a primeira).</summary>
        void StartSwing(bool chained)
        {
            _combo = chained ? _combo + 1 : 1;
            Enter(State.SwingWindup, settings.SwingWindup / Speed * (chained ? 0.65f : 1f));
        }

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
                case BossMove.Swing: StartSwing(false); break;
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
            HitPlayer(center, new Vector2(settings.LeapImpactHalfWidth * 2f, 1.5f), settings.LeapDamage);
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
                    settings.LightningTelegraph, settings.LightningStrikeTime, settings.LightningDamage, gameObject);
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
            var previous = Current;
            Current = state;
            _timer = time;
            _stateTime = 0f;
            _stateDuration = time;
            if (_box != null) _box.enabled = state != State.Dormant && state != State.Defeated && state != State.Companion;
            if (healAura != null) healAura.enabled = state == State.Heal;
            if (previous != state) StateChanged?.Invoke(previous, state);
        }

        // ------------------------------------------------------------ dano

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsFighting || info.Amount <= 0) return;
            if (IsShielded)
            {
                // o golpe bate na bolha e não passa: nem pelas costas, nem de cima, nem de baixo
                _shieldFlashUntil = Time.time + 0.18f;
                SeteVidas.Audio.Sfx.Play("escudo_dong", transform.position + Vector3.up * 1.5f);
                if (Time.time >= _nextShieldPopup)
                {
                    // a bolha pisca e solta faíscas claras onde o golpe bateu (sem o texto "escudo!")
                    _nextShieldPopup = Time.time + 0.25f;
                    float hitSide = info.Direction.x >= 0f ? -1f : 1f;
                    Vector2 bubble = _body.position + new Vector2(hitSide * 1.6f, 1.6f);
                    HitSparks.Burst(bubble, 6, ShieldLight, Gold, 2f, 5f, 0.2f, 10f, 0.2f);
                    PixelRing.Spawn(bubble, 1, 5, 0.15f, ShieldLight);
                }
                return;
            }
            Health = Mathf.Max(0, Health - info.Amount);
            _blinkUntil = Time.time + 0.15f;
            HitReact(info);

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
                    // a cura se parte: a luz dourada explode para fora (sem o texto "interrompido!")
                    Vector2 chest = _body.position + Vector2.up * (settings.BodySize.y * 0.6f);
                    HitSparks.Burst(chest, 16, ShieldLight, Gold, 3f, 7f, 0.35f, 15f, 0.2f);
                    PixelRing.Spawn(chest, 4, 26, 0.3f, ShieldLight, 2);
                    PixelRing.Spawn(chest, 2, 16, 0.25f, Gold, 1, 0.08f);
                    CameraShake.Shake(2f, 0.15f);
                }
            }
        }

        /// <summary>
        /// Reação ao golpe (melhoria 3): clarão de 1 quadro, faíscas e um recuo de metade, só com os pés no chão
        /// e fora de um golpe. O chefe nunca é interrompido (só a cura, pela regra dela).
        /// </summary>
        void HitReact(in DamageInfo info)
        {
            var table = settings.Reactions ?? HitReactionTable.Boss();
            var reaction = table.For(HitReactionRules.WeightOf(info.Type, info.Angle));
            int side = HitReactionRules.KnockbackSide(info.Direction, FacingDirection);
            var flash = HitFlash.For(spriteRenderer);
            if (flash != null) flash.Flash(reaction.Flash);
            Vector2 feet = _body.position;
            HitSparks.Spawn(new Vector2(feet.x - side * settings.BodySize.x * 0.4f, feet.y + settings.BodySize.y * 0.6f),
                reaction.Sparks, side, reaction.Dust, feet);
            CameraShake.Shake(reaction.ShakePx, reaction.ShakeTime);

            bool planted = Current == State.Idle || Current == State.Walk || Current == State.SwingRecover ||
                           Current == State.LeapRecover || Current == State.Stagger;
            if (!planted) return;
            float push = reaction.Knockback * table.KnockbackScale * 0.06f; // ~0,1 a 0,2 tile
            float half = settings.BodySize.x * 0.5f;
            float x = Mathf.Clamp(feet.x + side * push, arenaMinX + half, arenaMaxX - half);
            _body.position = new Vector2(x, feet.y);
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

        bool HitPlayer(Vector2 center, Vector2 size, int damage)
        {
            var col = Physics2D.OverlapBox(center, size, 0f, playerLayers);
            if (col == null) return false;
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null) return false;
            float dir = col.transform.position.x >= _body.position.x ? 1f : -1f;
            target.TakeDamage(new DamageInfo(damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, gameObject));
            return true;
        }

        // ---------------------------------------------------------- visual

        void LateUpdate()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.flipX = _facing < 0;
            _stateTime += Time.deltaTime;
            _poseTime += Time.deltaTime;
            if (animations != null) ApplyAnimation();
            else spriteRenderer.sprite = Current switch
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
            if (animations == null && _poseOverride != null && Current == State.Defeated) spriteRenderer.sprite = _poseOverride;
            spriteRenderer.enabled = Time.time >= _blinkUntil || ((int)(Time.time * 30f) & 1) == 0;
            if (Current == State.Stagger) spriteRenderer.color = ((int)(Time.time * 8f) & 1) == 0 ? Color.white : new Color(1f, 0.85f, 0.6f);
            else spriteRenderer.color = Color.white;

            if (healAura != null && healAura.enabled)
            {
                // pulsa em dois degraus (sem fade contínuo: o jeito do pixel art)
                float pulse = ((int)(Time.time * 5f) & 1) == 0 ? 0.6f : 0.4f;
                var c = healAura.color; c.a = pulse; healAura.color = c;
            }
            UpdateShield();
        }

        /// <summary>
        /// Animações do Koda: golpes e salto seguem o tempo de cada fase (mudar os tempos no Inspector não
        /// dessincroniza a arte); os demais estados tocam em loop. Spec: SeteVidasDocs/Personagens.md §2.4.
        /// </summary>
        void ApplyAnimation()
        {
            string key; bool timed = false;
            switch (Current)
            {
                case State.Walk: key = "walk"; break;
                case State.SwingWindup: key = "swing_windup"; timed = true; break;
                case State.SwingActive: key = "swing_active"; timed = true; break;
                case State.SwingRecover: key = "swing_recover"; timed = true; break;
                case State.LeapWindup: key = "leap_windup"; timed = true; break;
                case State.LeapAir: key = "leap_air"; break;
                case State.LeapRecover: key = "leap_recover"; timed = true; break;
                case State.Heal: key = "heal"; break;
                case State.Stagger: key = "stagger"; break;
                case State.LightningCast: key = "cast"; break;
                case State.Lightning: key = "lightning"; break;
                case State.Defeated: key = "kneel"; break;
                case State.Companion: key = "companion"; break;
                default: key = "idle"; break;
            }
            SpriteClip clip;
            SpriteFrame frame;
            if (_poseClip != null && Current == State.Defeated && (clip = animations.Get(_poseClip)) != null)
                frame = clip.At(_poseTime);
            else
            {
                clip = animations.Get(key) ?? animations.Get("idle");
                if (clip == null) return;
                frame = timed && _stateDuration > 0f ? clip.AtNormalized(_stateTime / _stateDuration) : clip.At(_stateTime);
            }
            CurrentClipName = clip.name;
            if (frame != null && frame.sprite != null) spriteRenderer.sprite = frame.sprite;
        }

        // ---------------------------------------------------------- escudo

        /// <summary>
        /// Bolha dourada em pixel art (56×64 px, base nos pés): anel claro, treliça de runas por dentro e um brilho
        /// no alto à esquerda. Gerada em código para não depender de arquivo de arte.
        /// </summary>
        SpriteRenderer BuildShield()
        {
            const int W = 56, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "fx_koda_shield" };
            var px = new Color32[W * H];
            float cx = (W - 1) * 0.5f, cy = (H - 1) * 0.5f, rx = W * 0.5f - 0.5f, ry = H * 0.5f - 0.5f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x - cx) / rx, ny = (y - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (d > 1f) { }
                    else if (d > 0.94f) c = new Color32(0xff, 0xf2, 0xb8, 0xf0);                      // borda clara
                    else if (d > 0.87f) c = new Color32(0xff, 0xc9, 0x4d, 0xa0);                      // borda dourada
                    else if (d > 0.70f && d < 0.78f && nx < -0.25f && ny > 0.3f) c = new Color32(0xff, 0xff, 0xff, 0xa8); // brilho
                    else if ((x + y) % 7 == 0 || (x - y + 70) % 7 == 0) c = new Color32(0xff, 0xd8, 0x70, 0x50);       // runas
                    else c = new Color32(0xff, 0xe0, 0x80, 0x22);                                     // véu
                    px[y * W + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            var go = new GameObject("Escudo sagrado");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 16f);
            if (spriteRenderer != null) { sr.sortingLayerID = spriteRenderer.sortingLayerID; sr.sortingOrder = spriteRenderer.sortingOrder + 2; }
            sr.enabled = false;
            return sr;
        }

        void UpdateShield()
        {
            if (_shield == null) return;
            bool on = IsShielded;
            _shield.enabled = on;
            if (!on) return;
            // cresce ao surgir, pulsa, e pisca branco quando um golpe bate nele
            float grow = Mathf.Clamp01(_stateTime / 0.15f);
            if (Current == State.Lightning) grow = 1f;
            // cresce em três degraus e fica no tamanho exato do desenho (sem escala quebrada no pixel)
            float s = grow < 0.34f ? 0.6f : grow < 0.67f ? 0.8f : 1f;
            _shield.transform.localScale = new Vector3(s, s, 1f);
            bool flash = Time.time < _shieldFlashUntil;
            float a = flash ? 1f : ((int)(Time.time * 4f) & 1) == 0 ? 0.85f : 0.7f;
            _shield.color = flash ? new Color(1.4f, 1.4f, 1.4f, a) : new Color(1f, 1f, 1f, a);
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
