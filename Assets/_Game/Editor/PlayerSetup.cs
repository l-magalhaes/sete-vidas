using System.IO;
using SeteVidas.Core;
using SeteVidas.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 1: sprites placeholder do gato, PlayerMovementSettings, prefab do jogador
    /// e câmera Cinemachine na Playground. Idempotente: pode rodar de novo.
    /// Spec: Docs/Design/controlador-gato.md.
    /// </summary>
    public static class PlayerSetup
    {
        const string ArtFolder = "Assets/_Game/Art/Characters/Cat/Placeholder";
        const string SettingsPath = "Assets/_Game/ScriptableObjects/Player/PlayerMovementSettings.asset";
        const string MaterialPath = "Assets/_Game/Settings/Physics/PlayerNoFriction.physicsMaterial2D";
        const string ControlsPath = "Assets/_Game/Settings/Input/GameControls.inputactions";
        public const string PrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";
        const string CameraName = "CM Player Camera";

        [MenuItem("Tools/Sete Vidas/4. Criar jogador e câmera (Fase 1)", priority = 40)]
        public static void CreatePlayerAndCamera()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!CreatePlayerAssets()) return;

            var scene = EditorSceneManager.OpenScene(GameConstants.PlaygroundScenePath, OpenSceneMode.Single);
            PlaceInScene(scene);
            Debug.Log("[Sete Vidas] Jogador e câmera prontos na Playground. Aperte Play.");
        }

        /// <summary>Usado pelo "2. Criar cenas": recoloca o jogador na Playground recriada, se o prefab já existir.</summary>
        public static void PlaceInPlaygroundIfReady()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            var scene = EditorSceneManager.OpenScene(GameConstants.PlaygroundScenePath, OpenSceneMode.Single);
            PlaceInScene(scene);
        }

        // ------------------------------------------------------------ assets

        static bool CreatePlayerAssets()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                Debug.LogError($"[Sete Vidas] {ControlsPath} não encontrado.");
                return false;
            }

            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();

            var sprites = CreatePlaceholderSprites();
            var settings = LoadOrCreate(SettingsPath, () => ScriptableObject.CreateInstance<PlayerMovementSettings>());
            var material = LoadOrCreate(MaterialPath, () => new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f });

            // O prefab é sempre gerado de novo a partir do código (os valores ajustáveis ficam no
            // PlayerMovementSettings, que é preservado). Não edite o prefab à mão.
            BuildPrefab(settings, controls, material, sprites);

            AssetDatabase.SaveAssets();
            return true;
        }

        static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        struct CatSprites { public Sprite Idle, Run1, Run2, Jump, Fall, Crouch, Hang; }

        static CatSprites CreatePlaceholderSprites()
        {
            return new CatSprites
            {
                Idle = MakeCatSprite("chr_cat_placeholder_idle", 22, Legs.Stand, 0),
                Run1 = MakeCatSprite("chr_cat_placeholder_run_1", 22, Legs.RunA, 0),
                Run2 = MakeCatSprite("chr_cat_placeholder_run_2", 22, Legs.RunB, 0),
                Jump = MakeCatSprite("chr_cat_placeholder_jump", 22, Legs.Tucked, 1),
                Fall = MakeCatSprite("chr_cat_placeholder_fall", 22, Legs.Stand, -1),
                Crouch = MakeCatSprite("chr_cat_placeholder_crouch", 14, Legs.Stand, 0),
                Hang = MakeCatSprite("chr_cat_placeholder_hang", 22, Legs.Dangling, 1),
            };
        }

        enum Legs { Stand, RunA, RunB, Tucked, Dangling }

        /// <summary>
        /// Retângulo azul-noite (cor do Ladino) com orelhas e olhos, no tamanho final aprovado:
        /// 12 px de largura e <paramref name="height"/> px de altura, num canvas de 16×24 com pivot nos pés.
        /// Olha para a direita; o SpriteRenderer espelha para a esquerda.
        /// </summary>
        static Sprite MakeCatSprite(string name, int height, Legs legs, int eyeShift)
        {
            string path = $"{ArtFolder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 16, h = 24;
            var body = new Color32(0x2b, 0x3a, 0x67, 0xff);
            var outline = new Color32(0x16, 0x1d, 0x38, 0xff);
            var eye = new Color32(0xf2, 0xd0, 0x4b, 0xff);
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            void Set(int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

            int bodyBottom = 2, bodyTop = height - 3; // 2 linhas de pernas, 2 de orelhas
            for (int y = bodyBottom; y <= bodyTop; y++)
            for (int x = 2; x <= 13; x++)
            {
                bool edge = x == 2 || x == 13 || y == bodyBottom || y == bodyTop;
                Set(x, y, edge ? outline : body);
            }

            // Orelhas
            foreach (int x in new[] { 3, 4, 11, 12 })
            {
                Set(x, bodyTop + 1, outline);
                if (x == 3 || x == 12) Set(x, bodyTop + 2, outline);
            }

            // Pernas
            int[] legX = legs switch
            {
                Legs.RunA => new[] { 3, 4, 11, 12 },
                Legs.RunB => new[] { 6, 7, 8, 9 },
                Legs.Tucked => new int[0],
                _ => new[] { 4, 5, 10, 11 },
            };
            foreach (int x in legX) { Set(x, 0, outline); Set(x, 1, outline); }

            // Pendurado: patas da frente esticadas para cima e para a frente (agarrando a quina, à direita)
            if (legs == Legs.Dangling)
            {
                for (int y = bodyTop - 4; y <= bodyTop - 1; y++) Set(14, y, outline);
                Set(15, bodyTop - 1, outline);
            }

            // Olhos (olhando para a direita)
            int eyeY = bodyTop - 3 + eyeShift;
            Set(9, eyeY, eye); Set(12, eyeY, eye);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // PixelArtImportPostprocessor: PPU 16, Point, pivot nos pés
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void BuildPrefab(PlayerMovementSettings settings, InputActionAsset controls, PhysicsMaterial2D material, CatSprites sprites)
        {
            var root = new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer(GameConstants.Layers.Player) };

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            // Caixa (fundo reto): a cápsula escorregava das quinas quando o gato parava na ponta.
            var box = root.AddComponent<BoxCollider2D>();
            float r = PlayerController.ColliderEdgeRadius;
            box.edgeRadius = r;
            box.size = new Vector2(settings.ColliderWidth - 2f * r, settings.StandingHeight - 2f * r);
            box.offset = new Vector2(0f, settings.StandingHeight * 0.5f);
            box.sharedMaterial = material;

            var ground = root.AddComponent<GroundCheck>();
            var reader = root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<PlayerController>();

            SetRef(reader, "actions", controls);
            SetRef(controller, "settings", settings);
            SetRef(controller, "input", reader);
            var so = new SerializedObject(ground);
            so.FindProperty("solidLayers").intValue = LayerMask.GetMask(
                GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            so.ApplyModifiedPropertiesWithoutUndo();

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            visualGo.layer = root.layer;
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprites.Idle;
            sr.sortingOrder = 5;
            var visual = visualGo.AddComponent<PlayerVisual>();
            SetRef(visual, "controller", controller);
            SetRef(visual, "spriteRenderer", sr);
            SetRef(visual, "idle", sprites.Idle);
            SetRef(visual, "jump", sprites.Jump);
            SetRef(visual, "fall", sprites.Fall);
            SetRef(visual, "crouch", sprites.Crouch);
            SetRef(visual, "hang", sprites.Hang);
            var vso = new SerializedObject(visual);
            var run = vso.FindProperty("run");
            run.arraySize = 2;
            run.GetArrayElementAtIndex(0).objectReferenceValue = sprites.Run1;
            run.GetArrayElementAtIndex(1).objectReferenceValue = sprites.Run2;
            vso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Sete Vidas] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------- cena

        static void PlaceInScene(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Camera mainCam = null;
            Vector3 spawn = new Vector3(0.5f, 1f, 0f);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "PlayerSpawn") spawn = root.transform.position;
                if (mainCam == null) mainCam = root.GetComponentInChildren<Camera>();
            }
            // Remove versões antigas antes de recolocar
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Player" || root.name == CameraName)
                    Object.DestroyImmediate(root);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.position = spawn;

            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
                mainCam.gameObject.AddComponent<CinemachineBrain>();

            var camGo = new GameObject(CameraName);
            SceneManager.MoveGameObjectToScene(camGo, scene);
            camGo.transform.position = new Vector3(spawn.x, spawn.y, -10f);

            var cm = camGo.AddComponent<CinemachineCamera>();
            cm.Target.TrackingTarget = player.transform;
            cm.Lens.OrthographicSize = GameConstants.ReferenceHeight / 2f / GameConstants.PixelsPerUnit;

            var composer = camGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.TargetOffset = new Vector3(0f, 1.5f, 0f);          // mostra mais o que está à frente/acima
            composer.Damping = new Vector3(0.3f, 0.5f, 0f);
            composer.Composition.ScreenPosition = Vector2.zero;
            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.1f, 0.2f);
            composer.Lookahead.Enabled = true;
            composer.Lookahead.Time = 0.25f;
            composer.Lookahead.Smoothing = 5f;
            composer.Lookahead.IgnoreY = true;

            camGo.AddComponent<CinemachinePixelPerfect>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
