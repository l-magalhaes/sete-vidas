using NUnit.Framework;
using SeteVidas.Audio;
using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;

namespace SeteVidas.Tests
{
    /// <summary>F1 da Floresta Silenciosa: as regras da base técnica (Docs/Floresta/plano-implementacao.md).</summary>
    public class ForestFoundationTests
    {
        // ------------------------------------------------------------ pogo por estado

        class Target : IPogoTarget { public bool AcceptsPogo { get; set; } }

        [Test]
        public void Pogo_OnlyTargetsThatAllowIt_Bounce()
        {
            Assert.IsTrue(DownAttackRules.TargetAllowsBounce(new object()), "Sem IPogoTarget: todo alvo quica (como antes)");
            Assert.IsTrue(DownAttackRules.TargetAllowsBounce(new Target { AcceptsPogo = true }));
            Assert.IsFalse(DownAttackRules.TargetAllowsBounce(new Target { AcceptsPogo = false }), "Planta mordendo: acerta, mas não quica");
        }

        // ------------------------------------------------------------ tronco rachado

        [Test]
        public void Breakable_StoneKeepsOldFlag_WoodHasItsOwn()
        {
            Assert.AreEqual("parede_quebrada_patio", BreakableRules.Flag(BreakableMaterial.Stone, "patio"), "Saves da Fortaleza continuam valendo");
            Assert.AreEqual("tronco_quebrado_A3", BreakableRules.Flag(BreakableMaterial.Wood, "A3"));
        }

        [Test]
        public void Breakable_WoodBreak_IsLouderThanStone()
        {
            BreakableRules.BreakNoise(BreakableMaterial.Wood, out float woodR, out float woodL);
            BreakableRules.BreakNoise(BreakableMaterial.Stone, out float stoneR, out float stoneL);
            Assert.AreEqual(14f, woodR);
            Assert.AreEqual(1f, woodL);
            Assert.Greater(woodR, stoneR);
            Assert.Greater(woodL, stoneL);
        }

        // ------------------------------------------------------------ madeira e ponte

        static SaveData WithWood(int n, int coins = 0)
        {
            var d = SaveData.NewGame();
            for (int i = 1; i <= n; i++) d.SetFlag(ReconstructionWood.WoodFlag("W" + i.ToString("00")));
            d.coins = coins;
            return d;
        }

        [Test]
        public void Wood_CountsFlags_AndCapsAtTwelve()
        {
            Assert.AreEqual(0, ReconstructionWood.Count(SaveData.NewGame()));
            Assert.AreEqual(8, ReconstructionWood.Count(WithWood(8)));
            Assert.AreEqual(12, ReconstructionWood.Count(WithWood(14)), "Nunca passa de 12");
            Assert.AreEqual(4, ReconstructionWood.Missing(WithWood(8)));
            Assert.AreEqual(0, ReconstructionWood.Count(null));
        }

        [Test]
        public void Wood_LabelsMatchTheDesign()
        {
            Assert.AreEqual("Madeira de Reconstrução: 8/12", ReconstructionWood.Label(WithWood(8)));
            Assert.AreEqual("Madeira de Reconstrução (3/12)", ReconstructionWood.PickupText(3));
        }

        [Test]
        public void Bridge_NeedsTwelveWoodAndTwentyCoins_Once()
        {
            Assert.IsFalse(ReconstructionWood.CanBuildBridge(WithWood(11, 99)), "Falta madeira");
            Assert.IsFalse(ReconstructionWood.CanBuildBridge(WithWood(12, 19)), "Falta moeda");
            var ok = WithWood(12, 20);
            Assert.IsTrue(ReconstructionWood.CanBuildBridge(ok));
            ok.SetFlag(ReconstructionWood.BridgeFlag);
            Assert.IsFalse(ReconstructionWood.CanBuildBridge(ok), "A ponte só é construída uma vez");
            Assert.IsTrue(ReconstructionWood.BridgeBuilt(ok));
        }

        [Test]
        public void Wood_ChestAndWoodFlags_AreDifferent()
        {
            Assert.AreEqual("bau_W03", ReconstructionWood.ChestFlag("W03"));
            Assert.AreEqual("madeira_W03", ReconstructionWood.WoodFlag("W03"));
            var d = SaveData.NewGame();
            d.SetFlag(ReconstructionWood.ChestFlag("W03")); // baú só de moedas: não conta madeira
            Assert.AreEqual(0, ReconstructionWood.Count(d));
        }

        // ------------------------------------------------------------ plataformas

        [Test]
        public void OneWay_CountsAsGround_OnlyFromAbove_AndNotRising()
        {
            Assert.IsTrue(PlatformRules.StandsOn(5f, 5f, 0f), "Parado em cima");
            Assert.IsTrue(PlatformRules.StandsOn(4.95f, 5f, -2f), "Caindo, pés no topo");
            Assert.IsFalse(PlatformRules.StandsOn(4.5f, 5f, -2f), "Atravessando por baixo: não é chão");
            Assert.IsFalse(PlatformRules.StandsOn(5f, 5f, 3f), "Subindo através dela: não é chão");
        }

        [Test]
        public void OneWay_DownPlusJump_DropsThrough_OnlyOnAPlatform()
        {
            Assert.IsTrue(PlatformRules.WantsDropThrough(true, true, true, 0f));
            Assert.IsTrue(PlatformRules.WantsDropThrough(true, true, false, -1f), "Analógico para baixo");
            Assert.IsFalse(PlatformRules.WantsDropThrough(false, true, true, 0f), "No chão sólido: pulo agachado normal");
            Assert.IsFalse(PlatformRules.WantsDropThrough(true, false, true, 0f), "Só agachado: não desce");
            Assert.IsFalse(PlatformRules.WantsDropThrough(true, true, false, 0f), "Só pulo: pula");
        }

        [Test]
        public void Leaf_SinksWithWeight_UpToThree_AndComesBack()
        {
            float d = 0f;
            for (int i = 0; i < 50; i++) d = PlatformRules.SinkStep(d, true, 0.1f);
            Assert.AreEqual(3f, d, 1e-4f, "Afunda até 3 tiles");
            d = PlatformRules.SinkStep(d, false, 1f);
            Assert.AreEqual(2f, d, 1e-4f, "Sobe 1 tile/s sem ninguém");
            for (int i = 0; i < 10; i++) d = PlatformRules.SinkStep(d, false, 1f);
            Assert.AreEqual(0f, d, 1e-4f);
        }

        [Test]
        public void Branch_DipsAQuarterTile_AndReturns()
        {
            float o = 0f;
            for (int i = 0; i < 20; i++) o = PlatformRules.SwayStep(o, true, 0.05f);
            Assert.AreEqual(0.25f, o, 1e-4f);
            for (int i = 0; i < 20; i++) o = PlatformRules.SwayStep(o, false, 0.05f);
            Assert.AreEqual(0f, o, 1e-4f);
        }

        [Test]
        public void Mushroom_BouncesOnlyWhenLandingOnTop()
        {
            const float dt = 0.02f;
            Assert.IsTrue(PlatformRules.MushroomBounces(-10f, 3.15f, 3f, dt, false), "Caindo: vai encostar neste passo (antes da física zerar a queda)");
            Assert.IsFalse(PlatformRules.MushroomBounces(-10f, 4f, 3f, dt, false), "Ainda longe do chapéu");
            Assert.IsTrue(PlatformRules.MushroomBounces(0f, 3f, 3f, dt, true), "Em pé no chapéu (o bug do print): quica");
            Assert.IsFalse(PlatformRules.MushroomBounces(0f, 3f, 3f, dt, false), "No ar, parado no topo do pulo: não");
            Assert.IsFalse(PlatformRules.MushroomBounces(-5f, 2f, 3f, dt, true), "Encostou pelo lado, embaixo do chapéu");
            Assert.Greater(PlatformRules.MushroomBounceHeight, 7.2f, "Bem mais alto que o pulo (4) e que o pogo (7,2)");
        }

        // ------------------------------------------------------------ viagem rápida

        [Test]
        public void Travel_ZoneComesFromTheScene()
        {
            Assert.AreEqual(TravelRules.Forest, TravelRules.Zone("Forest_A_Roots"));
            Assert.AreEqual(TravelRules.Fortress, TravelRules.Zone("Fortress_MVP"));
            Assert.AreEqual(TravelRules.Forest, TravelRules.Zone("Forest_Road", TravelRules.Forest));
        }

        [Test]
        public void Travel_ForestAndFortress_LinkOnlyAfterTheBridge()
        {
            var d = SaveData.NewGame();
            Assert.IsTrue(TravelRules.ZonesLinked(TravelRules.Forest, TravelRules.Forest, d));
            Assert.IsFalse(TravelRules.ZonesLinked(TravelRules.Forest, TravelRules.Fortress, d), "A queda é de mão única");
            d.SetFlag(ReconstructionWood.BridgeFlag);
            Assert.IsTrue(TravelRules.ZonesLinked(TravelRules.Forest, TravelRules.Fortress, d));
            Assert.IsTrue(TravelRules.ZonesLinked(TravelRules.Fortress, TravelRules.Forest, d));
        }

        [Test]
        public void Travel_Destinations_AreVisited_Linked_AndNotHere()
        {
            var d = SaveData.NewGame();
            TravelRules.RecordVisit(d, "S-F1", "Raízes Gêmeas", "Forest_A_Roots", TravelRules.Forest);
            TravelRules.RecordVisit(d, "S-F2", "Ravina", "Forest_C_Shadows", TravelRules.Forest);
            TravelRules.RecordVisit(d, "S4", "Câmara", "Fortress_MVP", TravelRules.Fortress);
            TravelRules.RecordVisit(d, "S-F1", "Raízes Gêmeas", "Forest_A_Roots", TravelRules.Forest); // de novo: não duplica
            Assert.AreEqual(3, d.visitedSanctuaries.Count);

            var from1 = TravelRules.Destinations(d, "S-F1", "Forest_A_Roots", TravelRules.Forest);
            Assert.AreEqual(1, from1.Count, "Só a Ravina: a Fortaleza ainda não se liga");
            Assert.AreEqual("S-F2", from1[0].id);

            d.SetFlag(ReconstructionWood.BridgeFlag);
            Assert.AreEqual(2, TravelRules.Destinations(d, "S-F1", "Forest_A_Roots", TravelRules.Forest).Count);
            Assert.IsTrue(TravelRules.Visited(d, "S4", "Fortress_MVP"));
        }

        [Test]
        public void Travel_VisitsSurviveSaveNormalization()
        {
            var d = SaveData.NewGame();
            d.visitedSanctuaries = null;
            var json = UnityEngine.JsonUtility.ToJson(d);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "setevidas_teste_viagem.json");
            System.IO.File.WriteAllText(path, json);
            var back = SaveStore.Read(path, out _);
            System.IO.File.Delete(path);
            Assert.IsNotNull(back.visitedSanctuaries, "Save antigo (sem a lista) carrega com a lista vazia");
        }

        // ------------------------------------------------------------ câmera, falas, música, diário

        [Test]
        public void Camera_LooksDown_WhenFallingLong_OrCrouchingStill()
        {
            Assert.AreEqual(0f, CameraLookRules.LookDown(0.5f, 0f));
            Assert.AreEqual(CameraLookRules.FallLook, CameraLookRules.LookDown(1.2f, 0f));
            Assert.AreEqual(CameraLookRules.CrouchLook, CameraLookRules.LookDown(0f, 0.8f));
            Assert.AreEqual(0f, CameraLookRules.LookDown(0f, 0.3f), "Agachar rápido não mexe a câmera");
        }

        [Test]
        public void Barks_NeverRepeat_UntilAllWereSaid()
        {
            var pool = new BarkPool(new[] { "a", "b", "c" }, seed: 7);
            var seen = new System.Collections.Generic.HashSet<string> { pool.Next(), pool.Next(), pool.Next() };
            Assert.AreEqual(3, seen.Count, "As três saem antes de repetir");
            string last = null;
            for (int i = 0; i < 30; i++)
            {
                string next = pool.Next();
                Assert.AreNotEqual(last, next, "Nunca a mesma duas vezes seguidas");
                last = next;
            }
            Assert.AreEqual("", new BarkPool(null).Next());
        }

        [Test]
        public void Barks_RespectTheCooldown()
        {
            Assert.IsTrue(BarkPool.CooldownOver(5f, -1f, 60f), "Primeira fala");
            Assert.IsFalse(BarkPool.CooldownOver(30f, 0f, 60f));
            Assert.IsTrue(BarkPool.CooldownOver(61f, 0f, 60f));
        }

        [Test]
        public void Barks_FallLines_FitThePixelFont()
        {
            foreach (var line in LionBarks.FallLines)
                foreach (char c in line)
                    if (c != ' ') Assert.IsTrue(PixelFont.Supports(c), $"\"{c}\" de \"{line}\" não existe na fonte pixel");
        }

        [Test]
        public void Music_AreaCue_WinsOverTheLevelTrack_ButNotOverDanger()
        {
            var s = new MusicRules.Situation { AreaCue = MusicCue.Ruins };
            Assert.AreEqual(MusicCue.Ruins, MusicRules.Pick(s));
            s.AnyCombat = true;
            Assert.AreEqual(MusicCue.Combat, MusicRules.Pick(s), "Combate vence a área");
            Assert.AreEqual(MusicCue.Explore, MusicRules.Pick(new MusicRules.Situation()), "Sem área: a música da fase");
        }

        [Test]
        public void Diary_ForestScenes_AreChapterTwo()
        {
            Assert.AreEqual(1, InventoryTexts.ChapterOf("Fortress_MVP"));
            Assert.AreEqual(2, InventoryTexts.ChapterOf("Forest_A_Roots"));
            Assert.AreEqual(2, InventoryTexts.ChapterOf("Forest_Test_A"));
            Assert.AreEqual(1, InventoryTexts.ChapterOf("Playground"));
            Assert.AreEqual(1, InventoryTexts.ChapterOf(null));
        }

        [Test]
        public void Cactus_FloatsBackAndForth_WithinItsTravel()
        {
            var travel = new UnityEngine.Vector2(1.5f, 0f);
            Assert.AreEqual(0f, FloatingCactus.PathOffset(travel, 3f, 0f).x, 1e-4f, "Começa no ponto inicial");
            Assert.AreEqual(1.5f, FloatingCactus.PathOffset(travel, 3f, 0.75f).x, 1e-4f, "Um quarto do período: na ponta");
            Assert.AreEqual(-1.5f, FloatingCactus.PathOffset(travel, 3f, 2.25f).x, 1e-4f, "Três quartos: na outra ponta");
            Assert.AreEqual(UnityEngine.Vector2.zero, FloatingCactus.PathOffset(UnityEngine.Vector2.zero, 3f, 1f), "Parado");
        }

        [Test]
        public void Cactus_Pogo_IsTheDaggerOnly()
        {
            Assert.IsTrue(DownAttackRules.Bounces(true, false, true), "Adaga na bola de espinhos: quica");
            Assert.IsFalse(DownAttackRules.Bounces(false, false, true), "Marreta: queda pesada, não quica");
        }

        [Test]
        public void TestScenes_UseTheTestSave_NeverTheRealOne()
        {
            Assert.IsTrue(SaveManager.IsTestScene("Playground"));
            Assert.IsTrue(SaveManager.IsTestScene("Forest_Test_A"), "O preparador das cenas de teste mexe no save: nunca no do jogo");
            Assert.IsFalse(SaveManager.IsTestScene("Forest_A_Roots"));
            Assert.IsFalse(SaveManager.IsTestScene("Fortress_MVP"));
            Assert.IsFalse(SaveManager.IsTestScene(null));
        }

        [Test]
        public void Cutscene_CollectsSteps_AndHasASeenFlag()
        {
            var c = new Cutscene("ponte_cai").Fade(1f, 0.3f).Wait(0.5f).Do(() => { }).Fade(0f, 0.3f).AllowSkipAfterFirstTime();
            Assert.AreEqual(4, c.StepCount);
            Assert.IsTrue(c.Skippable);
            Assert.AreEqual("cena_vista_ponte_cai", c.SeenFlag);
        }
    }
}
