using System.Collections.Generic;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Menu da viagem rápida (Docs/Design/floresta-mecanicas.md §5): os santuários visitados e ligados a este.
    /// Abre pelo botão de ataque no santuário; descansar tem o próprio botão (melhorias 14/11/2026).
    /// Pausa o jogo enquanto está aberto, no mesmo visual do inventário.
    /// </summary>
    public class TravelMenu : MonoBehaviour
    {
        const float W = 200f, RowH = 13f, Pad = 8f;
        static readonly Color Cream = new Color32(0xec, 0xdc, 0xb0, 0xff);
        static readonly Color Faint = new Color32(0x8a, 0x7a, 0x96, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xe0, 0x8a, 0xff);

        static TravelMenu s_instance;
        public static bool IsOpen => s_instance != null && s_instance._open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_instance = null; // Play sem recarregar o domínio

        Font _font;
        RectTransform _window;
        PromptRow _help;
        readonly List<Text> _rows = new List<Text>();
        readonly List<Texture2D> _textures = new List<Texture2D>();
        List<SanctuaryVisit> _destinations;
        Sanctuary _from;
        PlayerHealth _player;
        PlayerInputReader _input;
        int _cursor;
        bool _open;
        float _savedScale = 1f;
        int _openedFrame;

        /// <summary>Abre o menu no santuário <paramref name="from"/>: cada opção é um santuário para onde viajar.</summary>
        public static void Open(Sanctuary from, PlayerHealth player, List<SanctuaryVisit> destinations)
        {
            if (s_instance == null)
            {
                var go = new GameObject("Menu do santuário", typeof(RectTransform));
                s_instance = go.AddComponent<TravelMenu>();
                s_instance.Build();
            }
            s_instance.Show(from, player, destinations);
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 96;
            canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var dim = NewImage("Escuro", (RectTransform)transform, null);
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.6f);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;

            var frame = NewImage("Janela", (RectTransform)transform, PanelSprite());
            frame.type = Image.Type.Sliced;
            _window = frame.rectTransform;
            _window.anchorMin = _window.anchorMax = _window.pivot = new Vector2(0.5f, 0.5f);
            gameObject.SetActive(false);
        }

        void Show(Sanctuary from, PlayerHealth player, List<SanctuaryVisit> destinations)
        {
            _from = from;
            _player = player;
            _destinations = destinations ?? new List<SanctuaryVisit>();
            _cursor = 0;

            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();
            if (_help != null) Destroy(_help.gameObject);

            int count = Mathf.Max(1, _destinations.Count);
            float h = Pad * 2f + 14f + count * RowH + 14f;
            _window.sizeDelta = new Vector2(W, h);

            var title = NewText("Título", _window, 8, TextAnchor.UpperCenter, Gold);
            Place(title.rectTransform, 0f, -Pad, W, 12f);
            title.text = "Viagem rápida";
            _rows.Add(title);

            for (int i = 0; i < count; i++)
            {
                var t = NewText("Opção " + i, _window, 7, TextAnchor.MiddleLeft, Cream);
                Place(t.rectTransform, Pad + 6f, -(Pad + 14f + i * RowH), W - 2f * Pad - 6f, RowH);
                _rows.Add(t);
            }
            _help = PromptRow.Create(_window, "Ajuda", _font, 7, Faint, new Vector2(0f, -(h - 13f)), W);
            _help.Set(new PromptRow.Item(GameAction.Navigate, "escolher"), new PromptRow.Item(GameAction.Confirm, "ok"),
                      new PromptRow.Item(GameAction.Back, "voltar"));

            _savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (_input != null) _input.SetLock(this, true);
            _open = true;
            _openedFrame = Time.frameCount;
            gameObject.SetActive(true);
            SeteVidas.Audio.Sfx.Play("ui_inv_abre");
            Redraw();
        }

        void Redraw()
        {
            for (int i = 1; i < _rows.Count; i++)
            {
                int option = i - 1;
                bool sel = option == _cursor;
                string label = option < _destinations.Count ? NameOf(_destinations[option]) : "nenhum santuário";
                _rows[i].text = (sel ? "> " : "  ") + label;
                _rows[i].color = sel ? Gold : Cream;
            }
        }

        static string NameOf(SanctuaryVisit v) => string.IsNullOrEmpty(v.name) ? v.id : v.name;

        void Close()
        {
            _open = false;
            gameObject.SetActive(false);
            Time.timeScale = _savedScale;
            if (_input != null) _input.SetLock(this, false);
            _input = null;
        }

        void Update()
        {
            if (!_open || Time.frameCount == _openedFrame) return; // o mesmo E que abriu não confirma
            var k = Keyboard.current; var p = Gamepad.current;
            int dy = 0;
            bool ok = false, back = false;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
                ok |= k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
                back |= k.escapeKey.wasPressedThisFrame;
            }
            if (p != null)
            {
                if (p.dpad.up.wasPressedThisFrame || p.leftStick.up.wasPressedThisFrame) dy = -1;
                if (p.dpad.down.wasPressedThisFrame || p.leftStick.down.wasPressedThisFrame) dy = 1;
                ok |= p.buttonSouth.wasPressedThisFrame;
                back |= p.buttonEast.wasPressedThisFrame;
            }

            int count = _destinations.Count;
            if (dy != 0 && count > 0)
            {
                _cursor = (_cursor + dy + count) % count;
                SeteVidas.Audio.Sfx.Play("ui_cursor_inv");
                Redraw();
            }
            if (back) { SeteVidas.Audio.Sfx.Play("ui_inv_fecha"); Close(); return; }
            if (!ok) return;

            var player = _player;
            int choice = _cursor;
            Close();
            if (choice < 0 || choice >= _destinations.Count) return;
            var dest = _destinations[choice];
            SeteVidas.Audio.Sfx.Play("santuario_descansa");
            SceneTravel.ToSanctuary(dest.scene, dest.id, player);
        }

        void OnDestroy()
        {
            if (_open) Close();
            if (s_instance == this) s_instance = null;
            foreach (var t in _textures) if (t != null) Destroy(t);
        }

        // ------------------------------------------------------------ montagem

        Sprite PanelSprite()
        {
            var grid = InventoryIcons.Panel(PanelLook.Main);
            int h = grid.GetLength(0), w = grid.GetLength(1);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    string hex = grid[y, x];
                    px[(h - 1 - y) * w + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ui_viagem_painel" };
            tex.SetPixels32(px);
            tex.Apply(false);
            _textures.Add(tex);
            float b = InventoryIcons.PanelBorder;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Image NewImage(string name, RectTransform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, RectTransform parent, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
