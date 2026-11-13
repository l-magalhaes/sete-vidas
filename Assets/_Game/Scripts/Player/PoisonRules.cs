namespace SeteVidas.Player
{
    /// <summary>Estado do envenenamento do gato (zero = sem veneno).</summary>
    public struct PoisonState
    {
        /// <summary>Tiques que ainda faltam.</summary>
        public int TicksLeft;
        /// <summary>Tempo até o próximo tique (s).</summary>
        public float TickTimer;
        /// <summary>Encostou de novo enquanto estava envenenado: o veneno pode tirar o último meio coração.</summary>
        public bool Lethal;

        public bool Active => TicksLeft > 0;
    }

    /// <summary>
    /// Envenenamento do Templário Zumbi (pedido do usuário, 12/11/2026; spec: Docs/Design/zumbi-templario.md).
    /// Encostar envenena: meio coração (1 hit) por segundo, durante 3 segundos. Encostar de novo reinicia os 3 s
    /// (o relógio do próximo tique continua, senão o contato contínuo nunca deixaria um tique sair).
    /// Sozinho, o veneno para no último meio coração; depois de um segundo toque durante o veneno, ele mata.
    /// Os tiques não respeitam a invencibilidade do dano comum. C# puro, testado em EditMode.
    /// </summary>
    public static class PoisonRules
    {
        public const int Ticks = 3;
        public const float TickInterval = 1f;
        public const int HitsPerTick = 1;

        /// <summary>Encostou num zumbi.</summary>
        public static PoisonState Apply(PoisonState s)
        {
            bool again = s.Active;
            return new PoisonState
            {
                TicksLeft = Ticks,
                TickTimer = again ? s.TickTimer : TickInterval,
                Lethal = s.Lethal || again,
            };
        }

        /// <summary>
        /// Avança o tempo. Retorna true se houve um tique neste passo; <paramref name="damage"/> = hits a tirar (0 ou 1).
        /// Sem o segundo toque, um tique que deixaria o gato sem vida não tira nada (fica no último meio coração).
        /// </summary>
        public static bool Step(ref PoisonState s, float dt, int currentHits, out int damage)
        {
            damage = 0;
            if (!s.Active || dt <= 0f) return false;
            s.TickTimer -= dt;
            if (s.TickTimer > 0f) return false;
            s.TickTimer += TickInterval;
            s.TicksLeft--;
            damage = s.Lethal || currentHits > HitsPerTick ? HitsPerTick : 0;
            if (!s.Active) s = default;
            return true;
        }
    }
}
