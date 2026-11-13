using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Regras puras das plataformas da Floresta (EditMode). Docs/Level/floresta-mapa.md §3.
    /// Unidades em tiles e segundos.
    /// </summary>
    public static class PlatformRules
    {
        /// <summary>Folga para os pés contarem como "em cima" do topo da plataforma.</summary>
        public const float TopTolerance = 0.1f;
        /// <summary>Tempo que a colisão com a plataforma fica desligada depois de baixo + pulo.</summary>
        public const float DropThroughTime = 0.3f;

        /// <summary>Plataforma de mão única: os pés estão no topo (não atravessando) e o gato não está subindo.</summary>
        public static bool StandsOn(float feetY, float topY, float velocityY) =>
            velocityY <= 0.01f && feetY >= topY - TopTolerance;

        /// <summary>Baixo + pulo, parado só numa plataforma de mão única: desce por ela em vez de pular.</summary>
        public static bool WantsDropThrough(bool onPlatformOnly, bool jumpRequested, bool crouchHeld, float moveY) =>
            onPlatformOnly && jumpRequested && (crouchHeld || moveY < -0.5f);

        /// <summary>
        /// Folha larga: afunda <paramref name="sinkSpeed"/> com o gato em cima até <paramref name="maxDepth"/>;
        /// sem ninguém, volta à altura original na mesma velocidade. Devolve o novo afundamento (0 = no lugar).
        /// </summary>
        public static float SinkStep(float depth, bool occupied, float dt, float sinkSpeed = 1f, float maxDepth = 3f) =>
            occupied ? Mathf.Min(maxDepth, depth + sinkSpeed * dt) : Mathf.Max(0f, depth - sinkSpeed * dt);

        /// <summary>
        /// Galho que balança: com peso, desce até <paramref name="dip"/> (mola rápida); sem peso, volta.
        /// Devolve o deslocamento para baixo (0 = no lugar).
        /// </summary>
        public static float SwayStep(float offset, bool occupied, float dt, float dip = 0.25f, float speed = 2f) =>
            Mathf.MoveTowards(offset, occupied ? dip : 0f, speed * dt);

        /// <summary>
        /// Cogumelo saltador: quica se o gato vai encostar no chapéu neste passo, caindo (olha um passo à frente: a física
        /// zera a queda no mesmo passo do contato), ou se já está em pé em cima dele (chegou andando ou pousou).
        /// </summary>
        public static bool MushroomBounces(float velocityY, float feetY, float topY, float dt, bool grounded)
        {
            if (feetY < topY - 0.3f) return false;                                   // encostou pelo lado, embaixo do chapéu
            if (grounded && Mathf.Abs(feetY - topY) <= 0.15f) return true;           // em pé no chapéu
            return velocityY < -0.5f && feetY + velocityY * dt * 1.5f <= topY + 0.05f; // vai pousar agora
        }

        /// <summary>Altura do quique do cogumelo (tiles): bem mais que o pulo (4) e que o pogo (7,2).</summary>
        public const float MushroomBounceHeight = 12f; // bem mais alto que o pogo (pedido do usuário, 13/11/2026)
    }
}
