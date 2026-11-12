using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Melhoria 12: a vida da tela inicial. As camadas do fundo andam devagar (paralaxe automática, repetindo a cada
    /// <see cref="Layer.Width"/> px), a cauda do gato balança (quadros em vai e vem), as sete centelhas em volta do logo
    /// cintilam e uma delas se apaga de vez em quando (a maldição), e a tela entra a partir do preto.
    /// Tudo em pixel inteiro (a interface tem 480×270 de referência).
    /// </summary>
    public class TitleScreenFx : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            [Tooltip("Duas cópias lado a lado (a segunda logo à direita da primeira)")]
            public RectTransform A, B;
            [Tooltip("Largura do desenho (px)")] public float Width = 960f;
            [Tooltip("Velocidade (px/s)")] public float Speed = 2f;
            [Tooltip("Deslocamento inicial (px): onde a camada começa (a Cidadela na frente do sol)")] public float Start;
            [HideInInspector] public float Offset;
        }

        [SerializeField] Layer[] layers = new Layer[0];
        [SerializeField] Image cat;
        [SerializeField] Sprite[] catFrames = new Sprite[0];
        [SerializeField, Min(0.05f)] float catFrameTime = 0.35f;
        [SerializeField] Image[] sparks = new Image[0];
        [Tooltip("De quanto em quanto tempo uma centelha se apaga (s)")]
        [SerializeField] Vector2 snuffInterval = new Vector2(5f, 9f);
        [SerializeField] Image fade;
        [SerializeField, Min(0.01f)] float fadeTime = 0.8f;

        float _t, _nextSnuff, _snuffStart = -99f;
        int _snuffed = -1;

        void Start()
        {
            _nextSnuff = Random.Range(snuffInterval.x, snuffInterval.y);
            foreach (var l in layers) if (l != null) l.Offset = l.Start;
            if (fade != null) { fade.gameObject.SetActive(true); fade.color = new Color(0f, 0f, 0f, 1f); }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;

            foreach (var l in layers)
            {
                if (l == null || l.A == null) continue;
                l.Offset = Mathf.Repeat(l.Offset + l.Speed * dt, l.Width);
                float x = -Mathf.Floor(l.Offset);   // pixel inteiro
                l.A.anchoredPosition = new Vector2(x, l.A.anchoredPosition.y);
                if (l.B != null) l.B.anchoredPosition = new Vector2(x + l.Width, l.B.anchoredPosition.y);
            }

            // cauda: 0 1 2 1 0 ...
            if (cat != null && catFrames.Length > 0)
            {
                int n = catFrames.Length, cycle = Mathf.Max(1, 2 * n - 2);
                int k = (int)(_t / catFrameTime) % cycle;
                int frame = k < n ? k : cycle - k;
                cat.sprite = catFrames[Mathf.Clamp(frame, 0, n - 1)];
            }

            // centelhas: cintilam fora de tempo; uma se apaga por um instante e volta devagar
            if (_t >= _nextSnuff && sparks.Length > 0)
            {
                _snuffed = Random.Range(0, sparks.Length);
                _snuffStart = _t;
                _nextSnuff = _t + Random.Range(snuffInterval.x, snuffInterval.y);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] == null) continue;
                float a = 0.65f + 0.35f * Mathf.Sin(_t * 2.3f + i * 1.7f);
                if (i == _snuffed)
                {
                    float since = _t - _snuffStart;
                    a *= since < 0.6f ? 0f : Mathf.Clamp01((since - 0.6f) / 1.2f); // apaga, depois reacende
                }
                var c = sparks[i].color; c.a = a; sparks[i].color = c;
            }

            if (fade != null && fade.gameObject.activeSelf)
            {
                var c = fade.color; c.a = Mathf.Clamp01(1f - _t / fadeTime); fade.color = c;
                if (c.a <= 0f) fade.gameObject.SetActive(false);
            }
        }
    }
}
