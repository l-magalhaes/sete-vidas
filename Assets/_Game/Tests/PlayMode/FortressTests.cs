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
    /// <summary>Fase 6: a Fortaleza tem todas as salas do MVP e é separada da Playground.</summary>
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

        [UnityTest]
        public IEnumerator Start_CatAtTheEntrance_OnTheGround_SevenHearts()
        {
            yield return Load();
            var health = PlayerHealth.Active;
            Assert.IsNotNull(health);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(health.GetComponent<PlayerController>().IsGrounded, "O gato deveria estar no chão da entrada");
            Assert.AreEqual(0f, health.transform.position.y, 0.1f);
            Assert.AreEqual(7, health.HeartsRecovered);
        }

        [UnityTest]
        public IEnumerator HasEveryRoomOfTheMvp()
        {
            yield return Load();
            Assert.IsNotNull(Sanctuary.Find("fortaleza_muralha"), "Santuário no alto da muralha");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_salao"), "Santuário depois do salão");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_camara"), "Santuário antes da câmara");
            Assert.AreEqual(3, GameObject.Find("Fortaleza").GetComponentsInChildren<EnemyAI>().Length, "1 no corredor + 2 no salão");
            Assert.IsNotNull(Object.FindAnyObjectByType<GoldenBoss>(), "Golden na câmara");
            Assert.IsNotNull(Object.FindAnyObjectByType<CursedTome>(), "Tomo");
            Assert.IsNotNull(Object.FindAnyObjectByType<BreakableWall>(), "Parede rachada");
            Assert.IsNull(GameObject.Find("TrapCourse"), "A Fortaleza não pode ter coisas da Playground");
        }

        [UnityTest]
        public IEnumerator CrackedWall_IsTheOnlyWayToTheExit()
        {
            yield return Load();
            var wall = Object.FindAnyObjectByType<BreakableWall>();
            var ground = GameObject.Find("Grid/Ground").GetComponent<Tilemap>();
            int wx = Mathf.FloorToInt(wall.transform.position.x);
            for (int y = 4; y <= 13; y++)
                Assert.IsNotNull(ground.GetTile(new Vector3Int(wx, y, 0)), $"Vão acima da parede rachada em y {y}: dá para passar sem a marreta");

            var exit = GameObject.Find("Conversa: Saída da fortaleza");
            Assert.IsNotNull(exit);
            Assert.Greater(exit.transform.position.x, wall.transform.position.x, "A saída fica atrás da parede");
        }

        [UnityTest]
        public IEnumerator UpperFloor_NeedsClimbing()
        {
            yield return Load();
            var climbable = GameObject.Find("Grid/Climbable").GetComponent<Tilemap>();
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(72, 4, 0)), "Hera da muralha");
            // Andar de cima com topo em 14: muito acima do pulo (4) e do agarrar borda (5)
            var ground = GameObject.Find("Grid/Ground").GetComponent<Tilemap>();
            Assert.IsNotNull(ground.GetTile(new Vector3Int(74, 13, 0)));
            Assert.IsNull(ground.GetTile(new Vector3Int(74, 14, 0)));
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
