using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Varredura do controle: o gato não vira sozinho no analógico (retorno da mola, diagonal de agachar).</summary>
    public class FacingFilterTests
    {
        const float Frame = 1f / 60f;

        static int Run(FacingFilter f, Vector2 move, bool analog, float seconds)
        {
            int last = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Frame) last = f.Step(move, analog, Frame);
            return last;
        }

        [Test]
        public void Keyboard_TurnsImmediately()
        {
            var f = new FacingFilter();
            Assert.AreEqual(-1, f.Step(new Vector2(-1f, 0f), false, Frame));
            Assert.AreEqual(1, f.Step(new Vector2(1f, 0f), false, Frame));
            Assert.AreEqual(0, f.Step(Vector2.zero, false, Frame));
        }

        [Test]
        public void StickSnapback_DoesNotTurnTheCat()
        {
            var f = new FacingFilter();
            Assert.AreEqual(1, Run(f, new Vector2(1f, 0f), true, 0.2f), "Segurando para a direita: olha para a direita");
            // soltou: a mola passa do centro para a esquerda por 2 quadros
            Assert.AreEqual(0, f.Step(new Vector2(-0.6f, 0f), true, Frame));
            Assert.AreEqual(0, f.Step(new Vector2(-0.3f, 0f), true, Frame));
            Assert.AreEqual(0, f.Step(Vector2.zero, true, Frame), "O retorno da mola não vira o gato");
        }

        [Test]
        public void FirmPush_TurnsAfterAnInstant()
        {
            var f = new FacingFilter();
            Assert.AreEqual(0, f.Step(new Vector2(-1f, 0f), true, Frame), "No primeiro quadro ainda não");
            Assert.AreEqual(-1, Run(f, new Vector2(-1f, 0f), true, FacingFilter.StrongHold), "Em ~0,04 s vira");
        }

        [Test]
        public void GentlePush_TurnsOnlyIfHeld()
        {
            var f = new FacingFilter();
            Assert.AreEqual(0, Run(f, new Vector2(-0.3f, 0f), true, 0.1f));
            Assert.AreEqual(-1, Run(f, new Vector2(-0.3f, 0f), true, 0.1f), "Segurando o empurrão leve, vira");
        }

        [TestCase(0.3f, -0.95f)]   // agachar com um pouco de diagonal
        [TestCase(-0.45f, 0.9f)]   // escalar com um pouco de diagonal
        public void MostlyVerticalStick_DoesNotTurn(float x, float y)
        {
            var f = new FacingFilter();
            Assert.AreEqual(0, Run(f, new Vector2(x, y), true, 1f));
        }

        [Test]
        public void DiagonalRun_StillTurns()
        {
            var f = new FacingFilter();
            Assert.AreEqual(-1, Run(f, new Vector2(-0.71f, -0.7f), true, 0.1f));
        }
    }
}
