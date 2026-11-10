using NUnit.Framework;
using SeteVidas.Enemies;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Cão Sentinela Atirador (Docs/Design/atirador.md): visão inclinada para baixo e pose de mira.</summary>
    public class CrossbowRulesTests
    {
        [Test]
        public void TiltedCone_SeesTheFloorBelow_ThatAStraightConeMisses()
        {
            var eye = new Vector2(0f, 7f);
            var below = new Vector2(4f, 0.7f); // ~57° abaixo
            Assert.IsFalse(PerceptionMath.InVisionCone(eye, 1, below, 11f, 35f, 0f), "Cone reto (35°) não vê o chão lá embaixo");
            Assert.IsTrue(PerceptionMath.InVisionCone(eye, 1, below, 11f, 50f, 30f), "Inclinado 30° para baixo, vê");
            Assert.IsFalse(PerceptionMath.InVisionCone(eye, 1, new Vector2(-4f, 0.7f), 11f, 50f, 30f), "Nunca vê para trás");
        }

        [Test]
        public void TiltZero_IsTheOldCone()
        {
            var eye = Vector2.zero;
            for (int i = 0; i < 20; i++)
            {
                var target = new Vector2(Mathf.Cos(i * 0.4f), Mathf.Sin(i * 0.4f)) * 5f;
                Assert.AreEqual(PerceptionMath.InVisionCone(eye, 1, target, 8f, 35f),
                                PerceptionMath.InVisionCone(eye, 1, target, 8f, 35f, 0f));
            }
        }

        [Test]
        public void AngleBelow_AndAimPose()
        {
            Assert.AreEqual(0f, PerceptionMath.AngleBelowHorizontal(Vector2.zero, new Vector2(5f, 0f)), 0.01f);
            Assert.AreEqual(45f, PerceptionMath.AngleBelowHorizontal(Vector2.zero, new Vector2(-3f, -3f)), 0.01f, "Vale para os dois lados");
            Assert.Less(PerceptionMath.AngleBelowHorizontal(Vector2.zero, new Vector2(3f, 1f)), 0f, "Acima = negativo");
            Assert.AreEqual(0, PerceptionMath.AimBucket(10f));
            Assert.AreEqual(30, PerceptionMath.AimBucket(30f));
            Assert.AreEqual(60, PerceptionMath.AimBucket(62f));
            Assert.AreEqual(0, PerceptionMath.AimBucket(-15f), "Mirando para cima usa a pose reta");
        }
    }
}
