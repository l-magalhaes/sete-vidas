using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Virote da besta do Cão Sentinela Atirador: voa reto na direção da mira (inclusive para baixo), com o sprite
    /// apontando para onde vai. Machuca o gato ao encostar, crava na parede (fica um instante) e some.
    /// </summary>
    public class EnemyBolt : MonoBehaviour
    {
        const float Lifetime = 3f;
        const float StuckTime = 0.6f;

        Vector2 _velocity;
        int _damage;
        GameObject _source;
        float _age, _stuckFor = -1f;
        int _wallMask, _playerMask;
        SpriteRenderer _sprite;

        float _maxDistance, _travelled;

        /// <summary>
        /// Cria e dispara um virote. <paramref name="maxDistance"/> &gt; 0 = some depois de andar isso (espinhos do Cacto
        /// infectado, alcance 5); zero = só o tempo de vida.
        /// </summary>
        public static EnemyBolt Fire(Sprite sprite, Vector2 from, Vector2 velocity, int damage, GameObject source, int sortingOrder, float maxDistance = 0f)
        {
            var go = new GameObject("Virote");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            var bolt = go.AddComponent<EnemyBolt>();
            bolt._sprite = sr;
            bolt._velocity = velocity;
            bolt._damage = damage;
            bolt._source = source;
            bolt._maxDistance = maxDistance;
            bolt._wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            bolt._playerMask = LayerMask.GetMask(GameConstants.Layers.Player);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            return bolt;
        }

        public Vector2 Velocity => _velocity;

        void OnEnable() => GameEvents.WorldReset += Vanish;
        void OnDisable() => GameEvents.WorldReset -= Vanish;
        void Vanish() => Destroy(gameObject);

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_stuckFor >= 0f)
            {
                // cravado na parede: some piscando
                _stuckFor += dt;
                if (_sprite != null) _sprite.enabled = ((int)(_stuckFor * 20f) & 1) == 0 || _stuckFor < StuckTime * 0.5f;
                if (_stuckFor >= StuckTime) Destroy(gameObject);
                return;
            }

            _age += dt;
            Vector2 from = transform.position;
            Vector2 step = _velocity * dt;

            var player = Physics2D.OverlapCircle(from + step * 0.5f, 0.18f + step.magnitude * 0.5f, _playerMask);
            if (player != null)
            {
                var health = player.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(new DamageInfo(_damage, _velocity.normalized, DamageType.Normal, AttackAngle.Front, _source != null ? _source : gameObject));
                    Destroy(gameObject);
                    return;
                }
            }
            var wall = Physics2D.Raycast(from, _velocity.normalized, step.magnitude, _wallMask);
            if (wall.collider != null)
            {
                transform.position = wall.point - _velocity.normalized * 0.15f;
                SeteVidas.Audio.Sfx.Play("virote_crava", transform.position);
                _stuckFor = 0f;
                return;
            }
            if (_age >= Lifetime) { Destroy(gameObject); return; }
            _travelled += step.magnitude;
            if (_maxDistance > 0f && _travelled >= _maxDistance) { Destroy(gameObject); return; }
            transform.position = from + step;
        }
    }
}
