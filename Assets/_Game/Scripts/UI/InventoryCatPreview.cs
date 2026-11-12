using SeteVidas.Classes;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// O gato do inventário: os mesmos quadros do jogo (clip "idle", piscando de vez em quando), com a cauda e a capa
    /// de física própria e a arma da classe equipada (a marreta nas costas do Paladino). Roda no tempo real, então
    /// respira mesmo com o jogo pausado. Cada peça é uma Image posicionada em pixels a partir dos pés.
    /// </summary>
    public class InventoryCatPreview : MonoBehaviour
    {
        static readonly Vector2 CapeFromTrim = new Vector2(-1.5f, 0.5f);   // igual ao PlayerVisual

        CharacterAnimationSet _set;
        Image _cape, _tail, _back, _body, _front, _trim;
        SoftChain _tailChain, _capeChain;
        PixelBuffer _tailBuffer, _capeBuffer;
        Color32[] _tailPalette, _capePalette;
        readonly float[] _tailAngles = new float[SoftPoses.TailSegments.Length];
        readonly float[] _capeAngles = new float[SoftPoses.CapeSegments.Length];
        ClassDefinition _class;
        float _time, _nextBlink = 2.5f, _blinkTime = 99f;

        public bool HasArt => _set != null && _set.Get("idle") != null;
        /// <summary>Sprite da arma mostrada agora (para testes).</summary>
        public Sprite WeaponSprite => _back != null && _back.enabled ? _back.sprite : _front != null && _front.enabled ? _front.sprite : null;
        public Sprite BodySprite => _body != null ? _body.sprite : null;

        public void Build(CharacterAnimationSet set)
        {
            if (_body != null) return;
            _set = set;
            _cape = Part("Capa");
            _tail = Part("Cauda");
            _back = Part("Arma nas costas");
            _body = Part("Corpo");
            _front = Part("Arma");
            _trim = Part("Faixa da classe");

            _tailChain = new SoftChain(SoftPoses.TailSegments);
            _capeChain = new SoftChain(SoftPoses.CapeSegments) { Stiffness = 300f, Damping = 11f };
            _tailBuffer = new PixelBuffer("ui_inventario_cauda");
            _capeBuffer = new PixelBuffer("ui_inventario_capa");
            _tailPalette = PixelBuffer.LionPalette();
            _capePalette = PixelBuffer.LionPalette();
            _tail.sprite = _tailBuffer.Sprite;
            _cape.sprite = _capeBuffer.Sprite;
        }

        /// <summary>Troca a classe mostrada (arma e cor da faixa).</summary>
        public void Show(ClassDefinition def)
        {
            if (_class == def) return;
            _class = def;
        }

        void OnEnable() { _tailChain?.Reset(); _capeChain?.Reset(); }

        Image Part(string n)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        /// <summary>Põe a imagem com o pivô do sprite em <paramref name="px"/> (pixels a partir dos pés).</summary>
        static void Put(Image img, Sprite s, Vector2 px)
        {
            img.enabled = s != null;
            if (s == null) return;
            if (img.sprite != s) img.sprite = s;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var size = s.rect.size;
            rt.sizeDelta = size;
            rt.pivot = new Vector2(s.pivot.x / size.x, s.pivot.y / size.y);
            rt.anchoredPosition = new Vector2(Mathf.Round(px.x), Mathf.Round(px.y));
        }

        void Update()
        {
            if (!HasArt) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _time += dt;

            // piscar de vez em quando, igual ao jogo
            var blink = _set.Get("idle_blink");
            if (_time >= _nextBlink) { _blinkTime = 0f; _nextBlink = _time + Random.Range(3f, 5f); }
            _blinkTime += dt;
            var frame = blink != null && _blinkTime < blink.Duration ? blink.At(_blinkTime) : _set.Get("idle").At(_time);
            if (frame == null) return;

            Put(_body, frame.sprite, Vector2.zero);

            // arma da classe
            Sprite back = null, front = null;
            var weaponBody = new Vector2(frame.body.x, frame.body.y);
            if (_class != null && _class.Class == CharacterClass.Paladin && _set.Has("hammer1_windup"))
            {
                var clip = _set.Get("hammer_back");
                if (clip != null && clip.Count > 0) back = clip.frames[0].sprite;
            }
            else if (_class != null && _class.Class != CharacterClass.Rogue) front = _class.OverlayIdle;
            Put(_back, back, weaponBody);
            Put(_front, front, Vector2.zero);

            Color trimColor = _class != null ? _class.ThemeColor : new Color(0.17f, 0.23f, 0.4f);
            trimColor.a = 1f;
            Put(_trim, frame.hasTrim ? _set.trimPixel : null, frame.trim);
            if (_trim.enabled) _trim.color = trimColor;

            // cauda e capa: mesma física do jogo, no tempo real
            float t = Time.unscaledTime;
            if (frame.hasTail)
            {
                SoftPoses.Tail(BodyMood.Idle, 1, t, 0f, _tailAngles);
                Raster(_tail, _tailChain, _tailBuffer, _tailPalette, frame.tail, _tailAngles, dt, true);
            }
            else _tail.enabled = false;

            if (frame.hasTrim)
            {
                _capePalette[SoftPal.Trim] = trimColor;
                _capePalette[SoftPal.TrimDark] = Color.Lerp(trimColor, Color.black, 0.35f);
                SoftPoses.Cape(BodyMood.Idle, 1, t, 0f, _capeAngles);
                Raster(_cape, _capeChain, _capeBuffer, _capePalette, frame.trim + CapeFromTrim, _capeAngles, dt, false);
            }
            else _cape.enabled = false;
        }

        static void Raster(Image img, SoftChain chain, PixelBuffer buffer, Color32[] palette, Vector2 anchor, float[] angles, float dt, bool tail)
        {
            chain.Step(anchor.x, anchor.y, angles, dt);
            int ox = Mathf.FloorToInt(anchor.x) - PixelBuffer.Size / 2, oy = Mathf.FloorToInt(anchor.y) - PixelBuffer.Size / 2;
            SoftRaster.Clear(buffer.Indices);
            if (tail) SoftRaster.Tail(buffer.Indices, PixelBuffer.Size, ox, oy, chain.X, chain.Y, SoftPoses.TailRadius, 1f);
            else SoftRaster.Cape(buffer.Indices, PixelBuffer.Size, ox, oy, chain.X, chain.Y, SoftPoses.CapeHalfWidth, 1);
            SoftRaster.Outline(buffer.Indices, PixelBuffer.Size);
            buffer.Upload(palette);
            Put(img, buffer.Sprite, new Vector2(ox, oy));
        }

        void OnDestroy()
        {
            _tailBuffer?.Dispose();
            _capeBuffer?.Dispose();
        }
    }
}
