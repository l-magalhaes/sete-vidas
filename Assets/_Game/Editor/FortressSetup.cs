using System.IO;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Enemies;
using SeteVidas.World;
using SeteVidas.World.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Fase 6: gera a cena Fortress_MVP, o jogo de verdade (Novo jogo começa aqui). Separada da Playground.
    /// Salas, medidas e checagem anti-softlock: Docs/Level/fortaleza-mvp.md.
    /// ATENÇÃO: recria a cena do zero. Depois de começar a editar a Fortaleza à mão, não rode de novo.
    /// </summary>
    public static class FortressSetup
    {
        const float Upper = 14f; // chão do andar de cima (topo dos tiles em y 12..13)

        [MenuItem("Tools/Sete Vidas/13. Fase 6: Fortaleza (gera a cena do jogo)", priority = 49)]
        public static void Build()
        {
            if (File.Exists(Path.GetFullPath(GameConstants.FortressScenePath)) &&
                !EditorUtility.DisplayDialog("Gerar a Fortaleza",
                    "Isto recria a cena Fortress_MVP do zero (edições feitas à mão nela se perdem). Continuar?",
                    "Gerar", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Prefabs que a Fortaleza usa (todos idempotentes)
            PlayerSetup.CreatePlayerAssets();
            EnemySetup.CreateSentinelPrefab();
            EnemySetup.CreateBushPrefab();
            ProgressionSetup.CreateHudPrefab();
            ProgressionSetup.CreateSanctuaryPrefab();
            ClassSetup.CreateWallPrefab();
            BossSetup.CreatePrefabs();
            TrapsSetup.CreatePrefabs();
            CreateDialogues(); // garante os assets; são carregados de novo depois de abrir a cena

            var scene = SeteVidasSetup.NewSceneFromTemplate(GameConstants.FortressScenePath);
            SeteVidasSetup.SetupPixelPerfectCamera(SeteVidasSetup.FindCamera(scene), new Vector2(8f, 4f));
            var tiles = SeteVidasSetup.CreatePlaceholderTiles(); // depois de abrir a cena (ver CreatePlayground)
            // Abrir a cena descarrega da memória os assets criados antes: as conversas precisam ser carregadas
            // agora, senão as referências salvas ficam vazias (a narração não tocava)
            var dialogues = CreateDialogues();

            var grid = SeteVidasSetup.CreateGrid();
            var background = SeteVidasSetup.CreateTilemap(grid, "Background", null, -10, false);
            var ground = SeteVidasSetup.CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true);
            var climbable = SeteVidasSetup.CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 0, true);
            SeteVidasSetup.CreateTilemap(grid, "Foreground", null, 10, false);

            BuildTiles(tiles, background, ground, climbable);
            PlaceObjects(scene, dialogues);

            SeteVidasSetup.BuildTileColliders(scene);
            PlayerSetup.PlaceInScene(scene); // gato + câmera no PlayerSpawn; salva a cena
            SeteVidasSetup.RegisterScenesInBuild();
            Debug.Log("[Sete Vidas] Fortaleza gerada (Scenes/World/Fortress_MVP). Novo jogo começa nela. Mapa: Docs/Level/fortaleza-mvp.md");
        }

        // ------------------------------------------------------------- tiles

        static void Fill(Tilemap map, TileBase tile, int x0, int y0, int x1, int y1) => SeteVidasSetup.Fill(map, tile, x0, y0, x1, y1);

        static void BuildTiles(SeteVidasSetup.PlaceholderTiles t, Tilemap bg, Tilemap g, Tilemap c)
        {
            Fill(bg, t.Background, -4, -6, 225, 24);

            // Chão de baixo (salas 1 a 3 e a saída), 2 tiles de espessura; limite esquerdo
            Fill(g, t.Ground, -6, -2, 94, -1);
            Fill(g, t.Ground, -22, -10, -5, 25);         // rocha à esquerda: a câmera nunca mostra o vazio
            Fill(g, t.Ground, -22, -10, 94, -3);         // rocha sob o chão das salas de baixo

            // 1) Entrada externa: pedras de 2 e 3 tiles e um fosso de 4 com espinhos no fundo
            Fill(g, t.Ground, 9, 0, 11, 1);
            Fill(g, t.Ground, 14, 0, 16, 2);
            SeteVidasSetup.Clear(g, 20, -2, 23, -1);
            Fill(g, t.Ground, 20, -4, 23, -3);           // fundo do fosso (2 tiles abaixo)

            // Muralha externa com o arco de entrada (passagem de 4 tiles)
            Fill(g, t.Ground, 32, 4, 33, 25);

            // 2) Pátio: teto em 8 e a passagem baixa (1 tile) de x 42 a 47
            Fill(g, t.Ground, 34, 8, 59, 25);
            Fill(g, t.Ground, 42, 1, 47, 7);

            // 3) Muralha: teto em 24, hera escalável de y 4 a 13 (topo em 14) levando ao andar de cima
            Fill(g, t.Ground, 60, 24, 73, 25);
            Fill(c, t.Climbable, 72, 4, 73, 13);
            Fill(g, t.Ground, 76, 4, 76, 11);            // acima da parede rachada (x 76, y 0..3) até o andar de cima: só quebrando se passa

            // Andar de cima (chão com topo em 14) e teto em 24; maciço embaixo dele depois da saída
            Fill(g, t.Ground, 74, 12, 227, 13);
            Fill(g, t.Ground, 74, 24, 227, 25);
            Fill(g, t.Ground, 93, -2, 227, 11);          // parede da saída (x 93) e maciço sob o andar de cima

            // 4) Corredor patrulhado: passarela (topo em 19) e hera de 1 tile para subir nela
            Fill(g, t.Ground, 86, 18, 98, 18);
            Fill(c, t.Climbable, 85, 16, 85, 18);        // encostada na passarela; começa em 16: por baixo dela se anda (2 tiles livres)

            // 5) Salão de combate: duas plataformas (topo em 18)
            Fill(g, t.Ground, 122, 17, 125, 17);
            Fill(g, t.Ground, 130, 17, 133, 17);

            // 6b) Corredor das armadilhas: plataformas sobre o fogo e sobre a serra (topo em 17); fosso de espinhos
            Fill(g, t.Ground, 149, 16, 150, 16);         // plataformas a cada 2 tiles (saltos curtos)
            Fill(g, t.Ground, 153, 16, 154, 16);
            Fill(g, t.Ground, 157, 16, 158, 16);
            Fill(g, t.Ground, 161, 16, 162, 16);
            Fill(g, t.Ground, 168, 16, 171, 16);         // sobre a serra
            SeteVidasSetup.Clear(g, 176, 12, 181, 13);   // fosso de 2 tiles (6 de largura), espinhos no fundo

            // 7) Câmara do Golden: paredes acima das grades (passagem de 4 tiles), teto em 23 (9 de altura)
            Fill(g, t.Ground, 185, 18, 186, 23);
            Fill(g, t.Ground, 187, 23, 217, 23);
            Fill(g, t.Ground, 218, 18, 219, 23);
            Fill(g, t.Ground, 220, 20, 225, 23);         // teto da sala do tomo
            Fill(g, t.Ground, 226, -2, 227, 25);         // limite direito
        }

        // ------------------------------------------------------------ objetos

        struct FortressDialogues { public DialogueDefinition Intro, End; }

        static FortressDialogues CreateDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var result = new FortressDialogues
            {
                Intro = DialogueSetup.Conversation("D-FORT-01", true, 4,
                    (null, "", "A Cidadela do Sol Eterno, onde os cães viram templários e os templários, paladinos."),
                    (cat, "ironico", "Muralhas, sentinelas, armadilhas. O contratante não exagerou."),
                    (cat, "", "Bom. Seria chato se fosse fácil.")),
                End = DialogueSetup.Conversation("D-FIM", true, 2,
                    (null, "", "Você deixa a Cidadela sem o tomo. Com uma moeda de ouro e um juramento."),
                    (cat, "", "Uma moeda. Seis vidas. Vou cobrar o troco com juros, capivara."),
                    (null, "", "Fim da demonstração. Obrigado por jogar!")),
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        static void PlaceObjects(Scene scene, FortressDialogues dialogues)
        {
            var spawn = new GameObject("PlayerSpawn");
            SceneManager.MoveGameObjectToScene(spawn, scene);
            spawn.transform.position = new Vector3(3f, 0.1f, 0f);

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.HudPrefabPath), scene);
            hud.name = "HUD";

            var root = new GameObject("Fortaleza");
            SceneManager.MoveGameObjectToScene(root, scene);
            var t = root.transform;
            var sanctuary = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.SanctuaryPrefabPath);

            // 1) Entrada externa
            Trigger(t, "Chegada", new Vector2(6f, 1.5f), new Vector2(3f, 3f), dialogues.Intro, onceFlag: "intro_fortaleza");
            TrapsSetup.Sign(t, 5f, 2.6f, "Espaço: pular");
            var pit = TrapsSetup.Instance<SpikeStrip>(t, "SpikeStrip", "Espinhos do fosso", new Vector2(22f, -2f));
            pit.SetWidth(4);
            TrapsSetup.Rec(pit);
            TrapsSetup.Rec(pit.GetComponentInChildren<SpriteRenderer>());

            // 2) Pátio: fio de alarme na entrada da passagem baixa, flechas saindo da parede dela
            var launcher = TrapsSetup.Instance<ArrowLauncher>(t, "ArrowLauncher", "Lançador do pátio", new Vector2(41.6f, 1.2f));
            launcher.Configure(-1, 0f);
            TrapsSetup.Rec(launcher);
            var wire = TrapsSetup.Instance<TrapTrigger>(t, "Tripwire", "Fio do pátio", new Vector2(38.5f, 0f));
            wire.Configure(TrapTrigger.Kind.Tripwire, 1.6f, new TrapEffect[] { launcher });
            TrapsSetup.Rec(wire);
            TrapsSetup.Sign(t, 38.5f, 3f, "S: agachar (passe por baixo)");

            // 3) Muralha: hera para subir; a parede rachada (portão dos fundos) aparece embaixo, ainda inquebrável
            TrapsSetup.Sign(t, 68f, 3f, "W: escalar a hera");
            var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClassSetup.WallPrefabPath);
            var wall = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab, t);
            wall.name = "Parede rachada (portão dos fundos)";
            wall.transform.position = new Vector3(76.5f, 0f, 0f);
            var wso = new SerializedObject(wall.GetComponent<BreakableWall>());
            wso.FindProperty("wallId").stringValue = "fortaleza_portao_fundos";
            wso.ApplyModifiedPropertiesWithoutUndo();
            TrapsSetup.Sign(t, 76.5f, 5f, "rachaduras douradas... precisa de força");
            Trigger(t, "Saída da fortaleza", new Vector2(88f, 2f), new Vector2(4f, 4f), dialogues.End,
                onceFlag: "", loadScene: Path.GetFileNameWithoutExtension(GameConstants.MainMenuScenePath));
            TrapsSetup.Sign(t, 88f, 4.5f, "saída");

            // S1 no alto da muralha
            ProgressionSetup.PlaceSanctuary(sanctuary, t, "fortaleza_muralha", "Alto da muralha", 76.5f);
            MoveLast(t, Upper);

            // 4) Corredor patrulhado: sombras nas pontas, passarela por cima, mato e um fio de alarme (só barulho)
            var shade = EnemySetup.LoadShadowSprite();
            EnemySetup.CreateShadow(t, "Sombra do corredor A", new Vector2(81f, Upper + 2.5f), new Vector2(10f, 5f), 0.3f, shade);
            EnemySetup.CreateShadow(t, "Sombra do corredor B", new Vector2(103.5f, Upper + 2.5f), new Vector2(8f, 5f), 0.3f, shade);
            Sentinel(t, "Sentinela do corredor", 92.5f, -1, 89f, 96f);
            EnemySetup.PlaceBush(AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.BushPrefabPath), t, "Mato do corredor", 93.5f);
            MoveLast(t, Upper);
            var alarm = TrapsSetup.Instance<TrapTrigger>(t, "Tripwire", "Fio de alarme do corredor", new Vector2(104f, Upper));
            alarm.Configure(TrapTrigger.Kind.Tripwire, 1.6f, new TrapEffect[0]); // alarme: sem armadilha, chama a Sentinela
            alarm.SetNoise(16f, 0.6f); // ouvido no corredor inteiro: ela fica "?" e vem investigar o fio
            TrapsSetup.Rec(alarm);
            TrapsSetup.Sign(t, 80.5f, Upper + 3.2f, "sombra e mato: agache para sumir");

            // 5) Salão de combate: duas Sentinelas
            Sentinel(t, "Sentinela do salão A", 120f, +1, 116f, 124f);
            Sentinel(t, "Sentinela do salão B", 132f, -1, 128f, 136f);
            EnemySetup.PlaceBush(AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.BushPrefabPath), t, "Mato do salão", 127f);
            MoveLast(t, Upper);
            TrapsSetup.Sign(t, 114f, Upper + 3f, "clique: adagas · por trás dói mais");

            // 6) Santuário depois do salão
            ProgressionSetup.PlaceSanctuary(sanctuary, t, "fortaleza_salao", "Depois do salão", 143.5f);
            MoveLast(t, Upper);

            // 6b) Corredor das armadilhas: fogo, serra e fosso com plataformas frágeis
            TrapsSetup.Sign(t, 148f, Upper + 3f, "cuidado: armadilhas");
            for (int i = 0; i < 6; i++)
            {
                var jet = TrapsSetup.Instance<FlameJet>(t, "FlameJet", $"Jato {i + 1}", new Vector2(150.5f + 2f * i, Upper));
                jet.Configure(i * 0.35f, 2f);
                TrapsSetup.Rec(jet);
            }
            var saw = TrapsSetup.Instance<SawBlade>(t, "SawBlade", "Serra", new Vector2(167f, Upper + 0.55f));
            saw.Configure(167f, 173f);
            TrapsSetup.Rec(saw);
            var pitSpikes = TrapsSetup.Instance<SpikeStrip>(t, "SpikeStrip", "Espinhos do fosso alto", new Vector2(179f, 12f));
            pitSpikes.SetWidth(6);
            TrapsSetup.Rec(pitSpikes);
            TrapsSetup.Rec(pitSpikes.GetComponentInChildren<SpriteRenderer>());
            TrapsSetup.Instance<CrumblingPlatform>(t, "CrumblingPlatform", "Frágil 1", new Vector2(177.5f, Upper - 0.25f));
            TrapsSetup.Instance<CrumblingPlatform>(t, "CrumblingPlatform", "Frágil 2", new Vector2(180.5f, Upper - 0.25f));

            // 7) Câmara do Golden (com o santuário "Antes da câmara") e a sala do tomo
            BossSetup.PlaceArena(scene, new BossSetup.ArenaLayout
            {
                MinX = 187f, MaxX = 218f, FloorY = Upper, CeilingHeight = 9f,
                EntryGateX = 186.5f, ExitGateX = 218.5f, BossX = 207f, TomeX = 222.5f, SanctuaryX = 184.5f,
                SanctuaryId = "fortaleza_camara", SanctuaryName = "Antes da câmara",
            });

            // Cenário da Cidadela do Sol Eterno (estandartes, estátuas, pátio de treino, vitrais, placas de lore)
            CitadelDecorSetup.PlaceInFortress(t);
        }

        /// <summary>Sobe o último objeto criado em <paramref name="parent"/> para o andar de cima (y).</summary>
        static void MoveLast(Transform parent, float y)
        {
            var last = parent.GetChild(parent.childCount - 1);
            last.position = new Vector3(last.position.x, y, 0f);
        }

        static void Sentinel(Transform parent, string name, float x, int facing, float patrolA, float patrolB)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.SentinelPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = new Vector3(x, Upper, 0f);
            var so = new SerializedObject(go.GetComponent<EnemyAI>());
            var points = so.FindProperty("patrolPoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).floatValue = patrolA;
            points.GetArrayElementAtIndex(1).floatValue = patrolB;
            so.FindProperty("startFacing").intValue = facing;
            so.FindProperty("respawnDelay").floatValue = 0f; // na Fortaleza só voltam ao descansar ou morrer
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Trigger(Transform parent, string name, Vector2 center, Vector2 size, DialogueDefinition dialogue,
            string onceFlag = "", string requiresFlag = "", string loadScene = "")
        {
            var go = new GameObject("Conversa: " + name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var trigger = go.AddComponent<DialogueTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("dialogue").objectReferenceValue = dialogue;
            so.FindProperty("onceFlag").stringValue = onceFlag;
            so.FindProperty("requiresFlag").stringValue = requiresFlag;
            so.FindProperty("loadSceneAfter").stringValue = loadScene;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
