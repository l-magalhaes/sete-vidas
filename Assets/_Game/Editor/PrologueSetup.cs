using System.IO;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Prólogo do Novo jogo: o gato lê o contrato do roubo do tomo (cena Prologue, entre a tela inicial e a
    /// Fortaleza). Texto em ScriptableObjects/Dialogue/D-CONTRATO; roteiro em Docs/Narrativa/dialogos-mvp.md.
    /// </summary>
    public static class PrologueSetup
    {
        const string Art = "Assets/_Game/Art/UI/Prologue";
        const string BoxSprite = "Assets/_Game/Art/UI/Dialogue/ui_dialog_box.png";
        const string FrameSprite = "Assets/_Game/Art/UI/Dialogue/ui_portrait_frame.png";
        const string TorchSprite = "Assets/_Game/Art/Environments/Citadel/decor_torch.png";
        const float UiPixelScale = 100f / 16f;

        static readonly Color32 Ink = new Color32(0x3b, 0x2a, 0x1e, 0xff);
        static readonly Color32 Light = new Color32(0xe9, 0xdc, 0xb8, 0xff);

        [MenuItem("Tools/Sete Vidas/14. Prólogo: o contrato (antes da Fortaleza)", priority = 50)]
        public static void CreatePrologue()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Path.GetDirectoryName(GameConstants.PrologueScenePath));
            AssetDatabase.Refresh();
            CreateContract(); // garante o asset; carregado de novo depois de abrir a cena nova

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var contract = CreateContract();
            var seal = SealSprite();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0e, 0x0c, 0x18, 0xff);
            camGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Prólogo", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameConstants.ReferenceWidth, GameConstants.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            // Mesa à luz de vela: duas tochas e o pergaminho no centro
            var torch = AssetDatabase.LoadAssetAtPath<Sprite>(TorchSprite);
            if (torch != null)
            {
                Img(root, "Vela esquerda", torch, new Vector2(-200f, 20f), new Vector2(16f, 32f));
                Img(root, "Vela direita", torch, new Vector2(200f, 20f), new Vector2(16f, 32f));
            }
            var parchment = Img(root, "Pergaminho", AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite), new Vector2(0f, 12f), new Vector2(300f, 196f));
            parchment.type = Image.Type.Sliced;
            parchment.pixelsPerUnitMultiplier = UiPixelScale;
            var p = parchment.rectTransform;

            var title = Label(p, "Título", "CONTRATO DE SERVIÇO", 14, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(280f, 20f));
            title.fontStyle = FontStyle.Bold;
            var body = Label(p, "Texto", "", 10, Ink, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(256f, 130f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.lineSpacing = 1.1f;
            var sealImg = Img(p, "Lacre", seal, new Vector2(110f, -66f), new Vector2(24f, 24f));
            sealImg.gameObject.SetActive(false);

            // O gato pensando alto: retrato à esquerda do pergaminho
            var speakerBox = new GameObject("Gato", typeof(RectTransform));
            speakerBox.transform.SetParent(root, false);
            var frame = Img(speakerBox.transform, "Moldura", AssetDatabase.LoadAssetAtPath<Sprite>(FrameSprite), new Vector2(-190f, -40f), new Vector2(56f, 56f));
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = UiPixelScale;
            var face = Img(frame.rectTransform, "Retrato", null, Vector2.zero, new Vector2(48f, 48f));
            var name = Label(speakerBox.transform, "Nome", "Gato", 10, Light, new Vector2(0.5f, 0.5f), new Vector2(-190f, -76f), new Vector2(80f, 14f));
            name.fontStyle = FontStyle.Bold;
            speakerBox.SetActive(false);

            var hint = Label(root, "Dica", "E / Enter: continuar  ·  Esc: pular", 8, Light, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(300f, 12f));

            var controller = canvasGo.AddComponent<PrologueController>();
            var so = new SerializedObject(controller);
            so.FindProperty("contract").objectReferenceValue = contract;
            so.FindProperty("nextScene").stringValue = Path.GetFileNameWithoutExtension(GameConstants.FortressScenePath);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("speakerBox").objectReferenceValue = speakerBox;
            so.FindProperty("portrait").objectReferenceValue = face;
            so.FindProperty("speakerName").objectReferenceValue = name;
            so.FindProperty("seal").objectReferenceValue = sealImg.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), GameConstants.PrologueScenePath);
            SeteVidasSetup.RegisterScenesInBuild();
            Debug.Log("[Sete Vidas] Prólogo criado: Novo jogo agora abre o contrato antes da Fortaleza.");
        }

        /// <summary>
        /// O contrato (D-CONTRATO). Páginas sem falante = texto do contrato; "miuda" = letra miúda; "selo" = lacre.
        /// Depois, o gato comenta e aceita.
        /// </summary>
        public static DialogueDefinition CreateContract()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var d = DialogueSetup.Conversation("D-CONTRATO", true, 1,
                (null, "", "Ao Clã das Sombras, a Guilda dos Ladrões.\nAos cuidados do melhor gatuno da casa."),
                (null, "", "Objeto: um tomo de capa roxa, guardado na Câmara do Selo da Cidadela da Ordem dos Paladinos Cães."),
                (null, "", "Sou colecionador de relíquias antigas. A Ordem esconde esse livro há cem anos e se recusa a vendê-lo, emprestá-lo ou sequer mostrá-lo a quem não late."),
                (null, "", "A Cidadela tem muralhas altas, sentinelas que não dormem, armadilhas em cada corredor e um paladino de guarda diante do selo."),
                (null, "", "Mandei soldados. Mandei mercenários. Mandei magos. Nenhum voltou.\n\nSó a Guilda dos Ladrões entra onde ninguém entra e sai sem que ninguém perceba. Por isso escrevo a vocês."),
                (null, "", "Pagamento: uma bolsa de ouro, entregue em mãos assim que o tomo deixar o pedestal."),
                (null, "miuda", "Cláusula 7: o portador assume, de livre vontade, todos os custos pessoais decorrentes do manuseio do objeto."),
                (null, "selo", "Assinado,\n\nO Contratante"),
                (cat, "ironico", "Uma bolsa de ouro por um livro velho. Colecionador, é? Gente rica tem cada mania."),
                (cat, "", "Muralhas, sentinelas, armadilhas, um paladino... Ele tem razão: é trabalho para a Guilda."),
                (cat, "ironico", "Cláusula sete... letra miúda é coisa de advogado. Eu sou ladrão."),
                (cat, "", "Entrar, pegar, sair. Esta noite."));
            if (!d.IsDocument)
            {
                d.IsDocument = true;
                EditorUtility.SetDirty(d);
                AssetDatabase.SaveAssets();
            }
            return d;
        }

        // --------------------------------------------------------------- UI

        static Image Img(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
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
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Lacre de cera 24×24: roxo (a cor do Contratante), borda irregular e um "C" dourado.</summary>
        static Sprite SealSprite()
        {
            string path = $"{Art}/ui_wax_seal.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int s = 24;
            var wax = new Color32(0x5b, 0x2a, 0x6e, 0xff);
            var waxDark = new Color32(0x34, 0x14, 0x40, 0xff);
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float a = Mathf.Atan2(y - 11.5f, x - 11.5f);
                float r = 10.5f + 0.9f * Mathf.Sin(a * 7f);               // borda derretida
                float d = Mathf.Sqrt((x - 11.5f) * (x - 11.5f) + (y - 11.5f) * (y - 11.5f));
                if (d > r) continue;
                px[y * s + x] = d > r - 1.2f ? waxDark : (d > 6.5f && d < 7.5f ? gold : wax);
            }
            // "C" dourado no centro
            for (int y = 8; y <= 15; y++) px[y * s + 8] = gold;
            for (int x = 9; x <= 14; x++) { px[15 * s + x] = gold; px[8 * s + x] = gold; }
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
