using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Tina de água embaixo de uma gárgula (pedido do usuário). Não é sólida: o gato passa por ela (não empurra nem sobe).
    /// Passando andando ou correndo (ou caindo em cima), ela tomba de lado, derrama a água numa poça e faz barulho como o
    /// balde; agachado, passa sem derrubar. Volta ao lugar ao descansar no santuário ou ao morrer
    /// (<see cref="GameEvents.WorldReset"/>). Gatilho na layer Interactable (só o gato a toca).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class WaterTub : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Quadros da poça que a água derramada forma (os da poça da gárgula)")]
        [SerializeField] Sprite[] puddleFrames;
        [Tooltip("Fração da velocidade de andar do gato a partir da qual o esbarrão derruba a tina")]
        [SerializeField, Range(0f, 1f)] float tipSpeedFraction = 0.6f;
        [Tooltip("Caindo em cima mais rápido que isto (tiles/s), também derruba")]
        [SerializeField, Min(0f)] float tipFallSpeed = 8f;
        [Tooltip("Tempo tombando (s)")]
        [SerializeField, Min(0.02f)] float tipTime = 0.18f;

        Rigidbody2D _body;
        Vector3 _startPosition;
        bool _tipped;
        float _tipT = -1f;
        int _tipDir;
        Vector2 _tipFrom;
        GameObject _puddle;

        public bool IsTipped => _tipped;

        public void Configure(SpriteRenderer sprite, Sprite[] puddle)
        {
            spriteRenderer = sprite;
            puddleFrames = puddle;
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic; // não é empurrada: só tomba
            _body.freezeRotation = true;
            _startPosition = transform.position;
        }

        void OnEnable() => GameEvents.WorldReset += Restore;
        void OnDisable() => GameEvents.WorldReset -= Restore;

        /// <summary>Regra pura (EditMode): o esbarrão derruba a tina?</summary>
        public static bool Tips(float speedX, float speedY, float moveSpeed, bool crouching, float tipSpeedFraction, float tipFallSpeed)
        {
            if (-speedY >= tipFallSpeed) return true;                  // caiu em cima com força
            if (crouching) return false;                               // agachado passa sem derrubar
            return Mathf.Abs(speedX) >= moveSpeed * tipSpeedFraction;  // correndo contra
        }

        void OnTriggerEnter2D(Collider2D other) => Check(other);
        void OnTriggerStay2D(Collider2D other) => Check(other);

        void Check(Collider2D other)
        {
            if (_tipped) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.Settings == null) return;
            Vector2 v = player.Velocity;
            if (!Tips(v.x, v.y, player.Settings.MoveSpeed, player.IsCrouching, tipSpeedFraction, tipFallSpeed)) return;
            int dir = transform.position.x >= player.Body.position.x ? 1 : -1;
            Tip(dir);
        }

        /// <summary>Tomba para <paramref name="dir"/> (1 = direita), derrama e faz barulho.</summary>
        public void Tip(int dir)
        {
            if (_tipped) return;
            _tipped = true;
            _tipDir = dir >= 0 ? 1 : -1;
            _tipT = 0f;
            _tipFrom = _body.position;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.linearVelocity = Vector2.zero;

            var noise = NoisyObjectRules.Preset(NoisyObjectKind.Bucket);
            NoiseSystem.Emit(transform.position, noise.Radius, noise.Loudness, gameObject, "tina");
            SpillPuddle();
        }

        void FixedUpdate()
        {
            if (_tipT < 0f) return;
            _tipT += Time.fixedDeltaTime;
            // tomba em 3 passos (30°, 60°, 90°): no fim, deitada de lado, sem rotação quebrada no pixel
            float k = Mathf.Clamp01(_tipT / tipTime);
            float angle = -_tipDir * 90f * (Mathf.Ceil(k * 3f) / 3f);
            var box = (BoxCollider2D)GetComponent<Collider2D>();
            float halfW = box.size.x * 0.5f, halfH = box.size.y * 0.5f;
            // gira em volta da quina de baixo do lado em que tomba
            Vector2 pivot = _tipFrom + new Vector2(_tipDir * halfW, -halfH);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 offset = _tipFrom - pivot;
            Vector2 rotated = new Vector2(offset.x * Mathf.Cos(rad) - offset.y * Mathf.Sin(rad), offset.x * Mathf.Sin(rad) + offset.y * Mathf.Cos(rad));
            _body.MovePosition(pivot + rotated);
            _body.MoveRotation(angle);
            if (k >= 1f) _tipT = -1f;
        }

        void SpillPuddle()
        {
            if (puddleFrames == null || puddleFrames.Length == 0 || puddleFrames[0] == null) return;
            var box = (BoxCollider2D)GetComponent<Collider2D>();
            float floor = _body.position.y - box.size.y * 0.5f;
            const float px = 1f / 16f;
            _puddle = new GameObject("Água derramada");
            _puddle.transform.SetParent(transform.parent, false);
            var sr = _puddle.AddComponent<SpriteRenderer>();
            sr.sprite = puddleFrames[0];
            sr.sortingOrder = 2;
            float x = Mathf.Round((_body.position.x + _tipDir * 0.85f) * 16f) / 16f;
            _puddle.transform.position = new Vector3(x, floor + puddleFrames[0].bounds.extents.y - px, 0f);
            _puddle.AddComponent<SpriteLoop>().Configure(puddleFrames, 1f / 8f, 0f);
        }

        void Restore()
        {
            _tipped = false;
            _tipT = -1f;
            if (_puddle != null) Destroy(_puddle);
            _body.linearVelocity = Vector2.zero;
            _body.rotation = 0f;
            _body.position = _startPosition;
            transform.SetPositionAndRotation(_startPosition, Quaternion.identity);
        }
    }
}
