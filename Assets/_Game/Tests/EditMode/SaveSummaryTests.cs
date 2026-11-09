using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Save;

namespace SeteVidas.Tests
{
    /// <summary>Tela inicial: resumo do save mostrado embaixo do "Continuar".</summary>
    public class SaveSummaryTests
    {
        [Test]
        public void NewGameSave_ShowsSevenHeartsRogueAndStart()
        {
            Assert.AreEqual("7 corações · Ladino · início da fase", SaveSummary.Describe(SaveData.NewGame()));
        }

        [Test]
        public void AfterBond_ShowsHeartsClassesAndSanctuaryName()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 2;
            data.unlockedClasses.Add(nameof(CharacterClass.Paladin));
            data.lastSanctuaryId = "playground_arena";
            data.lastSanctuaryName = "Antes da arena";
            Assert.AreEqual("2 corações · Ladino, Paladino · Santuário: Antes da arena", SaveSummary.Describe(data));
        }

        [Test]
        public void OneHeart_Singular_AndOldSaveWithoutName_ShowsId()
        {
            var data = SaveData.NewGame();
            data.heartsRecovered = 1;
            data.lastSanctuaryId = "playground_inicio";
            data.lastSanctuaryName = "";
            StringAssert.StartsWith("1 coração ·", SaveSummary.Describe(data));
            StringAssert.EndsWith("Santuário: playground_inicio", SaveSummary.Describe(data));
        }

        [Test]
        public void AllClassesHavePortugueseNames()
        {
            Assert.AreEqual("Caçador", SaveSummary.ClassName(CharacterClass.Hunter));
            Assert.AreEqual("Druida", SaveSummary.ClassName(CharacterClass.Druid));
            foreach (CharacterClass c in System.Enum.GetValues(typeof(CharacterClass)))
                Assert.AreNotEqual(c.ToString(), SaveSummary.ClassName(c), $"{c} sem nome em português");
        }
    }
}
