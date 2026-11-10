using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Olhos e ouvidos do inimigo: cone de visão com linha de visão (paredes bloqueiam), alcance
    /// reduzido pela visibilidade do gato, e audição dos ruídos do <see cref="NoiseSystem"/>.
    /// Não decide nada: só alimenta o <see cref="AlertBrain"/>.
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        [Tooltip("Layers que bloqueiam a visão")]
        [SerializeField] LayerMask occluders;

        SentinelSettings _settings;
        AlertBrain _brain;
        Rigidbody2D _body;

        /// <summary>Viu o gato no último passo.</summary>
        public bool SeesPlayer { get; private set; }
        /// <summary>Alcance efetivo contra o gato no último passo (para os Gizmos).</summary>
        public float LastEffectiveRange { get; private set; }

        public void Init(SentinelSettings settings, AlertBrain brain)
        {
            _settings = settings;
            _brain = brain;
            _body = GetComponent<Rigidbody2D>();
            if (occluders == 0)
                occluders = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            LastEffectiveRange = settings.VisionRange;
        }

        void OnEnable() => NoiseSystem.Emitted += OnNoise;
        void OnDisable() => NoiseSystem.Emitted -= OnNoise;

        Vector2 Feet => _body != null ? _body.position : (Vector2)transform.position;
        public Vector2 Eye => Feet + Vector2.up * (_settings != null ? _settings.EyeHeight : 1.3f);

        /// <summary>Olha uma vez. Chame a cada passo de física, antes de <see cref="AlertBrain.Tick"/>.</summary>
        public void Sense(int facing, float dt)
        {
            SeesPlayer = false;
            var player = StealthController.Active;
            if (_brain == null || player == null || !player.isActiveAndEnabled || !player.IsTargetable) return;

            float visibility = player.Visibility;
            if (_brain.State == AlertState.Combat)
                visibility = Mathf.Max(visibility, _settings.CombatVisibilityFloor);

            float effective = PerceptionMath.EffectiveRange(_settings.VisionRange, visibility);
            LastEffectiveRange = effective;

            Vector2 eye = Eye, target = player.ChestPosition;
            if (!PerceptionMath.InVisionCone(eye, facing, target, effective, _settings.VisionHalfAngle, _settings.VisionTiltDown)) return;
            if (Physics2D.Linecast(eye, target, occluders).collider != null) return; // parede no caminho

            float distance = Vector2.Distance(eye, target);
            _brain.See(target, PerceptionMath.SightFillRate(distance, effective, _settings.SightFillRate), dt);
            SeesPlayer = true;
        }

        void OnNoise(NoiseEvent noise)
        {
            if (_brain == null || noise.Source == gameObject) return;
            float radius = noise.Radius * _settings.HearingMultiplier;
            if ((noise.Position - Feet).sqrMagnitude <= radius * radius)
                _brain.Hear(noise.Position, noise.Loudness);
        }
    }
}
