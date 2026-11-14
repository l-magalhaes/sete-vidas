using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Save;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F2 da Floresta Silenciosa (graybox): as regras de runtime que o graybox trouxe. O alcance das 39 salas
    /// (baús, saídas, softlock) é conferido pelo ForestReach, no menu 21b ou em Tools/LevelPreview/checar_floresta.ps1.
    /// </summary>
    public class ForestGrayboxTests
    {
        [Test]
        public void GrayboxScenes_UseTheTestSave_OnlyForestRegions()
        {
            Assert.IsTrue(ForestGrayboxSave.IsGraybox("Forest_A_Roots"));
            Assert.IsTrue(ForestGrayboxSave.IsGraybox("Forest_Road"));
            Assert.IsFalse(ForestGrayboxSave.IsGraybox("Forest_Test_A"), "As da F1 já são cenas de teste");
            Assert.IsFalse(ForestGrayboxSave.IsGraybox("Fortress_MVP"));
            Assert.IsFalse(ForestGrayboxSave.IsGraybox("MainMenu"));
            Assert.IsFalse(ForestGrayboxSave.IsGraybox(null));
        }

        [Test]
        public void Shortcut_ClosedSide_OpensOnlyAfterTheOtherSide()
        {
            var data = SaveData.NewGame();
            Assert.IsTrue(ResidenceDoor.ShortcutOpen(data, ""), "Porta sem flag exigida: sempre abre");
            Assert.IsFalse(ResidenceDoor.ShortcutOpen(data, "atalho_AT1"), "Lado fechado antes de abrir pelo outro lado");
            data.SetFlag("atalho_AT1");
            Assert.IsTrue(ResidenceDoor.ShortcutOpen(data, "atalho_AT1"), "Aberto do outro lado: funciona dos dois");
            Assert.IsFalse(ResidenceDoor.ShortcutOpen(null, "atalho_AT1"));
        }

        [Test]
        public void ShieldInfected_BlocksOnlyNormalHitsFromTheFront()
        {
            Assert.IsTrue(ZombieRules.ShieldBlocks(true, DamageType.Normal, AttackAngle.Front), "Adaga de frente: bloqueia");
            Assert.IsFalse(ZombieRules.ShieldBlocks(true, DamageType.Normal, AttackAngle.Back), "Pelas costas: passa");
            Assert.IsFalse(ZombieRules.ShieldBlocks(true, DamageType.Normal, AttackAngle.StealthBack));
            Assert.IsFalse(ZombieRules.ShieldBlocks(true, DamageType.Heavy, AttackAngle.Front), "Marreta quebra a guarda");
            Assert.IsFalse(ZombieRules.ShieldBlocks(false, DamageType.Normal, AttackAngle.Front), "Sem escudo: nada muda");
        }

        [Test]
        public void Bridge_FallenFlag_MatchesTheLayout()
        {
            Assert.AreEqual("ponte_caiu", BridgeCollapse.FallenFlag);
        }

        // ------------------------------------------------------------ atalhos de verdade (rev. 4)

        [Test]
        public void RealShortcut_OpensOnlyFromItsSide_AndThenForEveryone()
        {
            var up = UnityEngine.Vector2.up;
            Assert.IsTrue(ShortcutRules.CanOpen(false, true, new UnityEngine.Vector2(0.3f, 0.5f), up), "Em cima do alçapão: abre");
            Assert.IsFalse(ShortcutRules.CanOpen(false, true, new UnityEngine.Vector2(0f, -3f), up), "Embaixo: não alcança a tranca");
            Assert.IsFalse(ShortcutRules.CanOpen(false, false, new UnityEngine.Vector2(0f, 0.5f), up), "Lado fechado nunca abre");
            Assert.IsFalse(ShortcutRules.CanOpen(true, true, new UnityEngine.Vector2(0f, 0.5f), up), "Já aberto");

            var data = SaveData.NewGame();
            Assert.IsFalse(ShortcutRules.IsOpen(data, "atalho_ravina"));
            data.SetFlag("atalho_ravina");
            Assert.IsTrue(ShortcutRules.IsOpen(data, "atalho_ravina"), "A flag vale para os dois lados (as duas cenas)");
        }

        // ------------------------------------------------------------ Serpente

        [Test]
        public void Snake_StrikesFromTheHole_ThenStretches_ThenHides()
        {
            float t = 0f;
            var s = SeteVidas.Enemies.ForestEnemyRules.SnakeState.Hidden;
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.1f, 5f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Hidden, s, "Longe: fica no buraco");
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.1f, 2f, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Hidden, s, "Agachado na folhagem: não dá o bote");
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.1f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Warn, s, "Perto e à vista: chocalho");
            for (int i = 0; i < 5 && s == SeteVidas.Enemies.ForestEnemyRules.SnakeState.Warn; i++)
                s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.1f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Strike, s, "Depois do aviso (0,4 s): o bote");
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.2f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Stretched, s);
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.SnakeVulnerable(s), "Esticada: pogo e ponto fraco");
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 0.7f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Retreat, s);
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.SnakeInvulnerable(s, false), "Voltando para o buraco: invulnerável");
            s = SeteVidas.Enemies.ForestEnemyRules.StepSnake(s, ref t, 1.6f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.SnakeState.Hidden, s);
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.SnakeInvulnerable(s, false), "No buraco: não apanha");
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.SnakeInvulnerable(s, true), "A que rasteja apanha fora do bote");
        }
    }
}
