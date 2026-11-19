using NUnit.Framework;
using SeteVidas.World;
using UnityEngine;
using static SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Melhorias da Floresta de 17/11/2026 (pedidos do usuário): o Galho Seco contra o arco e a morte dele, o mel e a
    /// colmeia das vespas, a queda da ponte pelo peso da horda e o brilho roxo dos olhos da Isyathy.
    /// </summary>
    public class ForestTweaksTests
    {
        [Test]
        public void Branch_ArrowsChipSlowly_AndKeepTheRest()
        {
            float carry = 0f;
            int total = 0, arrows = 0;
            while (total < BranchHealth && arrows < 100) { total += BranchArrowHit(1, ref carry); arrows++; }
            Assert.GreaterOrEqual(arrows, 15, "com flecha (1 de dano), o Galho Seco aguenta bem mais que 6 tiros");
            Assert.AreEqual(BranchHealth, total);
            carry = 0f;
            Assert.AreEqual(0, BranchArrowHit(1, ref carry), "a primeira flecha só crava");
            Assert.Greater(carry, 0f, "mas o dano fica guardado");
        }

        [Test]
        public void Branch_Death_FallsFasterAndFaster_ThenRests()
        {
            Assert.AreEqual(0f, BranchFallAngleAt(0f));
            float a1 = BranchFallAngleAt(BranchFallTime * 0.25f), a2 = BranchFallAngleAt(BranchFallTime * 0.5f), a3 = BranchFallAngleAt(BranchFallTime * 0.75f);
            Assert.Greater(a3 - a2, a2 - a1, "a gravidade: cada vez mais rápido");
            Assert.AreEqual(BranchFallAngle, BranchFallAngleAt(BranchFallTime + BranchSettleTime + 0.1f), 1e-3f, "deitado no chão");
            Assert.Less(BranchFallAngleAt(BranchFallTime + BranchSettleTime * 0.4f), BranchFallAngle, "quica um pouco");
            Assert.Greater(BranchCorpseTime, 1f, "a animação inteira aparece antes de sumir");
        }

        [Test]
        public void Wasps_Drop_Honey_OneInFive()
        {
            Assert.AreEqual(0.2f, BatHoneyChance, 1e-5f);
            Assert.IsTrue(HoneyDrops(0.1f));
            Assert.IsFalse(HoneyDrops(0.5f));
            int drops = 0;
            for (int i = 0; i < 1000; i++) if (HoneyDrops(i / 1000f)) drops++;
            Assert.AreEqual(200, drops, 2);
            Assert.AreEqual(1, HoneyHeal, "o mel cura meio coração");
        }

        [Test]
        public void Hive_And_Honey_AreDrawn()
        {
            var hive = BeeArt.Hive();
            Assert.AreEqual(BeeArt.HiveW * BeeArt.HiveH, hive.Length);
            int filled = 0;
            foreach (var c in hive) if (c != 0u) filled++;
            Assert.Greater(filled, BeeArt.HiveW * BeeArt.HiveH / 3, "uma colmeia gorda");
            Assert.AreNotEqual(0u, hive[0 * BeeArt.HiveW + BeeArt.HiveW / 2 - 1], "o talo no alto (pendurada)");
            int honey = 0;
            foreach (var c in BeeArt.Honey()) if (c != 0u) honey++;
            Assert.Greater(honey, 20);
        }

        [Test]
        public void Bridge_ShakesOnlyUnderTheHordesWeight()
        {
            Assert.AreEqual(0f, BridgeRules.Weight(0, true), "só o gato: nada");
            Assert.AreEqual(0f, BridgeRules.Weight(BridgeRules.ZombiesToShake - 1, true), "poucos zumbis: nada");
            Assert.Greater(BridgeRules.Weight(BridgeRules.ZombiesToShake, true), 0f, "vários zumbis e o herói: treme");
            Assert.Greater(BridgeRules.Weight(5, true), BridgeRules.Weight(3, true), "mais peso, mais tremor");
            Assert.IsFalse(BridgeRules.Collapses(5f, 0f), "o gato ainda correndo: não desaba");
            Assert.IsFalse(BridgeRules.Collapses(5f, BridgeRules.LineTime * 0.5f), "ainda falando");
            Assert.IsFalse(BridgeRules.Collapses(0.5f, BridgeRules.LineTime));
            Assert.IsTrue(BridgeRules.Collapses(BridgeRules.ShakeBeforeCollapse, BridgeRules.LineTime));
            Assert.Less(BridgeRules.FleeInput, 1f, "foge olhando para trás: a horda alcança a ponte");
        }

        [Test]
        public void Horde_PacksRightBehindTheCat_WhenHeStops()
        {
            Assert.Less(BridgeRules.PackGap(0), 2.5f, "o primeiro colado nele");
            Assert.Less(BridgeRules.PackGap(7), 10f, "os 8 em cima da ponte, perto");
            float zs = 3.2f, cat = 7f * BridgeRules.FleeInput;
            Assert.Greater(BridgeRules.PaceMultiplier(15f, 2f, cat, zs) * zs, cat, "longe: corre mais que o gato");
            Assert.AreEqual(cat, BridgeRules.PaceMultiplier(2f, 2f, cat, zs) * zs, 1e-3f, "na vaga: acompanha");
            Assert.AreEqual(0f, BridgeRules.PaceMultiplier(1f, 2f, 0f, zs), "o gato parado e ele já na vaga: para");
            // o zumbi do fim da fila (nasce fora da tela, ~28 tiles atrás) chega na vaga antes de o gato andar os 27 tiles
            // até onde a ponte desaba (19/11/2026: a fuga ficou mais curta)
            float gap = 28f, t = 0f, x = 0f;
            while (t < 27f / cat) { float m = BridgeRules.PaceMultiplier(gap, BridgeRules.PackGap(7), cat, zs); gap += (cat - m * zs) * 0.02f; t += 0.02f; x += cat * 0.02f; }
            Assert.Less(Mathf.Abs(gap - BridgeRules.PackGap(7)), 0.5f, "chega amontoado");
        }

        [Test]
        public void Horde_SpawnsOffscreen()
        {
            float left = 100f;
            for (int rank = 0; rank < 8; rank++)
                Assert.Less(BridgeRules.OffscreenX(120f, rank, left), left, "nunca dentro da tela");
            Assert.Greater(BridgeRules.OffscreenX(120f, 0, left), BridgeRules.OffscreenX(120f, 1, left), "em fila, o primeiro na frente");
            Assert.AreEqual(80f, BridgeRules.OffscreenX(80f, 0, left), "posto já fora da tela: fica nele");
        }

        [Test]
        public void Honey_GoesToThePocket_AndHealsWhenEaten()
        {
            var data = new SeteVidas.Save.SaveData();
            Assert.IsTrue(HoneyRules.Add(data));
            Assert.AreEqual(1, HoneyRules.Count(data));
            Assert.IsFalse(HoneyRules.Use(data, false), "vida cheia: não gasta");
            Assert.AreEqual(1, data.honey);
            Assert.IsTrue(HoneyRules.Use(data, true));
            Assert.AreEqual(0, data.honey);
            Assert.IsFalse(HoneyRules.Use(data, true), "sem mel");
            for (int i = 0; i < 20; i++) HoneyRules.Add(data);
            Assert.AreEqual(20, data.honey, "sem limite (19/11/2026)");
            Assert.AreEqual(1, HoneyRules.Heal);
        }

        [Test]
        public void Honey_Icon_IsAPot()
        {
            var icon = SeteVidas.UI.InventoryIcons.Draw(SeteVidas.UI.InventoryIcon.Honey);
            int honey = 0;
            foreach (var c in icon) if (c == "#e0a030") honey++;
            Assert.Greater(honey, 15, "o mel do potinho");
        }

        [Test]
        public void Sighting_OnlyOnePairOfEyes_AtATime()
        {
            Assert.IsFalse(IsyathySighting.MayAppear(true, 10f, 0f), "outro par na tela: espera");
            Assert.IsFalse(IsyathySighting.MayAppear(false, 10f, 11f), "descanso depois de um sumir");
            Assert.IsTrue(IsyathySighting.MayAppear(false, 12f, 11f));
            Assert.GreaterOrEqual(IsyathySighting.Gap.x, 4f, "menos aparições");
        }

        [Test]
        public void Sighting_Eyes_GlowPurple_MoreWhenOpen()
        {
            float open = IsyathySighting.GlowStrength(IsyathySighting.EyesOpen, 0.3f);
            float half = IsyathySighting.GlowStrength(IsyathySighting.EyesHalf, 0.3f);
            float shut = IsyathySighting.GlowStrength(IsyathySighting.EyesClosed, 0.3f);
            Assert.Greater(open, half);
            Assert.Greater(half, shut);
            Assert.Greater(shut, 0f, "a praga brilha até com o olho fechando");
            Assert.LessOrEqual(open, 1f);
        }
    }
}
