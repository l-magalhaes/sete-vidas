using NUnit.Framework;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>Objetos barulhentos: quando cada tipo faz barulho.</summary>
    public class NoisyObjectRulesTests
    {
        static bool Emits(NoisyObjectKind kind, bool crouching, bool grounded, float speed, float sinceLast = 10f, bool spent = false) =>
            NoisyObjectRules.ShouldEmit(NoisyObjectRules.Preset(kind), crouching, grounded, speed, sinceLast, spent);

        [Test]
        public void Glass_MakesNoise_EvenCrouched_ButNotStandingStillOrInTheAir()
        {
            Assert.IsTrue(Emits(NoisyObjectKind.Glass, crouching: true, grounded: true, speed: 0.5f), "agachado andando");
            Assert.IsTrue(Emits(NoisyObjectKind.Glass, crouching: false, grounded: true, speed: 1f), "correndo");
            Assert.IsFalse(Emits(NoisyObjectKind.Glass, crouching: false, grounded: true, speed: 0f), "parado em cima");
            Assert.IsFalse(Emits(NoisyObjectKind.Glass, crouching: false, grounded: false, speed: 1f), "pulando por cima");
        }

        [Test]
        public void DryLeaves_OnlyWhenRunning()
        {
            Assert.IsTrue(Emits(NoisyObjectKind.DryLeaves, false, true, 1f));
            Assert.IsFalse(Emits(NoisyObjectKind.DryLeaves, false, true, 0.4f), "andando devagar");
            Assert.IsFalse(Emits(NoisyObjectKind.DryLeaves, true, true, 0.5f), "agachado");
        }

        [Test]
        public void Chimes_RingStanding_OrInTheAir_NotCrouched()
        {
            Assert.IsTrue(Emits(NoisyObjectKind.Chimes, false, true, 0f), "encostou de pé");
            Assert.IsTrue(Emits(NoisyObjectKind.Chimes, false, false, 0.5f), "pulando através");
            Assert.IsFalse(Emits(NoisyObjectKind.Chimes, true, true, 0.5f), "agachado passa por baixo");
        }

        [Test]
        public void Bucket_FallsOnce()
        {
            Assert.IsTrue(Emits(NoisyObjectKind.Bucket, false, true, 1f));
            Assert.IsFalse(Emits(NoisyObjectKind.Bucket, false, true, 1f, spent: true), "já caiu");
        }

        [Test]
        public void Cooldown_SpacesTheNoises()
        {
            Assert.IsFalse(Emits(NoisyObjectKind.Glass, false, true, 1f, sinceLast: 0.1f));
            Assert.IsTrue(Emits(NoisyObjectKind.Glass, false, true, 1f, sinceLast: 0.6f));
        }
    }
}
