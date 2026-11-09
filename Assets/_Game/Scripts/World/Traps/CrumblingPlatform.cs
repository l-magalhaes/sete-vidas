using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Plataforma que desmorona: o gato pisa, ela treme um instante e cai; volta depois de alguns segundos.
    /// Nunca conta como "chão seguro" (os espinhos não devolvem o gato para cima dela).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblingPlatform : MonoBehaviour
    {
        [Tooltip("Tempo tremendo antes de cair (s)")]
        [SerializeField, Min(0f)] float shakeTime = 0.5f;
        [Tooltip("Tempo até voltar (s)")]
        [SerializeField, Min(0.1f)] float respawnTime = 3f;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite intact;
        [SerializeField] Sprite cracked;

        BoxCollider2D _solid;
        Vector3 _spriteRest;
        float _timer = -1f;      // tremendo: 0..shakeTime
        float _fallenAt = -1f;
        int _playerMask;

        public bool IsFallen => _fallenAt >= 0f;
        public bool IsShaking => _timer >= 0f;

        void Awake()
        {
            _solid = GetComponent<BoxCollider2D>();
            if (spriteRenderer != null) _spriteRest = spriteRenderer.transform.localPosition;
            _playerMask = LayerMask.GetMask(GameConstants.Layers.Player);
        }

        void OnEnable() => GameEvents.WorldReset += Restore;
        void OnDisable() => GameEvents.WorldReset -= Restore;

        void FixedUpdate()
        {
            if (IsFallen)
            {
                if (Time.time - _fallenAt >= respawnTime) Restore();
                return;
            }
            if (!IsShaking && PlayerOnTop()) _timer = 0f;
            if (IsShaking)
            {
                _timer += Time.fixedDeltaTime;
                if (_timer >= shakeTime) Fall();
            }
        }

        bool PlayerOnTop()
        {
            var b = _solid.bounds;
            return Physics2D.OverlapBox(new Vector2(b.center.x, b.max.y + 0.06f), new Vector2(b.size.x - 0.05f, 0.1f), 0f, _playerMask) != null;
        }

        /// <summary>Cai agora (também usado pelos testes).</summary>
        public void Fall()
        {
            _timer = -1f;
            _fallenAt = Time.time;
            _solid.enabled = false;
        }

        void Restore()
        {
            _timer = -1f;
            _fallenAt = -1f;
            _solid.enabled = true;
        }

        void LateUpdate()
        {
            if (spriteRenderer == null) return;
            var t = spriteRenderer.transform;
            if (IsFallen)
            {
                // Despenca e some
                float k = Mathf.Clamp01((Time.time - _fallenAt) / 0.4f);
                t.localPosition = _spriteRest + Vector3.down * (3f * k * k);
                var c = spriteRenderer.color; c.a = 1f - k; spriteRenderer.color = c;
                return;
            }
            var color = spriteRenderer.color; color.a = 1f; spriteRenderer.color = color;
            t.localPosition = _spriteRest + (IsShaking ? new Vector3(((int)(Time.time * 50f) & 1) == 0 ? 0.04f : -0.04f, 0f) : Vector3.zero);
            if (cracked != null && intact != null) spriteRenderer.sprite = IsShaking ? cracked : intact;
        }
    }
}
