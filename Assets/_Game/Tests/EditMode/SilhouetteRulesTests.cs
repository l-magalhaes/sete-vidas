using NUnit.Framework;
using SeteVidas.Player;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 1: a silhueta entra e sai em 0,2 s.</summary>
    public class SilhouetteRulesTests
    {
        [Test]
        public void FadesInAndOut_InFadeTime()
        {
            float a = 0f;
            for (int i = 0; i < 10; i++) a = ShadowSilhouette.Step(a, true, 0.02f, 0.2f);
            Assert.AreEqual(1f, a, 1e-4f, "0,2 s na sombra: silhueta inteira");
            a = ShadowSilhouette.Step(a, false, 0.1f, 0.2f);
            Assert.AreEqual(0.5f, a, 1e-4f, "Metade do caminho de volta");
            a = ShadowSilhouette.Step(a, false, 0.2f, 0.2f);
            Assert.AreEqual(0f, a, 1e-4f);
        }
    }
}
