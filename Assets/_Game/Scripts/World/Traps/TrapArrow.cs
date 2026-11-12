using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>Flecha de armadilha: voa reto, machuca o gato e some ao bater numa parede ou depois de um tempo.</summary>
    public class TrapArrow : MonoBehaviour
    {
        [SerializeField] float lifetime = 4f;

        Vector2 _velocity;
        int _damage;
        float _age;
        int _wallMask, _playerMask;

        public void Launch(Vector2 velocity, int damage)
        {
            _velocity = velocity;
            _damage = damage;
            _wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            _playerMask = LayerMask.GetMask(GameConstants.Layers.Player);
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = velocity.x < 0f;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _age += dt;
            Vector2 from = transform.position;
            Vector2 step = _velocity * dt;

            var player = Physics2D.OverlapBox(from + step * 0.5f, new Vector2(Mathf.Abs(step.x) + 0.4f, 0.12f), 0f, _playerMask);
            if (player != null)
            {
                var health = player.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(new DamageInfo(_damage, _velocity.normalized, DamageType.Normal, AttackAngle.Front, gameObject));
                    Destroy(gameObject);
                    return;
                }
            }
            if (Physics2D.Raycast(from, _velocity.normalized, step.magnitude, _wallMask).collider != null || _age >= lifetime)
            {
                if (_age < lifetime) SeteVidas.Audio.Sfx.Play("flecha_crava", transform.position);
                Destroy(gameObject);
                return;
            }
            transform.position = from + step;
        }
    }
}
