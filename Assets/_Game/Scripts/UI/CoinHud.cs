using SeteVidas.Combat;
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
        Image _count, _gain;      // texto na fonte em pixel (PixelFont), em pixels do HUD
        string _countText = "";
        Vector2 _countPos, _gainPos;
        float _bumpUntil, _gainUntil;
        int _shown = -1;

        void Awake() => Build();

        void OnEnable() => CoinPurse.Changed += OnChanged;
        void OnDisable() => CoinPurse.Changed -= OnChanged;

        void Build()
        {
            if (_root != null) return;
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

            _countPos = new Vector2(11f, 0f);
            _gainPos = new Vector2(40f, 0f);
            _count = NewLabel("Quantidade", _root, _countPos);
            _gain = NewLabel("Ganho", _root, _gainPos);
            _gain.enabled = false;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Image NewLabel(string name, RectTransform parent, Vector2 at)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = at;
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Põe o texto na fonte em pixel, no tamanho exato do sprite (1 px do sprite = 1 px do HUD).</summary>
        static void SetLabel(Image img, string text, Color color)
        {
            var sprite = PixelFont.Render(text, color, PixelFont.Outline, 0f);
            img.sprite = sprite;
            img.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
        }

        void OnChanged(int total, int delta)
        {
            if (delta <= 0) return;
            _bumpUntil = Time.unscaledTime + 0.25f;
            _gainUntil = Time.unscaledTime + 1.2f;
            if (_gain != null) SetLabel(_gain, "+" + delta, gainColor);
        }

        void LateUpdate()
        {
            if (_count == null) return;
            int coins = CoinPurse.Coins;
            if (coins != _shown)
            {
                _shown = coins;
                _countText = coins.ToString();
                SetLabel(_count, _countText, textColor);
            }
            // pulo de 2 px que desce em passos (sem escala: o número continua no grid do pixel)
            float left = _bumpUntil - Time.unscaledTime;
            int jump = left > 0.17f ? 2 : left > 0.08f ? 1 : 0;
            _count.rectTransform.anchoredPosition = _countPos + new Vector2(0f, jump);
            if (_gain != null)
            {
                float gainLeft = _gainUntil - Time.unscaledTime;
                // "+N" ao lado do número; no fim pisca em vez de sumir na transparência
                _gain.enabled = gainLeft > 0f && (gainLeft > 0.3f || ((int)(gainLeft * 20f) & 1) == 0);
                _gain.rectTransform.anchoredPosition = new Vector2(_countPos.x + _count.rectTransform.sizeDelta.x + 3f, _countPos.y);
            }
        }

        /// <summary>Texto mostrado (testes).</summary>
        public string ShownText => _countText;
    }
}
