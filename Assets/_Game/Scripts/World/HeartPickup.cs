using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Meio coração que inimigos soltam ao morrer (chance baixa). Salta, cai até o chão e cura 1 hit ao
    /// encostar, sem passar do máximo. Com a vida cheia, continua no chão. Some depois de <c>lifetime</c>
    /// (pisca antes) e quando o mundo reinicia (descanso/morte), para não acumular.
    /// </summary>
    public class HeartPickup : MonoBehaviour
    {
        [Tooltip("Hits curados (meio coração = 1)")]
        [SerializeField, Min(1)] int healHits = 1;
        [Tooltip("Some depois deste tempo no chão (s). 0 = nunca")]
        [SerializeField, Min(0f)] float lifetime = 12f;
        [Tooltip("Pisca nos últimos segundos antes de sumir")]
        [SerializeField, Min(0f)] float blinkBeforeEnd = 3f;
        [SerializeField] float popVelocity = 6f;
        [SerializeField] float gravity = 40f;
        [SerializeField] LayerMask groundLayers;
        [SerializeField] SpriteRenderer spriteRenderer;

        float _vy;
        bool _landed;
        float _age;
        Vector3 _restPosition;

        public int HealHits => healHits;
        public bool Landed => _landed;

        void Awake()
        {
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            _vy = popVelocity;
            SeteVidas.Audio.Sfx.Play("coracao_cai", transform.position);
        }

        void OnEnable() => GameEvents.WorldReset += Vanish;
        void OnDisable() => GameEvents.WorldReset -= Vanish;

        void Vanish() => Destroy(gameObject);

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;

            if (!_landed)
            {
                _vy -= gravity * dt;
                float step = _vy * dt;
                if (_vy < 0f)
                {
                    var hit = Physics2D.Raycast(transform.position, Vector2.down, -step + 0.01f, groundLayers);
                    if (hit.collider != null)
                    {
                        transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                        _landed = true;
                        _restPosition = transform.position;
                        _age = 0f; // o tempo de vida conta a partir do pouso
                    }
                }
                if (!_landed) transform.position += Vector3.up * step;
            }
            else
            {
                // Flutua de leve para chamar atenção
                transform.position = _restPosition + Vector3.up * (0.06f * Mathf.Sin(_age * 4f));
                if (lifetime > 0f && _age >= lifetime) { Destroy(gameObject); return; }
            }

            if (spriteRenderer != null)
            {
                bool blinking = _landed && lifetime > 0f && _age >= lifetime - blinkBeforeEnd;
                spriteRenderer.enabled = !blinking || ((int)(_age * 12f) & 1) == 0;
            }
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other); // vida cheia ao encostar: pega quando precisar

        void TryCollect(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null && health.Heal(healHits))
            {
                SeteVidas.Audio.Sfx.Play("coracao_pega");
                Destroy(gameObject);
            }
        }

        /// <summary>Para testes: posiciona já no chão.</summary>
        public void PlaceLanded(Vector3 position)
        {
            transform.position = position;
            _restPosition = position;
            _landed = true;
            _age = 0f;
        }
    }
}
