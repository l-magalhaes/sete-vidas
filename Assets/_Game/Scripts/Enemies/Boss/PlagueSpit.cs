using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// O Cuspe Corrompido da Isyathy (fase II): uma bola da praga que voa em arco até onde o gato estava. Encostar tira
    /// meio coração; no chão, estoura em respingos roxos e deixa uma poça de veneno (<see cref="PoisonPuddle"/>).
    /// </summary>
    public class PlagueSpit : MonoBehaviour
    {
        const float Gravity = 22f, Life = 3f;
        Vector2 _vel;
        float _age;
        int _damage;
        GameObject _source;
        static Sprite s_sprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_sprite = null; }   // Play sem recarregar o domínio

        /// <summary>A velocidade de saída para cair em <paramref name="to"/> depois de <paramref name="time"/> s.</summary>
        public static Vector2 LaunchVelocity(Vector2 from, Vector2 to, float time)
        {
            time = Mathf.Max(0.1f, time);
            Vector2 d = to - from;
            return new Vector2(d.x / time, (d.y + 0.5f * Gravity * time * time) / time);
        }

        public static PlagueSpit Fire(Vector2 from, Vector2 to, float time, int damage, GameObject source)
        {
            var go = new GameObject("Cuspe da praga");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Glob();
            sr.sortingOrder = 7;
            var spit = go.AddComponent<PlagueSpit>();
            spit._vel = LaunchVelocity(from, to, time);
            spit._damage = damage;
            spit._source = source;
            return spit;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _vel.y -= Gravity * dt;
            Vector2 p = (Vector2)transform.position + _vel * dt;
            transform.position = p;
            transform.Rotate(0f, 0f, 540f * dt);
            if (Random.value < dt * 20f) HitSparks.One(p, -_vel * 0.05f, new Color32(0x8a, 0x3f, 0xa8, 0xff), 0.3f, 0f);

            var h = PlayerHealth.Active;
            if (h != null && !h.IsDead)
            {
                Vector2 cat = (Vector2)h.transform.position + Vector2.up * 0.7f;
                if (Vector2.Distance(cat, p) < 0.65f)
                {
                    h.TakeDamage(new DamageInfo(_damage, new Vector2(Mathf.Sign(_vel.x), 0.3f), DamageType.Normal, AttackAngle.Front, _source));
                    Splat(p, false);
                    return;
                }
            }
            if (_age > Life) Splat(p, false);
            else
            {
                var ground = Physics2D.OverlapPoint(p, LayerMask.GetMask(GameConstants.Layers.Ground));
                if (ground != null && !IsyathySolid.Is(ground)) Splat(p, true);   // o corpo dela não é chão
            }
        }

        void Splat(Vector2 p, bool ground)
        {
            HitSparks.Burst(p, 10, new Color32(0xc4, 0x7b, 0xff, 0xff), new Color32(0x4a, 0x1d, 0x5c, 0xff), 2f, 5f, 0.4f, 20f, 0.6f);
            if (ground) { PoisonPuddle.Spawn(p, _source); SeteVidas.Audio.Sfx.Play("isyathy_poca", p); }   // a poça de veneno no chão (no lugar da nuvem)
            Destroy(gameObject);
        }

        /// <summary>A bola da praga (10 × 10): roxa, com o miolo aceso.</summary>
        static Sprite Glob()
        {
            if (s_sprite != null) return s_sprite;
            const int n = 10;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - 4.5f, dy = y - 4.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = d > 4.6f ? new Color32(0, 0, 0, 0) : d > 3.6f ? new Color32(0x24, 0x0e, 0x2c, 0xff)
                        : d > 2.2f ? new Color32(0x6a, 0x26, 0x8c, 0xff) : dx < 0 && dy > 0 ? new Color32(0xf0, 0xc9, 0xff, 0xff) : new Color32(0xc4, 0x7b, 0xff, 0xff);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "cuspe_praga", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
            s_sprite.hideFlags = HideFlags.DontSave;
            return s_sprite;
        }
    }
}
