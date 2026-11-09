using System.Collections;
using NUnit.Framework;
using SeteVidas.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SeteVidas.Tests
{
    /// <summary>Fase 2: boneco de treino com os três multiplicadores e o conteúdo novo da Playground.</summary>
    public class CombatTests
    {
        GameObject _dummyGo;
        MeleeAttackSettings _daggers;

        [TearDown]
        public void TearDown()
        {
            if (_dummyGo != null) Object.Destroy(_dummyGo);
            if (_daggers != null) Object.Destroy(_daggers);
            Time.timeScale = 1f;
        }

        TrainingDummy MakeDummy(TrainingDummy.Side side)
        {
            _dummyGo = new GameObject("Dummy (teste)");
            var dummy = _dummyGo.AddComponent<TrainingDummy>();
            dummy.SetFacing(side);
            _daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            return dummy;
        }

        void Hit(TrainingDummy dummy, float attackerX)
        {
            var info = _daggers.ResolveHit(new Vector2(attackerX, 0f), dummy, null);
            dummy.TakeDamage(info);
        }

        [UnityTest]
        public IEnumerator Dummy_StealthOpener_ThenBack_ThenFront()
        {
            var dummy = MakeDummy(TrainingDummy.Side.Right);
            yield return null;
            Assert.AreEqual(10, dummy.Health);
            Assert.IsFalse(dummy.InCombat);

            Hit(dummy, -1f);                       // pelas costas, calmo → furtivo
            Assert.AreEqual(5, dummy.Health, "Furtivo pelas costas = 5");
            Assert.IsTrue(dummy.InCombat);

            Hit(dummy, -1f);                       // pelas costas, em combate
            Assert.AreEqual(2, dummy.Health, "Pelas costas = 3");

            Hit(dummy, 1f);                        // de frente
            Assert.AreEqual(0, dummy.Health, "De frente = 2");
            Assert.IsTrue(dummy.IsDefeated);
        }

        [UnityTest]
        public IEnumerator Dummy_FacingLeft_BackIsOnTheRight()
        {
            var dummy = MakeDummy(TrainingDummy.Side.Left);
            yield return null;
            Hit(dummy, -1f);                       // à esquerda = na frente dele
            Assert.AreEqual(8, dummy.Health);
        }

        [UnityTest]
        public IEnumerator Dummy_RespawnsWithFullHealth()
        {
            var dummy = MakeDummy(TrainingDummy.Side.Right);
            yield return null;
            for (int i = 0; i < 5; i++) Hit(dummy, 1f);
            Assert.IsTrue(dummy.IsDefeated);
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(dummy.MaxHealth, dummy.Health);
            Assert.IsFalse(dummy.InCombat, "Volta calmo: o próximo golpe pelas costas é furtivo de novo");
        }

        [UnityTest]
        public IEnumerator Playground_HasChimneyAndTwoDummiesFacingOppositeWays()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);

            var climbable = GameObject.Find("Grid/Climbable")?.GetComponent<Tilemap>();
            Assert.IsNotNull(climbable, "Tilemap Grid/Climbable não encontrado");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(92, 0, 0)), "Parede esquerda da chaminé (x 92) ausente. Rode Tools > Sete Vidas > 5");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(97, 5, 0)), "Parede direita da chaminé (x 97) ausente");

            var group = GameObject.Find("TrainingDummies");
            Assert.IsNotNull(group, "Grupo TrainingDummies ausente. Rode Tools > Sete Vidas > 5");
            var dummies = group.GetComponentsInChildren<TrainingDummy>();
            Assert.AreEqual(2, dummies.Length, "A Playground deve ter 2 bonecos de treino. Rode Tools > Sete Vidas > 5");
            Assert.AreNotEqual(dummies[0].FacingDirection, dummies[1].FacingDirection, "Um boneco para cada lado");
            foreach (var d in dummies)
                Assert.AreEqual(LayerMask.NameToLayer("Enemy"), d.gameObject.layer, "Boneco precisa estar na layer Enemy");
        }
    }
}
