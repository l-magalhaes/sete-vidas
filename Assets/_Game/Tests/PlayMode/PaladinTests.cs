using System.Collections;
using NUnit.Framework;
using SeteVidas.Abilities;
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
    /// Fase 5: "vejo a parede → não consigo quebrar → ganho o Paladino → volto e quebro", a parede continua
    /// quebrada depois de reabrir o jogo e a escalada funciona com o Paladino.
    /// </summary>
    public class PaladinTests
    {
        PlayerHealth _health;
        ClassManager _classes;
        CombatController _combat;
        BreakableWall _wall;
        CompanionBond _golden;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null; // Start de todo mundo
            _health = PlayerHealth.Active;
            _classes = ClassManager.Active;
            Assert.IsNotNull(_classes, "Gato sem ClassManager. Rode Tools > Sete Vidas > 8");
            _combat = _classes.GetComponent<CombatController>();
            _wall = Object.FindAnyObjectByType<BreakableWall>();
            _golden = Object.FindAnyObjectByType<CompanionBond>();
            Assert.IsNotNull(_wall, "Parede rachada ausente. Rode Tools > Sete Vidas > 8");
            Assert.IsNotNull(_golden, "Golden ausente. Rode Tools > Sete Vidas > 8");
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        void HitWall() => _wall.TakeDamage(_combat.Weapon.ResolveHit(_health.transform.position, _wall, _health.gameObject));

        [UnityTest]
        public IEnumerator NewGame_RogueOnly_LockedClassesDoNothing()
        {
            yield return Load();
            Assert.AreEqual(CharacterClass.Rogue, _classes.CurrentClass);
            Assert.IsFalse(_classes.TrySwitch(CharacterClass.Paladin), "Paladino ainda bloqueado");
            Assert.IsFalse(_classes.TrySwitch(CharacterClass.Mage), "Mago nem existe ainda");
            Assert.IsFalse(_classes.TryCycle(+1));
            Assert.AreEqual(CharacterClass.Rogue, _classes.CurrentClass);
        }

        [UnityTest]
        public IEnumerator SeeWall_CantBreak_BondGolden_SwitchToPaladin_Break()
        {
            yield return Load();
            int wallHealth = _wall.Health;

            // 1) Adagas: "tink", nada acontece
            HitWall();
            Assert.AreEqual(wallHealth, _wall.Health, "Adagas não arranham a parede rachada");

            // 2) Vínculo: +1 coração e Paladino
            _health.ApplyCurse(1); // o tomo apaga 6 das 7 vidas
            _golden.CompleteBond(_health);
            Assert.AreEqual(2, _health.HeartsRecovered, "O vínculo recupera o 2º coração");
            Assert.AreEqual(4, _health.MaxHits);
            Assert.IsTrue(SaveStore.Read(SaveManager.FilePath).unlockedClasses.Contains("Paladin"), "Desbloqueio vai para o disco na hora");

            // 3) Troca para o Paladino: a arma vira a marreta (Heavy)
            Assert.IsTrue(_classes.TrySwitch(CharacterClass.Paladin));
            Assert.AreEqual(DamageType.Heavy, _combat.Weapon.Type);

            // 4) Duas marretadas quebram a parede e gravam a flag
            HitWall();
            Assert.IsFalse(_wall.IsBroken);
            HitWall();
            Assert.IsTrue(_wall.IsBroken, "Duas marretadas (4 + 4) quebram a parede (8)");
            Assert.IsFalse(_wall.GetComponent<Collider2D>().enabled, "Quebrada não bloqueia mais");
            Assert.IsTrue(SaveStore.Read(SaveManager.FilePath).HasFlag(_wall.Flag), "Flag gravada no disco");

            // 5) Volta para o Ladino: adagas de novo
            Assert.IsTrue(_classes.TrySwitch(CharacterClass.Rogue));
            Assert.AreEqual(DamageType.Normal, _combat.Weapon.Type);
        }

        [UnityTest]
        public IEnumerator BrokenWall_StaysBroken_AfterReopening()
        {
            yield return Load();
            _health.ApplyCurse(1); // o tomo apaga 6 das 7 vidas
            _golden.CompleteBond(_health);
            _classes.TrySwitch(CharacterClass.Paladin);
            HitWall();
            HitWall();
            Assert.IsTrue(_wall.IsBroken);

            SaveManager.PathOverride = SaveManager.PathOverride; // "fecha o jogo": esquece a memória e lê do disco
            yield return Load();

            Assert.IsTrue(_wall.IsBroken, "A parede continua quebrada depois de reabrir");
            Assert.AreEqual(2, _health.HeartsRecovered, "O coração do vínculo também ficou salvo");
            Assert.IsTrue(_classes.IsAvailable(CharacterClass.Paladin));
        }

        [UnityTest]
        public IEnumerator NoGapAboveTheWall_OnlyTheHammerOpensTheTunnel()
        {
            yield return Load();
            var ground = GameObject.Find("Grid/Ground").GetComponent<UnityEngine.Tilemaps.Tilemap>();
            for (int x = 156; x <= 177; x++)
                Assert.IsNotNull(ground.GetTile(new Vector3Int(x, 4, 0)), $"Fresta na laje em x {x}: dá para cair atrás da parede sem a marreta");
            Assert.IsNotNull(ground.GetTile(new Vector3Int(178, 4, 0)), "A laje precisa encostar no limite direito");
        }

        [UnityTest]
        public IEnumerator ClimbingStillWorks_WithPaladinActive()
        {
            yield return Load();
            _health.ApplyCurse(1); // o tomo apaga 6 das 7 vidas
            _golden.CompleteBond(_health);
            Assert.IsTrue(_classes.TrySwitch(CharacterClass.Paladin));
            var climb = _classes.GetComponent<WallClimbAbility>();
            Assert.IsNotNull(climb);
            Assert.IsTrue(climb.enabled, "Escalada é locomoção global: não pode desligar na troca de classe");
        }

        [UnityTest]
        public IEnumerator Death_KeepsClassesAndHearts()
        {
            yield return Load();
            _health.ApplyCurse(1); // o tomo apaga 6 das 7 vidas
            _golden.CompleteBond(_health);
            _classes.TrySwitch(CharacterClass.Paladin);

            for (int i = 0; i < _health.MaxHits; i++)
            {
                _health.TakeDamage(new DamageInfo(1, Vector2.right, DamageType.Normal, AttackAngle.Front, null));
                yield return new WaitForSeconds(_health.GetComponent<PlayerController>().Settings.InvulnerabilityTime + 0.05f);
            }
            float t = 0f;
            while (_health.IsDead && t < 5f) { t += Time.deltaTime; yield return null; }

            Assert.AreEqual(2, _health.HeartsRecovered);
            Assert.IsTrue(_classes.IsAvailable(CharacterClass.Paladin), "Morrer não tira a classe");
            Assert.AreEqual(CharacterClass.Paladin, _classes.CurrentClass, "Volta com a mesma classe ativa");
        }
    }
}
