using System;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Progression;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// IA da Sentinela (spec: Docs/Design/furtividade-sentinela.md): Patrulhando → Suspeito → Investigando →
    /// Em combate, com ataque telegrafado, morte e (opcional) reaparecimento. As decisões ficam no
    /// <see cref="AlertBrain"/>; aqui ficam o corpo, o movimento e o visual.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyPerception))]
    public class EnemyAI : MonoBehaviour, IDamageable, IFacingTarget
    {
        enum AttackPhase { None, Windup, Active, Recovery }

        [SerializeField] SentinelSettings settings;
        [Tooltip("Pontos da patrulha: posições X no mundo (tiles). Vazio = fica de guarda no lugar")]
        [SerializeField] float[] patrolPoints = new float[0];
        [Tooltip("1 = começa olhando para a direita, -1 = para a esquerda")]
        [SerializeField] int startFacing = 1;
        [Tooltip("Volta depois de morta, em segundos (0 = não volta). Útil na Playground")]
        [SerializeField, Min(0f)] float respawnDelay;
        [SerializeField] LayerMask groundLayers;
        [SerializeField] LayerMask playerLayers;
        [Tooltip("Meio coração que pode cair ao morrer (chance em SentinelSettings.HeartDropChance)")]
        [SerializeField] GameObject heartDrop;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite idle;
        [SerializeField] Sprite[] walk;
        [SerializeField] Sprite windup;
        [SerializeField] Sprite attack;
        [Tooltip("Animações em pixel art (ex.: TemplarAnimationSet, gerado por Tools/EnemySprites). Vazio = sprites acima")]
        [SerializeField] CharacterAnimationSet animations;
        [Tooltip("\"?\" e \"!\" sobre a cabeça")]
        [SerializeField] TextMesh alertIcon;
        [Tooltip("Estado e suspeita (depuração)")]
        [SerializeField] TextMesh debugLabel;
        [SerializeField] bool showDebugLabel = true;

        readonly AlertBrain _brain = new AlertBrain();
        Rigidbody2D _body;
        BoxCollider2D _box;
        EnemyPerception _perception;

        int _health;
        int _facing = 1;
        int _patrolIndex;
        float _pauseTimer;
        float _searchLookTimer;
        AttackPhase _attackPhase;
        float _attackTimer;
        bool _attackLanded;
        float _deathTimer;
        float _respawnAt = -1f;
        float _blinkUntil;
        float _hurtAt = -99f;
        string _animKey;
        float _animTime;
        Vector2 _spawn;

        /// <summary>Animação mostrada neste quadro (testes e depuração; vazio sem <c>animations</c>).</summary>
        public string CurrentClipName { get; private set; } = "";

        public event Action<EnemyAI> Died;

        public AlertState State => _brain.State;
        public float Suspicion => _brain.Suspicion;
        public AlertBrain Brain => _brain;
        public int Health => _health;
        public bool IsDead => _brain.State == AlertState.Dead;
        public bool IsAttacking => _attackPhase != AttackPhase.None;
        public bool IsWindingUp => _attackPhase == AttackPhase.Windup;
        public SentinelSettings Settings => settings;
        public int FacingDirection => _facing;
        public bool InCombat => _brain.State == AlertState.Combat;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
            _perception = GetComponent<EnemyPerception>();
            if (settings == null)
            {
                Debug.LogError("[Sete Vidas] EnemyAI sem SentinelSettings.", this);
                enabled = false;
                return;
            }
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            if (playerLayers == 0) playerLayers = LayerMask.GetMask(GameConstants.Layers.Player);

            _body.gravityScale = settings.GravityScale;
            _body.freezeRotation = true;
            _spawn = _body.position;
            _health = settings.MaxHealth;
            _facing = startFacing >= 0 ? 1 : -1;
            _perception.Init(settings, _brain);
        }

        void OnEnable() => GameEvents.WorldReset += OnWorldReset;
        void OnDisable() => GameEvents.WorldReset -= OnWorldReset;

        /// <summary>Descanso no santuário ou volta depois de morrer: reaparece no início, vida cheia, patrulhando.</summary>
        void OnWorldReset()
        {
            if (settings == null) return;
            _attackPhase = AttackPhase.None;
            Respawn();
        }

        // ------------------------------------------------------------ física

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (IsDead) { StepDeath(dt); return; }

            _perception.Sense(_facing, dt);
            var before = _brain.State;
            _brain.Tick(dt, settings.Alert);
            if (_brain.State != before && before == AlertState.Combat) _attackPhase = AttackPhase.None;

            float vx = _brain.State switch
            {
                AlertState.Patrolling => Patrol(dt),
                AlertState.Suspicious => LookAt(_brain.LastKnownPosition.x),
                AlertState.Investigating => Investigate(dt),
                AlertState.Combat => Fight(dt),
                _ => 0f,
            };
            _body.linearVelocity = new Vector2(vx, _body.linearVelocity.y);
        }

        float Patrol(float dt)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                float v = MoveTo(_spawn.x, settings.PatrolSpeed, dt, out bool home);
                if (home) _facing = startFacing >= 0 ? 1 : -1;
                return v;
            }

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= dt;
                if (_pauseTimer <= 0f) _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                return 0f;
            }

            float speed = MoveTo(patrolPoints[_patrolIndex % patrolPoints.Length], settings.PatrolSpeed, dt, out bool arrived);
            if (arrived) _pauseTimer = settings.PatrolPause;
            return speed;
        }

        float LookAt(float x)
        {
            float dx = x - _body.position.x;
            if (Mathf.Abs(dx) > 0.05f) _facing = dx > 0f ? 1 : -1;
            return 0f;
        }

        float Investigate(float dt)
        {
            if (!_brain.IsSearching)
            {
                float v = MoveTo(_brain.LastKnownPosition.x, settings.InvestigateSpeed, dt, out bool arrived);
                if (arrived)
                {
                    _brain.ArrivedAtLastKnownPosition();
                    _searchLookTimer = settings.SearchLookInterval;
                }
                return v;
            }

            // Procurando: olha para um lado e para o outro
            _searchLookTimer -= dt;
            if (_searchLookTimer <= 0f)
            {
                _facing = -_facing;
                _searchLookTimer = settings.SearchLookInterval;
            }
            return 0f;
        }

        float Fight(float dt)
        {
            if (_attackPhase != AttackPhase.None)
            {
                StepAttack(dt);
                return 0f;
            }

            Vector2 target = _brain.LastKnownPosition;
            var player = StealthController.Active;
            if (_perception.SeesPlayer && player != null) target = player.Player.Body.position;

            float dx = target.x - _body.position.x;
            float dy = target.y - _body.position.y;
            LookAt(target.x);

            if (_perception.SeesPlayer && Mathf.Abs(dx) <= settings.AttackRange && Mathf.Abs(dy) < 1.2f)
            {
                _attackPhase = AttackPhase.Windup;
                _attackTimer = settings.AttackWindup;
                _attackLanded = false;
                return 0f;
            }
            if (Mathf.Abs(dx) < settings.AttackRange * 0.7f) return 0f; // não entra em cima do gato
            return MoveTo(target.x, settings.ChaseSpeed, dt, out _);
        }

        void StepAttack(float dt)
        {
            _attackTimer -= dt;
            if (_attackPhase == AttackPhase.Active && !_attackLanded) TryHitPlayer();
            if (_attackTimer > 0f) return;

            switch (_attackPhase)
            {
                case AttackPhase.Windup:
                    _attackPhase = AttackPhase.Active;
                    _attackTimer = settings.AttackActive;
                    TryHitPlayer();
                    break;
                case AttackPhase.Active:
                    _attackPhase = AttackPhase.Recovery;
                    _attackTimer = settings.AttackRecovery;
                    break;
                default:
                    _attackPhase = AttackPhase.None;
                    break;
            }
        }

        /// <summary>Centro e tamanho da hitbox da lança.</summary>
        public void GetAttackHitbox(out Vector2 center, out Vector2 size)
        {
            Vector2 feet = _body != null ? _body.position : (Vector2)transform.position;
            float half = settings != null ? settings.BodySize.x * 0.5f : 0.375f;
            size = settings != null ? settings.AttackHitboxSize : Vector2.one;
            center = new Vector2(feet.x + _facing * (half + (settings != null ? settings.AttackHitboxForward : 0.6f)), feet.y + 0.8f);
        }

        void TryHitPlayer()
        {
            if (_attackLanded) return;
            GetAttackHitbox(out var center, out var size);
            var hit = Physics2D.OverlapBox(center, size, 0f, playerLayers);
            if (hit == null) return;
            var target = hit.GetComponentInParent<IDamageable>();
            if (target == null) return;
            target.TakeDamage(new DamageInfo(settings.AttackDamage, new Vector2(_facing, 0f), DamageType.Normal, AttackAngle.Front, gameObject));
            _attackLanded = true;
        }

        /// <summary>Anda até <paramref name="x"/> sem cair de bordas nem atravessar paredes.</summary>
        float MoveTo(float x, float speed, float dt, out bool arrived)
        {
            float dx = x - _body.position.x;
            if (Mathf.Abs(dx) <= 0.1f) { arrived = true; return 0f; }
            int dir = dx > 0f ? 1 : -1;
            _facing = dir;
            if (!CanWalk(dir)) { arrived = true; return 0f; } // bloqueado: considera que chegou
            arrived = false;
            return dir * Mathf.Min(speed, Mathf.Abs(dx) / dt);
        }

        bool CanWalk(int dir)
        {
            Vector2 feet = _body.position;
            float half = settings.BodySize.x * 0.5f;
            var forward = new Vector2(dir, 0f);
            if (Physics2D.Raycast(feet + Vector2.up * 0.3f, forward, half + 0.1f, groundLayers).collider != null)
                return false; // parede
            var probe = feet + new Vector2(dir * (half + 0.15f), 0.1f);
            return Physics2D.Raycast(probe, Vector2.down, 0.6f, groundLayers).collider != null; // chão à frente
        }

        // ------------------------------------------------------- dano e morte

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || info.Amount <= 0) return;

            _health = Mathf.Max(0, _health - info.Amount);
            _blinkUntil = Time.time + 0.2f;
            _hurtAt = Time.time;
            DamagePopup.Spawn(transform.position + Vector3.up * 2.1f, DamagePopup.DamageLabel(info), DamagePopup.ColorFor(info.Angle));

            Vector2 attacker = info.Source != null ? (Vector2)info.Source.transform.position : _body.position - info.Direction;
            _brain.OnDamaged(attacker);
            if (_attackPhase == AttackPhase.None) LookAt(attacker.x);

            if (_health <= 0) Die();
        }

        void Die()
        {
            _brain.Kill();
            _attackPhase = AttackPhase.None;
            _deathTimer = 0f;
            _box.enabled = false;
            _body.linearVelocity = Vector2.zero;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _respawnAt = respawnDelay > 0f ? Time.time + settings.DeathDuration + respawnDelay : -1f;
            Died?.Invoke(this);
            TryDropHeart(UnityEngine.Random.value);
        }

        /// <summary>Solta o meio coração se <paramref name="roll01"/> cair dentro da chance. Retorna o item criado (ou null).</summary>
        public GameObject TryDropHeart(float roll01)
        {
            if (heartDrop == null || !HealthRules.RollDrop(settings.HeartDropChance, roll01)) return null;
            var chest = (Vector3)(_body.position + Vector2.up * (settings.BodySize.y * 0.6f));
            return Instantiate(heartDrop, chest, Quaternion.identity);
        }

        [ContextMenu("Depuração: soltar meio coração")]
        void DebugDropHeart() => TryDropHeart(0f);

        void StepDeath(float dt)
        {
            _deathTimer += dt;
            if (_respawnAt > 0f && Time.time >= _respawnAt) Respawn();
        }

        void Respawn()
        {
            _health = settings.MaxHealth;
            _brain.Reset();
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.position = _spawn;
            _body.linearVelocity = Vector2.zero;
            _box.enabled = true;
            _facing = startFacing >= 0 ? 1 : -1;
            _patrolIndex = 0;
            _pauseTimer = 0f;
            _respawnAt = -1f;
        }

        // ------------------------------------------------------------- visual

        void LateUpdate()
        {
            if (settings == null) return;
            UpdateSprite();
            UpdateLabels();
        }

        void UpdateSprite()
        {
            if (spriteRenderer == null) return;
            var t = spriteRenderer.transform;
            if (animations != null) { UpdateAnimation(t); return; }

            if (IsDead)
            {
                // Tomba para trás e some
                float k = settings.DeathDuration > 0f ? Mathf.Clamp01(_deathTimer / settings.DeathDuration) : 1f;
                t.localRotation = Quaternion.Euler(0f, 0f, _facing * 90f * Mathf.Min(1f, k * 1.5f));
                var c = spriteRenderer.color; c.a = 1f - k; spriteRenderer.color = c;
                spriteRenderer.enabled = k < 1f;
                return;
            }

            t.localRotation = Quaternion.identity;
            var color = spriteRenderer.color; color.a = 1f; spriteRenderer.color = color;
            spriteRenderer.flipX = _facing < 0;
            spriteRenderer.enabled = Time.time >= _blinkUntil || ((int)(Time.time * 30f) & 1) == 0;

            Sprite s = idle;
            if (_attackPhase == AttackPhase.Windup && windup != null) s = windup;
            else if (_attackPhase != AttackPhase.None && attack != null) s = attack;
            else if (Mathf.Abs(_body.linearVelocity.x) > 0.1f && walk != null && walk.Length > 0)
                s = walk[(int)(Time.time * 6f) % walk.Length];
            if (s != null) spriteRenderer.sprite = s;
        }

        /// <summary>
        /// Animações do <see cref="animations"/>: a estocada segue as fases do ataque (preparar, ativo, recuperar),
        /// a morte toca inteira (cai de costas) e depois o corpo some. Spec: SeteVidasDocs/Inimigos_Arte.md.
        /// </summary>
        void UpdateAnimation(Transform t)
        {
            t.localRotation = Quaternion.identity;
            spriteRenderer.flipX = _facing < 0;
            var color = spriteRenderer.color;
            SpriteFrame frame;

            if (IsDead)
            {
                var death = animations.Get("death");
                frame = death != null ? death.At(_deathTimer) : null;
                CurrentClipName = "death";
                // deitado um instante, depois some
                float lie = death != null ? death.Duration : 0f;
                float fade = Mathf.Clamp01((_deathTimer - lie - 0.4f) / 0.4f);
                color.a = 1f - fade;
                spriteRenderer.color = color;
                spriteRenderer.enabled = fade < 1f;
                if (frame != null) spriteRenderer.sprite = frame.sprite;
                return;
            }

            color.a = 1f;
            spriteRenderer.color = color;
            spriteRenderer.enabled = Time.time >= _blinkUntil || ((int)(Time.time * 30f) & 1) == 0;

            string key;
            float normalized = -1f; // >= 0: frame pelo progresso da fase
            if (_attackPhase != AttackPhase.None)
            {
                float duration = _attackPhase == AttackPhase.Windup ? settings.AttackWindup
                               : _attackPhase == AttackPhase.Active ? settings.AttackActive : settings.AttackRecovery;
                key = _attackPhase == AttackPhase.Windup ? "attack_windup" : _attackPhase == AttackPhase.Active ? "attack_active" : "attack_recovery";
                normalized = duration > 0f ? 1f - Mathf.Clamp01(_attackTimer / duration) : 1f;
            }
            else if (Time.time - _hurtAt < 0.2f) key = "hurt";
            else if (Mathf.Abs(_body.linearVelocity.x) > 0.1f) key = _brain.State == AlertState.Combat ? "run" : "walk";
            else if (_brain.State == AlertState.Suspicious || _brain.State == AlertState.Investigating || _brain.State == AlertState.Combat) key = "alert";
            else key = "idle";

            var clip = animations.Get(key) ?? animations.Get("idle");
            if (clip == null) return;
            if (key != _animKey) { _animKey = key; _animTime = 0f; }
            _animTime += Time.deltaTime;
            frame = normalized >= 0f ? clip.AtNormalized(normalized) : clip.At(_animTime);
            CurrentClipName = clip.name;
            if (frame != null && frame.sprite != null) spriteRenderer.sprite = frame.sprite;
        }

        void UpdateLabels()
        {
            if (alertIcon != null)
            {
                switch (_brain.State)
                {
                    case AlertState.Suspicious:
                    case AlertState.Investigating:
                        alertIcon.text = "?";
                        alertIcon.color = new Color32(0xff, 0xd8, 0x4d, 0xff);
                        break;
                    case AlertState.Combat:
                        alertIcon.text = "!";
                        alertIcon.color = new Color32(0xff, 0x3b, 0x3b, 0xff);
                        break;
                    default:
                        alertIcon.text = "";
                        break;
                }
            }

            if (debugLabel != null)
            {
                debugLabel.gameObject.SetActive(showDebugLabel && !IsDead);
                if (showDebugLabel)
                    debugLabel.text = $"{StateName(_brain.State)} {_brain.Suspicion:0.00}\n{_health}/{settings.MaxHealth}";
            }
        }

        public static string StateName(AlertState state) => state switch
        {
            AlertState.Patrolling => "Patrulhando",
            AlertState.Suspicious => "Suspeito",
            AlertState.Investigating => "Investigando",
            AlertState.Combat => "Em combate",
            _ => "Morta",
        };

#if UNITY_EDITOR
        // Depuração: cone (alcance base em cinza, efetivo na cor do estado), última posição conhecida e hitbox.
        void OnDrawGizmos()
        {
            if (settings == null) return;
            Vector2 feet = Application.isPlaying && _body != null ? _body.position : (Vector2)transform.position;
            Vector2 eye = feet + Vector2.up * settings.EyeHeight;
            int facing = Application.isPlaying ? _facing : (startFacing >= 0 ? 1 : -1);

            DrawCone(eye, facing, settings.VisionRange, new Color(0.6f, 0.6f, 0.6f, 0.4f));
            if (Application.isPlaying && _perception != null && !IsDead)
                DrawCone(eye, facing, _perception.LastEffectiveRange, StateColor());

            if (Application.isPlaying && _brain.HasLastKnownPosition && _brain.State != AlertState.Patrolling && !IsDead)
            {
                Vector3 p = _brain.LastKnownPosition;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(p + new Vector3(-0.3f, -0.3f), p + new Vector3(0.3f, 0.3f));
                Gizmos.DrawLine(p + new Vector3(-0.3f, 0.3f), p + new Vector3(0.3f, -0.3f));
            }

            if (patrolPoints != null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
                foreach (float x in patrolPoints) Gizmos.DrawLine(new Vector3(x, feet.y), new Vector3(x, feet.y + 0.5f));
            }

            if (Application.isPlaying && _attackPhase != AttackPhase.None)
            {
                GetAttackHitbox(out var c, out var s);
                Gizmos.color = _attackPhase == AttackPhase.Active ? Color.red : new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireCube(c, s);
            }
        }

        void DrawCone(Vector2 eye, int facing, float range, Color color)
        {
            Gizmos.color = color;
            float a = settings.VisionHalfAngle * Mathf.Deg2Rad;
            var up = new Vector2(Mathf.Cos(a) * facing, Mathf.Sin(a)) * range;
            var down = new Vector2(Mathf.Cos(a) * facing, -Mathf.Sin(a)) * range;
            Gizmos.DrawLine(eye, eye + up);
            Gizmos.DrawLine(eye, eye + down);
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float t0 = Mathf.Lerp(-a, a, i / (float)steps), t1 = Mathf.Lerp(-a, a, (i + 1) / (float)steps);
                Gizmos.DrawLine(eye + new Vector2(Mathf.Cos(t0) * facing, Mathf.Sin(t0)) * range,
                                eye + new Vector2(Mathf.Cos(t1) * facing, Mathf.Sin(t1)) * range);
            }
        }

        Color StateColor() => _brain.State switch
        {
            AlertState.Suspicious => new Color(1f, 0.85f, 0.3f),
            AlertState.Investigating => new Color(1f, 0.6f, 0.2f),
            AlertState.Combat => Color.red,
            _ => new Color(0.4f, 1f, 0.4f),
        };
#endif
    }
}
