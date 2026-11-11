using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>Estados da IA (GDD §5.1).</summary>
    public enum AlertState { Patrolling, Suspicious, Investigating, Combat, Dead }

    /// <summary>Limiares e tempos do medidor de suspeita.</summary>
    public struct AlertTuning
    {
        public float SuspiciousThreshold;   // Patrulhando → Suspeito
        public float InvestigateThreshold;  // Suspeito → Investigando (depois de olhar um tempo)
        public float CalmThreshold;         // Suspeito → Patrulhando (esqueceu)
        public float DrainDelay;            // segundos sem estímulo antes de esvaziar
        public float DrainRate;             // por segundo
        public float SuspiciousTime;        // tempo olhando antes de ir investigar
        public float LoseSightTime;         // em combate, sem estímulo por este tempo → investiga
        public float SearchTime;            // procurando na última posição conhecida

        public static AlertTuning Default => new AlertTuning
        {
            SuspiciousThreshold = 0.25f, InvestigateThreshold = 0.5f, CalmThreshold = 0.1f,
            DrainDelay = 1f, DrainRate = 0.15f, SuspiciousTime = 1.2f, LoseSightTime = 2f, SearchTime = 3f
        };
    }

    /// <summary>
    /// Medidor de suspeita + máquina de estados da Sentinela. C# puro: o EnemyAI informa o que viu e ouviu
    /// (<see cref="See"/>, <see cref="Hear"/>) e chama <see cref="Tick"/> uma vez por passo de física.
    /// </summary>
    public sealed class AlertBrain
    {
        float _sinceStimulus = float.MaxValue;
        float _searchTimer;

        public AlertState State { get; private set; } = AlertState.Patrolling;
        /// <summary>De 0 (calmo) a 1 (detectou).</summary>
        public float Suspicion { get; private set; }
        public Vector2 LastKnownPosition { get; private set; }
        public bool HasLastKnownPosition { get; private set; }
        /// <summary>Chegou à última posição conhecida e está olhando em volta.</summary>
        public bool IsSearching { get; private set; }
        public float TimeInState { get; private set; }
        /// <summary>Viu o alvo neste passo (zerado a cada Tick).</summary>
        public bool SeesTarget { get; private set; }
        public float SecondsSinceStimulus => _sinceStimulus;

        /// <summary>Viu o alvo neste passo. Chame antes de <see cref="Tick"/>.</summary>
        public void See(Vector2 position, float fillPerSecond, float dt)
        {
            if (State == AlertState.Dead) return;
            Suspicion = Mathf.Min(1f, Suspicion + Mathf.Max(0f, fillPerSecond) * dt);
            SeesTarget = true;
            Stimulus(position);
        }

        /// <summary>Ouviu um ruído: soma a intensidade de uma vez.</summary>
        public void Hear(Vector2 position, float loudness)
        {
            if (State == AlertState.Dead || loudness <= 0f) return;
            Suspicion = Mathf.Min(1f, Suspicion + loudness);
            Stimulus(position);
        }

        /// <summary>Tomou dano: entra em combate na hora.</summary>
        public void OnDamaged(Vector2 attackerPosition)
        {
            if (State == AlertState.Dead) return;
            Suspicion = 1f;
            Stimulus(attackerPosition);
            Enter(AlertState.Combat);
        }

        /// <summary>
        /// O gato esbarrou no corpo. De frente: vê na hora (combate). Pelas costas: sente o empurrão e vira
        /// para olhar (suspeito); se já estava procurando, a pista nova leva até o gato. Em combate, só atualiza a posição.
        /// </summary>
        public void OnBumped(Vector2 playerPosition, bool fromFront, in AlertTuning t)
        {
            if (State == AlertState.Dead) return;
            if (fromFront || State == AlertState.Combat)
            {
                Suspicion = 1f;
                Stimulus(playerPosition);
                if (State != AlertState.Combat) Enter(AlertState.Combat);
                return;
            }
            Suspicion = Mathf.Max(Suspicion, Mathf.Min(0.99f, t.InvestigateThreshold + 0.15f)); // o bastante para ir investigar
            Stimulus(playerPosition);
            if (State == AlertState.Patrolling || State == AlertState.Suspicious) Enter(AlertState.Suspicious);
        }

        public void Kill() => Enter(AlertState.Dead);

        /// <summary>Volta ao início (respawn).</summary>
        public void Reset()
        {
            Suspicion = 0f;
            HasLastKnownPosition = false;
            _sinceStimulus = float.MaxValue;
            SeesTarget = false;
            Enter(AlertState.Patrolling);
        }

        /// <summary>O corpo chegou à última posição conhecida: começa a procurar.</summary>
        public void ArrivedAtLastKnownPosition()
        {
            if (State == AlertState.Investigating) IsSearching = true;
        }

        public void Tick(float dt, in AlertTuning t)
        {
            if (State == AlertState.Dead) return;
            TimeInState += dt;
            if (_sinceStimulus < float.MaxValue) _sinceStimulus += dt;

            if (State != AlertState.Combat && _sinceStimulus > t.DrainDelay)
                Suspicion = Mathf.Max(0f, Suspicion - t.DrainRate * dt);

            switch (State)
            {
                case AlertState.Patrolling:
                    if (Suspicion >= 1f) Enter(AlertState.Combat);
                    else if (Suspicion >= t.SuspiciousThreshold) Enter(AlertState.Suspicious);
                    break;

                case AlertState.Suspicious:
                    if (Suspicion >= 1f) Enter(AlertState.Combat);
                    else if (Suspicion < t.CalmThreshold) Enter(AlertState.Patrolling);
                    else if (TimeInState >= t.SuspiciousTime && Suspicion >= t.InvestigateThreshold && !SeesTarget)
                        Enter(AlertState.Investigating);
                    break;

                case AlertState.Investigating:
                    if (Suspicion >= 1f) Enter(AlertState.Combat);
                    else if (IsSearching)
                    {
                        _searchTimer += dt;
                        if (_searchTimer >= t.SearchTime)
                        {
                            Suspicion = 0f;
                            Enter(AlertState.Patrolling);
                        }
                    }
                    break;

                case AlertState.Combat:
                    if (_sinceStimulus >= t.LoseSightTime)
                    {
                        Suspicion = Mathf.Max(t.InvestigateThreshold, t.SuspiciousThreshold);
                        Enter(AlertState.Investigating);
                    }
                    break;
            }

            SeesTarget = false;
        }

        void Stimulus(Vector2 position)
        {
            LastKnownPosition = position;
            HasLastKnownPosition = true;
            _sinceStimulus = 0f;
            if (State == AlertState.Investigating)
            {
                IsSearching = false; // pista nova: vai até ela antes de procurar de novo
                _searchTimer = 0f;
            }
        }

        void Enter(AlertState state)
        {
            State = state;
            TimeInState = 0f;
            IsSearching = false;
            _searchTimer = 0f;
        }
    }
}
