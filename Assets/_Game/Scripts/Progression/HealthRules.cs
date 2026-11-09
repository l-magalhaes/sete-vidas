using System;
using SeteVidas.Core;

namespace SeteVidas.Progression
{
    /// <summary>Como cada coração aparece no HUD.</summary>
    public enum HeartState
    {
        Full,     // 2 hits
        Half,     // 1 hit tomado
        Empty,    // recuperado, mas sem vida agora
        Locked    // ainda não recuperado (escurecido)
    }

    /// <summary>
    /// Regras de vida do GDD §14: cada coração recuperado vale 2 hits; cada golpe inimigo tira 1 hit.
    /// Morrer não remove corações nem classes. C# puro, testado em EditMode.
    /// </summary>
    public static class HealthRules
    {
        public static int ClampHearts(int heartsRecovered) => Math.Max(1, Math.Min(GameConstants.MaxHearts, heartsRecovered));

        /// <summary>Vida máxima em hits = corações recuperados × 2.</summary>
        public static int MaxHits(int heartsRecovered) => ClampHearts(heartsRecovered) * GameConstants.HitsPerHeart;

        /// <summary>Vida depois de levar <paramref name="damage"/> hits (nunca abaixo de 0).</summary>
        public static int ApplyDamage(int currentHits, int damage) => Math.Max(0, currentHits - Math.Max(0, damage));

        public static bool IsDead(int currentHits) => currentHits <= 0;

        /// <summary>Vida depois de curar <paramref name="amount"/> hits, nunca acima do máximo dos corações recuperados.</summary>
        public static int Heal(int currentHits, int amount, int heartsRecovered) =>
            Math.Min(MaxHits(heartsRecovered), Math.Max(0, currentHits) + Math.Max(0, amount));

        /// <summary>Sorteio de drop: <paramref name="roll01"/> entre 0 e 1 (ex.: Random.value). Chance 0,15 = 15%.</summary>
        public static bool RollDrop(float chance, float roll01) => roll01 < chance;

        /// <summary>Estado de cada um dos 7 corações para o HUD.</summary>
        public static HeartState[] Hearts(int heartsRecovered, int currentHits)
        {
            int recovered = ClampHearts(heartsRecovered);
            var result = new HeartState[GameConstants.MaxHearts];
            for (int i = 0; i < result.Length; i++)
            {
                if (i >= recovered) { result[i] = HeartState.Locked; continue; }
                int inThisHeart = Math.Max(0, Math.Min(GameConstants.HitsPerHeart, currentHits - i * GameConstants.HitsPerHeart));
                result[i] = inThisHeart >= GameConstants.HitsPerHeart ? HeartState.Full
                          : inThisHeart > 0 ? HeartState.Half
                          : HeartState.Empty;
            }
            return result;
        }
    }
}
