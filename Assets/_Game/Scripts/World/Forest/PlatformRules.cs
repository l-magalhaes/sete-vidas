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

        /// <summary>Tempo mínimo em pé na plataforma antes de baixo + pulo descer por ela (quem faz pogo segura baixo e
        /// pode ter apertado pulo pouco antes de pousar: sem isto, cairia através do galho ao pousar).</summary>
        public const float DropThroughMinStand = 0.15f;

        /// <summary>Espessura mínima do colisor de mão única (o topo não muda): galho fino demais é atravessado em queda rápida.</summary>
        public const float MinOneWayThickness = 0.5f;

        /// <summary>Baixo + pulo, parado só numa plataforma de mão única (há <paramref name="standingFor"/> s): desce por ela em vez de pular.</summary>
        public static bool WantsDropThrough(bool onPlatformOnly, bool jumpRequested, bool crouchHeld, float moveY, float standingFor = 1f) =>
            onPlatformOnly && jumpRequested && (crouchHeld || moveY < -0.5f) && standingFor >= DropThroughMinStand;

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

        // ---------------------------------------------------------------- alcance do pogo (level design)

        /// <summary>Pulo do chão: os pés chegam a 4 tiles (JumpHeight).</summary>
        public const float JumpApex = 4f;
        /// <summary>Degrau confortável entre galhos de mão única (neles não dá para agarrar a borda; o pulo chega a 4 no limite).</summary>
        public const float MaxBranchStep = 3f;
        /// <summary>
        /// Quique num cacto: a estocada pega com os pés ~1,2 acima do centro dele (caixa de 0,875 a 0,35 abaixo dos pés) e
        /// o quique sobe 7,2 (pulo 4 × 1,8): os pés chegam a ~8,4 acima do centro do cacto.
        /// </summary>
        public const float CactusApex = 8.4f;
        /// <summary>Para estocar o próximo cacto, os pés precisam passar pelo menos isto acima do centro dele.</summary>
        public const float StabClearance = 1f;
        /// <summary>Distância lateral confortável entre um ponto de pogo e o próximo (o gato anda 7 tiles/s, ~0,6 s no ar).</summary>
        public const float MaxPogoSide = 4.5f;

        /// <summary>Até onde os pés chegam saindo de um chão (pulo) ou de um cacto (quique).</summary>
        public static float ApexFrom(bool fromCactus, float y) => y + (fromCactus ? CactusApex : JumpApex);

        /// <summary>Dá para estocar o cacto em (<paramref name="toX"/>, <paramref name="toY"/>) saindo de (fromX, fromY)?</summary>
        public static bool ReachesCactus(bool fromCactus, float fromX, float fromY, float toX, float toY, bool checkSide = true) =>
            ApexFrom(fromCactus, fromY) >= toY + StabClearance && (!checkSide || Mathf.Abs(toX - fromX) <= MaxPogoSide);

        /// <summary>Dá para pousar num chão em pé em <paramref name="topY"/> saindo de um cacto em <paramref name="fromY"/>?</summary>
        public static bool CactusReachesFloor(float fromY, float topY) => ApexFrom(true, fromY) >= topY + 0.3f;

        /// <summary>Altura do quique do cogumelo (tiles): bem mais que o pulo (4) e que o pogo (7,2).</summary>
        public const float MushroomBounceHeight = 12f; // bem mais alto que o pogo (pedido do usuário, 13/11/2026)
    }
}
