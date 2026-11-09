using System.IO;
using NUnit.Framework;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Todos os testes PlayMode usam um save temporário vazio: o save de verdade do jogador nunca é lido
    /// nem alterado, e um santuário salvo não teleporta o gato nos outros testes.
    /// </summary>
    [SetUpFixture]
    public class SaveIsolationFixture
    {
        [OneTimeSetUp]
        public void UseTemporarySave()
        {
            SaveManager.PathOverride = Path.Combine(Application.temporaryCachePath, "save_playmode_tests.json");
            SaveManager.DeleteSave();
        }

        [OneTimeTearDown]
        public void RestoreRealSave()
        {
            SaveManager.DeleteSave();
            SaveManager.PathOverride = null;
        }
    }
}
