using System.IO;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 5: ClassDefinition do Ladino e do Paladino, marreta (PaladinHammer), ícones, camada da
    /// marreta sobre o gato, parede rachada, Golden (vínculo placeholder) e o trecho I da Playground.
    /// Spec: Docs/Design/classes-paladino.md.
    /// </summary>
    public static class ClassSetup
    {
        const string ClassArt = "Assets/_Game/Art/Characters/Cat/Classes";
        const string GoldenArt = "Assets/_Game/Art/Characters/Golden";
        const string IconArt = "Assets/_Game/Art/UI/Classes";
        const string EnvArt = "Assets/_Game/Art/Environments/Placeholder";
        const string ClassesFolder = "Assets/_Game/ScriptableObjects/Classes";
        const string HammerPath = "Assets/_Game/ScriptableObjects/Combat/PaladinHammer.asset";
        public const string WallPrefabPath = "Assets/_Game/Prefabs/Interactables/BreakableWall.prefab";
        public const string GoldenPrefabPath = "Assets/_Game/Prefabs/Interactables/Golden.prefab";

        [MenuItem("Tools/Sete Vidas/8. Fase 5: classes, Paladino e parede rachada", priority = 44)]
        public static void RunPhase5()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!PlayerSetup.CreatePlayerAssets()) return;   // gato com ClassManager, adagas e marreta
            ProgressionSetup.CreateHudPrefab();              // ícone da classe ao lado dos corações
            CreateWallPrefab();
            CreateGoldenPrefab();
            SeteVidasSetup.CreateScenes();                   // Playground com o trecho I (x 154–177)
            Debug.Log("[Sete Vidas] Fase 5 pronta: parede rachada em x 160 (embaixo) e Golden em x 168,5 (em cima). " +
                      "Apague o save (Tools > Sete Vidas > Save) para testar do zero.");
        }

        // ------------------------------------------------------------ classes

        public struct ClassAssets { public ClassDefinition Rogue, Paladin; }

        /// <summary>Cria/atualiza os ClassDefinition e a marreta. Valores já existentes nos assets são preservados.</summary>
        public static ClassAssets CreateClassAssets(MeleeAttackSettings daggers)
        {
            Directory.CreateDirectory(ClassArt);
            Directory.CreateDirectory(IconArt);
            Directory.CreateDirectory(ClassesFolder);
            AssetDatabase.Refresh();

            var hammer = AssetDatabase.LoadAssetAtPath<MeleeAttackSettings>(HammerPath);
            if (hammer == null)
            {
                hammer = ScriptableObject.CreateInstance<MeleeAttackSettings>();
                // Marreta: lenta, antecipação longa, dano alto e Heavy (GDD §5.3, spec da Fase 5) [PROPOSTA]
                hammer.BaseDamage = 4;
                hammer.Type = DamageType.Heavy;
                hammer.BackstabBonus = false;
                hammer.ComboSteps = 1;
                hammer.Windup = 0.35f;
                hammer.Active = 0.12f;
                hammer.Recovery = 0.2f;
                hammer.FinalRecovery = 0.45f;
                hammer.MoveMultiplierWhileAttacking = 0.3f;
                hammer.NoiseMultiplier = 2f;
                hammer.HitboxSize = new Vector2(1.4f, 1.3f);
                hammer.HitboxForward = 0.6f;
                hammer.HitstopFront = hammer.HitstopBack = hammer.HitstopStealth = 0.1f;
                AssetDatabase.CreateAsset(hammer, HammerPath);
            }

            var rogue = LoadOrCreateClass("Rogue", CharacterClass.Rogue, "Ladino", new Color32(0x4a, 0x63, 0xb8, 0xff));
            rogue.Weapon = daggers;
            rogue.Icon = MakeIcon("icon_class_rogue", new Color32(0x2b, 0x3a, 0x67, 0xff), Emblem.Daggers);
            EditorUtility.SetDirty(rogue);

            var paladin = LoadOrCreateClass("Paladin", CharacterClass.Paladin, "Paladino", new Color32(0xd9, 0xa4, 0x41, 0xff));
            paladin.Weapon = hammer;
            paladin.Icon = MakeIcon("icon_class_paladin", new Color32(0x8a, 0x62, 0x1e, 0xff), Emblem.Hammer);
            paladin.OverlayIdle = MakeHammerOverlay("chr_cat_paladin_hammer_idle", HammerPose.OnBack);
            paladin.OverlayWindup = MakeHammerOverlay("chr_cat_paladin_hammer_windup", HammerPose.Raised);
            paladin.OverlayStrike = MakeHammerOverlay("chr_cat_paladin_hammer_strike", HammerPose.Down);
            paladin.SlashSprites = new[] { MakeSmashSprite() };
            EditorUtility.SetDirty(paladin);

            AssetDatabase.SaveAssets();
            return new ClassAssets { Rogue = rogue, Paladin = paladin };
        }

        static ClassDefinition LoadOrCreateClass(string file, CharacterClass c, string displayName, Color color)
        {
            string path = $"{ClassesFolder}/{file}.asset";
            var def = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
            if (def != null) return def;
            def = ScriptableObject.CreateInstance<ClassDefinition>();
            def.Class = c;
            def.DisplayName = displayName;
            def.ThemeColor = color;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        // ------------------------------------------------------------- cena

        /// <summary>Trecho I da Playground (prefabs; os tiles ficam no SeteVidasSetup.CreatePlayground).</summary>
        public static void PlaceInPlayground(Scene scene)
        {
            var wall = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            if (wall == null) wall = CreateWallPrefab();

            var parent = new GameObject("PaladinSection");
            SceneManager.MoveGameObjectToScene(parent, scene);

            var w = (GameObject)PrefabUtility.InstantiatePrefab(wall, parent.transform);
            w.name = "Parede rachada (playground_tunel)";
            w.transform.position = new Vector3(160.5f, 0f, 0f);
            var so = new SerializedObject(w.GetComponent<BreakableWall>());
            so.FindProperty("wallId").stringValue = "playground_tunel";
            so.ApplyModifiedPropertiesWithoutUndo();

            var sign = new GameObject("Placa: atalho");
            sign.transform.SetParent(parent.transform, false);
            sign.transform.position = new Vector3(168.5f, 1.2f, 0f);
            DamagePopup.CreateText(sign, "Atalho secreto!\n(só a marreta abre)", new Color32(0xff, 0xe9, 0xa8, 0xff), 26);
        }

        // ------------------------------------------------------------ prefabs

        public static GameObject CreateWallPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WallPrefabPath));
            AssetDatabase.Refresh();
            // padrão: a parede de Soluki sem brilho dourado (Tools/EnvironmentSprites); a Fortaleza troca pela pedra do lugar.
            // Se a arte ainda não foi gerada, usa o desenho antigo.
            const string Arch = "Assets/_Game/Art/Environments/Citadel/Architecture";
            var intact = AssetDatabase.LoadAssetAtPath<Sprite>($"{Arch}/env_cid_parede_quebravel_soluki_intacta.png") ?? MakeWallSprite("env_breakable_wall", cracked: false);
            var cracked = AssetDatabase.LoadAssetAtPath<Sprite>($"{Arch}/env_cid_parede_quebravel_soluki_danificada.png") ?? MakeWallSprite("env_breakable_wall_cracked", cracked: true);
            var rubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Arch}/env_cid_parede_quebravel_soluki_entulho.png") ?? MakeRubbleSprite();

            var root = new GameObject("BreakableWall") { layer = LayerMask.NameToLayer(GameConstants.Layers.Breakable) };
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);

            var visual = Child(root, "Visual", new Vector3(0f, 2f, 0f)); // sprite 16×64 com pivot central
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = intact;
            sr.sortingOrder = 1;

            var rubble = Child(root, "Entulho", new Vector3(0f, 3f / 16f, 0f));
            var rsr = rubble.AddComponent<SpriteRenderer>();
            rsr.sprite = rubbleSprite;
            rsr.sortingOrder = 1;
            rubble.SetActive(false);

            var wall = root.AddComponent<BreakableWall>();
            var so = new SerializedObject(wall);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("intactSprite").objectReferenceValue = intact;
            so.FindProperty("crackedSprite").objectReferenceValue = cracked;
            so.FindProperty("rubble").objectReferenceValue = rubble;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, WallPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject CreateGoldenPrefab()
        {
            Directory.CreateDirectory(GoldenArt);
            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPrefabPath));
            AssetDatabase.Refresh();
            var sprite = MakeGoldenSprite();

            var root = new GameObject("Golden") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 2.2f);
            box.offset = new Vector2(0f, 1.1f);

            var visual = Child(root, "Visual", Vector3.zero);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;
            sr.flipX = true; // olha para a esquerda, para quem chega

            var prompt = DamagePopup.CreateText(Child(root, "Prompt", new Vector3(0f, 2.5f, 0f)), "E: falar", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            prompt.gameObject.SetActive(false);

            var bond = root.AddComponent<CompanionBond>();
            var so = new SerializedObject(bond);
            so.FindProperty("prompt").objectReferenceValue = prompt;
            // Conversas na interface de diálogo (D-GOLDEN-01 e D-GOLDEN-02, ScriptableObjects/Dialogue)
            var dialogues = DialogueSetup.CreateDialogueAssets();
            so.FindProperty("bondDialogue").objectReferenceValue = dialogues.Bond;
            so.FindProperty("unlockDialogue").objectReferenceValue = dialogues.Unlock;
            var idleProp = so.FindProperty("idleDialogues");
            idleProp.arraySize = dialogues.Idle.Length;
            for (int i = 0; i < dialogues.Idle.Length; i++) idleProp.GetArrayElementAtIndex(i).objectReferenceValue = dialogues.Idle[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GoldenPrefabPath);
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

        // ------------------------------------------------------------ sprites

        static readonly Color32 Outline = new Color32(0x16, 0x1d, 0x38, 0xff);
        static readonly Color32 Wood = new Color32(0x6b, 0x4a, 0x2b, 0xff);
        static readonly Color32 Steel = new Color32(0xb8, 0xbf, 0xcc, 0xff);
        static readonly Color32 Gold = new Color32(0xd9, 0xa4, 0x41, 0xff);

        delegate void Painter(System.Action<int, int, Color32> set);

        static Sprite Draw(string path, int w, int h, Painter paint)
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
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Rect(System.Action<int, int, Color32> set, int x0, int y0, int x1, int y1, Color32 fill, Color32 edge)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                set(x, y, x == x0 || x == x1 || y == y0 || y == y1 ? edge : fill);
        }

        enum Emblem { Daggers, Hammer }

        /// <summary>Ícone 16×16: moldura na cor da classe e o emblema claro (adagas cruzadas ou marreta).</summary>
        static Sprite MakeIcon(string name, Color32 background, Emblem emblem) =>
            Draw($"{IconArt}/{name}.png", 16, 16, set =>
            {
                var light = new Color32(0xf2, 0xee, 0xe0, 0xff);
                Rect(set, 0, 0, 15, 15, background, Outline);
                if (emblem == Emblem.Daggers)
                {
                    for (int i = 3; i <= 12; i++) { set(i, i, light); set(15 - i, i, light); }
                    set(3, 4, Gold); set(4, 3, Gold); set(12, 4, Gold); set(11, 3, Gold); // guardas
                }
                else
                {
                    for (int y = 2; y <= 9; y++) { set(7, y, Wood); set(8, y, Wood); } // cabo
                    Rect(set, 3, 9, 12, 13, Steel, Outline);                          // cabeça
                    for (int x = 4; x <= 11; x++) set(x, 11, Gold);                  // faixa dourada
                }
            });

        enum HammerPose { OnBack, Raised, Down }

        /// <summary>
        /// Marreta por cima do gato (canvas 32×28, pivot nos pés, gato olhando para a direita).
        /// Nas costas ao andar; erguida atrás da cabeça na preparação; batendo à frente no golpe.
        /// </summary>
        static Sprite MakeHammerOverlay(string name, HammerPose pose) =>
            Draw($"{ClassArt}/{name}.png", 32, 28, set =>
            {
                switch (pose)
                {
                    case HammerPose.OnBack: // cabo inclinado nas costas, cabeça acima do ombro esquerdo
                        for (int i = 0; i <= 10; i++) set(14 - i / 2, 6 + i, Wood);
                        Rect(set, 6, 16, 12, 20, Steel, Outline);
                        for (int x = 7; x <= 11; x++) set(x, 18, Gold);
                        break;
                    case HammerPose.Raised: // erguida atrás da cabeça
                        for (int y = 14; y <= 22; y++) set(13, y, Wood);
                        Rect(set, 8, 22, 17, 27, Steel, Outline);
                        for (int x = 9; x <= 16; x++) set(x, 24, Gold);
                        break;
                    default: // golpe: cabo à frente e cabeça no chão
                        for (int x = 18; x <= 25; x++) set(x, 8 - (x - 18) / 3, Wood);
                        Rect(set, 24, 0, 30, 7, Steel, Outline);
                        for (int y = 1; y <= 6; y++) set(27, y, Gold);
                        break;
                }
            });

        /// <summary>Impacto da marreta 24×20: onda dourada de poeira saindo do chão.</summary>
        static Sprite MakeSmashSprite() =>
            Draw($"{ClassArt}/fx_hammer_smash.png", 24, 20, set =>
            {
                var dust = new Color32(0xe8, 0xd9, 0xa8, 0xff);
                var glow = new Color32(0xff, 0xd3, 0x4d, 0xff);
                for (int x = 0; x < 24; x++)
                {
                    int top = 2 + (int)(12f * Mathf.Sin(Mathf.PI * x / 23f));
                    for (int y = 0; y <= top; y++)
                        if (y == top || y == top - 1) set(x, y, glow);
                        else if ((x + y) % 5 == 0) set(x, y, dust);
                }
            });

        /// <summary>
        /// Parede rachada 16×64 (1 × 4 tiles): a mesma pedra do chão, com rachaduras de brilho dourado
        /// (linguagem do gate do Paladino). A versão danificada tem rachaduras maiores.
        /// </summary>
        static Sprite MakeWallSprite(string name, bool cracked) =>
            Draw($"{EnvArt}/{name}.png", 16, 64, set =>
            {
                var stone = new Color32(0x5a, 0x55, 0x6e, 0xff);
                var edge = new Color32(0x3a, 0x36, 0x4a, 0xff);
                var crack = new Color32(0xff, 0xd3, 0x4d, 0xff);
                var crackDark = new Color32(0x8a, 0x62, 0x1e, 0xff);
                for (int b = 0; b < 4; b++) Rect(set, 0, b * 16, 15, b * 16 + 15, stone, edge); // 4 blocos
                int x = 8;
                for (int y = 4; y < 60; y++) // rachadura em zigue-zague
                {
                    x += (y % 6 == 0) ? 1 : (y % 6 == 3 ? -1 : 0);
                    set(x, y, crack);
                    set(x + 1, y, crackDark);
                    if (cracked && y % 9 == 0) for (int k = 1; k <= 4; k++) set(x - k, y + k / 2, crack); // galhos
                }
            });

        static Sprite MakeRubbleSprite() =>
            Draw($"{EnvArt}/env_breakable_wall_rubble.png", 20, 6, set =>
            {
                var stone = new Color32(0x5a, 0x55, 0x6e, 0xff);
                var edge = new Color32(0x3a, 0x36, 0x4a, 0xff);
                Rect(set, 0, 0, 6, 3, stone, edge);
                Rect(set, 5, 0, 12, 5, stone, edge);
                Rect(set, 12, 0, 19, 2, stone, edge);
            });

        /// <summary>
        /// Golden placeholder 24×34 px (maior que o gato): cão dourado de armadura, orelhas caídas, olhando para a
        /// direita, com a marreta apoiada no chão.
        /// </summary>
        static Sprite MakeGoldenSprite() =>
            Draw($"{GoldenArt}/chr_golden_placeholder.png", 24, 34, set =>
            {
                var fur = new Color32(0xe0, 0xa8, 0x4e, 0xff);
                var armor = new Color32(0xc9, 0xcf, 0xdb, 0xff);
                var edge = new Color32(0x4a, 0x30, 0x12, 0xff);
                var eye = new Color32(0x24, 0x18, 0x0c, 0xff);
                foreach (int x in new[] { 7, 8, 13, 14 }) { set(x, 0, edge); set(x, 1, edge); } // pernas
                Rect(set, 5, 2, 16, 22, armor, edge);                  // corpo de armadura
                for (int y = 4; y <= 18; y++) for (int x = 8; x <= 13; x++) set(x, y, Gold); // tabardo
                Rect(set, 6, 22, 16, 31, fur, edge);                   // cabeça
                Rect(set, 16, 23, 19, 26, fur, edge);                  // focinho
                set(19, 26, eye);                                      // nariz
                set(13, 28, eye); set(14, 28, eye);                    // olho
                for (int y = 22; y <= 30; y++) set(5, y, edge);        // orelha caída
                Rect(set, 3, 21, 5, 29, fur, edge);
                for (int y = 0; y <= 14; y++) set(20, y, Wood);        // marreta apoiada
                Rect(set, 18, 14, 23, 18, Steel, edge);
            });
    }
}
