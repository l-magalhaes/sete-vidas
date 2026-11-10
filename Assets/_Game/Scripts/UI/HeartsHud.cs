using SeteVidas.Player;
using SeteVidas.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Os 7 corações no canto superior esquerdo: cheio (2 hits), meio (1), vazio (0) e escurecido (ainda não
    /// recuperado). Lê o <see cref="PlayerHealth.Active"/>.
    /// </summary>
    public class HeartsHud : MonoBehaviour
    {
        [SerializeField] Image[] hearts = new Image[7];
        [SerializeField] Sprite full;
        [SerializeField] Sprite half;
        [SerializeField] Sprite empty;
        [SerializeField] Sprite locked;

        void LateUpdate()
        {
            var health = PlayerHealth.Active;
            if (health == null) return;

            var states = HealthRules.Hearts(health.HeartsRecovered, health.CurrentHits);
            for (int i = 0; i < hearts.Length && i < states.Length; i++)
            {
                if (hearts[i] == null) continue;
                var sprite = states[i] switch
                {
                    HeartState.Full => full,
                    HeartState.Half => half,
                    HeartState.Empty => empty,
                    _ => locked,
                };
                if (hearts[i].sprite != sprite) hearts[i].sprite = sprite;
            }
            UpdateHealCharges(health);
        }

        // ------------------------------------------------- magia de cura

        [Header("Magia de cura (Paladino)")]
        [Tooltip("Orbe cheio / vazio. Vazio = desenhados em código (dourado 9×9)")]
        [SerializeField] Sprite healFull;
        [SerializeField] Sprite healEmpty;

        Image[] _healPips;

        /// <summary>Um orbe dourado por uso, logo abaixo dos corações; só aparece depois de aprender a magia.</summary>
        void UpdateHealCharges(PlayerHealth health)
        {
            var spell = health.GetComponent<HealingSpell>();
            bool show = spell != null && spell.IsUnlocked && hearts.Length > 0 && hearts[0] != null;
            if (!show)
            {
                if (_healPips != null) foreach (var p in _healPips) if (p != null) p.enabled = false;
                return;
            }
            if (_healPips == null || _healPips.Length != spell.MaxCharges) BuildPips(spell.MaxCharges);
            for (int i = 0; i < _healPips.Length; i++)
            {
                _healPips[i].enabled = true;
                var s = i < spell.Charges ? healFull : healEmpty;
                if (_healPips[i].sprite != s) _healPips[i].sprite = s;
            }
        }

        void BuildPips(int count)
        {
            if (healFull == null) healFull = OrbSprite(true);
            if (healEmpty == null) healEmpty = OrbSprite(false);
            var first = hearts[0].rectTransform;
            float size = Mathf.Round(first.sizeDelta.x * 0.5625f);           // 9 px ao lado de corações de 16 px
            _healPips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("CuraUso" + (i + 1), typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(first.parent, false);
                rt.anchorMin = first.anchorMin; rt.anchorMax = first.anchorMax; rt.pivot = first.pivot;
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = first.anchoredPosition + new Vector2(i * (size + 2f), -(first.sizeDelta.y + 1f));
                var img = go.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                _healPips[i] = img;
            }
        }

        /// <summary>Orbe de luz 9×9 em pixel art: cheio (dourado com brilho) ou vazio (só o contorno).</summary>
        static Sprite OrbSprite(bool full)
        {
            var outline = new Color32(0x1a, 0x1c, 0x26, 0xff);
            var gold = new Color32(0xe0, 0xb0, 0x4a, 0xff);
            var light = new Color32(0xff, 0xf4, 0xc2, 0xff);
            var shade = new Color32(0xa8, 0x7a, 0x22, 0xff);
            var hollow = new Color32(0x3a, 0x33, 0x50, 0xff);
            var px = new Color32[81];
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                {
                    float dx = x - 4f, dy = y - 4f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (d <= 4.4f) c = outline;
                    if (d <= 3.4f) c = full ? (dy > 0.5f ? gold : shade) : hollow;      // y cresce para cima
                    if (full && d <= 1.6f && dx <= 0f && dy >= 0f) c = light;
                    px[y * 9 + x] = c;
                }
            // cruz de luz no centro (o "sol" da Ordem)
            if (full) { px[4 * 9 + 4] = light; px[5 * 9 + 4] = light; px[4 * 9 + 5] = light; }
            var tex = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
