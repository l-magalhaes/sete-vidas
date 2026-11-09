namespace SeteVidas.Combat
{
    /// <summary>Tempos de um ataque corpo a corpo, em segundos.</summary>
    public struct AttackTimings
    {
        public int Steps;           // golpes no combo
        public float Windup;        // preparação
        public float Active;        // hitbox ligada
        public float Recovery;      // recuperação entre golpes (janela para enfileirar o próximo)
        public float FinalRecovery; // recuperação depois do último golpe
    }

    /// <summary>
    /// Linha do tempo de um combo: preparação → ativo → recuperação, golpe a golpe.
    /// C# puro para ser testado em EditMode. Quem usa chama <see cref="Press"/> e <see cref="Tick"/>.
    /// </summary>
    public sealed class AttackSequence
    {
        public enum Phase { Idle, Windup, Active, Recovery }

        float _timer;
        bool _queued;

        public Phase Current { get; private set; } = Phase.Idle;
        /// <summary>Golpe atual do combo, começando em 0.</summary>
        public int Step { get; private set; }
        /// <summary>Muda a cada golpe que entra na fase ativa (para limpar a lista de alvos já acertados).</summary>
        public int SwingId { get; private set; }
        public bool IsAttacking => Current != Phase.Idle;
        public bool IsHitActive => Current == Phase.Active;

        /// <summary>Apertou ataque. Retorna true se iniciou ou enfileirou um golpe.</summary>
        public bool Press(in AttackTimings t)
        {
            if (Current == Phase.Idle)
            {
                Step = 0;
                Enter(Phase.Windup, t.Windup);
                return true;
            }
            if (Step < t.Steps - 1 && !_queued)
            {
                _queued = true;
                return true;
            }
            return false;
        }

        public void Tick(float deltaTime, in AttackTimings t)
        {
            if (Current == Phase.Idle) return;
            _timer -= deltaTime;

            // "while": um frame longo pode atravessar mais de uma fase
            for (int guard = 0; _timer <= 0f && Current != Phase.Idle && guard < 16; guard++)
            {
                switch (Current)
                {
                    case Phase.Windup:
                        SwingId++;
                        Enter(Phase.Active, t.Active);
                        break;
                    case Phase.Active:
                        Enter(Phase.Recovery, Step >= t.Steps - 1 ? t.FinalRecovery : t.Recovery);
                        break;
                    case Phase.Recovery:
                        if (_queued && Step < t.Steps - 1)
                        {
                            _queued = false;
                            Step++;
                            Enter(Phase.Windup, t.Windup);
                        }
                        else Cancel();
                        break;
                }
            }
        }

        /// <summary>Interrompe o combo (ex.: começou a escalar).</summary>
        public void Cancel()
        {
            Current = Phase.Idle;
            Step = 0;
            _queued = false;
            _timer = 0f;
        }

        void Enter(Phase phase, float duration)
        {
            Current = phase;
            _timer += duration;
        }
    }
}
