using System.IO;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Enemies;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 3: Sentinela (sprites + prefab + SentinelSettings), zonas de sombra e o trecho H
    /// da Playground. Idempotente. Spec: Docs/Design/furtividade-sentinela.md.
    /// </summary>
    public static class EnemySetup
    {
        const string SentinelArtFolder = "Assets/_Game/Art/Enemies/Sentinel";
        const string ShadowSpritePath = "Assets/_Game/Art/Environments/Placeholder/fx_shadow_zone.png";
        const string SettingsPath = "Assets/_Game/ScriptableObjects/Enemies/SentinelSettings.asset";
        const string NoFrictionPath = "Assets/_Game/Settings/Physics/PlayerNoFriction.physicsMaterial2D";
        public const string SentinelPrefabPath = "Assets/_Game/Prefabs/Enemies/Sentinel.prefab";
        public const string BushPrefabPath = "Assets/_Game/Prefabs/Interactables/Bush.prefab";
        const string BushSpritePath = "Assets/_Game/Art/Environments/Placeholder/env_bush_placeholder.png";

        [MenuItem("Tools/Sete Vidas/6. Fase 3: Sentinela e furtividade", priority = 42)]
        public static void RunPhase3()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SeteVidasSetup.ConfigureProject();               // matriz: gato e inimigos não colidem
            if (!PlayerSetup.CreatePlayerAssets()) return;   // gato com furtividade e dano provisório
            CombatSetup.CreateDummyPrefab();
            CreateSentinelPrefab();
            CreateBushPrefab();
            SeteVidasSetup.CreateScenes();                   // Playground com o trecho H (x 122–151)
            Debug.Log("[Sete Vidas] Fase 3 pronta: Sentinela patrulhando em x 133–140, sombras em x 122–130 e 144–152, passarela por cima.");
        }

        // ------------------------------------------------------------ prefab

        struct SentinelSprites { public Sprite Idle, Walk1, Walk2, Windup, Attack; }

        public static GameObject CreateSentinelPrefab()
        {
            Directory.CreateDirectory(SentinelArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(SentinelPrefabPath));
            AssetDatabase.Refresh();

            var settings = AssetDatabase.LoadAssetAtPath<SentinelSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SentinelSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var sprites = new SentinelSprites
            {
                Idle = MakeSentinelSprite("enm_sentinel_idle", Pose.Idle),
                Walk1 = MakeSentinelSprite("enm_sentinel_walk_1", Pose.WalkA),
                Walk2 = MakeSentinelSprite("enm_sentinel_walk_2", Pose.WalkB),
                Windup = MakeSentinelSprite("enm_sentinel_windup", Pose.Windup),
                Attack = MakeSentinelSprite("enm_sentinel_attack", Pose.Attack),
            };

            var root = new GameObject("Sentinel") { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = settings.GravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = settings.BodySize;
            box.offset = new Vector2(0f, settings.BodySize.y * 0.5f);
            box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var perception = root.AddComponent<EnemyPerception>();
            var ai = root.AddComponent<EnemyAI>();
            PlayerSetup.SetMask(perception, "occluders",
                GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);

            var visualGo = Child(root, "Visual", Vector3.zero);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprites.Idle;
            sr.sortingOrder = 4;

            var icon = DamagePopup.CreateText(Child(root, "AlertIcon", new Vector3(0f, 1.85f, 0f)), "", Color.yellow, 90);
            icon.fontStyle = FontStyle.Bold;
            var label = DamagePopup.CreateText(Child(root, "DebugLabel", new Vector3(0f, 2.75f, 0f)), "", new Color(1f, 1f, 1f, 0.8f), 24);

            var so = new SerializedObject(ai);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("idle").objectReferenceValue = sprites.Idle;
            so.FindProperty("windup").objectReferenceValue = sprites.Windup;
            so.FindProperty("attack").objectReferenceValue = sprites.Attack;
            so.FindProperty("alertIcon").objectReferenceValue = icon;
            so.FindProperty("debugLabel").objectReferenceValue = label;
            so.FindProperty("groundLayers").intValue = LayerMask.GetMask(
                GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            so.FindProperty("playerLayers").intValue = LayerMask.GetMask(GameConstants.Layers.Player);
            var walk = so.FindProperty("walk");
            walk.arraySize = 2;
            walk.GetArrayElementAtIndex(0).objectReferenceValue = sprites.Walk1;
            walk.GetArrayElementAtIndex(1).objectReferenceValue = sprites.Walk2;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SentinelPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject Child(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        // ------------------------------------------------------------- cena

        /// <summary>
        /// Trecho H da Playground (prefabs e zonas; os tiles ficam no SeteVidasSetup.CreatePlayground):
        /// Sentinela patrulhando x 133–140 e duas zonas de sombra.
        /// </summary>
        public static void PlaceStealthSection(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SentinelPrefabPath);
            if (prefab == null) prefab = CreateSentinelPrefab();

            var parent = new GameObject("StealthSection");
            SceneManager.MoveGameObjectToScene(parent, scene);

            var sentinel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            sentinel.name = "Sentinela (patrulha 133–140)";
            sentinel.transform.position = new Vector3(136.5f, 0f, 0f);
            var ai = sentinel.GetComponent<EnemyAI>();
            var so = new SerializedObject(ai);
            var points = so.FindProperty("patrolPoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).floatValue = 133f;
            points.GetArrayElementAtIndex(1).floatValue = 140f;
            so.FindProperty("startFacing").intValue = -1;
            so.FindProperty("respawnDelay").floatValue = 6f; // na Playground ela volta para treinar de novo
            so.ApplyModifiedPropertiesWithoutUndo();

            var shade = LoadShadowSprite();
            CreateShadow(parent.transform, "Sombra A (x 122–130)", new Vector2(126f, 2.5f), new Vector2(8f, 5f), 0.3f, shade);
            CreateShadow(parent.transform, "Sombra B (x 144–152)", new Vector2(148f, 2.5f), new Vector2(8f, 5f), 0.3f, shade);

            // Matinhos: agachado dentro, o gato some; de pé, aparece. Um no meio da patrulha (a Sentinela passa
            // ao lado sem ver e o gato sai atrás dela) e um na área livre antes dos bonecos, para testar.
            var bush = AssetDatabase.LoadAssetAtPath<GameObject>(BushPrefabPath);
            if (bush == null) bush = CreateBushPrefab();
            PlaceBush(bush, parent.transform, "Matinho (meio da patrulha)", 138f);
            PlaceBush(bush, parent.transform, "Matinho (teste, área livre)", 106f);
        }

        static void PlaceBush(GameObject prefab, Transform parent, string name, float x)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = new Vector3(x, 0f, 0f);
        }

        /// <summary>
        /// Matinho: esconderijo de 2 tiles de largura com 15 px de altura. Desenhado por cima do gato:
        /// agachado (14 px) ele some atrás das folhas; de pé (22 px) a cabeça aparece.
        /// </summary>
        public static GameObject CreateBushPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BushPrefabPath));
            AssetDatabase.Refresh();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BushSpritePath);
            if (sprite == null) sprite = MakeBushSprite();

            var root = new GameObject("Matinho");
            var spot = root.AddComponent<HidingSpot>();
            var so = new SerializedObject(spot);
            so.FindProperty("size").vector2Value = new Vector2(2f, 0.95f);
            so.ApplyModifiedPropertiesWithoutUndo();

            var visual = Child(root, "Visual", new Vector3(0f, 0.5f, 0f)); // sprite com pivot central, 1 tile de altura
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 7; // na frente do gato (5) e do corte (6), atrás da sombra (8)

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BushPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Moita 32×16 px: quatro tufos sobrepostos, contorno escuro, brilho no alto e pontas secas.</summary>
        static Sprite MakeBushSprite()
        {
            const int w = 32, h = 16;
            var outline = new Color32(0x24, 0x38, 0x22, 0xff);
            var leaf = new Color32(0x45, 0x66, 0x33, 0xff);
            var light = new Color32(0x6d, 0x8f, 0x45, 0xff);
            var dark = new Color32(0x33, 0x4f, 0x2a, 0xff);
            var dry = new Color32(0x9a, 0x92, 0x4a, 0xff);
            // Tufos: centro x, centro y, raio. Os do meio passam de 14 px para cobrir o gato agachado.
            var tufts = new[] { new Vector3(5f, 6f, 6f), new Vector3(12.5f, 8f, 7.5f), new Vector3(20f, 8f, 7.5f), new Vector3(27f, 6f, 5.5f) };

            bool Inside(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return false;
                foreach (var t in tufts)
                    if ((x - t.x) * (x - t.x) + (y - t.y) * (y - t.y) <= t.z * t.z) return true;
                return false;
            }

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!Inside(x, y)) continue;
                bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y + 1) || (y > 0 && !Inside(x, y - 1));
                Color32 c = leaf;
                if (edge) c = outline;
                else if (!Inside(x, y + 2) && (x % 3 != 0)) c = light;     // brilho logo abaixo do contorno de cima
                else if ((x * 7 + y * 3) % 11 == 0) c = dark;               // folhas na sombra
                px[y * w + x] = c;
            }
            // Pontas secas saindo do topo (reino em decadência)
            foreach (var (x, y) in new[] { (9, 15), (16, 15), (23, 15), (3, 12) })
                if (y < h && !Inside(x, y + 1)) px[y * w + x] = dry;

            return SavePng(BushSpritePath, w, h, px);
        }

        public static LightZone CreateShadow(Transform parent, string name, Vector2 center, Vector2 size, float exposure, Sprite shadeSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var zone = go.AddComponent<LightZone>();

            var shadeGo = new GameObject("Shade");
            shadeGo.transform.SetParent(go.transform, false);
            var sr = shadeGo.AddComponent<SpriteRenderer>();
            sr.sprite = shadeSprite;
            sr.color = new Color(0.04f, 0.03f, 0.1f, 0.5f);
            sr.sortingOrder = 8; // por cima do gato: ele escurece ao entrar na sombra

            var so = new SerializedObject(zone);
            so.FindProperty("shade").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
            zone.Configure(exposure, size);
            return zone;
        }

        static Sprite LoadShadowSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
            if (sprite != null) return sprite;
            const int s = GameConstants.TileSize; // 16×16 branco = 1×1 tile; a cor vem do SpriteRenderer
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            return SavePng(ShadowSpritePath, s, s, px);
        }

        // ----------------------------------------------------------- sprites

        enum Pose { Idle, WalkA, WalkB, Windup, Attack }

        /// <summary>
        /// Sentinela placeholder: cão de armadura com 26 px de altura num canvas de 32×28 (pivot nos pés),
        /// olhando para a direita. Tabardo vermelho (inimigo), olho claro do lado da frente e lança.
        /// A preparação recua a lança (aviso visível); o golpe a estica para a frente.
        /// </summary>
        static Sprite MakeSentinelSprite(string name, Pose pose)
        {
            string path = $"{SentinelArtFolder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 32, h = 28;
            var armor = new Color32(0x8c, 0x93, 0xa8, 0xff);
            var outline = new Color32(0x23, 0x26, 0x2f, 0xff);
            var tabard = new Color32(0xa3, 0x3a, 0x3a, 0xff);
            var eye = new Color32(0xff, 0xe0, 0x8a, 0xff);
            var shaft = new Color32(0x6b, 0x4a, 0x2b, 0xff);
            var tip = new Color32(0xe6, 0xeb, 0xf2, 0xff);
            var px = new Color32[w * h];

            void Set(int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }
            void Rect(int x0, int y0, int x1, int y1, Color32 fill)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, x == x0 || x == x1 || y == y0 || y == y1 ? outline : fill);
            }

            // Pernas
            int[] legs = pose switch
            {
                Pose.WalkA => new[] { 11, 12, 18, 19 },
                Pose.WalkB => new[] { 13, 14, 16, 17 },
                _ => new[] { 12, 13, 17, 18 },
            };
            foreach (int x in legs) { Set(x, 0, outline); Set(x, 1, outline); }

            Rect(10, 2, 21, 17, armor);                  // corpo
            for (int y = 4; y <= 14; y++)                 // tabardo
            for (int x = 13; x <= 18; x++) Set(x, y, tabard);
            Rect(11, 17, 21, 25, armor);                  // cabeça/elmo
            for (int x = 11; x <= 21; x++) Set(x, 22, outline); // fenda do elmo
            Set(19, 21, eye); Set(20, 21, eye);           // olho (frente = direita)
            Rect(21, 18, 23, 20, armor);                  // focinho
            for (int y = 15; y <= 21; y++) Set(9, y, outline); // orelha caída (atrás)
            Set(10, 15, outline);

            switch (pose)
            {
                case Pose.Windup: // lança recuada na altura do peito, ponta junto ao corpo
                    for (int x = 1; x <= 20; x++) Set(x, 12, shaft);
                    Set(21, 12, tip); Set(22, 12, tip); Set(21, 13, tip); Set(21, 11, tip);
                    break;
                case Pose.Attack: // estocada: lança esticada para a frente
                    for (int x = 12; x <= 28; x++) Set(x, 12, shaft);
                    Set(29, 12, tip); Set(30, 12, tip); Set(31, 12, tip); Set(29, 13, tip); Set(29, 11, tip);
                    break;
                default: // em pé, lança vertical na frente
                    for (int y = 1; y <= 24; y++) Set(23, y, shaft);
                    Set(23, 25, tip); Set(23, 26, tip); Set(23, 27, tip); Set(22, 25, tip); Set(24, 25, tip);
                    break;
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
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // PixelArtImportPostprocessor
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
