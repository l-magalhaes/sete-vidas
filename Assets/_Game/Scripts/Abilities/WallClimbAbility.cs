using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>
    /// Escalar paredes da layer Climbable (spec: Docs/Design/escalada.md).
    /// No ar: encostar segurando a direção da parede. No chão: encostar e apertar cima.
    /// W/S sobem e descem; Espaço salta para longe; segurar a direção oposta solta.
    /// </summary>
    public class WallClimbAbility : PlayerAbility
    {
        const float Probe = 0.08f;      // folga lateral para detectar a parede (tiles)
        const float InputThreshold = 0.5f;

        [SerializeField] WallClimbSettings settings;
        [Tooltip("Layers escaláveis")]
        [SerializeField] LayerMask climbableLayers;

        float _reattachTimer;

        /// <summary>Lado da parede: 1 = parede à direita, -1 = à esquerda.</summary>
        public int WallDirection { get; private set; }
        public bool IsClimbing => Player != null && Player.ActiveAbility == this;
        /// <summary>Velocidade vertical da escalada neste passo (para a animação).</summary>
        public float ClimbVelocity { get; private set; }
        public WallClimbSettings Settings => settings;

        protected override void Awake()
        {
            base.Awake();
            if (climbableLayers == 0) climbableLayers = LayerMask.GetMask(GameConstants.Layers.Climbable);
            if (settings == null)
            {
                Debug.LogError("[Sete Vidas] WallClimbAbility sem WallClimbSettings.", this);
                enabled = false;
            }
        }

        void FixedUpdate()
        {
            _reattachTimer -= Time.fixedDeltaTime;
            _wallCoyote -= Time.fixedDeltaTime;
        }

        float _wallCoyote;      // acabou de soltar a parede: o Espaço ainda pula dela
        int _coyoteWall;
        float _awayTimer;       // segurando a direção oposta (solta depois de ReleaseHoldTime)
        float _hopVy;           // pulo para cima agarrado (W + Espaço ou direção da parede + Espaço)

        /// <summary>No meio do pulo para cima agarrado na parede.</summary>
        public bool IsHopping => _hopVy > 0f;

        public override bool TryBegin(bool grounded)
        {
            // soltou a parede há um instante e apertou Espaço: ainda vale o pulo da parede (para o lado oposto)
            if (!grounded && _wallCoyote > 0f && Player.Timers.HasBufferedJump)
            {
                Player.Timers.Consume();
                WallDirection = _coyoteWall;
                _wallCoyote = 0f;
                WallJump();
                return false;
            }
            if (_reattachTimer > 0f || Player.IsCrouching) return false;

            float moveX = Player.Input.MoveX, moveY = Player.Input.MoveY;
            int dir = moveX > 0.01f ? 1 : moveX < -0.01f ? -1 : 0;
            bool up = moveY > InputThreshold;

            if (grounded)
            {
                if (!up) return false;             // no chão, só agarra com "cima"
                if (dir == 0) dir = Player.FacingDirection;
            }
            else if (dir == 0) return false;       // no ar, precisa segurar a direção da parede

            if (!TouchingWall(dir, out float wallFaceX)) return false;

            WallDirection = dir;
            Player.Face(dir);
            Player.Timers.Consume(); // um pulo apertado antes de agarrar não vira pulo da parede
            var body = Player.Body;
            body.position = new Vector2(wallFaceX - dir * (HalfWidth + 0.01f), body.position.y);
            body.linearVelocity = Vector2.zero;
            ClimbVelocity = 0f;
            return true;
        }

        public override bool Step(float dt)
        {
            var body = Player.Body;

            // Espaço: o lado apontado decide (pedido do usuário). Para longe ou sem direção: pula para longe.
            // Para a parede ou para cima: pulo para cima que agarra de novo.
            if (Player.Timers.HasBufferedJump)
            {
                Player.Timers.Consume();
                var choice = WallJumpRules.Choose(Player.Input.MoveX, Player.Input.MoveY, WallDirection, InputThreshold);
                if (choice == WallJumpRules.Kind.Away)
                {
                    WallJump();
                    return false;
                }
                _hopVy = Player.Settings.LaunchVelocityFor(settings.WallHopHeight, dt);
                _awayTimer = 0f;
            }

            // Segurar a direção oposta solta, mas só depois de um instante: apertar A/D e Espaço quase juntos pula
            if (Player.Input.MoveX * WallDirection < -InputThreshold && _hopVy <= 0f)
            {
                _awayTimer += dt;
                if (_awayTimer >= settings.ReleaseHoldTime)
                {
                    Release();
                    _wallCoyote = settings.WallJumpCoyote;
                    _coyoteWall = WallDirection;
                    return false;
                }
            }
            else _awayTimer = 0f;

            float moveY = Player.Input.MoveY;
            float vy = moveY > InputThreshold ? settings.ClimbUpSpeed
                     : moveY < -InputThreshold ? -settings.ClimbDownSpeed
                     : 0f;
            if (_hopVy > 0f)
            {
                // pulo agarrado: sobe com a gravidade do pulo e volta a segurar quando para de subir
                vy = _hopVy;
                _hopVy -= Player.Settings.Gravity * dt;
            }

            // Chegou ao chão descendo
            Player.Ground.Refresh(Player.Settings.ColliderWidth);
            if (vy < 0f && Player.Ground.IsGrounded)
            {
                Release();
                return false;
            }

            // Topo: as patas passaram da parede. Sobe para cima dela, se couber.
            if (vy > 0f && !HandsOnWall(body.position.y + vy * dt))
            {
                if (Player.Ground.TryFindLedge(WallDirection, Player.Settings.ColliderWidth,
                        Player.Settings.LedgeHandHeight, Player.Settings.LedgeGrabWindow,
                        Player.Settings.StandingHeight, out Vector2 corner))
                {
                    ClimbVelocity = 0f;
                    _hopVy = 0f;
                    _reattachTimer = settings.ReattachCooldown;
                    Player.ClimbOnto(corner, WallDirection);
                    return false;
                }
                vy = 0f; // sem espaço em cima: para no topo
                _hopVy = 0f;
            }

            // A parede acabou (desceu além da base no ar, ou ela sumiu)
            if (!TouchingWall(WallDirection, out _))
            {
                Release();
                return false;
            }

            ClimbVelocity = vy;
            body.linearVelocity = new Vector2(0f, vy);
            return true;
        }

        public override void OnInterrupted() => Release();

        void WallJump()
        {
            _hopVy = 0f;
            int away = -WallDirection;
            float vy = Player.Settings.LaunchVelocityFor(settings.WallJumpHeight, Time.fixedDeltaTime);
            Player.Face(away);
            Player.StartJump(new Vector2(away * settings.WallJumpSpeedX, vy));
            Player.LockHorizontalControl(settings.WallJumpInputLock);
            _reattachTimer = settings.ReattachCooldown;
            ClimbVelocity = 0f;
        }

        void Release()
        {
            _hopVy = 0f;
            _awayTimer = 0f;
            _reattachTimer = settings.ReattachCooldown;
            ClimbVelocity = 0f;
        }

        // ------------------------------------------------------------ sensores

        float HalfWidth => Player.Settings.ColliderWidth * 0.5f;

        /// <summary>O corpo encosta numa parede escalável do lado <paramref name="dir"/>?</summary>
        bool TouchingWall(int dir, out float wallFaceX)
        {
            wallFaceX = 0f;
            Vector2 feet = Player.Body.position;
            float height = Player.CurrentHeight;
            // Raios na altura do peito e dos pés: acha a face da parede
            for (int i = 0; i < 2; i++)
            {
                float y = feet.y + (i == 0 ? height * 0.6f : 0.15f);
                var hit = Physics2D.Raycast(new Vector2(feet.x, y), new Vector2(dir, 0f), HalfWidth + Probe, climbableLayers);
                if (hit.collider != null)
                {
                    wallFaceX = hit.point.x;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Ainda há parede escalável na altura das patas, com os pés em <paramref name="feetY"/>?</summary>
        bool HandsOnWall(float feetY)
        {
            float handY = feetY + Player.Settings.LedgeHandHeight - 0.1f;
            var origin = new Vector2(Player.Body.position.x, handY);
            return Physics2D.Raycast(origin, new Vector2(WallDirection, 0f), HalfWidth + Probe, climbableLayers).collider != null;
        }
    }
}

namespace SeteVidas.Abilities
{
    /// <summary>Regras puras do Espaço na parede (EditMode).</summary>
    public static class WallJumpRules
    {
        public enum Kind { Away, Hop }

        /// <summary>
        /// Para longe da parede (ou sem direção): pulo para longe. Para a parede ou para cima (W):
        /// pulo para cima que agarra de novo.
        /// </summary>
        public static Kind Choose(float moveX, float moveY, int wallDirection, float threshold)
        {
            float toward = moveX * wallDirection;
            if (toward < -threshold) return Kind.Away;
            if (toward > threshold || moveY > threshold) return Kind.Hop;
            return Kind.Away;
        }
    }
}
