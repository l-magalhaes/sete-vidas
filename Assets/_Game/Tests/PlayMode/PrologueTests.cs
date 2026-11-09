using System.Collections;
using NUnit.Framework;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Prólogo: o contrato é lido página por página e, ao fim, a Fortaleza começa.</summary>
    public class PrologueTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Prologue", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<PrologueController>(), "Sem prólogo. Rode Tools > Sete Vidas > 14");
        }

        [UnityTest]
        public IEnumerator Contract_IsReadPageByPage_ThenFortressStarts()
        {
            yield return Load();
            var prologue = Object.FindAnyObjectByType<PrologueController>();
            Assert.Greater(prologue.PageCount, 8, "Contrato + comentários do gato");
            Assert.AreEqual(0, prologue.Page);

            prologue.Advance(); // completa a 1ª página
            Assert.IsTrue(prologue.PageComplete);
            prologue.Advance(); // 2ª página
            Assert.AreEqual(1, prologue.Page);

            for (int i = 0; i < 60 && SceneManager.GetActiveScene().name == "Prologue"; i++)
            {
                prologue.Advance();
                yield return null;
            }
            yield return null;
            Assert.AreEqual("Fortress_MVP", SceneManager.GetActiveScene().name, "Depois do contrato, a Fortaleza");
        }

        [UnityTest]
        public IEnumerator Skip_GoesStraightToFortress()
        {
            yield return Load();
            Object.FindAnyObjectByType<PrologueController>().Finish();
            yield return null;
            yield return null;
            Assert.AreEqual("Fortress_MVP", SceneManager.GetActiveScene().name);
        }
    }
}
