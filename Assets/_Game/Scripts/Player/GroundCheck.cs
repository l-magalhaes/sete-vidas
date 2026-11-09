using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Detecta chão sob os pés e espaço livre acima, com Physics2D.OverlapBox.
    /// O transform do jogador fica nos pés (pivot bottom center).
    /// </summary>
    public class GroundCheck : MonoBehaviour
    {
        [Tooltip("Layers que contam como chão e teto")]
        [SerializeField] LayerMask solidLayers;
        [Tooltip("Profundidade da caixa de teste abaixo dos pés (tiles)")]
        [SerializeField] float probeDepth = 0.06f;
        [Tooltip("Recuo lateral da caixa, para não detectar paredes encostadas como chão (tiles)")]
        [SerializeField] float sideInset = 0.02f;

        Rigidbody2D _body;
        float _lastWidth = 0.75f;

        public bool IsGrounded { get; private set; }
        public LayerMask SolidLayers => solidLayers;

        // Com interpolação ligada, o transform fica "atrasado" em relação à física; use a posição do corpo.
        Vector2 Feet => _body != null ? _body.position : (Vector2)transform.position;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (solidLayers == 0)
                solidLayers = LayerMask.GetMask(
                    GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
        }

        /// <summary>Atualiza <see cref="IsGrounded"/>. Chame no início de cada passo de física.</summary>
        public void Refresh(float width)
        {
            _lastWidth = width;
            Vector2 feet = Feet;
            var center = new Vector2(feet.x, feet.y - probeDepth * 0.5f);
            var size = new Vector2(width - 2f * sideInset, probeDepth);
            IsGrounded = Physics2D.OverlapBox(center, size, 0f, solidLayers) != null;
        }

        /// <summary>Há espaço para o colisor ter esta altura a partir dos pés?</summary>
        public bool HasRoomFor(float width, float height)
        {
            const float skin = 0.02f; // evita contar o chão sob os pés como obstáculo
            Vector2 feet = Feet;
            var center = new Vector2(feet.x, feet.y + skin + (height - skin) * 0.5f);
            var size = new Vector2(width - 2f * sideInset, height - skin);
            return Physics2D.OverlapBox(center, size, 0f, solidLayers) == null;
        }

        /// <summary>
        /// Procura uma quina de borda à frente, na altura das patas.
        /// Exige: parede logo à frente abaixo das patas, topo livre perto das patas e espaço para ficar de pé em cima.
        /// </summary>
        /// <param name="dir">1 = direita, -1 = esquerda.</param>
        /// <param name="corner">Ponto da quina: x = face da parede, y = topo da borda.</param>
        public bool TryFindLedge(int dir, float width, float handHeight, float window, float standingHeight, out Vector2 corner)
        {
            corner = default;
            Vector2 feet = Feet;
            float halfWidth = width * 0.5f;
            float handY = feet.y + handHeight;
            var forward = new Vector2(dir, 0f);

            // 1) Parede à frente, logo abaixo da altura das patas
            var wall = Physics2D.Raycast(new Vector2(feet.x, handY - window), forward, halfWidth + 0.15f, solidLayers);
            if (wall.collider == null) return false;

            // 2) Topo da borda: raio para baixo, começando acima das patas. Se começar dentro do sólido,
            //    a parede continua para cima e não há quina aqui.
            var topOrigin = new Vector2(wall.point.x + dir * 0.05f, handY + window);
            if (Physics2D.OverlapPoint(topOrigin, solidLayers) != null) return false;
            var top = Physics2D.Raycast(topOrigin, Vector2.down, 2f * window, solidLayers);
            if (top.collider == null) return false;

            // 3) Espaço para ficar de pé em cima da borda (senão subir prenderia o gato no teto)
            var standCenter = new Vector2(wall.point.x + dir * (halfWidth + 0.05f), top.point.y + 0.02f + standingHeight * 0.5f);
            var standSize = new Vector2(width - 2f * sideInset, standingHeight - 0.04f);
            if (Physics2D.OverlapBox(standCenter, standSize, 0f, solidLayers) != null) return false;

            corner = new Vector2(wall.point.x, top.point.y);
            return true;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Vector2 feet = transform.position;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(new Vector3(feet.x, feet.y - probeDepth * 0.5f), new Vector3(_lastWidth - 2f * sideInset, probeDepth));
        }
#endif
    }
}
