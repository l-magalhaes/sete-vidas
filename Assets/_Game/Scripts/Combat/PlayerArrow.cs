using System.Collections.Generic;
using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Flecha do arco do Caçador (F4b): voa reta, acerta o primeiro inimigo (layer Enemy) no caminho, crava na parede e
    /// some. Alcance de 12 tiles. Feita na hora (sprite gerado), sem prefab; some ao reiniciar o mundo.
    /// </summary>
    public class PlayerArrow : MonoBehaviour
    {
        const float StuckTime = 0.5f;

        Vector2 _dir;
        float _speed, _left, _stuckFor = -1f;
        int _damage;
        GameObject _source;
        SpriteRenderer _sprite;

        static readonly List<PlayerArrow> s_flying = new List<PlayerArrow>();
        static Sprite s_sprite;
        static int s_wallMask = -1, s_enemyMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_flying.Clear(); s_sprite = null; s_wallMask = s_enemyMask = -1; } // Play sem recarregar o domínio

        /// <summary>Flechas do gato ainda voando (uma de cada vez).</summary>
        public static int InFlight
        {
            get
            {
                s_flying.RemoveAll(a => a == null || a._stuckFor >= 0f);
                return s_flying.Count;
            }
        }

        /// <summary>Atira uma flecha.</summary>
        public static PlayerArrow Fire(Vector2 from, Vector2 dir, float speed, float range, int damage, GameObject source)
        {
            var go = new GameObject("Flecha");
            go.transform.position = from;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var a = go.AddComponent<PlayerArrow>();
            a._sprite = go.AddComponent<SpriteRenderer>();
            a._sprite.sprite = ArrowSprite();
            a._sprite.sortingOrder = 6;
            a._dir = dir.normalized;
            a._speed = speed;
            a._left = range;
            a._damage = damage;
            a._source = source;
            if (s_wallMask < 0)
            {
                s_wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
                s_enemyMask = LayerMask.GetMask(GameConstants.Layers.Enemy);
            }
            s_flying.Add(a);
            return a;
        }

        void OnEnable() => GameEvents.WorldReset += Vanish;
        void OnDisable() { GameEvents.WorldReset -= Vanish; s_flying.Remove(this); }
        void Vanish() { if (this != null) Destroy(gameObject); }

        readonly RaycastHit2D[] _hits = new RaycastHit2D[6];

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_stuckFor >= 0f)
            {
                _stuckFor += dt;
                if (_sprite != null) _sprite.enabled = _stuckFor < StuckTime * 0.5f || ((int)(_stuckFor * 20f) & 1) == 0;
                if (_stuckFor >= StuckTime) Destroy(gameObject);
                return;
            }

            Vector2 from = transform.position;
            float step = Mathf.Min(_speed * dt, _left);

            // inimigo no caminho (os inimigos têm colisor de gatilho na layer Enemy)
            int n = Physics2D.CircleCastNonAlloc(from, 0.15f, _dir, _hits, step, s_enemyMask);
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i].collider;
                if (col == null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null) continue;
                var mb = target as MonoBehaviour;
                if (mb != null && _source != null && mb.gameObject == _source) continue;
                target.TakeDamage(new DamageInfo(_damage, _dir, DamageType.Normal, AttackAngle.Front, _source));
                HitSparks.Burst(_hits[i].point, 5, Color.white, new Color32(0xc4, 0x98, 0x60, 0xff), 1.5f, 3.5f, 0.2f, 10f, 0.2f);
                SeteVidas.Audio.Sfx.Play("flecha_crava", _hits[i].point, 0.8f, 1.2f);
                Destroy(gameObject);
                return;
            }

            var wall = Physics2D.Raycast(from, _dir, step, s_wallMask);
            if (wall.collider != null)
            {
                transform.position = wall.point - _dir * 0.2f;
                SeteVidas.Audio.Sfx.Play("flecha_crava", wall.point, 0.6f);
                _stuckFor = 0f;
                s_flying.Remove(this);
                return;
            }

            _left -= step;
            transform.position = from + _dir * step;
            if (_left <= 0f) Destroy(gameObject);
        }

        /// <summary>Flecha 12 × 3 px (haste de madeira, ponta de ferro, penas claras), apontando para a direita.</summary>
        static Sprite ArrowSprite()
        {
            if (s_sprite != null) return s_sprite;
            const int w = 12, h = 3;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "flecha_gato", hideFlags = HideFlags.DontSave
            };
            Color32 clear = new Color32(0, 0, 0, 0), wood = new Color32(0x96, 0x6e, 0x40, 0xff),
                    iron = new Color32(0xc8, 0xd0, 0xdc, 0xff), feather = new Color32(0xf2, 0xec, 0xd8, 0xff);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = clear;
                    if (y == 1) c = x >= 10 ? iron : x <= 2 ? feather : wood;
                    else if (x <= 2 && x != 1) c = feather;
                    else if (x == 10) c = iron;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            s_sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            s_sprite.hideFlags = HideFlags.DontSave;
            return s_sprite;
        }
    }
}
