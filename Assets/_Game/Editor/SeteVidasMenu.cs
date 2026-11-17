using SeteVidas.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// O menu Tools > Sete Vidas inteiro mora aqui (menos o "Save" e o "Play começa na tela inicial", que ficam junto
    /// do código deles). Só as ações do dia a dia, na ordem em que o jogador passa pelas cenas. Os geradores antigos
    /// de cada fase da Playground continuam no código (uns chamam os outros) e são rodados juntos pelo item
    /// "Manutenção > Playground".
    /// </summary>
    public static class SeteVidasMenu
    {
        const string M = "Tools/Sete Vidas/";
        const string Maint = M + "Manutenção/";

        // ------------------------------------------------- cenas do jogo (0–9)

        [MenuItem(M + "Tela inicial", priority = 0)]
        static void MainMenu() => MenuSetup.CreateMainMenu();

        [MenuItem(M + "Prólogo (o contrato)", priority = 1)]
        static void Prologue() => PrologueSetup.CreatePrologue();

        [MenuItem(M + "Fortaleza: gerar a cena (recria do zero)", priority = 2)]
        static void Fortress() => FortressSetup.Build();

        [MenuItem(M + "Fortaleza: atualizar sem recriar (fundos, zumbis, saída, atiradores)", priority = 3)]
        static void FortressPatch() => FortressSetup.PatchAll();

        [MenuItem(M + "Floresta: gerar as cenas (recria do zero)", priority = 4)]
        static void Forest() => ForestGrayboxSetup.Build();

        [MenuItem(M + "Floresta: conferir alcance (sem gerar)", priority = 5)]
        static void ForestReachCheck() => ForestGrayboxSetup.CheckOnly();

        [MenuItem(M + "Floresta: juntar desenho e colisão (cena aberta)", priority = 6)]
        static void ForestFix() => ForestFixVisuals.Fix();

        // ------------------------------------------------ dados do jogo (20–29)

        [MenuItem(M + "Mapa do mundo e Diário", priority = 20)]
        static void WorldMapAndBook()
        {
            ForestLore.BuildBook();
            WorldMapSetup.Bake();
        }

        /// <summary>
        /// Regrava os textos das conversas (Golden, Vulpis) e das placas (Fortaleza e Floresta) sem recriar nenhuma cena:
        /// as cenas apontam para os mesmos assets. Só regrava a conversa cuja revisão subiu no código (Conversation).
        /// </summary>
        [MenuItem(M + "Diálogos: atualizar textos (sem recriar cenas)", priority = 22)]
        static void DialogueTexts()
        {
            DialogueSetup.CreateDialogueAssets();
            DialogueSetup.CreateVulpisDialogues();
            FortressSetup.CreateDialogues();   // D-FORT e D-FIM, e as placas D-PLACA
            ForestLore.BuildBook();
            AssetDatabase.SaveAssets();
            Debug.Log("[Sete Vidas] Diálogos: textos atualizados (Golden, Vulpis, Fortaleza, placas da Fortaleza e da Floresta) e o Diário.");
        }

        [MenuItem(M + "Importar biblioteca de cenário", priority = 21)]
        static void Scenery() => EnvLibrary.ImportMenu();

        // Save/… (40–41) e "Play começa na tela inicial" (45): ProgressionSetup e PlayFromMainMenu

        // ---------------------------------------------------- manutenção (60+)

        [MenuItem(Maint + "Configurar projeto (pastas, layers, colisões, input, Cinemachine)", priority = 60)]
        static void Configure()
        {
            SeteVidasSetup.ConfigureProject();
            SeteVidasSetup.InstallPackagesIfMissing();
        }

        [MenuItem(Maint + "Gerar tudo do zero (máquina nova)", priority = 61)]
        static void BuildEverything()
        {
            if (!EditorUtility.DisplayDialog("Gerar tudo do zero",
                    "Recria a tela inicial, o prólogo, a Fortaleza, a Floresta e o mapa do mundo. " +
                    "Edições feitas à mão nessas cenas se perdem. Continuar?", "Gerar tudo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SeteVidasSetup.ConfigureProject();
            MenuSetup.CreateMainMenu();
            PrologueSetup.CreatePrologue();
            FortressSetup.BuildNow();
            ForestGrayboxSetup.Build();
            WorldMapSetup.Bake();
            SeteVidasSetup.InstallPackagesIfMissing(); // por último: a instalação recompila o projeto
            Debug.Log("[Sete Vidas] Tudo gerado: tela inicial, prólogo, Fortaleza, Floresta e mapa do mundo.");
        }

        [MenuItem(Maint + "Playground (cena de teste das mecânicas)", priority = 72)]
        static void Playground() => SeteVidasSetup.BuildPlayground();

        [MenuItem(Maint + "Floresta F1: cenas de teste antigas (Forest_Test)", priority = 73)]
        static void ForestTests() => ForestTestSetup.Build();
    }
}
