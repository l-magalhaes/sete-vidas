using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Rampas: ângulo, o que é andável, velocidade na rampa e folga do sprite.</summary>
    public class SlopeMathTests
    {
        const float MaxAngle = 46f; // valor padrão de PlayerMovementSettings.MaxSlopeAngle

        [TestCase(1f, 1f, 45f)]
        [TestCase(1f, 2f, 26.565f)]
        [TestCase(2f, 1f, 63.435f)]
        [TestCase(1f, -1f, 45f)]
        public void Angle_MatchesRiseOverRun(float rise, float run, float expected)
        {
            Assert.AreEqual(expected, SlopeMath.AngleOf(SlopeMath.NormalFor(rise, run)), 0.01f);
        }

        [Test]
        public void Walkable_OneToOneAndOneToTwo_NotTwoToOne()
        {
            Assert.IsTrue(SlopeMath.IsWalkable(Vector2.up, MaxAngle), "chão plano");
            Assert.IsTrue(SlopeMath.IsWalkable(SlopeMath.NormalFor(1f, 1f), MaxAngle), "1:1 (45°)");
            Assert.IsTrue(SlopeMath.IsWalkable(SlopeMath.NormalFor(1f, -2f), MaxAngle), "1:2 para a esquerda");
            Assert.IsFalse(SlopeMath.IsWalkable(SlopeMath.NormalFor(2f, 1f), MaxAngle), "2:1 (63°) é parede");
            Assert.IsFalse(SlopeMath.IsWalkable(Vector2.right, MaxAngle), "parede");
            Assert.IsFalse(SlopeMath.IsWalkable(Vector2.down, MaxAngle), "teto");
        }

        [Test]
        public void FlatGround_IsNotSlope()
        {
            Assert.IsFalse(SlopeMath.IsSlope(Vector2.up));
            Assert.IsTrue(SlopeMath.IsSlope(SlopeMath.NormalFor(1f, 2f)));
        }

        [TestCase(1f, 1f, 7f, 7f)]     // subindo 45° para a direita: sobe o mesmo que anda
        [TestCase(1f, 1f, -7f, -7f)]   // descendo a mesma rampa (andando para a esquerda)
        [TestCase(1f, 2f, 7f, 3.5f)]   // 1:2: sobe metade
        [TestCase(1f, -2f, 7f, -3.5f)] // rampa que sobe para a esquerda: andar para a direita desce
        public void VerticalSpeed_KeepsHorizontalSpeed(float rise, float run, float vx, float expectedVy)
        {
            Assert.AreEqual(expectedVy, SlopeMath.VerticalSpeedFor(SlopeMath.NormalFor(rise, run), vx), 0.001f);
        }

        [Test]
        public void VerticalSpeed_OnFlatGround_IsZero()
        {
            Assert.AreEqual(0f, SlopeMath.VerticalSpeedFor(Vector2.up, 7f), 0.0001f);
        }

        [TestCase(1f, 1f, 0.375f)]   // 45°: meia largura do gato (6 px)
        [TestCase(1f, 2f, 0.1875f)]  // 1:2: 3 px
        public void CenterGap_IsHalfWidthTimesTangent(float rise, float run, float expected)
        {
            Assert.AreEqual(expected, SlopeMath.CenterGap(0.375f, SlopeMath.NormalFor(rise, run)), 0.001f);
        }
    }
}
