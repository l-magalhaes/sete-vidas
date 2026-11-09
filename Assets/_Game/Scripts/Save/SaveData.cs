using System;
using System.Collections.Generic;
using SeteVidas.Classes;

namespace SeteVidas.Save
{
    /// <summary>
    /// Conteúdo do save em JSON. Classes ficam como texto (nome do enum) para que reordenar o enum
    /// não corrompa saves antigos. Mudou o formato? Suba <see cref="CurrentVersion"/> e trate em SaveStore.Migrate.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int heartsRecovered = 1;
        public List<string> unlockedClasses = new List<string> { nameof(CharacterClass.Rogue) };
        public string activeClass = nameof(CharacterClass.Rogue);
        /// <summary>Santuário onde o gato descansou por último ("" = nenhum: usa o início da cena).</summary>
        public string lastSanctuaryId = "";
        /// <summary>Nome da cena do último santuário.</summary>
        public string scene = "";
        /// <summary>Marcas do mundo: paredes quebradas, missões, diálogos vistos.</summary>
        public List<string> flags = new List<string>();

        public static SaveData NewGame() => new SaveData();

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        public bool IsUnlocked(CharacterClass c) => unlockedClasses.Contains(c.ToString());
    }
}
