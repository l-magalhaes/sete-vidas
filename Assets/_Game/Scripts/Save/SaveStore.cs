using System;
using System.Collections.Generic;
using System.IO;
using SeteVidas.Classes;
using SeteVidas.Progression;
using UnityEngine;

namespace SeteVidas.Save
{
    /// <summary>
    /// Lê e grava o save em JSON. Sem MonoBehaviour: testado em EditMode com arquivos temporários.
    /// Gravação segura: escreve num ".tmp" e só então troca pelo arquivo real, então fechar o jogo no meio
    /// do salvamento deixa o save anterior intacto.
    /// </summary>
    public static class SaveStore
    {
        public enum LoadResult { Loaded, NewGame, Recovered }

        public static void Write(string path, SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            Sanitize(data);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>
        /// Lê o save. Sem arquivo: jogo novo. Arquivo ilegível: guarda uma cópia ".corrupt" e começa um jogo novo
        /// (nunca trava o jogo por causa do save).
        /// </summary>
        public static SaveData Read(string path, out LoadResult result)
        {
            string tmp = path + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp); // sobra de um salvamento interrompido: o arquivo real está intacto

            if (!File.Exists(path))
            {
                result = LoadResult.NewGame;
                return SaveData.NewGame();
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) throw new FormatException("JSON vazio");
                Migrate(data);
                Sanitize(data);
                result = LoadResult.Loaded;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Sete Vidas] Save ilegível ({e.Message}). Uma cópia foi guardada como .corrupt e um jogo novo foi iniciado.");
                File.Copy(path, path + ".corrupt", overwrite: true);
                result = LoadResult.Recovered;
                return SaveData.NewGame();
            }
        }

        public static SaveData Read(string path) => Read(path, out _);

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }

        /// <summary>Converte saves de versões antigas. Hoje só existe a versão 1.</summary>
        static void Migrate(SaveData data)
        {
            if (data.version <= 0) data.version = 1; // arquivos sem versão: mesmo formato da 1
            data.version = SaveData.CurrentVersion;
        }

        /// <summary>
        /// Corrige dados impossíveis (save antigo ou editado à mão): corações entre 1 e 7, Ladino sempre
        /// desbloqueado, nomes de classe inválidos removidos e classe ativa sempre desbloqueada.
        /// </summary>
        public static void Sanitize(SaveData data)
        {
            data.heartsRecovered = HealthRules.ClampHearts(data.heartsRecovered);
            data.coins = Math.Max(0, data.coins);
            data.healCharges = Math.Max(0, Math.Min(SaveData.MaxHealCharges, data.healCharges));

            var valid = new List<string>();
            foreach (var name in data.unlockedClasses ?? new List<string>())
                if (Enum.TryParse(name, out CharacterClass c) && Enum.IsDefined(typeof(CharacterClass), c) && !valid.Contains(c.ToString()))
                    valid.Add(c.ToString());
            if (!valid.Contains(nameof(CharacterClass.Rogue))) valid.Insert(0, nameof(CharacterClass.Rogue));
            data.unlockedClasses = valid;

            if (string.IsNullOrEmpty(data.activeClass) || !valid.Contains(data.activeClass))
                data.activeClass = nameof(CharacterClass.Rogue);

            data.lastSanctuaryId ??= "";
            data.lastSanctuaryName ??= "";
            data.scene ??= "";
            data.flags ??= new List<string>();
            data.visitedSanctuaries ??= new List<SanctuaryVisit>();
            data.visitedSanctuaries.RemoveAll(v => v == null || string.IsNullOrEmpty(v.id) || string.IsNullOrEmpty(v.scene));
        }
    }
}
