using System.Collections;
using NUnit.Framework;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Templário Zumbi na Fortaleza (Docs/Design/zumbi-templario.md).</summary>
    public class ZombieTests
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
            Assert.IsNotNull(Object.FindAnyObjectByType<ZombieOutbreak>(),
                "Sem o ZombieOutbreak. Rode Tools > Sete Vidas > 13c (ou o 13)");
        }

        [UnityTest]
        public IEnumerator BeforeTheTome_OnlyTemplars()
        {
            SaveManager.Data.flags.Remove(CursedTome.TakenFlag);
            yield return Load();
            Assert.IsFalse(Object.FindAnyObjectByType<ZombieOutbreak>().HasStarted);
            Assert.AreEqual(0, Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude).Length);
            Assert.Greater(Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude).Length, 0);
        }

        [UnityTest]
        public IEnumerator AfterTheTome_EveryTemplarBecomesAZombie_PlusThreeMore()
        {
            yield return Load();
            int templars = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude).Length;
            SaveManager.Data.SetFlag(CursedTome.TakenFlag);
            yield return new WaitForSecondsRealtime(0.4f);   // o surto confere a marca 4× por segundo
            var outbreak = Object.FindAnyObjectByType<ZombieOutbreak>();
            Assert.IsTrue(outbreak.HasStarted);
            Assert.AreEqual(0, Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude).Length, "nenhum templário de pé");
            Assert.AreEqual(templars + 3, outbreak.Zombies.Count, "um zumbi por posto + 2 depois do tomo + 1 na estrada");
        }

        [UnityTest]
        public IEnumerator TouchingAZombie_Poisons()
        {
            SaveManager.Data.SetFlag(CursedTome.TakenFlag);
            yield return Load();
            var outbreak = Object.FindAnyObjectByType<ZombieOutbreak>();
            Assert.IsTrue(outbreak.HasStarted, "save com o tomo: começa com zumbis");
            yield return new WaitForFixedUpdate();
            var zombie = outbreak.Zombies[outbreak.Zombies.Count - 1];   // o da estrada
            var health = PlayerHealth.Active;
            health.GetComponent<PlayerController>().Teleport(zombie.transform.position);
            float until = Time.time + 1f;
            while (!health.IsPoisoned && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.IsTrue(health.IsPoisoned, "encostou: envenenado");
        }
    }
}
