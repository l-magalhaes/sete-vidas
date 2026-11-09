using System.Collections.Generic;
using SeteVidas.Classes;

namespace SeteVidas.Save
{
    /// <summary>Texto curto que descreve um save na tela inicial. C# puro, testado em EditMode.</summary>
    public static class SaveSummary
    {
        public static string ClassName(CharacterClass c) => c switch
        {
            CharacterClass.Rogue => "Ladino",
            CharacterClass.Paladin => "Paladino",
            CharacterClass.Mage => "Mago",
            CharacterClass.Monk => "Monge",
            CharacterClass.Bard => "Bardo",
            CharacterClass.Hunter => "Caçador",
            CharacterClass.Druid => "Druida",
            _ => c.ToString(),
        };

        /// <summary>Ex.: "2 corações · Ladino, Paladino · Santuário: Antes da arena".</summary>
        public static string Describe(SaveData data)
        {
            if (data == null) return "";
            var parts = new List<string>
            {
                data.heartsRecovered == 1 ? "1 coração" : $"{data.heartsRecovered} corações"
            };

            var classes = new List<string>();
            foreach (var name in data.unlockedClasses)
                if (System.Enum.TryParse(name, out CharacterClass c)) classes.Add(ClassName(c));
            if (classes.Count > 0) parts.Add(string.Join(", ", classes));

            if (!string.IsNullOrEmpty(data.lastSanctuaryName)) parts.Add("Santuário: " + data.lastSanctuaryName);
            else if (!string.IsNullOrEmpty(data.lastSanctuaryId)) parts.Add("Santuário: " + data.lastSanctuaryId);
            else parts.Add("início da fase");

            return string.Join(" · ", parts);
        }
    }
}
