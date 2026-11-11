using SeteVidas.Abilities;
using SeteVidas.Classes;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Visual do Lion: escolhe a animação pelo estado do jogador e cuida do "game feel" (inspiração em Celeste):
    /// esticar/achatar no pulo e na aterrissagem, cauda procedural, poeira, faixa da capa na cor da classe,
    /// centelha da última vida, morte em 7 centelhas e renascimento.
    /// Golpes e subida de borda seguem as fases do código (Windup/Active/Recovery, LedgeClimbDuration):
    /// mudar esses tempos no Inspector nunca dessincroniza a arte.
    /// Spec: SeteVidasDocs/Lion_Arte_e_Animacoes.md. Sem <see cref="animationSet"/>, usa os sprites placeholder.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        const float Px = 1f / 16f;

        [SerializeField] PlayerController controller;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] WallClimbAbility wallClimb;
        [SerializeField] CombatController combat;
        [SerializeField] PlayerHealth health;
        [SerializeField] ClassManager classes;

        [Header("Animações (geradas por Tools/LionSprites)")]
        [SerializeField] CharacterAnimationSet animationSet;

        [Header("Esticar e achatar (escala x, y)")]
        [SerializeField] Vector2 jumpSquash = new Vector2(0.8f, 1.25f);
        [SerializeField] Vector2 wallJumpSquash = new Vector2(0.85f, 1.2f);
        [SerializeField] Vector2 landSquash = new Vector2(1.25f, 0.8f);
        [SerializeField] Vector2 heavyLandSquash = new Vector2(1.4f, 0.65f);
        [SerializeField] Vector2 stealthHitSquash = new Vector2(1.15f, 0.9f);
        [Tooltip("Tempo para voltar ao tamanho normal (s)")]
        [SerializeField] float squashRecover = 0.1f;

        [Header("Regras de animação")]
        [Tooltip("Queda maior que isto (tiles) usa a aterrissagem pesada")]
        [SerializeField] float heavyLandTiles = 6f;
        [Tooltip("Parado por este tempo (s): lambe o pelo ou gira a chave")]
        [SerializeField] float idleExtraDelay = 6f;
        [SerializeField] Vector2 blinkInterval = new Vector2(3f, 5f);
        [Tooltip("Vida (hits) em que a centelha do peito aparece")]
        [SerializeField] int lastSparkHits = 2;

        [Header("Sprites placeholder (só sem animationSet)")]
        [SerializeField] Sprite idle;
        [SerializeField] Sprite[] run;
        [SerializeField] Sprite jump;
        [SerializeField] Sprite fall;
        [SerializeField] Sprite crouch;
        [SerializeField] Sprite hang;
        [SerializeField] Sprite[] climb;
        [SerializeField] Sprite attack;
        [Tooltip("Quadros por segundo da corrida e da escalada (placeholder)")]
        [SerializeField] float runFps = 10f;

        [Header("Corte da adaga (padrão, se a classe não tiver efeito próprio)")]
        [SerializeField] SpriteRenderer slashRenderer;
        [Tooltip("Um sprite por golpe do combo")]
        [SerializeField] Sprite[] slash;

        [Header("Camada da classe")]
        [Tooltip("Arma da classe desenhada sobre o gato (filho do Visual)")]
        [SerializeField] SpriteRenderer classOverlay;
        [Tooltip("Duração do brilho na cor da classe ao trocar (s)")]
        [SerializeField] float switchFlashTime = 0.25f;

        // ------------------------------------------------------------ estado

        int _climbFrame;
        float _climbAnimTime;
        float _flashUntil;
        Color _flashColor = Color.white;

        PlayerTail _tail;
        PlayerCape _cape;
        SpriteRenderer _trim, _chest;
        bool _built;

        SpriteClip _base;           // animação do estado atual
        float _baseTime;
        string _phaseKey;           // golpe atual (passo + fase)
        float _phaseTime;
        float _ledgeTime, _wallAnimTime, _wallDustTimer;
        int _climbStabIndex = -1;
        /// <summary>Onde a adaga de cima entra na parede no frame 0 da escalada (px a partir do pivot).</summary>
        static readonly Vector2 ClimbStab = new Vector2(6f, 16.5f);

        // animações de uma vez (tempo desde o início; grande = inativa)
        const float Off = 999f;
        float _jumpTime = Off, _landTime = Off, _wallJumpTime = Off, _hurtTime = Off, _crouchTime = Off;
        SpriteClip _landClip;
        SpriteClip _idleExtra; float _idleExtraTime = Off;
        SpriteClip _action; float _actionTime; bool _actionHold;
        float _idleTime, _nextBlink = 4f, _blinkTime = Off;
        bool _idleToggle;

        enum DeathPhase { None, Flash, Gone }
        DeathPhase _death;
        float _deathTime;
        enum RespawnPhase { None, Converging, Playing }
        RespawnPhase _respawn;
        float _respawnTime;

        bool _wasGrounded = true, _wasClimbingWall, _wasCollapsed, _wasCrouching;
        float _prevVy, _airTime, _peakY;
        int _prevFacing = 1, _lastRunIndex = -1;
        Vector2 _squash = Vector2.one;
        int _facing = 1;
        Vector2 _chestLocal;

        /// <summary>Nome da animação mostrada neste quadro (para testes e depuração).</summary>
        public string CurrentClipName { get; private set; }

        void Reset()
        {
            controller = GetComponentInParent<PlayerController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            wallClimb = GetComponentInParent<WallClimbAbility>();
            combat = GetComponentInParent<CombatController>();
            health = GetComponentInParent<PlayerHealth>();
            classes = GetComponentInParent<ClassManager>();
        }

        void OnEnable()
        {
            if (classes != null) classes.ClassChanged += OnClassChanged;
            if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; health.Respawned += OnRespawned; }
            if (combat != null) combat.Hit += OnHit;
        }

        void OnDisable()
        {
            if (classes != null) classes.ClassChanged -= OnClassChanged;
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Respawned -= OnRespawned; }
            if (combat != null) combat.Hit -= OnHit;
            transform.localScale = Vector3.one;
        }

        // ------------------------------------------------------------ API de cena

        /// <summary>Toca uma animação de cena (ex.: "grab_tome", "curse_kneel", "look_up"). <paramref name="hold"/> segura o último frame.</summary>
        public bool PlayAction(string clipName, bool hold = false)
        {
            var clip = animationSet != null ? animationSet.Get(clipName) : null;
            if (clip == null) return false;
            _action = clip;
            _actionTime = 0f;
            _actionHold = hold;
            return true;
        }

        public void StopAction() => _action = null;

        /// <summary>Tinta de cena multiplicada no gato, na cauda e na capa (branco = nenhuma).</summary>
        public Color SceneTint { get; set; } = Color.white;

        /// <summary>O frame mostra o gato caído no chão (a cauda deita junto).</summary>
        static bool IsLyingFrame(SpriteFrame f)
        {
            if (f == null || f.sprite == null) return false;
            string n = f.sprite.name;
            return n.StartsWith("chr_lion_lying") || n.StartsWith("chr_lion_nap_") && !n.StartsWith("chr_lion_nap_wake_") || n == "chr_lion_nap_wake_0" || n == "chr_lion_nap_wake_1" || n == "chr_lion_nap_wake_2" || n == "chr_lion_nap_wake_3" || n == "chr_lion_nap_wake_4" || n.StartsWith("chr_lion_curse_writhe") || n == "chr_lion_curse_kneel_6" || n == "chr_lion_get_up_0";
        }

        static readonly Color HealGold = new Color32(0xff, 0xd3, 0x4d, 0xff);

        /// <summary>
        /// Magia de cura (<see cref="HealingSpell"/>): na concentração, centelhas douradas sobem até o peito;
        /// ao curar (<paramref name="done"/>), o gato brilha em dourado e um anel de luz se abre.
        /// </summary>
        public void HealFx(bool done)
        {
            if (animationSet == null || spriteRenderer == null) return;
            Vector3 chest = ChestWorld();
            int layer = spriteRenderer.sortingLayerID, order = spriteRenderer.sortingOrder + 3;
            if (!done)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = (200f + i * 35f) * Mathf.Deg2Rad;
                    Vector3 from = chest + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f - 0.4f, 0f) * 0.9f;
                    var fx = SpriteFx.Play(animationSet.spark, 0.06f, from, false, HealGold, layer, order, default, 0.4f, true);
                    if (fx != null) fx.Toward(chest);
                }
                return;
            }
            _flashColor = HealGold;
            _flashUntil = Time.time + switchFlashTime * 1.6f;
            SpriteFx.Play(animationSet.classSwitch, 0.04f, chest, false, HealGold, layer, order);
            for (int i = 0; i < 4; i++)
            {
                Vector3 vel = new Vector3((i - 1.5f) * 0.6f, 2.2f + (i % 2) * 0.6f, 0f);
                SpriteFx.Play(animationSet.spark, 0.06f, chest, false, Color.Lerp(HealGold, Color.white, 0.4f), layer, order, vel, 0.45f, true);
            }
        }

        // ------------------------------------------------------------ eventos

        void OnClassChanged(ClassDefinition previous, ClassDefinition next)
        {
            if (next == null) return;
            _flashColor = next.ThemeColor;
            _flashUntil = Time.time + switchFlashTime;
            if (animationSet != null && previous != null)
                SpriteFx.Play(animationSet.classSwitch, 0.04f, ChestWorld(), false, next.ThemeColor,
                              spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 3);
        }

        void OnDamaged(DamageInfo info)
        {
            _hurtTime = 0f;
            if (_tail != null) _tail.Bristle(0.3f);
        }

        void OnDied()
        {
            _death = DeathPhase.Flash;
            _deathTime = 0f;
            _action = null;
            Hitstop.Trigger(0.1f); // congela um instante no golpe final
        }

        void OnRespawned()
        {
            _death = DeathPhase.None;
            _respawn = RespawnPhase.Converging;
            _respawnTime = 0f;
            _hurtTime = Off;
            _squash = Vector2.one;
            if (animationSet == null) return;
            // as sete centelhas voltam de fora e se juntam no peito
            Vector3 chest = ChestWorld();
            for (int i = 0; i < 7; i++)
            {
                float a = (90f + i * 360f / 7f) * Mathf.Deg2Rad;
                Vector3 from = chest + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 2.2f;
                var fx = SpriteFx.Play(animationSet.spark, 0.08f, from, false, SparkColor(),
                                       spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 3, default, 0.35f);
                if (fx != null) fx.Toward(chest);
            }
        }

        void OnHit(IDamageable target, DamageInfo info)
        {
            if (animationSet == null || info.Angle == AttackAngle.Front) return;
            var c = target as Component;
            Vector3 at = c != null ? c.transform.position + Vector3.up * 0.75f : transform.position + Vector3.right * _facing;
            bool stealth = info.Angle == AttackAngle.StealthBack;
            SpriteFx.Play(animationSet.backstab, stealth ? 0.04f : 0.03f, at, false, Color.white,
                          spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 4);
            if (!stealth) return;
            _squash = stealthHitSquash;
            // rastro azul-noite do bote
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = transform.position - new Vector3(_facing * 0.22f * (i + 1), 0f, 0f);
                var col = new Color(0.17f, 0.23f, 0.4f, 0.5f - i * 0.17f);
                SpriteFx.Play(new[] { spriteRenderer.sprite }, 0.15f, p, spriteRenderer.flipX, col,
                              spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder - 4, default, 0.15f, true);
            }
        }

        // ------------------------------------------------------------ quadro

        // Na rampa, a caixa de colisão encosta pela quina de cima e o meio dos pés fica no ar:
        // o desenho desce essa folga (em pixels inteiros) para as patas tocarem a rampa.
        float _slopeDrop;
        const float SlopeDropSpeed = 8f; // tiles/s: suaviza a entrada e a saída da rampa

        void UpdateSlopeDrop(float dt)
        {
            float target = 0f;
            var ground = controller.Ground;
            if (ground != null && controller.IsGrounded && !controller.IsHanging && !controller.IsClimbingLedge)
                target = ground.SlopeGap;
            _slopeDrop = Mathf.MoveTowards(_slopeDrop, target, SlopeDropSpeed * dt);
            var p = transform.localPosition;
            p.y = -Mathf.Round(_slopeDrop / Px) * Px;
            transform.localPosition = p;
        }

        void LateUpdate()
        {
            if (controller == null || spriteRenderer == null) return;
            UpdateSlopeDrop(Time.deltaTime);
            if (animationSet == null) { LegacyUpdate(); return; }
            Build();

            float dt = Time.deltaTime;
            bool attacking = combat != null && combat.IsAttacking;
            _facing = attacking ? combat.AttackDirection : controller.FacingDirection;
            if (_facing == 0) _facing = 1;

            DetectTransitions(dt);
            SpriteFrame frame = PickFrame(dt, attacking, out bool bodyVisible, out bool overlayAllowed);
            if (frame == null) { LegacyUpdate(); return; }

            bool flip = _facing < 0;
            spriteRenderer.sprite = frame.sprite;
            spriteRenderer.flipX = flip;
            spriteRenderer.enabled = bodyVisible;
            transform.localRotation = Quaternion.identity;

            // esticar/achatar: volta ao normal com suavização exponencial
            _squash = Vector2.Lerp(_squash, Vector2.one, 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.01f, squashRecover)));
            transform.localScale = new Vector3(_squash.x, _squash.y, 1f);

            // cor: brilho da troca de classe e piscar da invencibilidade (alfa 1 ↔ 0,3)
            float flash = Time.time < _flashUntil ? (_flashUntil - Time.time) / switchFlashTime : 0f;
            Color color = Color.Lerp(Color.white, _flashColor, flash);
            color *= SceneTint; // cenas (ex.: a maldição do tomo deixa o gato arroxeado)
            bool blinking = health != null && health.IsInvulnerable && !health.IsCollapsed && !health.IsDead &&
                            ((int)(Time.time / 0.08f) & 1) == 1;
            if (blinking) color.a = 0.3f;
            spriteRenderer.color = color;

            _chestLocal = new Vector2(frame.chest.x * _facing, frame.chest.y) * Px;
            UpdateOverlay(frame, attacking, bodyVisible && overlayAllowed, color);
            UpdateTrimAndChest(frame, bodyVisible, color.a);
            UpdateTailAndCape(frame, bodyVisible, dt, color);
            UpdateSlash();

            _wasGrounded = controller.IsGrounded;
            _wasClimbingWall = wallClimb != null && wallClimb.IsClimbing;
            _wasCollapsed = health != null && health.IsCollapsed;
            _wasCrouching = controller.IsCrouching;
            _prevVy = controller.Velocity.y;
            _prevFacing = _facing;
        }

        void DetectTransitions(float dt)
        {
            bool grounded = controller.IsGrounded;
            bool climbingWall = wallClimb != null && wallClimb.IsClimbing;
            Vector2 v = controller.Velocity;
            Vector3 feet = transform.position;
            bool busy = controller.IsHanging || controller.IsClimbingLedge || climbingWall;
            bool dead = health != null && (health.IsDead || health.IsCollapsed);

            // tempos das animações de uma vez
            _jumpTime += dt; _landTime += dt; _wallJumpTime += dt; _hurtTime += dt; _crouchTime += dt;
            _idleExtraTime += dt; _blinkTime += dt; _actionTime += dt;

            // no ar: guarda a altura máxima para medir a queda
            if (grounded || busy) { _airTime = 0f; _peakY = feet.y; }
            else { _airTime += dt; _peakY = Mathf.Max(_peakY, feet.y); }

            if (dead) return;

            // pulo: impulso para cima logo depois de sair do chão (inclui coyote time e buffer)
            bool jumped = !grounded && !busy && !_wasClimbingWall && _airTime < 0.15f && v.y > 5f && v.y - _prevVy > 5f && _hurtTime > 0.2f;
            if (jumped)
            {
                _jumpTime = 0f;
                _squash = jumpSquash;
                Dust(animationSet.dustJump, 0.05f, feet, false);
            }

            // pulo da parede
            if (_wasClimbingWall && !climbingWall && !grounded && v.y > 1f)
            {
                _wallJumpTime = 0f;
                _squash = wallJumpSquash;
                Dust(animationSet.dustWall, 0.06f, feet + new Vector3(-_facing * 0.4f, 0.6f), _facing < 0);
            }

            // aterrissagem: "cai de pé"
            if (!_wasGrounded && grounded && !busy)
            {
                float drop = _peakY - feet.y;
                if (drop > 0.3f)
                {
                    bool heavy = drop > heavyLandTiles;
                    _landClip = animationSet.Get(heavy ? "land_heavy" : "land");
                    _landTime = 0f;
                    _squash = heavy ? heavyLandSquash : landSquash;
                    Dust(animationSet.dustLand, 0.05f, feet, false);
                }
            }

            // virou correndo
            if (grounded && Mathf.Abs(v.x) > 2f && _facing != _prevFacing)
                Dust(animationSet.dustStep, 0.06f, feet, _facing < 0);

            // descendo a parede: poeira nas patas
            if (climbingWall && wallClimb.ClimbVelocity < -0.01f)
            {
                _wallDustTimer -= dt;
                if (_wallDustTimer <= 0f)
                {
                    _wallDustTimer = 0.15f;
                    Dust(animationSet.dustWall, 0.06f, feet + new Vector3(_facing * 0.45f, 1.1f), _facing < 0);
                }
            }

            // agachou
            if (controller.IsCrouching && !_wasCrouching) _crouchTime = 0f;

            // levantou depois da maldição
            bool collapsed = health != null && health.IsCollapsed;
            // (do cochilo no santuário ele acorda como gatinho: boceja, espreguiça e levanta)
            if (!collapsed && _wasCollapsed) PlayAction(_action != null && _action.name == "nap" && animationSet != null && animationSet.Get("nap_wake") != null ? "nap_wake" : "get_up");
        }

        SpriteFrame PickFrame(float dt, bool attacking, out bool bodyVisible, out bool overlayAllowed)
        {
            bodyVisible = true;
            overlayAllowed = true;
            var set = animationSet;
            bool grounded = controller.IsGrounded;
            Vector2 v = controller.Velocity;

            // ----- morte: flash branco, depois as sete centelhas e o gato some
            if (health != null && health.IsDead)
            {
                overlayAllowed = false;
                _deathTime += dt;
                if (_death == DeathPhase.Flash && _deathTime >= 0.066f)
                {
                    _death = DeathPhase.Gone;
                    Burst();
                }
                if (_death == DeathPhase.Gone) bodyVisible = false;
                return Show(set.Get(_death == DeathPhase.Gone ? "death" : "hurt_flash"), _deathTime);
            }

            // ----- renascimento: centelhas convergindo, depois o gato se forma
            if (_respawn == RespawnPhase.Converging)
            {
                _respawnTime += dt;
                bodyVisible = false;
                if (_respawnTime >= 0.35f) { _respawn = RespawnPhase.Playing; _respawnTime = 0f; }
                return Show(set.Get("respawn"), 0f);
            }
            if (_respawn == RespawnPhase.Playing)
            {
                _respawnTime += dt;
                var rc = set.Get("respawn");
                if (rc != null && _respawnTime < rc.Duration) { overlayAllowed = false; return Show(rc, _respawnTime); }
                _respawn = RespawnPhase.None;
            }

            // ----- caído (maldição) e animações de cena
            bool collapsed = health != null && health.IsCollapsed;
            if (collapsed)
            {
                overlayAllowed = false;
                if (_action != null && (_action.name == "curse_kneel" || _action.name == "curse_writhe" || _action.name == "nap")) return Show(_action, _actionTime);
                return Show(set.Get("lying"), 0f);
            }
            if (_action != null)
            {
                if (_actionTime < _action.Duration || _actionHold) { overlayAllowed = false; return Show(_action, _actionTime); }
                _action = null;
            }

            // ----- dano
            if (_hurtTime < 0.21f)
            {
                var flashClip = set.Get("hurt_flash");
                if (_hurtTime < 0.05f && flashClip != null) return Show(flashClip, 0f);
                return Show(set.Get("hurt"), _hurtTime - 0.05f);
            }

            // ----- borda e parede
            if (controller.IsHanging) { ResetIdle(); _ledgeTime = 0f; return Base(set.Get("ledge_hang"), dt); }
            if (controller.IsClimbingLedge)
            {
                ResetIdle();
                _ledgeTime += dt;
                float dur = controller.Settings != null ? controller.Settings.LedgeClimbDuration : 0.2f;
                return Normalized(set.Get("ledge_climb"), _ledgeTime / dur);
            }
            _ledgeTime = 0f;
            if (wallClimb != null && wallClimb.IsClimbing)
            {
                ResetIdle();
                if (Mathf.Abs(wallClimb.ClimbVelocity) > 0.01f) _wallAnimTime += dt; // só anima enquanto sobe ou desce
                var wc = set.Get("wall_climb");
                if (wc == null) return null;
                // frames 0 e 2: uma adaga crava no alto da parede e solta lasca de pedra
                int index = wc.IndexAt(_wallAnimTime);
                if (index != _climbStabIndex)
                {
                    _climbStabIndex = index;
                    if ((index == 0 || index == 2) && wallClimb.ClimbVelocity > 0.01f)
                    {
                        var hand = transform.TransformPoint(new Vector3(_facing * ClimbStab.x, ClimbStab.y + (index == 2 ? 2f : 0f), 0f) * Px);
                        Dust(animationSet.dustWall, 0.05f, hand, _facing < 0);
                    }
                }
                return wc.At(_wallAnimTime);
            }

            // ----- golpe (sincronizado com as fases do código)
            if (attacking)
            {
                ResetIdle();
                var f = AttackFrame(dt, grounded);
                if (f != null) return f;
            }
            else _phaseKey = null;

            // ----- pulo da parede
            var wj = set.Get("wall_jump");
            if (wj != null && _wallJumpTime < wj.Duration) { ResetIdle(); return Show(wj, _wallJumpTime); }

            // ----- agachado
            if (controller.IsCrouching)
            {
                ResetIdle();
                var entry = set.Get("crouch");
                if (Mathf.Abs(v.x) > 0.1f) return Base(set.Get("crouch_walk"), dt);
                if (entry != null && _crouchTime < entry.Duration) return Show(entry, _crouchTime);
                return Base(entry, dt, holdLast: true);
            }

            // ----- no ar
            if (!grounded)
            {
                ResetIdle();
                if (v.y > 2f)
                {
                    var up = set.Get("jump_up");
                    return Show(up, Mathf.Min(_jumpTime, up != null ? up.Duration : 0f));
                }
                if (v.y >= -2f) return Base(set.Get("jump_apex"), dt);
                return Base(set.Get("fall"), dt);
            }

            // ----- aterrissagem (cancelável: andou, sai dela)
            if (_landClip != null && _landTime < _landClip.Duration && Mathf.Abs(v.x) < 0.5f)
                return Show(_landClip, _landTime);

            // ----- correndo
            if (Mathf.Abs(v.x) > 0.1f)
            {
                ResetIdle();
                var runClip = set.Get("run");
                var fr = Base(runClip, dt);
                int idx = runClip != null ? runClip.IndexAt(_baseTime) : 0;
                if (idx != _lastRunIndex && (idx == 0 || idx == 3))
                    Dust(set.dustStep, 0.06f, transform.position - new Vector3(_facing * 0.25f, 0f), _facing < 0);
                _lastRunIndex = idx;
                return fr;
            }
            _lastRunIndex = -1;

            // ----- parado: piscar e, depois de um tempo, lamber o pelo ou girar a chave
            return IdleFrame(dt);
        }

        SpriteFrame IdleFrame(float dt)
        {
            var set = animationSet;
            _idleTime += dt;
            if (_idleExtra != null)
            {
                if (_idleExtraTime < _idleExtra.Duration) return Show(_idleExtra, _idleExtraTime);
                _idleExtra = null;
                _idleTime = 0f;
            }
            if (_idleTime >= idleExtraDelay)
            {
                _idleToggle = !_idleToggle;
                _idleExtra = set.Get(_idleToggle ? "idle_groom" : "idle_key");
                _idleExtraTime = 0f;
                if (_idleExtra != null) return Show(_idleExtra, 0f);
            }
            if (_idleTime >= _nextBlink)
            {
                _blinkTime = 0f;
                _nextBlink = _idleTime + Random.Range(blinkInterval.x, blinkInterval.y);
            }
            var blink = set.Get("idle_blink");
            if (blink != null && _blinkTime < blink.Duration) return Show(blink, _blinkTime);
            return Base(set.Get("idle"), dt);
        }

        void ResetIdle()
        {
            _idleTime = 0f;
            _idleExtra = null;
            _nextBlink = Random.Range(blinkInterval.x, blinkInterval.y);
        }

        SpriteFrame AttackFrame(float dt, bool grounded)
        {
            var weapon = combat.Weapon;
            if (weapon == null) return null;
            // marreta carregando (loop) e terremoto (pela linha do tempo do golpe)
            if (combat.IsCharging)
            {
                var charge = animationSet.Get("hammer_charge");
                if (charge != null) { _phaseKey = "charge"; return Base(charge, dt); }
            }
            if (combat.IsSlamming)
            {
                var slam = animationSet.Get("hammer_slam");
                if (slam != null) { _phaseKey = "slam"; return Normalized(slam, combat.SlamProgress); }
            }
            string phase = combat.IsWindingUp ? "windup" : combat.IsHitActive ? "active" : "recovery";
            int step = combat.ComboStep + 1;
            string key = step + phase;
            if (key != _phaseKey) { _phaseKey = key; _phaseTime = 0f; }
            else _phaseTime += dt;

            float duration = phase == "windup" ? weapon.Windup
                           : phase == "active" ? weapon.Active
                           : step >= weapon.ComboSteps ? weapon.FinalRecovery : weapon.Recovery;

            string w = WeaponKey();
            SpriteClip clip = null;
            if (controller.IsCrouching) clip = animationSet.Get(w + "_crouch_" + phase);
            if (clip == null && !grounded) clip = animationSet.Get(w + step + "_air_" + phase) ?? animationSet.Get(w + "1_air_" + phase);
            if (clip == null) clip = animationSet.Get(w + step + "_" + phase) ?? animationSet.Get(w + "1_" + phase);
            return Normalized(clip, _phaseTime / Mathf.Max(0.0001f, duration));
        }

        string WeaponKey()
        {
            bool paladin = classes != null && classes.CurrentClass == CharacterClass.Paladin;
            return paladin && animationSet.Has("hammer1_windup") ? "hammer" : "dagger";
        }

        // ------------------------------------------------------------ tocador

        SpriteFrame Base(SpriteClip clip, float dt, bool holdLast = false)
        {
            if (clip == null) return null;
            if (clip != _base) { _base = clip; _baseTime = 0f; }
            else _baseTime += dt;
            CurrentClipName = clip.name;
            return holdLast ? clip.frames[clip.Count - 1] : clip.At(_baseTime);
        }

        SpriteFrame Show(SpriteClip clip, float time)
        {
            if (clip == null) return null;
            _base = null;
            CurrentClipName = clip.name;
            return clip.At(Mathf.Max(0f, time));
        }

        SpriteFrame Normalized(SpriteClip clip, float t)
        {
            if (clip == null) return null;
            _base = null;
            CurrentClipName = clip.name;
            return clip.AtNormalized(t);
        }

        // ------------------------------------------------------------ camadas

        void Build()
        {
            if (_built) return;
            _built = true;
            int layer = spriteRenderer.sortingLayerID, order = spriteRenderer.sortingOrder;

            // fora do Visual para não esticar/achatar junto; a cauda fica na frente da capa
            var root = transform.parent != null ? transform.parent : transform;
            var tailGo = new GameObject("Tail");
            tailGo.transform.SetParent(root, false);
            _tail = tailGo.AddComponent<PlayerTail>();
            _tail.Build(layer, order - 2);
            var capeGo = new GameObject("Cape");
            capeGo.transform.SetParent(root, false);
            _cape = capeGo.AddComponent<PlayerCape>();
            _cape.Build(layer, order - 3);

            _trim = Child("CapeTrim", animationSet.trimPixel, layer, order + 1);
            _chest = Child("LastSpark", animationSet.chestSpark, layer, order + 1);
        }

        SpriteRenderer Child(string n, Sprite s, int layer, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s;
            r.sortingLayerID = layer;
            r.sortingOrder = order;
            r.enabled = false;
            return r;
        }

        static readonly Color ChargeGlow = new Color(1f, 0.85f, 0.45f, 1f);

        void UpdateOverlay(SpriteFrame frame, bool attacking, bool allowed, Color color)
        {
            if (classOverlay == null) return;
            var def = classes != null ? classes.Current : null;
            Sprite s = null;
            bool carry = false;
            int order = spriteRenderer.sortingOrder;

            if (allowed && def != null)
            {
                if (frame.overlay != null) s = frame.overlay;                    // arma no golpe (marreta)
                else if (WeaponKey() == "hammer")
                {
                    var back = animationSet.Get("hammer_back");                    // marreta nas costas
                    if (back != null && !controller.IsHanging) { s = back.frames[0].sprite; carry = true; }
                }
                else if (def.Class != CharacterClass.Rogue && !controller.IsCrouching && !controller.IsHanging)
                {
                    // classes ainda sem arte própria: camada antiga da ClassDefinition
                    if (attacking) s = combat.IsWindingUp ? def.OverlayWindup : def.OverlayStrike;
                    if (s == null) s = def.OverlayIdle;
                }
            }
            classOverlay.sprite = s;
            classOverlay.enabled = s != null;
            classOverlay.flipX = _facing < 0;
            classOverlay.color = combat != null && combat.IsCharging
                ? Color.Lerp(color, ChargeGlow, 0.35f + 0.35f * Mathf.Sin(Time.time * Mathf.Lerp(10f, 22f, combat.Charge01)))
                : color; // a marreta brilha em dourado enquanto carrega
            classOverlay.sortingOrder = carry ? order - 1 : order + 2;
            classOverlay.transform.localPosition = carry
                ? new Vector3(frame.body.x * _facing, frame.body.y, 0f) * Px
                : Vector3.zero;
        }

        void UpdateTrimAndChest(SpriteFrame frame, bool visible, float alpha)
        {
            if (_trim != null)
            {
                bool show = visible && frame.hasTrim && _trim.sprite != null;
                _trim.enabled = show;
                if (show)
                {
                    _trim.transform.localPosition = new Vector3(frame.trim.x * _facing, frame.trim.y, 0f) * Px;
                    var c = TrimColor();
                    c.a = alpha;
                    _trim.color = c;
                }
            }
            if (_chest != null)
            {
                bool last = health != null && !health.IsDead && health.CurrentHits <= lastSparkHits && health.CurrentHits > 0;
                bool show = visible && last && _chest.sprite != null;
                _chest.enabled = show;
                if (show)
                {
                    _chest.transform.localPosition = new Vector3(_chestLocal.x, _chestLocal.y, 0f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f); // ciclo de 1 s
                    var c = Color.Lerp(new Color32(0xff, 0xd3, 0x4d, 0xff), new Color32(0xff, 0xf4, 0xc2, 0xff), pulse);
                    c.a = alpha * (0.6f + 0.4f * pulse);
                    _chest.color = c;
                }
            }
        }

        /// <summary>Cor da classe atual (faixa e barra da capa), clareando no brilho da troca.</summary>
        Color TrimColor()
        {
            var def = classes != null ? classes.Current : null;
            var c = def != null ? def.ThemeColor : new Color(0.17f, 0.23f, 0.4f);
            if (Time.time < _flashUntil) c = Color.Lerp(c, Color.white, (_flashUntil - Time.time) / switchFlashTime);
            return c;
        }

        /// <summary>Topo da aba da capa: 1,5 px atrás da faixa do ombro.</summary>
        static readonly Vector2 CapeFromTrim = new Vector2(-1.5f, 0.5f);

        void UpdateTailAndCape(SpriteFrame frame, bool visible, float dt, Color tint)
        {
            if (_tail == null || _cape == null) return;
            bool showTail = visible && frame.hasTail;
            bool showCape = visible && frame.hasTrim;
            _tail.SetVisible(showTail, tint);
            _cape.SetVisible(showCape, tint);
            if (!showTail && !showCape) return;

            BodyMood mood = BodyMood.Idle;
            Vector2 v = controller.Velocity;
            if (IsLyingFrame(frame)) mood = BodyMood.Lying;
            else if (_hurtTime < 0.3f) mood = BodyMood.Hurt;
            else if (controller.IsHanging || controller.IsClimbingLedge || (wallClimb != null && wallClimb.IsClimbing)) mood = BodyMood.Hang;
            else if (controller.IsCrouching) mood = BodyMood.Sneak;
            else if (!controller.IsGrounded && v.y < -2f) mood = BodyMood.Fall;
            else if (!controller.IsGrounded && v.y > 0.5f) mood = BodyMood.Jump;
            else if (Mathf.Abs(v.x) > 0.1f || !controller.IsGrounded) mood = BodyMood.Run;
            float speed01 = Mathf.Clamp01(Mathf.Abs(v.x) / 6f);

            if (showTail) _tail.Step(Anchor(frame.tail), _facing, mood, speed01, dt);
            if (showCape)
            {
                _cape.SetTrim(TrimColor());
                _cape.Step(Anchor(frame.trim + CapeFromTrim), _facing, mood, speed01, dt);
            }
        }

        /// <summary>Ponto do frame (px a partir do pivot, olhando para a direita) no mundo.</summary>
        Vector3 Anchor(Vector2 px) => transform.TransformPoint(new Vector3(px.x * _facing, px.y, 0f) * Px);

        // ------------------------------------------------------------ efeitos

        Vector3 ChestWorld() => transform.TransformPoint(new Vector3(_chestLocal.x, _chestLocal.y == 0f ? 0.7f : _chestLocal.y, 0f));

        Color SparkColor()
        {
            var def = classes != null ? classes.Current : null;
            Color c = def != null ? def.ThemeColor : new Color(0.17f, 0.23f, 0.4f);
            return Color.Lerp(c, Color.white, 0.4f);
        }

        void Burst()
        {
            // a morte: o gato explode em sete centelhas, uma por vida
            Vector3 chest = ChestWorld();
            for (int i = 0; i < 7; i++)
            {
                float a = (90f + i * 360f / 7f) * Mathf.Deg2Rad;
                Vector3 vel = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 4f;
                SpriteFx.Play(animationSet.spark, 0.06f, chest, false, SparkColor(),
                              spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 3, vel, 0.35f, true);
            }
        }

        void Dust(Sprite[] frames, float frameTime, Vector3 at, bool flip)
        {
            SpriteFx.Play(frames, frameTime, at, flip, Color.white, spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 3);
        }

        // ------------------------------------------------------------ placeholder

        void LegacyUpdate()
        {
            bool attacking = combat != null && combat.IsAttacking;
            int facing = attacking ? combat.AttackDirection : controller.FacingDirection;
            spriteRenderer.flipX = facing < 0;
            spriteRenderer.sprite = PickLegacySprite(attacking);

            float flash = Time.time < _flashUntil ? (_flashUntil - Time.time) / switchFlashTime : 0f;
            spriteRenderer.color = Color.Lerp(Color.white, _flashColor, flash);

            // Morto: deitado de lado. Invencível depois de tomar dano: pisca
            bool dead = health != null && (health.IsDead || health.IsCollapsed);
            spriteRenderer.transform.localRotation = dead ? Quaternion.Euler(0f, 0f, facing * 90f) : Quaternion.identity;
            spriteRenderer.enabled = dead || health == null || !health.IsInvulnerable || health.IsCollapsed || ((int)(Time.time * 20f) & 1) == 0;

            LegacyOverlay(attacking, dead);
            UpdateSlash();
        }

        Sprite PickLegacySprite(bool attacking)
        {
            if (controller.IsHanging) return hang;
            if (controller.IsClimbingLedge) return crouch;
            if (wallClimb != null && wallClimb.IsClimbing && climb != null && climb.Length > 0)
            {
                if (Mathf.Abs(wallClimb.ClimbVelocity) > 0.01f) _climbAnimTime += Time.deltaTime;
                _climbFrame = (int)(_climbAnimTime * runFps * 0.8f) % climb.Length;
                return climb[_climbFrame];
            }
            if (controller.IsCrouching) return crouch;
            if (attacking && attack != null) return attack;
            Vector2 v = controller.Velocity;
            if (!controller.IsGrounded) return v.y > 0f ? jump : fall;
            if (Mathf.Abs(v.x) > 0.1f && run != null && run.Length > 0)
                return run[(int)(Time.time * runFps) % run.Length];
            return idle;
        }

        void LegacyOverlay(bool attacking, bool dead)
        {
            if (classOverlay == null) return;
            var def = classes != null ? classes.Current : null;
            Sprite s = null;
            if (def != null && !dead && !controller.IsHanging && !controller.IsClimbingLedge &&
                !(wallClimb != null && wallClimb.IsClimbing) && !controller.IsCrouching)
            {
                if (attacking) s = combat.IsWindingUp ? def.OverlayWindup : def.OverlayStrike;
                if (s == null) s = def.OverlayIdle;
            }
            classOverlay.sprite = s;
            classOverlay.enabled = s != null && spriteRenderer.enabled;
            classOverlay.flipX = spriteRenderer.flipX;
            classOverlay.color = spriteRenderer.color;
        }

        void UpdateSlash()
        {
            if (slashRenderer == null) return;
            var def = classes != null ? classes.Current : null;
            var sprites = def != null && def.SlashSprites != null && def.SlashSprites.Length > 0 ? def.SlashSprites : slash;
            bool show = combat != null && combat.IsHitActive && sprites != null && sprites.Length > 0;
            slashRenderer.enabled = show;
            if (!show) return;

            combat.GetHitbox(out Vector2 center, out _);
            slashRenderer.sprite = sprites[Mathf.Clamp(combat.ComboStep, 0, sprites.Length - 1)];
            slashRenderer.flipX = combat.AttackDirection < 0;
            // O sprite do efeito tem pivot embaixo (pasta Characters): desce meia altura para centralizar na hitbox
            float half = slashRenderer.sprite != null ? slashRenderer.sprite.bounds.size.y * 0.5f : 0.5f;
            slashRenderer.transform.position = new Vector3(center.x, center.y - half, 0f);
        }
    }
}
