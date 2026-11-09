using SeteVidas.Core;
using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Guarda o último lugar seguro onde o gato pisou: chão firme (não plataforma que desmorona), sem subir na
    /// vertical e longe de armadilhas. Os espinhos devolvem o gato para cá.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class SafeGroundTracker : MonoBehaviour
    {
        [Tooltip("Distância mínima de qualquer armadilha (tiles)")]
        [SerializeField] float dangerMargin = 1.2f;
        [Tooltip("Tempo pisando firme antes de contar como seguro (s)")]
        [SerializeField] float stableTime = 0.1f;

        PlayerController _player;
        float _stable;
        int _groundMask;

        public Vector2 LastSafe { get; private set; }

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _groundMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
        }

        void Start() => LastSafe = _player.Body.position;

        /// <summary>Esquece o ponto antigo (teleporte, respawn) e usa este.</summary>
        public void ResetTo(Vector2 position)
        {
            LastSafe = position;
            _stable = 0f;
        }

        void FixedUpdate()
        {
            Vector2 feet = _player.Body.position;
            // No chão, a velocidade para baixo é a gravidade do passo (a colisão zera depois): só não pode estar subindo
            bool safe = _player.IsGrounded && _player.Velocity.y <= 0.01f &&
                        !_player.IsHanging && !_player.IsClimbingLedge && _player.ActiveAbility == null &&
                        OnFirmGround(feet) && !TrapRules.IsNearDanger(feet + Vector2.up * 0.5f, dangerMargin);

            if (!safe) { _stable = 0f; return; }
            _stable += Time.fixedDeltaTime;
            if (_stable >= stableTime) LastSafe = feet;
        }

        bool OnFirmGround(Vector2 feet)
        {
            float w = _player.Settings.ColliderWidth * 0.9f;
            var col = Physics2D.OverlapBox(feet + Vector2.down * 0.05f, new Vector2(w, 0.1f), 0f, _groundMask);
            return col != null && col.GetComponent<CrumblingPlatform>() == null;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(LastSafe + Vector2.up * 0.2f, 0.2f);
        }
#endif
    }
}
