using System.IO;
using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Progression;
using SeteVidas.Save;

namespace SeteVidas.Tests
{
    /// <summary>Fase 4: vida em hits (GDD §14), estados do HUD e save em JSON.</summary>
    public class HealthAndSaveTests
    {
        // ------------------------------------------------------------- vida

        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(7, 14)]
        public void MaxHits_IsTwoPerHeart(int hearts, int expected)
        {
            Assert.AreEqual(expected, HealthRules.MaxHits(hearts));
        }

        [Test]
        public void MaxHits_ClampsHeartsBetweenOneAndSeven()
        {
            Assert.AreEqual(2, HealthRules.MaxHits(0));
            Assert.AreEqual(14, HealthRules.MaxHits(99));
        }

        [Test]
        public void OneHeart_SecondHitKills()
        {
            int hits = HealthRules.MaxHits(1);
            hits = HealthRules.ApplyDamage(hits, 1);
            Assert.IsFalse(HealthRules.IsDead(hits), "1º hit: meio coração");
            hits = HealthRules.ApplyDamage(hits, 1);
            Assert.IsTrue(HealthRules.IsDead(hits), "2º hit com 1 coração mata");
            Assert.AreEqual(0, HealthRules.ApplyDamage(hits, 5), "Nunca abaixo de zero");
        }

        [Test]
        public void Heal_HalfHeart_NeverAboveRecoveredHearts()
        {
            Assert.AreEqual(2, HealthRules.Heal(1, 1, 1), "1 coração, meio cheio + meio coração = cheio");
            Assert.AreEqual(2, HealthRules.Heal(2, 1, 1), "Já cheio: não passa de 2 hits");
            Assert.AreEqual(4, HealthRules.Heal(4, 1, 2), "2 corações cheios: continua 4");
            Assert.AreEqual(3, HealthRules.Heal(2, 1, 2));
        }

        [Test]
        public void DropRoll_LowChance()
        {
            Assert.IsTrue(HealthRules.RollDrop(0.15f, 0.10f));
            Assert.IsFalse(HealthRules.RollDrop(0.15f, 0.15f));
            Assert.IsFalse(HealthRules.RollDrop(0.15f, 0.90f));
            Assert.IsFalse(HealthRules.RollDrop(0f, 0f), "Chance 0 nunca solta");
        }

        [Test]
        public void Hud_ShowsFullHalfEmptyAndLocked()
        {
            // 3 corações recuperados (6 hits), com 3 hits: cheio, meio, vazio, e 4 bloqueados
            var s = HealthRules.Hearts(3, 3);
            Assert.AreEqual(7, s.Length);
            Assert.AreEqual(HeartState.Full, s[0]);
            Assert.AreEqual(HeartState.Half, s[1]);
            Assert.AreEqual(HeartState.Empty, s[2]);
            for (int i = 3; i < 7; i++) Assert.AreEqual(HeartState.Locked, s[i]);
        }

        [Test]
        public void Death_DoesNotRemoveHeartsOrClasses()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 2;
            data.unlockedClasses.Add(nameof(CharacterClass.Paladin));
            // A morte só zera a vida atual (que não fica no save); progresso fica intacto
            int hits = HealthRules.ApplyDamage(HealthRules.MaxHits(data.heartsRecovered), 99);
            Assert.IsTrue(HealthRules.IsDead(hits));
            Assert.AreEqual(2, data.heartsRecovered);
            Assert.IsTrue(data.IsUnlocked(CharacterClass.Paladin));
        }

        // -------------------------------------------------------------- save

        string _dir;
        string _path;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SeteVidasTests_" + System.Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_dir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Save_RoundTrip_KeepsEverything()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 2;
            data.unlockedClasses.Add("Paladin");
            data.activeClass = "Paladin";
            data.lastSanctuaryId = "fortaleza_patio";
            data.scene = "Fortress_MVP";
            data.SetFlag("parede_rachada_1");

            SaveStore.Write(_path, data);
            var loaded = SaveStore.Read(_path, out var result);

            Assert.AreEqual(SaveStore.LoadResult.Loaded, result);
            Assert.AreEqual(2, loaded.heartsRecovered);
            CollectionAssert.AreEqual(new[] { "Rogue", "Paladin" }, loaded.unlockedClasses);
            Assert.AreEqual("Paladin", loaded.activeClass);
            Assert.AreEqual("fortaleza_patio", loaded.lastSanctuaryId);
            Assert.AreEqual("Fortress_MVP", loaded.scene);
            Assert.IsTrue(loaded.HasFlag("parede_rachada_1"));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
        }

        [Test]
        public void Save_MissingFile_StartsNewGame()
        {
            var data = SaveStore.Read(_path, out var result);
            Assert.AreEqual(SaveStore.LoadResult.NewGame, result);
            Assert.AreEqual(1, data.heartsRecovered);
            CollectionAssert.AreEqual(new[] { "Rogue" }, data.unlockedClasses);
            Assert.AreEqual("", data.lastSanctuaryId);
        }

        [Test]
        public void Save_CorruptedFile_StartsNewGame_AndKeepsACopy()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "{ isto não é json");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("Save ilegível"));

            var data = SaveStore.Read(_path, out var result);

            Assert.AreEqual(SaveStore.LoadResult.Recovered, result);
            Assert.AreEqual(1, data.heartsRecovered);
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "O arquivo ruim deve ser guardado para análise");
        }

        [Test]
        public void Save_InterruptedWrite_LeftoverTmpIsIgnored()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 3;
            SaveStore.Write(_path, data);
            File.WriteAllText(_path + ".tmp", "{ meio arquivo"); // fechou o jogo no meio do próximo salvamento

            var loaded = SaveStore.Read(_path);
            Assert.AreEqual(3, loaded.heartsRecovered, "O save anterior continua valendo");
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void Save_ActiveClassNotUnlocked_FallsBackToRogue()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path,
                "{\"version\":1,\"heartsRecovered\":9,\"unlockedClasses\":[\"Mage\",\"Dragao\"],\"activeClass\":\"Druid\"}");

            var data = SaveStore.Read(_path);

            Assert.AreEqual(7, data.heartsRecovered, "Corações limitados a 7");
            CollectionAssert.AreEqual(new[] { "Rogue", "Mage" }, data.unlockedClasses, "Ladino sempre; classe inexistente sai");
            Assert.AreEqual("Rogue", data.activeClass, "Classe ativa não desbloqueada volta para o Ladino");
            Assert.IsNotNull(data.flags);
        }

        [Test]
        public void Save_Overwrite_ReplacesPreviousFile()
        {
            var a = SaveData.NewGame();
            a.lastSanctuaryId = "a";
            SaveStore.Write(_path, a);
            var b = SaveData.NewGame();
            b.lastSanctuaryId = "b";
            SaveStore.Write(_path, b);
            Assert.AreEqual("b", SaveStore.Read(_path).lastSanctuaryId);
        }
    }
}
