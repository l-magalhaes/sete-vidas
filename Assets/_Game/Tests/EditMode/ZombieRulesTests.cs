using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Player;

namespace SeteVidas.Tests
{
    /// <summary>Templário Zumbi e envenenamento (Docs/Design/zumbi-templario.md).</summary>
    public class ZombieRulesTests
    {
        static int Run(ref PoisonState s, float seconds, ref int hits, out int ticks)
        {
            ticks = 0;
            int lost = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += 0.02f)
                if (PoisonRules.Step(ref s, 0.02f, hits, out int dmg)) { ticks++; hits -= dmg; lost += dmg; }
            return lost;
        }

        [Test]
        public void Poison_TakesHalfAHeartPerSecond_ForThreeSeconds()
        {
            var s = PoisonRules.Apply(default);
            int hits = 4;
            int lost = Run(ref s, 3.05f, ref hits, out int ticks);
            Assert.AreEqual(3, ticks, "3 tiques");
            Assert.AreEqual(3, lost, "meio coração por tique");
            Assert.IsFalse(s.Active, "acabou depois de 3 s");
        }

        [Test]
        public void Poison_FirstTickComesAfterOneSecond()
        {
            var s = PoisonRules.Apply(default);
            int hits = 4;
            Run(ref s, 0.9f, ref hits, out int ticks);
            Assert.AreEqual(0, ticks);
            Run(ref s, 0.15f, ref hits, out ticks);
            Assert.AreEqual(1, ticks);
        }

        [Test]
        public void TouchingAgain_RestartsTheThreeSeconds_WithoutDelayingTheNextTick()
        {
            var s = PoisonRules.Apply(default);
            int hits = 14;
            Run(ref s, 2.5f, ref hits, out _);                  // 2 tiques
            s = PoisonRules.Apply(s);
            Assert.AreEqual(PoisonRules.Ticks, s.TicksLeft, "reiniciou os 3 tiques");
            Run(ref s, 0.6f, ref hits, out int ticks);
            Assert.AreEqual(1, ticks, "o relógio do tique continuou (contato contínuo não trava o veneno)");
            Assert.IsTrue(s.Lethal, "o segundo toque deixa o veneno matar");
        }

        [Test]
        public void PoisonAlone_StopsAtTheLastHalfHeart()
        {
            var s = PoisonRules.Apply(default);
            int hits = 2;
            Run(ref s, 3.5f, ref hits, out int ticks);
            Assert.AreEqual(3, ticks);
            Assert.AreEqual(1, hits, "sozinho, o veneno nunca tira o último meio coração");
        }

        [Test]
        public void TouchedAgain_PoisonKills()
        {
            var s = PoisonRules.Apply(default);
            int hits = 2;
            Run(ref s, 1.05f, ref hits, out _);                 // 1 hit
            s = PoisonRules.Apply(s);
            Run(ref s, 1.05f, ref hits, out _);
            Assert.AreEqual(0, hits, "encostou de novo: o veneno mata");
        }

        [Test]
        public void Zombie_TurnsAtWallsLedgesAndTheEndOfItsPost()
        {
            Assert.IsTrue(ZombieRules.ShouldTurn(true, true, 0f, 1, false, 0f, 0f), "parede");
            Assert.IsTrue(ZombieRules.ShouldTurn(false, false, 0f, 1, false, 0f, 0f), "beira");
            Assert.IsFalse(ZombieRules.ShouldTurn(false, true, 5f, 1, false, 0f, 0f), "sem faixa: segue");
            Assert.IsTrue(ZombieRules.ShouldTurn(false, true, 10f, 1, true, 2f, 10f), "fim da faixa à direita");
            Assert.IsFalse(ZombieRules.ShouldTurn(false, true, 10f, -1, true, 2f, 10f), "voltando, não vira");
        }

        [Test]
        public void Zombie_SeesAhead_SmellsClose_AndBarelyInTheShadow()
        {
            Assert.IsTrue(ZombieRules.Senses(5f, 0f, 1, 6f, 2f, 2f, false), "à frente, perto");
            Assert.IsFalse(ZombieRules.Senses(-5f, 0f, 1, 6f, 2f, 2f, false), "atrás e longe: não vê");
            Assert.IsTrue(ZombieRules.Senses(-1.5f, 0f, 1, 6f, 2f, 2f, false), "atrás e bem perto: o faro");
            Assert.IsFalse(ZombieRules.Senses(3f, 0f, 1, 6f, 2f, 2f, true), "escondido: só bem de perto");
            Assert.IsTrue(ZombieRules.Senses(0.8f, 0f, 1, 6f, 2f, 2f, true));
            Assert.IsFalse(ZombieRules.Senses(2f, 4f, 1, 6f, 2f, 2f, false), "outro andar");
        }

        [Test]
        public void Zombie_IsSlowerThanTheTemplar()
        {
            // 12/11/2026: o usuário achou difícil demais com 5,5; o zumbi agora é lento até perseguindo
            var zombie = UnityEngine.ScriptableObject.CreateInstance<ZombieSettings>();
            var templar = UnityEngine.ScriptableObject.CreateInstance<SeteVidas.Enemies.SentinelSettings>();
            Assert.Less(zombie.ChaseSpeed, templar.ChaseSpeed);
            Assert.Greater(zombie.ChaseSpeed, zombie.WanderSpeed * 2f, "mas acelera ao perseguir");
            Assert.Less(zombie.WanderSpeed, templar.PatrolSpeed, "vagando, arrasta os pés");
        }

        [Test]
        public void BackHit_OnTheZombie_IsDouble_NeverStealth()
        {
            // o zumbi nunca é pego de surpresa: pelas costas é Back, com o multiplicador dele (2×)
            Assert.AreEqual(AttackAngle.Back, DamageCalculator.Classify(-1f, 0f, 1, true));
            var m = DamageMultipliers.Default;
            m.Back = 2f;
            Assert.AreEqual(4, DamageCalculator.Apply(2, AttackAngle.Back, m), "adaga: 2 de frente, 4 pelas costas");
        }
    }
}
