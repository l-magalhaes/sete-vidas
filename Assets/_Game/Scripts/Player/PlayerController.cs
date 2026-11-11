using SeteVidas.Abilities;
using SeteVidas.Combat;
using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Andar, pular, agachar e agarrar bordas (spec: Docs/Design/controlador-gato.md).
    /// Usa gravidade própria (gravityScale = 0) para que a altura do pulo em tiles seja exata.
    /// Colisor em caixa: fundo reto, para o gato não escorregar das quinas.
    /// Habilidades de locomoção (escalada, depois pulo duplo e dash) são componentes <see cref="PlayerAbility"/>
    /// no mesmo objeto: o controlador passa o controle para elas sem precisar ser editado.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GroundCheck))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerMovementSettings settings;
        [SerializeField] PlayerInputReader input;

        Rigidbody2D _body;
        BoxCollider2D _collider;
        GroundCheck _ground;
        readonly JumpTimers _timers = new JumpTimers();
        bool _isJumping;        // subindo por causa de um pulo (permite o corte ao soltar o botão)
        bool _dropRequested;    // S apertado enquanto pendurado
        float _regrabTimer;     // tempo até poder agarrar borda de novo
        float _moveLockTimer;   // tempo em que a entrada horizontal não mexe na velocidade
        float _slopeLockTimer;  // depois de pulo/empurrão, não cola na rampa
        bool _wasGrounded;      // no passo anterior (para colar no começo de uma descida)
        bool _snapped;          // neste passo, colou na rampa sem estar encostado ainda

        PlayerAbility[] _abilities = new PlayerAbility[0];

        // Subida da borda
        float _climbElapsed;
        Vector2 _climbFrom, _climbMid, _climbTo;

        /// <summary>Agachado. A IA de furtividade (Fase 3) lê esta flag.</summary>
        public bool IsCrouching { get; private set; }
        /// <summary>Pendurado numa borda.</summary>
        public bool IsHanging { get; private set; }
        /// <summary>Subindo de uma borda para cima dela.</summary>
        public bool IsClimbingLedge { get; private set; }
        public bool IsGrounded => _ground != null && (_ground.IsGrounded || _snapped);
        /// <summary>De pé numa rampa andável.</summary>
        public bool IsOnSlope => _ground != null && _ground.OnSlope;
        /// <summary>1 = olhando para a direita, -1 = para a esquerda.</summary>
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;
        public PlayerMovementSettings Settings => settings;

        // Usados pelas habilidades e pelo combate
        public Rigidbody2D Body => _body;
        public PlayerInputReader Input => input;
        public GroundCheck Ground => _ground;
        public JumpTimers Timers => _timers;
        /// <summary>Habilidade com o controle agora (null = movimento normal).</summary>
        public PlayerAbility ActiveAbility { get; private set; }
        /// <summary>Mantém o gato agachado sem o botão (cenas e testes).</summary>
        public bool HoldCrouch { get; set; }

        /// <summary>Multiplicador externo da velocidade de andar (ex.: marreta lenta durante o golpe). 1 = normal.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Altura atual do colisor (em pé ou agachado), em tiles.</summary>
        public float CurrentHeight => settings == null ? 0f : IsCrouching ? settings.CrouchHeight : settings.StandingHeight;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _ground = GetComponent<GroundCheck>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            _abilities = GetComponents<PlayerAbility>();

            if (settings == null)
            {
                Debug.LogError("[Sete Vidas] PlayerController sem PlayerMovementSettings.", this);
                enabled = false;
                return;
            }

            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            ApplyColliderHeight(settings.StandingHeight);
        }

        void Update()
        {
            // "Apertou neste frame" só é confiável no Update; os pedidos ficam guardados para a física.
            if (input.JumpPressedThisFrame)
                _timers.OnJumpPressed(settings.JumpBufferTime);
            if (IsHanging && input.CrouchPressedThisFrame)
                _dropRequested = true;

            // filtrado no analógico (retorno da mola, diagonal de agachar): ver FacingFilter. Lido todo quadro,
            // mesmo com a direção travada, para o filtro contar o tempo certo
            int wanted = input.FacingIntent;

            // A direção fica travada na borda, durante habilidades e logo depois de um pulo da parede
            if (IsHanging || IsClimbingLedge || _moveLockTimer > 0f) return;
            if (ActiveAbility != null && ActiveAbility.LocksFacing) return;
            if (wanted != 0) FacingDirection = wanted;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _regrabTimer -= dt;
            _moveLockTimer -= dt;
            _slopeLockTimer -= dt;
            _snapped = false;

            if (IsClimbingLedge) { StepLedgeClimb(dt); _timers.Tick(dt); _wasGrounded = false; return; }
            if (IsHanging) { StepHanging(); _timers.Tick(dt); _wasGrounded = false; return; }
            if (ActiveAbility != null)
            {
                if (!ActiveAbility.enabled || !ActiveAbility.Step(dt)) ActiveAbility = null;
                _timers.Tick(dt);
                _wasGrounded = false;
                return;
            }

            Vector2 v = _body.linearVelocity;

            _ground.Refresh(settings.ColliderWidth, settings.MaxSlopeAngle);
            // Subindo a rampa o gato tem velocidade para cima e continua no chão; depois de um pulo, não
            bool canStick = _slopeLockTimer <= 0f && !_isJumping;
            bool grounded = _ground.IsGrounded && (v.y <= 0.01f || canStick);

            // Começo de uma descida: cola na rampa em vez de sair voando por um instante
            float snapDistance = 0f;
            if (!grounded && _wasGrounded && canStick && v.y <= 0.01f &&
                _ground.TryFindSnap(settings.ColliderWidth, settings.SlopeSnapDistance, out snapDistance))
            {
                grounded = true;
                _snapped = true;
            }

            if (grounded)
            {
                _timers.OnGrounded(settings.CoyoteTime);
                _isJumping = false;
            }

            UpdateCrouch(grounded);

            // Habilidades de locomoção: a primeira que quiser assume o controle
            foreach (var ability in _abilities)
            {
                if (ability == null || !ability.enabled || !ability.TryBegin(grounded)) continue;
                ActiveAbility = ability;
                _isJumping = false;
                _timers.Tick(dt);
                return;
            }

            // Andar (logo depois de um pulo da parede, a entrada não freia o impulso)
            if (_moveLockTimer <= 0f)
            {
                float target = MoveInput() * settings.MoveSpeed * SpeedMultiplier * (IsCrouching ? settings.CrouchSpeedMultiplier : 1f);
                float rate = Mathf.Abs(target) > 0.01f ? settings.Acceleration : settings.Deceleration;
                v.x = Mathf.MoveTowards(v.x, target, rate * dt);
            }

            // Rampa íngreme demais à frente: andando, o gato não tenta subir nela (senão fica pulando no pé dela)
            if (grounded && Mathf.Abs(v.x) > 0.01f && _ground.SteepAhead(v.x > 0f ? 1 : -1, settings.ColliderWidth))
                v.x = 0f;

            // Pular (agachado, só se houver espaço para ficar de pé)
            bool jumped = false;
            if (_timers.CanJump && (!IsCrouching || TryStandUp()))
            {
                v.y = settings.LaunchVelocity(dt);
                _timers.Consume();
                _isJumping = true;
                jumped = true;
                _slopeLockTimer = settings.SlopeJumpGrace;
            }

            // Pulo variável: soltar o botão corta a subida
            if (_isJumping && v.y > 0f && !input.JumpHeld)
            {
                v.y *= settings.JumpCutMultiplier;
                _isJumping = false;
            }

            bool onSlope = grounded && !jumped && (_snapped || _ground.OnSlope);
            if (onSlope && _snapped)
            {
                // desce exatamente até encostar neste passo
                v.y = -(snapDistance + 0.01f) / dt;
            }
            else if (onSlope)
            {
                // Mesma velocidade horizontal do plano; a vertical acompanha a rampa. Sem gravidade: parado,
                // o gato não escorrega. O empurrão vai contra a normal, então não puxa rampa abaixo.
                Vector2 n = _ground.GroundNormal;
                v.y = SlopeMath.VerticalSpeedFor(n, v.x);
                v -= n * settings.SlopeStickSpeed;
            }
            else
            {
                // Saiu de uma rampa para o plano ainda com velocidade para cima: para no plano
                if (grounded && !jumped && v.y > 0f) v.y = 0f;

                // Gravidade própria, mais forte na queda
                float gravity = settings.Gravity * (v.y < 0f ? settings.FallGravityMultiplier : 1f);
                v.y = Mathf.Max(v.y - gravity * dt, -settings.MaxFallSpeed);
            }

            v.x = BlockAgainstBodies(v.x, dt);
            _body.linearVelocity = v;
            _timers.Tick(dt);
            _wasGrounded = grounded && !jumped;

            if (!grounded && v.y <= 0f) TryGrabLedge();
        }

        /// <summary>
        /// Entrada horizontal para andar. No analógico, um x contra o lado em que o gato olha só move depois que a
        /// virada valer (FacingFilter): o retorno da mola não empurra o gato para trás e ele nunca anda de costas.
        /// </summary>
        float MoveInput()
        {
            float x = input.MoveX;
            if (input.IsAnalog && x * FacingDirection < 0f) return 0f;
            return x;
        }

        // ------------------------------------------------ esbarrar nos inimigos

        int _blockMask = -1;

        /// <summary>
        /// Corpos dos inimigos vivos (melhoria 6): andando contra um, o gato para ao encostar; se ficou por dentro
        /// (caiu em cima, o inimigo chegou nele), sai para o lado livre. Pular por cima continua valendo, e agachado
        /// atrás de um esconderijo o gato fica "atrás" do inimigo e ninguém esbarra em ninguém.
        /// </summary>
        float BlockAgainstBodies(float vx, float dt)
        {
            var bodies = SolidBodies.All;
            if (bodies.Count == 0) return vx;
            var stealth = StealthController.Active;
            if (stealth != null && stealth.Player == this && stealth.InCover) return vx;

            Vector2 feet = _body.position;
            float w = settings.ColliderWidth, h = CurrentHeight;
            var me = new Rect(feet.x - w * 0.5f, feet.y, w, h);
            float push = 0f;
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                var body = bodies[i];
                if (body == null || !body.IsSolid) continue;
                Rect b = body.Bounds;
                if (b.width <= 0f || !BodyBlockRules.OverlapsVertically(me, b)) continue;

                if (BodyBlockRules.HorizontalOverlap(me, b) > BodyBlockRules.InsideOverlap)
                {
                    int dir = BodyBlockRules.PushOutDirection(me.center.x, b.center.x,
                        SideIsFree(b.xMin - w * 0.5f - 0.02f, feet.y, w, h),
                        SideIsFree(b.xMax + w * 0.5f + 0.02f, feet.y, w, h));
                    if (dir != 0) push = dir * settings.BodyPushOutSpeed;
                    continue;
                }

                float before = vx;
                vx = BodyBlockRules.ClampVelocity(me, b, vx, dt);
                // empurrando contra o corpo (encostado e querendo ir para ele): a IA sente o esbarrão
                bool towards = (b.center.x - me.center.x) * input.MoveX > 0f;
                float gap = b.center.x > me.center.x ? b.xMin - me.xMax : me.xMin - b.xMax;
                if (towards && gap <= BodyBlockRules.ContactGap && (before != vx || Mathf.Abs(before) < 0.01f))
                    body.OnBumped(feet);
            }
            if (push != 0f && (vx == 0f || Mathf.Sign(vx) != Mathf.Sign(push) || Mathf.Abs(vx) < Mathf.Abs(push))) vx = push;
            return vx;
        }

        /// <summary>O gato cabe ali sem entrar em parede nem em perigo.</summary>
        bool SideIsFree(float centerX, float feetY, float w, float h)
        {
            if (_blockMask < 0)
                _blockMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable,
                    GameConstants.Layers.Breakable, GameConstants.Layers.Hazard);
            return Physics2D.OverlapBox(new Vector2(centerX, feetY + h * 0.5f + 0.05f), new Vector2(w - 0.1f, h - 0.15f), 0f, _blockMask) == null;
        }

        // ------------------------------------------------- API das habilidades

        /// <summary>Vira o gato para <paramref name="dir"/> (1 ou -1).</summary>
        public void Face(int dir)
        {
            if (dir != 0) FacingDirection = dir > 0 ? 1 : -1;
        }

        /// <summary>Lança o gato com esta velocidade. Sem pulo variável: soltar o botão não corta a subida.</summary>
        public void StartJump(Vector2 velocity)
        {
            _body.linearVelocity = velocity;
            _isJumping = false;
            _slopeLockTimer = settings.SlopeJumpGrace;
            _wasGrounded = false;
        }

        /// <summary>Durante este tempo, a entrada horizontal não muda a velocidade nem a direção.</summary>
        public void LockHorizontalControl(float seconds) => _moveLockTimer = Mathf.Max(_moveLockTimer, seconds);

        /// <summary>Sobe para cima da quina <paramref name="corner"/> (mesma animação da borda).</summary>
        public void ClimbOnto(Vector2 corner, int dir)
        {
            FacingDirection = dir;
            _climbTo = LedgeStandPoint(corner, dir);
            StartLedgeClimb();
        }

        /// <summary>
        /// Empurrão externo (dano): solta borda e parede, aplica a velocidade e não deixa a entrada
        /// horizontal anular o recuo por <paramref name="controlLock"/> segundos.
        /// </summary>
        public void ApplyKnockback(Vector2 velocity, float controlLock)
        {
            if (IsClimbingLedge)
            {
                IsClimbingLedge = false;
                _body.bodyType = RigidbodyType2D.Dynamic;
            }
            if (IsHanging)
            {
                IsHanging = false;
                _regrabTimer = settings.LedgeRegrabCooldown;
            }
            InterruptAbility();
            _isJumping = false;
            _timers.Consume();
            _body.linearVelocity = velocity;
            _slopeLockTimer = settings.SlopeJumpGrace; // o pulinho do recuo não é cancelado pela rampa
            _wasGrounded = false;
            LockHorizontalControl(controlLock);
        }

        /// <summary>Coloca o gato em <paramref name="feet"/> parado, de pé e sem nenhum estado especial (respawn, carregar save).</summary>
        public void Teleport(Vector2 feet)
        {
            if (IsClimbingLedge)
            {
                IsClimbingLedge = false;
                _body.bodyType = RigidbodyType2D.Dynamic;
            }
            IsHanging = false;
            InterruptAbility();
            if (IsCrouching && _ground.HasRoomFor(settings.ColliderWidth, settings.StandingHeight))
            {
                IsCrouching = false;
                ApplyColliderHeight(settings.StandingHeight);
            }
            _isJumping = false;
            _timers.Consume();
            _moveLockTimer = 0f;
            _body.position = feet;
            _body.linearVelocity = Vector2.zero;
            transform.position = feet;
        }

        /// <summary>Tira o controle da habilidade ativa (dano, morte, cutscene).</summary>
        public void InterruptAbility()
        {
            if (ActiveAbility == null) return;
            var ability = ActiveAbility;
            ActiveAbility = null;
            ability.OnInterrupted();
        }

        // ------------------------------------------------------------ agachar

        void UpdateCrouch(bool grounded)
        {
            bool wantsCrouch = input.CrouchHeld || HoldCrouch;

            if (!IsCrouching)
            {
                if (wantsCrouch && grounded)
                {
                    IsCrouching = true;
                    ApplyColliderHeight(settings.CrouchHeight);
                }
            }
            else if (!wantsCrouch || !grounded)
            {
                TryStandUp(); // sob teto baixo, continua agachado
            }
        }

        bool TryStandUp()
        {
            if (!IsCrouching) return true;
            if (!_ground.HasRoomFor(settings.ColliderWidth, settings.StandingHeight)) return false;
            IsCrouching = false;
            ApplyColliderHeight(settings.StandingHeight);
            return true;
        }

        /// <summary>
        /// Cantos levemente arredondados: a caixa desliza sobre emendas entre tiles em vez de enganchar
        /// (o gato parava de andar numa direção). O raio é pequeno, então o fundo continua reto e ele
        /// não escorrega das quinas. O tamanho externo continua sendo largura × altura.
        /// </summary>
        public const float ColliderEdgeRadius = 0.04f;

        void ApplyColliderHeight(float height)
        {
            _collider.edgeRadius = ColliderEdgeRadius;
            _collider.size = new Vector2(settings.ColliderWidth - 2f * ColliderEdgeRadius, height - 2f * ColliderEdgeRadius);
            _collider.offset = new Vector2(0f, height * 0.5f);
        }

        // ------------------------------------------------------- agarrar borda

        void TryGrabLedge()
        {
            if (!settings.LedgeGrabEnabled || _regrabTimer > 0f || IsCrouching) return;
            if (input.CrouchHeld) return; // segurando S = quer cair

            // Só agarra a borda para a qual o jogador está empurrando
            int dir = input.MoveX > 0.01f ? 1 : input.MoveX < -0.01f ? -1 : 0;
            if (dir == 0) return;

            if (!_ground.TryFindLedge(dir, settings.ColliderWidth, settings.LedgeHandHeight,
                    settings.LedgeGrabWindow, settings.StandingHeight, out Vector2 corner))
                return;

            IsHanging = true;
            _isJumping = false;
            _dropRequested = false;
            FacingDirection = dir;
            _climbTo = LedgeStandPoint(corner, dir);
            _body.position = new Vector2(corner.x - dir * (settings.ColliderWidth * 0.5f + 0.01f),
                                         corner.y - settings.LedgeHandHeight);
            _body.linearVelocity = Vector2.zero;
        }

        Vector2 LedgeStandPoint(Vector2 corner, int dir) =>
            new Vector2(corner.x + dir * (settings.ColliderWidth * 0.5f + 0.05f), corner.y + 0.01f);

        void StepHanging()
        {
            _body.linearVelocity = Vector2.zero;

            if (_dropRequested)
            {
                _dropRequested = false;
                IsHanging = false;
                _regrabTimer = settings.LedgeRegrabCooldown;
                return;
            }

            if (_timers.HasBufferedJump)
            {
                _timers.Consume();
                StartLedgeClimb();
            }
        }

        void StartLedgeClimb()
        {
            IsHanging = false;
            IsClimbingLedge = true;
            _climbElapsed = 0f;
            _climbFrom = _body.position;
            _climbMid = new Vector2(_climbFrom.x, _climbTo.y); // sobe reto, depois avança sobre a borda
            _body.bodyType = RigidbodyType2D.Kinematic;          // atravessa a quina durante a animação
            _body.linearVelocity = Vector2.zero;
        }

        void StepLedgeClimb(float dt)
        {
            _climbElapsed += dt;
            float t = Mathf.Clamp01(_climbElapsed / settings.LedgeClimbDuration);
            const float upPart = 0.6f; // 60% do tempo subindo, 40% avançando
            Vector2 p = t < upPart
                ? Vector2.Lerp(_climbFrom, _climbMid, t / upPart)
                : Vector2.Lerp(_climbMid, _climbTo, (t - upPart) / (1f - upPart));
            _body.MovePosition(p);

            if (t >= 1f)
            {
                IsClimbingLedge = false;
                _body.bodyType = RigidbodyType2D.Dynamic;
                _body.position = _climbTo;
                _body.linearVelocity = Vector2.zero;
            }
        }
    }
}
