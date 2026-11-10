using System.Collections;
using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Magia de cura (Docs/Design/magia-cura.md): vem com o Paladino, cura 1 coração (2 hits), tem 3 usos e só
    /// recarrega no santuário. Dano no meio interrompe sem gastar; vida cheia não gasta.
    /// </summary>
    public class HealingSpellTests
    {
        PlayerHealth _health;
        HealingSpell _spell;

        IEnumerator Load(bool paladin)
        {
            SaveManager.DeleteSave();
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            _health = PlayerHealth.Active;
            _spell = _health.GetComponent<HealingSpell>();
            Assert.IsNotNull(_spell, "Gato sem HealingSpell. Rode Tools > Sete Vidas > Player");
            if (paladin) ClassManager.Active.Unlock(CharacterClass.Paladin);
            // espera pousar
            var controller = _health.GetComponent<PlayerController>();
            float t = 0f;
            while (!controller.IsGrounded && t < 3f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        void Hurt(int hits)
        {
            for (int i = 0; i < hits; i++)
            {
                _health.TakeDamage(new DamageInfo(1, Vector2.right, DamageType.Normal, AttackAngle.Front, null));
                typeof(PlayerHealth).GetField("_invulnerableUntil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(_health, -1f);
            }
        }

        /// <summary>O dano arremessa o gato: espera ele pousar (a cura é só no chão).</summary>
        IEnumerator Land()
        {
            var controller = _health.GetComponent<PlayerController>();
            yield return new WaitForSeconds(0.1f);
            float t = 0f;
            while (!controller.IsGrounded && t < 3f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator CastAndWait()
        {
            yield return Land();
            Assert.IsTrue(_spell.TryCast(), "Deveria poder curar: " + _spell.BlockReason());
            float t = 0f;
            while (_spell.IsCasting && t < 2f) { t += Time.deltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator Locked_UntilPaladin()
        {
            yield return Load(paladin: false);
            Hurt(2);
            Assert.IsFalse(_spell.IsUnlocked);
            Assert.IsFalse(_spell.TryCast(), "Sem o Paladino, nada de cura");
        }

        [UnityTest]
        public IEnumerator Heals_OneHeart_ThreeTimes_ThenRefillsAtSanctuary()
        {
            yield return Load(paladin: true);
            Assert.AreEqual(3, _spell.Charges, "Aprende a magia com 3 usos");

            for (int i = 0; i < 3; i++)
            {
                Hurt(3);
                int before = _health.CurrentHits;
                yield return CastAndWait();
                Assert.AreEqual(Mathf.Min(_health.MaxHits, before + 2), _health.CurrentHits, "Cura 1 coração (2 hits)");
                Assert.AreEqual(2 - i, _spell.Charges);
                yield return new WaitForSeconds(0.1f);
            }

            Hurt(2);
            yield return Land();
            Assert.IsFalse(_spell.TryCast(), "Sem usos");
            Assert.AreEqual("sem usos", _spell.BlockReason());

            var sanctuary = Object.FindAnyObjectByType<Sanctuary>();
            Assert.IsNotNull(sanctuary, "Sem santuário na Playground");
            sanctuary.Rest(_health);
            Assert.AreEqual(3, _spell.Charges, "Descansar no santuário recarrega");
            Assert.AreEqual(3, SaveStore.Read(SaveManager.FilePath).healCharges, "Os usos vão para o save");
        }

        [UnityTest]
        public IEnumerator FullHealth_DoesNotSpendCharge()
        {
            yield return Load(paladin: true);
            Assert.AreEqual(_health.MaxHits, _health.CurrentHits);
            Assert.IsFalse(_spell.TryCast());
            Assert.AreEqual(3, _spell.Charges);
        }

        [UnityTest]
        public IEnumerator Damage_DuringCast_Interrupts_WithoutSpending()
        {
            yield return Load(paladin: true);
            Hurt(3);
            yield return Land();
            Assert.IsTrue(_spell.TryCast(), _spell.BlockReason());
            yield return null;
            int before = _health.CurrentHits;
            Hurt(1);
            float t = 0f;
            while (_spell.IsCasting && t < 2f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(before - 1, _health.CurrentHits, "Interrompida: não curou");
            Assert.AreEqual(3, _spell.Charges, "Interrompida: o uso não foi gasto");
            Assert.IsFalse(_health.GetComponent<PlayerInputReader>().GameplayLocked, "O controle volta");
        }
    }
}
