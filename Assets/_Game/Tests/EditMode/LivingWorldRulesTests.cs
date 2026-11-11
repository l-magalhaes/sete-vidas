using NUnit.Framework;
using SeteVidas.World.Scenery;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Etapa 7 (mundo vivo): as regras puras dos pulsos, das partículas e da reação dos bichos.</summary>
    public class LivingWorldRulesTests
    {
        [Test]
        public void Pulse_StaysBetweenMinAndMax()
        {
            for (float t = 0f; t < 10f; t += 0.1f)
            {
                float a = PulseLight.Value(t, 2.5f, 0.3f, 0.6f, 1f);
                Assert.GreaterOrEqual(a, 0.6f - 1e-4f);
                Assert.LessOrEqual(a, 1f + 1e-4f);
            }
        }

        [Test]
        public void RunesAndVeins_PulseOutOfTime()
        {
            // meia volta de diferença: quando uma está no máximo, a outra está no mínimo
            float rune = PulseLight.Value(0.625f, 2.5f, 0f, 0.6f, 1f), vein = PulseLight.Value(0.625f, 2.5f, 0.5f, 0.6f, 1f);
            Assert.AreEqual(1f, rune, 0.01f);
            Assert.AreEqual(0.6f, vein, 0.01f);
        }

        [Test]
        public void FailingRunes_GoOutRarely()
        {
            int off = 0, n = 0;
            for (float t = 0f; t < 37f; t += 0.01f) { n++; if (PulseLight.FailsAt(t, 0.2f)) off++; }
            Assert.Greater(off, 0, "falham de vez em quando");
            Assert.Less(off / (float)n, 0.06f, "mas quase sempre estão acesas");
        }

        [Test]
        public void Particles_FadeInAndOut()
        {
            Assert.AreEqual(0f, AmbientEmitter.Fade(0f, 8f, 1.5f, 1.5f));
            Assert.AreEqual(1f, AmbientEmitter.Fade(4f, 8f, 1.5f, 1.5f));
            Assert.AreEqual(0f, AmbientEmitter.Fade(8f, 8f, 1.5f, 1.5f), 1e-4f);
            Assert.LessOrEqual(AmbientEmitter.GlobalCap, 40, "orçamento por tela (6.1)");
        }

        [Test]
        public void FleeingBird_DisappearsOnlyOffscreen()
        {
            Assert.IsFalse(ReactOnProximity.Offscreen(new Vector2(10f, 5f), Vector2.zero, 1f), "ainda na tela");
            Assert.IsTrue(ReactOnProximity.Offscreen(new Vector2(16.5f, 0f), Vector2.zero, 1f), "saiu pela lateral");
            Assert.IsTrue(ReactOnProximity.Offscreen(new Vector2(0f, 10f), Vector2.zero, 1f), "saiu por cima");
        }

        [Test]
        public void Rat_DisappearsOnlyAtTheHole()
        {
            Assert.IsFalse(ReactOnProximity.ReachedHideout(214.5f, 215.6f), "ainda correndo");
            Assert.IsTrue(ReactOnProximity.ReachedHideout(215.55f, 215.6f), "chegou ao buraco");
        }

        [Test]
        public void Reaction_StartsInsideTheRadius()
        {
            Assert.IsTrue(ReactOnProximity.Near(Vector2.zero, new Vector2(2.9f, 0f), 3f));
            Assert.IsFalse(ReactOnProximity.Near(Vector2.zero, new Vector2(3.1f, 0f), 3f));
        }
    }
}
