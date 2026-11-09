using NUnit.Framework;
using SeteVidas.Enemies.Boss;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Chefão Golden: escolha de golpes e relâmpagos sempre com lugar seguro.</summary>
    public class BossRulesTests
    {
        static BossSituation Situation(float distance, float hp = 1f, bool heal = false, bool lightning = false) => new BossSituation
        {
            DistanceToPlayer = distance, HealthFraction = hp, HealReady = heal, LightningReady = lightning,
            SwingRange = 1.8f, HealBelowFraction = 0.7f,
        };

        [Test]
        public void Pick_CloseUp_SwingsMostly()
        {
            Assert.AreEqual(BossMove.Swing, BossRules.Pick(Situation(1f), 0.2f));
            Assert.AreEqual(BossMove.Leap, BossRules.Pick(Situation(1f), 0.9f), "Às vezes salta para trás do gato");
        }

        [Test]
        public void Pick_FarAway_Leaps()
        {
            Assert.AreEqual(BossMove.Leap, BossRules.Pick(Situation(8f), 0.9f));
        }

        [Test]
        public void Pick_HealsOnlyWhenHurtAndReady()
        {
            Assert.AreEqual(BossMove.Heal, BossRules.Pick(Situation(3f, hp: 0.5f, heal: true), 0.1f));
            Assert.AreNotEqual(BossMove.Heal, BossRules.Pick(Situation(3f, hp: 0.9f, heal: true), 0.1f), "Com vida alta não cura");
            Assert.AreNotEqual(BossMove.Heal, BossRules.Pick(Situation(3f, hp: 0.5f, heal: false), 0.1f), "Cura em recarga");
        }

        [Test]
        public void Pick_LightningWhenReadyAndPlayerKeepsDistance()
        {
            Assert.AreEqual(BossMove.Lightning, BossRules.Pick(Situation(6f, lightning: true), 0.9f));
        }

        [Test]
        public void Lightning_AlwaysLeavesASafeGap_AndHitsWhereThePlayerStands()
        {
            const float min = 180f, max = 211f, width = 1.4f, gap = 2.5f;
            var rng = new System.Random(7);
            for (int run = 0; run < 200; run++)
            {
                float playerX = min + (float)rng.NextDouble() * (max - min);
                var cols = BossRules.LightningColumns(min, max, 5, width, gap, playerX, () => (float)rng.NextDouble());

                Assert.IsNotEmpty(cols);
                Assert.IsFalse(BossRules.IsClear(cols, width, playerX - 0.1f, playerX + 0.1f),
                    $"Deveria cair um raio onde o gato está (x {playerX:0.0}): ele precisa se mexer");

                // Existe uma faixa livre de pelo menos 'gap' tiles dentro da arena
                bool found = false;
                for (float a = min; a + gap <= max && !found; a += 0.1f)
                    found = BossRules.IsClear(cols, width, a, a + gap);
                Assert.IsTrue(found, $"Sem lugar seguro de {gap} tiles (rodada {run})");

                foreach (float x in cols) Assert.That(x, Is.InRange(min, max));
            }
        }

        [Test]
        public void Lightning_TinyArena_NoStrikes()
        {
            var cols = BossRules.LightningColumns(0f, 2f, 3, 1.4f, 2.5f, 1f, () => 0.5f);
            Assert.IsEmpty(cols, "Arena menor que a faixa segura: não pode haver raios");
        }

        [Test]
        public void DefaultSettings_FairForTheCat()
        {
            var s = ScriptableObject.CreateInstance<GoldenBossSettings>();
            try
            {
                Assert.GreaterOrEqual(s.LightningTelegraph, 1f, "Aviso dos raios curto demais para reagir");
                Assert.GreaterOrEqual(s.LeapWindup, 0.4f, "Aviso do salto curto demais");
                Assert.Greater(s.LeapHeight, 1.375f + 1f, "O gato (1,375 tile) precisa passar por baixo do salto com folga");
                Assert.GreaterOrEqual(s.SwingWindup, 0.45f);
                Assert.Greater(s.HealInterruptDamage, 0);
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }
    }
}
