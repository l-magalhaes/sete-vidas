using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Descanso no santuário: escurece, salva no escuro, o gato cochila na caixinha de areia e levanta.</summary>
    public class SanctuaryRestTests
    {
        const string SanctuaryB = "playground_furtividade";

        [TearDown]
        public void TearDown() { SaveManager.DeleteSave(); }

        [UnityTest]
        public IEnumerator Rest_FadesNapsInSandbox_ThenGivesControlBack()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            var health = PlayerHealth.Active;
            Assert.IsNotNull(health, "Gato sem PlayerHealth. Rode Tools > Sete Vidas > 7");
            var sanctuary = Sanctuary.Find(SanctuaryB);
            Assert.IsNotNull(sanctuary, "Santuário de x 119,5 ausente. Rode Tools > Sete Vidas > 7");
            var input = health.GetComponent<PlayerInputReader>();
            var visual = health.GetComponentInChildren<PlayerVisual>();

            health.ApplyCurse(1);
            sanctuary.BeginRest(health);
            yield return null;
            Assert.IsTrue(sanctuary.IsResting);
            Assert.IsTrue(health.Shielded, "Sem dano enquanto a tela escurece");
            Assert.IsFalse(health.IsCollapsed, "Não deita no chão: só aparece deitado na caixinha, depois do escuro");
            if (input != null) Assert.IsTrue(input.GameplayLocked, "Sem controle durante o descanso");

            // no meio da sequência: já salvou, está na caixinha de areia, cochilando
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(SanctuaryB, SaveManager.Data.lastSanctuaryId);
            Assert.AreEqual(health.MaxHits, health.CurrentHits, "Vida cheia");
            Assert.Less(Vector2.Distance(health.transform.position, sanctuary.SandboxPoint), 0.3f, "Acorda na caixinha de areia");
            Assert.IsTrue(health.IsCollapsed || !sanctuary.IsResting || visual == null || visual.CurrentClipName == "nap_wake", "Deitado na caixinha");
            if (visual != null) Assert.IsTrue(visual.CurrentClipName == "nap" || visual.CurrentClipName == "nap_wake", "Cochilando como gatinho (sem tela preta o cochilo é mais curto): " + visual.CurrentClipName);

            // fim: levantou, controle de volta, sem piscar
            float t = 0f;
            while (sanctuary.IsResting && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(sanctuary.IsResting, "A sequência deve terminar");
            Assert.IsFalse(health.IsCollapsed);
            Assert.IsFalse(health.IsInvulnerable, "Levantar do cochilo não deixa o gato piscando");
            Assert.IsFalse(health.Shielded);
            if (input != null) Assert.IsFalse(input.GameplayLocked);
        }
    }
}
