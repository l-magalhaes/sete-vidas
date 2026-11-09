using System.IO;
using SeteVidas.Combat;
using SeteVidas.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 2: boneco de treino (sprite + prefab) e montagem completa
    /// (jogador com escalada e adagas, Playground com chaminé e bonecos). Idempotente.
    /// Specs: Docs/Design/escalada.md e Docs/Design/combate-adagas.md.
    /// </summary>
    public static class CombatSetup
    {
        const string DummyArtFolder = "Assets/_Game/Art/Enemies/TrainingDummy";
        public const string DummyPrefabPath = "Assets/_Game/Prefabs/Enemies/TrainingDummy.prefab";

        [MenuItem("Tools/Sete Vidas/5. Fase 2: escalada, adagas e boneco de treino", priority = 41)]
        public static void RunPhase2()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!PlayerSetup.CreatePlayerAssets()) return;   // prefab do gato com escalada e combate
            CreateDummyPrefab();
            SeteVidasSetup.CreateScenes();                   // recria a Playground com chaminé, bonecos e jogador
            Debug.Log("[Sete Vidas] Fase 2 pronta: chaminé escalável (x 92–98) e bonecos de treino (x 110 e 115) na Playground.");
        }

        /// <summary>Cria o sprite e o prefab do boneco. Sempre refaz o prefab (não edite à mão).</summary>
        public static GameObject CreateDummyPrefab()
        {
            Directory.CreateDirectory(DummyArtFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DummyPrefabPath));
            AssetDatabase.Refresh();
            var sprite = MakeDummySprite();

            var root = new GameObject("TrainingDummy") { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };

            // Trigger: o gato atravessa o boneco para chegar às costas dele
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.75f, 1.5f);
            box.offset = new Vector2(0f, 0.75f);

            var visualGo = new GameObject("Visual") { layer = root.layer };
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;

            var labelGo = new GameObject("Status") { layer = root.layer };
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            var label = DamagePopup.CreateText(labelGo, "10/10", new Color(1f, 1f, 1f, 0.8f), 28);

            var dummy = root.AddComponent<TrainingDummy>();
            var so = new SerializedObject(dummy);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("hurtbox").objectReferenceValue = box;
            so.FindProperty("statusLabel").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DummyPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Coloca os bonecos de treino na Playground (chamado ao recriar a cena).</summary>
        public static void PlaceDummies(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath);
            if (prefab == null) prefab = CreateDummyPrefab();
            if (prefab == null) return;

            var parent = new GameObject("TrainingDummies");
            SceneManager.MoveGameObjectToScene(parent, scene);
            Place(prefab, parent.transform, "Dummy olhando para a direita", new Vector3(110.5f, 0f, 0f), TrainingDummy.Side.Right);
            Place(prefab, parent.transform, "Dummy olhando para a esquerda", new Vector3(115.5f, 0f, 0f), TrainingDummy.Side.Left);
        }

        static void Place(GameObject prefab, Transform parent, string name, Vector3 position, TrainingDummy.Side side)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            var dummy = go.GetComponent<TrainingDummy>();
            dummy.SetFacing(side); // também espelha o sprite
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponentInChildren<SpriteRenderer>());
        }

        /// <summary>
        /// Boneco de palha 16×26 px olhando para a direita: estaca, saco de palha com cinto de corda e
        /// cabeça com um olho e um "nariz" do lado da frente. As costas (lado sem rosto) leem de longe.
        /// </summary>
        static Sprite MakeDummySprite()
        {
            string path = $"{DummyArtFolder}/enm_training_dummy.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int w = 16, h = 26;
            var post = new Color32(0x6b, 0x4a, 0x2b, 0xff);
            var straw = new Color32(0xc9, 0xa2, 0x5a, 0xff);
            var strawDark = new Color32(0x9a, 0x76, 0x3a, 0xff);
            var outline = new Color32(0x4a, 0x34, 0x1a, 0xff);
            var rope = new Color32(0x7a, 0x5c, 0x3c, 0xff);
            var face = new Color32(0x24, 0x18, 0x0c, 0xff);
            var px = new Color32[w * h];

            void Set(int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }
            void Rect(int x0, int y0, int x1, int y1, Color32 fill)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, x == x0 || x == x1 || y == y0 || y == y1 ? outline : fill);
            }

            Rect(7, 0, 8, 6, post);              // estaca
            Rect(3, 6, 12, 18, straw);           // corpo
            for (int y = 8; y <= 16; y += 3) Set(5, y, strawDark); // fiapos de palha
            for (int y = 9; y <= 17; y += 3) Set(10, y, strawDark);
            for (int x = 4; x <= 11; x++) Set(x, 11, rope);        // cinto de corda
            Rect(4, 18, 11, 25, straw);          // cabeça
            Set(9, 22, face); Set(10, 22, face);  // olho (lado da frente = direita)
            Set(12, 21, outline); Set(12, 20, outline); // "nariz" saindo para a frente
            Set(9, 20, face);                     // boca

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // pasta Enemies: pivot nos pés
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
