using SeteVidas.Audio;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Zombie
{
    /// <summary>
    /// Templário Zumbi (pedido do usuário, 12/11/2026; spec: Docs/Design/zumbi-templario.md). Os templários tomados
    /// pela praga depois que o gato pega o tomo. Sem lança nem besta: anda sempre para a frente, vira na parede e na
    /// beira, e ao sentir o gato (visão curta à frente, faro bem perto, barulho da furtividade) corre atrás dele mais
    /// rápido que um templário. O corpo não é sólido: encostar nele envenena (<see cref="PlayerHealth.Poison"/>).
    /// Nunca é pego de surpresa (sem golpe furtivo), mas leva dano dobrado pelas costas.
    /// Criado em tempo de jogo pelo <see cref="ZombieOutbreak"/> (ver <see cref="Spawn"/>).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class ZombieAI : MonoBehaviour, IDamageable, IFacingTarget, IKnockdown, IBackDamageMultiplier
    {
        public enum ZombieState { Wander, Alert, Chase, Dead }

        [SerializeField] ZombieSettings settings;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] CharacterAnimationSet animations;
        [SerializeField] LayerMask groundLayers;
        [SerializeField] LayerMask playerLayers;

        Rigidbody2D _body;
        BoxCollider2D _box;
        Vector2 _spawn;
        int _startFacing = 1, _facing = 1;
        bool _hasBounds;
        float _minX, _maxX;
        int _health;
        float _stateTime, _pauseTimer, _stunTimer, _lostTimer, _nextTouch, _nextStep, _nextGroan, _nextChaseSound;
        float _deathTime, _hurtTime = 99f, _blinkUntil;
        string _animKey;
        float _animTime;

        public ZombieState State { get; private set; } = ZombieState.Wander;
        public bool IsDead => State == ZombieState.Dead;
        public int Health => _health;
        public ZombieSettings Settings => settings;
        public int FacingDirection => _facing;
        public bool InCombat => true;                                    // nunca é pego de surpresa
        public float BackMultiplier => settings != null ? settings.BackMultiplier : 2f;
        public bool IsStunned => _stunTimer > 0f && !IsDead;
        public string CurrentClipName { get; private set; }

        /// <summary>Monta um zumbi (corpo, colisor na layer Enemy, desenho) no posto dado.</summary>
        public static ZombieAI Spawn(Transform parent, string name, Vector2 at, int facing, float patrolA, float patrolB,
                                     ZombieSettings settings, CharacterAnimationSet animations, PhysicsMaterial2D material)
        {
            if (settings == null) settings = ScriptableObject.CreateInstance<ZombieSettings>();
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = settings.GravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = settings.BodySize;
            box.offset = new Vector2(0f, settings.BodySize.y * 0.5f);
            if (material != null) box.sharedMaterial = material;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            var ai = go.AddComponent<ZombieAI>();
            ai.settings = settings;
            ai.animations = animations;
            ai.spriteRenderer = sr;
            ai.groundLayers = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            ai.playerLayers = LayerMask.GetMask(GameConstants.Layers.Player);
            ai._spawn = at;
            ai._startFacing = facing >= 0 ? 1 : -1;
            ai._hasBounds = !Mathf.Approximately(patrolA, patrolB);
            ai._minX = Mathf.Min(patrolA, patrolB);
            ai._maxX = Mathf.Max(patrolA, patrolB);
            go.SetActive(true);
            return ai;
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
        }

        void OnEnable()
        {
            GameEvents.WorldReset += Respawn;
            NoiseSystem.Emitted += OnNoise;
        }

        void OnDisable()
        {
            GameEvents.WorldReset -= Respawn;
            NoiseSystem.Emitted -= OnNoise;
        }

        void Start()
        {
            if (settings == null) settings = ScriptableObject.CreateInstance<ZombieSettings>();
            if (_spawn == Vector2.zero) _spawn = _body.position;
            Respawn();
        }

        /// <summary>Volta ao posto, vivo e vagando (descansar no santuário ou morrer).</summary>
        public void Respawn()
        {
            if (settings == null) return;
            _health = settings.MaxHealth;
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.position = _spawn;
            _body.linearVelocity = Vector2.zero;
            _box.enabled = true;
            _facing = _startFacing;
            _stunTimer = _pauseTimer = _lostTimer = 0f;
            _nextGroan = Random.Range(2f, 6f);
            Enter(ZombieState.Wander);
            if (spriteRenderer != null) { spriteRenderer.enabled = true; var c = spriteRenderer.color; c.a = 1f; spriteRenderer.color = c; }
        }

        void Enter(ZombieState s)
        {
            State = s;
            _stateTime = 0f;
        }

        // ------------------------------------------------------------- sentidos

        PlayerHealth Target => PlayerHealth.Active;

        bool SensesPlayer()
        {
            var p = Target;
            if (p == null || p.IsDead || p.IsCollapsed) return false;
            Vector2 me = _body.position, cat = p.transform.position;
            bool hidden = StealthController.Active != null && StealthController.Active.IsHidden;
            if (!ZombieRules.Senses(cat.x - me.x, cat.y - me.y, _facing, settings.SightRange, settings.CloseRange, settings.SenseHeight, hidden))
                return false;
            // parede no meio tapa a visão (o faro bem perto passa)
            Vector2 eye = me + Vector2.up * (settings.BodySize.y * 0.8f);
            Vector2 to = cat + Vector2.up * 0.6f - eye;
            return Mathf.Abs(cat.x - me.x) <= settings.CloseRange || !Physics2D.Raycast(eye, to.normalized, to.magnitude, groundLayers);
        }

        void OnNoise(NoiseEvent n)
        {
            if (IsDead || settings == null || n.Source == gameObject) return;
            var p = Target;
            if (p == null || n.Source == null || n.Source.GetComponentInParent<PlayerHealth>() != p) return;   // só o gato chama
            if (!ZombieRules.Hears(Vector2.Distance(n.Position, _body.position), n.Radius, settings.HearingMultiplier)) return;
            if (Mathf.Abs(n.Position.y - _body.position.y) > settings.SenseHeight + 1f) return;
            Notice(n.Position.x);
        }

        /// <summary>Fica sabendo onde o gato está (a horda da ponte já nasce correndo atrás dele).</summary>
        public void Alert(float x) { if (!IsDead) Notice(x); }

        void Notice(float x)
        {
            _lostTimer = 0f;
            if (State == ZombieState.Wander)
            {
                _facing = x >= _body.position.x ? 1 : -1;
                Enter(ZombieState.Alert);
                Sfx.Play("zumbi_sente", _body.position + Vector2.up);
                _nextChaseSound = 1f;
            }
        }

        // ------------------------------------------------------------ movimento

        void FixedUpdate()
        {
            if (settings == null) return;
            float dt = Time.fixedDeltaTime;
            _stateTime += dt;
            if (IsDead) return;

            TouchPlayer();

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                _body.linearVelocity = new Vector2(Mathf.MoveTowards(_body.linearVelocity.x, 0f, 12f * dt), _body.linearVelocity.y);
                return;
            }

            bool senses = SensesPlayer();
            if (senses) Notice(Target.transform.position.x);

            switch (State)
            {
                case ZombieState.Wander: StepWander(dt); break;
                case ZombieState.Alert:
                    SetSpeed(0f);
                    if (_stateTime >= settings.AlertTime) Enter(ZombieState.Chase);
                    break;
                case ZombieState.Chase:
                    if (!senses) _lostTimer += dt;
                    if (_lostTimer >= settings.LoseTime || Target == null || Target.IsDead) { Enter(ZombieState.Wander); break; }
                    StepChase(dt);
                    break;
            }
        }

        void StepWander(float dt)
        {
            if (_pauseTimer > 0f) { _pauseTimer -= dt; SetSpeed(0f); return; }
            if (ZombieRules.ShouldTurn(WallAhead(), GroundAhead(), _body.position.x, _facing, _hasBounds, _minX, _maxX))
            {
                _facing = -_facing;
                _pauseTimer = settings.TurnPause;
                SetSpeed(0f);
                return;
            }
            SetSpeed(_facing * settings.WanderSpeed);
            Footsteps(dt, 0.6f);
            _nextGroan -= dt;
            if (_nextGroan <= 0f) { _nextGroan = Random.Range(4f, 9f); Sfx.Play("zumbi_ocioso", _body.position + Vector2.up); }
        }

        void StepChase(float dt)
        {
            var p = Target;
            int dir = ZombieRules.ChaseDirection(_body.position.x, p.transform.position.x, 0.15f);
            if (dir != 0) _facing = dir;
            // corre, mas não se joga do andar: para na beira e na parede
            bool blocked = dir == 0 || WallAhead() || !GroundAhead();
            SetSpeed(blocked ? 0f : _facing * settings.ChaseSpeed);
            if (!blocked) Footsteps(dt, 0.3f);
            _nextChaseSound -= dt;
            if (_nextChaseSound <= 0f) { _nextChaseSound = Random.Range(1.4f, 2.2f); Sfx.Play("zumbi_caca", _body.position + Vector2.up); }
        }

        void Footsteps(float dt, float interval)
        {
            _nextStep -= dt;
            if (_nextStep > 0f) return;
            _nextStep = interval;
            Sfx.Play("zumbi_passo", _body.position);
        }

        void SetSpeed(float vx) => _body.linearVelocity = new Vector2(vx, _body.linearVelocity.y);

        bool WallAhead()
        {
            Vector2 origin = _body.position + new Vector2(0f, settings.BodySize.y * 0.5f);
            float reach = settings.BodySize.x * 0.5f + 0.15f;
            return Physics2D.Raycast(origin, new Vector2(_facing, 0f), reach, groundLayers);
        }

        bool GroundAhead()
        {
            Vector2 foot = _body.position + new Vector2(_facing * (settings.BodySize.x * 0.5f + 0.2f), 0.1f);
            return Physics2D.Raycast(foot, Vector2.down, 0.6f, groundLayers);
        }

        // ------------------------------------------------------------- o toque

        void TouchPlayer()
        {
            if (Time.time < _nextTouch) return;
            Vector2 center = _body.position + Vector2.up * (settings.BodySize.y * 0.5f);
            var col = Physics2D.OverlapBox(center, settings.ContactSize, 0f, playerLayers);
            if (col == null) return;
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || health.IsCollapsed) return;
            _nextTouch = Time.time + settings.ContactCooldown;
            health.Poison();
            // o toque também acorda o zumbi (ele sente o gato encostando)
            Notice(health.transform.position.x);
            Vector2 chest = (Vector2)health.transform.position + Vector2.up * 0.8f;
            HitSparks.Burst(chest, 6, new Color32(0x8f, 0xd1, 0x4f, 0xff), new Color32(0x5b, 0x2a, 0x6e, 0xff), 1.5f, 3.5f, 0.35f, 4f, 0.6f);
        }

        // -------------------------------------------------------------- dano

        // ------------------------------------------------------------ escudo (Infectado de escudo, Floresta F3)

        SpriteRenderer _shield;

        public bool HasShield => _shield != null;

        /// <summary>Dá um escudo (desenho na frente do corpo): bloqueia golpes comuns de frente (<see cref="ZombieRules.ShieldBlocks"/>).</summary>
        public void GiveShield(Sprite sprite)
        {
            if (_shield != null) return;
            var go = new GameObject("Escudo");
            go.transform.SetParent(spriteRenderer != null ? spriteRenderer.transform : transform, false);
            _shield = go.AddComponent<SpriteRenderer>();
            _shield.sprite = sprite;
            _shield.sortingOrder = 5;
            PlaceShield();
        }

        void PlaceShield()
        {
            if (_shield == null || settings == null) return;
            _shield.transform.localPosition = new Vector3(_facing * settings.BodySize.x * 0.55f, settings.BodySize.y * 0.45f, 0f);
            _shield.flipX = _facing < 0;
            _shield.enabled = !IsDead;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || info.Amount <= 0 || settings == null) return;
            if (ZombieRules.ShieldBlocks(HasShield, info.Type, info.Angle))
            {
                // bloqueou: faísca, "tink", e agora sabe onde o gato está
                Sfx.Play("adaga_tink", _body.position + Vector2.up, 0.8f, 0.9f);
                var flashShield = HitFlash.For(_shield);
                if (flashShield != null) flashShield.Flash(0.08f);
                if (info.Source != null) Notice(info.Source.transform.position.x);
                return;
            }
            _health = Mathf.Max(0, _health - info.Amount);
            _hurtTime = 0f;
            _blinkUntil = Time.time + 0.15f;
            var table = settings.Reactions ?? new HitReactionTable();
            var reaction = table.For(HitReactionRules.WeightOf(info.Type, info.Angle));
            var flash = HitFlash.For(spriteRenderer);
            if (flash != null) { flash.Flash(reaction.Flash); flash.Squash(reaction.SquashPx, Mathf.Max(0.1f, reaction.Flash * 2f)); }
            int side = HitReactionRules.KnockbackSide(info.Direction, _facing);
            Vector2 feet = _body.position;
            HitSparks.Spawn(new Vector2(feet.x - side * settings.BodySize.x * 0.4f, feet.y + settings.BodySize.y * 0.6f),
                            reaction.Sparks, side, reaction.Dust, feet);
            CameraShake.Shake(reaction.ShakePx, reaction.ShakeTime);

            if (_health <= 0) { Die(info.Angle == AttackAngle.Back); return; }
            Sfx.Play("zumbi_dano", _body.position + Vector2.up);
            // levou um golpe: sabe onde o gato está
            if (info.Source != null) Notice(info.Source.transform.position.x);
            float knock = reaction.Knockback * table.KnockbackScale;
            _stunTimer = reaction.Stun;
            _body.linearVelocity = new Vector2(side * knock, reaction.Lift > 0f ? reaction.Lift : _body.linearVelocity.y);
        }

        /// <summary>Derrubado pelo terremoto da marreta.</summary>
        public void Knockdown(float seconds)
        {
            if (IsDead || seconds <= 0f) return;
            _stunTimer = Mathf.Max(_stunTimer, seconds);
        }

        bool _forwardDeath;

        void Die(bool fromBehind)
        {
            _forwardDeath = fromBehind;
            Enter(ZombieState.Dead);
            _deathTime = 0f;
            _box.enabled = false;
            _body.linearVelocity = Vector2.zero;
            _body.bodyType = RigidbodyType2D.Kinematic;
            Sfx.Play("zumbi_morte", _body.position + Vector2.up);
        }

        // ------------------------------------------------------------ visual

        void LateUpdate()
        {
            if (spriteRenderer == null || animations == null) return;
            float dt = Time.deltaTime;
            _hurtTime += dt;
            string key;
            if (IsDead) { _deathTime += dt; key = _forwardDeath && animations.Has("death_stealth") ? "death_stealth" : "death"; }
            else if (_hurtTime < 0.2f) key = "hurt";
            else if (State == ZombieState.Alert) key = "alert";
            else if (State == ZombieState.Chase && Mathf.Abs(_body.linearVelocity.x) > 0.1f) key = "run";
            else if (Mathf.Abs(_body.linearVelocity.x) > 0.1f) key = "walk";
            else key = "idle";

            var clip = animations.Get(key) ?? animations.Get("idle");
            if (clip == null) return;
            if (key != _animKey) { _animKey = key; _animTime = 0f; }
            _animTime += dt;
            var frame = clip.At(_animTime);
            CurrentClipName = clip.name;
            if (frame != null && frame.sprite != null) spriteRenderer.sprite = frame.sprite;
            spriteRenderer.flipX = _facing < 0;
            PlaceShield();

            if (IsDead)
            {
                // a animação já se desfaz em fiapos; depois do último quadro, some
                spriteRenderer.enabled = _deathTime < clip.Duration + settings.CorpseTime;
                return;
            }
            spriteRenderer.enabled = Time.time >= _blinkUntil || ((int)(Time.time * 30f) & 1) == 0;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Vector2 p = Application.isPlaying ? _body.position : (Vector2)transform.position;
            Gizmos.color = new Color(0.56f, 0.82f, 0.31f, 0.8f);
            Gizmos.DrawWireCube(p + Vector2.up * (settings.BodySize.y * 0.5f), settings.ContactSize);
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawLine(p + Vector2.up * 1.3f, p + new Vector2(_facing * settings.SightRange, 1.3f));
            if (_hasBounds) { Gizmos.DrawLine(new Vector3(_minX, p.y), new Vector3(_minX, p.y + 2f)); Gizmos.DrawLine(new Vector3(_maxX, p.y), new Vector3(_maxX, p.y + 2f)); }
        }
#endif
    }
}
