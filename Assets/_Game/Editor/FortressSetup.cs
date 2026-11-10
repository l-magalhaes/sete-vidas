using System.Collections.Generic;
using System.IO;
using SeteVidas.Combat;
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
    /// O mapa inteiro (tiles e objetos) está em <see cref="FortressLayout"/>; aqui só se monta a cena.
    /// Prévia sem abrir o Unity: Tools/LevelPreview/gerar_mapa.ps1. Salas e anti-softlock: Docs/Level/fortaleza-mvp.md.
    /// ATENÇÃO: recria a cena do zero. Depois de começar a editar a Fortaleza à mão, não rode de novo.
    /// </summary>
    public static class FortressSetup
    {
        const string OutdoorArt = "Assets/_Game/Art/Environments/Outdoors";
        const string CoinPrefabPath = "Assets/_Game/Prefabs/Interactables/Coin.prefab";

        [MenuItem("Tools/Sete Vidas/13. Fase 6: Fortaleza (gera a cena do jogo)", priority = 49)]
        public static void Build()
        {
            if (File.Exists(Path.GetFullPath(GameConstants.FortressScenePath)) &&
                !EditorUtility.DisplayDialog("Gerar a Fortaleza",
                    "Isto recria a cena Fortress_MVP do zero (edições feitas à mão nela se perdem). Continuar?",
                    "Gerar", "Cancelar"))
                return;
            BuildNow();
        }

        // ------------------------------------------- aviso de mapa desatualizado

        /// <summary>Versão do mapa gravada na cena gerada por último (por máquina).</summary>
        const string BuiltVersionKey = "SeteVidas.FortressLayoutBuilt";

        /// <summary>
        /// Quando o <see cref="FortressLayout"/> muda de versão, o Unity pergunta uma vez se quer gerar a cena de novo
        /// (senão o jogo continua com o mapa antigo, porque a cena só muda quando o gerador roda).
        /// </summary>
        [InitializeOnLoadMethod]
        static void AskWhenLayoutChanged()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                int built = EditorPrefs.GetInt(BuiltVersionKey, 0);
                if (built >= FortressLayout.Version) return;
                EditorPrefs.SetInt(BuiltVersionKey, FortressLayout.Version); // pergunta uma vez só por versão
                if (EditorUtility.DisplayDialog("Sete Vidas: o mapa da Fortaleza mudou",
                        $"O layout da Fortaleza está na versão {FortressLayout.Version}, mas a cena foi gerada com uma versão anterior.\n\n" +
                        "Gerar a cena Fortress_MVP agora? (Edições feitas à mão nela se perdem.)\n\n" +
                        "Dá para gerar depois em Tools > Sete Vidas > 13. Fase 6: Fortaleza.",
                        "Gerar agora", "Depois"))
                    BuildNow();
            };
        }

        static void BuildNow()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorPrefs.SetInt(BuiltVersionKey, FortressLayout.Version);

            // Prefabs que a Fortaleza usa (todos idempotentes)
            PlayerSetup.CreatePlayerAssets();
            EnemySetup.CreateSentinelPrefab();
            EnemySetup.CreateCrossbowSentinelPrefab();
            EnemySetup.CreateBushPrefab();
            ProgressionSetup.CreateHudPrefab();
            ProgressionSetup.CreateSanctuaryPrefab();
            ClassSetup.CreateWallPrefab();
            BossSetup.CreatePrefabs();
            TrapsSetup.CreatePrefabs();
            CreateDialogues(); // garante os assets; são carregados de novo depois de abrir a cena

            var scene = SeteVidasSetup.NewSceneFromTemplate(GameConstants.FortressScenePath);
            var cam = SeteVidasSetup.FindCamera(scene);
            SeteVidasSetup.SetupPixelPerfectCamera(cam, new Vector2(FortressLayout.SpawnX, FortressLayout.SpawnY + 4f));
            cam.backgroundColor = new Color32(0x1f, 0x15, 0x33, 0xff); // topo do céu do pôr do sol
            var tiles = SeteVidasSetup.CreatePlaceholderTiles(); // depois de abrir a cena (ver CreatePlayground)
            // Abrir a cena descarrega da memória os assets criados antes: as conversas precisam ser carregadas
            // agora, senão as referências salvas ficam vazias (a narração não tocava)
            var dialogues = CreateDialogues();

            var grid = SeteVidasSetup.CreateGrid();
            var maps = new Dictionary<FortressLayout.Map, Tilemap>
            {
                [FortressLayout.Map.Background] = SeteVidasSetup.CreateTilemap(grid, "Background", null, -10, false),
                [FortressLayout.Map.Ground] = SeteVidasSetup.CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true),
                [FortressLayout.Map.Climbable] = SeteVidasSetup.CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 0, true),
            };
            SeteVidasSetup.CreateTilemap(grid, "Foreground", null, 10, false);
            BuildTiles(tiles, maps);

            var root = new GameObject("Fortaleza");
            SceneManager.MoveGameObjectToScene(root, scene);
            PlaceObjects(scene, root.transform, dialogues);
            BuildScenery(scene);

            SeteVidasSetup.BuildTileColliders(scene);
            PlayerSetup.PlaceInScene(scene); // gato + câmera no PlayerSpawn; salva a cena
            SeteVidasSetup.RegisterScenesInBuild();
            Debug.Log("[Sete Vidas] Fortaleza gerada (Scenes/World/Fortress_MVP). Novo jogo começa no alto da montanha. Mapa: Docs/Level/fortaleza-mvp.md");
        }

        // ------------------------------------------------------------- tiles

        static void BuildTiles(SeteVidasSetup.PlaceholderTiles t, Dictionary<FortressLayout.Map, Tilemap> maps)
        {
            foreach (var r in FortressLayout.Tiles())
            {
                var map = maps[r.Map];
                TileBase tile = r.Clear ? null
                    : r.Map == FortressLayout.Map.Ground ? t.Ground
                    : r.Map == FortressLayout.Map.Climbable ? t.Climbable : t.Background;
                SeteVidasSetup.Fill(map, tile, r.X0, r.Y0, r.X1, r.Y1);
            }
        }

        // --------------------------------------------------------- conversas

        static Dictionary<string, DialogueDefinition> CreateDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var d = CitadelDecorSetup.CreateLore(); // placas de lore (D-PLACA-xx)
            d["D-FORT-01"] = DialogueSetup.Conversation("D-FORT-01", true, 5,
                (null, "", "Do alto da montanha, a Cidadela do Sol Eterno brilha no pôr do sol."),
                (cat, "ironico", "Muralhas, sentinelas, armadilhas. O contratante não exagerou."),
                (cat, "", "Caminho mais curto até lá? Para baixo."));
            d["D-FORT-02"] = DialogueSetup.Conversation("D-FORT-02", true, 1,
                (cat, "ironico", "Água. Claro que tinha que ser água."),
                (cat, "", "Caí de pé, pelo menos. Como sempre."));
            d["D-FIM"] = DialogueSetup.Conversation("D-FIM", true, 2,
                (null, "", "Você deixa a Cidadela sem o tomo. Com uma moeda de ouro e um juramento."),
                (cat, "", "Uma moeda. Seis vidas. Vou cobrar o troco com juros, capivara."),
                (null, "", "Fim da demonstração. Obrigado por jogar!"));
            AssetDatabase.SaveAssets();
            return d;
        }

        // ---------------------------------------------------------- objetos

        static readonly HashSet<string> Linked = new HashSet<string> { "tripwire", "plate" };

        static void PlaceObjects(Scene scene, Transform t, Dictionary<string, DialogueDefinition> dialogues)
        {
            var spawn = new GameObject("PlayerSpawn");
            SceneManager.MoveGameObjectToScene(spawn, scene);
            spawn.transform.position = new Vector3(FortressLayout.SpawnX, FortressLayout.SpawnY + 0.1f, 0f);

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.HudPrefabPath), scene);
            hud.name = "HUD";

            CitadelDecorSetup.EnsureFolder();
            var created = new Dictionary<string, Component>();
            var objects = FortressLayout.Objects();
            // 1ª passada: tudo menos os gatilhos de armadilha; 2ª: fios e placas, que ligam nos efeitos já criados
            foreach (var o in objects) if (!Linked.Contains(o.Kind)) Place(t, o, dialogues, created);
            foreach (var o in objects) if (Linked.Contains(o.Kind)) Place(t, o, dialogues, created);

            // A Câmara do Selo: chefe (Koda), grades, tomo e o santuário "Antes da câmara"
            BossSetup.PlaceArena(scene, new BossSetup.ArenaLayout
            {
                MinX = FortressLayout.ArenaMinX, MaxX = FortressLayout.ArenaMaxX, FloorY = FortressLayout.ArenaFloor,
                CeilingHeight = FortressLayout.ArenaCeiling, EntryGateX = FortressLayout.ArenaEntryGateX,
                ExitGateX = FortressLayout.ArenaExitGateX, BossX = FortressLayout.BossX, TomeX = FortressLayout.TomeX,
                SanctuaryX = FortressLayout.SealSanctuaryX, SanctuaryId = "fortaleza_camara", SanctuaryName = "Antes da câmara",
            });
        }

        static void Place(Transform t, FortressLayout.Obj o, Dictionary<string, DialogueDefinition> dialogues, Dictionary<string, Component> created)
        {
            var at = new Vector2(o.X, o.Y);
            Component made = null;
            switch (o.Kind)
            {
                case "trigger":
                    dialogues.TryGetValue(o.Id, out var dialogue);
                    Trigger(t, o.Name, at, new Vector2(o.W, o.H), dialogue, onceFlag: o.Link,
                        loadScene: o.Text == "menu" ? Path.GetFileNameWithoutExtension(GameConstants.MainMenuScenePath) : "");
                    break;
                case "sign":
                    TrapsSetup.Sign(t, o.X, o.Y, o.Text);
                    break;
                case "lore":
                    if (dialogues.TryGetValue(o.Id, out var text)) CitadelDecorSetup.Sign(t, o.Name, o.X, o.Y, text);
                    else Debug.LogWarning($"[Sete Vidas] Placa sem texto: {o.Id}");
                    break;
                case "prop":
                    CitadelDecorSetup.Prop(t, o.Name, o.Text, at);
                    break;
                case "shield":
                    for (int i = 0; i < (int)o.W; i++) CitadelDecorSetup.Prop(t, $"{o.Name} {i + 1}", "shield", at + Vector2.right * (o.A * i));
                    break;
                case "dummy":
                    CitadelDecorSetup.Dummy(t, o.Name, at, o.Facing >= 0 ? TrainingDummy.Side.Right : TrainingDummy.Side.Left);
                    break;
                case "sentinel":
                    made = Sentinel(t, o.Name, at, o.Facing, o.A, o.B);
                    break;
                case "archer":
                    made = Archer(t, o.Name, at, o.Facing);
                    break;
                case "bush":
                    EnemySetup.PlaceBush(AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.BushPrefabPath), t, o.Name, o.X);
                    MoveLast(t, o.Y);
                    break;
                case "shadow":
                    made = EnemySetup.CreateShadow(t, o.Name, at, new Vector2(o.W, o.H), 0.3f, EnemySetup.LoadShadowSprite());
                    break;
                case "sanctuary":
                    ProgressionSetup.PlaceSanctuary(AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.SanctuaryPrefabPath), t, o.Id, o.Text, o.X);
                    MoveLast(t, o.Y);
                    break;
                case "wall":
                    made = Wall(t, o.Name, at, o.Id);
                    break;
                case "coin":
                    made = Coin(t, o.Name, at, o.Id, o.Value);
                    break;
                case "spikes":
                {
                    var s = TrapsSetup.Instance<SpikeStrip>(t, "SpikeStrip", o.Name, at);
                    s.SetWidth((int)o.W);
                    TrapsSetup.Rec(s);
                    TrapsSetup.Rec(s.GetComponentInChildren<SpriteRenderer>());
                    made = s;
                    break;
                }
                case "launcher":
                {
                    var l = TrapsSetup.Instance<ArrowLauncher>(t, "ArrowLauncher", o.Name, at);
                    l.Configure(o.Facing, o.A);
                    TrapsSetup.Rec(l);
                    made = l;
                    break;
                }
                case "spears":
                    made = TrapsSetup.Instance<SpearTrap>(t, "SpearTrap", o.Name, at);
                    break;
                case "flame":
                {
                    var j = TrapsSetup.Instance<FlameJet>(t, "FlameJet", o.Name, at);
                    j.Configure(o.A, 2f);
                    TrapsSetup.Rec(j);
                    made = j;
                    break;
                }
                case "saw":
                {
                    var s = TrapsSetup.Instance<SawBlade>(t, "SawBlade", o.Name, at);
                    s.Configure(o.A, o.B);
                    TrapsSetup.Rec(s);
                    made = s;
                    break;
                }
                case "crumble":
                    made = TrapsSetup.Instance<CrumblingPlatform>(t, "CrumblingPlatform", o.Name, at);
                    break;
                case "tripwire":
                case "plate":
                {
                    var targets = new List<TrapEffect>();
                    if (!string.IsNullOrEmpty(o.Link) && created.TryGetValue(o.Link, out var target) && target is TrapEffect effect) targets.Add(effect);
                    var trigger = TrapsSetup.Instance<TrapTrigger>(t, o.Kind == "plate" ? "PressurePlate" : "Tripwire", o.Name, at);
                    trigger.Configure(o.Kind == "plate" ? TrapTrigger.Kind.PressurePlate : TrapTrigger.Kind.Tripwire, o.W > 0f ? o.W : 1.6f, targets.ToArray());
                    if (o.A > 0f) trigger.SetNoise(o.A, 0.6f); // fio de alarme: o barulho chama as sentinelas
                    TrapsSetup.Rec(trigger);
                    made = trigger;
                    break;
                }
                case "waterfall":
                    Waterfall(t, o.Name, o.X, o.Y, o.H);
                    break;
                case "pool":
                    Pool(t, o.Name, o.X, o.Y, (int)o.W);
                    break;
                default:
                    Debug.LogWarning($"[Sete Vidas] Objeto de layout desconhecido: {o.Kind} ({o.Name})");
                    break;
            }
            if (made != null) created[o.Name] = made;
        }

        /// <summary>Sobe/desce o último objeto criado em <paramref name="parent"/> para <paramref name="y"/>.</summary>
        static void MoveLast(Transform parent, float y)
        {
            var last = parent.GetChild(parent.childCount - 1);
            last.position = new Vector3(last.position.x, y, 0f);
        }

        static EnemyAI Sentinel(Transform parent, string name, Vector2 at, int facing, float patrolA, float patrolB)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.SentinelPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = at;
            var ai = go.GetComponent<EnemyAI>();
            var so = new SerializedObject(ai);
            var points = so.FindProperty("patrolPoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).floatValue = patrolA;
            points.GetArrayElementAtIndex(1).floatValue = patrolB;
            so.FindProperty("startFacing").intValue = facing;
            so.FindProperty("respawnDelay").floatValue = 0f; // na Fortaleza só voltam ao descansar ou morrer
            so.ApplyModifiedPropertiesWithoutUndo();
            return ai;
        }

        /// <summary>Cão Sentinela Atirador: parado no posto (sem patrulha), olhando para <paramref name="facing"/>.</summary>
        static EnemyAI Archer(Transform parent, string name, Vector2 at, int facing)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.CrossbowPrefabPath);
            if (prefab == null) prefab = EnemySetup.CreateCrossbowSentinelPrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = at;
            var ai = go.GetComponent<EnemyAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("patrolPoints").arraySize = 0;
            so.FindProperty("startFacing").intValue = facing;
            so.FindProperty("respawnDelay").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ai;
        }

        static BreakableWall Wall(Transform parent, string name, Vector2 at, string id)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClassSetup.WallPrefabPath), parent);
            go.name = name;
            go.transform.position = at;
            var wall = go.GetComponent<BreakableWall>();
            var so = new SerializedObject(wall);
            so.FindProperty("wallId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return wall;
        }

        static CoinPickup Coin(Transform parent, string name, Vector2 at, string id, int value)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
            if (prefab == null) { Debug.LogWarning("[Sete Vidas] Coin.prefab não encontrado"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = at;
            var coin = go.GetComponent<CoinPickup>();
            var so = new SerializedObject(coin);
            so.FindProperty("pickupId").stringValue = id;
            so.FindProperty("value").intValue = Mathf.Max(1, value);
            so.ApplyModifiedPropertiesWithoutUndo();
            return coin;
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

        // ---------------------------------------------------------- cenário

        static Sprite Outdoor(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutdoorArt}/{name}.png");
            if (s == null) Debug.LogWarning($"[Sete Vidas] Falta {name}.png. Rode Tools/CharacterSprites/gerar.ps1");
            return s;
        }

        static Sprite[] Frames(string prefix, int count)
        {
            var list = new Sprite[count];
            for (int i = 0; i < count; i++) list[i] = Outdoor(prefix + i);
            return list;
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, Vector3 local, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Céu do pôr do sol (preso à câmera), montanhas ao longe com a Cidadela e morros com pinheiros (paralaxe).
        /// Dentro do castelo o fundo de pedra cobre tudo; do lado de fora, o pôr do sol aparece.
        /// </summary>
        static void BuildScenery(Scene scene)
        {
            var root = new GameObject("Céu e montanhas");
            SceneManager.MoveGameObjectToScene(root, scene);

            var sky = Renderer(root.transform, "Céu do pôr do sol", Outdoor("env_sky_sunset"), Vector3.zero, -100);
            sky.gameObject.AddComponent<ParallaxLayer>().Configure(Vector2.zero, Vector2.zero, 0f);

            Layer(root.transform, "Montanhas ao longe", Outdoor("env_mountains_far"), new Vector2(0.06f, 0.06f), new Vector2(0f, 4f), -95);
            Layer(root.transform, "Morros com pinheiros", Outdoor("env_hills_near"), new Vector2(0.15f, 0.15f), new Vector2(0f, 1f), -90);
        }

        static void Layer(Transform parent, string name, Sprite sprite, Vector2 factor, Vector2 anchor, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            float width = sprite != null ? sprite.bounds.size.x : 40f;
            for (int i = -1; i <= 1; i++) Renderer(go.transform, $"{name} {i + 2}", sprite, new Vector3(i * width, 0f, 0f), order);
            go.AddComponent<ParallaxLayer>().Configure(factor, anchor, width);
        }

        /// <summary>Cachoeira do penhasco: coluna de pedaços de 2×2 tiles que descem juntos, com espuma na lagoa.</summary>
        static void Waterfall(Transform parent, string name, float x, float bottom, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, bottom, 0f);
            var frames = Frames("env_waterfall_", 4);
            for (float y = 1f; y < height + 1f; y += 2f) Renderer(go.transform, "Água", frames[0], new Vector3(0f, y, 0f), -6);
            go.AddComponent<SpriteLoop>().Configure(frames, 0.08f);

            var foam = new GameObject("Espuma");
            foam.transform.SetParent(parent, false);
            foam.transform.position = new Vector3(x, bottom + 0.4f, 0f);
            var foamFrames = Frames("env_waterfall_foam_", 2);
            Renderer(foam.transform, "Espuma", foamFrames[0], Vector3.zero, 9);
            foam.AddComponent<SpriteLoop>().Configure(foamFrames, 0.2f);
        }

        /// <summary>Lagoa ao pé da cachoeira: água na frente do gato (ele "cai de pé" dentro dela).</summary>
        static void Pool(Transform parent, string name, float centerX, float floorY, int width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centerX, floorY, 0f);
            var frames = Frames("env_pool_", 2);
            for (int i = 0; i < width; i++)
            {
                var sr = Renderer(go.transform, "Água", frames[0], new Vector3(i - (width - 1) * 0.5f, 0.625f, 0f), 8);
                sr.color = new Color(1f, 1f, 1f, 0.78f);
            }
            go.AddComponent<SpriteLoop>().Configure(frames, 0.5f);
        }
    }
}
