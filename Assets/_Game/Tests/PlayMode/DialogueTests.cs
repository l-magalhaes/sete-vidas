using System.Collections;
using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Interface de diálogo: abre, pausa o jogo, alterna os lados, conclui o vínculo e devolve o controle.</summary>
    public class DialogueTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator GoldenBond_ThroughDialogueUi()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            var runner = DialogueRunner.Active;
            Assert.IsNotNull(runner, "HUD sem DialogueRunner. Rode Tools > Sete Vidas > 9");
            var golden = Object.FindAnyObjectByType<CompanionBond>();
            var health = PlayerHealth.Active;
            var input = health.GetComponent<PlayerInputReader>();

            golden.Interact(health);
            Assert.IsTrue(runner.IsOpen, "E perto do Golden abre a conversa");
            Assert.AreEqual(0f, Time.timeScale, "O jogo pausa durante a conversa");
            Assert.IsTrue(input.GameplayLocked, "O gato não se mexe durante a conversa");
            Assert.AreEqual(SpeakerProfile.ScreenSide.Right, runner.CurrentLine.Speaker.Side, "1ª fala: Golden, à direita");

            runner.Advance(); // completa o texto da 1ª fala
            Assert.IsTrue(runner.LineComplete);
            Assert.AreEqual(0, runner.LineIndex);
            runner.Advance(); // 2ª fala
            Assert.AreEqual(1, runner.LineIndex);
            Assert.AreEqual(SpeakerProfile.ScreenSide.Left, runner.CurrentLine.Speaker.Side, "2ª fala: gato, à esquerda");
            Assert.AreEqual("ironico", runner.CurrentLine.Expression);

            // Até o fim da conversa de vínculo
            for (int guard = 0; guard < 20 && !golden.IsBonded; guard++) runner.Advance();
            Assert.IsTrue(golden.IsBonded, "Fim da conversa: vínculo feito");
            Assert.AreEqual(2, health.HeartsRecovered);
            Assert.IsTrue(ClassManager.Active.IsAvailable(CharacterClass.Paladin));

            // Em seguida, a linha de sistema (sem retrato) com a dica da troca
            Assert.IsTrue(runner.IsOpen);
            Assert.IsNull(runner.CurrentLine.Speaker, "Dica de troca é linha de sistema");
            runner.Advance();
            runner.Advance();
            Assert.IsFalse(runner.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "O jogo volta a correr");

            yield return null;
            yield return null;
            Assert.IsFalse(input.GameplayLocked, "O controle volta no frame seguinte ao fechamento");
        }

        [UnityTest]
        public IEnumerator AfterBond_EachTalkIsOneShortLine()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            var runner = DialogueRunner.Active;
            Assert.IsNotNull(runner, "HUD sem DialogueRunner. Rode Tools > Sete Vidas > 9");
            var golden = Object.FindAnyObjectByType<CompanionBond>();
            var health = PlayerHealth.Active;
            golden.CompleteBond(health);

            golden.Interact(health);
            Assert.IsTrue(runner.IsOpen);
            Assert.AreEqual("Golden", runner.CurrentLine.Speaker.DisplayName);
            Assert.AreEqual(2, health.HeartsRecovered, "Conversar de novo não dá outro coração");
        }
    }
}
