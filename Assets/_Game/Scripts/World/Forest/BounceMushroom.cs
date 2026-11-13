using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Cogumelo saltador: quem cai em cima (ou chega andando nele) quica 6 tiles, sem precisar atacar. Chapéu turquesa:
    /// a mesma cor do pogo (Docs/Arte/floresta-guia-visual.md). O colisor acompanha o topo do chapéu desenhado.
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    [RequireComponent(typeof(BoxCollider2D))]
    public class BounceMushroom : MonoBehaviour
    {
        [SerializeField, Min(1f)] float bounceHeight = PlatformRules.MushroomBounceHeight;
        [Tooltip("O chapéu que amassa no quique")]
        [SerializeField] Transform cap;

        BoxCollider2D _col;
        float _squashUntil, _readyAt;
        Vector3 _capScale = Vector3.one;

        void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            if (cap != null) _capScale = cap.localScale;
        }

        void FixedUpdate()
        {
            if (Time.time < _readyAt) return; // acabou de quicar
            var h = PlayerHealth.Active;
            if (h == null || h.IsDead) return;
            var player = h.GetComponent<PlayerController>();
            if (player == null || player.Body == null) return;
            Vector2 feet = player.Body.position;
            var b = _col.bounds;
            if (feet.x < b.min.x - 0.1f || feet.x > b.max.x + 0.1f) return;
            if (!PlatformRules.MushroomBounces(player.Body.linearVelocity.y, feet.y, b.max.y, Time.fixedDeltaTime, player.IsGrounded)) return;
            _readyAt = Time.time + 0.2f;
            player.Bounce(bounceHeight);
            _squashUntil = Time.time + 0.15f;
            SeteVidas.Audio.Sfx.Play("gato_pulo", transform.position, 1.5f, 0.7f); // provisório: "boing" grave
        }

        void Update()
        {
            if (cap == null) return;
            bool squash = Time.time < _squashUntil;
            cap.localScale = squash ? new Vector3(_capScale.x * 1.25f, _capScale.y * 0.7f, 1f) : _capScale;
        }
    }
}
