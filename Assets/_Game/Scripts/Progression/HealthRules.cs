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


        // ---------------------------------------------------------------- corações temporários (14/11/2026)

        /// <summary>No máximo 2 corações temporários (4 hits) de uma vez.</summary>
        public const int MaxTempHits = 4;

        /// <summary>
        /// Quantos hits temporários cabem: até <see cref="MaxTempHits"/> e só nas vagas do HUD depois dos corações
        /// recuperados (com 7 corações, não sobra vaga).
        /// </summary>
        public static int TempCap(int heartsRecovered) =>
            Math.Min(MaxTempHits, (GameConstants.MaxHearts - ClampHearts(heartsRecovered)) * GameConstants.HitsPerHeart);

        /// <summary>Ganhou <paramref name="hits"/> temporários (baú): soma até o limite.</summary>
        public static int AddTemp(int tempHits, int hits, int heartsRecovered) =>
            Math.Max(0, Math.Min(TempCap(heartsRecovered), Math.Max(0, tempHits) + Math.Max(0, hits)));

        /// <summary>
        /// Dano com corações temporários: eles absorvem primeiro; o que sobra sai da vida.
        /// Devolve a vida nova; <paramref name="tempHits"/> fica com o que sobrou dos temporários.
        /// </summary>
        public static int ApplyDamage(int currentHits, ref int tempHits, int damage)
        {
            damage = Math.Max(0, damage);
            int absorbed = Math.Min(Math.Max(0, tempHits), damage);
            tempHits = Math.Max(0, tempHits) - absorbed;
            return ApplyDamage(currentHits, damage - absorbed);
        }

        /// <summary>Estado do coração temporário <paramref name="index"/> (0 = o primeiro depois dos recuperados).</summary>
        public static HeartState TempHeart(int index, int tempHits)
        {
            int left = tempHits - index * GameConstants.HitsPerHeart;
            return left >= 2 ? HeartState.Full : left == 1 ? HeartState.Half : HeartState.Locked;
        }

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
