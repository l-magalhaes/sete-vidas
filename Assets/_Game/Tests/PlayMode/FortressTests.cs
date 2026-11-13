using System.Collections;
using NUnit.Framework;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Fase 6: a Fortaleza vertical (Docs/Level/fortaleza-mvp.md). Começa no alto da montanha, desce até o pátio,
    /// sobe pelas vinhas e pelas torres até a Câmara do Selo no topo, e sai pelos fundos depois do Paladino.
    /// </summary>
    public class FortressTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Fortress_MVP", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(GameObject.Find("Fortaleza"), "Fortaleza não gerada. Rode Tools > Sete Vidas > 13");
        }

        static Tilemap Map(string name) => GameObject.Find("Grid/" + name).GetComponent<Tilemap>();

        /// <summary>Melhoria 9: nenhuma palavra flutuando no mundo; entrar na área de uma dica mostra o pergaminho, sair esconde.</summary>
        [UnityTest]
        public IEnumerator Tutorial_HintsInAScroll_NoFloatingText()
        {
            yield return Load();
            foreach (var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                Assert.IsFalse(tm.name.StartsWith("Placa: ") || (tm.transform.parent != null && tm.transform.parent.name.StartsWith("Placa: ")), $"Texto flutuando: {tm.text}");
            var hints = Object.FindObjectsByType<TutorialHint>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(hints.Length, 10, "As dicas do tutorial no lugar dos textos");

            TutorialHint jump = null; // a dica de atacar, nos bonecos do pátio (some ao atacar; o teste não ataca)
            foreach (var h in hints) if (h.Id == "atacar") jump = h;
            Assert.IsNotNull(jump, "Dica de atacar");
            var data = SeteVidas.Save.SaveManager.Data;
            data.flags.Remove(SeteVidas.UI.TutorialScroll.FlagPrefix + "atacar");
            data.flags.Remove(SeteVidas.UI.TutorialScroll.DisabledFlag);
            // o gato nasce na área da dica "andar" (penhasco do início): marca as do começo como vistas para elas
            // não ocuparem o pergaminho antes da dica testada
            data.SetFlag(SeteVidas.UI.TutorialScroll.FlagPrefix + "andar");
            data.SetFlag(SeteVidas.UI.TutorialScroll.FlagPrefix + "salto");
            SeteVidas.UI.TutorialScroll.HintsDisabled = false;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var health = player.GetComponent<PlayerHealth>();
            health.Shielded = true;   // nenhuma flecha ou armadilha manda o gato de volta ao começo durante o teste
            for (int i = 0; i < 5; i++) yield return null;   // a cena assenta (Start, primeiro passo da física)
            // a fala de abertura pausa o jogo (sem física, a área da dica não percebe o gato): passa as falas
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            for (int i = 0; i < 20 && runner != null && runner.IsOpen; i++) runner.Advance();
            yield return null;
            Assert.Greater(Time.timeScale, 0f, "O jogo voltou a rodar depois da fala");
            player.Teleport(new Vector2(jump.transform.position.x, jump.transform.position.y + 0.05f));
            for (int i = 0; i < 20; i++) yield return null;
            Assert.IsTrue(SeteVidas.UI.TutorialScroll.Exists, "O pergaminho foi criado");
            Assert.AreEqual("atacar", SeteVidas.UI.TutorialScroll.Get().CurrentId,
                $"Dica na tela errada; gato em {player.Body.position}, dica em {jump.transform.position}");
            health.Shielded = false;

            player.Teleport(new Vector2(jump.transform.position.x - 8f, jump.transform.position.y + 0.05f)); // longe das outras dicas do pátio
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsNull(SeteVidas.UI.TutorialScroll.Get().CurrentId, "Saiu da área: a dica sai");
        }

        /// <summary>Melhoria 10: o inventário abre (pausa o jogo e trava o controle) e fecha (volta tudo); não abre em diálogo.</summary>
        [UnityTest]
        public IEnumerator Inventory_OpensPausedAndCloses()
        {
            yield return Load();
            for (int i = 0; i < 3; i++) yield return null;
            var menu = SeteVidas.UI.InventoryMenu.Ensure();
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            if (runner != null && runner.IsOpen)
            {
                Assert.IsFalse(SeteVidas.UI.InventoryMenu.CanOpen(), "Durante o diálogo, não abre");
                for (int i = 0; i < 20 && runner.IsOpen; i++) runner.Advance();
                yield return null;
            }
            Assert.IsFalse(SeteVidas.UI.InventoryMenu.IsOpen);
            menu.Open();
            Assert.IsTrue(SeteVidas.UI.InventoryMenu.IsOpen, "Abriu");
            Assert.AreEqual(0f, Time.timeScale, "O jogo pausa");
            var input = Object.FindAnyObjectByType<PlayerController>().GetComponent<PlayerInputReader>();
            Assert.IsTrue(input.GameplayLocked, "O controle do gato trava");
            // aba Classes: só as recuperadas aparecem, o Ladino equipado; Enter na equipada não faz nada
            menu.SetPage(SeteVidas.UI.InventoryMenu.Page.Classes);
            var save = SeteVidas.Save.SaveManager.Data;
            Assert.AreEqual(save.unlockedClasses.Count, menu.Entries.Count, "As classes que faltam ficam escondidas");
            Assert.IsTrue(menu.Entries[0].Equipped, "O Ladino está equipado");
            menu.Select(0);
            Assert.IsFalse(menu.Confirm(), "Já equipada: nada muda");

            // o gato do retrato respira com o jogo pausado (tempo real)
            Assert.IsTrue(menu.Cat.HasArt, "O retrato usa as animações do gato");
            var seen = new System.Collections.Generic.HashSet<Sprite>();
            for (int i = 0; i < 30; i++) { yield return new WaitForSecondsRealtime(0.05f); seen.Add(menu.Cat.BodySprite); }
            Assert.Greater(seen.Count, 1, "A animação idle anda mesmo pausado");
            Assert.IsNull(menu.Cat.WeaponSprite, "Ladino: sem marreta");

            // equipar o Paladino pelo inventário: a marreta aparece nas costas na hora
            var data = SeteVidas.Save.SaveManager.Data;
            bool hadPaladin = data.IsUnlocked(SeteVidas.Classes.CharacterClass.Paladin);
            if (!hadPaladin) data.unlockedClasses.Add(nameof(SeteVidas.Classes.CharacterClass.Paladin));
            menu.SetPage(SeteVidas.UI.InventoryMenu.Page.Classes);
            menu.Select(1);
            Assert.IsTrue(menu.Confirm(), "Equipou o Paladino");
            Assert.AreEqual(SeteVidas.Classes.CharacterClass.Paladin, SeteVidas.Classes.ClassManager.Active.CurrentClass);
            yield return null;
            Assert.IsNotNull(menu.Cat.WeaponSprite, "A marreta aparece no retrato");
            SeteVidas.Classes.ClassManager.Active.TrySwitch(SeteVidas.Classes.CharacterClass.Rogue);
            if (!hadPaladin) data.unlockedClasses.Remove(nameof(SeteVidas.Classes.CharacterClass.Paladin));

            // Habilidades: só as que o gato tem, e o pergaminho da cura (se já veio do Koda)
            menu.SetPage(SeteVidas.UI.InventoryMenu.Page.Abilities);
            int known = 0;
            foreach (var a in SeteVidas.UI.InventoryTexts.Abilities) if (a.Unlocked) known++;
            var heal = Object.FindAnyObjectByType<PlayerController>().GetComponent<SeteVidas.Player.HealingSpell>();
            bool hasHeal = heal != null && heal.IsUnlocked;
            Assert.AreEqual(known + (hasHeal ? 1 : 0), menu.Entries.Count, "Sem as travadas; a cura mora aqui");
            foreach (var e in menu.Entries) Assert.AreNotEqual("???", e.Name);

            // Itens: só as moedas (os corações ficam no painel do gato)
            menu.SetPage(SeteVidas.UI.InventoryMenu.Page.Items);
            Assert.AreEqual(1, menu.Entries.Count);
            Assert.AreEqual("Moedas", menu.Entries[0].Name);

            // Diário: o capítulo 1 tem a linha do título, as placas começam na linha de baixo
            menu.SetPage(SeteVidas.UI.InventoryMenu.Page.Diary);
            foreach (var e in menu.Entries) Assert.GreaterOrEqual(e.Cell, 5, "A 1ª linha é o título do capítulo");

            menu.Close();
            Assert.IsFalse(SeteVidas.UI.InventoryMenu.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "Volta a rodar");
            Assert.IsFalse(input.GameplayLocked);
            Assert.AreEqual("Ladino", SeteVidas.UI.InventoryTexts.Classes[0].Name, "A 1ª carta é a do Ladino");
        }

        /// <summary>Música: o diretor está na cena e o início, no penhasco, já toca a música da fase.</summary>
        [UnityTest]
        public IEnumerator Music_StartsWithTheLevelTheme()
        {
            yield return Load();
            var director = SeteVidas.Audio.MusicDirector.Active;
            Assert.IsNotNull(director, "Diretor de música na cena (menu 13)");
            for (int i = 0; i < 30; i++) yield return null;
            Assert.AreEqual(SeteVidas.Audio.MusicCue.Explore, director.Current, "O penhasco usa a música da fase");
            Assert.IsNotNull(director.CurrentClip, "A faixa da fase está tocando (Assets/_Game/Audio/Music)");
            Assert.AreEqual("mus_fortaleza_base", director.CurrentClip.name);
        }

        /// <summary>Melhoria 8: escuridão nas duas pontas, na frente de tudo; o fim do mapa (x 251) fica no preto.</summary>
        [UnityTest]
        public IEnumerator EdgeFog_CoversBothEndsOfTheMap()
        {
            yield return Load();
            var fog = GameObject.Find("Escuridão das pontas");
            Assert.IsNotNull(fog, "Sem a escuridão das pontas. Rode Tools > Sete Vidas > 13");
            var renderers = fog.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(4, renderers.Length, "Gradiente e preto na saída e na entrada");
            bool wallHidden = false, startHidden = false;
            foreach (var r in renderers)
            {
                Assert.IsNotNull(r.sprite, r.name);
                Assert.AreEqual(25, r.sortingOrder, $"{r.name}: na frente de tudo");
                if (r.name.Contains("preto") && r.bounds.min.x <= 247.01f && r.bounds.max.x > 251f) wallHidden = true;
                if (r.name.Contains("preto") && r.bounds.max.x >= -13.01f && r.bounds.min.x < -18f) startHidden = true;
            }
            Assert.IsTrue(wallHidden, "A parede do fim (x 251) fica no preto");
            Assert.IsTrue(startHidden, "A rocha do começo (x -18) fica no preto");
        }

        /// <summary>Melhoria 2: caindo forte na lagoa (como no salto de fé), nasce o respingo grande, só na água.</summary>
        [UnityTest]
        public IEnumerator Lake_StrongFall_MakesBigSplash()
        {
            yield return Load();
            var water = Object.FindAnyObjectByType<WaterSurface>();
            Assert.IsNotNull(water, "Lagoa sem WaterSurface. Rode Tools > Sete Vidas > 13");
            var player = Object.FindAnyObjectByType<PlayerController>();
            var area = water.GetComponent<BoxCollider2D>().bounds;
            player.Teleport(new Vector2(area.center.x, water.SurfaceY + 1.5f));
            player.StartJump(new Vector2(0f, -15f));
            for (int i = 0; i < 30 && water.LastSplash == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(2, water.LastSplash, "Queda a 15 tiles/s: respingo grande");
            Assert.Greater(water.AliveCount, 0, "O respingo está tocando");
            Assert.LessOrEqual(water.AliveCount, WaterSurface.MaxAlive);
        }

        [UnityTest]
        public IEnumerator Start_CatOnTopOfTheMountain_SevenHearts()
        {
            yield return Load();
            var health = PlayerHealth.Active;
            Assert.IsNotNull(health);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(health.GetComponent<PlayerController>().IsGrounded, "O gato deveria estar no alto do penhasco");
            Assert.AreEqual(60f, health.transform.position.y, 0.15f, "O jogo começa no alto da montanha (y 60)");
            Assert.AreEqual(7, health.HeartsRecovered);
        }

        [UnityTest]
        public IEnumerator HasEveryArea()
        {
            yield return Load();
            // só 2 santuários (melhorias, item 0): na metade do caminho e antes do chefe
            Assert.AreEqual(2, Object.FindObjectsByType<Sanctuary>(FindObjectsSortMode.None).Length, "Dois santuários na Fortaleza");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_muralha"), "Santuário do portão da muralha (metade)");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_camara"), "Santuário antes da câmara (chefe)");
            Assert.IsNull(Sanctuary.Find("fortaleza_patio"), "O do pátio saiu");
            Assert.IsNull(Sanctuary.Find("fortaleza_sino"), "O da Torre do Sino saiu");
            Assert.GreaterOrEqual(GameObject.Find("Fortaleza").GetComponentsInChildren<EnemyAI>().Length, 10, "Dez templários no caminho");
            var boss = Object.FindAnyObjectByType<GoldenBoss>();
            Assert.IsNotNull(boss, "Koda na câmara");
            Assert.Greater(boss.transform.position.y, 100f, "A Câmara do Selo fica no topo da torre");
            Assert.IsNotNull(Object.FindAnyObjectByType<CursedTome>(), "Tomo");
            Assert.GreaterOrEqual(Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Length, 7, "Paredes rachadas: estábulo, tomo, cofres e portão");
            Assert.GreaterOrEqual(Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, 20, "Moedas pelas fases");
            Assert.IsNull(GameObject.Find("TrapCourse"), "A Fortaleza não pode ter coisas da Playground");
        }

        [UnityTest]
        public IEnumerator Waterfall_AndSunset_AtTheStart()
        {
            yield return Load();
            Assert.IsNotNull(GameObject.Find("Fortaleza/Cachoeira"), "Cachoeira no penhasco do início");
            Assert.IsNotNull(GameObject.Find("Fortaleza/Lagoa"), "Lagoa onde o gato cai");
            Assert.IsNotNull(GameObject.Find("Céu e montanhas"), "Céu do pôr do sol e montanhas");
            var far = GameObject.Find("Céu e montanhas/Conjunto A: Alto da montanha/bg_ext_cordilheira");
            Assert.IsNotNull(far, "Conjunto A (alto da montanha) montado pela biblioteca de cenário");
            Assert.IsNotNull(far.GetComponent<SeteVidas.World.ParallaxLayer>(), "com paralaxe");
            // a Cidadela saiu do fundo do início (daqui ela já está perto); no lugar, Aetherion corrompida ao longe
            Assert.IsNull(GameObject.Find("Céu e montanhas/Conjunto A: Alto da montanha/bg_ext_cordilheira_a"), "Sem a Cidadela ao longe");
            var tower = GameObject.Find("Céu e montanhas/Conjunto A: Alto da montanha/bg_ext_aetherion");
            Assert.IsNotNull(tower, "Aetherion, a torre dos magos, ao longe");
            Assert.IsNotNull(tower.GetComponent<SeteVidas.World.AetherionFx>(), "com a corrupção animada");
            Assert.IsNull(GameObject.Find("Céu e montanhas/Conjunto V: Vale, muralhas e galeria/bg_ext_montanha_inicio"),
                "O penhasco do fundo do vale saiu: fica a floresta");
            Assert.AreEqual("A", Object.FindAnyObjectByType<SeteVidas.World.Scenery.BackgroundSets>().CurrentSet);
        }

        [UnityTest]
        public IEnumerator Windows_CutTheBackWall_ToShowTheSkyOfThatHeight()
        {
            yield return Load();
            var root = GameObject.Find("Janelas");
            Assert.IsNotNull(root, "Janelas dos interiores (Etapa 6B)");
            Assert.AreEqual(12, root.transform.childCount, "galeria, salão, armadilhas, câmara e fundos");
            foreach (Transform w in root.transform)
            {
                Assert.IsNotNull(w.GetComponent<SpriteRenderer>().sprite, w.name + ": moldura");
                Assert.IsNotNull(w.GetComponentInChildren<SpriteMask>().sprite, w.name + ": recorte");
            }
            var wall = GameObject.Find("Grid/Background").GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            Assert.AreEqual(SpriteMaskInteraction.VisibleOutsideMask, wall.maskInteraction,
                "a parede do fundo some dentro das janelas e o fundo daquela altura aparece");
        }

        [UnityTest]
        public IEnumerator Background_ChangesWithTheArea_WithCrossfade()
        {
            yield return Load();
            var sets = Object.FindAnyObjectByType<SeteVidas.World.Scenery.BackgroundSets>();
            Assert.IsNotNull(sets, "BackgroundSets (Etapa 6)");
            Assert.AreEqual("A", sets.CurrentSet, "começa no alto da montanha");
            var player = PlayerHealth.Active.GetComponent<PlayerController>();
            player.Teleport(new Vector2(60f, 0.05f));                 // pátio: vale ao pé do castelo
            yield return null;
            Assert.AreEqual("V", sets.CurrentSet, "vale, muralhas e galeria: um exterior só");
            Assert.IsTrue(sets.IsFading, "troca suave, não um corte");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(sets.IsFading);
            Assert.IsFalse(GameObject.Find("Céu e montanhas").transform.Find("Conjunto A: Alto da montanha").gameObject.activeSelf,
                "o conjunto que saiu é desligado");
            player.Teleport(new Vector2(132f, 43.05f));               // alto da muralha 3: mesmo conjunto, só o céu e o horizonte mudam
            yield return null;
            Assert.AreEqual("V", sets.CurrentSet, "subir a muralha não troca o ambiente");
            Assert.IsFalse(sets.IsFading);
            player.Teleport(new Vector2(240f, 0.05f));                // a estrada depois do portão dos fundos: noite
            yield return null;
            Assert.AreEqual("H", sets.CurrentSet);
        }

        [UnityTest]
        public IEnumerator Exterior_HasNaturalTerrain_Slope_AndDetails()
        {
            yield return Load();
            var ground = Map("Ground");
            var top = ground.GetTile(new Vector3Int(0, FortressLayoutTop - 1, 0)) as Tile;
            Assert.IsNotNull(top, "Chão do alto da montanha");
            StringAssert.StartsWith("tile_ext_solo_topo", top.sprite.name, "Superfície da montanha com grama (TerrainPainter)");
            Assert.AreEqual(Tile.ColliderType.Grid, top.colliderType, "O desenho muda, a colisão continua o quadrado cheio");
            var rock = ground.GetTile(new Vector3Int(27, 1, 0)) as Tile;
            StringAssert.StartsWith("tile_ext_rocha_topo", rock.sprite.name, "Pedra de 2 da trilha é rocha");

            Assert.IsNull(ground.GetTile(new Vector3Int(15, -1, 0)), "Bacia da lagoa: 1 tile de fundo no pé do penhasco");
            Assert.IsNotNull(GameObject.Find("Rampas/Rampa da margem da lagoa"), "Margem da lagoa em rampa: sai-se andando");
            Assert.IsNotNull(GameObject.Find("Fortaleza/Cachoeira/Nascente"), "A cachoeira nasce de uma fenda no penhasco");

            var slope = GameObject.Find("Rampas/Rampa do morrinho");
            Assert.IsNotNull(slope, "Rampa do morrinho atrás do início");
            Assert.IsNotNull(slope.GetComponent<PolygonCollider2D>());
            Assert.AreEqual(2, slope.GetComponentsInChildren<SpriteRenderer>().Length, "Rampa 1:2 de 2 tiles: dois pedaços de arte final");

            var details = GameObject.Find("Detalhes do terreno");
            Assert.IsNotNull(details);
            Assert.Greater(details.transform.childCount, 30, "Tufos, seixos e fendas espalhados pelo exterior");
            int puddles = 0;
            foreach (Transform t in details.transform) if (t.name == "env_sol_arq_poca") puddles++;
            int tubs = 0;
            foreach (Transform t in details.transform) if (t.GetComponent<SeteVidas.World.WaterTub>() != null) tubs++;
            Assert.Greater(puddles, 0, "Poças no chão embaixo das gárgulas (a água que pinga se junta)");
            Assert.GreaterOrEqual(tubs, 5, "Tinas de água com física embaixo das gárgulas de fora (pátio e muralhas)");
            foreach (var sr in details.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.IsNotNull(sr.sprite, sr.name);
                Assert.IsTrue(sr.sortingOrder == 1 || sr.sortingOrder == 2 || sr.sortingOrder == -9, $"{sr.name}: terreno_detalhe (1), gota da gárgula (2) ou acontecimento na parede do fundo (-9)");
            }
        }

        const int FortressLayoutTop = 60; // FortressLayout.CliffTop (o layout fica no editor)

        static string SpriteAt(string map, int x, int y) => (Map(map).GetTile(new Vector3Int(x, y, 0)) as Tile)?.sprite?.name ?? "";

        [UnityTest]
        public IEnumerator Citadel_StoneTellsTheAge_AndEveryVineHasAGargoyle()
        {
            yield return Load();
            // a pedra muda com a altura: fundação embaixo, Soluki no meio, reforma em cima (muralhas em degrau)
            StringAssert.StartsWith("tile_cid_fundacao_", SpriteAt("Ground", 115, 10), "muralha 1: fundação");
            StringAssert.StartsWith("tile_cid_soluki_", SpriteAt("Ground", 124, 20), "muralha 2: Soluki");
            StringAssert.StartsWith("tile_cid_reforma_", SpriteAt("Ground", 132, 38), "muralha 3: reforma");
            StringAssert.StartsWith("tile_ext_terra_batida_topo", SpriteAt("Ground", 70, -1), "pátio de terra batida");
            StringAssert.StartsWith("tile_sol_madeira_", SpriteAt("Ground", 52, 6), "arquibancada de madeira");
            StringAssert.StartsWith("tile_cid_soluki_fundo_", SpriteAt("Background", 160, 46), "parede do fundo da galeria");
            StringAssert.StartsWith("tile_cid_fundacao_escalavel_", SpriteAt("Climbable", 112, 10), "vinha 1 na pedra da fundação");

            var details = GameObject.Find("Detalhes do terreno").transform;
            int gargoyles = 0, events = 0;
            foreach (Transform d in details)
            {
                if (d.name.StartsWith("env_sol_arq_gargula") || d.name.StartsWith("env_sol_arq_calha")) gargoyles++;
                if (d.name.StartsWith("env_cid_parede_")) events++;
            }
            Assert.GreaterOrEqual(gargoyles, 12, "Uma gárgula (ou calha) no topo de cada vinha (13 vinhas no mapa)");
            Assert.Greater(events, 10, "Acontecimentos nas paredes do fundo (regra contra repetição)");
        }

        [UnityTest]
        public IEnumerator Vegetation_DensityFalls_FromMountainToWall_AndGrassReacts()
        {
            yield return Load();
            int mountain = 0, patio = 0, swaying = 0;
            foreach (Transform t in GameObject.Find("Fortaleza/Enfeites").transform)
            {
                if (!t.name.StartsWith("Anciã") && !t.name.StartsWith("Jovem") && !t.name.StartsWith("Muda") && !t.name.StartsWith("Adulta") &&
                    !t.name.StartsWith("Arbusto") && !t.name.StartsWith("Árvore") && !t.name.StartsWith("Toco") && !t.name.StartsWith("Mato")) continue;
                if (t.position.x < 13f) mountain++;
                else if (t.position.x >= 44f && t.position.x < 112f) patio++;
                if (t.GetComponent<SeteVidas.World.SpriteLoop>() != null) swaying++;
            }
            Assert.Greater(mountain, patio, "mais vegetação na montanha que no pátio (a mata só invade as bordas do forte)");
            Assert.Greater(swaying, 10, "copas e arbustos balançando com o vento");

            int bending = Object.FindObjectsByType<SeteVidas.World.Scenery.BendOnPass>(FindObjectsInactive.Include).Length;
            Assert.Greater(bending, 30, "tufos de grama que curvam quando o gato passa");
        }

        [UnityTest]
        public IEnumerator LivingWorld_CrystalLights_RatFlees_AndParticlesStayInBudget()
        {
            yield return Load();
            Assert.IsNotNull(GameObject.Find("Fortaleza/Mundo vivo"), "Mundo vivo (Etapa 7)");
            var player = PlayerHealth.Active.GetComponent<PlayerController>();

            // o cristal Primata da trilha acende com o gato perto e apaga quando ele se afasta
            ReactOnProximityProbe(out var crystal, "vestigio_primata: env_pri_obj_cristal_apagado");
            var rest = crystal.GetComponentInChildren<SpriteRenderer>().sprite;
            player.Teleport(new Vector2(31.6f, 0.05f));
            yield return new WaitForSeconds(0.3f);
            Assert.AreNotEqual(rest, crystal.GetComponentInChildren<SpriteRenderer>().sprite, "aceso perto do gato");
            player.Teleport(new Vector2(42f, 0.05f));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(rest, crystal.GetComponentInChildren<SpriteRenderer>().sprite, "apagado de novo");

            // o rato dos fundos foge para o buraco e some
            var rat = GameObject.Find("Fortaleza/Mundo vivo/Rato dos fundos").GetComponent<SeteVidas.World.Scenery.ReactOnProximity>();
            player.Teleport(new Vector2(211.5f, FortressLayoutFloor(101f)));
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(rat.IsAway, "fugiu");

            // as partículas nunca passam do orçamento (6.1)
            player.Teleport(new Vector2(160f, 101.05f));
            yield return new WaitForSeconds(3f);
            Assert.LessOrEqual(SeteVidas.World.Scenery.AmbientEmitter.AliveInScene, SeteVidas.World.Scenery.AmbientEmitter.GlobalCap);
            Assert.Greater(SeteVidas.World.Scenery.AmbientEmitter.AliveInScene, 0, "a câmara tem poeira roxa suspensa");
        }

        static float FortressLayoutFloor(float y) => y + 0.05f;

        static void ReactOnProximityProbe(out SeteVidas.World.Scenery.ReactOnProximity react, string name)
        {
            var go = GameObject.Find("Fortaleza/Enfeites/" + name);
            Assert.IsNotNull(go, name);
            react = go.GetComponent<SeteVidas.World.Scenery.ReactOnProximity>();
            Assert.IsNotNull(react, name + ": reage ao gato");
        }

        [UnityTest]
        public IEnumerator EverySaw_RunsOnAVisibleRail()
        {
            yield return Load();
            var saws = Object.FindObjectsByType<SeteVidas.World.Traps.SawBlade>(FindObjectsInactive.Include);
            Assert.Greater(saws.Length, 0);
            foreach (var saw in saws)
            {
                var rail = GameObject.Find("Fortaleza/" + saw.name + " (trilho)");
                Assert.IsNotNull(rail, saw.name + ": trilho atrás do disco, no sentido em que ele anda");
                var sr = rail.GetComponent<SpriteRenderer>();
                Assert.AreEqual(SpriteDrawMode.Tiled, sr.drawMode);
                Assert.Less(sr.sortingOrder, saw.GetComponentInChildren<SpriteRenderer>().sortingOrder, "o trilho fica atrás da lâmina");
                Assert.AreEqual(2, rail.transform.childCount, "um suporte em cada ponta");
            }
        }

        [UnityTest]
        public IEnumerator NoisyObjects_HaveArt_AndTheBucketStandsUpAgainAfterRest()
        {
            yield return Load();
            var noisy = Object.FindObjectsByType<NoisyObject>(FindObjectsInactive.Include);
            // Etapa 5A: folhas secas (trilha), palha (pátio), sininhos e balde (galeria), cacos (salão e armadilhas), balde (fundos);
            // e os sininhos de alarme da galeria (no lugar do fio de alarme)
            Assert.AreEqual(8, noisy.Length, "Objetos barulhentos da Fortaleza");
            foreach (var n in noisy)
            {
                var sr = n.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(sr != null ? sr.sprite : null, n.name + " sem desenho (rode Tools/EnvironmentSprites/gerar.ps1)");
                Assert.IsTrue(n.GetComponent<Collider2D>().isTrigger, n.name);
            }
            // o balde derrubado volta a ficar em pé quando o mundo reinicia (descanso no santuário)
            NoisyObject bucket = null;
            foreach (var n in noisy) if (n.Config.OneShot) { bucket = n; break; }
            Assert.IsNotNull(bucket, "Balde (cai uma vez)");
            var rest = bucket.GetComponentInChildren<SpriteRenderer>().sprite;
            bucket.ResetState();
            Assert.IsFalse(bucket.Spent);
            Assert.AreEqual(rest, bucket.GetComponentInChildren<SpriteRenderer>().sprite);
        }

        [UnityTest]
        public IEnumerator Dressing_ComesFromTheSceneryLibrary()
        {
            yield return Load();
            var dressing = GameObject.Find("Fortaleza/Enfeites");
            Assert.IsNotNull(dressing, "Enfeites (FortressDressing). Gere a cena: Tools > Sete Vidas > 13. Fase 6: Fortaleza");
            // 152 itens no FortressDressing (Etapas 1–8 e as melhorias), com 4 fileiras (4 escudos do salão, 5 ameias da torre,
            // 13 trechos do cano de óleo, 12 trechos do tapete do salão): 179 objetos (com as placas de seta da melhoria 9 e o arranjo do usuário: sem vitrais, escudos com nome, runas, marca da Guilda, placa e emblema do altar)
            Assert.AreEqual(179, dressing.transform.childCount, "Todos os enfeites do FortressDressing");
            foreach (var sr in dressing.GetComponentsInChildren<SpriteRenderer>())
                Assert.IsNotNull(sr.sprite, $"{sr.name} sem sprite: id fora da biblioteca?");
            Assert.AreEqual(-5, GameObject.Find("Fortaleza/Enfeites/Estátua do guardião").GetComponent<SpriteRenderer>().sortingOrder,
                "Camada decor_atras");
            Assert.IsNotNull(Map("Ground").GetTile(new Vector3Int(12, 59, 0)), "Borda do penhasco (topo em 60)");
            Assert.IsNull(Map("Ground").GetTile(new Vector3Int(15, 30, 0)), "Vão livre para o salto de fé");
        }

        [UnityTest]
        public IEnumerator CastleWall_IsClimbedByVines_InThreeSteps()
        {
            yield return Load();
            var climbable = Map("Climbable");
            var ground = Map("Ground");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(112, 4, 0)), "Vinha da muralha 1");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(120, 16, 0)), "Vinha da muralha 2");
            Assert.IsNotNull(climbable.GetTile(new Vector3Int(128, 30, 0)), "Vinha da muralha 3");
            Assert.IsNotNull(ground.GetTile(new Vector3Int(135, 42, 0)), "Topo da muralha 3 em 43");
            Assert.IsNull(ground.GetTile(new Vector3Int(135, 43, 0)));
        }

        [UnityTest]
        public IEnumerator Exit_IsBehindTheBackGate()
        {
            yield return Load();
            BreakableWall gate = null;
            foreach (var w in Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None))
                if (w.name == "Portão dos fundos") gate = w;
            Assert.IsNotNull(gate, "Portão dos fundos");
            var ground = Map("Ground");
            int gx = Mathf.FloorToInt(gate.transform.position.x);
            for (int y = 4; y <= 12; y++)
                Assert.IsNotNull(ground.GetTile(new Vector3Int(gx, y, 0)), $"Vão acima do portão em y {y}: dá para passar sem a marreta");
            var exit = GameObject.Find("Conversa: Saída da fortaleza");
            Assert.IsNotNull(exit);
            Assert.Greater(exit.transform.position.x, gate.transform.position.x, "A saída fica atrás do portão");
        }

        [UnityTest]
        public IEnumerator IntroNarration_PlaysOnlyOnce()
        {
            yield return Load();
            var trigger = GameObject.Find("Conversa: Chegada").GetComponent<DialogueTrigger>();
            Assert.IsTrue(trigger.HasDialogue, "Gatilho sem conversa ligada. Rode Tools > Sete Vidas > 13 de novo");
            var health = PlayerHealth.Active;
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            // o gato nasce dentro da área de chegada: a narração pode já ter começado sozinha (é o comportamento certo)
            if (!runner.IsOpen) Assert.IsTrue(trigger.Play(health), "Primeira vez: narração de chegada");
            Assert.IsTrue(runner.IsOpen, "A narração de chegada está tocando");
            for (int i = 0; i < 20 && runner.IsOpen; i++) runner.Advance();
            Assert.IsFalse(trigger.Play(health), "Depois: não repete");
        }
    }
}
