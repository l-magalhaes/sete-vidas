using System.Collections;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Isyathy vista de longe (Docs/Level/floresta-mapa.md §6, F5). Pedidos do usuário (14/11/2026): ela aparece LÁ
    /// ATRÁS, no fundo, entre as árvores e a névoa, com o paralaxe, numa animação curta, e depois some. Nunca fica
    /// parada à vista. Sem colisão; nunca ataca. A fala do Lion, uma vez por jogo (flag "fala_" + chave).
    /// <para>
    /// Melhorias (14/11/2026): o pescoço que espiava e o corpo que atravessava saíram (o usuário não gostou). Ficam SÓ
    /// OS OLHOS, e aparecendo bastante: acendem entre as árvores, piscam, a cabeça mexe de leve (às vezes seguindo o
    /// gato), às vezes estreitam, e somem. No lago, os olhos somem se o gato agacha no chão.
    /// </para>
    /// Depois que ela morre (flag <see cref="DeadFlag"/>), não aparece mais: no lugar vem o espírito (F8).
    /// </summary>
    public class IsyathySighting : MonoBehaviour
    {
        /// <summary>O jeito da sala (mapa §6). Hoje todas mostram os olhos; o tipo muda o tamanho e o lago.</summary>
        public enum Kind { Eyes, Silhouette, Lake, Scales, Coiled, Random }

        /// <summary>Flag do save: a Isyathy foi morta (F8). Os olhos doentes não aparecem mais.</summary>
        public const string DeadFlag = "isyathy_morta";

        /// <summary>Quadros dos olhos (isy_olhos_0..2): abertos, meio, fechados.</summary>
        public const int EyesOpen = 0, EyesHalf = 1, EyesClosed = 2;

        /// <summary>Espera antes da primeira aparição numa sala (s): logo.</summary>
        public static readonly Vector2 FirstDelay = new Vector2(0.6f, 1.8f);
        /// <summary>Intervalo entre aparições (s): curto, para os olhos aparecerem bastante.</summary>
        public static readonly Vector2 Gap = new Vector2(1.8f, 4.5f);

        [SerializeField] Kind kind;
        [SerializeField] SpriteRenderer art;
        [Tooltip("Olhos: aberto, meio, fechado")]
        [SerializeField] Sprite[] eyeFrames = new Sprite[0];
        [Tooltip("A sala (mundo): só aparece com o gato dentro")]
        [SerializeField] Rect room;
        [Tooltip("Tamanho (está longe)")]
        [SerializeField, Min(0.1f)] float scale = 0.6f;
        [SerializeField, Range(0f, 1f)] float maxAlpha = 0.9f;
        [Tooltip("Paralaxe (0 = preso ao mundo, 1 = preso à câmera). No fundo, entre as copas de longe e as árvores médias: ~0,08")]
        [SerializeField, Range(0f, 1f)] float parallax = 0.08f;
        [SerializeField] string barkKey = "";
        [SerializeField] string[] barkLines = new string[0];

        float _alpha;
        Vector2 _base;   // onde o desenho fica com a câmera bem em cima dele (o paralaxe desloca a partir daqui)
        bool _visible;

        public Kind Type => kind;
        public Rect Room => room;

        public void Configure(Kind newKind, SpriteRenderer renderer, Sprite[] eyes, Rect area,
                              float newScale, float alpha, float parallaxFactor, string key, string[] lines)
        {
            kind = newKind; art = renderer; eyeFrames = eyes; room = area;
            scale = newScale; maxAlpha = alpha; parallax = parallaxFactor; barkKey = key; barkLines = lines;
        }

        // ------------------------------------------------------------ regras (testadas no EditMode)

        /// <summary>Pode aparecer? Nunca depois da morte (aí é o espírito, F8).</summary>
        public static bool CanAppear(bool dead) => !dead;

        /// <summary>O olhar do lago some quando o gato agacha no chão (se esconde).</summary>
        public static bool LakeVanishes(bool crouching, bool grounded) => crouching && grounded;

        /// <summary>Posição com paralaxe: o desenho acompanha a câmera uma fração <paramref name="factor"/> (camadas do fundo).</summary>
        public static Vector2 Parallax(Vector2 basePos, Vector2 cam, float factor) => basePos + (cam - basePos) * factor;

        /// <summary>
        /// Onde aparecer, a partir da câmera: de um dos lados (nunca no meio, onde está o gato), a 35–75% da meia largura,
        /// um pouco acima do meio da tela (a altura das árvores do fundo). <paramref name="side"/> −1/1, <paramref name="t"/> 0..1.
        /// </summary>
        public static Vector2 SpotFromCamera(Vector2 cam, float halfWidth, float halfHeight, int side, float t)
        {
            float x = cam.x + side * halfWidth * Mathf.Lerp(0.35f, 0.75f, t);
            float y = cam.y + halfHeight * Mathf.Lerp(0.05f, 0.35f, 1f - t);
            return new Vector2(x, y);
        }

        // ------------------------------------------------------------ jogo

        void Start()
        {
            bool dead = SaveManager.Data != null && SaveManager.Data.HasFlag(DeadFlag);
            if (!CanAppear(dead) || art == null || eyeFrames == null || eyeFrames.Length == 0) { gameObject.SetActive(false); return; }
            _alpha = 0f;
            Apply();
            StartCoroutine(Loop());
        }

        void LateUpdate()
        {
            if (art == null || !_visible) return;
            var cam = Camera.main;
            if (cam == null) return;
            var p = Parallax(_base, cam.transform.position, parallax);
            art.transform.position = new Vector3(p.x, p.y, art.transform.position.z);
        }

        bool PlayerInside()
        {
            var h = PlayerHealth.Active;
            return h != null && !h.IsDead && room.Contains((Vector2)h.transform.position + Vector2.up * 0.5f);
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(Random.Range(FirstDelay.x, FirstDelay.y));
            while (true)
            {
                while (!PlayerInside()) yield return new WaitForSeconds(0.3f);
                yield return Appear();
                yield return new WaitForSeconds(Random.Range(Gap.x, Gap.y));
            }
        }

        IEnumerator Appear()
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            // um pouco maior que o pescoço de antes (só os olhos precisam ler entre as árvores), com variação de distância
            float s = scale * Random.Range(0.85f, 1.15f);
            art.color = new Color(0.9f, 0.9f, 0.85f, 0f);   // pouco escurecidos: precisam ser vistos no fundo
            art.transform.localScale = new Vector3(s, s, 1f);
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            int side = Random.value < 0.5f ? -1 : 1;
            art.flipX = Random.value < 0.5f;
            _base = SpotFromCamera(cam.transform.position, halfW, halfH, side, Random.value);
            _visible = true;
            StartCoroutine(Bark(1.2f));
            yield return EyesAnim(side);
            _alpha = 0f; Apply();
            _visible = false;
        }

        /// <summary>
        /// Olhos: acendem já fechados e abrem devagar, piscam, a cabeça mexe de leve (às vezes acompanhando o gato),
        /// às vezes estreitam desconfiados, piscam de novo e fecham ao sumir.
        /// </summary>
        IEnumerator EyesAnim(int side)
        {
            SetFrame(EyesClosed);
            yield return Fade(maxAlpha, 0.45f);
            SetFrame(EyesHalf); yield return new WaitForSeconds(0.14f);
            SetFrame(EyesOpen);
            bool vanished = false, follows = Random.value < 0.5f;
            float t = 0f, stay = Random.Range(2.4f, 4.6f), nextBlink = Random.Range(0.6f, 1.2f);
            float narrowAt = Random.value < 0.4f ? Random.Range(1f, stay - 0.8f) : -1f;
            var pc = PlayerHealth.Active != null ? PlayerHealth.Active.GetComponent<PlayerController>() : null;
            var home = _base;
            float phase = Random.value * 6f;
            while (t < stay)
            {
                t += Time.deltaTime;
                if (kind == Kind.Lake && pc != null && LakeVanishes(pc.IsCrouching, pc.IsGrounded)) { vanished = true; break; }
                var sway = new Vector2(Mathf.Sin(phase + t * 0.9f) * 0.25f, Mathf.Sin(phase + t * 1.7f) * 0.08f);   // a cabeça mexe de leve
                if (follows && pc != null)   // vira devagar para o lado do gato
                    sway.x += Mathf.Clamp((pc.transform.position.x - home.x) * 0.04f, -0.5f, 0.5f) * Mathf.Clamp01(t / 1.5f);
                _base = home + sway;
                if (narrowAt > 0f && t >= narrowAt)
                {
                    narrowAt = -1f;   // estreita os olhos (meio fechado), desconfiada, e abre de novo
                    SetFrame(EyesHalf); yield return new WaitForSeconds(Random.Range(0.5f, 0.9f));
                    SetFrame(EyesOpen);
                }
                else if (t >= nextBlink)
                {
                    nextBlink = t + Random.Range(0.9f, 1.8f);
                    yield return Blink();
                    if (Random.value < 0.3f) yield return Blink();   // piscada dupla
                }
                yield return null;
            }
            SetFrame(EyesHalf); yield return new WaitForSeconds(0.1f); SetFrame(EyesClosed);
            yield return Fade(0f, vanished ? 0.2f : 0.5f);
        }

        IEnumerator Blink()
        {
            SetFrame(EyesHalf); yield return new WaitForSeconds(0.06f);
            SetFrame(EyesClosed); yield return new WaitForSeconds(0.11f);
            SetFrame(EyesHalf); yield return new WaitForSeconds(0.06f);
            SetFrame(EyesOpen);
        }

        IEnumerator Fade(float target, float seconds)
        {
            float start = _alpha, t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _alpha = Mathf.Lerp(start, target, t / seconds);
                Apply();
                yield return null;
            }
            _alpha = target; Apply();
        }

        IEnumerator Bark(float delay)
        {
            var data = SaveManager.Data;
            string flag = "fala_" + barkKey;
            if (data == null || string.IsNullOrEmpty(barkKey) || barkLines == null || barkLines.Length == 0 || data.HasFlag(flag)) yield break;
            yield return new WaitForSeconds(delay);
            data.SetFlag(flag);
            foreach (var line in barkLines)
            {
                var h = PlayerHealth.Active;
                if (h == null || h.IsDead) yield break;
                SpeechBubble.Show(h.transform, line, 2.6f);
                yield return new WaitForSeconds(2.6f);
            }
        }

        void SetFrame(int i)
        {
            if (art != null && eyeFrames != null && eyeFrames.Length > 0) art.sprite = eyeFrames[Mathf.Clamp(i, 0, eyeFrames.Length - 1)];
        }

        void Apply()
        {
            if (art == null) return;
            var c = art.color; c.a = _alpha; art.color = c;
            art.enabled = _alpha > 0.001f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.56f, 0.82f, 0.31f, 0.6f);
            Gizmos.DrawWireCube(room.center, room.size);
        }
    }
}
