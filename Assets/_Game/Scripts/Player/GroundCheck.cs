using SeteVidas.Core;
using SeteVidas.World;
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
        [Tooltip("Plataformas de mão única (galhos): só contam como chão com os pés em cima e o gato descendo")]
        [SerializeField] LayerMask platformLayers;

        Rigidbody2D _body;
        float _lastWidth = 0.75f;
        float _maxSlopeAngle = 46f;

        /// <summary>Os raios de rampa saem um pouco acima dos pés, para não começarem dentro do chão.</summary>
        const float RayLift = 0.05f;

        public bool IsGrounded { get; private set; }
        /// <summary>Em pé só numa plataforma de mão única (baixo + pulo desce por ela).</summary>
        public bool OnPlatformOnly { get; private set; }
        public LayerMask SolidLayers => solidLayers;

        /// <summary>Normal do chão sob os pés (para cima no chão plano).</summary>
        public Vector2 GroundNormal { get; private set; } = Vector2.up;
        /// <summary>De pé numa rampa andável.</summary>
        public bool OnSlope => IsGrounded && SlopeMath.IsSlope(GroundNormal);
        /// <summary>Encostado só numa rampa íngreme demais: não conta como chão, o gato escorrega.</summary>
        public bool OnSteepSlope { get; private set; }
        /// <summary>Distância entre o meio dos pés e a rampa (a caixa encosta pela quina). O sprite desce isso.</summary>
        public float SlopeGap { get; private set; }

        // Com interpolação ligada, o transform fica "atrasado" em relação à física; use a posição do corpo.
        Vector2 Feet => _body != null ? _body.position : (Vector2)transform.position;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (solidLayers == 0)
                solidLayers = LayerMask.GetMask(
                    GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            if (platformLayers == 0) platformLayers = LayerMask.GetMask(GameConstants.Layers.Platform);
        }

        /// <summary>
        /// Atualiza <see cref="IsGrounded"/>, a normal do chão e a folga da rampa. Chame no início de cada passo de física.
        /// <paramref name="maxSlopeAngle"/> negativo = usa o último valor informado (habilidades não precisam saber dele).
        /// </summary>
        public void Refresh(float width, float maxSlopeAngle = -1f)
        {
            _lastWidth = width;
            if (maxSlopeAngle >= 0f) _maxSlopeAngle = maxSlopeAngle;
            Vector2 feet = Feet;
            var center = new Vector2(feet.x, feet.y - probeDepth * 0.5f);
            var size = new Vector2(width - 2f * sideInset, probeDepth);
            bool touching = Physics2D.OverlapBox(center, size, 0f, solidLayers) != null;

            GroundNormal = Vector2.up;
            OnSteepSlope = false;
            SlopeGap = 0f;

            if (touching)
            {
                // Normal do contato: raios curtos das duas quinas e do meio. Vale o mais "para cima":
                // com uma quina no plano e outra na rampa, o gato está no plano.
                float reach = RayLift + probeDepth + 0.04f;
                if (TryBestNormal(feet, width, reach, out Vector2 normal, out _))
                {
                    GroundNormal = normal;
                    if (!SlopeMath.IsWalkable(normal, _maxSlopeAngle))
                    {
                        OnSteepSlope = true;
                        touching = false; // íngreme demais: não é chão, a gravidade faz escorregar
                    }
                }
            }
            OnPlatformOnly = false;
            if (!touching && StandsOnPlatform(center, size, feet))
            {
                touching = true;
                OnPlatformOnly = true;
                GroundNormal = Vector2.up;
                OnSteepSlope = false;
            }
            IsGrounded = touching;

            // Na rampa, a caixa encosta pela quina de cima e o meio dos pés fica no ar
            if (IsGrounded && SlopeMath.IsSlope(GroundNormal))
            {
                float expected = SlopeMath.CenterGap(width * 0.5f, GroundNormal);
                var hit = Physics2D.Raycast(new Vector2(feet.x, feet.y + RayLift), Vector2.down, RayLift + expected + 0.05f, solidLayers);
                if (hit.collider != null && SlopeMath.IsSlope(hit.normal))
                    SlopeGap = Mathf.Max(0f, hit.distance - RayLift);
            }
        }

        // ------------------------------------------------- plataformas de mão única

        readonly Collider2D[] _platformHits = new Collider2D[4];
        readonly System.Collections.Generic.List<Collider2D> _dropped = new System.Collections.Generic.List<Collider2D>();
        float _dropUntil = -1f;
        Collider2D _self;

        /// <summary>Pés em cima de uma plataforma (não atravessando por baixo) e o gato não está subindo.</summary>
        bool StandsOnPlatform(Vector2 center, Vector2 size, Vector2 feet)
        {
            if (platformLayers == 0) return false;
            RestoreDropped();
            float vy = _body != null ? _body.linearVelocity.y : 0f;
            int n = Physics2D.OverlapBoxNonAlloc(center, size, 0f, _platformHits, platformLayers);
            for (int i = 0; i < n; i++)
            {
                var col = _platformHits[i];
                if (col == null || _dropped.Contains(col)) continue;
                if (PlatformRules.StandsOn(feet.y, col.bounds.max.y, vy)) return true;
            }
            return false;
        }

        /// <summary>
        /// Baixo + pulo numa plataforma de mão única: ignora a colisão com as plataformas sob os pés por
        /// <paramref name="seconds"/> (o gato cai através delas). Devolve se havia alguma.
        /// </summary>
        public bool DropThroughPlatforms(float seconds = PlatformRules.DropThroughTime)
        {
            if (_self == null) _self = GetComponent<Collider2D>();
            Vector2 feet = Feet;
            var center = new Vector2(feet.x, feet.y - probeDepth * 0.5f);
            var size = new Vector2(_lastWidth - 2f * sideInset, probeDepth + 0.1f);
            int n = Physics2D.OverlapBoxNonAlloc(center, size, 0f, _platformHits, platformLayers);
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                var col = _platformHits[i];
                if (col == null || _dropped.Contains(col)) continue;
                if (_self != null) Physics2D.IgnoreCollision(_self, col, true);
                _dropped.Add(col);
                any = true;
            }
            if (any) _dropUntil = Time.time + seconds;
            return any;
        }

        void RestoreDropped()
        {
            if (_dropped.Count == 0 || Time.time < _dropUntil) return;
            foreach (var col in _dropped)
                if (col != null && _self != null) Physics2D.IgnoreCollision(_self, col, false);
            _dropped.Clear();
        }

        /// <summary>
        /// Procura chão andável até <paramref name="maxDistance"/> abaixo dos pés (para colar o gato na rampa
        /// ao começar uma descida, em vez de ele voar um pouco). Devolve a distância até encostar.
        /// </summary>
        public bool TryFindSnap(float width, float maxDistance, out float distance)
        {
            distance = 0f;
            if (!TryBestNormal(Feet, width, RayLift + maxDistance, out Vector2 normal, out float closest)) return false;
            if (!SlopeMath.IsWalkable(normal, _maxSlopeAngle)) return false;
            distance = Mathf.Max(0f, closest - RayLift);
            return true;
        }

        /// <summary>Há uma rampa íngreme demais logo à frente dos pés? (o gato não tenta subir nela andando)</summary>
        public bool SteepAhead(int dir, float width)
        {
            if (dir == 0) return false;
            Vector2 feet = Feet;
            var hit = Physics2D.Raycast(new Vector2(feet.x, feet.y + RayLift), new Vector2(dir, 0f), width * 0.5f + 0.06f, solidLayers);
            return hit.collider != null && hit.normal.y > 0.01f && !SlopeMath.IsWalkable(hit.normal, _maxSlopeAngle);
        }

        /// <summary>Raios para baixo das duas quinas e do meio: a normal mais "para cima" e a menor distância.</summary>
        bool TryBestNormal(Vector2 feet, float width, float length, out Vector2 best, out float closest)
        {
            best = Vector2.up;
            closest = float.MaxValue;
            bool any = false;
            float half = width * 0.5f - sideInset;
            for (int i = -1; i <= 1; i++)
            {
                var origin = new Vector2(feet.x + i * half, feet.y + RayLift);
                var hit = Physics2D.Raycast(origin, Vector2.down, length, solidLayers);
                if (hit.collider == null || hit.normal.y <= 0.01f) continue;
                if (!any || hit.normal.y > best.y) best = hit.normal;
                closest = Mathf.Min(closest, hit.distance);
                any = true;
            }
            return any;
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
