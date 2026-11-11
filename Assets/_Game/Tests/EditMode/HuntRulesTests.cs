using NUnit.Framework;
using SeteVidas.Enemies;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Caçada: a Sentinela que perdeu o gato no combate lembra o rumo dele e procura por mais tempo.</summary>
    public class HuntRulesTests
    {
        const float Dt = 0.02f;

        static AlertBrain SeenRunningRight(in AlertTuning t)
        {
            var brain = new AlertBrain();
            // o gato passa correndo para a direita à vista dela até ela entrar em combate
            for (int i = 0; i < 100 && brain.State != AlertState.Combat; i++)
            {
                brain.See(new Vector2(5f + i * 0.1f, 0f), 2f, Dt);
                brain.Tick(Dt, t);
            }
            Assert.AreEqual(AlertState.Combat, brain.State);
            return brain;
        }

        static void Wait(AlertBrain brain, float seconds, in AlertTuning t)
        {
            for (float s = 0f; s < seconds; s += Dt) brain.Tick(Dt, t);
        }

        [Test]
        public void RemembersWhichWayTheCatWent()
        {
            var t = AlertTuning.Default;
            Assert.AreEqual(1, SeenRunningRight(t).LastKnownHeading);
        }

        [Test]
        public void LosingTheCatInCombat_StartsAHunt()
        {
            var t = AlertTuning.Default;
            var brain = SeenRunningRight(t);
            Wait(brain, t.LoseSightTime + 0.05f, t);
            Assert.AreEqual(AlertState.Investigating, brain.State);
            Assert.IsTrue(brain.IsHunting);
        }

        [Test]
        public void Hunt_SearchesLonger_AndDoesNotForget()
        {
            var t = AlertTuning.Default;
            var brain = SeenRunningRight(t);
            Wait(brain, t.LoseSightTime + 0.05f, t);
            float suspicion = brain.Suspicion;
            brain.ArrivedAtLastKnownPosition();
            Wait(brain, t.SearchTime + 0.5f, t);
            Assert.AreEqual(AlertState.Investigating, brain.State, "Caçando, procura mais que a busca comum");
            Assert.AreEqual(suspicion, brain.Suspicion, 1e-4f, "A suspeita não esvazia enquanto caça");
            Wait(brain, t.HuntSearchTime, t);
            Assert.AreEqual(AlertState.Patrolling, brain.State, "Depois de procurar, desiste");
            Assert.IsFalse(brain.IsHunting);
        }

        [Test]
        public void NoiseInvestigation_IsNotAHunt()
        {
            var t = AlertTuning.Default;
            var brain = new AlertBrain();
            brain.Hear(new Vector2(3f, 0f), 0.6f);
            Wait(brain, t.SuspiciousTime + 0.1f, t);
            Assert.AreEqual(AlertState.Investigating, brain.State);
            Assert.IsFalse(brain.IsHunting, "Só caça quem já viu o gato no combate");
        }

        [Test]
        public void SeeingTheCatAgain_EndsTheHunt()
        {
            var t = AlertTuning.Default;
            var brain = SeenRunningRight(t);
            Wait(brain, t.LoseSightTime + 0.05f, t);
            for (int i = 0; i < 50 && brain.State != AlertState.Combat; i++) { brain.See(new Vector2(20f, 0f), 2f, Dt); brain.Tick(Dt, t); }
            Assert.AreEqual(AlertState.Combat, brain.State);
            Assert.IsFalse(brain.IsHunting);
        }
    }
}
