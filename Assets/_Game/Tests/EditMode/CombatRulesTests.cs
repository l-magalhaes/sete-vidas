using NUnit.Framework;
using SeteVidas.Abilities;
using SeteVidas.Combat;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Fase 2: ataque pelas costas (GDD §5.2), combo das adagas e pulo da parede.</summary>
    public class CombatRulesTests
    {
        static readonly DamageMultipliers M = DamageMultipliers.Default;

        // ------------------------------------------------------- ângulo do golpe

        [Test]
        public void Classify_AttackerOnTheSideTheTargetFaces_IsFront()
        {
            // alvo em x 0 olhando para a direita; gato em x 1 (na frente dele)
            Assert.AreEqual(AttackAngle.Front, DamageCalculator.Classify(1f, 0f, 1, false));
            Assert.AreEqual(AttackAngle.Front, DamageCalculator.Classify(-1f, 0f, -1, true));
        }

        [Test]
        public void Classify_BehindAndCalm_IsStealthBack()
        {
            Assert.AreEqual(AttackAngle.StealthBack, DamageCalculator.Classify(-1f, 0f, 1, false));
            Assert.AreEqual(AttackAngle.StealthBack, DamageCalculator.Classify(1f, 0f, -1, false));
        }

        [Test]
        public void Classify_BehindButInCombat_IsBack()
        {
            Assert.AreEqual(AttackAngle.Back, DamageCalculator.Classify(-1f, 0f, 1, true));
        }

        [Test]
        public void Classify_Aligned_CountsAsFront()
        {
            Assert.AreEqual(AttackAngle.Front, DamageCalculator.Classify(0.01f, 0f, 1, false));
        }

        // --------------------------------------------------------------- dano

        [TestCase(AttackAngle.Front, 2)]
        [TestCase(AttackAngle.Back, 3)]
        [TestCase(AttackAngle.StealthBack, 5)]
        public void Apply_DaggerBaseDamage2_GivesSpecValues(AttackAngle angle, int expected)
        {
            Assert.AreEqual(expected, DamageCalculator.Apply(2, angle, M));
        }

        [Test]
        public void Apply_NeverBelowOne_AndZeroBaseIsZero()
        {
            var weak = new DamageMultipliers { Front = 0.1f, Back = 0.1f, StealthBack = 0.1f };
            Assert.AreEqual(1, DamageCalculator.Apply(1, AttackAngle.Front, weak));
            Assert.AreEqual(0, DamageCalculator.Apply(0, AttackAngle.StealthBack, M));
        }

        [Test]
        public void DummyBalance_FrontFive_BackFour_StealthOpenerThree()
        {
            const int health = 10;
            Assert.AreEqual(5, DamageCalculator.HitsToDefeat(health, 2), "Só de frente");
            Assert.AreEqual(4, DamageCalculator.HitsToDefeat(health, 3), "Pelas costas em combate");
            // Furtivo (5) abre; o resto é pelas costas em combate (3)
            Assert.AreEqual(3, 1 + DamageCalculator.HitsToDefeat(health - 5, 3), "Furtivo + costas");
        }

        [Test]
        public void DefaultDaggers_MatchSpec()
        {
            var s = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            try
            {
                Assert.AreEqual(2, s.BaseDamage);
                Assert.AreEqual(DamageType.Normal, s.Type, "Adagas não podem quebrar parede rachada");
                Assert.IsTrue(s.BackstabBonus);
                float combo = s.ComboSteps * (s.Windup + s.Active) + s.Recovery + s.FinalRecovery;
                Assert.Less(combo, 0.7f, $"Ladino deve ser rápido: combo completo = {combo:0.00} s");
                Assert.Less(s.HitstopFront, s.HitstopBack);
                Assert.Less(s.HitstopBack, s.HitstopStealth, "O furtivo precisa travar mais a tela");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -------------------------------------------------------------- combo

        static readonly AttackTimings T = new AttackTimings
        {
            Steps = 2, Windup = 0.04f, Active = 0.08f, Recovery = 0.12f, FinalRecovery = 0.22f
        };

        static void Run(AttackSequence seq, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.01f) seq.Tick(0.01f, T);
        }

        [Test]
        public void Combo_SinglePress_OneSwingThenIdle()
        {
            var seq = new AttackSequence();
            Assert.IsTrue(seq.Press(T));
            Assert.AreEqual(AttackSequence.Phase.Windup, seq.Current);
            Run(seq, 0.05f);
            Assert.IsTrue(seq.IsHitActive, "Depois da preparação, a hitbox liga");
            Assert.AreEqual(1, seq.SwingId);
            Run(seq, 0.4f);
            Assert.IsFalse(seq.IsAttacking);
            Assert.AreEqual(1, seq.SwingId, "Sem segundo aperto, não há segundo golpe");
        }

        [Test]
        public void Combo_SecondPressDuringFirstSwing_QueuesSecondSwing()
        {
            var seq = new AttackSequence();
            seq.Press(T);
            Run(seq, 0.06f);
            Assert.IsTrue(seq.Press(T), "Apertar durante o 1º golpe enfileira o 2º");
            Run(seq, 0.25f);
            Assert.AreEqual(1, seq.Step);
            Assert.AreEqual(2, seq.SwingId);
        }

        [Test]
        public void Combo_ThirdPress_IsIgnoredUntilComboEnds()
        {
            var seq = new AttackSequence();
            seq.Press(T);
            seq.Press(T);
            Assert.IsFalse(seq.Press(T), "Combo de 2: o 3º aperto não enfileira");
            Run(seq, 1f);
            Assert.IsFalse(seq.IsAttacking);
            Assert.AreEqual(2, seq.SwingId);
            Assert.IsTrue(seq.Press(T), "Terminado o combo, recomeça no 1º golpe");
            Assert.AreEqual(0, seq.Step);
        }

        [Test]
        public void Combo_Cancel_StopsImmediately()
        {
            var seq = new AttackSequence();
            seq.Press(T);
            seq.Cancel();
            Assert.IsFalse(seq.IsAttacking);
        }

        // ---------------------------------------------------- pulo da parede

        [Test]
        public void WallJump_ReachesConfiguredHeight_WithNormalJumpGravity()
        {
            var move = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            var climb = ScriptableObject.CreateInstance<WallClimbSettings>();
            try
            {
                const float dt = 0.02f;
                float g = move.Gravity;
                float v = move.LaunchVelocityFor(climb.WallJumpHeight, dt), y = 0f, max = 0f;
                for (int i = 0; i < 1000 && v > 0f; i++) { v -= g * dt; y += v * dt; max = Mathf.Max(max, y); }
                Assert.AreEqual(climb.WallJumpHeight, max, 0.05f);
                Assert.Less(climb.WallJumpHeight, move.JumpHeight, "Pular da parede é mais baixo que o pulo normal");
            }
            finally
            {
                Object.DestroyImmediate(move);
                Object.DestroyImmediate(climb);
            }
        }
    }
}
