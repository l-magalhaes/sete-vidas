using System;
using SeteVidas.Save;

namespace SeteVidas.Progression
{
    /// <summary>
    /// A bolsa de moedas do Lion. O saldo fica no save (<see cref="SaveData.coins"/>), então nada se perde ao
    /// morrer ou fechar o jogo. Moedas entram pelas <c>CoinPickup</c> do mundo e saem pela loja (futura), que
    /// usa <see cref="CanAfford"/> e <see cref="TrySpend"/>. Spec: Docs/Design/moedas.md.
    /// </summary>
    public static class CoinPurse
    {
        /// <summary>Teto da bolsa (cabe no HUD).</summary>
        public const int Max = 99999;

        /// <summary>Saldo atual.</summary>
        public static int Coins => SaveManager.Data.coins;

        /// <summary>(saldo novo, variação). O HUD escuta para animar o contador.</summary>
        public static event Action<int, int> Changed;

        /// <summary>Guarda <paramref name="amount"/> moedas. Não salva sozinho: quem chama decide (a moeda do mundo salva na hora).</summary>
        public static void Add(int amount)
        {
            if (amount <= 0) return;
            var data = SaveManager.Data;
            int before = data.coins;
            data.coins = Math.Min(Max, before + amount);
            Changed?.Invoke(data.coins, data.coins - before);
        }

        public static bool CanAfford(int price) => price >= 0 && Coins >= price;

        /// <summary>Gasta <paramref name="price"/> moedas e salva. Retorna false (e não gasta nada) se não tiver o suficiente.</summary>
        public static bool TrySpend(int price)
        {
            if (!CanAfford(price)) return false;
            if (price == 0) return true;
            SaveManager.Data.coins -= price;
            SaveManager.Save();
            Changed?.Invoke(SaveManager.Data.coins, -price);
            return true;
        }

        /// <summary>Marca de save de uma moeda já pega no mundo.</summary>
        public static string FlagFor(string pickupId) => "moeda:" + pickupId;
    }
}
