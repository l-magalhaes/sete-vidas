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
    /// Tela inicial (melhoria 12: no tema): fundo vivo do pôr do sol com a Cidadela, o gato de costas no penhasco, o logo
    /// com as sete centelhas, Continuar / Novo jogo / Opções / Área de testes / Sair em pergaminho, cursor de patinha e a
    /// confirmação de apagar o progresso. Spec: Docs/Design/tela-inicial.md.
    /// </summary>
    public static class MenuSetup
    {
        const string Art = "Assets/_Game/Art/UI/Menu";
        const string BoxSprite = "Assets/_Game/Art/UI/Dialogue/ui_dialog_box.png";
        const string ArrowSprite = "Assets/_Game/Art/UI/Dialogue/ui_dialog_next.png";
        const float UiPixelScale = 100f / 16f;

        static readonly Color32 Ink = new Color32(0x3b, 0x2a, 0x1e, 0xff);
        static readonly Color32 Gold = new Color32(0xf2, 0xd0, 0x4b, 0xff);
        static readonly Color32 Light = new Color32(0xe9, 0xdc, 0xb8, 0xff);

        // fundo vivo (melhoria 12): as camadas que já existem na fase
        const string SkyArt = "Assets/_Game/Art/Environments/Outdoors/env_sky_sunset.png";
        const string MountainsArt = "Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_cordilheira_a.png";
        const string ForestArt = "Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_floresta_vale.png";

        [MenuItem("Tools/Sete Vidas/12. Tela inicial (Novo jogo / Continuar)", priority = 48)]
        public static void CreateMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Path.GetDirectoryName(GameConstants.MainMenuScenePath));
            AssetDatabase.Refresh();

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
            var fx = canvasGo.AddComponent<TitleScreenFx>();

            // Fundo vivo: céu do pôr do sol, a cordilheira com a Cidadela pequena e a floresta do vale, andando devagar
            Img(root, "Céu", AssetDatabase.LoadAssetAtPath<Sprite>(SkyArt), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 270f)).raycastTarget = false;
            // a mesma vista do alto da montanha (conjunto A da fase): a Cidadela em silhueta na frente do sol poente
            var below = Img(root, "Base da cordilheira", null, new Vector2(0f, 0f), Vector2.zero, new Vector2(480f, 4f));
            below.color = new Color32(0x3a, 0x22, 0x47, 0xff);   // o preenchimento abaixo da camada (preencher_abaixo)
            below.raycastTarget = false;
            below.rectTransform.pivot = Vector2.zero;
            var mountains = Strip(root, "Cordilheira com a Cidadela", AssetDatabase.LoadAssetAtPath<Sprite>(MountainsArt), 960f, 200f, 3f);
            var forest = Strip(root, "Floresta do vale", AssetDatabase.LoadAssetAtPath<Sprite>(ForestArt), 960f, 150f, -28f);

            // Primeiro plano: o gato sentado na beira do penhasco, de costas, olhando a Cidadela; a cauda balança
            var cliff = CliffSprite();
            Img(root, "Penhasco", cliff, new Vector2(0f, 0f), Vector2.zero, new Vector2(cliff.rect.width, cliff.rect.height)).raycastTarget = false;
            var catFrames = CatBackSprites();
            var cat = Img(root, "Gato de costas", catFrames[0], new Vector2(0f, 0f), new Vector2(118f, CliffTop - 2f),
                new Vector2(catFrames[0].rect.width, catFrames[0].rect.height));
            cat.raycastTarget = false;

            // Logo em pixel art com as sete centelhas em volta (uma se apaga de vez em quando: a maldição)
            var logo = LogoSprite();
            Img(root, "Logo", logo, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(logo.rect.width, logo.rect.height)).raycastTarget = false;
            var spark = SparkSprite();
            var sparks = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                // num arco por cima do logo, da esquerda para a direita
                float a = Mathf.Lerp(200f, -20f, i / 6f) * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Round(Mathf.Cos(a) * 104f), Mathf.Round(-52f + Mathf.Sin(a) * 34f));
                sparks[i] = Img(root, $"Centelha {i + 1}", spark, new Vector2(0.5f, 1f), p, new Vector2(5f, 5f));
                sparks[i].raycastTarget = false;
            }

            // Botões em pergaminho, à direita
            const float bx = 142f, bw = 116f;
            var cont = MakeButton(root, "Continuar", new Vector2(bx, 8f), bw);
            var info = Label(root, "Resumo do save", "", 8, Light, new Vector2(0.5f, 0.5f), new Vector2(bx, -7f), new Vector2(240f, 10f));
            info.gameObject.AddComponent<Shadow>().effectColor = new Color32(0x1a, 0x10, 0x20, 0xff);
            var newGame = MakeButton(root, "Novo jogo", new Vector2(bx, -24f), bw);
            var options = MakeButton(root, "Dicas: ligadas", new Vector2(bx, -48f), bw);
            var test = MakeButton(root, "Área de testes", new Vector2(bx, -72f), bw);
            var quit = MakeButton(root, "Sair", new Vector2(bx, -96f), bw);
            var cursor = Img(root, "Patinha", PawSprite(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7f, 7f));
            cursor.raycastTarget = false;

            // Confirmação: apagar o progresso
            var panel = Img(root, "Confirmação", AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(260f, 84f));
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = UiPixelScale;
            Label(panel.rectTransform, "Pergunta", "Começar um jogo novo?\nO progresso salvo será apagado.", 10, Ink,
                new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 30f));
            var yes = MakeButton(panel.rectTransform, "Sim, apagar", new Vector2(-62f, -20f), 110f);
            var no = MakeButton(panel.rectTransform, "Não", new Vector2(62f, -20f), 110f);

            cursor.transform.SetAsLastSibling(); // patinha por cima da caixa de confirmação

            // a tela entra a partir do preto
            var fade = Img(root, "Escuro da entrada", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 270f));
            fade.color = Color.black;
            fade.raycastTarget = false;
            fade.transform.SetAsLastSibling();

            var menu = canvasGo.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("newGameSceneName").stringValue = Path.GetFileNameWithoutExtension(GameConstants.FortressScenePath);
            so.FindProperty("testScene").stringValue = Path.GetFileNameWithoutExtension(GameConstants.PlaygroundScenePath);
            so.FindProperty("testAreaButton").objectReferenceValue = test;
            so.FindProperty("newGameButton").objectReferenceValue = newGame;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("optionsButton").objectReferenceValue = options;
            so.FindProperty("optionsLabel").objectReferenceValue = options.GetComponentInChildren<Text>();
            so.FindProperty("continueInfo").objectReferenceValue = info;
            so.FindProperty("confirmPanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("confirmYes").objectReferenceValue = yes;
            so.FindProperty("confirmNo").objectReferenceValue = no;
            so.FindProperty("cursor").objectReferenceValue = cursor.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var fso = new SerializedObject(fx);
            var layers = fso.FindProperty("layers");
            layers.arraySize = 2;
            SetLayer(layers.GetArrayElementAtIndex(0), mountains, 960f, 0.5f, 470f);   // longe: bem devagar; a Cidadela começa na frente do sol
            SetLayer(layers.GetArrayElementAtIndex(1), forest, 960f, 1.5f, 242f);    // perto: um pouco mais rápido
            fso.FindProperty("cat").objectReferenceValue = cat;
            var frames = fso.FindProperty("catFrames");
            frames.arraySize = catFrames.Length;
            for (int i = 0; i < catFrames.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = catFrames[i];
            var sp = fso.FindProperty("sparks");
            sp.arraySize = sparks.Length;
            for (int i = 0; i < sparks.Length; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = sparks[i];
            fso.FindProperty("fade").objectReferenceValue = fade;
            fso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GameConstants.MainMenuScenePath);
            SeteVidasSetup.RegisterScenesInBuild();
            Debug.Log("[Sete Vidas] Tela inicial criada (Scenes/MainMenu) e colocada como primeira cena da build. Abra-a e aperte Play.");
        }

        /// <summary>Uma camada do fundo que anda: duas cópias lado a lado, presas embaixo à esquerda.</summary>
        static (RectTransform a, RectTransform b) Strip(RectTransform root, string name, Sprite sprite, float w, float h, float y)
        {
            var a = Img(root, name + " (1)", sprite, new Vector2(0f, 0f), new Vector2(0f, y), new Vector2(w, h));
            var b = Img(root, name + " (2)", sprite, new Vector2(0f, 0f), new Vector2(w, y), new Vector2(w, h));
            a.raycastTarget = b.raycastTarget = false;
            a.rectTransform.pivot = b.rectTransform.pivot = Vector2.zero;
            return (a.rectTransform, b.rectTransform);
        }

        static void SetLayer(SerializedProperty p, (RectTransform a, RectTransform b) strip, float width, float speed, float start)
        {
            p.FindPropertyRelative("A").objectReferenceValue = strip.a;
            p.FindPropertyRelative("B").objectReferenceValue = strip.b;
            p.FindPropertyRelative("Width").floatValue = width;
            p.FindPropertyRelative("Speed").floatValue = speed;
            p.FindPropertyRelative("Start").floatValue = start;
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

        const float CliffTop = 80f;   // altura do topo do penhasco na tela (px)

        delegate void Painter(System.Action<int, int, Color32> set);

        /// <summary>Grava (ou reaproveita) um PNG desenhado em código, com a importação de pixel art.</summary>
        static Sprite Draw(string name, int w, int h, Painter paint)
        {
            string path = $"{Art}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var px = new Color32[w * h];
            paint((x, y, c) => { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; });
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Mapa de caracteres (linha de cima primeiro) com contorno automático de 1 px nos 4 vizinhos.</summary>
        static void PaintMap(System.Action<int, int, Color32> set, string[] map, int w, int h, System.Func<char, Color32> color, Color32 outline)
        {
            bool On(int x, int y) => y >= 0 && y < map.Length && x >= 0 && x < map[y].Length && map[y][x] != '.';
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int my = y, mx = x;
                    Color32? c = null;
                    if (On(mx, my)) c = color(map[my][mx]);
                    else if (On(mx - 1, my) || On(mx + 1, my) || On(mx, my - 1) || On(mx, my + 1)) c = outline;
                    if (c.HasValue) set(x, h - 1 - y, c.Value);   // a textura cresce para cima
                }
        }

        static readonly Color32 CatOutline = new Color32(0x1a, 0x1c, 0x26, 0xff);
        static Color32 CatColor(char k) => k switch
        {
            'L' => new Color32(0xf4, 0xd6, 0xa3, 0xff), 'F' => new Color32(0xe3, 0xb7, 0x7d, 0xff), 'f' => new Color32(0xb9, 0x85, 0x52, 0xff),
            'G' => new Color32(0x6a, 0x70, 0x80, 0xff), 'H' => new Color32(0x4a, 0x4f, 0x5c, 0xff), 'h' => new Color32(0x33, 0x36, 0x3f, 0xff),
            'T' => new Color32(0x2b, 0x3a, 0x67, 0xff), 't' => new Color32(0x1c, 0x26, 0x45, 0xff),
            _ => new Color32(0, 0, 0, 0),
        };

        // o gato sentado de costas: capuz da Guilda com as orelhas para fora, a capa com a faixa do Ladino, as ancas
        static readonly string[] CatBack =
        {
            "......................",
            "......L.....L.........",
            "......LF...FL.........",
            "......FHHHHHF.........",
            ".....HHGGGGGHH........",
            ".....HGGGGGGGH........",
            ".....HGGGGGGGH........",
            ".....HHGGGGGHH........",
            "......hHHHHHh.........",
            "....HHHHhHHhHHHH......",
            "...HHHGHhHHhHGHHH.....",
            "...HHHGHhHHhHGHHH.....",
            "...HHHHHhHHhHHHHH.....",
            "..HHHHHHhHHhHHHHHH....",
            "..HHHHHHhHHhHHHHHH....",
            "..TTTTTTTTTTTTTTTTT...",
            "..ttttttttttttttttt...",
            "..FFFFFf.....fFFFFFF..",
            "...fff.........fff....",
        };
        // a cauda em 3 posições (x, y no mapa acima): levantada, deitada, mais alta
        static readonly int[][] CatTails =
        {
            new[] { 18, 16, 19, 16, 20, 15, 21, 14, 21, 13, 22, 12, 22, 11 },
            new[] { 18, 16, 19, 17, 20, 17, 21, 17, 22, 16, 23, 16, 24, 15 },
            new[] { 18, 16, 19, 15, 20, 14, 20, 13, 21, 12, 21, 11, 22, 10 },
        };

        static Sprite[] CatBackSprites()
        {
            const int w = 28, h = 21;
            var frames = new Sprite[CatTails.Length];
            for (int f = 0; f < CatTails.Length; f++)
            {
                var map = new char[h][];
                for (int y = 0; y < h; y++) { map[y] = new char[w]; for (int x = 0; x < w; x++) map[y][x] = y < CatBack.Length && x < CatBack[y].Length ? CatBack[y][x] : '.'; }
                var t = CatTails[f];
                for (int i = 0; i < t.Length; i += 2)
                {
                    bool tip = i == t.Length - 2;
                    map[t[i + 1]][t[i]] = tip ? 'L' : 'F';
                    if (!tip && map[t[i + 1]][t[i] + 1] == '.') map[t[i + 1]][t[i] + 1] = 'f';   // a cauda com 2 px de grossura
                }
                var rows = new string[h];
                for (int y = 0; y < h; y++) rows[y] = new string(map[y]);
                frames[f] = Draw("ui_menu_gato_costas_" + f, w, h, set => PaintMap(set, rows, w, h, CatColor, CatOutline));
            }
            return frames;
        }

        /// <summary>Penhasco em primeiro plano, à esquerda: rocha quase preta com a borda de cima iluminada pelo pôr do sol.</summary>
        static Sprite CliffSprite() => Draw("ui_menu_penhasco", 200, (int)CliffTop + 2, set =>
        {
            var dark = new Color32(0x14, 0x10, 0x1a, 0xff);
            var rock = new Color32(0x1e, 0x18, 0x26, 0xff);
            var rim = new Color32(0x57, 0x4c, 0x62, 0xff);
            var warm = new Color32(0x8a, 0x5a, 0x4a, 0xff);
            int top = (int)CliffTop;
            for (int x = 0; x < 200; x++)
            {
                // topo reto onde o gato senta (x 100..160), depois a beira cai em degraus irregulares
                int edge = x < 160 ? top - (x < 20 ? (20 - x) / 6 : 0) + ((x * 7) % 5 == 0 ? -1 : 0)
                                   : top - (x - 160) * (x - 160) / 22 - ((x * 13) % 4);
                for (int y = 0; y <= edge && y < top + 2; y++)
                {
                    Color32 c = y >= edge - 1 ? (x > 90 && x < 170 ? warm : rim) : (((x / 6 + y / 5) % 4 == 0) ? dark : rock);
                    set(x, y, c);
                }
            }
        });

        // fonte 5×7 do logo (só as letras de SETE VIDAS)
        static readonly System.Collections.Generic.Dictionary<char, string[]> Font5x7 = new System.Collections.Generic.Dictionary<char, string[]>
        {
            { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
            { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
            { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'V', new[] { "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.." } },
            { 'I', new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
            { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
            { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        };

        /// <summary>Logo "SETE VIDAS": letras de 5×7 em blocos de 3 px, dourado com brilho em cima, contorno e sombra.</summary>
        static Sprite LogoSprite()
        {
            const string text = "SETE VIDAS";
            const int scale = 3;
            // monta a grade de 1 px por "pixel" da fonte
            int width = 0;
            foreach (char ch in text) width += (ch == ' ' ? 3 : Font5x7[ch][0].Length) + 1;
            var grid = new bool[7, width];
            int cx = 0;
            foreach (char ch in text)
            {
                if (ch == ' ') { cx += 4; continue; }
                var g = Font5x7[ch];
                for (int y = 0; y < 7; y++) for (int x = 0; x < g[y].Length; x++) if (g[y][x] == '#') grid[y, cx + x] = true;
                cx += g[0].Length + 1;
            }
            int w = width * scale + 4, h = 7 * scale + 5;
            return Draw("ui_menu_logo", w, h, set =>
            {
                var gold = new Color32(0xf2, 0xd0, 0x4b, 0xff);
                var light = new Color32(0xff, 0xf4, 0xc2, 0xff);
                var shade = new Color32(0xc8, 0x96, 0x2a, 0xff);
                var outline = new Color32(0x2a, 0x10, 0x30, 0xff);
                bool Fill(int px, int py)
                {
                    int gx = (px - 2) / scale, gy = (py - 2) / scale;
                    return px >= 2 && py >= 2 && gx < width && gy < 7 && grid[gy, gx];
                }
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                    {
                        Color32? c = null;
                        if (Fill(px, py))
                        {
                            int ry = (py - 2) % (scale * 7) / scale;     // linha da fonte
                            c = ry <= 1 ? light : ry >= 5 ? shade : gold;
                        }
                        else if (Fill(px - 1, py) || Fill(px + 1, py) || Fill(px, py - 1) || Fill(px, py + 1) || Fill(px - 1, py - 1) || Fill(px - 2, py - 2))
                            c = outline;                                  // contorno + sombra para baixo à direita
                        if (c.HasValue) set(px, h - 1 - py, c.Value);
                    }
            });
        }

        /// <summary>Centelha 5×5: miolo branco e braços dourados (uma das sete vidas).</summary>
        static Sprite SparkSprite() => Draw("ui_menu_centelha", 5, 5, set =>
        {
            var gold = new Color32(0xf2, 0xd0, 0x4b, 0xff);
            var white = new Color32(0xff, 0xfa, 0xe8, 0xff);
            set(2, 2, white);
            set(1, 2, gold); set(3, 2, gold); set(2, 1, gold); set(2, 3, gold);
            set(0, 2, new Color32(0xf2, 0xd0, 0x4b, 0x80)); set(4, 2, new Color32(0xf2, 0xd0, 0x4b, 0x80));
            set(2, 0, new Color32(0xf2, 0xd0, 0x4b, 0x80)); set(2, 4, new Color32(0xf2, 0xd0, 0x4b, 0x80));
        });

        /// <summary>Cursor em forma de patinha 7×7: almofada grande e quatro dedos, dourado com contorno escuro.</summary>
        static Sprite PawSprite() => Draw("ui_menu_patinha", 7, 7, set =>
        {
            string[] map = { ".#...#.", "#.#.#.#", ".#...#.", "..###..", ".#####.", ".#####.", "..###.." };
            var gold = new Color32(0xf2, 0xd0, 0x4b, 0xff);
            for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++) if (map[y][x] == '#') set(x, 6 - y, gold);
        });
    }
}
