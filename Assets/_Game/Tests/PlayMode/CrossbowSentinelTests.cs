using System.Collections;
using NUnit.Framework;
using SeteVidas.Enemies;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Cão Sentinela Atirador (Docs/Design/atirador.md): no alto da arquibancada do pátio, vê o gato lá embaixo,
    /// mira (linha de aviso), atira um virote para baixo e recarrega. Não desce do posto.
    /// </summary>
    public class CrossbowSentinelTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static EnemyAI Find(string name)
        {
            foreach (var e in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
                if (e.name == name) return e;
            return null;
        }

        [UnityTest]
        public IEnumerator Archer_OnTheBleachers_ShootsDownAtTheCat_AndHoldsPosition()
        {
            yield return SceneManager.LoadSceneAsync("Fortress_MVP", LoadSceneMode.Single);
            yield return null;
            var archer = Find("Atirador da arquibancada");
            Assert.IsNotNull(archer, "Sem o atirador da arquibancada. Gere a Fortaleza de novo (Tools > Sete Vidas > 13)");
            Assert.IsTrue(archer.Settings.Ranged, "O atirador usa a besta");
            Assert.Greater(archer.transform.position.y, 5f, "Fica no alto");
            var start = archer.transform.position;

            // o gato no pátio, à frente e abaixo dele, parado na luz
            var health = PlayerHealth.Active;
            var player = health.GetComponent<PlayerController>();
            player.Teleport(new Vector2(45f, 0.05f));
            int hits = health.CurrentHits;

            bool aimed = false, shot = false;
            float t = 0f;
            while (t < 10f && !shot)
            {
                aimed |= archer.IsAiming;
                shot = health.CurrentHits < hits || Object.FindAnyObjectByType<EnemyBolt>() != null;
                if (aimed && archer.IsAiming) Assert.Less(archer.AimDirection.y, 0f, "Mira para baixo");
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(aimed, "Mirou antes de atirar (a linha de aviso)");
            Assert.IsTrue(shot, "Atirou um virote no gato");
            Assert.AreEqual(start.x, archer.transform.position.x, 0.3f, "Não sai do posto");
        }
    }
}
