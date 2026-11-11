using NUnit.Framework;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 3: o inimigo sente o dano (peso do golpe, interrupção, recuo).</summary>
    public class HitReactionRulesTests
    {
        [TestCase(DamageType.Normal, AttackAngle.Front, HitWeight.Light)]
        [TestCase(DamageType.Normal, AttackAngle.Back, HitWeight.Strong)]
        [TestCase(DamageType.Normal, AttackAngle.StealthBack, HitWeight.Strong)]
        [TestCase(DamageType.Heavy, AttackAngle.Front, HitWeight.Heavy)]
        [TestCase(DamageType.Heavy, AttackAngle.StealthBack, HitWeight.Heavy)]
        public void Weight_ByWeaponAndAngle(DamageType type, AttackAngle angle, HitWeight expected)
        {
            Assert.AreEqual(expected, HitReactionRules.WeightOf(type, angle));
        }

        [Test]
        public void LightHit_InterruptsOnlyEarlyInTheWindup()
        {
            Assert.IsTrue(HitReactionRules.Interrupts(HitWeight.Light, true, 0.3f, false, 0.5f));
            Assert.IsFalse(HitReactionRules.Interrupts(HitWeight.Light, true, 0.7f, false, 0.5f), "Passou de 50%: o ataque sai");
            Assert.IsFalse(HitReactionRules.Interrupts(HitWeight.Light, false, 1f, false, 0.5f), "Já golpeando: não para");
        }

        [Test]
        public void StrongAndHeavy_AlwaysInterrupt_ButNeverTheBoss()
        {
            Assert.IsTrue(HitReactionRules.Interrupts(HitWeight.Strong, false, 1f, false, 0.5f));
            Assert.IsTrue(HitReactionRules.Interrupts(HitWeight.Heavy, true, 0.9f, false, 0.5f));
            Assert.IsFalse(HitReactionRules.Interrupts(HitWeight.Heavy, true, 0.1f, true, 0.5f), "O chefe nunca é interrompido");
        }

        [Test]
        public void Knockback_FollowsTheHit()
        {
            Assert.AreEqual(1, HitReactionRules.KnockbackSide(new Vector2(0.9f, 0.1f), -1));
            Assert.AreEqual(-1, HitReactionRules.KnockbackSide(new Vector2(-1f, 0f), 1));
            Assert.AreEqual(-1, HitReactionRules.KnockbackSide(Vector2.up, 1), "Sem lado: para trás do alvo");
        }

        [Test]
        public void Table_HeavierHitsPushMore_AndOnlyHammerShakes()
        {
            var t = new HitReactionTable();
            Assert.Less(t.Light.Knockback, t.Strong.Knockback);
            Assert.Less(t.Strong.Knockback, t.Heavy.Knockback);
            Assert.AreEqual(0f, t.Light.ShakePx);
            Assert.Greater(t.Heavy.ShakePx, 0f);
            Assert.AreEqual(0.5f, HitReactionTable.Boss().KnockbackScale, 1e-4f, "O chefe recua só metade");
        }

        [Test]
        public void Squash_IsWiderAndShorter()
        {
            var s = HitReactionRules.SquashScale(1, 16f, 24f);
            Assert.Greater(s.x, 1f);
            Assert.Less(s.y, 1f);
            Assert.AreEqual(Vector2.one, HitReactionRules.SquashScale(0, 16f, 24f));
        }
    }
}
