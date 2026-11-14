using SeteVidas.Classes;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Roda de troca rápida de classe (pedido do usuário, 14/11/2026; referência: roda de armas). Segurar a troca de
    /// classe (R / RB, ou Q / LB) abre a roda com o tempo em câmera lenta; o direcional, o analógico ou o mouse
    /// apontam a fatia; soltar o botão troca. As 7 fatias são as classes na ordem (Ladino em cima, sentido horário);
    /// as que faltam aparecem só como fatia escura, sem ícone nem nome. No meio: o nome da classe apontada e "n / total".
    /// Montada na hora (sem prefab), uma por cena.
    /// </summary>
    public class ClassWheel : MonoBehaviour
    {
        const int Count = ClassRules.ClassCount;
        const float Size = 190f;              // diâmetro na tela (referência 480 × 270)
        const float GapDegrees = 3f;

        static ClassWheel s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_instance = null; s_ring = null; s_disc = null; } // Play sem recarregar o domínio

        public static bool IsOpen => s_instance != null && s_instance._open;

        static Sprite s_ring, s_disc;

        ClassManager _classes;
        PlayerInputReader _input;
        int _button;          // 1 = R/RB, -1 = Q/LB (o botão que abriu; soltar troca)
        bool _open;
        int _hover = -1;
        float _savedScale = 1f;
        Vector2 _mouseAtOpen;
        bool _mouseMoved;

        CanvasGroup _group;
        readonly Image[] _segments = new Image[Count];
        readonly Image[] _icons = new Image[Count];
        readonly Text[] _labels = new Text[Count];
        Text _title, _counter;
        Font _font;

        static readonly Color SegmentColor = new Color(0.30f, 0.31f, 0.38f, 0.92f);
        static readonly Color LockedColor = new Color(0.14f, 0.14f, 0.18f, 0.88f);
        static readonly Color CurrentEdge = new Color(1f, 0.88f, 0.54f, 1f);

        /// <summary>Abre a roda (chamado pelo <see cref="ClassManager"/> quando a troca é segurada).</summary>
        public static void Open(ClassManager classes, PlayerInputReader input, int button)
        {
            if (classes == null || input == null || Time.timeScale <= 0f) return; // pausado (diálogo, inventário)
            if (s_instance == null)
            {
                var go = new GameObject("Roda de classes");
                s_instance = go.AddComponent<ClassWheel>();
                s_instance.Build();
            }
            s_instance.Show(classes, input, button);
        }

        void Show(ClassManager classes, PlayerInputReader input, int button)
        {
            _classes = classes;
            _input = input;
            _button = button;
            _open = true;
            _hover = (int)classes.CurrentClass;
            _savedScale = Time.timeScale;
            Time.timeScale = ClassRules.WheelTimeScale;
            _input.SetLock(this, true);
            _mouseAtOpen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            _mouseMoved = false;
            _group.alpha = 1f;
            SeteVidas.Audio.Sfx.Play("ui_inv_abre", null, 0.7f, 1.2f);
            Refresh();
        }

        void Close(bool confirm)
        {
            if (!_open) return;
            _open = false;
            _group.alpha = 0f;
            if (Time.timeScale > 0f) Time.timeScale = _savedScale > 0f ? _savedScale : 1f;
            if (_input != null) _input.SetLock(this, false);
            if (!confirm || _classes == null || _hover < 0) return;
            var c = (CharacterClass)_hover;
            if (c == _classes.CurrentClass) return;
            if (_classes.IsAvailable(c) && _classes.TrySwitch(c)) SeteVidas.Audio.Sfx.Play("ui_confirma");
            else SeteVidas.Audio.Sfx.Play("ui_negado");
        }

        void OnDisable()
        {
            if (_open) Close(false);
        }

        void Update()
        {
            if (!_open) return;
            if (_input == null || _classes == null || !_input.isActiveAndEnabled) { Close(false); return; }
            var health = PlayerHealth.Active;
            if (health != null && health.IsDead) { Close(false); return; }

            // mira: analógico/direcional; o mouse vale depois de mexido
            Vector2 dir = _input.RawMove;
            if (Mouse.current != null)
            {
                Vector2 m = Mouse.current.position.ReadValue();
                if (!_mouseMoved && (m - _mouseAtOpen).sqrMagnitude > 64f) _mouseMoved = true;
                if (_mouseMoved && dir.sqrMagnitude < 0.04f) dir = (m - new Vector2(Screen.width, Screen.height) * 0.5f) / (Screen.height * 0.1f);
            }
            int slot = ClassRules.WheelSlotAt(dir, Count, 0.35f);
            if (slot >= 0 && slot != _hover)
            {
                _hover = slot;
                SeteVidas.Audio.Sfx.Play("ui_cursor", null, 0.6f);
                Refresh();
            }

            bool held = _button > 0 ? _input.NextClassHeldRaw : _input.PrevClassHeldRaw;
            if (!held) Close(true);
        }

        // ------------------------------------------------------------ desenho

        void Refresh()
        {
            var data = SaveManager.Data;
            int unlocked = 0, order = 0;
            for (int i = 0; i < Count; i++)
            {
                var c = (CharacterClass)i;
                bool open = data != null && data.IsUnlocked(c) && _classes.Definition(c) != null;
                if (open) { unlocked++; if (i <= _hover) order = unlocked; }
                var def = _classes.Definition(c);
                bool hover = i == _hover;
                bool current = c == _classes.CurrentClass;
                Color theme = def != null ? def.ThemeColor : Color.white;
                _segments[i].color = !open ? LockedColor
                    : hover ? Color.Lerp(theme, Color.white, 0.25f)
                    : current ? Color.Lerp(SegmentColor, CurrentEdge, 0.25f) : SegmentColor;
                _segments[i].rectTransform.localScale = Vector3.one * (hover ? 1.06f : 1f);
                // só as classes que o gato tem mostram o ícone; as outras ficam só a fatia escura (pedido de 14/11/2026)
                _icons[i].enabled = open;
                if (open) _icons[i].sprite = WheelIcons.For(c, false);
                _labels[i].text = open ? (def != null ? def.DisplayName : c.ToString()) : "";
                _labels[i].color = hover ? Color.white : new Color(0.85f, 0.83f, 0.78f, 1f);
            }
            var h = _hover >= 0 ? (CharacterClass)_hover : _classes.CurrentClass;
            bool hOpen = data != null && data.IsUnlocked(h) && _classes.Definition(h) != null;
            _title.text = hOpen ? _classes.Definition(h).DisplayName : "???";
            _counter.text = hOpen ? "‹  " + order + " / " + unlocked + "  ›" : "ainda não";
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var dim = NewImage("Escuro", transform, null);
            dim.color = new Color(0.02f, 0.02f, 0.05f, 0.45f);
            var drt = dim.rectTransform;
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;

            var wheel = new GameObject("Roda", typeof(RectTransform)).GetComponent<RectTransform>();
            wheel.SetParent(transform, false);
            wheel.sizeDelta = new Vector2(Size, Size);

            float step = 360f / Count;
            for (int i = 0; i < Count; i++)
            {
                var seg = NewImage("Fatia " + i, wheel, RingSprite());
                seg.rectTransform.sizeDelta = new Vector2(Size, Size);
                seg.type = Image.Type.Filled;
                seg.fillMethod = Image.FillMethod.Radial360;
                seg.fillOrigin = (int)Image.Origin360.Top;
                seg.fillClockwise = true;
                seg.fillAmount = (step - GapDegrees) / 360f;
                // a fatia i fica centrada no ângulo i × passo (horário a partir de cima)
                seg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(i * step - step * 0.5f + GapDegrees * 0.5f));
                _segments[i] = seg;

                float a = ClassRules.WheelSlotAngle(i, Count) * Mathf.Deg2Rad;
                float r = Size * 0.5f * 0.76f;
                Vector2 at = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r;
                var icon = NewImage("Ícone " + i, wheel, null);
                icon.rectTransform.sizeDelta = new Vector2(24f, 24f);
                icon.rectTransform.anchoredPosition = at + new Vector2(0f, 5f);
                _icons[i] = icon;
                var label = NewText("Nome " + i, wheel, 9, TextAnchor.MiddleCenter);
                label.rectTransform.sizeDelta = new Vector2(70f, 12f);
                label.rectTransform.anchoredPosition = at + new Vector2(0f, -13f);
                _labels[i] = label;
            }

            var disc = NewImage("Centro", wheel, DiscSprite());
            disc.rectTransform.sizeDelta = new Vector2(Size * 0.5f, Size * 0.5f);
            disc.color = new Color(0.93f, 0.92f, 0.88f, 0.95f);
            _title = NewText("Classe", wheel, 13, TextAnchor.MiddleCenter);
            _title.color = new Color(0.12f, 0.11f, 0.14f, 1f);
            _title.rectTransform.sizeDelta = new Vector2(90f, 16f);
            _title.rectTransform.anchoredPosition = new Vector2(0f, 7f);
            _counter = NewText("Contador", wheel, 10, TextAnchor.MiddleCenter);
            _counter.color = new Color(0.25f, 0.42f, 0.62f, 1f);
            _counter.rectTransform.sizeDelta = new Vector2(90f, 14f);
            _counter.rectTransform.anchoredPosition = new Vector2(0f, -9f);
        }

        Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Anel 96 × 96 (branco, borda escura de 1 px): cada fatia é ele cortado em pizza.</summary>
        static Sprite RingSprite()
        {
            if (s_ring != null) return s_ring;
            const int n = 96;
            float c = (n - 1) * 0.5f, outer = 47f, inner = 25f;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (d > outer || d < inner) continue;
                    bool edge = d > outer - 1.2f || d < inner + 1.2f;
                    px[y * n + x] = edge ? new Color32(0xc8, 0xc8, 0xd0, 0xff) : new Color32(0xff, 0xff, 0xff, 0xff);
                }
            s_ring = MakeSprite(px, n, "ui_roda_anel");
            return s_ring;
        }

        static Sprite DiscSprite()
        {
            if (s_disc != null) return s_disc;
            const int n = 48;
            float c = (n - 1) * 0.5f;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (d <= 23.5f) px[y * n + x] = new Color32(0xff, 0xff, 0xff, 0xff);
                }
            s_disc = MakeSprite(px, n, "ui_roda_centro");
            return s_disc;
        }

        static Sprite MakeSprite(Color32[] px, int n, string name)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            var s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }

    /// <summary>Ícones das classes para a roda (os mesmos desenhos do inventário), feitos uma vez.</summary>
    static class WheelIcons
    {
        static readonly Sprite[] s_open = new Sprite[ClassRules.ClassCount], s_locked = new Sprite[ClassRules.ClassCount];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { System.Array.Clear(s_open, 0, s_open.Length); System.Array.Clear(s_locked, 0, s_locked.Length); }

        static readonly InventoryIcon[] Icons =
        {
            InventoryIcon.Rogue, InventoryIcon.Paladin, InventoryIcon.Mage, InventoryIcon.Monk,
            InventoryIcon.Bard, InventoryIcon.Hunter, InventoryIcon.Druid,
        };

        public static Sprite For(CharacterClass c, bool locked)
        {
            int i = (int)c;
            var cache = locked ? s_locked : s_open;
            if (cache[i] != null) return cache[i];
            var grid = locked ? InventoryIcons.Silhouette(Icons[i]) : InventoryIcons.Draw(Icons[i]);
            int h = grid.GetLength(0), w = grid.GetLength(1);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    string hex = grid[y, x];
                    px[(h - 1 - y) * w + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var col) ? (Color32)col : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            cache[i] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            cache[i].hideFlags = HideFlags.DontSave;
            return cache[i];
        }
    }
}
