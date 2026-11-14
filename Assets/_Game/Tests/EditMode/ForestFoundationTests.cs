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
        public void Wood_CountsAllFlags_BridgeNeedsTwelve()
        {
            Assert.AreEqual(0, ReconstructionWood.Count(SaveData.NewGame()));
            Assert.AreEqual(8, ReconstructionWood.Count(WithWood(8)));
            Assert.AreEqual(14, ReconstructionWood.Count(WithWood(14)), "Conta todas: a floresta tem 18");
            Assert.AreEqual(4, ReconstructionWood.Missing(WithWood(8)));
            Assert.AreEqual(0, ReconstructionWood.Missing(WithWood(14)), "Sobrando não falta nada");
            Assert.AreEqual(0, ReconstructionWood.Count(null));
        }

        [Test]
        public void Wood_LabelsMatchTheDesign()
        {
            Assert.AreEqual("Madeira de Reconstrução: 8/18", ReconstructionWood.Label(WithWood(8)));
            Assert.AreEqual("A ponte precisa de 12 (faltam 4)", ReconstructionWood.BridgeHint(WithWood(8)));
            Assert.AreEqual("Já dá para a ponte (12)", ReconstructionWood.BridgeHint(WithWood(13)));
            Assert.AreEqual("+1 Madeira de Reconstrução", ReconstructionWood.PickupText); // sem contador: a conta fica no inventário
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
            Assert.IsFalse(PlatformRules.WantsDropThrough(true, true, true, 0f, 0.05f), "Acabou de pousar fazendo pogo (baixo + pulo guardado): não cai através");
            Assert.IsTrue(PlatformRules.WantsDropThrough(true, true, true, 0f, 0.2f), "Em pé há um instante: desce");
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

        // ------------------------------------------------------------ mapa

        static MapRoomRecord Room(string scene, string id, string region, float x, float y, float w, float h) =>
            new MapRoomRecord { scene = scene, id = id, region = region, x = x, y = y, w = w, h = h };

        [Test]
        public void Map_RecordsRooms_OncePerSceneAndId_GroupedByRegion()
        {
            var d = SaveData.NewGame();
            Assert.IsTrue(MapRules.Record(d, Room("Forest_A_Roots", "A1", "Raízes", 0, 0, 30, 17)));
            Assert.IsTrue(MapRules.Record(d, Room("Forest_A_Roots", "A2", "Raízes", 30, 0, 30, 34)));
            Assert.IsTrue(MapRules.Record(d, Room("Forest_B_Thorns", "A1", "Bosque", 0, 40, 30, 51)), "Mesmo id, outra cena: outra sala");
            Assert.IsFalse(MapRules.Record(d, Room("Forest_A_Roots", "A1", "Raízes", 0, 0, 31, 17)), "Já conhecida: atualiza, não duplica");
            Assert.AreEqual(3, d.mapRooms.Count);
            Assert.AreEqual(31f, d.mapRooms[0].w, "A forma nova vale");
            CollectionAssert.AreEqual(new[] { "Raízes", "Bosque" }, MapRules.Regions(d), "Na ordem da descoberta");
            Assert.AreEqual(2, MapRules.RoomsIn(d, "Raízes").Count);
            Assert.IsTrue(MapRules.Seen(d, "Forest_A_Roots", "A2"));
            Assert.IsFalse(MapRules.Record(d, null));
        }

        [Test]
        public void Map_FitsTheRegion_InThePanel_Centered()
        {
            var rooms = new System.Collections.Generic.List<MapRoomRecord> { Room("s", "a", "r", 0, 0, 30, 17), Room("s", "b", "r", 30, -10, 20, 40) };
            var world = MapRules.Bounds(rooms);
            Assert.AreEqual(new UnityEngine.Rect(0, -10, 50, 40), world);
            var panel = new UnityEngine.Vector2(270f, 132f);
            float s = MapRules.FitScale(world, panel);
            Assert.AreEqual(3f, s, "Escala inteira que cabe com margem (pixel art)");
            Assert.LessOrEqual(world.width * s, panel.x);
            Assert.LessOrEqual(world.height * s, panel.y);
            var center = MapRules.ToPanel(world.center, world, panel, s);
            Assert.AreEqual(panel.x * 0.5f, center.x, 1e-3f);
            Assert.AreEqual(panel.y * 0.5f, center.y, 1e-3f);
        }

        [Test]
        public void Map_ChestAndSanctuaryIcons_FollowTheSave()
        {
            var d = SaveData.NewGame();
            Assert.IsFalse(MapRules.ChestOpen(d, "W04"));
            d.SetFlag(ReconstructionWood.ChestFlag("W04"));
            Assert.IsTrue(MapRules.ChestOpen(d, "W04"), "Baú aberto aparece apagado no mapa");
            Assert.IsFalse(MapRules.SanctuaryLit(d, "Forest_A_Roots", "S-F1"));
            TravelRules.RecordVisit(d, "S-F1", "Raízes Gêmeas", "Forest_A_Roots", TravelRules.Forest);
            Assert.IsTrue(MapRules.SanctuaryLit(d, "Forest_A_Roots", "S-F1"));
        }

        // ------------------------------------------------------------ chave, porta e arena

        [Test]
        public void Door_OpensOnlyWithItsKey_AndStaysOpen()
        {
            var d = SaveData.NewGame();
            Assert.IsFalse(KeyRules.CanOpen(d, "cabana_vulpis", "cabana"), "Sem chave: trancada");
            d.SetFlag(KeyRules.KeyFlag("cabana"));
            Assert.IsTrue(KeyRules.HasKey(d, "cabana"));
            Assert.IsTrue(KeyRules.CanOpen(d, "cabana_vulpis", "cabana"));
            d.SetFlag(KeyRules.DoorFlag("cabana_vulpis"));
            Assert.IsTrue(KeyRules.DoorOpen(d, "cabana_vulpis"), "Aberta para sempre");
            Assert.AreEqual("Chave da cabana", KeyRules.Name("cabana"));
        }

        [Test]
        public void Keys_LeaveThePocket_AfterTheirDoorOpens()
        {
            var d = SaveData.NewGame();
            d.SetFlag(KeyRules.KeyFlag("cabana"));
            CollectionAssert.AreEqual(new[] { "cabana" }, KeyRules.KeysInPocket(d, KeyRules.DoorOfKey));
            d.SetFlag(KeyRules.DoorFlag("cabana_vulpis"));
            Assert.AreEqual(0, KeyRules.KeysInPocket(d, KeyRules.DoorOfKey).Count, "Usada: some do inventário");
        }

        [Test]
        public void Arena_ClosesOnEntry_OpensWhenEveryoneFalls_ReopensOnDeath()
        {
            var s = ArenaRules.State.Waiting;
            Assert.AreEqual(ArenaRules.State.Waiting, ArenaRules.Step(s, false, false, 3), "Fora da sala: espera");
            s = ArenaRules.Step(s, true, false, 3);
            Assert.AreEqual(ArenaRules.State.Fighting, s, "Entrou: fecha");
            Assert.AreEqual(ArenaRules.State.Fighting, ArenaRules.Step(s, true, false, 1), "Ainda tem inimigo");
            Assert.AreEqual(ArenaRules.State.Waiting, ArenaRules.Step(s, true, true, 2), "Morreu lá dentro: reabre");
            s = ArenaRules.Step(s, true, false, 0);
            Assert.AreEqual(ArenaRules.State.Cleared, s, "Todos caíram: vencida");
            Assert.AreEqual(ArenaRules.State.Cleared, ArenaRules.Step(s, true, false, 3), "Vencida para sempre");
            Assert.AreEqual(ArenaRules.State.Waiting, ArenaRules.Step(ArenaRules.State.Waiting, true, false, 0), "Sem inimigos: não fecha");
            Assert.AreEqual("arena_vencida_C2", ArenaRules.ClearedFlag("C2"));
        }

        // ------------------------------------------------------------ inimigos da Floresta (versão básica)

        [Test]
        public void DryBranch_StaysPut_IsTall_SwingsThenGetsStuck()
        {
            Assert.Greater(SeteVidas.Enemies.ForestEnemyRules.BranchHeight, 5f, "Mais alto que pulo + agarrar a borda: não dá para passar por cima");
            float t = 0f;
            var s = SeteVidas.Enemies.ForestEnemyRules.BranchState.Disguised;
            s = SeteVidas.Enemies.ForestEnemyRules.StepBranch(s, ref t, 0.1f, 6f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BranchState.Disguised, s, "Gato longe: continua tronco");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBranch(s, ref t, 0.1f, 3f, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BranchState.Disguised, s, "Escondido na sombra: não acorda");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBranch(s, ref t, 0.1f, 3f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BranchState.Awake, s, "Perto e visível: acorda (e fica parado)");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBranch(s, ref t, 0.1f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BranchState.Windup, s, "Ao alcance: levanta o braço");
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.BranchVulnerable(s), "Avisando: ainda não dá pogo");
            for (int i = 0; i < 7; i++) s = SeteVidas.Enemies.ForestEnemyRules.StepBranch(s, ref t, 0.1f, 2f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BranchState.Stuck, s, "Depois de 0,6 s: bate e o braço fica preso");
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.BranchVulnerable(s));
        }

        [Test]
        public void Sprout_ReleasesBats_EveryFewSeconds_UpToTwo()
        {
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.SproutSpawns(4f, 0, true));
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.SproutSpawns(2f, 0, true), "Ainda no intervalo");
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.SproutSpawns(9f, 2, true), "Já tem 2 morcegos dele no ar");
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.SproutSpawns(9f, 0, false), "Gato longe: não solta");
        }

        [Test]
        public void Plant_LongNeckLunge_ReachesFar_AndOnlyRetractAllowsPogo()
        {
            Assert.GreaterOrEqual(SeteVidas.Enemies.ForestEnemyRules.PlantReach, 6f, "Pega bem longe (pedido do usuário)");
            float t = 0f;
            var s = SeteVidas.Enemies.ForestEnemyRules.PlantState.Idle;
            s = SeteVidas.Enemies.ForestEnemyRules.StepPlant(s, ref t, 0.05f, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.PlantState.Idle, s);
            s = SeteVidas.Enemies.ForestEnemyRules.StepPlant(s, ref t, 0.05f, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.PlantState.Windup, s);
            bool sawLunge = false, sawHold = false;
            for (int i = 0; i < 40; i++)
            {
                s = SeteVidas.Enemies.ForestEnemyRules.StepPlant(s, ref t, 0.05f, true);
                if (s == SeteVidas.Enemies.ForestEnemyRules.PlantState.Lunge) sawLunge = true;
                if (s == SeteVidas.Enemies.ForestEnemyRules.PlantState.Hold) { sawHold = true; Assert.AreEqual(1f, SeteVidas.Enemies.ForestEnemyRules.PlantExtension(s, t), "Lá na ponta"); }
                if (s == SeteVidas.Enemies.ForestEnemyRules.PlantState.Retract) break;
            }
            Assert.IsTrue(sawLunge && sawHold, "Aviso -> bote -> ponta -> volta");
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.PlantVulnerable(s), "Voltando: pogo na cabeça");
            Assert.IsFalse(SeteVidas.Enemies.ForestEnemyRules.PlantBites(s), "Voltando não morde");
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.PlantBites(SeteVidas.Enemies.ForestEnemyRules.PlantState.Lunge));
            Assert.AreEqual(0f, SeteVidas.Enemies.ForestEnemyRules.PlantExtension(SeteVidas.Enemies.ForestEnemyRules.PlantState.Idle, 0f));
        }

        [Test]
        public void Plant_NeverBitesBehindItsWall()
        {
            var up = UnityEngine.Vector2.up;
            var aimSide = SeteVidas.Enemies.ForestEnemyRules.PlantAim(up, new UnityEngine.Vector2(1f, 0.1f));
            Assert.Greater(aimSide.y, 0f, "Gato quase de lado: o bote vai até 80°, nunca para trás");
            var aimBack = SeteVidas.Enemies.ForestEnemyRules.PlantAim(up, new UnityEngine.Vector2(0f, -1f));
            Assert.Greater(aimBack.y, 0f);
            Assert.AreEqual(1f, aimSide.magnitude, 1e-4f);
        }

        [Test]
        public void Bat_SleepsUntilNearOrNoise_ChasesAndRecoilsAfterHitting()
        {
            float t = 0f;
            var s = SeteVidas.Enemies.ForestEnemyRules.BatState.Sleep;
            s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 4.5f, true, false, false, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Sleep, s, "Agachado a 4,5 tiles: passa sem acordar");
            Assert.IsTrue(SeteVidas.Enemies.ForestEnemyRules.BatWakes(6.5f, false, false), "De pé, acorda de longe (7 tiles)");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 8f, false, true, false, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Wake, s, "Barulho: acorda");
            for (int i = 0; i < 5; i++) s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 8f, false, false, false, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Chase, s, "Persegue");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 0.5f, false, false, true, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Recoil, s, "Encostou: recua");
            s = SeteVidas.Enemies.ForestEnemyRules.BatState.Chase;
            s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 20f, false, false, false, false);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Return, s, "Longe demais: desiste e volta");
            s = SeteVidas.Enemies.ForestEnemyRules.StepBat(s, ref t, 0.1f, 20f, false, false, false, true);
            Assert.AreEqual(SeteVidas.Enemies.ForestEnemyRules.BatState.Sleep, s, "No poleiro: dorme de novo");
        }


        [Test]
        public void PogoReach_MatchesTheDaggerNumbers()
        {
            Assert.LessOrEqual(9f - 6f, PlatformRules.MaxBranchStep, "Galhos da B (6 e 9): degrau que o pulo alcança");
            Assert.Less(PlatformRules.MaxBranchStep, PlatformRules.JumpApex, "Degrau de galho sempre abaixo do limite do pulo");
            Assert.IsTrue(PlatformRules.ReachesCactus(false, 0f, 9f, 38.5f, 11.5f, checkSide: false), "Do galho (9) até o B5");
            Assert.IsTrue(PlatformRules.CactusReachesFloor(11.5f, 15f), "Do B5 até o patamar");
            Assert.AreEqual(4f * 1.8f + 1.2f, PlatformRules.CactusApex, 1e-3f, "Quique 7,2 a partir de ~1,2 acima do centro");
            Assert.IsTrue(PlatformRules.ReachesCactus(false, 0f, 0f, 17.5f, 2.8f, checkSide: false), "Do chão até 3");
            Assert.IsFalse(PlatformRules.ReachesCactus(false, 0f, 0f, 0f, 3.5f, checkSide: false), "O pulo não chega");
            Assert.IsTrue(PlatformRules.ReachesCactus(true, 17.5f, 2.8f, 21.5f, 8.3f), "Corrente do abrigo da B, passo 1");
            Assert.IsTrue(PlatformRules.ReachesCactus(true, 21.5f, 8.3f, 18f, 14.8f), "Passo 2");
            Assert.IsTrue(PlatformRules.CactusReachesFloor(14.8f, 18f), "Último cacto até o abrigo");
            Assert.IsFalse(PlatformRules.ReachesCactus(true, 18.2f, 3.2f, 25f, 9f), "O desenho antigo: 6,8 para o lado, longe demais");
            Assert.IsFalse(PlatformRules.ReachesCactus(true, 0f, 0f, 0f, 7.5f), "Mais de 7,4 acima: não alcança");
        }

        [Test]
        public void Blade_SwingsSideToSide_WithinItsAngle()
        {
            Assert.AreEqual(0f, SeteVidas.World.Traps.SwingingBlade.AngleAt(50f, 2.4f, 0f), 1e-4f, "Começa pendurada reta");
            Assert.AreEqual(50f, SeteVidas.World.Traps.SwingingBlade.AngleAt(50f, 2.4f, 0.6f), 1e-3f, "Um quarto: na ponta");
            Assert.AreEqual(-50f, SeteVidas.World.Traps.SwingingBlade.AngleAt(50f, 2.4f, 1.8f), 1e-3f, "Três quartos: na outra ponta");
        }

        [Test]
        public void TrapSwitch_TurnsItsGroupOff_ForGood()
        {
            var d = SaveData.NewGame();
            Assert.IsFalse(SeteVidas.World.Traps.TrapSwitch.IsOff(d, "teste_C_baixo"));
            d.SetFlag(SeteVidas.World.Traps.TrapSwitch.Flag("teste_C_baixo"));
            Assert.IsTrue(SeteVidas.World.Traps.TrapSwitch.IsOff(d, "teste_C_baixo"), "Pisou: desligadas (no save)");
            Assert.IsFalse(SeteVidas.World.Traps.TrapSwitch.IsOff(d, "outro_grupo"), "Só o grupo do botão");
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
