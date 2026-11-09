using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Armadilhas: ciclo dos jatos de chama, placa de pressão e altura do fio de alarme.</summary>
    public class TrapRulesTests
    {
        [Test]
        public void Phase_CyclesOffWarningOn()
        {
            // off 1, aviso 0,5, ligado 2 (ciclo de 3,5 s)
            Assert.AreEqual(TrapPhase.Off, TrapRules.PhaseAt(0.5f, 0f, 1f, 0.5f, 2f));
            Assert.AreEqual(TrapPhase.Warning, TrapRules.PhaseAt(1.2f, 0f, 1f, 0.5f, 2f));
            Assert.AreEqual(TrapPhase.On, TrapRules.PhaseAt(2f, 0f, 1f, 0.5f, 2f));
            Assert.AreEqual(TrapPhase.Off, TrapRules.PhaseAt(3.6f, 0f, 1f, 0.5f, 2f), "Recomeça");
        }

        [Test]
        public void Phase_OffsetMakesAWave()
        {
            // Mesmo instante, deslocamentos diferentes: jatos vizinhos em fases diferentes
            var a = TrapRules.PhaseAt(0.5f, 0f, 1f, 0.5f, 2f);
            var b = TrapRules.PhaseAt(0.5f, 0.7f, 1f, 0.5f, 2f);
            Assert.AreEqual(TrapPhase.Off, a);
            Assert.AreEqual(TrapPhase.Warning, b);
        }

        [Test]
        public void Plate_OnlyStandingStepsTrigger()
        {
            Assert.IsTrue(TrapRules.PlateTriggers(grounded: true, crouching: false));
            Assert.IsFalse(TrapRules.PlateTriggers(grounded: true, crouching: true), "Agachado: passos leves");
            Assert.IsFalse(TrapRules.PlateTriggers(grounded: false, crouching: false), "Pulando por cima");
        }

        [Test]
        public void Wire_SitsBetweenCrouchAndStandingHeight()
        {
            var s = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                const float wireBottom = 1.05f, wireTop = 1.15f; // valores do prefab Tripwire
                Assert.IsFalse(TrapRules.WireTouches(0f, s.CrouchHeight, wireBottom, wireTop), "Agachado passa por baixo");
                Assert.IsTrue(TrapRules.WireTouches(0f, s.StandingHeight, wireBottom, wireTop), "De pé esbarra");
                Assert.IsFalse(TrapRules.WireTouches(1.3f, 1.3f + s.StandingHeight, wireBottom, wireTop), "Pulando alto passa por cima");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void DangerRegistry_MarginAroundAreas()
        {
            System.Func<Rect> area = () => new Rect(10f, 0f, 2f, 1f);
            TrapRules.RegisterDanger(area);
            try
            {
                Assert.IsTrue(TrapRules.IsNearDanger(new Vector2(11f, 0.5f), 0f));
                Assert.IsTrue(TrapRules.IsNearDanger(new Vector2(13f, 0.5f), 1.2f), "1 tile ao lado, margem 1,2: perigoso");
                Assert.IsFalse(TrapRules.IsNearDanger(new Vector2(14f, 0.5f), 1.2f));
            }
            finally
            {
                TrapRules.UnregisterDanger(area);
            }
        }
    }
}
