using UnityEngine;
using UnityEngine.InputSystem;

namespace SeteVidas.Player
{
    /// <summary>
    /// Lê o asset GameControls (mapa "Player") e expõe os valores que o controlador usa.
    /// Inclui a troca de classe (R/Q e 1–7).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        const float StickDeadZone = 0.2f;

        [SerializeField] InputActionAsset actions;

        InputActionMap _map;
        InputAction _move, _jump, _crouch, _attack, _interact, _nextClass, _prevClass;
        readonly InputAction[] _classSlots = new InputAction[7];

        /// <summary>
        /// Diálogo aberto (ou cutscene): todas as leituras de jogo devolvem "nada apertado".
        /// Só <see cref="AdvanceDialoguePressedThisFrame"/> continua funcionando.
        /// </summary>
        public bool GameplayLocked { get; set; }

        /// <summary>E, Espaço ou ataque: avança o diálogo. Ignora o bloqueio.</summary>
        public bool AdvanceDialoguePressedThisFrame =>
            (_interact != null && _interact.WasPressedThisFrame()) ||
            (_jump != null && _jump.WasPressedThisFrame()) ||
            (_attack != null && _attack.WasPressedThisFrame());

        /// <summary>Direção horizontal: -1, 0 ou 1 no teclado; contínua no analógico.</summary>
        public float MoveX
        {
            get
            {
                if (_move == null || GameplayLocked) return 0f;
                float x = _move.ReadValue<Vector2>().x;
                return Mathf.Abs(x) < StickDeadZone ? 0f : x;
            }
        }

        /// <summary>Direção vertical (W/S, setas, analógico): sobe e desce na escalada.</summary>
        public float MoveY
        {
            get
            {
                if (_move == null || GameplayLocked) return 0f;
                float y = _move.ReadValue<Vector2>().y;
                return Mathf.Abs(y) < StickDeadZone ? 0f : y;
            }
        }

        public bool JumpPressedThisFrame => !GameplayLocked && _jump != null && _jump.WasPressedThisFrame();
        public bool JumpHeld => !GameplayLocked && _jump != null && _jump.IsPressed();
        public bool CrouchHeld => !GameplayLocked && _crouch != null && _crouch.IsPressed();
        public bool CrouchPressedThisFrame => !GameplayLocked && _crouch != null && _crouch.WasPressedThisFrame();
        public bool AttackPressedThisFrame => !GameplayLocked && _attack != null && _attack.WasPressedThisFrame();
        public bool InteractPressedThisFrame => !GameplayLocked && _interact != null && _interact.WasPressedThisFrame();
        public bool NextClassPressedThisFrame => !GameplayLocked && _nextClass != null && _nextClass.WasPressedThisFrame();
        public bool PrevClassPressedThisFrame => !GameplayLocked && _prevClass != null && _prevClass.WasPressedThisFrame();

        /// <summary>Tecla de classe apertada neste frame: 1 a 7, ou 0 se nenhuma.</summary>
        public int ClassSlotPressedThisFrame
        {
            get
            {
                if (GameplayLocked) return 0;
                for (int i = 0; i < _classSlots.Length; i++)
                    if (_classSlots[i] != null && _classSlots[i].WasPressedThisFrame()) return i + 1;
                return 0;
            }
        }

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
            _attack = _map.FindAction("Attack", throwIfNotFound: true);
            _interact = _map.FindAction("Interact", throwIfNotFound: true);
            _nextClass = _map.FindAction("NextClass", throwIfNotFound: false);
            _prevClass = _map.FindAction("PrevClass", throwIfNotFound: false);
            for (int i = 0; i < _classSlots.Length; i++)
                _classSlots[i] = _map.FindAction($"SelectClass{i + 1}", throwIfNotFound: false);
        }

        void OnEnable() => _map?.Enable();
        void OnDisable() => _map?.Disable();
    }
}
