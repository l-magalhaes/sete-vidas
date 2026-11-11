using NUnit.Framework;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 5: ataque para baixo no ar (estocada que quica e queda pesada).</summary>
    public class DownAttackRulesTests
    {
        [Test]
        public void OnlyInTheAir_WithDownHeld()
        {
            Assert.IsTrue(DownAttackRules.WantsDownAttack(false, -1f, false), "No ar com baixo no analógico");
            Assert.IsTrue(DownAttackRules.WantsDownAttack(false, 0f, true), "No ar com S");
            Assert.IsFalse(DownAttackRules.WantsDownAttack(true, -1f, true), "No chão é o golpe agachado");
            Assert.IsFalse(DownAttackRules.WantsDownAttack(false, -0.3f, false), "Analógico só um pouco para baixo: golpe normal");
        }

        [Test]
        public void Dagger_BouncesOnTargets_AndOnSpikesAndSaws_HammerNever()
        {
            Assert.IsTrue(DownAttackRules.Bounces(true, true, false));
            Assert.IsTrue(DownAttackRules.Bounces(true, false, true), "Espinho ou serra por cima: quica");
            Assert.IsFalse(DownAttackRules.Bounces(true, false, false), "No vazio: não quica");
            Assert.IsFalse(DownAttackRules.Bounces(false, true, true), "A marreta não quica");
        }

        [Test]
        public void StabBox_IsUnderTheFeet()
        {
            var box = DownAttackRules.StabBox(new Vector2(10f, 5f), new Vector2(0.9f, 0.8f), 0.25f);
            Assert.AreEqual(10f, box.center.x, 1e-4f);
            Assert.Less(box.center.y, 5f);
            Assert.Less(box.yMin, 5f - 0.5f, "Alcança abaixo dos pés antes do corpo encostar");
        }

        [Test]
        public void PogoSurfaces_DetectsSpikesUnderTheStab()
        {
            System.Func<Rect> spikes = () => new Rect(9f, 0f, 3f, 0.5f);
            PogoSurfaces.Register(spikes);
            try
            {
                Assert.IsTrue(PogoSurfaces.Touches(DownAttackRules.StabBox(new Vector2(10f, 0.9f), new Vector2(0.9f, 0.8f), 0.25f)));
                Assert.IsFalse(PogoSurfaces.Touches(DownAttackRules.StabBox(new Vector2(10f, 3f), new Vector2(0.9f, 0.8f), 0.25f)), "Longe demais");
                Assert.IsFalse(PogoSurfaces.Touches(DownAttackRules.StabBox(new Vector2(14f, 0.9f), new Vector2(0.9f, 0.8f), 0.25f)), "Ao lado");
            }
            finally
            {
                PogoSurfaces.Unregister(spikes);
            }
        }
    }
}
