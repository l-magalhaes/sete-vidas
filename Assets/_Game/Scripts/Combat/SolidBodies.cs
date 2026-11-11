using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>Um corpo em que o gato esbarra (inimigo vivo, chefe na luta). Spec: melhorias da Fortaleza, item 6.</summary>
    public interface ISolidBody
    {
        /// <summary>Caixa do corpo no mundo.</summary>
        Rect Bounds { get; }
        /// <summary>Sólido agora (vivo, na luta). Morto não bloqueia.</summary>
        bool IsSolid { get; }
        /// <summary>O gato empurrou o corpo, vindo de <paramref name="playerPosition"/>.</summary>
        void OnBumped(Vector2 playerPosition);
    }

    /// <summary>
    /// Corpos sólidos só para o gato. A física continua ignorando Player × Enemy (ninguém empurra ninguém);
    /// o controlador do gato lê esta lista e para ao encostar, ou sai de lado se ficou por dentro.
    /// </summary>
    public static class SolidBodies
    {
        static readonly List<ISolidBody> s_all = new List<ISolidBody>();

        public static IReadOnlyList<ISolidBody> All => s_all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        public static void Register(ISolidBody body) { if (body != null && !s_all.Contains(body)) s_all.Add(body); }
        public static void Unregister(ISolidBody body) => s_all.Remove(body);

        /// <summary>Caixa de um BoxCollider2D ligado (vazia se desligado).</summary>
        public static Rect RectOf(Collider2D box)
        {
            if (box == null || !box.enabled) return Rect.zero;
            var b = box.bounds;
            return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }
    }

    /// <summary>Regras puras do esbarrão (EditMode).</summary>
    public static class BodyBlockRules
    {
        /// <summary>Folga em cima do corpo: com os pés acima disto, o gato está passando por cima (pulo).</summary>
        public const float TopMargin = 0.15f;
        /// <summary>Distância que já conta como encostado.</summary>
        public const float ContactGap = 0.05f;
        /// <summary>Sobreposição horizontal que conta como "por dentro".</summary>
        public const float InsideOverlap = 0.02f;

        /// <summary>As alturas se cruzam (o gato não está por cima nem por baixo do corpo).</summary>
        public static bool OverlapsVertically(Rect player, Rect body) =>
            player.yMin < body.yMax - TopMargin && player.yMax > body.yMin + 0.05f;

        /// <summary>Quanto as larguras se cruzam (negativo = há um vão entre eles).</summary>
        public static float HorizontalOverlap(Rect player, Rect body) =>
            Mathf.Min(player.xMax, body.xMax) - Mathf.Max(player.xMin, body.xMin);

        /// <summary>
        /// Velocidade horizontal permitida ao lado de um corpo: o gato chega até encostar e para.
        /// Andando para longe do corpo, não muda nada.
        /// </summary>
        public static float ClampVelocity(Rect player, Rect body, float vx, float dt)
        {
            if (dt <= 0f) return vx;
            if (body.center.x >= player.center.x)
            {
                if (vx <= 0f) return vx;
                float gap = Mathf.Max(0f, body.xMin - player.xMax);
                return Mathf.Min(vx, gap / dt);
            }
            if (vx >= 0f) return vx;
            float gapLeft = Mathf.Max(0f, player.xMin - body.xMax);
            return Mathf.Max(vx, -gapLeft / dt);
        }

        /// <summary>
        /// Lado para onde o gato sai quando ficou por dentro do corpo: o lado em que o centro dele já está,
        /// a não ser que esse lado esteja fechado (parede ou perigo). 0 = os dois fechados (não empurra).
        /// </summary>
        public static int PushOutDirection(float playerCenterX, float bodyCenterX, bool leftFree, bool rightFree)
        {
            int preferred = playerCenterX >= bodyCenterX ? 1 : -1;
            bool preferredFree = preferred > 0 ? rightFree : leftFree;
            if (preferredFree) return preferred;
            bool otherFree = preferred > 0 ? leftFree : rightFree;
            return otherFree ? -preferred : 0;
        }

        /// <summary>O gato encostou pela frente do corpo (do lado para onde ele olha).</summary>
        public static bool IsFrontBump(float bodyX, int bodyFacing, float playerX) =>
            (playerX - bodyX) * bodyFacing >= 0f;
    }
}
