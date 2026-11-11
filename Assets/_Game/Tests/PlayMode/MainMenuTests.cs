using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Tela inicial: Continuar só com save; Continuar leva ao santuário; Novo jogo apaga e começa com 7 corações.</summary>
    public class MainMenuTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<MainMenu>(), "Sem tela inicial. Rode Tools > Sete Vidas > 12");
        }

        /// <summary>Grava um save como se o jogador tivesse descansado no santuário antes da arena.</summary>
        static void WriteSaveAtArena()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 2;
            data.unlockedClasses.Add("Paladin");
            data.lastSanctuaryId = "playground_arena";
            data.lastSanctuaryName = "Antes da arena";
            data.scene = "Playground";
            SaveStore.Write(SaveManager.FilePath, data);
            SaveManager.PathOverride = SaveManager.PathOverride; // esquece a memória: lê do disco
        }

        [UnityTest]
        public IEnumerator NoSave_ContinueDisabled()
        {
            SaveManager.DeleteSave();
            yield return LoadMenu();
            Assert.IsFalse(Object.FindAnyObjectByType<MainMenu>().CanContinue);
        }

        [UnityTest]
        public IEnumerator Continue_LoadsSavedSceneAtTheSanctuary()
        {
            WriteSaveAtArena();
            yield return LoadMenu();
            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.IsTrue(menu.CanContinue);

            menu.Continue();
            yield return null;
            yield return null;
            yield return null; // a cena nova roda Awake/Start
            Assert.AreEqual("Playground", SceneManager.GetActiveScene().name);
            var health = PlayerHealth.Active;
            Assert.AreEqual(2, health.HeartsRecovered, "Continua com o progresso salvo");
            Assert.AreEqual(Sanctuary.Find("playground_arena").SpawnPoint.x, health.transform.position.x, 0.3f,
                "Começa no santuário onde parou");
        }

        [UnityTest]
        public IEnumerator TestArea_OpensPlayground()
        {
            SaveManager.DeleteSave();
            yield return LoadMenu();
            Object.FindAnyObjectByType<MainMenu>().OpenTestArea();
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual("Playground", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator NewGame_ErasesSave_AndStartsWithSevenHearts()
        {
            WriteSaveAtArena();
            yield return LoadMenu();
            Object.FindAnyObjectByType<MainMenu>().StartNewGame();
            yield return null;
            yield return null;

            // Com o prólogo na build, o Novo jogo passa primeiro pelo contrato
            var prologue = Object.FindAnyObjectByType<PrologueController>();
            if (prologue != null)
            {
                Assert.AreEqual("Prologue", SceneManager.GetActiveScene().name);
                prologue.Finish();
                yield return null;
                yield return null;
                yield return null;
            }
            // a narração de chegada grava a flag dela assim que o gato nasce (dentro da área): o que não pode sobrar é o save
            // antigo (santuário da arena, 2 corações)
            Assert.IsTrue(!SaveManager.HasSaveFile || string.IsNullOrEmpty(SaveStore.Read(SaveManager.FilePath).lastSanctuaryId), "Novo jogo apaga o save");
            Assert.AreEqual("Fortress_MVP", SceneManager.GetActiveScene().name, "Novo jogo começa na Fortaleza (tela inicial antiga? Rode Tools > Sete Vidas > 12)");
            Assert.AreEqual(7, PlayerHealth.Active.HeartsRecovered);
            Assert.AreEqual(3f, PlayerHealth.Active.transform.position.x, 0.5f, "Começa na entrada da Fortaleza");
        }
    }
}
