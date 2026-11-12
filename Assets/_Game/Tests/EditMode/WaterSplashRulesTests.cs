using NUnit.Framework;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 2: o respingo depende da velocidade da queda.</summary>
    public class WaterSplashRulesTests
    {
        [Test]
        public void StrongFall_BigSplash_ShortFall_Small_WalkingIn_None()
        {
            Assert.AreEqual(2, WaterSurface.SplashFor(-15f, 12f, 2f), "Salto de fé: respingo grande");
            Assert.AreEqual(1, WaterSurface.SplashFor(-6f, 12f, 2f), "Pulo ou queda curta: pequeno");
            Assert.AreEqual(0, WaterSurface.SplashFor(-0.5f, 12f, 2f), "Entrou andando pela beira: só a ondinha");
        }
    }
}
