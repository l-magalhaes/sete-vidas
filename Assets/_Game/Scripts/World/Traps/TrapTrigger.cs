using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Gatilho de armadilha "inativa": só machuca se o gato a ativar.
    /// <list type="bullet">
    /// <item><b>Fio de alarme:</b> esticado na altura do peito. De pé (ou pulando baixo) esbarra; agachado passa por baixo.</item>
    /// <item><b>Placa de pressão:</b> no chão. Pisando de pé dispara; andando agachado (passos leves) não.</item>
    /// </list>
    /// Disparar faz barulho alto (as Sentinelas ouvem) e aciona os <see cref="TrapEffect"/> ligados. Rearma sozinho.
    /// </summary>
    public class TrapTrigger : MonoBehaviour
    {
        public enum Kind { Tripwire, PressurePlate }

        [SerializeField] Kind kind = Kind.Tripwire;
        [SerializeField] TrapEffect[] effects = new TrapEffect[0];
        [Tooltip("Largura (tiles), centrada no transform")]
        [SerializeField] float width = 2f;
        [Tooltip("Fio: altura da base do fio acima do chão (tiles). Entre a altura agachado (0,875) e em pé (1,375)")]
        [SerializeField] float wireHeight = 1.05f;
        [SerializeField] float wireThickness = 0.1f;
        [Tooltip("Rearma depois deste tempo (s)")]
        [SerializeField, Min(0.1f)] float rearmTime = 3f;
        [Header("Barulho ao disparar")]
        [SerializeField] float noiseRadius = 8f;
        [SerializeField, Range(0f, 1f)] float noiseLoudness = 0.6f;
        [Header("Visual")]
        [Tooltip("Fio esticado ou placa solta")]
        [SerializeField] SpriteRenderer armedVisual;
        [Tooltip("Fio arrebentado ou placa afundada")]
        [SerializeField] SpriteRenderer firedVisual;

        float _rearmAt = -1f;
        int _playerMask;

        public Kind TriggerKind => kind;
        public bool IsArmed => _rearmAt < 0f;
        public float WireBottom => transform.position.y + wireHeight;

        public void Configure(Kind newKind, float newWidth, TrapEffect[] linked)
        {
            kind = newKind;
            width = newWidth;
            effects = linked;
        }

        /// <summary>Alcance do barulho ao disparar (alarme que deve chamar inimigos mais longe).</summary>
        public void SetNoise(float radius, float loudness)
        {
            noiseRadius = radius;
            noiseLoudness = loudness;
        }

        void Awake() => _playerMask = LayerMask.GetMask(GameConstants.Layers.Player);

        void OnEnable() => GameEvents.WorldReset += Rearm;
        void OnDisable() => GameEvents.WorldReset -= Rearm;

        void Rearm() => _rearmAt = -1f;

        void FixedUpdate()
        {
            if (!IsArmed)
            {
                if (Time.time >= _rearmAt) Rearm();
                return;
            }
            if (Touching()) Trigger();
        }

        bool Touching()
        {
            Vector2 p = transform.position;
            if (kind == Kind.Tripwire)
            {
                // Física de verdade: o colisor do gato (que encolhe agachado) cruza a faixa do fio?
                var center = new Vector2(p.x, p.y + wireHeight + wireThickness * 0.5f);
                return Physics2D.OverlapBox(center, new Vector2(width, wireThickness), 0f, _playerMask) != null;
            }

            var col = Physics2D.OverlapBox(new Vector2(p.x, p.y + 0.1f), new Vector2(width, 0.2f), 0f, _playerMask);
            if (col == null) return false;
            var player = col.GetComponentInParent<PlayerController>();
            return player != null && TrapRules.PlateTriggers(player.IsGrounded, player.IsCrouching);
        }

        /// <summary>Dispara (também usado pelos testes).</summary>
        public void Trigger()
        {
            if (!IsArmed) return;
            _rearmAt = Time.time + rearmTime;
            foreach (var e in effects) if (e != null) e.Fire();
            // Aviso visível: o jogador sempre sabe que disparou alguma coisa
            Combat.DamagePopup.Spawn((Vector2)transform.position + Vector2.up * 2.2f,
                kind == Kind.Tripwire ? "ALARME!" : "CLIC!", new Color32(0xff, 0xa8, 0x3a, 0xff));
            NoiseSystem.Emit((Vector2)transform.position + Vector2.up, noiseRadius, noiseLoudness, gameObject,
                kind == Kind.Tripwire ? "fio de alarme" : "placa de pressão");
        }

        void LateUpdate()
        {
            if (armedVisual != null) armedVisual.enabled = IsArmed;
            if (firedVisual != null) firedVisual.enabled = !IsArmed;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Vector3 p = transform.position;
            if (kind == Kind.Tripwire) Gizmos.DrawWireCube(p + Vector3.up * (wireHeight + wireThickness * 0.5f), new Vector3(width, wireThickness));
            else Gizmos.DrawWireCube(p + Vector3.up * 0.1f, new Vector3(width, 0.2f));
            foreach (var e in effects) if (e != null) Gizmos.DrawLine(p, e.transform.position);
        }
#endif
    }
}
