using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Efeitos do dash (melhorias 14/11/2026, pedido do usuário: "como o Hollow Knight, um impulso"):
    /// <list type="bullet">
    /// <item><b>Rastro de vento</b>: faixas brancas em pixel que nascem atrás do gato, esticam enquanto ele avança e,
    /// quando o dash acaba, ficam no ar e somem se desfiando.</item>
    /// <item><b>Imagens-fantasma</b>: cópias claras do desenho do gato que ficam para trás e apagam rápido.</item>
    /// </list>
    /// Tudo gerado em código (sem PNG). O desenho do rastro é feito uma vez e reaproveitado.
    /// </summary>
    public class DashFx : MonoBehaviour
    {
        const int TexW = 56, TexH = 15;
        const float Ppu = 16f;

        static Sprite s_streak;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_streak = null; // Play sem recarregar o domínio

        SpriteRenderer _sr;
        Transform _follow;
        Vector3 _offset;
        float _age, _grow, _fade = 0.28f;
        bool _released;
        float _startAlpha;
        Vector3 _releasedScale;

        /// <summary>
        /// Rastro de vento atrás do gato: segue <paramref name="follow"/> (o corpo) até <see cref="Release"/>.
        /// <paramref name="dir"/> = para onde ele vai; <paramref name="height"/> = altura do centro do corpo (mundo).
        /// </summary>
        public static DashFx Streak(Transform follow, int dir, float height, int sortingLayerId, int order, float growTime)
        {
            var go = new GameObject("Dash: rastro de vento");
            var fx = go.AddComponent<DashFx>();
            fx._sr = go.AddComponent<SpriteRenderer>();
            fx._sr.sprite = StreakSprite();
            fx._sr.sortingLayerID = sortingLayerId;
            fx._sr.sortingOrder = order;
            fx._sr.color = new Color(0.92f, 0.96f, 1f, 0.9f);
            fx._startAlpha = 0.9f;
            fx._follow = follow;
            // a ponta do rastro (pivot) fica no meio do corpo; o resto se estende para trás
            fx._offset = new Vector3(dir * 0.15f, height, 0f);
            fx._grow = Mathf.Max(0.05f, growTime);
            go.transform.localScale = new Vector3(dir * 0.25f, 1f, 1f);
            go.transform.position = follow.position + fx._offset;
            return fx;
        }

        /// <summary>Cópia clara do desenho atual, deixada para trás.</summary>
        public static void Ghost(SpriteRenderer source, float alpha = 0.5f, float life = 0.18f)
        {
            if (source == null || source.sprite == null) return;
            var go = new GameObject("Dash: imagem-fantasma");
            go.transform.position = source.transform.position;
            go.transform.localScale = source.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.flipX = source.flipX;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;
            sr.color = new Color(0.8f, 0.9f, 1f, alpha);
            var fx = go.AddComponent<DashFx>();
            fx._sr = sr;
            fx._startAlpha = alpha;
            fx._fade = life;
            fx._released = true;
            fx._releasedScale = go.transform.localScale;
        }

        /// <summary>O dash acabou: o rastro fica onde está e some, esticando um pouco para trás.</summary>
        public void Release()
        {
            if (_released) return;
            _released = true;
            _follow = null;
            _age = 0f;
            _releasedScale = transform.localScale;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (!_released)
            {
                if (_follow == null || _age > 0.6f) { Release(); return; }   // segurança: morreu ou trocou de cena no meio
                transform.position = _follow.position + _offset;
                // estica até o tamanho inteiro enquanto o gato avança
                float k = Mathf.Clamp01(_age / _grow);
                float sx = Mathf.Lerp(0.25f, 1f, 1f - (1f - k) * (1f - k));
                transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * sx, 1f, 1f);
                return;
            }
            float t = Mathf.Clamp01(_age / _fade);
            var c = _sr.color;
            c.a = _startAlpha * (1f - t) * (1f - t);
            _sr.color = c;
            // desfia: fica mais comprido e mais fino ao sumir
            transform.localScale = new Vector3(_releasedScale.x * (1f + 0.35f * t), _releasedScale.y * (1f - 0.45f * t), 1f);
            if (t >= 1f) Destroy(gameObject);
        }

        /// <summary>
        /// O desenho do rastro (virado para a direita, pivot na ponta da direita): faixas horizontais de comprimentos
        /// diferentes, fortes perto do gato e apagando para trás, com uma faixa central larga e clara.
        /// </summary>
        static Sprite StreakSprite()
        {
            if (s_streak != null) return s_streak;
            var px = new Color32[TexW * TexH];
            // (linha, comprimento, força): o miolo é largo e forte; as bordas, fios curtos
            int[,] lines =
            {
                { 7, 56, 255 }, { 6, 50, 230 }, { 8, 52, 230 }, { 5, 40, 190 }, { 9, 44, 190 },
                { 4, 30, 150 }, { 10, 34, 150 }, { 3, 22, 120 }, { 11, 18, 120 }, { 2, 12, 90 },
                { 12, 26, 90 }, { 1, 8, 70 }, { 13, 10, 70 },
            };
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                int y = lines[i, 0], len = lines[i, 1], strength = lines[i, 2];
                // cada fio começa um pouco atrás da ponta (o miolo encosta no gato)
                int start = TexW - 1 - Mathf.Abs(y - 7) * 2;
                for (int k = 0; k < len; k++)
                {
                    int x = start - k;
                    if (x < 0) break;
                    float fade = 1f - (float)k / len;                    // apaga para trás
                    if (k > len * 0.55f && ((x + y) & 1) == 0) continue; // no fim, o fio se desfaz em pontinhos
                    byte a = (byte)Mathf.Clamp(strength * fade * fade + (k < 3 ? 30 : 0), 0, 255);
                    var old = px[y * TexW + x];
                    if (a > old.a) px[y * TexW + x] = new Color32(255, 255, 255, a);
                }
            }
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "fx_dash_vento" };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_streak = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), new Vector2(1f, 0.5f), Ppu);
            return s_streak;
        }
    }
}
