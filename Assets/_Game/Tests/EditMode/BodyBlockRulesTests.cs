using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 6: esbarrar nos inimigos (parar ao encostar, sair de lado, reação da IA).</summary>
    public class BodyBlockRulesTests
    {
        static Rect Cat(float x, float y = 0f) => new Rect(x - 0.375f, y, 0.75f, 1.375f);
        static readonly Rect Guard = new Rect(4.625f, 0f, 0.75f, 1.6f); // centro em x 5

        [Test]
        public void WalkingTowardBody_StopsAtContact()
        {
            const float dt = 0.02f;
            var cat = Cat(4f); // borda direita em 4,375; vão de 0,25 até o corpo
            float vx = BodyBlockRules.ClampVelocity(cat, Guard, 20f, dt);
            Assert.AreEqual(0.25f / dt, vx, 1e-3f, "Chega até encostar, não passa");
            Assert.AreEqual(0f, BodyBlockRules.ClampVelocity(Cat(4.25f), Guard, 5f, dt), 1e-4f, "Encostado: para");
        }

        [Test]
        public void WalkingAway_IsNotBlocked()
        {
            Assert.AreEqual(-5f, BodyBlockRules.ClampVelocity(Cat(4.25f), Guard, -5f, 0.02f));
            Assert.AreEqual(5f, BodyBlockRules.ClampVelocity(Cat(5.75f), Guard, 5f, 0.02f));
        }

        [Test]
        public void JumpingOverTheHead_DoesNotOverlap()
        {
            Assert.IsFalse(BodyBlockRules.OverlapsVertically(Cat(5f, 1.6f), Guard), "Pés acima da cabeça: passando por cima");
            Assert.IsTrue(BodyBlockRules.OverlapsVertically(Cat(5f, 0f), Guard));
            Assert.IsTrue(BodyBlockRules.OverlapsVertically(Cat(5f, 1.3f), Guard), "Pés abaixo do topo: ainda esbarra");
        }

        [TestCase(5.2f, true, true, 1)]
        [TestCase(4.8f, true, true, -1)]
        [TestCase(5.2f, true, false, -1)]   // parede ou espinho à direita: sai pela esquerda
        [TestCase(5.2f, false, false, 0)]   // os dois lados fechados: não empurra
        public void PushOut_PrefersOwnSide_ButNeverIntoWalls(float catX, bool leftFree, bool rightFree, int expected)
        {
            Assert.AreEqual(expected, BodyBlockRules.PushOutDirection(catX, 5f, leftFree, rightFree));
        }

        [Test]
        public void FrontBump_DependsOnFacing()
        {
            Assert.IsTrue(BodyBlockRules.IsFrontBump(5f, 1, 6f));
            Assert.IsFalse(BodyBlockRules.IsFrontBump(5f, -1, 6f));
        }

        [Test]
        public void BumpFromFront_EntersCombat()
        {
            var brain = new AlertBrain();
            brain.OnBumped(new Vector2(6f, 0f), fromFront: true, AlertTuning.Default);
            Assert.AreEqual(AlertState.Combat, brain.State);
        }

        [Test]
        public void BumpFromBehind_TurnsSuspicious_ThenInvestigates()
        {
            var brain = new AlertBrain();
            var t = AlertTuning.Default;
            brain.OnBumped(new Vector2(4f, 0f), fromFront: false, t);
            Assert.AreEqual(AlertState.Suspicious, brain.State);
            Assert.AreEqual(new Vector2(4f, 0f), brain.LastKnownPosition, "Vira para onde sentiu o esbarrão");
            for (float s = 0f; s < t.SuspiciousTime + 0.1f; s += 0.02f) brain.Tick(0.02f, t);
            Assert.AreEqual(AlertState.Investigating, brain.State, "Sem ver ninguém, vai investigar o esbarrão");
        }

        [Test]
        public void Bump_DoesNothingToTheDead()
        {
            var brain = new AlertBrain();
            brain.Kill();
            brain.OnBumped(Vector2.zero, true, AlertTuning.Default);
            Assert.AreEqual(AlertState.Dead, brain.State);
        }
    }
}
