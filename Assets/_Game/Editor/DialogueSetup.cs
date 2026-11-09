using System.IO;
using SeteVidas.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Sistema de diálogo: arte da interface (caixa, placa do nome, moldura do retrato, seta), retratos
    /// placeholder, falantes, conversas do Golden e a interface dentro do prefab do HUD.
    /// Spec: Docs/Design/dialogo.md. Textos: Docs/Narrativa/dialogos-mvp.md.
    /// </summary>
    public static class DialogueSetup
    {
        const string UiArt = "Assets/_Game/Art/UI/Dialogue";
        const string PortraitArt = "Assets/_Game/Art/UI/Portraits";
        const string DataFolder = "Assets/_Game/ScriptableObjects/Dialogue";
        const float UiPixelScale = 100f / 16f; // sprites PPU 16 numa Canvas de referência 100: 1 pixel = 1 unidade

        [MenuItem("Tools/Sete Vidas/9. Diálogos (interface com retratos)", priority = 45)]
        public static void RunDialogueSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateDialogueAssets();
            ProgressionSetup.CreateHudPrefab();   // HUD com a interface de diálogo
            ClassSetup.CreateGoldenPrefab();      // Golden usando as conversas novas
            SeteVidasSetup.CreateScenes();
            Debug.Log("[Sete Vidas] Diálogos prontos: fale com o Golden (x 168,5, em cima da laje). Conversas em ScriptableObjects/Dialogue.");
        }

        // ------------------------------------------------------------- dados

        public struct GoldenDialogues
        {
            public DialogueDefinition Bond, Unlock;
            public DialogueDefinition[] Idle;
        }

        /// <summary>
        /// Cria os falantes e as conversas do Golden. Conversas existentes são preservadas, salvo quando o roteiro
        /// mudou (revisão maior no código). Textos de referência: Docs/Narrativa/dialogos-mvp.md.
        /// </summary>
        public static GoldenDialogues CreateDialogueAssets()
        {
            var (cat, golden) = Speakers();

            var result = new GoldenDialogues
            {
                // D-GOLDEN-01 (rev. 2): resgate depois da maldição e juramento de destruir o tomo
                Bond = Conversation("D-GOLDEN-01", true, 2,
                    (golden, "", "Respire, ladrãozinho. Seu último coração ainda bate... por pouco."),
                    (cat, "ironico", "Você... me salvou? Depois da surra que eu te dei?"),
                    (golden, "", "Quem aguenta o selo e ainda respira é forte. Força assim não se desperdiça."),
                    (golden, "", "Jure destruir esse tomo. Em troca, minha força é sua."),
                    (cat, "", "Juro. Não que eu tenha muita escolha.")),
                Unlock = Conversation("D-GOLDEN-01-SISTEMA", true, 1,
                    (null, "", "Paladino desbloqueado! Aperte 2 (ou Q/R) para trocar de classe.")),
                // D-GOLDEN-02 (rev. 2), em rodízio: lembram o juramento
                Idle = new[]
                {
                    Conversation("D-GOLDEN-02a", false, 2, (golden, "", "Paredes rachadas não resistem a um golpe honesto.")),
                    Conversation("D-GOLDEN-02b", false, 2, (golden, "", "O tomo ainda sussurra, não é? Não o escute. Lembre do juramento.")),
                    Conversation("D-GOLDEN-02c", false, 2,
                        (golden, "", "Força não substitui cuidado. Volte inteiro, ladrãozinho."),
                        (cat, "ironico", "Inteiro é o que eu menos estou.")),
                },
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        static SpeakerProfile Speaker(string file, string name, SpeakerProfile.ScreenSide side, Color nameColor, float pitch, Sprite portrait)
        {
            string path = $"{DataFolder}/Speakers/{file}.asset";
            var s = AssetDatabase.LoadAssetAtPath<SpeakerProfile>(path);
            if (s != null) return s;
            s = ScriptableObject.CreateInstance<SpeakerProfile>();
            s.DisplayName = name;
            s.Side = side;
            s.NameColor = nameColor;
            s.VoicePitch = pitch;
            s.Portrait = portrait;
            AssetDatabase.CreateAsset(s, path);
            return s;
        }

        /// <summary>
        /// Cria a conversa ou, se o asset existente tem revisão menor que <paramref name="revision"/>, reescreve o texto
        /// (mudança de roteiro). Com a mesma revisão, o asset (e qualquer edição feita nele) fica como está.
        /// </summary>
        public static DialogueDefinition Conversation(string id, bool required, int revision, params (SpeakerProfile speaker, string expression, string text)[] lines)
        {
            Directory.CreateDirectory(DataFolder);
            string path = $"{DataFolder}/{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (d != null && d.Revision >= revision) return d;
            bool isNew = d == null;
            if (isNew) d = ScriptableObject.CreateInstance<DialogueDefinition>();
            d.ScriptId = id;
            d.Required = required;
            d.Revision = revision;
            d.Lines.Clear();
            foreach (var (speaker, expression, text) in lines)
                d.Lines.Add(new DialogueLine { Speaker = speaker, Expression = expression, Text = text });
            if (isNew) AssetDatabase.CreateAsset(d, path);
            else EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>Falantes do MVP (gato e Golden), criados se faltarem.</summary>
        public static (SpeakerProfile cat, SpeakerProfile golden) Speakers()
        {
            Directory.CreateDirectory(DataFolder + "/Speakers");
            Directory.CreateDirectory(PortraitArt);
            AssetDatabase.Refresh();
            var cat = Speaker("Gato", "Gato", SpeakerProfile.ScreenSide.Left, new Color32(0xc9, 0xd6, 0xff, 0xff), 1.35f,
                Portrait("portrait_cat_neutral", CatFace.Neutral));
            if (cat.Expressions.Count == 0)
            {
                cat.Expressions.Add(new SpeakerProfile.Expression { Key = "ironico", Portrait = Portrait("portrait_cat_ironic", CatFace.Ironic) });
                EditorUtility.SetDirty(cat);
            }
            var golden = Speaker("Golden", "Golden", SpeakerProfile.ScreenSide.Right, new Color32(0xff, 0xd8, 0x80, 0xff), 0.8f,
                GoldenPortrait());
            return (cat, golden);
        }

        // --------------------------------------------------------- interface

        /// <summary>Monta a interface de diálogo dentro do HUD (chamado pelo ProgressionSetup.CreateHudPrefab).</summary>
        public static void BuildDialogueUi(GameObject hudRoot)
        {
            Directory.CreateDirectory(UiArt);
            AssetDatabase.Refresh();
            var boxSprite = BoxSprite();
            var plateSprite = NameplateSprite();
            var frameSprite = FrameSprite();
            var arrowSprite = ArrowSprite();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = Ui(hudRoot.transform, "Dialogue");
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // Caixa embaixo, centralizada
            var box = Ui(root, "Box");
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, 8f);
            box.sizeDelta = new Vector2(380f, 62f);
            Sliced(box, boxSprite);

            var bodyRt = Ui(box, "Text");
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(10f, 8f);
            bodyRt.offsetMax = new Vector2(-10f, -10f);
            var body = bodyRt.gameObject.AddComponent<Text>();
            body.font = font;
            body.fontSize = 10;
            body.color = new Color32(0x3b, 0x2a, 0x1e, 0xff);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.alignment = TextAnchor.UpperLeft;
            body.raycastTarget = false;

            var arrowRt = Ui(box, "Continue");
            arrowRt.anchorMin = arrowRt.anchorMax = arrowRt.pivot = new Vector2(1f, 0f);
            arrowRt.anchoredPosition = new Vector2(-8f, 6f);
            arrowRt.sizeDelta = new Vector2(5f, 7f);
            var arrow = arrowRt.gameObject.AddComponent<Image>();
            arrow.sprite = arrowSprite;
            arrow.raycastTarget = false;

            // Placa do nome (o DialogueRunner posiciona do lado de quem fala)
            var plate = Ui(box, "Nameplate");
            plate.sizeDelta = new Vector2(96f, 16f);
            Sliced(plate, plateSprite);
            var nameRt = Ui(plate, "Name");
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = nameRt.offsetMax = Vector2.zero;
            var nameText = nameRt.gameObject.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 10;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.raycastTarget = false;

            // Moldura do retrato 56×56 com o rosto 48×48 (escala inteira: 1 pixel = 1 unidade)
            var frame = Ui(box, "PortraitFrame");
            frame.sizeDelta = new Vector2(56f, 56f);
            Sliced(frame, frameSprite);
            var faceRt = Ui(frame, "Portrait");
            faceRt.anchorMin = faceRt.anchorMax = faceRt.pivot = new Vector2(0.5f, 0.5f);
            faceRt.sizeDelta = new Vector2(48f, 48f);
            var face = faceRt.gameObject.AddComponent<Image>();
            face.raycastTarget = false;

            var runner = hudRoot.AddComponent<DialogueRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("box").objectReferenceValue = box;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("nameplate").objectReferenceValue = plate;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("portraitFrame").objectReferenceValue = frame;
            so.FindProperty("portrait").objectReferenceValue = face;
            so.FindProperty("continueArrow").objectReferenceValue = arrow;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
        }

        static RectTransform Ui(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Sliced(RectTransform rt, Sprite sprite)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = UiPixelScale; // bordas com o tamanho em pixels do desenho
            img.raycastTarget = false;
        }

        // ------------------------------------------------------- arte da UI

        static readonly Color32 Ink = new Color32(0x3b, 0x2a, 0x1e, 0xff);

        delegate void Painter(System.Action<int, int, Color32> set);

        static Sprite Draw(string path, int w, int h, Painter paint, int border = 0)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var px = new Color32[w * h];
            paint((x, y, c) => { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; });
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // PixelArtImportPostprocessor
            if (border > 0)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Caixa de pergaminho 16×16 (borda 5): contorno escuro, filete marrom e cantos chanfrados.</summary>
        static Sprite BoxSprite() => Draw($"{UiArt}/ui_dialog_box.png", 16, 16, set =>
        {
            var parchment = new Color32(0xe9, 0xdc, 0xb8, 0xff);
            var trim = new Color32(0x8a, 0x6a, 0x4a, 0xff);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, 15 - x), Mathf.Min(y, 15 - y));
                bool corner = (x < 2 || x > 13) && (y < 2 || y > 13) && !((x == 1 || x == 14) && (y == 1 || y == 14));
                if (corner) continue;                         // canto chanfrado
                set(x, y, d == 0 || (x == 1 || x == 14) && (y == 1 || y == 14) ? Ink : d == 2 ? trim : parchment);
            }
        }, border: 5);

        /// <summary>Placa do nome 16×16 (borda 4): madeira escura com filete dourado.</summary>
        static Sprite NameplateSprite() => Draw($"{UiArt}/ui_dialog_nameplate.png", 16, 16, set =>
        {
            var wood = new Color32(0x6b, 0x4a, 0x2b, 0xff);
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, 15 - x), Mathf.Min(y, 15 - y));
                if ((x == 0 || x == 15) && (y == 0 || y == 15)) continue;
                set(x, y, d == 0 ? Ink : d == 1 ? gold : wood);
            }
        }, border: 4);

        /// <summary>Moldura do retrato 16×16 (borda 4): dourada por fora, fundo escuro por dentro.</summary>
        static Sprite FrameSprite() => Draw($"{UiArt}/ui_portrait_frame.png", 16, 16, set =>
        {
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
            var goldDark = new Color32(0x8a, 0x62, 0x1e, 0xff);
            var back = new Color32(0x2a, 0x22, 0x33, 0xff);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, 15 - x), Mathf.Min(y, 15 - y));
                set(x, y, d == 0 ? Ink : d == 1 ? gold : d == 2 ? goldDark : d == 3 ? Ink : back);
            }
        }, border: 4);

        static Sprite ArrowSprite() => Draw($"{UiArt}/ui_dialog_next.png", 5, 7, set =>
        {
            for (int y = 0; y < 7; y++)
            {
                int half = 3 - Mathf.Abs(y - 3);
                for (int x = 0; x <= half + 1 && x < 5; x++) set(x, y, Ink);
            }
        });

        // ---------------------------------------------------------- retratos

        enum CatFace { Neutral, Ironic }

        static void Ellipse(System.Action<int, int, Color32> set, float cx, float cy, float rx, float ry, Color32 fill, Color32? edge = null)
        {
            for (int y = Mathf.FloorToInt(cy - ry) - 1; y <= Mathf.CeilToInt(cy + ry) + 1; y++)
            for (int x = Mathf.FloorToInt(cx - rx) - 1; x <= Mathf.CeilToInt(cx + rx) + 1; x++)
            {
                float v = (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry);
                if (v > 1f) continue;
                float outer = (x - cx) * (x - cx) / ((rx - 1f) * (rx - 1f)) + (y - cy) * (y - cy) / ((ry - 1f) * (ry - 1f));
                set(x, y, edge.HasValue && outer > 1f ? edge.Value : fill);
            }
        }

        static void Triangle(System.Action<int, int, Color32> set, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int minX = (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y)), maxY = (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) set(x, y, color);
            }
        }

        static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

        /// <summary>
        /// Gato 48×48 (olhando para a direita, ele fica à esquerda da tela): cabeça azul-noite, orelhas com
        /// interior rosado, olhos amarelos de pupila fina e o lenço escuro do Clã das Sombras.
        /// Irônico: pálpebras a meio pau e sorriso torto.
        /// </summary>
        static Sprite Portrait(string name, CatFace face) => Draw($"{PortraitArt}/{name}.png", 48, 48, set =>
        {
            var fur = new Color32(0x2b, 0x3a, 0x67, 0xff);
            var furLight = new Color32(0x3d, 0x52, 0x8c, 0xff);
            var outline = new Color32(0x16, 0x1d, 0x38, 0xff);
            var earIn = new Color32(0xb8, 0x6a, 0x8a, 0xff);
            var eye = new Color32(0xf2, 0xd0, 0x4b, 0xff);
            var pupil = new Color32(0x10, 0x10, 0x18, 0xff);
            var nose = new Color32(0xe0, 0x8a, 0xa8, 0xff);
            var scarf = new Color32(0x16, 0x1d, 0x38, 0xff);
            var scarfLight = new Color32(0x2a, 0x30, 0x52, 0xff);

            Ellipse(set, 24f, 4f, 17f, 8f, fur, outline);                                       // ombros
            Triangle(set, new Vector2(9, 28), new Vector2(19, 31), new Vector2(11, 44), outline); // orelhas
            Triangle(set, new Vector2(29, 31), new Vector2(39, 28), new Vector2(37, 44), outline);
            Triangle(set, new Vector2(11, 29), new Vector2(17, 31), new Vector2(12, 40), earIn);
            Triangle(set, new Vector2(31, 31), new Vector2(37, 29), new Vector2(36, 40), earIn);
            Ellipse(set, 24f, 23f, 15f, 13f, fur, outline);                                     // cabeça
            Ellipse(set, 25f, 17f, 6f, 4f, furLight);                                           // focinho claro
            for (int x = 7; x <= 41; x++) for (int y = 7; y <= 10; y++) set(x, y, (x + y) % 4 == 0 ? scarfLight : scarf); // lenço
            Triangle(set, new Vector2(30, 10), new Vector2(38, 10), new Vector2(36, 2), scarf);   // nó do lenço

            foreach (int ex in new[] { 18, 31 })
            {
                Ellipse(set, ex, 24f, 3.5f, 3.5f, eye);
                for (int y = 22; y <= 26; y++) set(ex + 1, y, pupil);                              // pupila fina, olhando para a direita
                if (face == CatFace.Ironic)
                    for (int x = ex - 4; x <= ex + 4; x++) { set(x, 25, outline); set(x, 26, fur); set(x, 27, fur); } // pálpebra
            }
            set(25, 19, nose); set(26, 19, nose);
            if (face == CatFace.Ironic) { set(24, 15, outline); set(25, 15, outline); set(26, 16, outline); set(27, 16, outline); set(28, 17, outline); } // sorriso torto
            else { set(24, 16, outline); set(25, 15, outline); set(26, 16, outline); set(27, 15, outline); set(28, 16, outline); }  // boca em "w"
        });

        /// <summary>Golden 48×48 (olhando para a esquerda, ele fica à direita): orelhas caídas, focinho claro e gola de armadura.</summary>
        static Sprite GoldenPortrait() => Draw($"{PortraitArt}/portrait_golden.png", 48, 48, set =>
        {
            var fur = new Color32(0xe0, 0xa8, 0x4e, 0xff);
            var furDark = new Color32(0xb9, 0x7a, 0x2e, 0xff);
            var snout = new Color32(0xf2, 0xcf, 0x8a, 0xff);
            var outline = new Color32(0x4a, 0x30, 0x12, 0xff);
            var eye = new Color32(0x3a, 0x24, 0x10, 0xff);
            var steel = new Color32(0xc9, 0xcf, 0xdb, 0xff);
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);

            Ellipse(set, 24f, 3f, 20f, 8f, steel, outline);                // gola de armadura
            for (int x = 6; x <= 42; x++) set(x, 9, gold);                 // faixa dourada
            Ellipse(set, 11f, 22f, 5f, 11f, furDark, outline);             // orelhas caídas
            Ellipse(set, 37f, 22f, 5f, 11f, furDark, outline);
            Ellipse(set, 24f, 25f, 13f, 14f, fur, outline);                // cabeça
            Ellipse(set, 21f, 16f, 8f, 6f, snout, outline);                // focinho (para a esquerda)
            for (int x = 15; x <= 18; x++) for (int y = 17; y <= 19; y++) set(x, y, outline); // nariz
            foreach (int ex in new[] { 18, 28 })
            {
                set(ex, 26, eye); set(ex + 1, 26, eye); set(ex, 27, eye); set(ex + 1, 27, eye);
                set(ex, 27, new Color32(0xff, 0xff, 0xff, 0xff));           // brilho no olho
                for (int x = ex - 1; x <= ex + 2; x++) set(x, 30, furDark); // sobrancelha calma
            }
            for (int x = 18; x <= 23; x++) set(x, 12, outline);             // boca serena
        });
    }
}
