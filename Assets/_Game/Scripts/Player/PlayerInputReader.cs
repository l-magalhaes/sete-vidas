using UnityEngine;
using UnityEngine.InputSystem;

namespace SeteVidas.Player
{
    /// <summary>
    /// Lê o asset GameControls (mapa "Player") e expõe os valores que o controlador usa.
    /// Outras ações (Attack, Interact, troca de classe…) entram aqui nas próximas fases.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        const float StickDeadZone = 0.2f;

        [SerializeField] InputActionAsset actions;

        InputActionMap _map;
        InputAction _move, _jump, _crouch;

        /// <summary>Direção horizontal: -1, 0 ou 1 no teclado; contínua no analógico.</summary>
        public float MoveX
        {
            get
            {
                if (_move == null) return 0f;
                float x = _move.ReadValue<Vector2>().x;
                return Mathf.Abs(x) < StickDeadZone ? 0f : x;
            }
        }

        public bool JumpPressedThisFrame => _jump != null && _jump.WasPressedThisFrame();
        public bool JumpHeld => _jump != null && _jump.IsPressed();
        public bool CrouchHeld => _crouch != null && _crouch.IsPressed();
        public bool CrouchPressedThisFrame => _crouch != null && _crouch.WasPressedThisFrame();

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("[Sete Vidas] PlayerInputReader sem o asset GameControls.", this);
                enabled = false;
                return;
            }
            _map = actions.FindActionMap("Player", throwIfNotFound: true);
            _move = _map.FindAction("Move", throwIfNotFound: true);
            _jump = _map.FindAction("Jump", throwIfNotFound: true);
            _crouch = _map.FindAction("Crouch", throwIfNotFound: true);
        }

        void OnEnable() => _map?.Enable();
        void OnDisable() => _map?.Disable();
    }
}
