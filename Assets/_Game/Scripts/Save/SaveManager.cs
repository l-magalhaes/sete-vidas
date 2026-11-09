using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.Save
{
    /// <summary>
    /// Dono do save durante o jogo. Criado sozinho no primeiro acesso a <see cref="Data"/> (não precisa estar
    /// na cena) e sobrevive a trocas de cena. O arquivo fica em Application.persistentDataPath/save_slot1.json.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        const string FileName = "save_slot1.json";

        static SaveManager s_instance;

        /// <summary>
        /// Caminho alternativo (testes usam um arquivo temporário para não mexer no save de verdade).
        /// Mudar este valor recarrega o save no próximo acesso.
        /// </summary>
        public static string PathOverride
        {
            get => s_pathOverride;
            set
            {
                s_pathOverride = value;
                if (s_instance != null) s_instance._data = null;
            }
        }
        static string s_pathOverride;

        SaveData _data;

        public static string FilePath => !string.IsNullOrEmpty(s_pathOverride)
            ? s_pathOverride
            : Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Dados atuais (carrega do disco na primeira vez).</summary>
        public static SaveData Data
        {
            get
            {
                var m = Instance;
                if (m._data == null)
                {
                    m._data = SaveStore.Read(FilePath, out var result);
                    if (result == SaveStore.LoadResult.Loaded)
                        Debug.Log($"[Sete Vidas] Save carregado: {m._data.heartsRecovered} coração(ões), santuário '{m._data.lastSanctuaryId}'.");
                }
                return m._data;
            }
        }

        static SaveManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("[SaveManager]");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<SaveManager>();
                }
                return s_instance;
            }
        }

        /// <summary>Existe um save no disco (para o "Continuar" da tela inicial).</summary>
        public static bool HasSaveFile => File.Exists(FilePath);

        /// <summary>Grava os dados atuais no disco.</summary>
        public static void Save()
        {
            SaveStore.Write(FilePath, Data);
        }

        /// <summary>Descansou num santuário: marca o checkpoint e grava.</summary>
        public static void SaveAtSanctuary(string sanctuaryId, string displayName = "")
        {
            var data = Data;
            data.lastSanctuaryId = sanctuaryId ?? "";
            data.lastSanctuaryName = displayName ?? "";
            data.scene = SceneManager.GetActiveScene().name;
            Save();
        }

        /// <summary>Apaga o save do disco e volta a um jogo novo na memória.</summary>
        public static void DeleteSave()
        {
            SaveStore.Delete(FilePath);
            if (s_instance != null) s_instance._data = null;
        }

        void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }
    }
}
