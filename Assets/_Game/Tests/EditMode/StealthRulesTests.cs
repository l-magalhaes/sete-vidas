using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Fase 3: visibilidade, cone de visão, medidor de suspeita e estados da Sentinela (GDD §5.1).</summary>
    public class StealthRulesTests
    {
        const float Dt = 0.02f;
        static readonly AlertTuning T = AlertTuning.Default;

        // ------------------------------------------------------- visibilidade

        [TestCase(1f, false, 1f)]
        [TestCase(1f, true, 0.6f)]
        [TestCase(0.3f, false, 0.3f)]
        [TestCase(0.3f, true, 0.18f)]
        public void Visibility_IsLightTimesCrouch(float light, bool crouching, float expected)
        {
            Assert.AreEqual(expected, StealthMath.Visibility(light, crouching, 0.6f), 1e-4f);
        }

        [Test]
        public void Bush_HidesOnlyWhenCrouched()
        {
            Assert.AreEqual(0.15f, StealthMath.Visibility(1f, true, 0.6f, 0.25f), 1e-4f, "Agachado no mato, na luz");
            Assert.AreEqual(1f, StealthMath.Visibility(1f, false, 0.6f, 0.25f), 1e-4f, "De pé no mato: totalmente visível");
            float range = PerceptionMath.EffectiveRange(8f, StealthMath.Visibility(1f, true, 0.6f, 0.25f));
            Assert.Less(range, 1.5f, "Agachado no mato, a Sentinela só percebe colado");
        }

        [Test]
        public void ShadowAndCrouch_HidesBeyondTwoTiles()
        {
            float range = PerceptionMath.EffectiveRange(8f, StealthMath.Visibility(0.3f, true, 0.6f));
            Assert.Less(range, 2f, "Agachado na sombra, a Sentinela só percebe colado");
        }

        [Test]
        public void Landing_NormalJumpIsSilent_FallFromWalkwayIsLoud_HangDropIsSilent()
        {
            var s = ScriptableObject.CreateInstance<StealthSettings>();
            var m = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                const float walkway = 5f; // passarela do trecho H da Playground
                Assert.IsFalse(StealthMath.LandingIsLoud(m.JumpHeight, s.LandingNoiseHeight), "Pulo normal");
                Assert.IsTrue(StealthMath.LandingIsLoud(walkway, s.LandingNoiseHeight), "Cair da passarela");
                Assert.IsFalse(StealthMath.LandingIsLoud(walkway - m.LedgeHandHeight, s.LandingNoiseHeight), "Pendurar e soltar");
            }
            finally
            {
                Object.DestroyImmediate(s);
                Object.DestroyImmediate(m);
            }
        }

        // ---------------------------------------------------------------- cone

        [Test]
        public void Cone_SeesInFront_NotBehind_NotBeyondRange()
        {
            var eye = new Vector2(0f, 1.3f);
            Assert.IsTrue(PerceptionMath.InVisionCone(eye, 1, new Vector2(5f, 1f), 8f, 35f), "Na frente");
            Assert.IsFalse(PerceptionMath.InVisionCone(eye, 1, new Vector2(-2f, 1f), 8f, 35f), "Atrás");
            Assert.IsFalse(PerceptionMath.InVisionCone(eye, 1, new Vector2(9f, 1.3f), 8f, 35f), "Longe demais");
            Assert.IsFalse(PerceptionMath.InVisionCone(eye, 1, new Vector2(2f, 5f), 8f, 35f), "Muito acima (fora do ângulo)");
        }

        [Test]
        public void SightFill_FasterWhenClose_ZeroOutOfRange()
        {
            float close = PerceptionMath.SightFillRate(0.5f, 8f, 0.8f);
            float far = PerceptionMath.SightFillRate(7.5f, 8f, 0.8f);
            Assert.Greater(close, far);
            Assert.AreEqual(0f, PerceptionMath.SightFillRate(9f, 8f, 0.8f));
        }

        // -------------------------------------------------------------- estados

        static void Run(AlertBrain b, float seconds, float seeFill = 0f)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                if (seeFill > 0f) b.See(new Vector2(5f, 1f), seeFill, Dt);
                b.Tick(Dt, T);
            }
        }

        [Test]
        public void BriefGlimpse_GoesSuspicious_ThenCalmsBackToPatrol()
        {
            var b = new AlertBrain();
            Run(b, 0.4f, seeFill: 0.8f);            // ~0,32 de suspeita
            Assert.AreEqual(AlertState.Suspicious, b.State);
            Run(b, 5f);                              // sem estímulo: esvazia
            Assert.AreEqual(AlertState.Patrolling, b.State);
        }

        [Test]
        public void ContinuousSight_ReachesCombat()
        {
            var b = new AlertBrain();
            Run(b, 1.5f, seeFill: 1f);
            Assert.AreEqual(AlertState.Combat, b.State);
        }

        [Test]
        public void LosingTarget_InCombat_Investigates_ThenSearches_ThenPatrols()
        {
            var b = new AlertBrain();
            Run(b, 1.5f, seeFill: 1f);
            Assert.AreEqual(AlertState.Combat, b.State);

            Run(b, T.LoseSightTime + 0.1f);
            Assert.AreEqual(AlertState.Investigating, b.State, "Perdeu o gato: vai à última posição conhecida");
            Assert.AreEqual(new Vector2(5f, 1f), b.LastKnownPosition);

            b.ArrivedAtLastKnownPosition();
            Assert.IsTrue(b.IsSearching);
            Run(b, T.SearchTime + 0.1f);
            Assert.AreEqual(AlertState.Patrolling, b.State, "Não achou: volta à patrulha");
            Assert.AreEqual(0f, b.Suspicion);
        }

        [Test]
        public void Noise_MakesSuspicious_AndPointsToTheSound()
        {
            var b = new AlertBrain();
            b.Hear(new Vector2(-3f, 0f), 0.4f);
            b.Tick(Dt, T);
            Assert.AreEqual(AlertState.Suspicious, b.State);
            Assert.AreEqual(new Vector2(-3f, 0f), b.LastKnownPosition);
        }

        [Test]
        public void TwoLoudNoises_LeadToInvestigation()
        {
            var b = new AlertBrain();
            b.Hear(new Vector2(-3f, 0f), 0.4f);
            b.Hear(new Vector2(-3f, 0f), 0.4f);
            Run(b, T.SuspiciousTime + 0.1f);
            Assert.AreEqual(AlertState.Investigating, b.State);
        }

        [Test]
        public void SingleFootstep_IsNotEnough()
        {
            var b = new AlertBrain();
            b.Hear(Vector2.zero, 0.15f);
            b.Tick(Dt, T);
            Assert.AreEqual(AlertState.Patrolling, b.State);
        }

        [Test]
        public void Damage_GoesStraightToCombat_AndDeadIgnoresEverything()
        {
            var b = new AlertBrain();
            b.OnDamaged(new Vector2(-1f, 0f));
            Assert.AreEqual(AlertState.Combat, b.State);
            b.Kill();
            b.Hear(Vector2.zero, 1f);
            b.Tick(Dt, T);
            Assert.AreEqual(AlertState.Dead, b.State);
            b.Reset();
            Assert.AreEqual(AlertState.Patrolling, b.State);
        }

        // -------------------------------------------------------- balanceamento

        [Test]
        public void Sentinel_StealthBackstabKillsInOneHit_BackTwo_FrontThree()
        {
            var sentinel = ScriptableObject.CreateInstance<SentinelSettings>();
            var daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            try
            {
                int hp = sentinel.MaxHealth;
                var m = daggers.Multipliers;
                int Dmg(AttackAngle a) => DamageCalculator.Apply(daggers.BaseDamage, a, m);
                Assert.AreEqual(1, DamageCalculator.HitsToDefeat(hp, Dmg(AttackAngle.StealthBack)), "Furtivo");
                Assert.AreEqual(2, DamageCalculator.HitsToDefeat(hp, Dmg(AttackAngle.Back)), "Costas");
                Assert.AreEqual(3, DamageCalculator.HitsToDefeat(hp, Dmg(AttackAngle.Front)), "Frente");
            }
            finally
            {
                Object.DestroyImmediate(sentinel);
                Object.DestroyImmediate(daggers);
            }
        }

        [Test]
        public void Sentinel_IsSlowerThanCat_AndTelegraphsAttack()
        {
            var sentinel = ScriptableObject.CreateInstance<SentinelSettings>();
            var cat = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                Assert.Less(sentinel.ChaseSpeed, cat.MoveSpeed, "O gato precisa conseguir fugir");
                Assert.GreaterOrEqual(sentinel.AttackWindup, 0.3f, "Preparação curta demais para reagir");
                Assert.Less(sentinel.SuspiciousThreshold, sentinel.InvestigateThreshold);
            }
            finally
            {
                Object.DestroyImmediate(sentinel);
                Object.DestroyImmediate(cat);
            }
        }
    }
}
