using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// O amanhecer da cabeceira leste (18/11/2026, pedido do usuário: "ao sair para o topo depois do chefe, estar
    /// amanhecendo no fundo"): uma faixa de luz quente subindo do horizonte (laranja, rosa, violeta e some em cima, onde
    /// as estrelas continuam), entre o céu de estrelas (-100) e as montanhas (-95). Segue a câmera e acende conforme ela
    /// passa de <see cref="fromX"/> para <see cref="toX"/> (a ponte → a R5): o amanhecer chega com o gato.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class DawnSky : MonoBehaviour
    {
        public const int Order = -98;

        [SerializeField] float fromX, toX = 1f;

        static Sprite s_gradient;
        SpriteRenderer _sr;
        Camera _cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_gradient = null; }   // Play sem recarregar o domínio

        public void Configure(float from, float to) { fromX = from; toX = to; }

        /// <summary>Quanto do amanhecer aparece com a câmera em <paramref name="x"/> (0..1, suave).</summary>
        public static float Amount(float x, float from, float to)
        {
            if (to <= from) return x >= from ? 1f : 0f;
            float t = Mathf.Clamp01((x - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        /// <summary>A cor da faixa na altura <paramref name="t"/> (0 = o pé da tela, 1 = o alto), com a transparência.</summary>
        public static Color32 At(float t)
        {
            Color low = new Color(0.96f, 0.66f, 0.38f), mid = new Color(0.86f, 0.42f, 0.5f), high = new Color(0.45f, 0.3f, 0.6f);
            Color c = t < 0.35f ? Color.Lerp(low, mid, t / 0.35f) : Color.Lerp(mid, high, Mathf.Clamp01((t - 0.35f) / 0.4f));
            c.a = t < 0.3f ? 0.9f : Mathf.Lerp(0.9f, 0f, Mathf.Clamp01((t - 0.3f) / 0.5f));
            return c;
        }

        static Sprite Gradient()
        {
            if (s_gradient != null) return s_gradient;
            const int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "amanhecer", hideFlags = HideFlags.DontSave };
            var px = new Color32[h];
            // em faixas de 4 px (o degradê em degraus, do jeito do pixel art; sem pontilhado)
            for (int y = 0; y < h; y++) px[y] = At((y / 4 * 4) / (float)(h - 1));
            tex.SetPixels32(px);
            tex.Apply(false);
            s_gradient = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 1f);
            s_gradient.hideFlags = HideFlags.DontSave;
            return s_gradient;
        }

        void Awake()
        {
            var go = new GameObject("Faixa do amanhecer");
            go.transform.SetParent(transform, false);
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = Gradient();
            _sr.sortingOrder = Order;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || _sr == null) return;
            float a = Amount(_cam.transform.position.x, fromX, toX);
            _sr.enabled = a > 0.01f;
            if (!_sr.enabled) return;
            float hh = _cam.orthographicSize * 2f, ww = hh * _cam.aspect;
            _sr.transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0f);
            _sr.transform.localScale = new Vector3(ww + 2f, hh / 64f, 1f);
            _sr.color = new Color(1f, 1f, 1f, a);
        }
    }
}
