using SeteVidas.Save;

namespace SeteVidas.Progression
{
    /// <summary>
    /// Madeira de Reconstrução (Floresta Silenciosa, Docs/Design/floresta-mecanicas.md §3): 12 tábuas em baús.
    /// Entregues à Tobi com 20 moedas, reconstroem a Ponte do Último Marco (opcional, decisão de 13/11/2026).
    /// Sem campo novo no save: cada tábua é a flag "madeira_&lt;id do baú&gt;", e a conta sai das flags.
    /// </summary>
    public static class ReconstructionWood
    {
        /// <summary>Tábuas que a Tobi pede para a ponte.</summary>
        public const int Total = 12;
        /// <summary>Tábuas espalhadas pela floresta (baús W01–W19, sem o W16): mais que as 12 da ponte, de propósito.</summary>
        public const int InForest = 18;
        public const int BridgePrice = 20;
        public const string WoodPrefix = "madeira_";
        public const string ChestPrefix = "bau_";
        public const string BridgeFlag = "ponte_reconstruida";
        public const string DisplayName = "Madeira de Reconstrução";

        public static string WoodFlag(string chestId) => WoodPrefix + chestId;
        public static string ChestFlag(string chestId) => ChestPrefix + chestId;

        /// <summary>Tábuas coletadas (todas: pode passar das 12 da ponte, até as 18 da floresta).</summary>
        public static int Count(SaveData data)
        {
            if (data == null || data.flags == null) return 0;
            int n = 0;
            foreach (var f in data.flags)
                if (f != null && f.StartsWith(WoodPrefix, System.StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>Quantas faltam para a ponte (0 quando já dá).</summary>
        public static int Missing(SaveData data) => System.Math.Max(0, Total - Count(data));

        /// <summary>"Madeira de Reconstrução: 8/18" (o inventário: quantas da floresta já foram achadas).</summary>
        public static string Label(SaveData data) => DisplayName + ": " + Count(data) + "/" + InForest;

        /// <summary>Rodapé do inventário: quanto falta para a ponte.</summary>
        public static string BridgeHint(SaveData data)
        {
            int missing = Missing(data);
            return missing > 0 ? "A ponte precisa de " + Total + " (faltam " + missing + ")" : "Já dá para a ponte (" + Total + ")";
        }

        /// <summary>
        /// O aviso ao pegar: só o nome, sem contador (pedido do usuário, 13/11/2026: a conta fica no inventário).
        /// </summary>
        public const string PickupText = "+1 " + DisplayName;

        public static bool BridgeBuilt(SaveData data) => data != null && data.HasFlag(BridgeFlag);

        /// <summary>A Tobi constrói: 12 tábuas, 20 moedas e a ponte ainda não existe.</summary>
        public static bool CanBuildBridge(SaveData data) =>
            data != null && !BridgeBuilt(data) && Count(data) >= Total && data.coins >= BridgePrice;
    }
}
