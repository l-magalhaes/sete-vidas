using NUnit.Framework;
using SeteVidas.UI;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 9: dicas em pergaminho, uma por vez e uma vez por save.</summary>
    public class TutorialRulesTests
    {
        [Test]
        public void OneAtATime_InTheOrderTheyCame()
        {
            var q = new TutorialQueue();
            q.Enter("pular", false);
            q.Enter("agachar", false);
            Assert.AreEqual("pular", q.Current);
            Assert.AreEqual(1, q.Pending);
            Assert.AreEqual("pular", q.Done(), "Fez a ação: a dica some e fica vista");
            Assert.AreEqual("agachar", q.Current, "A próxima entra");
        }

        [Test]
        public void AlreadySeen_DoesNotShowAgain()
        {
            var q = new TutorialQueue();
            q.Enter("pular", alreadySeen: true);
            Assert.IsNull(q.Current);
        }

        [Test]
        public void LeavingTheArea_HidesIt_AndDropsItFromTheQueue()
        {
            var q = new TutorialQueue();
            q.Enter("pular", false);
            q.Enter("agachar", false);
            q.Exit("agachar");
            Assert.AreEqual(0, q.Pending, "Saiu da área antes de aparecer: sai da fila");
            q.Exit("pular");
            Assert.IsNull(q.Current);
        }

        [Test]
        public void GoesAwayAfterSixSeconds()
        {
            var q = new TutorialQueue();
            q.Enter("sombra", false);
            Assert.IsFalse(q.Tick(5f));
            Assert.IsTrue(q.Tick(1.1f));
        }

        [Test]
        public void SameHintTwice_IsOneHint()
        {
            var q = new TutorialQueue();
            q.Enter("parede", false);
            q.Enter("parede", false);
            Assert.AreEqual(0, q.Pending);
        }

        [Test]
        public void KeyNames_InPortuguese()
        {
            Assert.AreEqual("Espaço", TutorialScroll.Translate("Space"));
            Assert.AreEqual("clique", TutorialScroll.Translate("LMB"));
            Assert.AreEqual("S", TutorialScroll.Translate("S"));
        }
    }
}
