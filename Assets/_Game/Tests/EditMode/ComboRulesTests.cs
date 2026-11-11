using NUnit.Framework;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 4: 3º golpe da adaga com avanço e o terremoto da marreta.</summary>
    public class ComboRulesTests
    {
        [Test]
        public void Finisher_IsTheLastHit_OfACombo()
        {
            Assert.IsTrue(ComboRules.IsFinisher(2, 3));
            Assert.IsFalse(ComboRules.IsFinisher(1, 3));
            Assert.IsFalse(ComboRules.IsFinisher(0, 1), "A marreta (1 golpe) não tem golpe final com avanço");
        }

        [TestCase(2, 3)]   // de frente
        [TestCase(3, 5)]   // pelas costas
        [TestCase(5, 8)]   // furtivo
        public void FinisherDamage_IsOneAndAHalf_RoundedUp(int amount, int expected)
        {
            Assert.AreEqual(expected, ComboRules.FinisherDamage(amount, 1.5f));
        }

        [Test]
        public void Lunge_StopsAtWalls_AndAtEdges()
        {
            const float dt = 0.02f, speed = 12.5f;
            Assert.AreEqual(0.25f, ComboRules.LungeStep(1.5f, speed, dt, 10f, true), 1e-4f, "Livre: anda a velocidade");
            Assert.AreEqual(0.1f, ComboRules.LungeStep(1.5f, speed, dt, 0.1f, true), 1e-4f, "Parede perto: só até encostar");
            Assert.AreEqual(0f, ComboRules.LungeStep(1.5f, speed, dt, 10f, false), "Sem chão à frente: para na borda");
            Assert.AreEqual(0.05f, ComboRules.LungeStep(0.05f, speed, dt, 10f, true), 1e-4f, "Não passa do total");
        }

        [Test]
        public void Lunge_TotalDistance_IsTheSetting()
        {
            float left = 1.5f, moved = 0f;
            for (int i = 0; i < 100 && left > 0f; i++)
            {
                float step = ComboRules.LungeStep(left, 1.5f / 0.12f, 0.02f, 99f, true);
                moved += step;
                left -= step;
            }
            Assert.AreEqual(1.5f, moved, 1e-3f);
        }

        [Test]
        public void Charge_StartsAfterHolding_AndFillsUntilFull()
        {
            Assert.IsFalse(ChargeRules.IsCharge(0.3f, 0.4f), "Soltou antes de 0,4 s: golpe normal");
            Assert.IsTrue(ChargeRules.IsCharge(0.4f, 0.4f));
            Assert.AreEqual(0f, ChargeRules.Charge01(0.4f, 0.4f, 1.2f), 1e-4f);
            Assert.AreEqual(0.5f, ChargeRules.Charge01(0.8f, 0.4f, 1.2f), 1e-4f);
            Assert.AreEqual(1f, ChargeRules.Charge01(3f, 0.4f, 1.2f), 1e-4f);
        }

        [Test]
        public void Shockwave_RangeAndDamage_GrowWithCharge()
        {
            var range = new Vector2(3f, 5f);
            var damage = new Vector2Int(3, 5);
            Assert.AreEqual(3f, ChargeRules.Range(0f, range), 1e-4f);
            Assert.AreEqual(5f, ChargeRules.Range(1f, range), 1e-4f);
            Assert.AreEqual(3, ChargeRules.Damage(0f, damage));
            Assert.AreEqual(5, ChargeRules.Damage(1f, damage));
        }

        [Test]
        public void Shockwave_HitsOnlyTargetsStandingOnTheSameFloor()
        {
            var origin = new Vector2(10f, 0f);
            Assert.IsTrue(ChargeRules.InShockwave(origin, 5f, new Rect(14f, 0f, 0.75f, 1.6f)), "A 4 tiles, no chão");
            Assert.IsTrue(ChargeRules.InShockwave(origin, 5f, new Rect(5.5f, 0f, 0.75f, 1.6f)), "Do outro lado também");
            Assert.IsFalse(ChargeRules.InShockwave(origin, 5f, new Rect(16f, 0f, 0.75f, 1.6f)), "A 6 tiles, fora");
            Assert.IsFalse(ChargeRules.InShockwave(origin, 5f, new Rect(12f, 2f, 0.75f, 1.6f)), "No ar ou numa plataforma, fora");
        }
    }
}
