using NUnit.Framework;
using SeteVidas.Enemies;

namespace SeteVidas.Tests
{
    /// <summary>O corpo da serpente sem toca (17/11/2026): uma curva só, do rabo afinando até o pescoço.</summary>
    public class SnakeSpineTests
    {
        [Test]
        public void Ends_ArePinned_TailOnGround_NeckOnHead()
        {
            Assert.AreEqual(-5f, SnakeSpine.Y(-20f, -20f, -5f, 10f, 3f, 3f, 12f, 1.3f), 1e-3f);
            Assert.AreEqual(3f, SnakeSpine.Y(10f, -20f, -5f, 10f, 3f, 3f, 12f, 1.3f), 1e-3f);
        }

        [Test]
        public void Tail_Tapers_ToAPoint()
        {
            Assert.AreEqual(0, SnakeSpine.Half(0.5f, 0f));
            Assert.AreEqual(1, SnakeSpine.Half(3.5f, 0f));
            Assert.AreEqual(2, SnakeSpine.Half(20f, 0f));
        }

        [Test]
        public void Warn_Coils_Strike_Straightens()
        {
            Assert.Greater(SnakeSpine.Amplitude(0f, 1f), SnakeSpine.Amplitude(0f, 0f), "enrola no aviso");
            Assert.Less(SnakeSpine.Wavelength(0f, 1f), SnakeSpine.Wavelength(0f, 0f), "S mais apertado");
            Assert.Less(SnakeSpine.Amplitude(1f, 0f), SnakeSpine.Amplitude(0f, 0f), "esticada no bote, mais reta");
            Assert.Greater(SnakeSpine.Wavelength(1f, 0f), SnakeSpine.Wavelength(0f, 0f));
        }

        [Test]
        public void Wave_Moves_WithThePhase()
        {
            float a = SnakeSpine.Y(-5f, -20f, 0f, 10f, 0f, 2f, 14f, 0f);
            float b = SnakeSpine.Y(-5f, -20f, 0f, 10f, 0f, 2f, 14f, 1.5f);
            Assert.Greater(System.Math.Abs(a - b), 0.1f);
        }

        [Test]
        public void NoStep_BiggerThan2px_BetweenColumns()
        {
            // o pior caso (enrolada): a curva não salta mais de 2 px de uma coluna para a outra (o desenho fecha o degrau)
            float amp = SnakeSpine.Amplitude(0f, 1f), wave = SnakeSpine.Wavelength(0f, 1f);
            float prev = SnakeSpine.Y(-26f, -26f, 0f, 2f, 3f, amp, wave, 0.7f);
            for (float x = -25f; x <= 2f; x += 1f)
            {
                float y = SnakeSpine.Y(x, -26f, 0f, 2f, 3f, amp, wave, 0.7f);
                Assert.LessOrEqual(System.Math.Abs(y - prev), 2.6f, $"x {x}");
                prev = y;
            }
        }
    }
}
