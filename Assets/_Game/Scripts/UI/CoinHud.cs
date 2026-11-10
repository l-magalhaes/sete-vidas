using SeteVidas.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Contador da bolsa de moedas no canto superior esquerdo, abaixo dos corações (e dos usos da cura).
    /// Monta o próprio ícone e o texto ao começar. Ao ganhar moedas, o número dá um pulinho e mostra "+N".
    /// Spec: Docs/Design/moedas.md.
    /// </summary>
    public class CoinHud : MonoBehaviour
    {
        [Tooltip("Ícone 9×9 (ui_coin.png)")]
        [SerializeField] Sprite icon;
        [Tooltip("Canto superior esquerdo do contador, em pixels do HUD (480×270 ×4)")]
        [SerializeField] Vector2 position = new Vector2(28f, -38f);
        [SerializeField] Color textColor = new Color32(0xff, 0xe9, 0xa8, 0xff);
        [SerializeField] Color gainColor = new Color32(0xff, 0xd3, 0x4d, 0xff);

        RectTransform _root;
        Text _count, _gain;
        float _bumpUntil, _gainUntil;
        int _shown = -1;

        void Awake() => Build();

        void OnEnable() => CoinPurse.Changed += OnChanged;
        void OnDisable() => CoinPurse.Changed -= OnChanged;

        void Build()
        {
            if (_root != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _root = NewRect("Moedas", transform);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = position;
            _root.sizeDelta = new Vector2(80f, 10f);

            var iconRt = NewRect("Ícone", _root);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(9f, 9f);
            var img = iconRt.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;

            _count = NewText("Quantidade", _root, font, textColor, new Vector2(12f, 1f));
            _gain = NewText("Ganho", _root, font, gainColor, new Vector2(48f, 1f));
            _gain.text = "";
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Text NewText(string name, RectTransform parent, Font font, Color color, Vector2 at)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = at;
            rt.sizeDelta = new Vector2(48f, 12f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = 9;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0x1a, 0x1c, 0x26, 0xff);
            shadow.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        void OnChanged(int total, int delta)
        {
            if (delta <= 0) return;
            _bumpUntil = Time.unscaledTime + 0.25f;
            _gainUntil = Time.unscaledTime + 1.2f;
            if (_gain != null) _gain.text = "+" + delta;
        }

        void LateUpdate()
        {
            if (_count == null) return;
            int coins = CoinPurse.Coins;
            if (coins != _shown) { _shown = coins; _count.text = coins.ToString(); }
            float bump = Time.unscaledTime < _bumpUntil ? 1f + 0.25f * ((_bumpUntil - Time.unscaledTime) / 0.25f) : 1f;
            _count.rectTransform.localScale = new Vector3(bump, bump, 1f);
            if (_gain != null)
            {
                float left = _gainUntil - Time.unscaledTime;
                _gain.enabled = left > 0f;
                if (left > 0f) { var c = gainColor; c.a = Mathf.Clamp01(left / 0.4f); _gain.color = c; }
            }
        }

        /// <summary>Texto mostrado (testes).</summary>
        public string ShownText => _count != null ? _count.text : "";
    }
}
