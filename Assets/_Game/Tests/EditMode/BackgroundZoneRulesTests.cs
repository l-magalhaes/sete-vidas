using NUnit.Framework;
using SeteVidas.World.Scenery;

namespace SeteVidas.Tests
{
    /// <summary>Etapa 6: a escolha do conjunto de fundo pela posição (zonas com sobreposição nas bordas).</summary>
    public class BackgroundZoneRulesTests
    {
        static readonly BackgroundZoneRules.Zone[] Zones =
        {
            new BackgroundZoneRules.Zone("A", 0, 0, 10, 10),
            new BackgroundZoneRules.Zone("B", 8, 0, 20, 10),   // sobrepõe A em x 8..10
        };

        [Test]
        public void Pick_FirstZoneThatContains()
        {
            Assert.AreEqual("A", BackgroundZoneRules.Pick(Zones, null, 2, 5));
            Assert.AreEqual("B", BackgroundZoneRules.Pick(Zones, null, 15, 5));
        }

        [Test]
        public void Pick_KeepsCurrent_InsideTheOverlap()
        {
            Assert.AreEqual("B", BackgroundZoneRules.Pick(Zones, "B", 9, 5), "parado na sobreposição, não fica trocando");
            Assert.AreEqual("A", BackgroundZoneRules.Pick(Zones, "A", 9, 5));
        }

        [Test]
        public void SkyHeightBlend_IsSmooth_AndClamped()
        {
            Assert.AreEqual(0f, SkyHeightBlend.Amount(5f, 10f, 40f));
            Assert.AreEqual(1f, SkyHeightBlend.Amount(50f, 10f, 40f));
            Assert.AreEqual(0.5f, SkyHeightBlend.Amount(25f, 10f, 40f), 0.001f);
            Assert.Less(SkyHeightBlend.Amount(11f, 10f, 40f), 0.02f, "começa devagar: nada de troca brusca");
        }

        [Test]
        public void Pick_OutsideEverything_KeepsCurrent()
        {
            Assert.AreEqual("B", BackgroundZoneRules.Pick(Zones, "B", 50, 50));
        }
    }
}
