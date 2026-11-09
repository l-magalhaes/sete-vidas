using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>Tela preta que escurece e clareia (morte, troca de cena). Usa tempo real: funciona com o jogo pausado.</summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Image image;

        public static ScreenFader Active { get; private set; }

        public float Alpha => image != null ? image.color.a : 0f;

        void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            SetAlpha(0f);
        }

        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        public void SetAlpha(float alpha)
        {
            if (image == null) return;
            var c = image.color;
            c.a = Mathf.Clamp01(alpha);
            image.color = c;
            image.enabled = c.a > 0.001f;
        }

        /// <summary>Clarão colorido (maldição, relâmpago): sobe até <paramref name="peak"/> e some. Depois volta a ser preto.</summary>
        public IEnumerator Flash(Color color, float peak, float up, float down)
        {
            if (image == null) yield break;
            image.color = new Color(color.r, color.g, color.b, 0f);
            yield return FadeTo(peak, up);
            yield return FadeTo(0f, down);
            image.color = new Color(0f, 0f, 0f, 0f);
            image.enabled = false;
        }

        public IEnumerator FadeTo(float alpha, float duration)
        {
            float from = Alpha;
            if (duration <= 0f) { SetAlpha(alpha); yield break; }
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, alpha, t / duration));
                yield return null;
            }
            SetAlpha(alpha);
        }
    }
}
