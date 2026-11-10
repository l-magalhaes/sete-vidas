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
            Assert.IsNotNull(Sanctuary.Find("fortaleza_patio"), "Santuário do pátio de treino");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_muralha"), "Santuário do portão da muralha");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_sino"), "Santuário da torre do sino");
            Assert.IsNotNull(Sanctuary.Find("fortaleza_camara"), "Santuário antes da câmara");
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
            Assert.IsNotNull(GameObject.Find("Céu e montanhas/Morros com pinheiros").GetComponent<SeteVidas.World.ParallaxLayer>(),
                "Fundo montado pela biblioteca de cenário, com paralaxe");
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
        public IEnumerator Dressing_ComesFromTheSceneryLibrary()
        {
            yield return Load();
            var dressing = GameObject.Find("Fortaleza/Enfeites");
            Assert.IsNotNull(dressing, "Enfeites (FortressDressing). Gere a cena: Tools > Sete Vidas > 13. Fase 6: Fortaleza");
            // 67 itens no FortressDressing (Etapas 1–4: enfeites, rochas, fósseis, estruturas, alcovas, vegetação), com 2 fileiras
            // (4 escudos do salão, 5 ameias da torre): 74 objetos
            Assert.AreEqual(74, dressing.transform.childCount, "Todos os enfeites do FortressDressing");
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
            Assert.IsTrue(trigger.Play(health), "Primeira vez: narração de chegada");
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            for (int i = 0; i < 20 && runner.IsOpen; i++) runner.Advance();
            Assert.IsFalse(trigger.Play(health), "Depois: não repete");
        }
    }
}
