using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// A poça de veneno da praga (pedido do usuário, 17/11/2026: no lugar das nuvens de esporos, um projétil que deixa uma
    /// poça no chão). Nasce onde o cuspe da Isyathy cai (<see cref="PlagueSpit"/>), em cima do chão ou da plataforma
    /// embaixo; borbulha por <see cref="Life"/> s e fere o gato que pisar (meio coração, com um intervalo). Some
    /// piscando: no fim já não fere.
    /// </summary>
    public class PoisonPuddle : MonoBehaviour
    {
        public const float Life = 5.5f, HalfWidth = 1.3f, Height = 0.7f, HurtEvery = 1f;
        const float FadeIn = 0.25f, FadeOut = 0.8f;

        float _age, _nextHurt;
        SpriteRenderer _sr;
        GameObject _source;
        static Sprite[] s_frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_frames = null; }   // Play sem recarregar o domínio

        /// <summary>A poça em cima da superfície embaixo de <paramref name="at"/> (até 4 tiles para baixo).</summary>
        public static PoisonPuddle Spawn(Vector2 at, GameObject source)
        {
            Vector2 p = at;
            foreach (var hit in Physics2D.RaycastAll(at + Vector2.up * 0.4f, Vector2.down, 4.5f, LayerMask.GetMask(GameConstants.Layers.Ground)))
                if (!IsyathySolid.Is(hit.collider)) { p = hit.point; break; }   // o corpo dela não é chão
            var go = new GameObject("Poça de veneno");
            go.transform.position = p;
            var puddle = go.AddComponent<PoisonPuddle>();
            puddle._source = source;
            puddle._sr = go.AddComponent<SpriteRenderer>();
            puddle._sr.sprite = Frames()[0];
            puddle._sr.sortingOrder = 4;
            return puddle;
        }

        /// <summary>A poça fere agora (nem nascendo, nem sumindo).</summary>
        public static bool Hurts(float age) => age >= FadeIn && age <= Life - FadeOut * 0.5f;

        /// <summary>Os pés do gato estão dentro da poça (centro <paramref name="center"/>, na superfície).</summary>
        public static bool Inside(Vector2 center, Vector2 feet) => Mathf.Abs(feet.x - center.x) <= HalfWidth && feet.y >= center.y - 0.2f && feet.y <= center.y + Height;

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= Life) { Destroy(gameObject); return; }
            // borbulha (3 quadros) e some piscando
            var frames = Frames();
            _sr.sprite = frames[(int)(_age * 6f) % frames.Length];
            float a = _age < FadeIn ? _age / FadeIn : _age > Life - FadeOut ? (((int)(_age * 16f) & 1) == 0 ? 0.35f : 0.8f) : 1f;
            _sr.color = new Color(1f, 1f, 1f, a);
            float grow = Mathf.Min(1f, _age / FadeIn);
            transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1f, grow), 1f, 1f);
            if (Random.value < Time.deltaTime * 5f)
                HitSparks.One((Vector2)transform.position + new Vector2(Random.Range(-HalfWidth, HalfWidth) * 0.8f, 0.25f), new Vector2(0f, Random.Range(1f, 2.2f)), new Color32(0x9a, 0xe0, 0x5a, 0xff), 0.5f, 0f);

            if (!Hurts(_age) || Time.time < _nextHurt) return;
            var h = PlayerHealth.Active;
            if (h == null || h.IsDead) return;
            if (!Inside(transform.position, h.transform.position)) return;
            _nextHurt = Time.time + HurtEvery;
            h.TakeDamage(new DamageInfo(IsyathyBossRules.PuddleDamage, new Vector2(Mathf.Sign(h.transform.position.x - transform.position.x + 0.01f), 0.6f), DamageType.Normal, AttackAngle.Front, _source));
        }

        /// <summary>A poça (44 × 8 px, o pivô na superfície): roxa por fora, verde-praga por dentro, com bolhas que estouram.</summary>
        static Sprite[] Frames()
        {
            if (s_frames != null) return s_frames;
            const int w = 44, h = 8;
            s_frames = new Sprite[3];
            var rim = new Color32(0x3a, 0x14, 0x46, 0xff);
            var deep = new Color32(0x6a, 0x26, 0x8c, 0xff);
            var goo = new Color32(0x7c, 0xb8, 0x3c, 0xff);
            var lit = new Color32(0xc4, 0xf0, 0x7a, 0xff);
            var bubble = new Color32(0xe6, 0xff, 0xb0, 0xff);
            for (int f = 0; f < 3; f++)
            {
                var px = new Color32[w * h];
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float k = Mathf.Abs(x - (w - 1) * 0.5f) / ((w - 1) * 0.5f);
                        int top = k > 0.92f ? 0 : k > 0.75f ? 1 : k > 0.45f ? 2 : 3;   // a poça é mais alta no meio
                        if (y > top) continue;
                        bool edge = y == top || k > 0.9f;
                        px[y * w + x] = edge ? rim : (y == 0 ? deep : ((x + y + f) % 7 == 0 ? lit : goo));
                    }
                // bolhas subindo (cada quadro numa altura)
                int[] bx = { 9, 20, 31 };
                for (int i = 0; i < bx.Length; i++)
                {
                    int by = 4 + (f + i) % 3;
                    if (by < h) { px[by * w + bx[i]] = bubble; if ((f + i) % 3 == 1 && by + 1 < h) px[(by + 1) * w + bx[i]] = goo; }
                }
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "poca_veneno_" + f, hideFlags = HideFlags.DontSave };
                tex.SetPixels32(px);
                tex.Apply(false);
                s_frames[f] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f / h), 16f);
                s_frames[f].hideFlags = HideFlags.DontSave;
            }
            return s_frames;
        }
    }
}
