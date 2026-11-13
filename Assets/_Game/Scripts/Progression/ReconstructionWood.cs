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
        public const int Total = 12;
        public const int BridgePrice = 20;
        public const string WoodPrefix = "madeira_";
        public const string ChestPrefix = "bau_";
        public const string BridgeFlag = "ponte_reconstruida";
        public const string DisplayName = "Madeira de Reconstrução";

        public static string WoodFlag(string chestId) => WoodPrefix + chestId;
        public static string ChestFlag(string chestId) => ChestPrefix + chestId;

        /// <summary>Tábuas coletadas (nunca passa de <see cref="Total"/>).</summary>
        public static int Count(SaveData data)
        {
            if (data == null || data.flags == null) return 0;
            int n = 0;
            foreach (var f in data.flags)
                if (f != null && f.StartsWith(WoodPrefix, System.StringComparison.Ordinal)) n++;
            return n > Total ? Total : n;
        }

        public static int Missing(SaveData data) => Total - Count(data);

        /// <summary>"Madeira de Reconstrução: 8/12".</summary>
        public static string Label(SaveData data) => DisplayName + ": " + Count(data) + "/" + Total;

        /// <summary>"Madeira de Reconstrução (8/12)": o aviso ao pegar.</summary>
        public static string PickupText(int count) => DisplayName + " (" + count + "/" + Total + ")";

        public static bool BridgeBuilt(SaveData data) => data != null && data.HasFlag(BridgeFlag);

        /// <summary>A Tobi constrói: 12 tábuas, 20 moedas e a ponte ainda não existe.</summary>
        public static bool CanBuildBridge(SaveData data) =>
            data != null && !BridgeBuilt(data) && Count(data) >= Total && data.coins >= BridgePrice;
    }
}
