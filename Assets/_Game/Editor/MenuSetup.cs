using System.IO;
using SeteVidas.Core;
using SeteVidas.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Tela inicial: cena MainMenu (primeira da build) com fundo, título, Novo jogo / Continuar / Sair e a
    /// confirmação de apagar o progresso. Spec: Docs/Design/tela-inicial.md.
    /// </summary>
    public static class MenuSetup
    {
        const string Art = "Assets/_Game/Art/UI/Menu";
        const string BoxSprite = "Assets/_Game/Art/UI/Dialogue/ui_dialog_box.png";
        const string ArrowSprite = "Assets/_Game/Art/UI/Dialogue/ui_dialog_next.png";
        const string HeartSprite = "Assets/_Game/Art/UI/Hearts/ui_heart_full.png";
        const string CatSprite = "Assets/_Game/Art/Characters/Cat/Placeholder/chr_cat_placeholder_idle.png";
        const float UiPixelScale = 100f / 16f;

        static readonly Color32 Ink = new Color32(0x3b, 0x2a, 0x1e, 0xff);
        static readonly Color32 Gold = new Color32(0xf2, 0xd0, 0x4b, 0xff);
        static readonly Color32 Light = new Color32(0xe9, 0xdc, 0xb8, 0xff);

        [MenuItem("Tools/Sete Vidas/12. Tela inicial (Novo jogo / Continuar)", priority = 48)]
        public static void CreateMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Path.GetDirectoryName(GameConstants.MainMenuScenePath));
            AssetDatabase.Refresh();
            var background = BackgroundSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x14, 0x12, 0x1e, 0xff);
            camGo.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions(); // teclado (setas/WASD, Enter), controle e mouse

            var canvasGo = new GameObject("Menu", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameConstants.ReferenceWidth, GameConstants.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Fundo 480×270 (escala inteira) e o gato olhando para a fortaleza
            var bg = Img(root, "Fundo", background, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 270f));
            bg.raycastTarget = false;
            var cat = Img(root, "Gato", AssetDatabase.LoadAssetAtPath<Sprite>(CatSprite), new Vector2(0.5f, 0f), new Vector2(-150f, 20f), new Vector2(48f, 72f));
            cat.raycastTarget = false;

            // Título e as sete vidas
            var title = Label(root, "Título", "SETE VIDAS", 28, Gold, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(300f, 34f));
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<Shadow>().effectColor = new Color32(0x2a, 0x10, 0x30, 0xff);
            var heart = AssetDatabase.LoadAssetAtPath<Sprite>(HeartSprite);
            for (int i = 0; i < 7; i++)
                Img(root, $"Vida {i + 1}", heart, new Vector2(0.5f, 1f), new Vector2(-54f + i * 18f, -64f), new Vector2(16f, 16f)).raycastTarget = false;

            // Botões
            var newGame = MakeButton(root, "Novo jogo", new Vector2(70f, 10f));
            var cont = MakeButton(root, "Continuar", new Vector2(70f, -16f));
            var info = Label(root, "Resumo do save", "", 8, Light, new Vector2(0.5f, 0.5f), new Vector2(70f, -33f), new Vector2(240f, 12f));
            var quit = MakeButton(root, "Sair", new Vector2(70f, -52f));
            var cursor = Img(root, "Seta", AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSprite), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 7f));
            cursor.color = Gold;
            cursor.raycastTarget = false;

            // Confirmação: apagar o progresso
            var panel = Img(root, "Confirmação", AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(260f, 84f));
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = UiPixelScale;
            Label(panel.rectTransform, "Pergunta", "Começar um jogo novo?\nO progresso salvo será apagado.", 10, Ink,
                new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 30f));
            var yes = MakeButton(panel.rectTransform, "Sim, apagar", new Vector2(-62f, -20f), 110f);
            var no = MakeButton(panel.rectTransform, "Não", new Vector2(62f, -20f), 110f);

            cursor.transform.SetAsLastSibling(); // seta por cima da caixa de confirmação

            var menu = canvasGo.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("newGameScene").stringValue = Path.GetFileNameWithoutExtension(GameConstants.PlaygroundScenePath);
            so.FindProperty("newGameButton").objectReferenceValue = newGame;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("continueInfo").objectReferenceValue = info;
            so.FindProperty("confirmPanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("confirmYes").objectReferenceValue = yes;
            so.FindProperty("confirmNo").objectReferenceValue = no;
            so.FindProperty("cursor").objectReferenceValue = cursor.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GameConstants.MainMenuScenePath);
            SeteVidasSetup.RegisterScenesInBuild();
            Debug.Log("[Sete Vidas] Tela inicial criada (Scenes/MainMenu) e colocada como primeira cena da build. Abra-a e aperte Play.");
        }

        // --------------------------------------------------------------- UI

        static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        static Text Label(Transform parent, string name, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(Transform parent, string text, Vector2 pos, float width = 130f)
        {
            var img = Img(parent, "Botão " + text, AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 20f));
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = UiPixelScale;
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.82f, 0.75f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(1f, 0.93f, 0.6f);
            colors.pressedColor = new Color(1f, 0.8f, 0.4f);
            colors.disabledColor = new Color(0.45f, 0.43f, 0.45f, 0.8f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            img.gameObject.AddComponent<SelectOnHover>();
            var label = Label(img.rectTransform, "Texto", text, 10, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 20f));
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        // ------------------------------------------------------------- arte

        /// <summary>
        /// Fundo 480×270: céu noturno em degradê com estrelas e lua, a Fortaleza dos Paladinos em silhueta e um
        /// brilho roxo-esverdeado da praga saindo da torre (onde fica o tomo).
        /// </summary>
        static Sprite BackgroundSprite()
        {
            string path = $"{Art}/ui_menu_background.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 480, h = 270;
            var px = new Color32[w * h];
            var top = new Color32(0x0e, 0x0c, 0x18, 0xff);
            var bottom = new Color32(0x2a, 0x22, 0x40, 0xff);
            var star = new Color32(0xe9, 0xdc, 0xb8, 0xff);
            var moon = new Color32(0xf2, 0xe6, 0xc8, 0xff);
            var keep = new Color32(0x10, 0x0e, 0x18, 0xff);
            var window = new Color32(0xd9, 0xa4, 0x41, 0xff);
            var plague = new Color32(0x6b, 0x2a, 0x8a, 0xff);
            var plagueGreen = new Color32(0x8f, 0xd1, 0x4f, 0xff);

            // Céu em faixas (sem dithering suave: pixel art)
            for (int y = 0; y < h; y++)
            {
                float k = Mathf.Floor(y / 18f) * 18f / h;
                var c = Color32.Lerp(bottom, top, k);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            var rng = new System.Random(7);
            for (int i = 0; i < 90; i++) px[(120 + rng.Next(150)) * w + rng.Next(w)] = star;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float d = (x - 400f) * (x - 400f) + (y - 220f) * (y - 220f);
                if (d < 16f * 16f && (x - 407f) * (x - 407f) + (y - 225f) * (y - 225f) > 14f * 14f) px[y * w + x] = moon; // lua crescente
            }

            // Fortaleza: muralha, torres com ameias e a torre do tomo
            void Fill(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++) px[y * w + x] = c;
            }
            Fill(0, 0, w - 1, 22, keep);                    // chão
            Fill(180, 22, 470, 70, keep);                   // muralha
            for (int x = 180; x <= 470; x += 12) Fill(x, 70, x + 6, 76, keep); // ameias
            Fill(200, 22, 236, 120, keep);                  // torre esquerda
            for (int x = 200; x <= 236; x += 9) Fill(x, 120, x + 5, 127, keep);
            Fill(400, 22, 440, 110, keep);                  // torre direita
            for (int x = 400; x <= 440; x += 9) Fill(x, 110, x + 5, 117, keep);
            Fill(300, 22, 340, 165, keep);                  // torre do tomo
            for (int y = 165; y <= 190; y++) Fill(320 - (190 - y) * 20 / 25, y, 320 + (190 - y) * 20 / 25, y, keep); // telhado
            foreach (var (wx, wy) in new[] { (214, 90), (420, 80), (260, 45), (360, 50), (440, 45) }) Fill(wx, wy, wx + 3, wy + 5, window);
            // Brilho da praga na janela do tomo
            for (int y = 130; y <= 160; y++) for (int x = 305; x <= 335; x++)
            {
                float d = Mathf.Sqrt((x - 320f) * (x - 320f) + (y - 145f) * (y - 145f));
                if (d < 4f) px[y * w + x] = plagueGreen;
                else if (d < 12f && ((x + y) % 3 == 0)) px[y * w + x] = plague;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
