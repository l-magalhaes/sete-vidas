using NUnit.Framework;
using SeteVidas.Progression;

namespace SeteVidas.Tests
{
    /// <summary>F9: a Tobi e a ponte de madeira (<see cref="TobiRules"/>, <see cref="ReconstructionWood"/>).</summary>
    public class TobiTests
    {
        [Test]
        public void Talk_FollowsTheStory()
        {
            Assert.AreEqual(TobiTalk.Meet, TobiRules.Next(false, 20, 99, false), "primeiro o encontro, mesmo com tudo");
            Assert.AreEqual(TobiTalk.MissingWood, TobiRules.Next(true, 11, 99, false));
            Assert.AreEqual(TobiTalk.MissingCoins, TobiRules.Next(true, 12, 19, false), "madeira eu tenho, pagamento não");
            Assert.AreEqual(TobiTalk.Build, TobiRules.Next(true, 12, 20, false));
            Assert.AreEqual(TobiTalk.Build, TobiRules.Next(true, 18, 50, false), "pode ter mais tábuas que as 12");
            Assert.AreEqual(TobiTalk.After, TobiRules.Next(true, 0, 0, true), "depois da ponte, as falas de depois");
            Assert.AreEqual(TobiTalk.Meet, TobiRules.Next((SeteVidas.Save.SaveData)null));
        }

        [Test]
        public void MissingWood_SaysTheCount_Singular()
        {
            string t = "Faltam " + TobiRules.CountToken + ". E eu não trabalho fiado!";
            Assert.AreEqual("Faltam 3. E eu não trabalho fiado!", TobiRules.FillCount(t, 3));
            Assert.AreEqual("Falta 1. E eu não trabalho fiado!", TobiRules.FillCount(t, 1));
            Assert.IsFalse(TobiRules.CountToken.Contains("{"), "\"{...}\" é dos atalhos de botão nas falas");
        }

        [Test]
        public void Bridge_BuildsFromTheWest_AndTheSceneIsShort()
        {
            Assert.IsTrue(TobiRules.Built(10f, 12f));
            Assert.IsFalse(TobiRules.Built(14f, 12f));
            Assert.Less(104f / TobiRules.BuildSpeed, 5f, "a nuvem de obra atravessa os 104 tiles em menos de 5 s");
            Assert.AreEqual(12, ReconstructionWood.Total);
            Assert.AreEqual(20, ReconstructionWood.BridgePrice);
        }

        [Test]
        public void EveryEnemy_DropsHeartsLikeTheTemplars_AndManaAtTenPercent()
        {
            // (o teste fica aqui por ser do mesmo pedido de itens; as regras estão em SeteVidas.World.EnemyDrops)
            Assert.AreEqual(0.15f, SeteVidas.World.EnemyDrops.HeartChance, 1e-4f, "a mesma chance dos cães templários");
            Assert.AreEqual(0.10f, SeteVidas.World.EnemyDrops.ManaChance, 1e-4f);
            Assert.IsTrue(SeteVidas.World.EnemyDrops.DropsMana(0.05f, true));
            Assert.IsFalse(SeteVidas.World.EnemyDrops.DropsMana(0.0f, false), "antes do Paladino, o potinho não cai");
            Assert.IsFalse(SeteVidas.World.EnemyDrops.DropsMana(0.15f, true));
            Assert.IsTrue(SeteVidas.World.EnemyDrops.DropsHeart(0.1f));
            Assert.IsFalse(SeteVidas.World.EnemyDrops.DropsHeart(0.2f));
            Assert.AreEqual(3, SeteVidas.Save.SaveData.MaxHealCharges, "o potinho enche os 3 lacres da cura");
        }

        [Test]
        public void Exit_DawnAndMiaraveuGate()
        {
            // o amanhecer acende da ponte para a cabeceira leste
            Assert.AreEqual(0f, SeteVidas.World.Scenery.DawnSky.Amount(0f, 10f, 20f), 1e-4f);
            Assert.AreEqual(1f, SeteVidas.World.Scenery.DawnSky.Amount(25f, 10f, 20f), 1e-4f);
            Assert.Greater(SeteVidas.World.Scenery.DawnSky.At(0.05f).a, 200, "quente e forte no horizonte");
            Assert.AreEqual(0, SeteVidas.World.Scenery.DawnSky.At(1f).a, "some em cima: as estrelas continuam");
            Assert.Less(SeteVidas.World.Scenery.DawnSky.Order, -95, "atrás das montanhas");
            // o portão: desenho com a placa
            var px = SeteVidas.World.MiaraveuGateArt.Pixels();
            int filled = 0;
            foreach (var p in px) if (p.a > 0) filled++;
            Assert.Greater(filled, 3000);
            Assert.AreEqual("MIARAVEU", SeteVidas.World.MiaraveuGateArt.Line2);
        }

        [Test]
        public void Tobi_HasEveryAnimation_PortraitAndBridgeArt()
        {
            string dir = "Assets/_Game/Art/Characters/Tobi";
            foreach (var clip in new[] { "idle", "talk", "greedy", "offended", "proud", "wrench", "carry", "dig", "emerge", "pull", "hammer", "drill" })
                Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(dir, clip)), clip);
            foreach (var p in new[] { "portrait_tobi", "portrait_tobi_offended", "portrait_tobi_greedy", "portrait_tobi_proud" })
                Assert.IsTrue(System.IO.File.Exists("Assets/_Game/Art/UI/Portraits/" + p + ".png"), p);
            for (int v = 0; v < 4; v++)
                Assert.IsTrue(System.IO.File.Exists($"Assets/_Game/Art/Environments/Forest/Placeholder/ponte_madeira_{v}.png"), "bloco " + v);
            Assert.IsTrue(System.IO.File.Exists("Assets/_Game/Art/Environments/Forest/Placeholder/corda_ponte.png"));
            // cada fala da Tobi tem a animação dela
            foreach (var e in new[] { "ofendida", "gananciosa", "orgulhosa" })
                Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(dir, SeteVidas.World.VulpisVisual.ClipFor(e))), e);
        }
    }
}
