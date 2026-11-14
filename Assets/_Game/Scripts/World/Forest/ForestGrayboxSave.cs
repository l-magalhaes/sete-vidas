using SeteVidas.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.World
{
    /// <summary>
    /// Graybox da Floresta (F2): dando Play no editor direto numa cena Forest_* (sem passar pela tela inicial), usa o
    /// save de testes, como as Forest_Test_*. Assim o preparador (<see cref="ForestTestBootstrap"/>) nunca mexe no save
    /// do jogo. Na build, e vindo da tela inicial, nada muda. Quando a estrada ligar a Fortaleza à Floresta (F3), o jogo
    /// chega nestas cenas pelo save escolhido na tela inicial.
    /// </summary>
    public static class ForestGrayboxSave
    {
        static bool s_setByUs; // sobrevive entre Plays (sem recarga de domínio): é o que desfaz a troca

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PickSlot()
        {
            if (!Application.isEditor) return;
            bool graybox = IsGraybox(SceneManager.GetActiveScene().name);
            if (graybox)
            {
                SaveManager.Slot = SaveManager.TestSlot;
                s_setByUs = true;
            }
            else if (s_setByUs)
            {
                if (SaveManager.Slot == SaveManager.TestSlot) SaveManager.Slot = null;
                s_setByUs = false;
            }
        }

        /// <summary>Cena do graybox da Floresta (Forest_*, menos as de teste da F1, que já usam o save de testes).</summary>
        public static bool IsGraybox(string scene) =>
            scene != null && scene.StartsWith("Forest_", System.StringComparison.Ordinal) && !SaveManager.IsTestScene(scene);
    }
}
