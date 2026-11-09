using System.Collections;
using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Chefão Golden: a grade fecha ao entrar, a cura é interrompida, vencer abre a sala do tomo, pegar o tomo
    /// apaga 6 vidas e o Golden faz o vínculo (2º coração + Paladino). Morrer reinicia a arena.
    /// </summary>
    public class BossTests
    {
        GoldenBoss _boss;
        BossArena _arena;
        CursedTome _tome;
        PlayerHealth _health;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            _boss = Object.FindAnyObjectByType<GoldenBoss>();
            _arena = Object.FindAnyObjectByType<BossArena>();
            _tome = Object.FindAnyObjectByType<CursedTome>();
            _health = PlayerHealth.Active;
            Assert.IsNotNull(_boss, "Sem chefão na Playground. Rode Tools > Sete Vidas > 10");
            Assert.IsNotNull(_arena);
            Assert.IsNotNull(_tome);
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        /// <summary>Fecha qualquer conversa aberta (o teste "aperta E" até acabar).</summary>
        static void SkipDialogues()
        {
            var runner = DialogueRunner.Active;
            for (int i = 0; i < 60 && runner != null && runner.IsOpen; i++) runner.Advance();
        }

        static DamageInfo Hit(int amount) => new DamageInfo(amount, Vector2.right, DamageType.Normal, AttackAngle.Front, null);

        void Defeat()
        {
            _arena.StartEncounter(_health);
            SkipDialogues();
            _boss.TakeDamage(Hit(_boss.MaxHealth));
            SkipDialogues();
        }

        [UnityTest]
        public IEnumerator Entering_ClosesGate_AndStartsFight()
        {
            yield return Load();
            Assert.IsFalse(_boss.IsFighting);
            _arena.StartEncounter(_health);
            var gates = GameObject.Find("BossSection").GetComponentsInChildren<ArenaGate>();
            int closed = 0;
            foreach (var g in gates) if (g.IsClosed) closed++;
            Assert.AreEqual(2, closed, "Grade de entrada fechou (e a do tomo continua fechada)");

            SkipDialogues();
            Assert.IsTrue(_boss.IsFighting, "Depois da conversa de entrada, a luta começa");
        }

        [UnityTest]
        public IEnumerator Heal_IsInterruptedByEnoughDamage()
        {
            yield return Load();
            _arena.StartEncounter(_health);
            SkipDialogues();
            _boss.TakeDamage(Hit(_boss.MaxHealth / 2)); // abaixo de 70%: pode curar

            // Força a cura para o teste (a escolha normal é sorteada)
            _boss.ForceMove(BossMove.Heal);
            yield return new WaitForSeconds(1.1f);
            int healed = _boss.Health;
            Assert.Greater(healed, _boss.MaxHealth - _boss.MaxHealth / 2, "Concentrado, ele recupera vida aos poucos");

            _boss.TakeDamage(Hit(_boss.Settings.HealInterruptDamage));
            Assert.AreEqual(GoldenBoss.State.Stagger, _boss.Current, "Dano suficiente interrompe a cura");
        }

        [UnityTest]
        public IEnumerator Defeat_OpensTomeRoom_AndIsSaved()
        {
            yield return Load();
            Defeat();
            Assert.IsTrue(_boss.IsDefeated);
            foreach (var g in GameObject.Find("BossSection").GetComponentsInChildren<ArenaGate>())
                Assert.IsFalse(g.IsClosed, "Vencido o chefe, as grades abrem");
            Assert.IsTrue(SaveStore.Read(SaveManager.FilePath).HasFlag(BossArena.DefeatedFlag), "Vitória gravada no disco");
            Assert.IsFalse(ClassManager.Active.IsAvailable(CharacterClass.Paladin), "A classe só vem depois do tomo e do juramento");
        }

        [UnityTest]
        public IEnumerator Death_ResetsArena()
        {
            yield return Load();
            _arena.StartEncounter(_health);
            SkipDialogues();
            _boss.TakeDamage(Hit(10));
            SeteVidas.Core.GameEvents.RaiseWorldReset(); // o que acontece quando o gato morre ou descansa
            Assert.IsFalse(_boss.IsFighting);
            Assert.AreEqual(_boss.MaxHealth, _boss.Health, "Volta com vida cheia");
            Assert.IsFalse(_arena.Started, "A arena espera o gato entrar de novo");
        }

        [UnityTest]
        public IEnumerator Tome_Curse_LeavesOneHeart_ThenGoldenBond_GivesPaladin()
        {
            yield return Load();
            Defeat();
            Assert.AreEqual(7, _health.HeartsRecovered, "Antes do tomo: 7 vidas");

            var routine = _tome.Take(_health);
            _tome.StartCoroutine(routine);

            // Avança as conversas (narração do tomo, maldição e juramento) até o fim da cena
            float t = 0f;
            while (_tome.IsRunning && t < 20f)
            {
                SkipDialogues();
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(_tome.IsRunning, "A cena do tomo deveria terminar");
            Assert.IsTrue(_tome.IsTaken);
            Assert.AreEqual(2, _health.HeartsRecovered, "A maldição deixa 1; o juramento devolve o 2º");
            Assert.IsTrue(ClassManager.Active.IsAvailable(CharacterClass.Paladin), "Paladino em troca do juramento");
            Assert.AreEqual(GoldenBoss.State.Companion, _boss.Current, "O Golden fica como companheiro");
            Assert.IsFalse(_health.IsCollapsed);
            yield return null;
            yield return null;
            Assert.IsFalse(_health.GetComponent<PlayerInputReader>().GameplayLocked, "O controle volta");

            var onDisk = SaveStore.Read(SaveManager.FilePath);
            Assert.AreEqual(2, onDisk.heartsRecovered);
            Assert.IsTrue(onDisk.HasFlag(CursedTome.TakenFlag));
            Assert.IsTrue(onDisk.HasFlag(CursedTome.StolenFlag), "O contratante levou o tomo");
            var coin = _tome.transform.Find("Moeda de ouro");
            Assert.IsNotNull(coin, "Tomo sem a moeda. Rode Tools > Sete Vidas > 10 ou 13");
            Assert.IsTrue(coin.gameObject.activeSelf, "A moeda do pagamento fica no chão");
            var contractor = _tome.transform.Find("Contratante");
            Assert.IsFalse(contractor.gameObject.activeSelf, "O contratante sumiu no teleporte");
        }
    }
}
