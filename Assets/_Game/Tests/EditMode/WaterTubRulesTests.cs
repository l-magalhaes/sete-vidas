using NUnit.Framework;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>Tina de água: o gato passa por ela; andando, correndo ou caindo em cima, ela tomba e derrama; agachado, não.</summary>
    public class WaterTubRulesTests
    {
        const float Move = 6f, Fraction = 0.6f, Fall = 8f;

        [Test]
        public void Running_TipsIt()
        {
            Assert.IsTrue(WaterTub.Tips(6f, 0f, Move, false, Fraction, Fall));
            Assert.IsTrue(WaterTub.Tips(-4f, 0f, Move, false, Fraction, Fall), "Para os dois lados");
        }

        [Test]
        public void WalkingSlowly_OrCrouched_PassesWithoutTipping()
        {
            Assert.IsFalse(WaterTub.Tips(2f, 0f, Move, false, Fraction, Fall), "Bem devagar (analógico leve): fica em pé");
            Assert.IsFalse(WaterTub.Tips(6f, 0f, Move, true, Fraction, Fall), "Agachado: passa sem derrubar");
        }

        [Test]
        public void FallingOnTop_TipsIt()
        {
            Assert.IsTrue(WaterTub.Tips(0f, -10f, Move, true, Fraction, Fall));
            Assert.IsFalse(WaterTub.Tips(0f, -3f, Move, false, Fraction, Fall), "Pisou de leve: fica em pé");
        }
    }
}
