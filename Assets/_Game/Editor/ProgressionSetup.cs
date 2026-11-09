using System.IO;
using SeteVidas.Core;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 4: sprites dos corações e do santuário, prefabs HUD e Sanctuary, e os dois santuários
    /// da Playground. Também os comandos de save. Spec: Docs/Design/vida-morte-save.md.
    /// </summary>
    public static class ProgressionSetup
    {
        const string UiArtFolder = "Assets/_Game/Art/UI/Hearts";
        const string SanctuarySpritesFolder = "Assets/_Game/Art/Environments/Placeholder";
        public const string HudPrefabPath = "Assets/_Game/Prefabs/UI/HUD.prefab";
        public const string SanctuaryPrefabPath = "Assets/_Game/Prefabs/Interactables/Sanctuary.prefab";
        public const string HeartPickupPrefabPath = "Assets/_Game/Prefabs/Interactables/HalfHeartPickup.prefab";

        [MenuItem("Tools/Sete Vidas/7. Fase 4: vida, santuário e save", priority = 43)]
        public static void RunPhase4()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!PlayerSetup.CreatePlayerAssets()) return;   // gato com PlayerHealth
            CreateHudPrefab();
            CreateSanctuaryPrefab();
            CreateHeartPickupPrefab();
            EnemySetup.CreateSentinelPrefab();               // Sentinela com chance de soltar meio coração
            SeteVidasSetup.CreateScenes();                   // Playground com HUD e santuários em x 3 e x 119,5
            Debug.Log("[Sete Vidas] Fase 4 pronta: corações no HUD, santuários em x 3 e x 119,5 (E para descansar). " +
                      "Save em " + SaveManager.FilePath);
        }

        [MenuItem("Tools/Sete Vidas/Save/Apagar save", priority = 60)]
        public static void DeleteSave()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Sete Vidas] Pare o Play antes de apagar o save.");
                return;
            }
            if (!EditorUtility.DisplayDialog("Apagar save", $"Apagar {SaveManager.FilePath}?\nO próximo Play começa um jogo novo.", "Apagar", "Cancelar"))
                return;
            SaveManager.DeleteSave();
            Debug.Log("[Sete Vidas] Save apagado.");
        }

        [MenuItem("Tools/Sete Vidas/Save/Abrir pasta do save", priority = 61)]
        public static void RevealSave()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(File.Exists(SaveManager.FilePath) ? SaveManager.FilePath : Application.persistentDataPath);
        }

        // --------------------------------------------------------------- cena

        /// <summary>HUD e santuários da Playground (chamado ao recriar a cena).</summary>
        public static void PlaceInPlayground(Scene scene)
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hud == null) hud = CreateHudPrefab();
            var hudGo = (GameObject)PrefabUtility.InstantiatePrefab(hud, scene);
            hudGo.name = "HUD";

            var sanctuary = AssetDatabase.LoadAssetAtPath<GameObject>(SanctuaryPrefabPath);
            if (sanctuary == null) sanctuary = CreateSanctuaryPrefab();
            var parent = new GameObject("Sanctuaries");
            SceneManager.MoveGameObjectToScene(parent, scene);
            PlaceSanctuary(sanctuary, parent.transform, "playground_inicio", 3f);
            PlaceSanctuary(sanctuary, parent.transform, "playground_furtividade", 119.5f);
        }

        static void PlaceSanctuary(GameObject prefab, Transform parent, string id, float x)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"Santuário ({id})";
            go.transform.position = new Vector3(x, 0f, 0f);
            var so = new SerializedObject(go.GetComponent<Sanctuary>());
            so.FindProperty("sanctuaryId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- HUD

        public static GameObject CreateHudPrefab()
        {
            Directory.CreateDirectory(UiArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(HudPrefabPath));
            AssetDatabase.Refresh();
            var full = HeartSprite("ui_heart_full", HeartLook.Full);
            var half = HeartSprite("ui_heart_half", HeartLook.Half);
            var empty = HeartSprite("ui_heart_empty", HeartLook.Empty);
            var locked = HeartSprite("ui_heart_locked", HeartLook.Locked);

            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameConstants.ReferenceWidth, GameConstants.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // 1080p = ×4, como o jogo

            // Corações: canto superior esquerdo, 16×16 a cada 18 px
            var row = UiChild(root, "Hearts");
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(8f, -8f);
            row.sizeDelta = new Vector2(GameConstants.MaxHearts * 18f, 16f);
            var hud = root.AddComponent<HeartsHud>();
            var so = new SerializedObject(hud);
            var hearts = so.FindProperty("hearts");
            hearts.arraySize = GameConstants.MaxHearts;
            for (int i = 0; i < GameConstants.MaxHearts; i++)
            {
                var rt = UiChild(row.gameObject, $"Heart {i + 1}");
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 18f, 0f);
                rt.sizeDelta = new Vector2(16f, 16f);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = i == 0 ? full : locked;
                img.raycastTarget = false;
                hearts.GetArrayElementAtIndex(i).objectReferenceValue = img;
            }
            so.FindProperty("full").objectReferenceValue = full;
            so.FindProperty("half").objectReferenceValue = half;
            so.FindProperty("empty").objectReferenceValue = empty;
            so.FindProperty("locked").objectReferenceValue = locked;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Tela preta da morte, por cima de tudo
            var fadeRt = UiChild(root, "Fader");
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
            var fadeImg = fadeRt.gameObject.AddComponent<Image>();
            fadeImg.color = new Color(0f, 0f, 0f, 0f);
            fadeImg.raycastTarget = false;
            fadeImg.enabled = false;
            var fader = fadeRt.gameObject.AddComponent<ScreenFader>();
            var fso = new SerializedObject(fader);
            fso.FindProperty("image").objectReferenceValue = fadeImg;
            fso.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static RectTransform UiChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return (RectTransform)go.transform;
        }

        enum HeartLook { Full, Half, Empty, Locked }

        // Coração de 13×11 px centralizado num quadro de 16×16 (linha de cima primeiro)
        static readonly string[] HeartShape =
        {
            "..XXX...XXX..",
            ".XXXXX.XXXXX.",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            ".XXXXXXXXXXX.",
            "..XXXXXXXXX..",
            "...XXXXXXX...",
            "....XXXXX....",
            ".....XXX.....",
            "......X......",
        };

        /// <summary>
        /// Corações 16×16: cheio (vermelho com brilho), meio (metade esquerda cheia), vazio (interior escuro)
        /// e bloqueado (cinza com rachadura: coração ainda não recuperado).
        /// </summary>
        static Sprite HeartSprite(string name, HeartLook look)
        {
            string path = $"{UiArtFolder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var red = new Color32(0xd6, 0x30, 0x4a, 0xff);
            var shine = new Color32(0xff, 0x8a, 0x9a, 0xff);
            var outline = new Color32(0x3a, 0x0f, 0x18, 0xff);
            var hollow = new Color32(0x4a, 0x2a, 0x33, 0xff);
            var stone = new Color32(0x3b, 0x3b, 0x48, 0xff);
            var stoneEdge = new Color32(0x22, 0x22, 0x2a, 0xff);

            const int size = 16, offX = 1, offTop = 2;
            int rows = HeartShape.Length, cols = HeartShape[0].Length;
            bool Inside(int c, int r) => r >= 0 && r < rows && c >= 0 && c < cols && HeartShape[r][c] == 'X';

            var px = new Color32[size * size];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (!Inside(c, r)) continue;
                bool edge = !Inside(c - 1, r) || !Inside(c + 1, r) || !Inside(c, r - 1) || !Inside(c, r + 1);
                Color32 color = look switch
                {
                    HeartLook.Locked => edge ? stoneEdge : stone,
                    HeartLook.Empty => edge ? outline : hollow,
                    HeartLook.Half => edge ? outline : (c <= cols / 2 ? red : hollow),
                    _ => edge ? outline : red,
                };
                if (look != HeartLook.Locked && look != HeartLook.Empty && !edge && r == 1 && (c == 2 || c == 3)) color = shine;
                if (look == HeartLook.Locked && !edge && (c == 6 && r >= 2 && r <= 7 || c == 5 && r == 4 || c == 7 && r == 6)) color = stoneEdge; // rachadura
                int x = offX + c, y = size - 1 - (offTop + r);
                px[y * size + x] = color;
            }
            return SavePng(path, size, size, px);
        }

        // ------------------------------------------------------ meio coração

        /// <summary>
        /// Meio coração no chão: metade esquerda do coração do HUD (7×11 px) com brilho claro em volta,
        /// para ler como "pegue isto". Layer Interactable (só o gato encosta), colisor trigger.
        /// </summary>
        public static GameObject CreateHeartPickupPrefab()
        {
            Directory.CreateDirectory(UiArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(HeartPickupPrefabPath));
            AssetDatabase.Refresh();
            var sprite = HalfHeartPickupSprite();

            var root = new GameObject("HalfHeartPickup") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            col.offset = new Vector2(0f, 0.4f);

            var visual = new GameObject("Visual") { layer = root.layer };
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 7f / 16f, 0f); // sprite 9×14 com pivot central, base no chão
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;

            var pickup = root.AddComponent<HeartPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("groundLayers").intValue = LayerMask.GetMask(
                GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, HeartPickupPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Sprite HalfHeartPickupSprite()
        {
            string path = $"{UiArtFolder}/pickup_half_heart.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var red = new Color32(0xd6, 0x30, 0x4a, 0xff);
            var shine = new Color32(0xff, 0x8a, 0x9a, 0xff);
            var outline = new Color32(0x3a, 0x0f, 0x18, 0xff);
            var glow = new Color32(0xff, 0xd0, 0xd8, 0x90);

            const int w = 9, h = 14, cols = 7; // metade esquerda (colunas 0..6) + 1 px de brilho de cada lado
            int rows = HeartShape.Length;
            bool Inside(int c, int r) => r >= 0 && r < rows && c >= 0 && c < cols && HeartShape[r][c] == 'X';

            var px = new Color32[w * h];
            for (int r = -1; r <= rows; r++)
            for (int c = -1; c <= cols; c++)
            {
                int x = c + 1, y = h - 2 - (r + 1);
                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                if (Inside(c, r))
                {
                    bool edge = !Inside(c - 1, r) || !Inside(c + 1, r) || !Inside(c, r - 1) || !Inside(c, r + 1);
                    px[y * w + x] = edge ? outline : (r == 1 && (c == 2 || c == 3) ? shine : red);
                }
                else if (Inside(c - 1, r) || Inside(c + 1, r) || Inside(c, r - 1) || Inside(c, r + 1))
                    px[y * w + x] = glow; // brilho em volta
            }
            return SavePng(path, w, h, px);
        }

        // ---------------------------------------------------------- santuário

        public static GameObject CreateSanctuaryPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SanctuaryPrefabPath));
            AssetDatabase.Refresh();
            var unlit = SanctuarySprite("env_sanctuary_unlit", lit: false);
            var lit = SanctuarySprite("env_sanctuary_lit", lit: true);

            var root = new GameObject("Sanctuary") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.5f, 2f);
            box.offset = new Vector2(0f, 1f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f); // sprite 32×32 com pivot central
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = unlit;
            sr.sortingOrder = 2; // atrás do gato

            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(root.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: descansar", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);

            var sanctuary = root.AddComponent<Sanctuary>();
            var so = new SerializedObject(sanctuary);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("unlitSprite").objectReferenceValue = unlit;
            so.FindProperty("litSprite").objectReferenceValue = lit;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SanctuaryPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Santuário felino 32×32 px: pedestal de pedra, estátua de gato sentado e duas velas.
        /// Aceso (checkpoint ativo): chamas nas velas e olhos dourados na estátua.
        /// </summary>
        static Sprite SanctuarySprite(string name, bool lit)
        {
            string path = $"{SanctuarySpritesFolder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 32, h = 32;
            var stone = new Color32(0x5a, 0x55, 0x6e, 0xff);
            var outline = new Color32(0x2a, 0x26, 0x38, 0xff);
            var statue = new Color32(0x8a, 0x86, 0xa0, 0xff);
            var wax = new Color32(0xe8, 0xe0, 0xc8, 0xff);
            var flame = new Color32(0xff, 0xd3, 0x4d, 0xff);
            var flameCore = new Color32(0xff, 0x8a, 0x2b, 0xff);
            var eyeLit = new Color32(0xff, 0xd3, 0x4d, 0xff);
            var px = new Color32[w * h];

            void Set(int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }
            void Rect(int x0, int y0, int x1, int y1, Color32 fill)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, x == x0 || x == x1 || y == y0 || y == y1 ? outline : fill);
            }

            Rect(3, 0, 28, 5, stone);       // pedestal
            Rect(10, 6, 21, 19, statue);    // corpo sentado
            Rect(11, 19, 20, 27, statue);   // cabeça
            Set(12, 28, outline); Set(13, 28, outline); Set(12, 29, outline); // orelhas
            Set(19, 28, outline); Set(18, 28, outline); Set(19, 29, outline);
            for (int y = 7; y <= 14; y++) Set(22, y, outline); // cauda
            Set(23, 14, outline); Set(23, 15, outline);
            Color32 eye = lit ? eyeLit : outline;
            Set(13, 23, eye); Set(14, 23, eye); Set(17, 23, eye); Set(18, 23, eye);

            foreach (int cx in new[] { 5, 26 })   // velas
            {
                Rect(cx - 1, 6, cx + 1, 11, wax);
                Set(cx, 12, outline);              // pavio
                if (lit)
                {
                    Set(cx, 13, flameCore); Set(cx, 14, flame); Set(cx - 1, 13, flame); Set(cx + 1, 13, flame); Set(cx, 15, flame);
                }
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
