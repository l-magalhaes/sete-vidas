using System.Collections;
using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Fase 4: dano, morte, volta ao santuário, inimigos reaparecendo e continuar de onde salvou.</summary>
    public class PlayerHealthTests
    {
        const string SanctuaryB = "playground_furtividade";

        PlayerHealth _health;
        PlayerController _player;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null; // Start do PlayerHealth
            _health = PlayerHealth.Active;
            Assert.IsNotNull(_health, "Gato sem PlayerHealth. Rode Tools > Sete Vidas > 7");
            _player = _health.GetComponent<PlayerController>();
            Assert.IsNotNull(Sanctuary.Find(SanctuaryB), "Santuário de x 119,5 ausente. Rode Tools > Sete Vidas > 7");
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave(); // não deixa santuário salvo para os outros testes
            Time.timeScale = 1f;
        }

        static DamageInfo Hit() => new DamageInfo(1, Vector2.right, DamageType.Normal, AttackAngle.Front, null);

        [UnityTest]
        public IEnumerator NewGame_OneHeart_TwoHits()
        {
            yield return Load();
            Assert.AreEqual(1, _health.HeartsRecovered);
            Assert.AreEqual(2, _health.MaxHits);
            Assert.AreEqual(2, _health.CurrentHits);
        }

        [UnityTest]
        public IEnumerator Invulnerability_SecondHitRightAfter_DoesNotCount()
        {
            yield return Load();
            _health.TakeDamage(Hit());
            _health.TakeDamage(Hit());
            Assert.AreEqual(1, _health.CurrentHits, "Dois golpes no mesmo instante contam como 1 hit");
            Assert.IsTrue(_health.IsInvulnerable);
        }

        [UnityTest]
        public IEnumerator Rest_Saves_ThenDeath_RespawnsThereWithFullHealth()
        {
            yield return Load();
            var sanctuary = Sanctuary.Find(SanctuaryB);
            sanctuary.Rest(_health);

            var onDisk = SaveStore.Read(SaveManager.FilePath);
            Assert.AreEqual(SanctuaryB, onDisk.lastSanctuaryId, "Descansar deve gravar o santuário no arquivo");
            Assert.AreEqual("Playground", onDisk.scene);

            _player.Teleport(new Vector2(20f, 0.01f));
            _health.TakeDamage(Hit());
            yield return new WaitForSeconds(_player.Settings.InvulnerabilityTime + 0.1f);
            _health.TakeDamage(Hit());
            Assert.IsTrue(_health.IsDead, "2º hit com 1 coração mata");

            float t = 0f;
            while (_health.IsDead && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(_health.IsDead, "Deveria ter voltado em poucos segundos");
            Assert.AreEqual(sanctuary.SpawnPoint.x, _player.Body.position.x, 0.2f, "Volta no último santuário");
            Assert.AreEqual(_health.MaxHits, _health.CurrentHits, "Volta com vida cheia");
            Assert.AreEqual(1, _health.HeartsRecovered, "Morrer não remove corações");
        }

        [UnityTest]
        public IEnumerator Rest_BringsDefeatedSentinelBack()
        {
            yield return Load();
            var sentinel = Object.FindAnyObjectByType<EnemyAI>();
            Assert.IsNotNull(sentinel);
            sentinel.TakeDamage(new DamageInfo(99, Vector2.left, DamageType.Normal, AttackAngle.Front, null));
            Assert.IsTrue(sentinel.IsDead);

            Sanctuary.Find(SanctuaryB).Rest(_health);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(sentinel.IsDead, "Inimigos reaparecem ao descansar");
            Assert.AreEqual(sentinel.Settings.MaxHealth, sentinel.Health);
            Assert.AreEqual(AlertState.Patrolling, sentinel.State);
        }

        [UnityTest]
        public IEnumerator Sentinel_DropsHalfHeartOnlyWithinChance()
        {
            yield return Load();
            var sentinel = Object.FindAnyObjectByType<EnemyAI>();
            Assert.IsNull(sentinel.TryDropHeart(0.99f), "Fora da chance (15%): nada cai");
            var drop = sentinel.TryDropHeart(0f);
            Assert.IsNotNull(drop, "Dentro da chance: cai meio coração. Rode Tools > Sete Vidas > 7 se o prefab da Sentinela for antigo");
            Assert.IsNotNull(drop.GetComponent<HeartPickup>());
            Object.Destroy(drop);
        }

        [UnityTest]
        public IEnumerator HalfHeart_Heals1Hit_StaysWhenFull_NeverExceeds()
        {
            yield return Load();
            var sentinel = Object.FindAnyObjectByType<EnemyAI>();
            var drop = sentinel.TryDropHeart(0f).GetComponent<HeartPickup>();

            // Vida cheia: encosta e o item continua no chão
            drop.PlaceLanded(_player.Body.position);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(drop != null, "Com vida cheia, o meio coração não deve ser gasto");
            Assert.AreEqual(2, _health.CurrentHits);

            // Toma 1 hit: na próxima física, pega e volta a 2 (nunca 3)
            _health.TakeDamage(Hit());
            _player.Teleport(drop.transform.position);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(drop == null, "Precisando de vida, o meio coração é recolhido");
            Assert.AreEqual(_health.MaxHits, _health.CurrentHits, "Cura 1 hit sem passar do máximo");
        }

        [UnityTest]
        public IEnumerator ReopenGame_ContinuesAtSavedSanctuary()
        {
            yield return Load();
            Sanctuary.Find(SanctuaryB).Rest(_health);

            // "Fechar e abrir": esquece o save da memória (força ler do disco) e recarrega a cena
            SaveManager.PathOverride = SaveManager.PathOverride;
            yield return Load();

            Assert.AreEqual(Sanctuary.Find(SanctuaryB).SpawnPoint.x, _player.Body.position.x, 0.2f,
                "Ao abrir de novo, o gato começa no santuário salvo");
        }
    }
}
