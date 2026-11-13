using System;
using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeteVidas.UI
{
    /// <summary>Com o que o jogador está jogando agora.</summary>
    public enum InputDeviceKind { KeyboardMouse, Gamepad }

    /// <summary>As ações que aparecem nas dicas, nos avisos e nos menus.</summary>
    public enum GameAction
    {
        Move, Up, Crouch, Jump, Attack, Special, Interact, Heal, Dash, NextClass, PrevClass,
        Inventory, Confirm, Back, TabLeft, TabRight, Navigate, Skip,
    }

    /// <summary>
    /// O último aparelho usado (pedido do usuário, 12/11/2026): ao mexer no controle, os avisos passam a mostrar os
    /// botões do controle de Xbox; ao tocar no teclado ou no mouse, as teclas. Começa no controle se houver um ligado.
    /// </summary>
    public static class InputDevices
    {
        public static InputDeviceKind Current { get; private set; } = InputDeviceKind.KeyboardMouse;
        /// <summary>Trocou de aparelho (os avisos se redesenham).</summary>
        public static event Action Changed;
        static bool s_started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()  // Play sem recarregar o domínio
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            Current = InputDeviceKind.KeyboardMouse;
            Changed = null;
            s_started = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Start()
        {
            if (s_started) return;
            s_started = true;
            Current = Gamepad.current != null ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;
            InputSystem.onDeviceChange += OnDeviceChange;
            var go = new GameObject("[Controle em uso]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<InputDeviceWatcher>();
        }

        static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad)) return;
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) Set(InputDeviceKind.Gamepad);
            else if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) && Gamepad.current == null)
                Set(InputDeviceKind.KeyboardMouse);
        }

        /// <summary>Muda o aparelho em uso (também usado pelos testes).</summary>
        public static void Set(InputDeviceKind kind)
        {
            if (Current == kind) return;
            Current = kind;
            Changed?.Invoke();
        }
    }

    /// <summary>Olha a cada quadro qual aparelho foi usado por último (botão, gatilho, analógico; tecla, clique, rodinha).</summary>
    public class InputDeviceWatcher : MonoBehaviour
    {
        void Update()
        {
            var pad = Gamepad.current;
            if (pad != null && UsedThisFrame(pad)) { InputDevices.Set(InputDeviceKind.Gamepad); return; }
            var k = Keyboard.current;
            var m = Mouse.current;
            if ((k != null && k.anyKey.wasPressedThisFrame) ||
                (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || Mathf.Abs(m.scroll.ReadValue().y) > 0.1f)))
                InputDevices.Set(InputDeviceKind.KeyboardMouse);
        }

        static bool UsedThisFrame(Gamepad p)
        {
            return p.buttonSouth.wasPressedThisFrame || p.buttonEast.wasPressedThisFrame || p.buttonWest.wasPressedThisFrame ||
                   p.buttonNorth.wasPressedThisFrame || p.leftShoulder.wasPressedThisFrame || p.rightShoulder.wasPressedThisFrame ||
                   p.leftTrigger.wasPressedThisFrame || p.rightTrigger.wasPressedThisFrame || p.startButton.wasPressedThisFrame ||
                   p.selectButton.wasPressedThisFrame || p.dpad.ReadValue().sqrMagnitude > 0.25f ||
                   p.leftStick.ReadValue().sqrMagnitude > 0.25f || p.rightStick.ReadValue().sqrMagnitude > 0.25f;
        }
    }

    /// <summary>
    /// Qual tecla ou botão faz cada ação, no aparelho em uso: o ícone em pixel art (<see cref="InputGlyphs"/>) e o nome
    /// em texto. Lê os controles do jogo (GameControls) quando o gato está na cena; nos menus, usa a mesma tabela padrão.
    /// </summary>
    public static class InputPrompts
    {
        // tabela padrão: a mesma do GameControls (teclado/mouse, controle). "|" junta duas teclas num ícone só.
        static readonly Dictionary<GameAction, string[]> s_defaults = new Dictionary<GameAction, string[]>
        {
            { GameAction.Move, new[] { "<Keyboard>/a|<Keyboard>/d", "<Gamepad>/leftStick" } },
            { GameAction.Up, new[] { "<Keyboard>/w", "<Gamepad>/leftStick/up" } },
            { GameAction.Crouch, new[] { "<Keyboard>/s", "<Gamepad>/leftStick/down" } },
            { GameAction.Jump, new[] { "<Keyboard>/space", "<Gamepad>/buttonSouth" } },
            { GameAction.Attack, new[] { "<Mouse>/leftButton", "<Gamepad>/buttonWest" } },
            { GameAction.Special, new[] { "<Mouse>/rightButton", "<Gamepad>/buttonNorth" } },
            { GameAction.Interact, new[] { "<Keyboard>/e", "<Gamepad>/buttonEast" } },
            { GameAction.Heal, new[] { "<Keyboard>/f", "<Gamepad>/leftTrigger" } },
            { GameAction.Dash, new[] { "<Keyboard>/leftShift", "<Gamepad>/rightTrigger" } },
            { GameAction.NextClass, new[] { "<Keyboard>/r", "<Gamepad>/rightShoulder" } },
            { GameAction.PrevClass, new[] { "<Keyboard>/q", "<Gamepad>/leftShoulder" } },
            { GameAction.Inventory, new[] { "<Keyboard>/tab", "<Gamepad>/start" } },
            { GameAction.Confirm, new[] { "<Keyboard>/enter", "<Gamepad>/buttonSouth" } },
            { GameAction.Back, new[] { "<Keyboard>/escape", "<Gamepad>/buttonEast" } },
            { GameAction.TabLeft, new[] { "<Keyboard>/q", "<Gamepad>/leftShoulder" } },
            { GameAction.TabRight, new[] { "<Keyboard>/e", "<Gamepad>/rightShoulder" } },
            { GameAction.Navigate, new[] { "<Keyboard>/arrows", "<Gamepad>/dpad" } },
            { GameAction.Skip, new[] { "<Keyboard>/escape", "<Gamepad>/start" } },
        };

        static readonly Dictionary<GameAction, string> s_actionNames = new Dictionary<GameAction, string>
        {
            { GameAction.Move, "Player/Move" }, { GameAction.Up, "Player/Move" }, { GameAction.Crouch, "Player/Crouch" },
            { GameAction.Jump, "Player/Jump" }, { GameAction.Attack, "Player/Attack" }, { GameAction.Special, "Player/Special" },
            { GameAction.Interact, "Player/Interact" }, { GameAction.Heal, "Player/Heal" }, { GameAction.Dash, "Player/Dash" },
            { GameAction.NextClass, "Player/NextClass" }, { GameAction.PrevClass, "Player/PrevClass" },
        };

        public static InputDeviceKind Current => InputDevices.Current;

        /// <summary>O caminho do controle (ex.: "&lt;Gamepad&gt;/buttonSouth") da ação no aparelho; "|" separa duas teclas.</summary>
        public static string PathFor(GameAction action, InputDeviceKind kind)
        {
            string fromAsset = FromAsset(action, kind);
            if (!string.IsNullOrEmpty(fromAsset)) return fromAsset;
            return s_defaults.TryGetValue(action, out var d) ? d[kind == InputDeviceKind.Gamepad ? 1 : 0] : "";
        }

        static string FromAsset(GameAction action, InputDeviceKind kind)
        {
            if (!s_actionNames.TryGetValue(action, out string name)) return null;
            var health = PlayerHealth.Active;
            var reader = health != null ? health.GetComponent<PlayerInputReader>() : null;
            var asset = reader != null ? reader.Actions : null;
            var a = asset != null ? asset.FindAction(name) : null;
            if (a == null) return null;
            string group = kind == InputDeviceKind.Gamepad ? "Gamepad" : "Keyboard&Mouse";
            string left = null, right = null;
            for (int i = 0; i < a.bindings.Count; i++)
            {
                var b = a.bindings[i];
                if (b.isComposite || string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group)) continue;
                string path = b.effectivePath;
                if (b.isPartOfComposite)
                {
                    // o andar no teclado é um composto (A/D); "subir" é a parte de cima dele
                    if (action == GameAction.Up && b.name == "up") return path;
                    if (action == GameAction.Move && b.name == "left" && left == null) left = path;
                    if (action == GameAction.Move && b.name == "right" && right == null) right = path;
                    if (left != null && right != null) return left + "|" + right;
                    continue;
                }
                if (action == GameAction.Up) { if (path.EndsWith("Stick")) return path + "/up"; continue; }
                return path;
            }
            return null;
        }

        /// <summary>O ícone em pixel art da ação no aparelho em uso.</summary>
        public static Sprite Glyph(GameAction action) => Glyph(action, Current);

        public static Sprite Glyph(GameAction action, InputDeviceKind kind) => InputGlyphs.ForPaths(PathFor(action, kind));

        /// <summary>O nome curto da tecla ou do botão, em texto ("Espaço", "E", "clique", "A", "LB"...).</summary>
        public static string Name(GameAction action) => Name(action, Current);

        public static string Name(GameAction action, InputDeviceKind kind)
        {
            var parts = PathFor(action, kind).Split('|');
            for (int i = 0; i < parts.Length; i++) parts[i] = ControlName(parts[i]);
            return string.Join("/", parts);
        }

        /// <summary>Nome em português de um controle ("&lt;Keyboard&gt;/space" → "Espaço", "&lt;Gamepad&gt;/buttonSouth" → "A").</summary>
        public static string ControlName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            int slash = path.IndexOf('/');
            string device = slash > 0 ? path.Substring(0, slash) : "";
            string control = slash > 0 ? path.Substring(slash + 1) : path;
            if (device == "<Gamepad>")
            {
                switch (control)
                {
                    case "buttonSouth": return "A";
                    case "buttonEast": return "B";
                    case "buttonWest": return "X";
                    case "buttonNorth": return "Y";
                    case "leftShoulder": return "LB";
                    case "rightShoulder": return "RB";
                    case "leftTrigger": return "LT";
                    case "rightTrigger": return "RT";
                    case "start": return "Menu";
                    case "select": return "Exibir";
                    case "leftStick": return "analógico";
                    case "rightStick": return "analógico dir.";
                    case "leftStick/up": case "dpad/up": return "↑";
                    case "leftStick/down": case "dpad/down": return "↓";
                    case "leftStick/left": case "dpad/left": return "←";
                    case "leftStick/right": case "dpad/right": return "→";
                    case "dpad": return "direcional";
                    default: return control;
                }
            }
            if (device == "<Mouse>")
                return control == "rightButton" ? "clique dir." : control == "middleButton" ? "rodinha" : "clique";
            return KeyLabel(control, false);
        }

        /// <summary>O texto de uma tecla. <paramref name="glyph"/>: a versão curta que cabe no desenho da tecla.</summary>
        public static string KeyLabel(string key, bool glyph)
        {
            switch (key)
            {
                case "space": return glyph ? "ESPAÇO" : "Espaço";
                case "leftShift": case "rightShift": case "shift": return glyph ? "SHIFT" : "Shift";
                case "tab": return glyph ? "TAB" : "Tab";
                case "escape": return glyph ? "ESC" : "Esc";
                case "enter": case "numpadEnter": return glyph ? "ENTER" : "Enter";
                case "leftCtrl": case "rightCtrl": return glyph ? "CTRL" : "Ctrl";
                case "upArrow": return "↑";
                case "downArrow": return "↓";
                case "leftArrow": return "←";
                case "rightArrow": return "→";
                case "arrows": return glyph ? "SETAS" : "Setas";
                default: return key.Length == 1 ? key.ToUpperInvariant() : key;
            }
        }

        /// <summary>
        /// Troca as marcas de tecla de um texto pelo nome no aparelho em uso: {pular}, {agachar}, {subir}, {atacar},
        /// {especial}, {interagir}, {curar}, {dash}, {inventario}, {trocar_classe} (diálogos e descrições).
        /// </summary>
        public static string Format(string text) => Format(text, Current);

        public static string Format(string text, InputDeviceKind kind)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            return text
                .Replace("{pular}", Name(GameAction.Jump, kind))
                .Replace("{agachar}", Name(GameAction.Crouch, kind))
                .Replace("{subir}", Name(GameAction.Up, kind))
                .Replace("{atacar}", Name(GameAction.Attack, kind))
                .Replace("{especial}", Name(GameAction.Special, kind))
                .Replace("{interagir}", Name(GameAction.Interact, kind))
                .Replace("{curar}", Name(GameAction.Heal, kind))
                .Replace("{dash}", Name(GameAction.Dash, kind))
                .Replace("{inventario}", Name(GameAction.Inventory, kind))
                .Replace("{trocar_classe}", kind == InputDeviceKind.Gamepad
                    ? Name(GameAction.PrevClass, kind) + "/" + Name(GameAction.NextClass, kind)
                    : "2 (ou " + Name(GameAction.PrevClass, kind) + "/" + Name(GameAction.NextClass, kind) + ")");
        }
    }
}
