using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Faixas do medidor de alerta da Isyathy adormecida (Docs/Design/floresta-mecanicas.md §4).</summary>
    public enum NestState { Asleep, Restless, Agitated, Searching }

    /// <summary>Como o gato está se mexendo neste quadro (o ganho contínuo do alerta).</summary>
    public enum NestMotion { Still, CrouchMove, Walk, Run, Air, Climb }

    /// <summary>
    /// Regras puras do Ninho Adormecido (F7, EditMode): medidor 0–100, ganho por movimento e por barulho, queda,
    /// faixas, bote de busca e galhos frágeis. Valores de partida para o playtest (floresta-mecanicas.md §4).
    /// </summary>
    public static class IsyathyRules
    {
        public const float Max = 100f;
        public const float RestlessAt = 25f, AgitatedAt = 50f, SearchAt = 80f;

        // ganho contínuo (por segundo)
        public const float CrouchMoveGain = 1f, WalkGain = 4f, RunGain = 10f, ClimbGain = 1f;
        /// <summary>Velocidade (tiles/s) abaixo da qual o gato conta como parado.</summary>
        public const float StillSpeed = 0.3f;

        // queda (por segundo)
        public const float HiddenDecay = 15f, StillDecay = 5f, CalmDecay = 30f;
        /// <summary>Escondido, o alerta só começa a cair depois deste tempo parado (s).</summary>
        public const float HiddenDelay = 1f;

        // bote de busca
        public const float WarningTime = 1.2f, DiveTime = 0.22f, BiteHold = 0.25f, RetreatTime = 1f;
        public const float AfterStrike = 60f, StrikeCooldown = 4f;
        public const float StrikeWidth = 4f, StrikeHeight = 3f;
        /// <summary>Meio coração (1 hit) [APROVADO].</summary>
        public const int StrikeDamage = 1;
        /// <summary>Até onde (tiles) o bote procura o chão embaixo do último barulho.</summary>
        public const float GroundSearch = 8f;

        // galhos frágeis
        public const float BranchStandTime = 1.5f, BranchRegrow = 8f;

        public static NestState StateOf(float alert) =>
            alert >= SearchAt ? NestState.Searching
            : alert >= AgitatedAt ? NestState.Agitated
            : alert >= RestlessAt ? NestState.Restless
            : NestState.Asleep;

        /// <summary>Ciclo da respiração (s): 4 dormindo, 2,5 inquieta, 1,5 agitada, 1 em busca.</summary>
        public static float BreathPeriod(NestState s) =>
            s == NestState.Asleep ? 4f : s == NestState.Restless ? 2.5f : s == NestState.Agitated ? 1.5f : 1f;

        /// <summary>
        /// O jeito de se mexer: no ar não conta (a aterrissagem barulhenta é um barulho à parte); pendurado, escalando
        /// ou no dash conta como escalada (o dash ainda soma o barulho próprio); no chão, agachado, andando ou correndo.
        /// </summary>
        public static NestMotion MotionOf(float speedX, float speedY, bool grounded, bool crouching, bool climbing,
                                          float moveSpeed, float runFraction)
        {
            float speed = Mathf.Max(Mathf.Abs(speedX), climbing ? Mathf.Abs(speedY) : 0f);
            if (speed < StillSpeed) return NestMotion.Still;
            if (climbing) return NestMotion.Climb;
            if (!grounded) return NestMotion.Air;
            if (crouching) return NestMotion.CrouchMove;
            return Mathf.Abs(speedX) >= runFraction * moveSpeed ? NestMotion.Run : NestMotion.Walk;
        }

        public static bool IsMoving(NestMotion m) => m != NestMotion.Still;

        /// <summary>Ganho por segundo do jeito de se mexer.</summary>
        public static float MotionGain(NestMotion m)
        {
            switch (m)
            {
                case NestMotion.CrouchMove: return CrouchMoveGain;
                case NestMotion.Walk: return WalkGain;
                case NestMotion.Run: return RunGain;
                case NestMotion.Climb: return ClimbGain;
                default: return 0f;
            }
        }

        /// <summary>
        /// Quanto um barulho do <see cref="Core.NoiseSystem"/> soma de uma vez. Os passos não contam aqui (o ganho
        /// contínuo já mede o movimento). Barulho desconhecido: proporcional ao volume.
        /// </summary>
        public static float NoiseGain(string label, float loudness)
        {
            switch (label)
            {
                case "passo": return 0f;
                case "aterrissagem": return 15f;
                case "golpe": return SwingGain;
                case "tronco": return 12f;           // cada golpe no tronco rachado
                case "tronco_quebra": return 25f;    // a quebra (3 golpes + quebra ≈ 60)
                case "parede": return 25f;
                case "bau": return 5f;
                case "dash": return 6f;
                case "arco": return 4f;
                case "terremoto": return 30f;
                case "inimigo": return 8f;
                default: return Mathf.Clamp01(loudness) * 20f;
            }
        }

        /// <summary>Ganho do "golpe" (cada ataque).</summary>
        public const float SwingGain = 20f;
        /// <summary>Até quanto tempo depois do golpe o barulho do tronco conta como desse golpe (s).</summary>
        public const float TrunkAfterSwing = 0.6f;

        /// <summary>
        /// Golpe que acertou o tronco rachado: o tronco tem o ganho próprio (12 por golpe, 25 na quebra, ≈ 60 no total),
        /// então o "golpe" que veio logo antes é devolvido. Devolve o quanto tirar.
        /// </summary>
        public static float SwingRefund(string label, float sinceSwing, bool swingCounted) =>
            swingCounted && sinceSwing <= TrunkAfterSwing && (label == "tronco" || label == "tronco_quebra") ? SwingGain : 0f;

        /// <summary>Queda por segundo: na clareira do musgo, rápida; se movendo, nenhuma; escondido e parado (depois de
        /// <see cref="HiddenDelay"/>), 15; parado fora de cobertura, 5.</summary>
        public static float DecayRate(bool moving, bool hidden, float stillFor, bool calm)
        {
            if (calm) return CalmDecay;
            if (moving) return 0f;
            if (hidden && stillFor >= HiddenDelay) return HiddenDecay;
            return StillDecay;
        }

        /// <summary>Um passo do medidor: ganho e queda, preso em 0..100. Na clareira do musgo não há ganho contínuo.</summary>
        public static float Step(float alert, float gainPerSecond, float decayPerSecond, float dt) =>
            Mathf.Clamp(alert + (gainPerSecond - decayPerSecond) * dt, 0f, Max);

        public static float Add(float alert, float amount) => Mathf.Clamp(alert + amount, 0f, Max);

        /// <summary>O bote sai: alerta em busca, sem bote em andamento e depois da espera.</summary>
        public static bool CanStrike(float alert, float cooldownLeft, bool busy) =>
            !busy && cooldownLeft <= 0f && alert >= SearchAt;

        /// <summary>
        /// Os pés do gato dentro da área do bote: 4 de largura centrada no alvo, do chão até 3 acima (com meio tile de folga
        /// embaixo, para os pés em cima de um galho fino).
        /// </summary>
        public static bool InStrike(Vector2 feet, Vector2 ground) =>
            Mathf.Abs(feet.x - ground.x) <= StrikeWidth * 0.5f && feet.y >= ground.y - 0.5f && feet.y <= ground.y + StrikeHeight;

        /// <summary>A área do bote no mundo (para quebrar os galhos frágeis).</summary>
        public static Rect StrikeArea(Vector2 ground) =>
            new Rect(ground.x - StrikeWidth * 0.5f, ground.y - 0.5f, StrikeWidth, StrikeHeight + 0.5f);

        /// <summary>Galho frágil cai com o gato parado em cima tempo demais, com a Isyathy agitada ou em busca.</summary>
        public static bool BranchFalls(float standingFor, NestState state) =>
            state >= NestState.Agitated && standingFor >= BranchStandTime;

        /// <summary>Galho frágil treme (em tiles) conforme a faixa: nada dormindo/inquieta, 1 px agitada, 2 px em busca.</summary>
        public static float TrembleAmplitude(NestState state) =>
            state == NestState.Searching ? 2f / 16f : state == NestState.Agitated ? 1f / 16f : 0f;

        /// <summary>
        /// Olho do HUD: 0 fechado (dormindo), 1 meio (inquieta), 2 aberto (agitada ou em busca). Os quadros isy_olhos são
        /// 0 aberto, 1 meio, 2 fechado: <c>2 - EyeStage</c>.
        /// </summary>
        public static int EyeStage(NestState s) => s == NestState.Asleep ? 0 : s == NestState.Restless ? 1 : 2;

        /// <summary>Intervalo (s) entre as aparições do corpo no fundo: mais vezes quanto mais acordada.</summary>
        public static Vector2 BodyGap(NestState s) =>
            s == NestState.Asleep ? new Vector2(9f, 15f)
            : s == NestState.Restless ? new Vector2(6f, 10f)
            : new Vector2(3f, 6f);
    }
}
