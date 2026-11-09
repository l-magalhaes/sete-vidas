using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Core
{
    /// <summary>Um barulho no mundo: passos, aterrissagem, golpe (spec: Docs/Design/furtividade-sentinela.md).</summary>
    public readonly struct NoiseEvent
    {
        public readonly Vector2 Position;
        /// <summary>Até onde se ouve (tiles).</summary>
        public readonly float Radius;
        /// <summary>Quanto soma ao medidor de suspeita de quem ouve (0 a 1).</summary>
        public readonly float Loudness;
        public readonly GameObject Source;
        public readonly string Label;
        public readonly float Time;

        public NoiseEvent(Vector2 position, float radius, float loudness, GameObject source, string label, float time)
        {
            Position = position;
            Radius = radius;
            Loudness = loudness;
            Source = source;
            Label = label;
            Time = time;
        }
    }

    /// <summary>
    /// Canal global de ruídos. Quem faz barulho chama <see cref="Emit"/>; inimigos assinam <see cref="Emitted"/>.
    /// Guarda os ruídos recentes para os Gizmos de depuração.
    /// </summary>
    public static class NoiseSystem
    {
        const int RecentCapacity = 16;
        static readonly List<NoiseEvent> s_recent = new List<NoiseEvent>();

        public static event Action<NoiseEvent> Emitted;

        /// <summary>Ruídos recentes (mais novo por último), para depuração.</summary>
        public static IReadOnlyList<NoiseEvent> Recent => s_recent;

        public static void Emit(Vector2 position, float radius, float loudness, GameObject source, string label)
        {
            if (radius <= 0f || loudness <= 0f) return;
            var e = new NoiseEvent(position, radius, loudness, source, label, UnityEngine.Time.time);
            if (s_recent.Count >= RecentCapacity) s_recent.RemoveAt(0);
            s_recent.Add(e);
            Emitted?.Invoke(e);
        }

        /// <summary>Limpa ouvintes e histórico (Enter Play Mode sem recarregar o domínio).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Emitted = null;
            s_recent.Clear();
        }
    }
}
