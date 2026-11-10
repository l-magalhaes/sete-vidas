using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Bolsa de moedas (Docs/Design/moedas.md): moedas do mundo vão para a bolsa, ficam no save e não voltam;
    /// a loja gasta com TrySpend; o contador do HUD mostra o saldo.
    /// </summary>
    public class CoinTests
    {
        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        IEnumerator Load()
        {
            SaveManager.DeleteSave();
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
        }

        static CoinPickup Find(string id)
        {
            foreach (var c in Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.PickupId == id) return c;
            return null;
        }

        [UnityTest]
        public IEnumerator Purse_StartsEmpty_AddsAndSpends()
        {
            yield return Load();
            Assert.AreEqual(0, CoinPurse.Coins, "Jogo novo: bolsa vazia");
            CoinPurse.Add(30);
            Assert.AreEqual(30, CoinPurse.Coins);
            Assert.IsFalse(CoinPurse.TrySpend(31), "Não gasta o que não tem");
            Assert.AreEqual(30, CoinPurse.Coins);
            Assert.IsTrue(CoinPurse.TrySpend(12));
            Assert.AreEqual(18, CoinPurse.Coins);
            Assert.AreEqual(18, SaveStore.Read(SaveManager.FilePath).coins, "Gastar salva na hora");
        }

        [UnityTest]
        public IEnumerator WorldCoin_GoesToPurse_AndNeverComesBack()
        {
            yield return Load();
            var coin = Find("pg_inicio");
            Assert.IsNotNull(coin, "Sem a moeda pg_inicio na Playground");
            Assert.IsTrue(coin.gameObject.activeInHierarchy);
            coin.Collect(PlayerHealth.Active);
            Assert.AreEqual(1, CoinPurse.Coins);
            Assert.IsFalse(coin.gameObject.activeSelf, "Some ao ser pega");
            var disk = SaveStore.Read(SaveManager.FilePath);
            Assert.AreEqual(1, disk.coins, "A moeda vai para o save na hora");
            Assert.IsTrue(disk.HasFlag(CoinPurse.FlagFor("pg_inicio")));

            // recarrega a cena com o mesmo save: a moeda não volta
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            Assert.IsFalse(Find("pg_inicio").gameObject.activeSelf, "Moeda já pega não reaparece");
            Assert.AreEqual(1, CoinPurse.Coins);
        }

        [UnityTest]
        public IEnumerator Hud_ShowsBalance()
        {
            yield return Load();
            var hud = Object.FindAnyObjectByType<SeteVidas.UI.CoinHud>();
            Assert.IsNotNull(hud, "HUD sem contador de moedas. Rode Tools > Sete Vidas (HUD)");
            CoinPurse.Add(7);
            yield return null;
            Assert.AreEqual("7", hud.ShownText);
        }
    }
}
