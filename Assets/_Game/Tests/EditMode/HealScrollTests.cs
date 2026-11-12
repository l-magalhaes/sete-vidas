using NUnit.Framework;
using SeteVidas.UI;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 11: pergaminho da cura com a pata de luz e lacres de cera.</summary>
    public class HealScrollTests
    {
        static int Count(string[,] px, string hex)
        {
            int n = 0;
            foreach (var c in px) if (c == hex) n++;
            return n;
        }

        [Test]
        public void Scroll_HasTheGoldenPaw_OnlyWhileThereAreUses()
        {
            Assert.Greater(Count(HealScrollPixels.DrawScroll(false), "#e0b04a"), 8, "A pata de luz dourada");
            Assert.AreEqual(0, Count(HealScrollPixels.DrawScroll(true), "#e0b04a"), "Sem usos: desbotado, sem o dourado");
        }

        [Test]
        public void Seal_LitOrSpent()
        {
            Assert.AreEqual(1, Count(HealScrollPixels.DrawSeal(true), "#ffd34d"), "Lacre aceso: o pingo dourado no meio");
            Assert.AreEqual(0, Count(HealScrollPixels.DrawSeal(false), "#ffd34d"), "Lacre gasto: apagado");
            Assert.AreEqual(0, Count(HealScrollPixels.DrawSeal(false), "#a8323a"), "Sem a cera vermelha");
        }
    }
}
