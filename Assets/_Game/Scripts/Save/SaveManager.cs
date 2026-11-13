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
        /// <summary>Save do jogo (Fortaleza).</summary>
        public const string MainSlot = "slot1";
        /// <summary>Save da área de testes (Playground): nunca mistura com o jogo.</summary>
        public const string TestSlot = "playground";
        const string TestScene = "Playground";

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

        /// <summary>
        /// Qual save usar. Vazio = automático: a Playground usa o save de testes; o resto, o do jogo.
        /// A tela inicial escolhe explicitamente. Mudar recarrega o save no próximo acesso.
        /// </summary>
        public static string Slot
        {
            get => s_slot;
            set
            {
                s_slot = value;
                if (s_instance != null) s_instance._data = null;
            }
        }
        static string s_slot;

        public static string ActiveSlot => !string.IsNullOrEmpty(s_slot) ? s_slot
            : IsTestScene(SceneManager.GetActiveScene().name) ? TestSlot : MainSlot;

        /// <summary>Cenas de teste usam o save da área de testes (Playground e as cenas Forest_Test_* da F1 da Floresta).</summary>
        public static bool IsTestScene(string scene) =>
            scene == TestScene || (scene != null && scene.StartsWith("Forest_Test_", System.StringComparison.Ordinal));

        public static string PathForSlot(string slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

        public static string FilePath => !string.IsNullOrEmpty(s_pathOverride) ? s_pathOverride : PathForSlot(ActiveSlot);

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

        /// <summary>Apaga os saves do jogo e da área de testes.</summary>
        public static void DeleteAllSaves()
        {
            SaveStore.Delete(PathForSlot(MainSlot));
            SaveStore.Delete(PathForSlot(TestSlot));
            if (s_instance != null) s_instance._data = null;
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
