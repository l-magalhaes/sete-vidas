using System.Collections.Generic;
using System.IO;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Enemies;
using SeteVidas.World;
using SeteVidas.World.Traps;
using SeteVidas.World.Scenery;
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

            // Biblioteca de cenário: garante os sprites desenhados no editor e lê os manifest.json
            CitadelDecorSetup.EnsureFolder();
            CitadelDecorSetup.EnsureSprites();
            var library = EnvLibrary.ImportAll();
            if (library.Problems.Count > 0)
                Debug.LogWarning($"[Sete Vidas] Biblioteca de cenário com {library.Problems.Count} problema(s); a Fortaleza é gerada mesmo assim.");

            var grid = SeteVidasSetup.CreateGrid();
            var maps = new Dictionary<FortressLayout.Map, Tilemap>
            {
                [FortressLayout.Map.Background] = SeteVidasSetup.CreateTilemap(grid, "Background", null, -10, false),
                [FortressLayout.Map.Ground] = SeteVidasSetup.CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true),
                [FortressLayout.Map.Climbable] = SeteVidasSetup.CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 1, true), // ordem 1: a vinha sempre por cima da pedra
            };
            SeteVidasSetup.CreateTilemap(grid, "Foreground", null, 10, false);
            BuildTiles(tiles, maps);

            var slopes = new GameObject("Rampas");
            SceneManager.MoveGameObjectToScene(slopes, scene);
            foreach (var r in FortressLayout.Slopes())
                SlopeSetup.Create(slopes.transform, "Rampa " + r.Note, r.X0, r.Y0, r.W, r.H, r.RisesRight, FortressTerrain.MaterialAt(r.X0, r.Y0));

            // Terreno natural (Etapa 2 do plano de cenário): solo e rocha no lugar do tile provisório, com tufos e seixos
            var terrainDetails = new GameObject("Detalhes do terreno");
            SceneManager.MoveGameObjectToScene(terrainDetails, scene);
            int painted = TerrainPainter.Paint(maps[FortressLayout.Map.Ground], maps[FortressLayout.Map.Climbable], maps[FortressLayout.Map.Background],
                FortressLayout.Slopes(), terrainDetails.transform);
            Debug.Log($"[Sete Vidas] Terreno e arquitetura: {painted} tiles, {terrainDetails.transform.childCount} detalhes (tufos, seixos, fendas, gárgulas).");

            var root = new GameObject("Fortaleza");
            SceneManager.MoveGameObjectToScene(root, scene);
            PlaceObjects(scene, root.transform, dialogues);
            var dressing = new GameObject("Enfeites");
            dressing.transform.SetParent(root.transform, false);
            DressingBuilder.Build(dressing.transform, FortressDressing.Items());
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

            // a parede quebrável usa a pedra do lugar (plano, Etapa 3): uma porta antiga fechada com a alvenaria da época
            string material = FortressTerrain.MaterialAt(Mathf.FloorToInt(at.x), Mathf.FloorToInt(at.y));
            if (TerrainAutotile.IsMasonry(material) && material != TerrainAutotile.Wood)
            {
                var intact = TerrainPainter.LoadSprite(TerrainAutotile.BreakableWallName(material, "intacta"));
                var cracked = TerrainPainter.LoadSprite(TerrainAutotile.BreakableWallName(material, "danificada"));
                var rubble = TerrainPainter.LoadSprite(TerrainAutotile.BreakableWallName(material, "entulho"));
                if (intact != null && cracked != null)
                {
                    so.FindProperty("intactSprite").objectReferenceValue = intact;
                    so.FindProperty("crackedSprite").objectReferenceValue = cracked;
                    var sr = (SpriteRenderer)so.FindProperty("spriteRenderer").objectReferenceValue;
                    if (sr != null) { sr.sprite = intact; PrefabUtility.RecordPrefabInstancePropertyModifications(sr); }
                    var rubbleGo = (GameObject)so.FindProperty("rubble").objectReferenceValue;
                    var rsr = rubbleGo != null ? rubbleGo.GetComponent<SpriteRenderer>() : null;
                    if (rsr != null && rubble != null) { rsr.sprite = rubble; PrefabUtility.RecordPrefabInstancePropertyModifications(rsr); }
                }
            }
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
        /// Paralaxe, âncora, repetição e ordem vêm do manifesto (Art/Environments/Outdoors/manifest.json).
        /// Dentro do castelo o fundo de pedra cobre tudo; do lado de fora, o pôr do sol aparece.
        /// Os conjuntos de fundo por área e altura (A–H) entram na Etapa 6 do plano de cenário.
        /// </summary>
        static void BuildScenery(Scene scene)
        {
            var root = new GameObject("Céu e montanhas");
            SceneManager.MoveGameObjectToScene(root, scene);
            DressingBuilder.Background(root.transform, "bg_ceu_por_do_sol", "Céu do pôr do sol");
            DressingBuilder.Background(root.transform, "bg_ext_cordilheira_cidadela", "Montanhas ao longe");
            DressingBuilder.Background(root.transform, "bg_ext_morros_pinheiros", "Morros com pinheiros");
        }

        /// <summary>Quadros e tempo por quadro de um asset animado da biblioteca.</summary>
        static (Sprite[] frames, float frameTime, int order) Animated(string id)
        {
            var def = EnvLibrary.Get(id);
            if (def == null || def.frames.Length == 0) return (new Sprite[] { null }, 0.1f, 0);
            return (def.frames, 1f / def.entry.animacao.fps, def.SortingOrder);
        }

        /// <summary>
        /// Cachoeira: nasce de uma fenda na face do penhasco (sprite da nascente, sobre a rocha), desce colada na face
        /// em pedaços de 2 tiles e bate na lagoa com espuma. <paramref name="x"/> = meio da coluna (24 px),
        /// <paramref name="surface"/> = linha da água da lagoa, <paramref name="height"/> = da superfície ao topo da água.
        /// </summary>
        static void Waterfall(Transform parent, string name, float x, float surface, float height)
        {
            const float Px = 1f / 16f;
            float top = surface + height;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, surface, 0f);

            // nascente 32×24: a coluna ocupa x 8..31 do sprite (meio em 19,5 px) e as 8 linhas de baixo
            var spring = Animated("env_ext_ani_cachoeira_nascente");
            var springGo = new GameObject("Nascente");
            springGo.transform.SetParent(go.transform, false);
            springGo.transform.position = new Vector3(x - (19.5f - 16f) * Px, top - 12f * Px, 0f);
            Renderer(springGo.transform, "Nascente", spring.frames[0], Vector3.zero, spring.order);
            springGo.AddComponent<SpriteLoop>().Configure(spring.frames, spring.frameTime);

            // coluna: de cima para baixo, começando onde a nascente termina (sem sobrepor: duas águas translúcidas
            // juntas escurecem); o último pedaço pode
            // passar da linha da água (fica atrás do chão e da lagoa)
            var water = Animated("env_ext_ani_cachoeira");
            var column = new GameObject("Coluna");
            column.transform.SetParent(go.transform, false);
            for (float pieceTop = top - 24f * Px; pieceTop > surface; pieceTop -= 2f)
                Renderer(column.transform, "Água", water.frames[0], new Vector3(x, pieceTop - 1f, 0f) - go.transform.position, water.order);
            column.AddComponent<SpriteLoop>().Configure(water.frames, water.frameTime);

            // espuma: a linha da água fica na linha 9 do sprite (2 px abaixo do meio)
            var foamAnim = Animated("env_ext_ani_cachoeira_espuma");
            var foam = new GameObject("Espuma");
            foam.transform.SetParent(parent, false);
            foam.transform.position = new Vector3(x, surface + 2f * Px, 0f);
            Renderer(foam.transform, "Espuma", foamAnim.frames[0], Vector3.zero, foamAnim.order);
            foam.AddComponent<SpriteLoop>().Configure(foamAnim.frames, foamAnim.frameTime);
        }

        /// <summary>
        /// Lagoa: um tile de água por célula da bacia, na frente do gato (ele "cai de pé" dentro dela). A água é
        /// translúcida no próprio sprite; a superfície fica 2 px abaixo do topo do tile.
        /// </summary>
        static void Pool(Transform parent, string name, float centerX, float floorY, int width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            int firstCell = Mathf.RoundToInt(centerX - width * 0.5f);
            go.transform.position = new Vector3(firstCell, floorY, 0f);
            var pool = Animated("env_ext_ani_lagoa");
            for (int i = 0; i < width; i++)
                Renderer(go.transform, "Água", pool.frames[0], new Vector3(i + 0.5f, 0.5f, 0f), pool.order);
            go.AddComponent<SpriteLoop>().Configure(pool.frames, pool.frameTime);
        }
    }
}
