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
            public DialogueDefinition Bond, BondFollowUp, Unlock;
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
                // D-GOLDEN-01 (rev. 5): o resgate. Koda gastou a última força segurando a praga do livro e precisa descansar
                Bond = Conversation("D-GOLDEN-01", true, 5,
                    (golden, "ferido", "Ladrãozinho! Aguente firme. Seu último coração ainda bate... por pouco."),
                    (cat, "ironico", "Ótimo. Agora o cão de lata vai me dar sermão antes de eu morrer."),
                    (golden, "", "Sermão nenhum. Quem fez isso? Quem levou o tomo?"),
                    (cat, "", "O contratante. Uma capivara. Me pagou com uma moeda... e com seis vidas."),
                    (golden, "ferido", "Quando o selo rompeu, gastei a última força que eu tinha para segurar a praga que saiu do livro."),
                    (golden, "ferido", "Contive um pouco. Só um pouco. Ela já escorre pelas muralhas e vai se espalhar por todo lugar."),
                    (cat, "ironico", "E você vai atrás do tomo assim? Mal fica de pé."),
                    (golden, "serio", "Não agora. Preciso descansar antes de ir atrás dele. Mas a praga não espera por mim."),
                    (golden, "serio", "Jure me ajudar a recuperar o tomo e destruí-lo. Em troca, minha força é sua.")),
                // D-GOLDEN-01b: o gato aceita, movido pela vingança
                BondFollowUp = Conversation("D-GOLDEN-01b", true, 2,
                    (cat, "", "Destruir o tomo... tanto faz. Eu quero a capivara."),
                    (cat, "", "Ninguém me usa como chave e me paga com troco. Ninguém."),
                    (golden, "serio", "Vingança é um começo ruim. Mas é um começo. Jure, e levante."),
                    (cat, "", "Eu juro. Pelo tomo, pela moeda... e pelas seis vidas que ele me tirou.")),
                Unlock = Conversation("D-GOLDEN-01-SISTEMA", true, 2,
                    (null, "", "Paladino desbloqueado! Aperte {trocar_classe} para trocar de classe."),
                    (null, "", "Magia de cura aprendida! Aperte {curar} para curar 1 coração. 3 usos; o santuário recarrega.")),
                // D-GOLDEN-02 (rev. 2), em rodízio: lembram o juramento
                Idle = new[]
                {
                    Conversation("D-GOLDEN-02a", false, 3,
                        (golden, "ferido", "Ainda sinto a praga do livro nas patas. Preciso descansar antes de ir atrás do tomo."),
                        (golden, "", "Até lá, a minha força vai com você. Paredes rachadas não resistem a um golpe honesto.")),
                    Conversation("D-GOLDEN-02b", false, 3,
                        (golden, "", "Onde aquela capivara for, a praga vai atrás. Vamos alcançá-la."),
                        (cat, "ironico", "E devolver a moeda. Enfiada onde ela não gosta.")),
                    Conversation("D-GOLDEN-02c", false, 3,
                        (golden, "sorriso", "Força não substitui cuidado. Volte inteiro, ladrãozinho."),
                        (cat, "ironico", "Inteiro é o que eu menos estou.")),
                },
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>O contratante: capivara necromante (fala grave e arrastada). Criado se faltar.</summary>
        public static SpeakerProfile ContractorSpeaker()
        {
            Directory.CreateDirectory(DataFolder + "/Speakers");
            Directory.CreateDirectory(PortraitArt);
            AssetDatabase.Refresh();
            var capy = Speaker("Contratante", "Contratante", SpeakerProfile.ScreenSide.Right, new Color32(0xc8, 0x9c, 0xff, 0xff), 0.6f,
                ContractorPortrait());
            AddExpressions(capy, ContractorExpressions);
            return capy;
        }

        /// <summary>Retratos gerados por Tools/CharacterSprites/gerar.ps1 (Koda e Contratante).</summary>
        static readonly (string key, string file)[] KodaExpressions =
        {
            ("serio", "portrait_koda_serious"), ("ferido", "portrait_koda_hurt"), ("sorriso", "portrait_koda_smile"),
        };
        static readonly (string key, string file)[] ContractorExpressions =
        {
            ("deboche", "portrait_contractor_grin"), ("revelacao", "portrait_contractor_reveal"),
        };

        static void AddExpressions(SpeakerProfile profile, (string key, string file)[] list)
        {
            foreach (var (key, file) in list)
            {
                if (profile.Expressions.Exists(e => e.Key == key)) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitArt}/{file}.png");
                if (sprite == null) continue;
                profile.Expressions.Add(new SpeakerProfile.Expression { Key = key, Portrait = sprite });
                EditorUtility.SetDirty(profile);
            }
        }

        /// <summary>
        /// Capivara necromante 48×48 (olhando para a esquerda): pelo marrom, focinho largo, olhos semicerrados com o
        /// brilho verde-ácido da praga, capuz roxo e uma corrente de ouro (é rica: paga com ouro).
        /// </summary>
        static Sprite ContractorPortrait() => Draw($"{PortraitArt}/portrait_contractor.png", 48, 48, set =>
        {
            var fur = new Color32(0x8a, 0x5a, 0x3a, 0xff);
            var furDark = new Color32(0x5e, 0x3b, 0x24, 0xff);
            var snout = new Color32(0xa8, 0x72, 0x50, 0xff);
            var outline = new Color32(0x2a, 0x16, 0x0c, 0xff);
            var hood = new Color32(0x4a, 0x22, 0x66, 0xff);
            var hoodEdge = new Color32(0x22, 0x10, 0x2e, 0xff);
            var glow = new Color32(0x8f, 0xd1, 0x4f, 0xff);
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);

            Ellipse(set, 24f, 4f, 20f, 9f, hood, hoodEdge);                 // ombros com o manto
            Ellipse(set, 25f, 26f, 17f, 17f, hood, hoodEdge);               // capuz por trás da cabeça
            Ellipse(set, 25f, 23f, 13f, 12f, fur, outline);                 // cabeça
            Ellipse(set, 15f, 18f, 9f, 7f, snout, outline);                 // focinho largo (para a esquerda)
            for (int x = 10; x <= 13; x++) { set(x, 21, outline); set(x, 20, outline); } // narina
            for (int x = 9; x <= 18; x++) set(x, 14, outline);              // boca reta, de quem não se importa
            set(17, 15, outline); set(18, 15, outline);                     // canto da boca: sorriso de lado
            Ellipse(set, 31f, 33f, 2.5f, 2f, furDark, outline);             // orelhinha
            foreach (int ex in new[] { 20, 28 })                            // olhos semicerrados, brilho da praga
            {
                for (int x = ex - 2; x <= ex + 1; x++) set(x, 26, outline);
                set(ex - 1, 25, glow); set(ex, 25, glow);
            }
            for (int x = 8; x <= 40; x += 3) { set(x, 9, gold); set(x + 1, 10, gold); } // corrente de ouro
            Ellipse(set, 24f, 7f, 2f, 2f, gold, outline);                   // medalhão
        });

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
            var cat = Speaker("Gato", "Lionel", SpeakerProfile.ScreenSide.Left, new Color32(0xf4, 0xd6, 0xa3, 0xff), 1.35f,
                LionPortrait("portrait_cat_neutral"));
            foreach (var (key, file) in LionExpressions)
            {
                if (cat.Expressions.Exists(e => e.Key == key)) continue;
                var sprite = LionPortrait(file);
                if (sprite == null) continue;
                cat.Expressions.Add(new SpeakerProfile.Expression { Key = key, Portrait = sprite });
                EditorUtility.SetDirty(cat);
            }
            var golden = Speaker("Golden", "Koda", SpeakerProfile.ScreenSide.Right, new Color32(0xff, 0xd8, 0x80, 0xff), 0.8f,
                GoldenPortrait());
            AddExpressions(golden, KodaExpressions);
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

        /// <summary>
        /// Retratos do Lionel: gerados por Tools/LionSprites/gerar.ps1 (LionPortraits.cs) a partir da ficha em
        /// SeteVidasDocs/Personagens.md. Aqui só são carregados.
        /// </summary>
        static readonly (string key, string file)[] LionExpressions =
        {
            ("ironico", "portrait_cat_ironic"), ("surpreso", "portrait_cat_surprised"), ("ferido", "portrait_cat_hurt"),
            ("serio", "portrait_cat_serious"), ("terno", "portrait_cat_tender"),
        };

        static Sprite LionPortrait(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitArt}/{file}.png");
            if (sprite == null) Debug.LogWarning($"Retrato {file} não encontrado. Rode Tools/LionSprites/gerar.ps1.");
            return sprite;
        }

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
