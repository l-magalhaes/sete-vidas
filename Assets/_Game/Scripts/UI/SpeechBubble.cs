using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.UI
{
    /// <summary>
    /// Balão curto em pixel sobre a cabeça de alguém (falas curtas do Lion, Docs/Narrativa/floresta-roteiro.md).
    /// Não para o jogo: segue quem fala por alguns segundos e some piscando. Um balão por pessoa.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        const float BlinkTime = 0.3f;
        static readonly Color Cream = new Color32(0xec, 0xdc, 0xb0, 0xff);

        Transform _who;
        SpriteRenderer _text;
        float _age, _life;
        float _height;

        /// <summary>Mostra <paramref name="text"/> sobre <paramref name="who"/> por <paramref name="seconds"/>.</summary>
        public static SpeechBubble Show(Transform who, string text, float seconds = 2f, float height = 1.9f)
        {
            if (!Application.isPlaying || who == null || string.IsNullOrEmpty(text)) return null;
            var old = who.GetComponentInChildren<SpeechBubble>();
            if (old != null) Destroy(old.gameObject);

            var go = new GameObject("Fala " + text);
            go.transform.SetParent(who, false);
            var bubble = go.AddComponent<SpeechBubble>();
            bubble._who = who;
            bubble._life = Mathf.Max(0.5f, seconds);
            bubble._height = height;
            var t = new GameObject("Texto").AddComponent<SpriteRenderer>();
            t.transform.SetParent(go.transform, false);
            t.sprite = PixelFont.Render(text, Cream, PixelFont.Outline);
            t.sortingOrder = 55;
            bubble._text = t;
            bubble.Place();
            return bubble;
        }

        void Place()
        {
            // fica reto mesmo quando quem fala vira de lado (flip por escala negativa)
            var s = _who.lossyScale;
            transform.localScale = new Vector3(s.x < 0f ? -1f : 1f, 1f, 1f);
            transform.position = PixelFont.Snap(_who.position + Vector3.up * _height);
        }

        void LateUpdate()
        {
            if (_who == null) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            Place();
            _text.enabled = _age < _life - BlinkTime || ((int)((_age - (_life - BlinkTime)) * 20f) & 1) == 0;
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
