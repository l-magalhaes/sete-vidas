using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Furtividade do gato: calcula a visibilidade (luz × agachar) e emite ruídos de passos,
    /// aterrissagem e golpes (spec: Docs/Design/furtividade-sentinela.md). Os inimigos leem <see cref="Active"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class StealthController : MonoBehaviour
    {
        [SerializeField] StealthSettings settings;
        [Tooltip("Mostra a visibilidade no canto da tela (depuração)")]
        [SerializeField] bool showDebugHud; // só depuração: a silhueta na sombra responde "estou escondido?" (melhoria 1)

        PlayerController _player;
        CombatController _combat;
        PlayerHealth _health;
        float _footstepTimer;
        bool _wasAirborne;
        float _airPeakY;

        /// <summary>O gato atual (o MVP tem um só).</summary>
        public static StealthController Active { get; private set; }

        /// <summary>0 = invisível, 1 = totalmente exposto.</summary>
        public float Visibility { get; private set; } = 1f;
        public float LightExposure { get; private set; } = 1f;
        /// <summary>Multiplicador do esconderijo onde o gato está (1 = nenhum). Só vale agachado.</summary>
        public float Concealment { get; private set; } = 1f;
        /// <summary>Agachado dentro de um matinho/esconderijo.</summary>
        public bool InCover => _player != null && _player.IsCrouching && Concealment < 1f;
        /// <summary>Inimigos podem ver e mirar o gato (falso enquanto ele está morto).</summary>
        public bool IsTargetable => _health == null || !_health.IsDead;
        public bool IsHidden => settings != null && Visibility <= settings.HiddenThreshold;
        public PlayerController Player => _player;
        public StealthSettings Settings => settings;

        /// <summary>Ponto que os inimigos tentam ver (peito do gato).</summary>
        public Vector2 ChestPosition
        {
            get
            {
                Vector2 feet = _player.Body != null ? _player.Body.position : (Vector2)transform.position;
                return feet + Vector2.up * (_player.CurrentHeight * 0.6f);
            }
        }

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            // silhueta na sombra (melhoria 1): fica junto da furtividade, sem precisar recriar o prefab
            if (GetComponent<ShadowSilhouette>() == null) gameObject.AddComponent<ShadowSilhouette>();
            _combat = GetComponent<CombatController>();
            _health = GetComponent<PlayerHealth>();
            if (settings == null)
            {
                Debug.LogError("[Sete Vidas] StealthController sem StealthSettings.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            Active = this;
            if (_combat != null) _combat.SwingStarted += OnSwing;
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
            if (_combat != null) _combat.SwingStarted -= OnSwing;
        }

        void Update()
        {
            LightExposure = LightZone.ExposureAt(ChestPosition);
            Concealment = HidingSpot.ConcealmentAt(ChestPosition);
            Visibility = StealthMath.Visibility(LightExposure, _player.IsCrouching, settings.CrouchVisibilityMultiplier, Concealment);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 feet = _player.Body.position;
            bool supported = _player.IsHanging || _player.IsClimbingLedge || _player.ActiveAbility != null;
            bool grounded = _player.IsGrounded && _player.Velocity.y <= 0.01f;

            // Passos: correndo em pé no chão
            float speed = Mathf.Abs(_player.Velocity.x);
            bool running = grounded && !_player.IsCrouching &&
                           speed >= settings.FootstepMinSpeedFraction * _player.Settings.MoveSpeed;
            if (running)
            {
                _footstepTimer -= dt;
                if (_footstepTimer <= 0f)
                {
                    NoiseSystem.Emit(feet, settings.FootstepRadius, settings.FootstepLoudness, gameObject, "passo");
                    _footstepTimer = settings.FootstepInterval;
                }
            }
            else _footstepTimer = settings.FootstepInterval * 0.5f; // o 1º passo sai logo depois de começar a correr

            // Aterrissagem: a altura conta a partir do ponto mais alto no ar (ou de onde se pendurou/escalou)
            if (supported || grounded)
            {
                if (grounded && _wasAirborne &&
                    StealthMath.LandingIsLoud(_airPeakY - feet.y, settings.LandingNoiseHeight))
                    NoiseSystem.Emit(feet, settings.LandingRadius, settings.LandingLoudness, gameObject, "aterrissagem");
                _wasAirborne = false;
                _airPeakY = feet.y;
            }
            else
            {
                if (!_wasAirborne) _airPeakY = feet.y;
                _wasAirborne = true;
                _airPeakY = Mathf.Max(_airPeakY, feet.y);
            }
        }

        void OnSwing()
        {
            if (settings == null) return;
            float k = _combat != null && _combat.Weapon != null ? _combat.Weapon.NoiseMultiplier : 1f;
            NoiseSystem.Emit(ChestPosition, settings.AttackNoiseRadius * k, Mathf.Clamp01(settings.AttackLoudness * k), gameObject, "golpe");
        }

        void OnGUI()
        {
            if (!showDebugHud || settings == null) return;
            string where = LightExposure < 1f ? "sombra" : "luz";
            string pose = InCover ? ", agachado no mato" : _player.IsCrouching ? ", agachado" : Concealment < 1f ? ", de pé no mato" : "";
            string hidden = IsHidden ? "  [ESCONDIDO]" : "";
            GUI.Label(new Rect(10, Screen.height - 30, 480, 24), $"Visibilidade {Visibility * 100f:0}% ({where}{pose}){hidden}");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            // Ruídos do último segundo e meio: círculo do raio de alcance
            foreach (var n in NoiseSystem.Recent)
            {
                float age = Time.time - n.Time;
                if (age > 1.5f) continue;
                UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f, 1f - age / 1.5f);
                UnityEditor.Handles.DrawWireDisc(n.Position, Vector3.forward, n.Radius);
            }
        }
#endif
    }
}
