using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Fase 5: troca de classe (Q/R, 1–7) e a marreta do Paladino.</summary>
    public class ClassRulesTests
    {
        static System.Func<CharacterClass, bool> Unlocked(params CharacterClass[] list)
        {
            var set = new HashSet<CharacterClass>(list);
            return set.Contains;
        }

        [Test]
        public void Cycle_OnlyRogue_NothingToSwitchTo()
        {
            Assert.IsFalse(ClassRules.TryCycle(CharacterClass.Rogue, +1, Unlocked(CharacterClass.Rogue), out _));
        }

        [Test]
        public void Cycle_RogueAndPaladin_BothDirectionsReachPaladin()
        {
            var u = Unlocked(CharacterClass.Rogue, CharacterClass.Paladin);
            Assert.IsTrue(ClassRules.TryCycle(CharacterClass.Rogue, +1, u, out var next));
            Assert.AreEqual(CharacterClass.Paladin, next);
            Assert.IsTrue(ClassRules.TryCycle(CharacterClass.Rogue, -1, u, out next));
            Assert.AreEqual(CharacterClass.Paladin, next, "Dá a volta: Q no Ladino vai para a última desbloqueada");
        }

        [Test]
        public void Cycle_SkipsLockedClasses()
        {
            var u = Unlocked(CharacterClass.Rogue, CharacterClass.Paladin, CharacterClass.Bard);
            Assert.IsTrue(ClassRules.TryCycle(CharacterClass.Paladin, +1, u, out var next));
            Assert.AreEqual(CharacterClass.Bard, next, "Pula Mago e Monge, bloqueados");
            Assert.IsTrue(ClassRules.TryCycle(CharacterClass.Bard, +1, u, out next));
            Assert.AreEqual(CharacterClass.Rogue, next);
        }

        [Test]
        public void Slots_1To7_MapToClassesInGddOrder()
        {
            Assert.IsTrue(ClassRules.FromSlot(1, out var c)); Assert.AreEqual(CharacterClass.Rogue, c);
            Assert.IsTrue(ClassRules.FromSlot(2, out c)); Assert.AreEqual(CharacterClass.Paladin, c);
            Assert.IsTrue(ClassRules.FromSlot(7, out c)); Assert.AreEqual(CharacterClass.Druid, c);
            Assert.IsFalse(ClassRules.FromSlot(0, out _));
            Assert.IsFalse(ClassRules.FromSlot(8, out _));
        }

        [Test]
        public void CannotSwitch_MidAttack_OrDead()
        {
            Assert.IsTrue(ClassRules.CanSwitch(isAttacking: false, isDead: false));
            Assert.IsFalse(ClassRules.CanSwitch(isAttacking: true, isDead: false));
            Assert.IsFalse(ClassRules.CanSwitch(isAttacking: false, isDead: true));
        }

        [Test]
        public void HammerBalance_SentinelTwoHits_WallTwoHits_DaggersNeverBreakWalls()
        {
            // Valores do asset PaladinHammer criado pelo setup (ver ClassSetup.CreateClassAssets)
            int hammer = 4;
            var sentinel = ScriptableObject.CreateInstance<SentinelSettings>();
            var daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            try
            {
                Assert.AreEqual(2, DamageCalculator.HitsToDefeat(sentinel.MaxHealth, hammer), "Sentinela (5): 2 marretadas");
                Assert.AreEqual(2, DamageCalculator.HitsToDefeat(8, hammer), "Parede (8): 2 marretadas");
                Assert.AreEqual(DamageType.Normal, daggers.Type, "Adagas não são Heavy");
            }
            finally
            {
                Object.DestroyImmediate(sentinel);
                Object.DestroyImmediate(daggers);
            }
        }
    }
}
