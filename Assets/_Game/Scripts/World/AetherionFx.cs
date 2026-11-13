using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.World
{
    /// <summary>
    /// A corrupção de Aetherion, a torre dos magos ao longe (pedido do usuário, 12/11/2026): coroa de nuvens roxas
    /// girando em volta do topo, janelas verde-ácido pulsando fora de sincronia, relâmpagos lilás de tempos em tempos
    /// (com o contorno da torre aceso), corvos circulando e fiapos escuros subindo do topo quebrado.
    /// Fica na camada da torre (fundo da Fortaleza, conjuntos A, F, G e H) ou na tela inicial (modo UI, com Image).
    /// Os quadros vêm de Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Ext/Aetherion); relâmpago, corvos e
    /// fiapos são desenhados aqui. Segue o pixel: tudo em posições inteiras, sem transparência.
    /// </summary>
    public class AetherionFx : MonoBehaviour
    {
        [SerializeField] Sprite[] clouds = new Sprite[0];
        [SerializeField] Sprite[] windows = new Sprite[0];
        [SerializeField] Sprite flash;
        [Tooltip("Noite: sem o sol, a corrupção aparece mais (relâmpagos mais frequentes, janelas mais acesas)")]
        [SerializeField] bool night;
        [Tooltip("Escala do desenho (1 = fundo da fase, 0,5 = tela inicial)")]
        [SerializeField] float scale = 1f;
        [SerializeField, Min(0.05f)] float cloudFrameTime = 0.22f;
        [SerializeField] Vector2 lightningInterval = new Vector2(8f, 15f);

        const int Crows = 4, Wisps = 6;
        static readonly Color32 CrowColor = new Color32(0x14, 0x0f, 0x18, 0xff);
        static readonly Color32 WispColor = new Color32(0x3a, 0x1e, 0x48, 0xff);
        static readonly Color32 BoltColor = new Color32(0xc8, 0xa0, 0xff, 0xff);
        static readonly Color32 BoltCore = new Color32(0xf0, 0xe4, 0xff, 0xff);

        static Sprite[] s_crow;
        static Sprite s_dot;
        static readonly Dictionary<int, Sprite> s_bolts = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_crow = null; s_dot = null; s_bolts.Clear(); } // Play sem recarregar o domínio

        // um "desenho" é um SpriteRenderer (fase) ou uma Image (tela inicial)
        class Part
        {
            public SpriteRenderer R; public Image I;
            public void Set(Sprite s) { if (R != null) R.sprite = s; else if (I != null) { I.sprite = s; if (s != null) I.rectTransform.sizeDelta = s.rect.size * PxToUi; } }
            public void Show(bool on) { if (R != null) R.enabled = on; else if (I != null) I.enabled = on; }
            public float PxToUi = 1f;
            public void At(Transform owner, Vector2 px)
            {
                var t = R != null ? R.transform : I.rectTransform;
                if (R != null) t.localPosition = new Vector3(Mathf.Round(px.x) / 16f, Mathf.Round(px.y) / 16f, 0f);
                else ((RectTransform)t).anchoredPosition = new Vector2(Mathf.Round(px.x), Mathf.Round(px.y)) * PxToUi;
            }
        }

        Transform _anchor;          // o desenho da torre (pivô no centro)
        Vector2 _size;              // tamanho da torre em px
        bool _ui;
        int _order;
        Part _clouds, _flash, _bolt;
        Part[] _windows, _crows, _wisps;
        float[] _windowNext, _crowAngle, _crowSpeed, _wispAge;
        Vector2[] _wispFrom;
        float _time, _nextLightning, _flashAge = 99f;

        /// <summary>Configura (setup do editor).</summary>
        public void Configure(Sprite[] cloudFrames, Sprite[] windowFrames, Sprite flashFrame, bool isNight, float drawScale)
        {
            clouds = cloudFrames; windows = windowFrames; flash = flashFrame; night = isNight; scale = drawScale;
            if (night) lightningInterval = new Vector2(5f, 10f);
        }

        void Start()
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            var img = sr == null ? GetComponent<Image>() : null;
            if (sr == null && img == null) { enabled = false; return; }
            _ui = img != null;
            _anchor = _ui ? img.transform : sr.transform;
            _order = sr != null ? sr.sortingOrder : 0;
            var baseSprite = _ui ? img.sprite : sr.sprite;
            _size = baseSprite != null ? baseSprite.rect.size : new Vector2(110f, 180f) * scale;
            float pxToUi = _ui && baseSprite != null ? img.rectTransform.sizeDelta.x / baseSprite.rect.width : 1f;

            _windows = new Part[Mathf.Max(0, windows.Length)];
            _windowNext = new float[_windows.Length];
            for (int i = 0; i < _windows.Length; i++)
            {
                _windows[i] = NewPart("Janelas " + i, 1, pxToUi, true);
                _windows[i].Set(windows[i]);
                _windowNext[i] = Random.Range(0f, 1.2f);
            }
            _clouds = NewPart("Coroa de nuvens", 2, pxToUi, true);
            if (clouds.Length > 0) _clouds.Set(clouds[0]);
            _flash = NewPart("Clarão", 3, pxToUi, true);
            _flash.Set(flash);
            _flash.Show(false);
            _bolt = NewPart("Relâmpago", 3, pxToUi, false);
            _bolt.Show(false);

            _crows = new Part[Crows];
            _crowAngle = new float[Crows];
            _crowSpeed = new float[Crows];
            for (int i = 0; i < Crows; i++)
            {
                _crows[i] = NewPart("Corvo " + i, 4, pxToUi, false);
                _crows[i].Set(CrowSprites()[0]);
                _crowAngle[i] = i * Mathf.PI * 2f / Crows + Random.value;
                _crowSpeed[i] = Random.Range(0.35f, 0.6f) * (i % 2 == 0 ? 1f : -1f);
            }
            _wisps = new Part[Wisps];
            _wispAge = new float[Wisps];
            _wispFrom = new Vector2[Wisps];
            for (int i = 0; i < Wisps; i++)
            {
                _wisps[i] = NewPart("Fiapo " + i, 1, pxToUi, false);
                _wisps[i].Set(Dot());
                _wispAge[i] = -Random.Range(0f, 1.5f);
            }
            _nextLightning = Random.Range(2f, lightningInterval.y);
        }

        Part NewPart(string name, int orderOffset, float pxToUi, bool centered)
        {
            var p = new Part { PxToUi = pxToUi };
            var go = _ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            go.transform.SetParent(_anchor, false);
            if (_ui)
            {
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                p.I = go.AddComponent<Image>();
                p.I.raycastTarget = false;
            }
            else
            {
                p.R = go.AddComponent<SpriteRenderer>();
                p.R.sortingOrder = _order + orderOffset;
            }
            return p;
        }

        /// <summary>Ponto do desenho da torre (px a partir do canto de cima à esquerda) em coordenadas locais (px do centro).</summary>
        Vector2 FromTopLeft(float x, float y) => new Vector2(x - _size.x * 0.5f, _size.y * 0.5f - y);

        void Update()
        {
            if (_anchor == null) return;
            float dt = Time.unscaledDeltaTime;   // a tela inicial e a pausa também mexem
            _time += dt;
            float s = scale;

            // coroa de nuvens
            if (clouds.Length > 0) _clouds.Set(clouds[(int)(_time / cloudFrameTime) % clouds.Length]);

            // janelas: cada quadro liga e desliga no próprio ritmo (fora de sincronia)
            for (int i = 0; i < _windows.Length; i++)
            {
                _windowNext[i] -= dt;
                if (_windowNext[i] > 0f) continue;
                bool on = Random.value < (night ? 0.75f : 0.55f);
                _windows[i].Show(on);
                _windowNext[i] = Random.Range(0.5f, 1.6f);
            }

            // relâmpago: clarão duplo (liga, apaga, liga) e um raio fino dentro das nuvens
            _nextLightning -= dt;
            if (_nextLightning <= 0f)
            {
                _nextLightning = Random.Range(lightningInterval.x, lightningInterval.y);
                _flashAge = 0f;
                int h = Mathf.RoundToInt(Random.Range(10f, 18f) * Mathf.Max(0.6f, s));
                _bolt.Set(Bolt(h, Random.Range(0, 6)));
                float bx = Random.Range(-30f, 30f) * s;
                _bolt.At(_anchor, FromTopLeft(_size.x * 0.5f + bx, 22f * s + h * 0.5f));
            }
            _flashAge += dt;
            bool flashOn = _flashAge < 0.06f || (_flashAge > 0.12f && _flashAge < 0.17f);
            _flash.Show(flashOn && flash != null);
            _bolt.Show(_flashAge < 0.17f);

            // corvos circulando o topo (elipse achatada), batendo asa
            var crowFrames = CrowSprites();
            for (int i = 0; i < Crows; i++)
            {
                _crowAngle[i] += _crowSpeed[i] * dt;
                float a = _crowAngle[i];
                var p = FromTopLeft(_size.x * 0.5f + Mathf.Cos(a) * 30f * s, (8f + 6f * (i % 2)) * s + Mathf.Sin(a) * 6f * s);
                _crows[i].At(_anchor, p);
                _crows[i].Set(crowFrames[((int)((_time + i * 0.13f) / 0.16f)) & 1]);
                if (_crows[i].R != null) _crows[i].R.flipX = Mathf.Sin(a) * _crowSpeed[i] > 0f;
            }

            // fiapos escuros subindo do topo quebrado
            for (int i = 0; i < Wisps; i++)
            {
                _wispAge[i] += dt;
                if (_wispAge[i] < 0f) { _wisps[i].Show(false); continue; }
                if (_wispFrom[i] == Vector2.zero) // nasceu agora: escolhe de onde sobe
                    _wispFrom[i] = FromTopLeft(_size.x * 0.5f + Random.Range(-6f, 6f) * s, 30f * s);
                float k = _wispAge[i] / 1.4f;
                if (k >= 1f) { _wispAge[i] = -Random.Range(0.2f, 1.2f); _wispFrom[i] = Vector2.zero; _wisps[i].Show(false); continue; }
                var wp = _wispFrom[i] + new Vector2(Mathf.Sin(k * 6f + i) * 1.5f, k * 16f * s);
                _wisps[i].At(_anchor, wp);
                _wisps[i].Show(k < 0.7f || ((int)(k * 20f) & 1) == 0); // some piscando no alto
            }
        }

        // --------------------------------------------------------------- desenhos

        static Sprite[] CrowSprites()
        {
            if (s_crow != null && s_crow[0] != null) return s_crow;
            // asa para cima ("V") e asa para baixo ("^"), 5×3
            string[] up = { "#...#", ".#.#.", "..#.." };
            string[] down = { ".....", "..#..", "##.##" };
            s_crow = new[] { Map(up, CrowColor, "corvo 0"), Map(down, CrowColor, "corvo 1") };
            return s_crow;
        }

        static Sprite Dot()
        {
            if (s_dot != null) return s_dot;
            s_dot = Map(new[] { "#" }, WispColor, "fiapo");
            return s_dot;
        }

        static Sprite Map(string[] rows, Color32 color, string name)
        {
            int w = rows[0].Length, h = rows.Length;
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                    px[(h - 1 - r) * w + c] = rows[r][c] == '#' ? color : new Color32(0, 0, 0, 0);
            return Combat.PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0.5f), name);
        }

        /// <summary>Raio fino em zigue-zague (1 px lilás com o miolo claro), <paramref name="h"/> px de altura.</summary>
        static Sprite Bolt(int h, int variant)
        {
            int key = h * 8 + variant;
            if (s_bolts.TryGetValue(key, out var s) && s != null) return s;
            int w = 9;
            var px = new Color32[w * h];
            var rng = new System.Random(key * 17 + 3);
            int x = w / 2;
            for (int y = h - 1; y >= 0; y--)
            {
                if (y % 3 == 0) x = Mathf.Clamp(x + rng.Next(-1, 2), 1, w - 2);
                px[y * w + x] = BoltCore;
                if (x + 1 < w) px[y * w + x + 1] = BoltColor;
                if (y == h / 2 && x > 2) { px[y * w + x - 1] = BoltColor; px[(y - 1) * w + x - 2] = BoltColor; } // galho
            }
            s = Combat.PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0.5f), "relâmpago de Aetherion");
            s_bolts[key] = s;
            return s;
        }
    }
}
