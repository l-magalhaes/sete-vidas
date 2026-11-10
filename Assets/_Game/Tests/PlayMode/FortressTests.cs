using System.Collections;
using NUnit.Framework;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Fase 6: a Fortaleza vertical (Docs/Level/fortaleza-mvp.md). Começa no alto da montanha, desce até o pátio,
    /// sobe pelas vinhas e pelas torres até a Câmara do Selo no topo, e sai pelos fundos depois do Paladino.
    /// </summary>
    public class FortressTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Fortress_MVP", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(GameObject.Find("Fortaleza"), "Fortaleza não gerada. Rode Tools > Sete Vidas > 13");
        }

        static Tilemap Map(string name) => GameObject.Find("Grid/" + name).GetComponent<Tilemap>();

        [UnityTest]
        public IEnumerator Start_CatOnTopOfTheMountain_SevenHearts()
        {
            yield return Load();
            var health = PlayerHealth.Active;
            Assert.IsNotNull(health);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(health.GetComponent<PlayerController>().IsGrounded, "O gato deveria estar no alto do penhasco");
            Assert.AreEqual(60f, health.transform.position.y, 0.15f, "O jogo começa no alto da montanha (y 60)");
            Assert.AreEqual(7, health.HeartsRecovered);
        }

        [UnityTest]
        public IEnumerator HasEveryArea()
        {
            yield return Load();
            Assert.IsNotNull(Sanctuary.Find("fortaleza_patio"), "Santuário do pátio de treino");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_muralha"), "Santuário do portão da muralha");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_sino"), "Santuário da torre do sino");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_camara"), "Santuário antes da câmara");
            Assert.GreaterOrEqual(GameObject.Find("Fortaleza").GetComponentsInChildren<EnemyAI>().Length, 10, "Dez templários no caminho");
            var boss = Object.FindAnyObjectByType<GoldenBoss>();
            Assert.IsNotNull(boss, "Koda na câmara");
            Assert.Greater(boss.transform.position.y, 100f, "A Câmara do Selo fica no topo da torre");
            Assert.IsNotNull(Object.FindAnyObjectByType<CursedTome>(), "Tomo");
            Assert.GreaterOrEqual(Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Length, 7, "Paredes rachadas: estábulo, tomo, cofres e portão");
            Assert.GreaterOrEqual(Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, 20, "Moedas pelas fases");
            Assert.IsNull(GameObject.Find("TrapCourse"), "A Fortaleza não pode ter coisas da Playground");
        }

        [UnityTest]
        public IEnumerator Waterfall_AndSunset_AtTheStart()
        {
            yield return Load();
            Assert.IsNotNull(GameObject.Find("Fortaleza/Cachoeira"), "Cachoeira no penhasco do início");
            Assert.IsNotNull(GameObject.Find("Fortaleza/Lagoa"), "Lagoa onde o gato cai");
            Assert.IsNotNull(GameObject.Find("Céu e montanhas"), "Céu do pôr do sol e montanhas");
            Assert.IsNotNull(Map("Ground").GetTile(new Vector3Int(12, 59, 0)), "Borda do penhasco (topo em 60)");
            Assert.IsNull(Map("Ground").GetTile(new Vector3Int(15, 30, 0)), "Vão livre para o salto de fé");
        }

        [UnityTest]
        public IEnumerator CastleWall_IsClimbedByVines_InThreeSteps()
        {
            yield return Load();
            var climbable = Map("Climbable");
            var ground = Map("Ground");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(112, 4, 0)), "Vinha da muralha 1");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(120, 16, 0)), "Vinha da muralha 2");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(128, 30, 0)), "Vinha da muralha 3");
            Assert.IsNotNull(ground.GetTile(new Vector3Int(135, 42, 0)), "Topo da muralha 3 em 43");
            Assert.IsNull(ground.GetTile(new Vector3Int(135, 43, 0)));
        }

        [UnityTest]
        public IEnumerator Exit_IsBehindTheBackGate()
        {
            yield return Load();
            BreakableWall gate = null;
            foreach (var w in Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None))
                if (w.name == "Portão dos fundos") gate = w;
            Assert.IsNotNull(gate, "Portão dos fundos");
            var ground = Map("Ground");
            int gx = Mathf.FloorToInt(gate.transform.position.x);
            for (int y = 4; y <= 12; y++)
                Assert.IsNotNull(ground.GetTile(new Vector3Int(gx, y, 0)), $"Vão acima do portão em y {y}: dá para passar sem a marreta");
            var exit = GameObject.Find("Conversa: Saída da fortaleza");
            Assert.IsNotNull(exit);
            Assert.Greater(exit.transform.position.x, gate.transform.position.x, "A saída fica atrás do portão");
        }

        [UnityTest]
        public IEnumerator IntroNarration_PlaysOnlyOnce()
        {
            yield return Load();
            var trigger = GameObject.Find("Conversa: Chegada").GetComponent<DialogueTrigger>();
            Assert.IsTrue(trigger.HasDialogue, "Gatilho sem conversa ligada. Rode Tools > Sete Vidas > 13 de novo");
            var health = PlayerHealth.Active;
            Assert.IsTrue(trigger.Play(health), "Primeira vez: narração de chegada");
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            for (int i = 0; i < 20 && runner.IsOpen; i++) runner.Advance();
            Assert.IsFalse(trigger.Play(health), "Depois: não repete");
        }
    }
}
