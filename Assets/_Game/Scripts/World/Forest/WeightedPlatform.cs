using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Base das plataformas que reagem ao peso do gato (galho que balança, folha larga).</summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    public abstract class WeightedPlatform : MonoBehaviour
    {
        [Tooltip("O que se mexe junto (sprite e colisor). Vazio = este objeto")]
        [SerializeField] protected Transform body;

        protected Vector3 _rest;
        protected float _offset;
        Rigidbody2D _rb;
        Collider2D _col;
        PlayerController _player;

        protected virtual void Awake()
        {
            if (body == null) body = transform;
            _rest = body.localPosition;
            _rb = body.GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = body.gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _col = body.GetComponent<Collider2D>();
        }

        /// <summary>O gato está em pé em cima (no chão, pés no topo e dentro da largura).</summary>
        protected bool Occupied()
        {
            if (_col == null) return false;
            if (_player == null)
            {
                var h = PlayerHealth.Active;
                if (h == null) return false;
                _player = h.GetComponent<PlayerController>();
                if (_player == null) return false;
            }
            if (!_player.IsGrounded) return false;
            Vector2 feet = _player.Body.position;
            var b = _col.bounds;
            return feet.x >= b.min.x - 0.2f && feet.x <= b.max.x + 0.2f && Mathf.Abs(feet.y - b.max.y) < 0.2f;
        }

        protected abstract float NextOffset(float offset, bool occupied, float dt);

        void FixedUpdate()
        {
            _offset = NextOffset(_offset, Occupied(), Time.fixedDeltaTime);
            Vector3 local = _rest + Vector3.down * _offset;
            Vector3 world = body.parent != null ? body.parent.TransformPoint(local) : local;
            _rb.MovePosition(world);
        }
    }

}
