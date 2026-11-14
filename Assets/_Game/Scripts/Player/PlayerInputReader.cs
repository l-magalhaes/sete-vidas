using UnityEngine;
using UnityEngine.InputSystem;

namespace SeteVidas.Player
{
    /// <summary>
    /// Lê o asset GameControls (mapa "Player") e expõe os valores que o controlador usa.
    /// Inclui a troca de classe (R/Q e 1–7) e a magia de cura (F).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        const float StickDeadZone = 0.2f;

        [SerializeField] InputActionAsset actions;

        /// <summary>O asset de controles (as dicas leem o nome das teclas dele).</summary>
        public InputActionAsset Actions => actions;

        InputActionMap _map;
        InputAction _move, _jump, _crouch, _attack, _interact, _nextClass, _prevClass, _heal;
        readonly InputAction[] _classSlots = new InputAction[7];

        /// <summary>
        /// Diálogo aberto (ou cutscene): todas as leituras de jogo devolvem "nada apertado".
        /// Só <see cref="AdvanceDialoguePressedThisFrame"/> continua funcionando.
        /// </summary>
        public bool GameplayLocked => _locks.Count > 0;
        readonly System.Collections.Generic.HashSet<object> _locks = new System.Collections.Generic.HashSet<object>();

        /// <summary>Trava ou destrava o controle em nome de <paramref name="owner"/> (diálogo, cena). Só destrava quando ninguém mais trava.</summary>
        public void SetLock(object owner, bool locked)
        {
            if (owner == null) return;
            if (locked) _locks.Add(owner);
            else _locks.Remove(owner);
        }

        /// <summary>E, Espaço ou ataque: avança o diálogo. Ignora o bloqueio.</summary>
        public bool AdvanceDialoguePressedThisFrame =>
            (_interact != null && _interact.WasPressedThisFrame()) ||
            (_jump != null && _jump.WasPressedThisFrame()) ||
            (_attack != null && _attack.WasPressedThisFrame());

        bool _scripted;
        float _scriptedX;

        /// <summary>
        /// Andar roteirizado (entrar e sair de cena, melhorias 14/11/2026): enquanto ligado, o gato anda para
        /// <paramref name="x"/> (−1 a 1) mesmo com o controle travado. O resto do controle continua travado.
        /// </summary>
        public void SetScriptedMove(float x)
        {
            _scripted = true;
            _scriptedX = Mathf.Clamp(x, -1f, 1f);
        }

        public void ClearScriptedMove() { _scripted = false; _scriptedX = 0f; }

        /// <summary>Está andando sozinho (testes).</summary>
        public bool IsScriptedMove => _scripted;

        /// <summary>Direção horizontal: -1, 0 ou 1 no teclado; contínua no analógico.</summary>
        public float MoveX
        {
            get
            {
                if (_scripted) return _scriptedX;
                if (_move == null || GameplayLocked) return 0f;
                float x = _move.ReadValue<Vector2>().x;
                return Mathf.Abs(x) < StickDeadZone ? 0f : x;
            }
        }

        readonly FacingFilter _facing = new FacingFilter();
        int _facingFrame = -1, _facingIntent;

        /// <summary>O direcional agora é um analógico (não teclado nem D-pad).</summary>
        public bool IsAnalog => _move?.activeControl is UnityEngine.InputSystem.Controls.StickControl;

        /// <summary>
        /// Para que lado o jogador quer olhar neste quadro (−1, 1 ou 0 = sem pedido). Filtrado no analógico contra o
        /// retorno da mola e a diagonal de agachar/escalar (<see cref="FacingFilter"/>). Use isto para virar o gato;
        /// use <see cref="MoveX"/> para a velocidade.
        /// </summary>
        public int FacingIntent
        {
            get
            {
                if (_scripted) return _scriptedX > 0.01f ? 1 : _scriptedX < -0.01f ? -1 : 0;
                if (_facingFrame == Time.frameCount) return _facingIntent;
                _facingFrame = Time.frameCount;
                Vector2 v = _move == null || GameplayLocked ? Vector2.zero : _move.ReadValue<Vector2>();
                _facingIntent = _facing.Step(v, IsAnalog, Time.unscaledDeltaTime);
                return _facingIntent;
            }
        }

        /// <summary>
        /// Lado pedido agora sem esperar o filtro: teclado na hora; analógico só com empurrão firme e horizontal.
        /// Usado no aperto do ataque, para o golpe sair para onde o jogador acabou de virar.
        /// </summary>
        public int FirmDirection
        {
            get
            {
                if (_move == null || GameplayLocked) return 0;
                Vector2 v = _move.ReadValue<Vector2>();
                float ax = Mathf.Abs(v.x);
                if (!IsAnalog) return v.x > 0.01f ? 1 : v.x < -0.01f ? -1 : 0;
                return ax >= FacingFilter.StrongX && ax >= Mathf.Abs(v.y) ? (v.x > 0f ? 1 : -1) : 0;
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
        /// <summary>Ataque segurado (golpe carregado da marreta).</summary>
        public bool AttackHeld => !GameplayLocked && _attack != null && _attack.IsPressed();
        public bool InteractPressedThisFrame => !GameplayLocked && _interact != null && _interact.WasPressedThisFrame();
        public bool NextClassPressedThisFrame => !GameplayLocked && _nextClass != null && _nextClass.WasPressedThisFrame();
        public bool PrevClassPressedThisFrame => !GameplayLocked && _prevClass != null && _prevClass.WasPressedThisFrame();
        /// <summary>F (ou gatilho esquerdo): magia de cura do Paladino.</summary>
        public bool HealPressedThisFrame => !GameplayLocked && _heal != null && _heal.WasPressedThisFrame();

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
            _heal = _map.FindAction("Heal", throwIfNotFound: false);
            for (int i = 0; i < _classSlots.Length; i++)
                _classSlots[i] = _map.FindAction($"SelectClass{i + 1}", throwIfNotFound: false);
        }

        void OnEnable() => _map?.Enable();
        void OnDisable() => _map?.Disable();
    }
}
