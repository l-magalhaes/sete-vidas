using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// Um relâmpago: primeiro uma coluna clara piscando cada vez mais forte (o aviso, onde ele VAI cair);
    /// depois o raio cai e machuca quem estiver na coluna. Some sozinho.
    /// </summary>
    public class LightningStrike : MonoBehaviour
    {
        [SerializeField] SpriteRenderer telegraph;
        [SerializeField] SpriteRenderer bolt;
        [SerializeField] LayerMask playerLayers;

        float _width, _height, _telegraphTime, _strikeTime, _age;
        int _damage;
        bool _struck, _hit;
        GameObject _source;

        public bool HasStruck => _struck;
        public float Width => _width;

        /// <param name="floor">Pés da coluna (chão da arena).</param>
        public void Init(Vector2 floor, float width, float height, float telegraphTime, float strikeTime, int damage, GameObject source)
        {
            transform.position = floor;
            _width = width;
            _height = height;
            _telegraphTime = telegraphTime;
            _strikeTime = strikeTime;
            _damage = damage;
            _source = source;
            if (playerLayers == 0) playerLayers = LayerMask.GetMask(Core.GameConstants.Layers.Player);
            Audio.Sfx.Play("raio_aviso", floor + Vector2.up);

            if (telegraph != null)
            {
                telegraph.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                telegraph.transform.localScale = new Vector3(width, height, 1f); // sprite 1×1 tile
            }
            if (bolt != null)
            {
                bolt.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                bolt.transform.localScale = new Vector3(width, height / 4f, 1f); // sprite 1×4 tiles
                bolt.enabled = false;
            }
        }

        void Update()
        {
            _age += Time.deltaTime;

            if (!_struck)
            {
                // Aviso: pisca mais rápido e mais forte perto do raio
                float k = Mathf.Clamp01(_age / _telegraphTime);
                if (telegraph != null)
                {
                    float blink = ((int)(_age * Mathf.Lerp(6f, 20f, k)) & 1) == 0 ? 1f : 0.6f;
                    var c = telegraph.color;
                    c.a = Mathf.Lerp(0.12f, 0.45f, k) * blink;
                    telegraph.color = c;
                }
                if (_age >= _telegraphTime) Strike();
                return;
            }

            if (!_hit) TryHit();
            if (_age >= _telegraphTime + _strikeTime) Destroy(gameObject);
        }

        void Strike()
        {
            _struck = true;
            Audio.Sfx.Play("raio", (Vector2)transform.position + Vector2.up);
            if (telegraph != null) telegraph.enabled = false;
            if (bolt != null) bolt.enabled = true;
            TryHit();
        }

        void TryHit()
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * (_height * 0.5f);
            var col = Physics2D.OverlapBox(center, new Vector2(_width * 0.9f, _height), 0f, playerLayers);
            if (col == null) return;
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null) return;
            float dir = col.transform.position.x >= transform.position.x ? 1f : -1f;
            target.TakeDamage(new DamageInfo(_damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, _source));
            _hit = true;
        }
    }
}
