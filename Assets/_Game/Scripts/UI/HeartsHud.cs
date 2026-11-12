using System.Collections.Generic;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.World;
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

        [Tooltip("Melhoria 7: corações vermelhos vestidos com o acessório da classe (desenhados em código). Desligado: os 4 sprites acima")]
        [SerializeField] bool classHearts = true;
        [Tooltip("Ao recuperar um coração, ele pulsa por este tempo (s)")]
        [SerializeField, Min(0f)] float recoverPulse = 0.35f;
        [Tooltip("Coração perdido (maldição): racha e some neste tempo (s)")]
        [SerializeField, Min(0f)] float loseFade = 0.45f;

        static readonly Dictionary<int, Sprite> s_classSprites = new Dictionary<int, Sprite>();
        HeartState[] _last;
        float[] _pulseUntil, _fadeUntil;
        Vector2[] _basePos;
        Image _paladinGlow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_classSprites.Clear(); s_glowSprite = null; s_scroll = s_scrollFaded = s_sealLit = s_sealOut = null; } // Play sem recarregar o domínio

        /// <summary>
        /// De que classe é o coração <paramref name="index"/> (regra do usuário): antes da maldição, os 7 são do Ladino;
        /// depois dela, cada coração é o da classe na ordem (o 2º recuperado é o do Paladino, e assim por diante).
        /// </summary>
        public static int HeartClass(int index, bool curseDone) => curseDone ? index : 0;

        /// <summary>A maldição já terminou neste save (com migração dos saves de antes da marca).</summary>
        static bool CurseDone(int heartsRecovered)
        {
            var data = SaveManager.Data;
            if (data == null) return false;
            return data.HasFlag(PlayerHealth.CurseDoneFlag);
        }

        void Start()
        {
            InventoryMenu.Ensure(); // melhoria 10: o inventário (Tab, I ou Start) vem junto do HUD
            // save de antes da marca: tomo pego e menos de 7 corações = a maldição já passou
            var data = SaveManager.Data;
            var health = PlayerHealth.Active;
            if (data != null && health != null && data.HasFlag(CursedTome.TakenFlag) && health.HeartsRecovered < GameConstants.MaxHearts)
                data.SetFlag(PlayerHealth.CurseDoneFlag);
        }

        void LateUpdate()
        {
            var health = PlayerHealth.Active;
            if (health == null) return;

            var states = HealthRules.Hearts(health.HeartsRecovered, health.CurrentHits);
            if (_last == null || _last.Length != states.Length) {
                _last = (HeartState[])states.Clone();
                _pulseUntil = new float[states.Length];
                _fadeUntil = new float[states.Length];
                _basePos = new Vector2[hearts.Length];
                for (int i = 0; i < hearts.Length; i++) if (hearts[i] != null) _basePos[i] = hearts[i].rectTransform.anchoredPosition;
            }
            bool curseDone = CurseDone(health.HeartsRecovered);
            for (int i = 0; i < hearts.Length && i < states.Length; i++)
            {
                if (hearts[i] == null) continue;

                // perdeu o coração (maldição): racha e some; travado não aparece
                if (_last[i] != HeartState.Locked && states[i] == HeartState.Locked) _fadeUntil[i] = Time.unscaledTime + loseFade;
                bool fading = states[i] == HeartState.Locked && Time.unscaledTime < _fadeUntil[i];
                hearts[i].enabled = !classHearts || states[i] != HeartState.Locked || fading;
                var tint = hearts[i].color;
                tint.a = fading && loseFade > 0f ? Mathf.Clamp01((_fadeUntil[i] - Time.unscaledTime) / loseFade) : 1f;
                hearts[i].color = tint;

                Sprite sprite = classHearts ? ClassSprite(HeartClass(i, curseDone), states[i]) : null;
                if (sprite == null)
                    sprite = states[i] switch
                    {
                        HeartState.Full => full,
                        HeartState.Half => half,
                        HeartState.Empty => empty,
                        _ => locked,
                    };
                if (hearts[i].sprite != sprite) hearts[i].sprite = sprite;

                // recuperou o coração (estava travado): pulsa
                if (_last[i] == HeartState.Locked && states[i] != HeartState.Locked) _pulseUntil[i] = Time.unscaledTime + recoverPulse;
                _last[i] = states[i];
                float left = _pulseUntil[i] - Time.unscaledTime;
                float k = left > 0f && recoverPulse > 0f ? Mathf.Sin((1f - left / recoverPulse) * Mathf.PI) : 0f;
                float scale = 1f + 0.35f * k;
                var rt = hearts[i].rectTransform;
                rt.localScale = new Vector3(scale, scale, 1f);
                // o pivô é o canto de cima: compensa para crescer a partir do meio
                Vector2 grow = rt.sizeDelta * (scale - 1f) * 0.5f;
                rt.anchoredPosition = _basePos[i] + new Vector2(-grow.x, grow.y);
            }
            UpdatePaladinGlow(curseDone && classHearts && states.Length > 1 && states[1] != HeartState.Locked);
            UpdateHealCharges(health);
        }

        // ------------------------------------------- luz dourada do Paladino

        static Sprite s_glowSprite;

        /// <summary>O coração do Paladino tem uma luz dourada saindo dele (pedido do usuário): raios atrás, pulsando.</summary>
        void UpdatePaladinGlow(bool show)
        {
            if (hearts.Length < 2 || hearts[1] == null) return;
            if (_paladinGlow == null)
            {
                if (!show) return;
                var heart = hearts[1].rectTransform;
                var go = new GameObject("Luz do Paladino", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(heart.parent, false);
                rt.SetSiblingIndex(heart.GetSiblingIndex()); // atrás do coração
                rt.anchorMin = heart.anchorMin; rt.anchorMax = heart.anchorMax; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = heart.sizeDelta * 1.75f;
                _paladinGlow = go.GetComponent<Image>();
                _paladinGlow.sprite = GlowSprite();
                _paladinGlow.raycastTarget = false;
            }
            _paladinGlow.enabled = show;
            if (!show) return;
            var h = hearts[1].rectTransform;
            var g = _paladinGlow.rectTransform;
            // no meio do coração (o pivô dele é o canto de cima; o desenho fica 2 px abaixo do meio do quadro)
            g.anchoredPosition = h.anchoredPosition + new Vector2(h.sizeDelta.x * 0.5f, -h.sizeDelta.y * 0.5f - 1f);
            float t = Time.unscaledTime;
            var c = _paladinGlow.color;
            c.a = 0.55f + 0.3f * Mathf.Sin(t * 3f);
            _paladinGlow.color = c;
            float s = 1f + 0.08f * Mathf.Sin(t * 3f + 1f);
            g.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Raios dourados 28×28 em pixel art (8 direções, mais fortes perto do meio), em código.</summary>
        static Sprite GlowSprite()
        {
            if (s_glowSprite != null) return s_glowSprite;
            const int n = 28;
            var px = new Color32[n * n];
            var gold = new Color32(0xff, 0xd3, 0x4d, 0xff);
            var pale = new Color32(0xff, 0xf4, 0xc2, 0xff);
            float cx = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - cx, dy = y - cx, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float toRay = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 45f) * 45f));
                    bool ray = toRay < (d < 9f ? 10f : 6f) && d < 13.5f && d > 5f && ((x + y) % 2 == 0 || d < 9f);
                    bool halo = d < 8.5f && d > 5f && (x + y) % 2 == 0;     // halo pontilhado em volta
                    if (!ray && !halo) continue;
                    var c = d < 8f ? pale : gold;
                    c.a = (byte)Mathf.Clamp(255f * (1f - (d - 5f) / 9f), 60f, 255f);
                    px[y * n + x] = c;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
            s_glowSprite.name = "ui_luz_paladino";
            return s_glowSprite;
        }

        static HeartLook LookOf(HeartState state) => state switch
        {
            HeartState.Full => HeartLook.Full,
            HeartState.Half => HeartLook.Half,
            HeartState.Empty => HeartLook.Empty,
            _ => HeartLook.Locked,
        };

        /// <summary>O coração da classe <paramref name="index"/> no estado dado (16×16, criado uma vez e guardado).</summary>
        public static Sprite ClassSprite(int index, HeartState state)
        {
            var look = LookOf(state);
            int key = index * 8 + (int)look;
            if (s_classSprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var grid = ClassHeartPixels.Draw(index, look);
            int size = ClassHeartPixels.Size;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    string hex = grid[y, x];
                    // a grade vem de cima para baixo; a textura, de baixo para cima
                    px[(size - 1 - y) * size + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = $"ui_coracao_{index}_{look}";
            s_classSprites[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------- magia de cura

        [Header("Magia de cura (Paladino)")]
        [Tooltip("Orbe cheio / vazio. Vazio = desenhados em código (dourado 9×9)")]
        [SerializeField] Sprite healFull;
        [SerializeField] Sprite healEmpty;

        [Tooltip("Melhoria 11: pergaminho com a pata de luz e lacres de cera (em código). Desligado: os orbes")]
        [SerializeField] bool healScroll = true;
        [Tooltip("Ao usar a cura, o pergaminho brilha por este tempo (s)")]
        [SerializeField, Min(0f)] float scrollFlash = 0.18f;

        Image[] _healPips;
        Image _scroll;
        int _lastCharges = -1;
        float _scrollFlashUntil;
        static Sprite s_scroll, s_scrollFaded, s_sealLit, s_sealOut;

        /// <summary>
        /// Melhoria 11: o pergaminho da cura logo abaixo dos corações, com um lacre de cera por uso ao lado (aceso ou
        /// apagado); sem usos, desbota; ao curar, brilha. Só aparece depois de aprender a magia.
        /// </summary>
        void UpdateHealCharges(PlayerHealth health)
        {
            var spell = health.GetComponent<HealingSpell>();
            bool show = spell != null && spell.IsUnlocked && hearts.Length > 0 && hearts[0] != null;
            if (!show)
            {
                if (_healPips != null) foreach (var p in _healPips) if (p != null) p.enabled = false;
                if (_scroll != null) _scroll.enabled = false;
                return;
            }
            if (_healPips == null || _healPips.Length != spell.MaxCharges) BuildPips(spell.MaxCharges);
            if (_lastCharges > spell.Charges) _scrollFlashUntil = Time.unscaledTime + scrollFlash; // curou: brilha
            _lastCharges = spell.Charges;

            for (int i = 0; i < _healPips.Length; i++)
            {
                _healPips[i].enabled = true;
                bool lit = i < spell.Charges;
                var s = healScroll ? (lit ? s_sealLit : s_sealOut) : (lit ? healFull : healEmpty);
                if (_healPips[i].sprite != s) _healPips[i].sprite = s;
            }
            if (_scroll != null)
            {
                _scroll.enabled = true;
                var s = spell.Charges > 0 ? s_scroll : s_scrollFaded;
                if (_scroll.sprite != s) _scroll.sprite = s;
                bool flashing = Time.unscaledTime < _scrollFlashUntil;
                float k = flashing && scrollFlash > 0f ? 1f + 0.25f * Mathf.Sin((1f - (_scrollFlashUntil - Time.unscaledTime) / scrollFlash) * Mathf.PI) : 1f;
                _scroll.rectTransform.localScale = new Vector3(k, k, 1f);
            }
        }

        void BuildPips(int count)
        {
            if (healFull == null) healFull = OrbSprite(true);
            if (healEmpty == null) healEmpty = OrbSprite(false);
            if (healScroll && s_scroll == null)
            {
                s_scroll = SpriteFromGrid(HealScrollPixels.DrawScroll(false), HealScrollPixels.Size, "ui_pergaminho_cura");
                s_scrollFaded = SpriteFromGrid(HealScrollPixels.DrawScroll(true), HealScrollPixels.Size, "ui_pergaminho_cura_desbotado");
                s_sealLit = SpriteFromGrid(HealScrollPixels.DrawSeal(true), HealScrollPixels.SealSize, "ui_lacre_aceso");
                s_sealOut = SpriteFromGrid(HealScrollPixels.DrawSeal(false), HealScrollPixels.SealSize, "ui_lacre_apagado");
            }
            var first = hearts[0].rectTransform;
            Vector2 below = first.anchoredPosition + new Vector2(0f, -(first.sizeDelta.y + 1f));
            if (healScroll && _scroll == null)
            {
                _scroll = NewImage("Pergaminho da cura", first);
                // pivô no meio para o brilho crescer do centro
                _scroll.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _scroll.rectTransform.sizeDelta = new Vector2(HealScrollPixels.Size, HealScrollPixels.Size);
                _scroll.rectTransform.anchoredPosition = below + new Vector2(HealScrollPixels.Size * 0.5f, -HealScrollPixels.Size * 0.5f);
            }
            if (_healPips != null) foreach (var p in _healPips) if (p != null) Destroy(p.gameObject);
            _healPips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var img = NewImage("CuraUso" + (i + 1), first);
                var rt = img.rectTransform;
                if (healScroll)
                {
                    // lacres de 5×5 ao lado do pergaminho, alinhados com a borda de baixo dele
                    rt.sizeDelta = new Vector2(HealScrollPixels.SealSize, HealScrollPixels.SealSize);
                    rt.anchoredPosition = below + new Vector2(HealScrollPixels.Size + 1f + i * (HealScrollPixels.SealSize + 1f), -(HealScrollPixels.Size - HealScrollPixels.SealSize - 2f));
                }
                else
                {
                    float size = Mathf.Round(first.sizeDelta.x * 0.5625f);           // 9 px ao lado de corações de 16 px
                    rt.sizeDelta = new Vector2(size, size);
                    rt.anchoredPosition = below + new Vector2(i * (size + 2f), 0f);
                }
                _healPips[i] = img;
            }
        }

        static Image NewImage(string name, RectTransform like)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(like.parent, false);
            rt.anchorMin = like.anchorMin; rt.anchorMax = like.anchorMax; rt.pivot = like.pivot;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Sprite a partir de uma grade de cores hex (de cima para baixo), filtro Point, 16 px por unidade.</summary>
        static Sprite SpriteFromGrid(string[,] grid, int size, string name)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    string hex = grid[y, x];
                    px[(size - 1 - y) * size + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;
            return sprite;
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
