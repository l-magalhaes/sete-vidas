using System.IO;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.World;
using SeteVidas.World.Traps;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F1 da Floresta Silenciosa (Docs/Floresta/plano-implementacao.md): duas cenas de teste em blocos para provar a base
    /// técnica antes do graybox. Critério de pronto da F1: andar entre as duas cenas, quebrar um tronco, abrir um baú,
    /// sair e voltar com tudo lembrado, e viajar entre os dois santuários.
    /// <list type="bullet">
    /// <item>Forest_Test_A: santuário, tronco rachado com baú atrás, poço vertical com galhos de mão única, galho que
    /// balança, folha que afunda, cogumelo saltador, galho podre e um baú no topo; porta para a B.</item>
    /// <item>Forest_Test_B: santuário, outro tronco + baú, porta de volta para a A e uma porta interna (mesma cena).</item>
    /// </list>
    /// Arte provisória desenhada aqui (Art/Environments/Forest/Placeholder), na paleta da Floresta.
    /// </summary>
    public static class ForestTestSetup
    {
        public const string Folder = "Assets/_Game/Scenes/World/Forest";
        public const string SceneA = "Forest_Test_A";
        public const string SceneB = "Forest_Test_B";
        const string ArtFolder = "Assets/_Game/Art/Environments/Forest/Placeholder";

        [MenuItem("Tools/Sete Vidas/20. Floresta F1: cenas de teste (A e B)", priority = 70)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SeteVidasSetup.ConfigureLayers(); // garante a layer Platform (galhos de mão única)
            PlayerSetup.CreatePlayerAssets();
            ProgressionSetup.CreateHudPrefab();
            ProgressionSetup.CreateSanctuaryPrefab();
            ClassSetup.CreateWallPrefab();
            Directory.CreateDirectory(Folder);
            EnsureSprites();

            BuildSceneA();
            BuildSceneB();
            SeteVidasSetup.RegisterScenesInBuild();
            EditorSceneManager.OpenScene($"{Folder}/{SceneA}.unity");
            Debug.Log("[Sete Vidas] Floresta F1: cenas Forest_Test_A e Forest_Test_B geradas. Aperte Play na A. " +
                      "Roteiro de teste: Docs/QA/floresta-f1-plano-de-teste.md");
        }

        // ------------------------------------------------------------ cena A

        static void BuildSceneA()
        {
            var ctx = Begin(SceneA, new Vector2(3f, 0.1f), new Color32(0x16, 0x1a, 0x38, 0xff));
            var g = ctx.Ground;

            // chão e paredes (o chão fica em y 0: tiles de y -3 a -1)
            Fill(g, -8, -3, 64, -1);
            Fill(g, -8, 0, -7, 42);            // parede da esquerda
            // parede da direita: a porta para a B saiu do chão e foi para o alto (corredor em y 18..21, rev. do usuário)
            Fill(g, 63, 0, 64, 17);
            Fill(g, 63, 22, 64, 42);
            Fill(g, -8, 40, 64, 42);           // teto

            // santuário
            Sanctuary(ctx.Root, "S-T1", "Clareira de teste", 1f);

            // tronco rachado (x 10) fechando uma alcova com o baú W01
            Fill(g, 9, 4, 17, 4);              // teto da alcova (topo em 5: dá para agarrar a borda e seguir por cima)
            Fill(g, 5, 0, 6, 1);               // degrau antes do tronco (topo em 2)
            Fill(g, 17, 0, 17, 3);             // fundo da alcova
            Trunk(ctx.Root, "Tronco rachado (A1)", 10.5f, 0f, "teste_A1");
            Chest(ctx.Root, "Baú W01", 14f, 0f, "TESTE_W01", wood: true, coins: 0);

            // poço vertical (x 22..36): galhos de mão única a cada 3 tiles, alternando os lados
            Fill(g, 21, 4, 21, 38);            // parede esquerda do poço (passagem pelo chão, y 0..3)
            // parede direita fechada embaixo; só se passa para o lado dos cactos pelo alto (aberta em y 35..38)
            Fill(g, 37, 0, 37, 34);
            Mushroom(ctx.Root, "Cogumelo saltador", 24f, 0f);
            Cactus(ctx.Root, "Cacto (poço, meio)", 23.5f, 14f, new Vector2(0f, 1f), 3f);
            for (int i = 0; i < 6; i++)
            {
                float y = 3f + i * 3f;
                float x = i % 2 == 0 ? 25f : 30f;
                Branch(ctx.Root, $"Galho de mão única {i + 1}", x, y, 5f);
            }
            Leaf(ctx.Root, "Folha larga", 28.5f, 21f);
            Swaying(ctx.Root, "Galho que balança", 32f, 24f, 4f);
            RottenBranch(ctx.Root, "Galho podre", 26f, 27f);
            // no lugar do galho 7: sobe e desce (27..29), baixo o bastante para pular nele do galho podre (y 27)
            Cactus(ctx.Root, "Cacto (poço, topo)", 31.5f, 29.3f, new Vector2(0f, 1f), 3f);
            Fill(g, 22, 32, 28, 32);           // patamar do topo (em pé em y 33), com o baú W02
            Chest(ctx.Root, "Baú W02 (topo do poço)", 24f, 33f, "TESTE_W02", wood: true, coins: 5);
            Fill(g, 34, 34, 42, 34);           // borda que atravessa a parede para o lado dos cactos (em pé em y 35)

            // lado dos cactos (x 38..62): chão de espinhos, cactos para quicar e plataformas até a porta no alto
            TrapsSetup.Spikes(ctx.Root, 50f, 24);
            // Coluna em zigue-zague, ~6 tiles entre um e outro (o quique sobe 7,2): dos espinhos do chão dá para subir
            // tudo de volta até a borda (y 35). Descendo da borda, o de cima leva à plataforma alta; os de 12 e 18, ao corredor.
            Cactus(ctx.Root, "Cacto 1 (y 30, leva à plataforma)", 45.5f, 30f, new Vector2(1f, 0f), 3f);
            Cactus(ctx.Root, "Cacto 2 (y 24)", 40.2f, 24f, new Vector2(0f, 0.5f), 2.5f);
            Cactus(ctx.Root, "Cacto 3 (y 15)", 51.1f, 15f, Vector2.zero, 3f);
            Cactus(ctx.Root, "Cacto 4 (y 11)", 45.5f, 10.8f, new Vector2(0.75f, 0f), 3f);
            Cactus(ctx.Root, "Cacto 5 (y 5,5, alcança dos espinhos)", 49.3f, 5.5f, new Vector2(0f, 0.3f), 2.5f); // quique nos espinhos: até ~7,7
            // plataforma alta (em pé em y 26) e um galho até o corredor: o caminho para baixo sempre aparece na tela
            Fill(g, 52, 25, 58, 25);
            Branch(ctx.Root, "Galho entre os cactos", 44.3f, 22f, 4f);
            // cogumelos (posições do usuário, 13/11/2026): o caminho de volta para cima
            Mushroom(ctx.Root, "Cogumelo da plataforma alta", 52.7f, 26f);
            Mushroom(ctx.Root, "Cogumelo do corredor", 60f, 18f);
            // e no chão de espinhos: espinhos e cogumelos juntos (cair no cogumelo salva)
            Mushroom(ctx.Root, "Cogumelo nos espinhos 1", 41f, 0f);
            Mushroom(ctx.Root, "Cogumelo nos espinhos 2", 55.5f, 0f);
            Mushroom(ctx.Root, "Cogumelo nos espinhos 3", 61.7f, 0f);
            Fill(g, 54, 17, 62, 17);           // chão do corredor da porta (em pé em y 18)
            TrapsSetup.Sign(ctx.Root, 62.1f, 18.2f, "Saída →");

            // porta para a B (no alto, à direita) e a chegada de volta
            Exit(ctx.Root, "Porta para a B", new Vector2(63.5f, 20f), new Vector2(1f, 4f), SceneB, "B_da_A");
            Entrance(ctx.Root, "Chegada (da B)", new Vector2(56.5f, 18.05f), "A_da_B", -1); // antes do cogumelo do corredor

            End(ctx);
        }

        // ------------------------------------------------------------ cena B

        static void BuildSceneB()
        {
            var ctx = Begin(SceneB, new Vector2(3f, 0.1f), new Color32(0x10, 0x14, 0x2c, 0xff));
            var g = ctx.Ground;

            Fill(g, -8, -3, 70, -1);
            Fill(g, -8, 4, -7, 30);            // parede da esquerda (porta para a A embaixo, y 0..3)
            Fill(g, 69, 0, 70, 30);
            Fill(g, -8, 28, 70, 30);

            Entrance(ctx.Root, "Chegada (da A)", new Vector2(2f, 0.05f), "B_da_A", 1);
            Exit(ctx.Root, "Porta para a A", new Vector2(-7.5f, 2f), new Vector2(1f, 4f), SceneA, "A_da_B");
            Sanctuary(ctx.Root, "S-T2", "Vale de teste", 8f);

            // tronco + baú (madeira)
            Fill(g, 19, 4, 26, 4);             // teto da alcova (topo em 5)
            Fill(g, 15, 0, 16, 1);             // degrau (topo em 2)
            Fill(g, 26, 0, 26, 3);
            Trunk(ctx.Root, "Tronco rachado (B1)", 19.5f, 0f, "teste_B1");
            Chest(ctx.Root, "Baú W03", 23f, 0f, "TESTE_W03", wood: true, coins: 0);

            // porta interna (mesma cena): do fundo do corredor para o patamar alto
            Fill(g, 44, 14, 66, 14);           // patamar alto
            Exit(ctx.Root, "Porta interna (sobe)", new Vector2(66f, 2f), new Vector2(1f, 4f), "", "B_patamar");
            Entrance(ctx.Root, "Chegada (patamar)", new Vector2(64f, 15.05f), "B_patamar", -1);
            Branch(ctx.Root, "Galho de mão única (descer)", 38f, 10f, 4f);
            Branch(ctx.Root, "Galho de mão única (descer 2)", 34f, 6f, 4f);

            End(ctx);
        }

        // ------------------------------------------------------------ montagem da cena

        struct Ctx { public Scene Scene; public Transform Root; public Tilemap Ground; public Tile Tile; }

        static Ctx Begin(string name, Vector2 spawn, Color32 sky)
        {
            string path = $"{Folder}/{name}.unity";
            var scene = SeteVidasSetup.NewSceneFromTemplate(path);
            var cam = SeteVidasSetup.FindCamera(scene);
            SeteVidasSetup.SetupPixelPerfectCamera(cam, spawn + Vector2.up * 4f);
            cam.backgroundColor = sky;
            var tiles = SeteVidasSetup.CreatePlaceholderTiles();

            var grid = SeteVidasSetup.CreateGrid();
            SceneManager.MoveGameObjectToScene(grid.gameObject, scene);
            var ground = SeteVidasSetup.CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true);

            var root = new GameObject("Floresta (teste)");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ForestTestBootstrap>(); // save de quem saiu da Fortaleza (Paladino, 2 corações)
            var spawnGo = new GameObject("PlayerSpawn");
            SceneManager.MoveGameObjectToScene(spawnGo, scene);
            spawnGo.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.HudPrefabPath), scene);
            hud.name = "HUD";
            return new Ctx { Scene = scene, Root = root.transform, Ground = ground, Tile = tiles.Ground };
        }

        static void End(Ctx ctx)
        {
            SeteVidasSetup.BuildTileColliders(ctx.Scene);
            PlayerSetup.PlaceInScene(ctx.Scene); // gato + câmera no PlayerSpawn; salva a cena
            AddForestPlayerParts(ctx.Scene);
            EditorSceneManager.MarkSceneDirty(ctx.Scene);
            EditorSceneManager.SaveScene(ctx.Scene);
        }

        /// <summary>Peças do gato que só a Floresta usa: alvo da câmera que olha para baixo e as falas curtas.</summary>
        internal static void AddForestPlayerParts(Scene scene)
        {
            GameObject player = null;
            CinemachineCamera cm = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null && root.GetComponent<PlayerController>() != null) player = root;
                if (cm == null) cm = root.GetComponentInChildren<CinemachineCamera>();
            }
            if (player == null) { Debug.LogWarning("[Sete Vidas] Gato não encontrado na cena da Floresta"); return; }
            var look = player.GetComponent<CameraLook>() ?? player.AddComponent<CameraLook>();
            if (player.GetComponent<LionBarks>() == null) player.AddComponent<LionBarks>();
            if (cm != null)
            {
                cm.Target.TrackingTarget = look.Target;
                // salas verticais: a câmera acompanha a altura mais de perto
                var composer = cm.GetComponent<CinemachinePositionComposer>();
                if (composer != null)
                {
                    composer.Composition.DeadZone.Size = new Vector2(0.1f, 0.12f);
                    composer.Damping = new Vector3(0.3f, 0.35f, 0f);
                }
            }
        }

        static Tile s_tile;

        static void Fill(Tilemap map, int x0, int y0, int x1, int y1)
        {
            if (s_tile == null) s_tile = SeteVidasSetup.CreatePlaceholderTiles().Ground;
            SeteVidasSetup.Fill(map, s_tile, x0, y0, x1, y1);
        }

        // ------------------------------------------------------------ objetos

        static void Sanctuary(Transform parent, string id, string displayName, float x)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.SanctuaryPrefabPath);
            ProgressionSetup.PlaceSanctuary(prefab, parent, id, displayName, x);
        }

        static void Trunk(Transform parent, string name, float x, float y, string id)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClassSetup.WallPrefabPath), parent);
            go.name = name;
            go.transform.position = new Vector3(x, y, 0f);
            var wall = go.GetComponent<BreakableWall>();
            var so = new SerializedObject(wall);
            so.FindProperty("wallId").stringValue = id;
            so.FindProperty("material").enumValueIndex = (int)BreakableMaterial.Wood;
            so.FindProperty("maxHealth").intValue = 12;   // 3 marretadas (dano 4)
            BreakableRules.BreakNoise(BreakableMaterial.Wood, out float r, out float l);
            so.FindProperty("breakNoiseRadius").floatValue = r;
            so.FindProperty("breakNoiseLoudness").floatValue = l;
            var intact = LoadSprite("tronco_rachado_intacto");
            so.FindProperty("intactSprite").objectReferenceValue = intact;
            so.FindProperty("crackedSprite").objectReferenceValue = LoadSprite("tronco_rachado_lascado");
            var sr = (SpriteRenderer)so.FindProperty("spriteRenderer").objectReferenceValue;
            if (sr != null) { sr.sprite = intact; PrefabUtility.RecordPrefabInstancePropertyModifications(sr); }
            var rubble = (GameObject)so.FindProperty("rubble").objectReferenceValue;
            var rsr = rubble != null ? rubble.GetComponent<SpriteRenderer>() : null;
            if (rsr != null) { rsr.sprite = LoadSprite("tronco_lascas"); PrefabUtility.RecordPrefabInstancePropertyModifications(rsr); }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Chest(Transform parent, string name, float x, float y, string id, bool wood, int coins)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.5f, 2f);
            box.offset = new Vector2(0f, 1f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("bau_fechado");
            sr.sortingOrder = 2;
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: abrir", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);
            var chest = go.AddComponent<WoodChest>();
            var so = new SerializedObject(chest);
            so.FindProperty("chestId").stringValue = id;
            so.FindProperty("containsWood").boolValue = wood;
            so.FindProperty("coins").intValue = coins;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("closedSprite").objectReferenceValue = LoadSprite("bau_fechado");
            so.FindProperty("openSprite").objectReferenceValue = LoadSprite("bau_aberto");
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Galho de mão única: <paramref name="x"/> é a ponta esquerda; o topo fica em <paramref name="y"/>.</summary>
        static void Branch(Transform parent, string name, float x, float y, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + width * 0.5f, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "galho", new Vector2(width, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
        }

        static void Swaying(Transform parent, string name, float x, float y, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + width * 0.5f, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "galho", new Vector2(width, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
            go.AddComponent<SwayingBranch>();
        }

        static void Leaf(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "folha_larga", new Vector2(3f, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
            go.AddComponent<SinkingLeaf>();
        }

        static void Mushroom(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.75f, 0.95f);   // topo do chapéu desenhado (o gato não afunda nele)
            box.offset = new Vector2(0f, 0.475f);
            var cap = Visual(go.transform, "cogumelo", new Vector2(2f, 1f), new Vector3(0f, 0.5f, 0f), 1);
            var mush = go.AddComponent<BounceMushroom>();
            var so = new SerializedObject(mush);
            so.FindProperty("cap").objectReferenceValue = cap.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RottenBranch(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + 1f, y - 0.25f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 0.5f);
            var sr = Visual(go.transform, "galho_podre", new Vector2(2f, 0.5f), Vector3.zero, 1);
            var cp = go.AddComponent<CrumblingPlatform>();
            var so = new SerializedObject(cp);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("intact").objectReferenceValue = LoadSprite("galho_podre");
            so.FindProperty("cracked").objectReferenceValue = LoadSprite("galho_podre_rachando");
            so.FindProperty("isBranch").boolValue = true;
            so.FindProperty("respawnTime").floatValue = 5f;
            so.FindProperty("shakeTime").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Cacto Flutuante (bola de espinhos, ponto de pogo). <paramref name="travel"/> = vaivém a partir dali.</summary>
        static void Cactus(Transform parent, string name, float x, float y, Vector2 travel, float period)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("cacto_flutuante");
            sr.sortingOrder = 3;
            var cactus = go.AddComponent<FloatingCactus>();
            cactus.Configure(0.6f, travel, period);
            var so = new SerializedObject(cactus);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.FindProperty("returnToSafeGround").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SpriteRenderer Visual(Transform parent, string sprite, Vector2 size, Vector3 local, int order)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = order;
            return sr;
        }

        static void Exit(Transform parent, string name, Vector2 center, Vector2 size, string scene, string entrance)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<RoomExit>().Configure(scene, entrance);
        }

        static void Entrance(Transform parent, string name, Vector2 feet, string id, int facing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            go.AddComponent<RoomEntrance>().Configure(id, facing);
        }

        // ------------------------------------------------------------ arte provisória

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 BarkDark = new Color32(0x34, 0x26, 0x1e, 0xff);
        static readonly Color32 Bark = new Color32(0x52, 0x26, 0x1e, 0xff);
        static readonly Color32 BarkMid = new Color32(0x82, 0x3e, 0x2e, 0xff);
        static readonly Color32 Wood = new Color32(0x96, 0x6e, 0x40, 0xff);
        static readonly Color32 WoodLight = new Color32(0xc4, 0x98, 0x60, 0xff);
        static readonly Color32 Leaf1 = new Color32(0x2e, 0x48, 0x34, 0xff);
        static readonly Color32 Leaf2 = new Color32(0x4a, 0x68, 0x46, 0xff);
        static readonly Color32 Leaf3 = new Color32(0x6c, 0x86, 0x60, 0xff);
        static readonly Color32 Rotten = new Color32(0x6e, 0x6a, 0x64, 0xff);
        static readonly Color32 RottenDark = new Color32(0x46, 0x44, 0x42, 0xff);
        static readonly Color32 Pogo = new Color32(0x5f, 0xd3, 0xc7, 0xff);
        static readonly Color32 PogoLight = new Color32(0xb9, 0xf5, 0xec, 0xff);
        static readonly Color32 Stem = new Color32(0xe4, 0xe2, 0xdc, 0xff);
        static readonly Color32 Crack = new Color32(0xff, 0xd3, 0x4d, 0xff);
        static readonly Color32 CrackDark = new Color32(0x8a, 0x62, 0x1e, 0xff);
        static readonly Color32 Iron = new Color32(0xc0, 0xc6, 0xd2, 0xff);
        static readonly Color32 Outline = new Color32(0x1a, 0x14, 0x12, 0xff);

        /// <summary>Desenha os sprites provisórios (só se ainda não existem: dá para trocar pela arte de verdade no mesmo nome).</summary>
        static void EnsureSprites()
        {
            Directory.CreateDirectory(ArtFolder);
            Make("galho", 16, 6, (x, y) => y <= 2 ? (y == 0 ? BarkDark : (x % 5 == 0 ? BarkDark : Bark)) :
                                           y <= 3 ? Leaf1 : (x % 4 == 1 || x % 4 == 2) ? (y == 4 ? Leaf2 : Leaf3) : Clear);
            Make("galho_podre", 32, 8, (x, y) => y <= 5 ? (y == 0 || y == 5 ? RottenDark : (x % 7 == 3 ? RottenDark : Rotten)) : (x % 9 == 2 ? Rotten : Clear));
            Make("galho_podre_rachando", 32, 8, (x, y) => y <= 5 ? ((x == 10 || x == 11 || x == 21) && y > 1 ? Clear : y == 0 || y == 5 ? RottenDark : Rotten) : Clear);
            Make("folha_larga", 48, 6, (x, y) => y == 0 ? Leaf1 : y <= 3 ? (y == 2 && x > 3 && x < 44 ? Leaf2 : Leaf3) :
                                                 (x > 6 && x < 41 && y == 4 ? Leaf3 : Clear));
            Make("cogumelo", 32, 16, (x, y) =>
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 7f) / 8f;
                if (y >= 7 && dx * dx + dy * dy <= 1f) return (y >= 12 && x > 8 && x < 16) ? PogoLight : (y == 7 ? new Color32(0x34, 0x8c, 0xa0, 0xff) : Pogo);
                if (y < 7 && x >= 12 && x <= 19) return x == 12 || x == 19 ? new Color32(0xa8, 0xa4, 0x98, 0xff) : Stem;
                return Clear;
            });
            Make("cacto_flutuante", 24, 24, PaintCactus);
            Make("bau_fechado", 16, 16, (x, y) => PaintChest(x, y, open: false));
            Make("bau_aberto", 16, 16, (x, y) => PaintChest(x, y, open: true));
            Make("tronco_rachado_intacto", 16, 64, (x, y) => PaintTrunk(x, y, damaged: false));
            Make("tronco_rachado_lascado", 16, 64, (x, y) => PaintTrunk(x, y, damaged: true));
            Make("tronco_lascas", 16, 8, (x, y) => y <= 2 && (x + y * 3) % 4 != 0 ? (x % 3 == 0 ? WoodLight : Wood) : (y == 3 && x % 5 == 1 ? Bark : Clear));
        }

        /// <summary>Bola verde com 8 espinhos de ponta vermelha e a flor turquesa do pogo no topo (24×24).</summary>
        static Color32 PaintCactus(int x, int y)
        {
            var green = new Color32(0x2e, 0x6a, 0x4a, 0xff);
            var greenLight = new Color32(0x4a, 0x96, 0x5e, 0xff);
            var red = new Color32(0xc8, 0x3a, 0x3a, 0xff);
            float cx = 11.5f, cy = 11.5f, dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            // flor do pogo no topo
            if (y >= 18 && y <= 20 && x >= 10 && x <= 13) return (y == 20 || x == 10 || x == 13) ? Pogo : PogoLight;
            if (d <= 6.5f) return d > 5.5f ? Outline : (dx < -1f && dy > 1f ? greenLight : green);
            // espinhos: 8 direções
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + Mathf.PI / 8f;
                float ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                float along = dx * ux + dy * uy, across = Mathf.Abs(-dx * uy + dy * ux);
                if (along > 6f && along < 11.5f && across < 1.3f - (along - 6f) * 0.2f) return along > 9.5f ? red : Outline;
            }
            return Clear;
        }

        static Color32 PaintChest(int x, int y, bool open)
        {
            if (x < 1 || x > 14) return Clear;
            if (!open)
            {
                if (y > 12) return Clear;
                if (x == 1 || x == 14 || y == 0 || y == 12) return Outline;
                if (y == 7) return Iron;
                if ((x == 7 || x == 8) && (y == 5 || y == 6)) return Crack;   // fechadura dourada
                return y > 7 ? WoodLight : Wood;
            }
            if (y > 14) return Clear;
            if (y >= 8) return (x == 1 || x == 14 || y == 14) ? Outline : (y == 8 ? Iron : BarkMid); // tampa aberta, de pé
            if (x == 1 || x == 14 || y == 0) return Outline;
            if (y == 7) return Outline;
            return y >= 5 ? BarkDark : Wood;
        }

        static Color32 PaintTrunk(int x, int y, bool damaged)
        {
            if (x < 1 || x > 14) return Clear;
            if (x == 1 || x == 14) return Outline;
            Color32 c = (x + y / 3) % 5 == 0 ? BarkDark : (x % 4 == 2 ? BarkMid : Bark);
            // rachadura em zigue-zague pelo meio (amarela: "a marreta quebra isto")
            int zig = 7 + ((y / 4) % 2 == 0 ? (y % 4) - 1 : 2 - (y % 4));
            if (y > 4 && y < 60 && (x == zig || (damaged && (x == zig + 1 || x == zig - 2)))) return y % 8 < 6 ? Crack : CrackDark;
            if (damaged && (y == 20 || y == 41) && x > 3 && x < 12) return CrackDark;
            return c;
        }

        static void Make(string name, int w, int h, System.Func<int, int, Color32> paint)
        {
            string path = $"{ArtFolder}/{name}.png";
            if (File.Exists(path)) return;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = paint(x, y);
            tex.SetPixels32(px);
            tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
