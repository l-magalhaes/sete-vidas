using System.IO;
using SeteVidas.Abilities;
using SeteVidas.Classes;
using SeteVidas.Combat;
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
        const string ClimbSettingsPath = "Assets/_Game/ScriptableObjects/Player/WallClimbSettings.asset";
        const string DaggersPath = "Assets/_Game/ScriptableObjects/Combat/RogueDaggers.asset";
        const string StealthPath = "Assets/_Game/ScriptableObjects/Player/StealthSettings.asset";
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

        /// <summary>Cria/atualiza sprites, assets de valores e o prefab do jogador (Fases 1 e 2).</summary>
        public static bool CreatePlayerAssets()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                Debug.LogError($"[Sete Vidas] {ControlsPath} não encontrado.");
                return false;
            }

            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(DaggersPath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();

            var sprites = CreatePlaceholderSprites();
            var assets = new PlayerAssets
            {
                Movement = LoadOrCreate(SettingsPath, () => ScriptableObject.CreateInstance<PlayerMovementSettings>()),
                Climb = LoadOrCreate(ClimbSettingsPath, () => ScriptableObject.CreateInstance<WallClimbSettings>()),
                Daggers = LoadOrCreate(DaggersPath, () => ScriptableObject.CreateInstance<MeleeAttackSettings>()),
                Stealth = LoadOrCreate(StealthPath, () => ScriptableObject.CreateInstance<StealthSettings>()),
                Material = LoadOrCreate(MaterialPath, () => new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f }),
                Controls = controls,
            };

            // O prefab é sempre gerado de novo a partir do código (os valores ajustáveis ficam nos
            // ScriptableObjects, que são preservados). Não edite o prefab à mão.
            assets.Classes = ClassSetup.CreateClassAssets(assets.Daggers); // Ladino e Paladino (Fase 5)
            BuildPrefab(assets, sprites);

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

        struct PlayerAssets
        {
            public PlayerMovementSettings Movement;
            public WallClimbSettings Climb;
            public MeleeAttackSettings Daggers;
            public StealthSettings Stealth;
            public ClassSetup.ClassAssets Classes;
            public PhysicsMaterial2D Material;
            public InputActionAsset Controls;
        }

        struct CatSprites { public Sprite Idle, Run1, Run2, Jump, Fall, Crouch, Hang, Climb1, Climb2, Attack, Slash1, Slash2; }

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
                Climb1 = MakeCatSprite("chr_cat_placeholder_climb_1", 22, Legs.ClimbA, 1),
                Climb2 = MakeCatSprite("chr_cat_placeholder_climb_2", 22, Legs.ClimbB, 1),
                Attack = MakeCatSprite("chr_cat_placeholder_attack", 22, Legs.Strike, 0),
                Slash1 = MakeSlashSprite("fx_dagger_slash_1", 8),
                Slash2 = MakeSlashSprite("fx_dagger_slash_2", 4),
            };
        }

        enum Legs { Stand, RunA, RunB, Tucked, Dangling, ClimbA, ClimbB, Strike }

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
                Legs.Tucked or Legs.ClimbA or Legs.ClimbB => new int[0],
                _ => new[] { 4, 5, 10, 11 },
            };
            foreach (int x in legX) { Set(x, 0, outline); Set(x, 1, outline); }

            // Pendurado: patas da frente esticadas para cima e para a frente (agarrando a quina, à direita)
            if (legs == Legs.Dangling)
            {
                for (int y = bodyTop - 4; y <= bodyTop - 1; y++) Set(14, y, outline);
                Set(15, bodyTop - 1, outline);
            }

            // Escalando: patas encostadas na parede (à direita), alternando a altura entre os dois quadros
            if (legs == Legs.ClimbA || legs == Legs.ClimbB)
            {
                int front = legs == Legs.ClimbA ? bodyTop - 2 : bodyTop - 5;
                int back = legs == Legs.ClimbA ? 3 : 6;
                foreach (int y in new[] { front, front + 1, back, back + 1 }) { Set(14, y, outline); Set(15, y, outline); }
            }

            // Golpe: pata da frente esticada na altura do peito
            if (legs == Legs.Strike)
            {
                int y = bodyTop - 7;
                Set(14, y, outline); Set(15, y, outline); Set(14, y + 1, outline); Set(15, y + 1, outline);
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

        /// <summary>
        /// Corte da adaga: arco claro de 18×16 px virado para a direita (o tamanho da hitbox, ~1,1 × 0,9 tile).
        /// <paramref name="centerY"/> muda a curva entre o 1º golpe (horizontal) e o 2º (de baixo para cima).
        /// </summary>
        static Sprite MakeSlashSprite(string name, int centerY)
        {
            string path = $"{ArtFolder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 18, h = 16;
            var edge = new Color32(0xf4, 0xf7, 0xfb, 0xff);
            var inner = new Color32(0xa9, 0xbc, 0xd6, 0xff);
            var px = new Color32[w * h];
            const float cx = -2f, outer = 17.5f, mid = 16f, innerR = 14f;
            for (int y = 0; y < h; y++)
            for (int x = 6; x < w; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - centerY) * (y - centerY));
                if (d <= outer && d > mid) px[y * w + x] = edge;
                else if (d <= mid && d > innerR) px[y * w + x] = inner;
            }
            return SavePng(path, w, h, px);
        }

        static Sprite SavePng(string path, int w, int h, Color32[] px)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // PixelArtImportPostprocessor: PPU 16, Point
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void BuildPrefab(PlayerAssets assets, CatSprites sprites)
        {
            var settings = assets.Movement;
            var controls = assets.Controls;
            var material = assets.Material;
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
            var climb = root.AddComponent<WallClimbAbility>();
            var combat = root.AddComponent<CombatController>();
            var stealth = root.AddComponent<StealthController>();
            var health = root.AddComponent<PlayerHealth>();
            root.AddComponent<SafeGroundTracker>(); // último chão seguro (espinhos devolvem o gato para cá)
            var classes = root.AddComponent<ClassManager>();
            var daggersAbility = root.AddComponent<MeleeWeaponAbility>();
            var hammerAbility = root.AddComponent<MeleeWeaponAbility>();
            root.AddComponent<ClassSwitchFeedback>();
            SetArray(classes, "classes", assets.Classes.Rogue, assets.Classes.Paladin);
            SetRef(classes, "input", reader);
            SetRef(classes, "combat", combat);
            SetEnum(daggersAbility, "forClass", (int)CharacterClass.Rogue);
            SetEnum(hammerAbility, "forClass", (int)CharacterClass.Paladin);
            foreach (var a in new[] { daggersAbility, hammerAbility })
            {
                SetRef(a, "combat", combat);
                SetRef(a, "classes", classes);
            }

            SetRef(reader, "actions", controls);
            SetRef(controller, "settings", settings);
            SetRef(controller, "input", reader);
            SetRef(climb, "settings", assets.Climb);
            SetMask(climb, "climbableLayers", GameConstants.Layers.Climbable);
            SetRef(combat, "weapon", assets.Daggers);
            SetRef(combat, "input", reader);
            SetMask(combat, "targetLayers", GameConstants.Layers.Enemy, GameConstants.Layers.Breakable);
            SetRef(stealth, "settings", assets.Stealth);
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
            SetRef(visual, "attack", sprites.Attack);
            SetRef(visual, "wallClimb", climb);
            SetRef(visual, "combat", combat);
            SetRef(visual, "health", health);
            SetRef(visual, "classes", classes);
            SetArray(visual, "run", sprites.Run1, sprites.Run2);
            SetArray(visual, "climb", sprites.Climb1, sprites.Climb2);
            SetArray(visual, "slash", sprites.Slash1, sprites.Slash2);

            // Corte da adaga: fora do "Visual" para não herdar o espelhamento do corpo
            var slashGo = new GameObject("Slash");
            slashGo.transform.SetParent(root.transform, false);
            slashGo.layer = root.layer;
            var slashRenderer = slashGo.AddComponent<SpriteRenderer>();
            slashRenderer.sortingOrder = 6;
            slashRenderer.enabled = false;
            SetRef(visual, "slashRenderer", slashRenderer);

            // Camada da classe (marreta etc.): filho do Visual, desenhada por cima do gato
            var overlayGo = new GameObject("ClassOverlay");
            overlayGo.transform.SetParent(visualGo.transform, false);
            overlayGo.layer = root.layer;
            var overlay = overlayGo.AddComponent<SpriteRenderer>();
            overlay.sortingOrder = 6;
            overlay.enabled = false;
            SetRef(visual, "classOverlay", overlay);

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

        static void SetArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Sete Vidas] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Sete Vidas] Campo {field} não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetMask(Object target, string field, params string[] layers)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Sete Vidas] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.intValue = LayerMask.GetMask(layers);
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
