using System.Collections.Generic;
using SeteVidas.Abilities;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Melhoria 9: o tutorial em pergaminho. Uma dica por vez desliza de baixo, no canto inferior esquerdo, com a tecla
    /// (desenhada como uma teclinha, com o texto do controle atual: teclado ou gamepad) e uma frase curta. Some ao fazer
    /// a ação, ao sair da área ou depois de 6 s; some durante os diálogos e volta depois. Cada dica aparece uma vez por
    /// save (flag dica_&lt;id&gt;). Criado em código na primeira dica (não precisa de prefab).
    /// </summary>
    public class TutorialScroll : MonoBehaviour
    {
        const float SlideTime = 0.2f, Width = 168f, Height = 30f, Margin = 8f;
        public const string FlagPrefix = "dica_";
        /// <summary>Flag do save: o jogador desligou as dicas (menu de pausa, melhoria 10).</summary>
        public const string DisabledFlag = "dicas_desligadas";

        const string DisabledPref = "SeteVidas.DicasDesligadas";

        /// <summary>
        /// O jogador desligou as dicas (tela inicial ou inventário). Fica nas preferências do jogador, não no save: um jogo
        /// novo apaga o save, mas não deve religar as dicas.
        /// </summary>
        public static bool HintsDisabled
        {
            get => PlayerPrefs.GetInt(DisabledPref, 0) == 1;
            set { PlayerPrefs.SetInt(DisabledPref, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static TutorialScroll s_instance;
        static Sprite s_paper, s_key;

        readonly TutorialQueue _queue = new TutorialQueue();
        readonly Dictionary<string, TutorialHint> _hints = new Dictionary<string, TutorialHint>();
        RectTransform _panel, _keyBox;
        Image _keyImage;
        Text _keyText, _phrase;
        float _slide;           // 0 escondido, 1 na tela
        bool _gamepad;

        public static bool Exists => s_instance != null;
        /// <summary>A dica na tela agora (testes).</summary>
        public string CurrentId => _queue.Current;
        /// <summary>O pergaminho está aparecendo (testes).</summary>
        public bool IsShowing => _queue.Current != null && _slide > 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_instance = null; s_paper = null; s_key = null; } // Play sem recarregar o domínio

        public static TutorialScroll Get()
        {
            if (s_instance != null) return s_instance;
            var go = new GameObject("Dicas (pergaminho)", typeof(RectTransform));
            s_instance = go.AddComponent<TutorialScroll>();
            s_instance.Build();
            return s_instance;
        }

        static bool Seen(string id) => HintsDisabled || (SaveManager.Data != null &&
            (SaveManager.Data.HasFlag(FlagPrefix + id) || SaveManager.Data.HasFlag(DisabledFlag)));

        public void Enter(TutorialHint hint)
        {
            if (hint == null) return;
            if (!string.IsNullOrEmpty(hint.RequiresFlag) && (SaveManager.Data == null || !SaveManager.Data.HasFlag(hint.RequiresFlag))) return;
            _hints[hint.Id] = hint;
            _queue.Enter(hint.Id, Seen(hint.Id));
        }

        public void Exit(TutorialHint hint)
        {
            if (hint == null) return;
            // "ler" (sem ação): sair da área depois de ter visto conta como lida
            if (hint.Id == _queue.Current && hint.Dismiss == HintDismiss.Read && _slide > 0.9f) MarkSeen(hint.Id);
            _queue.Exit(hint.Id);
        }

        static void MarkSeen(string id)
        {
            if (SaveManager.Data != null && !string.IsNullOrEmpty(id)) SaveManager.Data.SetFlag(FlagPrefix + id);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TrackDevice();
            bool dialogue = DialogueRunner.Active != null && DialogueRunner.Active.IsOpen;
            TutorialHint hint = null;
            if (_queue.Current != null) _hints.TryGetValue(_queue.Current, out hint);

            if (hint != null && !dialogue)
            {
                if (DidAction(hint))
                {
                    MarkSeen(_queue.Done());
                    hint = null;
                }
                else if (_slide >= 1f && _queue.Tick(dt))
                {
                    if (hint.Dismiss == HintDismiss.Read) MarkSeen(hint.Id); // ficou o tempo todo na tela: lida
                    _queue.Done();
                    hint = null;
                }
            }

            bool show = hint != null && !dialogue;
            if (show) Fill(hint);
            _slide = Mathf.MoveTowards(_slide, show ? 1f : 0f, dt / SlideTime);
            float e = _slide * _slide * (3f - 2f * _slide);      // suave
            _panel.anchoredPosition = new Vector2(Margin, Mathf.Lerp(-Height - 4f, Margin, e));
            _panel.gameObject.SetActive(_slide > 0f);
        }

        bool DidAction(TutorialHint hint)
        {
            var health = PlayerHealth.Active;
            if (health == null) return false;
            var input = health.GetComponent<PlayerInputReader>();
            var player = health.GetComponent<PlayerController>();
            switch (hint.Dismiss)
            {
                case HintDismiss.Cover:
                    var stealth = health.GetComponent<StealthController>();
                    return stealth != null && stealth.InCover;
                case HintDismiss.Read:
                    return false;
            }
            switch (hint.Key)
            {
                case HintKey.Jump: return input != null && input.JumpPressedThisFrame;
                case HintKey.Crouch: return player != null && player.IsCrouching;
                case HintKey.Attack: return input != null && input.AttackPressedThisFrame;
                case HintKey.Up: return player != null && player.ActiveAbility is WallClimbAbility;
                case HintKey.Move: return input != null && Mathf.Abs(input.MoveX) > 0.5f;
                case HintKey.Interact: return input != null && input.InteractPressedThisFrame;
                case HintKey.NextClass: return input != null && (input.NextClassPressedThisFrame || input.PrevClassPressedThisFrame || input.ClassSlotPressedThisFrame > 0);
                case HintKey.Heal: return input != null && input.HealPressedThisFrame;
                default: return false;
            }
        }

        // ------------------------------------------------------------ tecla

        // o aparelho em uso vem do InputDevices (o mesmo dos avisos, do inventário e dos menus)
        void TrackDevice() => _gamepad = InputDevices.Current == InputDeviceKind.Gamepad;

        /// <summary>O ícone em pixel art da tecla ou do botão da dica no aparelho em uso.</summary>
        static Sprite KeyGlyph(HintKey key)
        {
            switch (key)
            {
                case HintKey.None: return null;
                case HintKey.Jump: return InputPrompts.Glyph(GameAction.Jump);
                case HintKey.Crouch: return InputPrompts.Glyph(GameAction.Crouch);
                case HintKey.Attack: return InputPrompts.Glyph(GameAction.Attack);
                case HintKey.Up: return InputPrompts.Glyph(GameAction.Up);
                case HintKey.Interact: return InputPrompts.Glyph(GameAction.Interact);
                case HintKey.Heal: return InputPrompts.Glyph(GameAction.Heal);
                case HintKey.NextClass:
                    var kind = InputDevices.Current;
                    return InputGlyphs.ForPaths(InputPrompts.PathFor(GameAction.PrevClass, kind) + "|" + InputPrompts.PathFor(GameAction.NextClass, kind));
                default: return InputPrompts.Glyph(GameAction.Move);
            }
        }

        /// <summary>O texto da tecla de <paramref name="key"/> no controle atual ("Espaço", "S", "clique", "A"...).</summary>
        string KeyLabel(HintKey key)
        {
            var health = PlayerHealth.Active;
            var reader = health != null ? health.GetComponent<PlayerInputReader>() : null;
            var asset = reader != null ? reader.Actions : null;
            if (asset == null || key == HintKey.None) return "";
            string group = _gamepad ? "Gamepad" : "Keyboard&Mouse";
            InputAction action = key switch
            {
                HintKey.Jump => asset.FindAction("Player/Jump"),
                HintKey.Crouch => asset.FindAction("Player/Crouch"),
                HintKey.Attack => asset.FindAction("Player/Attack"),
                HintKey.Interact => asset.FindAction("Player/Interact"),
                HintKey.NextClass => asset.FindAction("Player/NextClass"),
                HintKey.Heal => asset.FindAction("Player/Heal"),
                _ => asset.FindAction("Player/Move"),
            };
            if (action == null) return "";

            // andar: as duas teclas de lado do primeiro conjunto (A/D); no controle, o analógico
            if (key == HintKey.Move)
            {
                string left = "", right = "";
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group)) continue;
                    if (!b.isPartOfComposite) return Translate(action.GetBindingDisplayString(i));
                    if (b.name == "left" && left == "") left = Translate(action.GetBindingDisplayString(i));
                    if (b.name == "right" && right == "") right = Translate(action.GetBindingDisplayString(i));
                    if (left != "" && right != "") return left + "/" + right;
                }
                return left + (right != "" ? "/" + right : "");
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite) continue;
                if (key == HintKey.Up && b.isPartOfComposite && b.name != "up") continue;
                if (string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group)) continue;
                return Translate(action.GetBindingDisplayString(i));
            }
            return "";
        }

        /// <summary>Nomes das teclas em português (o Input System devolve em inglês).</summary>
        public static string Translate(string label)
        {
            switch (label)
            {
                case "Space": return "Espaço";
                case "LMB": case "Left Button": return "clique";
                case "RMB": case "Right Button": return "clique dir.";
                case "Up Arrow": case "Up": return "↑";
                case "Down Arrow": case "Down": return "↓";
                case "Left Stick Up": case "Left Stick/Up": return "↑";
                case "LS": case "Left Stick": return "analógico";
                case "Left Arrow": return "←";
                case "Right Arrow": return "→";
                default: return label;
            }
        }

        // ------------------------------------------------------------ desenho

        void Fill(TutorialHint hint)
        {
            _phrase.text = hint.Phrase;
            // o botão ou a tecla em pixel art (controle de Xbox ou teclado e mouse, o que estiver em uso)
            var glyph = KeyGlyph(hint.Key);
            _keyBox.gameObject.SetActive(glyph != null);
            float keyW = 0f;
            if (glyph != null)
            {
                _keyImage.sprite = glyph;
                _keyImage.type = Image.Type.Simple;
                _keyText.text = "";
                keyW = glyph.rect.width;
                _keyBox.sizeDelta = new Vector2(keyW, glyph.rect.height);
            }
            _phrase.rectTransform.anchoredPosition = new Vector2(10f + (keyW > 0f ? keyW + 5f : 0f), 0f);
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = new GameObject("Pergaminho", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panel.transform;
            _panel.SetParent(transform, false);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = Vector2.zero;
            _panel.sizeDelta = new Vector2(Width, Height);
            var img = panel.GetComponent<Image>();
            img.sprite = PaperSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var keyGo = new GameObject("Tecla", typeof(RectTransform), typeof(Image));
            _keyBox = (RectTransform)keyGo.transform;
            _keyBox.SetParent(_panel, false);
            _keyBox.anchorMin = _keyBox.anchorMax = _keyBox.pivot = new Vector2(0f, 0.5f);
            _keyBox.anchoredPosition = new Vector2(10f, 0f);
            _keyImage = keyGo.GetComponent<Image>();
            _keyImage.sprite = KeySprite();
            _keyImage.type = Image.Type.Sliced;
            _keyImage.raycastTarget = false;
            _keyText = NewText("Texto da tecla", _keyBox, font, new Color32(0x24, 0x1c, 0x14, 0xff), TextAnchor.MiddleCenter);
            var kt = _keyText.rectTransform;
            kt.anchorMin = Vector2.zero; kt.anchorMax = Vector2.one; kt.offsetMin = kt.offsetMax = Vector2.zero;

            _phrase = NewText("Frase", _panel, font, new Color32(0x3a, 0x2a, 0x18, 0xff), TextAnchor.MiddleLeft);
            var pt = _phrase.rectTransform;
            pt.anchorMin = pt.anchorMax = pt.pivot = new Vector2(0f, 0.5f);
            pt.sizeDelta = new Vector2(Width - 20f, Height);
            _panel.gameObject.SetActive(false);
        }

        static Text NewText(string name, RectTransform parent, Font font, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = 8;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Pergaminho 24×16 fatiado (9-slice): papel claro, rolos nas pontas, contorno marrom.</summary>
        public static Sprite PaperSprite()
        {
            if (s_paper != null) return s_paper;
            const int w = 24, h = 16;
            var px = new Color32[w * h];
            var edge = new Color32(0x3a, 0x2a, 0x18, 0xff);
            var paper = new Color32(0xec, 0xdc, 0xb0, 0xff);
            var shade = new Color32(0xd4, 0xbe, 0x8a, 0xff);
            var roll = new Color32(0xc8, 0xa4, 0x68, 0xff);
            var rollDark = new Color32(0x8a, 0x6a, 0x3a, 0xff);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    bool rollZone = x < 4 || x >= w - 4;
                    if (y == 0 || y == h - 1 || x == 0 || x == w - 1) c = edge;
                    else if (rollZone) c = (x == 3 || x == w - 4) ? edge : (y % 3 == 0 ? rollDark : roll);
                    else c = y <= 2 ? shade : paper;
                    if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1)) c = new Color32(0, 0, 0, 0); // cantos redondos
                    px[y * w + x] = c;
                }
            s_paper = MakeSliced(px, w, h, new Vector4(5f, 4f, 5f, 4f), "ui_dica_pergaminho");
            return s_paper;
        }

        /// <summary>Teclinha 9×9 fatiada: tampa clara, borda escura e sombra embaixo.</summary>
        public static Sprite KeySprite()
        {
            if (s_key != null) return s_key;
            const int n = 9;
            var px = new Color32[n * n];
            var edge = new Color32(0x24, 0x1c, 0x14, 0xff);
            var cap = new Color32(0xf8, 0xf0, 0xd8, 0xff);
            var side = new Color32(0xb8, 0xa0, 0x74, 0xff);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color32 c = (x == 0 || x == n - 1 || y == 0 || y == n - 1) ? edge : y <= 2 ? side : cap;
                    if ((x == 0 || x == n - 1) && (y == 0 || y == n - 1)) c = new Color32(0, 0, 0, 0);
                    px[y * n + x] = c;
                }
            s_key = MakeSliced(px, n, n, new Vector4(3f, 3f, 3f, 3f), "ui_dica_tecla");
            return s_key;
        }

        static Sprite MakeSliced(Color32[] px, int w, int h, Vector4 border, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false);
            // 100 px por unidade = o referencePixelsPerUnit do Canvas: 1 pixel do desenho = 1 pixel do jogo (480×270)
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = name;
            return s;
        }

        void OnDestroy() { if (s_instance == this) s_instance = null; }
    }
}
