using SeteVidas.Save;

namespace SeteVidas.Progression
{
    /// <summary>O que a Tobi diz quando o gato fala com ela (Docs/Narrativa/floresta-roteiro.md, D-TOBI-01 a 05).</summary>
    public enum TobiTalk
    {
        /// <summary>D-TOBI-01: o encontro (obrigatório; acontece sozinho quando o gato chega perto).</summary>
        Meet,
        /// <summary>D-TOBI-02: faltam tábuas (repetível, com a conta).</summary>
        MissingWood,
        /// <summary>D-TOBI-03: as tábuas estão, falta o pagamento (repetível).</summary>
        MissingCoins,
        /// <summary>D-TOBI-04: a reconstrução (a cutscene 2).</summary>
        Build,
        /// <summary>D-TOBI-05: depois da ponte (em rodízio).</summary>
        After,
    }

    /// <summary>
    /// Regras da Tobi e da ponte nova (F9; funções puras, testadas em Tests/EditMode/TobiTests). A entrega é opcional: 12
    /// tábuas (<see cref="ReconstructionWood"/>) e 20 moedas reconstroem a Ponte do Último Marco em madeira, com a volta
    /// para a Fortaleza e a corda no começo da ponte, descendo para as Raízes.
    /// </summary>
    public static class TobiRules
    {
        public const string MetFlag = "tobi_conheceu";
        /// <summary>A fala da aurora ao chegar na cabeceira leste (D-SAIDA-01), uma vez.</summary>
        public const string AuroraFlag = "fala_aurora";
        /// <summary>O lugar da conta nas falas (trocado na hora; "{n}" é dos atalhos de botão).</summary>
        public const string CountToken = "#N#";

        /// <summary>O que ela diz agora.</summary>
        public static TobiTalk Next(bool met, int wood, int coins, bool built)
        {
            if (built) return TobiTalk.After;
            if (!met) return TobiTalk.Meet;
            if (wood < ReconstructionWood.Total) return TobiTalk.MissingWood;
            if (coins < ReconstructionWood.BridgePrice) return TobiTalk.MissingCoins;
            return TobiTalk.Build;
        }

        public static TobiTalk Next(SaveData data) => data == null ? TobiTalk.Meet
            : Next(data.HasFlag(MetFlag), ReconstructionWood.Count(data), data.coins, ReconstructionWood.BridgeBuilt(data));

        /// <summary>"Faltam 3. E eu não trabalho fiado!" (no singular com 1).</summary>
        public static string FillCount(string text, int missing)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (missing == 1) text = text.Replace("Faltam " + CountToken, "Falta " + CountToken);
            return text.Replace(CountToken, missing.ToString());
        }

        /// <summary>Quantos tiles por segundo a nuvem de obra anda, montando a ponte (a cena dura uns 4 s numa ponte de 104).</summary>
        public const float BuildSpeed = 26f;

        /// <summary>A tábua em <paramref name="x"/> já foi montada, com a nuvem de obra em <paramref name="front"/> (vindo do oeste).</summary>
        public static bool Built(float x, float front) => x <= front;
    }
}
