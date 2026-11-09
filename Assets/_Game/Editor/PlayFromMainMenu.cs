using SeteVidas.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// O Play do editor começa pela tela inicial, como a build, mesmo com outra cena aberta.
    /// Ao parar, o editor volta para a cena que estava aberta. Desligue no menu para testar uma cena direto.
    /// Os testes automáticos (Test Runner) não são afetados.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        const string MenuPath = "Tools/Sete Vidas/Play começa na tela inicial";
        const string PrefKey = "SeteVidas.PlayFromMainMenu";

        static PlayFromMainMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += () => Menu.SetChecked(MenuPath, Enabled);
        }

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, priority = 62)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Menu.SetChecked(MenuPath, Enabled);
            UnityEngine.Debug.Log(Enabled
                ? "[Sete Vidas] Play começa na tela inicial."
                : "[Sete Vidas] Play começa na cena aberta (útil para testar a Playground ou a Fortaleza direto).");
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            // O Test Runner abre uma cena temporária ("InitTestScene..."): nunca troca a cena dos testes
            bool runningTests = SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
            if (!Enabled || runningTests)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameConstants.MainMenuScenePath);
        }
    }
}
