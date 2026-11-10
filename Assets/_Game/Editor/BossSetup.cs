using System.IO;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
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
    /// Chefão Golden: sprites dos golpes, relâmpago, grades, tomo, prefabs, conversas da arena, barra de vida
    /// no HUD e o trecho J da Playground (arena + sala do tomo). Spec: Docs/Design/chefe-golden.md.
    /// </summary>
    public static class BossSetup
    {
        const string GoldenArt = "Assets/_Game/Art/Characters/Golden";
        const string EnvArt = "Assets/_Game/Art/Environments/Placeholder";
        const string ContractorArt = "Assets/_Game/Art/Characters/Contractor";
        const string KodaAnimationsPath = "Assets/_Game/ScriptableObjects/Characters/KodaAnimationSet.asset";
        const string ContractorAnimationsPath = "Assets/_Game/ScriptableObjects/Characters/ContractorAnimationSet.asset";
        const string WhiteSquare = "Assets/_Game/Art/Environments/Placeholder/fx_shadow_zone.png";
        const string SettingsPath = "Assets/_Game/ScriptableObjects/Enemies/GoldenBossSettings.asset";
        public const string BossPrefabPath = "Assets/_Game/Prefabs/Enemies/GoldenBoss.prefab";
        const string LightningPrefabPath = "Assets/_Game/Prefabs/Enemies/LightningStrike.prefab";
        const string GatePrefabPath = "Assets/_Game/Prefabs/Interactables/ArenaGate.prefab";
        const string TomePrefabPath = "Assets/_Game/Prefabs/Interactables/CursedTome.prefab";

        // Trecho J da Playground (tiles em SeteVidasSetup.CreatePlayground)
        public const float ArenaMinX = 180f, ArenaMaxX = 211f, ArenaFloorY = 5f;

        [MenuItem("Tools/Sete Vidas/10. Chefão: Golden, arena e tomo", priority = 46)]
        public static void RunBossSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreatePrefabs();
            ProgressionSetup.CreateHudPrefab(); // barra de vida do chefe
            SeteVidasSetup.CreateScenes();
            Debug.Log("[Sete Vidas] Chefão pronto: suba a laje (x 156) e siga para a direita até a arena (x 180–211). " +
                      "Apague o save (Tools > Sete Vidas > Save) para começar com 7 corações.");
        }

        public static void CreatePrefabs()
        {
            CreateLightningPrefab();
            CreateGatePrefab();
            CreateTomePrefab();
            CreateBossPrefab();
        }

        // ----------------------------------------------------------- conversas

        public struct ArenaDialogues { public DialogueDefinition Intro, Defeat, Take, Seal, Curse, Villain, VillainLeave; }

        public static ArenaDialogues CreateDialogues()
        {
            var (cat, golden) = DialogueSetup.Speakers();
            var capy = DialogueSetup.ContractorSpeaker();
            var result = new ArenaDialogues
            {
                Intro = DialogueSetup.Conversation("D-GOLDEN-00", true, 3,
                    (golden, "serio", "Alto, ladrão. Esse tomo dorme selado há cinco séculos. E vai continuar dormindo."),
                    (cat, "ironico", "Engraçado. Meu contratante jurou que ele estava bem acordado."),
                    (golden, "", "Então volte e diga a ele que encontrou um paladino no caminho.")),
                Defeat = DialogueSetup.Conversation("D-GOLDEN-DERROTA", true, 2,
                    (golden, "ferido", "Chega... Você é rápido, admito. Rápido demais para o próprio bem."),
                    (golden, "ferido", "Pegue, se precisa. Mas o selo cobra um preço que ninguém paga duas vezes."),
                    (cat, "ironico", "Todo mundo diz isso. Ninguém cobra.")),
                Take = DialogueSetup.Conversation("D-TOMO-01", true, 2,
                    (null, "", "O tomo é pesado e frio. As runas acendem sob as suas patas."),
                    (cat, "", "...Ele está me olhando?"),
                    (null, "", "O selo se rompe com um estalo seco. Algo puxa o ar dos seus pulmões.")),
                // Koda, derrotado, crava a marreta no chão e segura a última vida (Lore Oficial §X)
                Seal = DialogueSetup.Conversation("D-KODA-SELO", true, 2,
                    (null, "", "A última centelha vacila. O roxo avança sobre ela."),
                    (golden, "serio", "Não! Nenhuma morte reclama o que ainda pertence à vida!")),
                Curse = DialogueSetup.Conversation("D-TOMO-02", true, 3,
                    (null, "", "Um clarão dourado detém a maldição. O paladino tomba, sem forças."),
                    (null, "", "Seis das suas sete vidas se foram. Resta uma última centelha."),
                    (cat, "", "Não... não, não, não. Isso não estava no contrato.")),
                // O contratante (capivara necromante) aparece com o gato caído
                Villain = DialogueSetup.Conversation("D-CAPIVARA-01", true, 4,
                    (capy, "deboche", "Ah, estava sim. Cláusula sete. Você leu... e achou graça."),
                    (cat, "", "Você... o contratante. O que você fez comigo?"),
                    (capy, "", "Eu? Nada. O tomo dorme há cinco séculos e só acorda bebendo vidas."),
                    (capy, "revelacao", "Sete vidas de gato. Raras. Deliciosas. Exatamente o que ele precisava."),
                    (cat, "", "Você me usou... desde o começo."),
                    (capy, "deboche", "Como se usa uma chave, gatinho. E chaves não reclamam da porta.")),
                VillainLeave = DialogueSetup.Conversation("D-CAPIVARA-02", true, 2,
                    (null, "", "Uma moeda de ouro tilinta no chão, ao lado do seu focinho."),
                    (capy, "deboche", "O pagamento está feito pelos seus serviços. Não diga que não sou honesto."),
                    (capy, "deboche", "Adeus. Aproveite a sua última vida. Ela não vai durar."),
                    (null, "", "Ele some numa névoa roxa, levando o tomo. Só a moeda fica para trás.")),
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        // ------------------------------------------------------------- cena

        /// <summary>Posições de uma arena do Golden (tudo em tiles, no mundo).</summary>
        public struct ArenaLayout
        {
            public float MinX, MaxX, FloorY, CeilingHeight;
            public float EntryGateX, ExitGateX, BossX, TomeX, SanctuaryX;
            public string SanctuaryId, SanctuaryName;
        }

        /// <summary>Arena, Golden, grades, tomo e santuário do trecho J (chamado ao recriar a Playground).</summary>
        public static void PlaceInPlayground(Scene scene) => PlaceArena(scene, new ArenaLayout
        {
            MinX = ArenaMinX, MaxX = ArenaMaxX, FloorY = ArenaFloorY, CeilingHeight = 9f,
            EntryGateX = 179.5f, ExitGateX = 211.5f, BossX = 200f, TomeX = 216.5f, SanctuaryX = 172f,
            SanctuaryId = "playground_arena", SanctuaryName = "Antes da arena",
        });

        /// <summary>Monta a arena em qualquer cena (Playground e Fortaleza).</summary>
        public static void PlaceArena(Scene scene, ArenaLayout a)
        {
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (bossPrefab == null) { CreatePrefabs(); bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath); }
            var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath);
            var tomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TomePrefabPath);
            var dialogues = CreateDialogues();

            var parent = new GameObject("BossSection");
            SceneManager.MoveGameObjectToScene(parent, scene);

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, parent.transform);
            boss.name = "Golden (chefão)";
            boss.transform.position = new Vector3(a.BossX, a.FloorY, 0f);
            var bso = new SerializedObject(boss.GetComponent<GoldenBoss>());
            bso.FindProperty("arenaMinX").floatValue = a.MinX;
            bso.FindProperty("arenaMaxX").floatValue = a.MaxX;
            bso.FindProperty("lightningHeight").floatValue = a.CeilingHeight;
            bso.ApplyModifiedPropertiesWithoutUndo();

            var entry = Gate(gatePrefab, parent.transform, "Grade de entrada", a.EntryGateX, a.FloorY, closed: false);
            var exit = Gate(gatePrefab, parent.transform, "Grade do tomo", a.ExitGateX, a.FloorY, closed: true);

            // Gatilho da arena: entrou, a grade fecha
            var arenaGo = new GameObject("Arena") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            arenaGo.transform.SetParent(parent.transform, false);
            arenaGo.transform.position = new Vector3((a.MinX + a.MaxX) * 0.5f, a.FloorY + 4f, 0f);
            var trigger = arenaGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(a.MaxX - a.MinX - 5f, 8f); // começa uns tiles depois da grade: o gato já está dentro
            var arena = arenaGo.AddComponent<BossArena>();
            var aso = new SerializedObject(arena);
            aso.FindProperty("boss").objectReferenceValue = boss.GetComponent<GoldenBoss>();
            aso.FindProperty("entryGate").objectReferenceValue = entry;
            aso.FindProperty("exitGate").objectReferenceValue = exit;
            aso.FindProperty("introDialogue").objectReferenceValue = dialogues.Intro;
            aso.FindProperty("defeatDialogue").objectReferenceValue = dialogues.Defeat;
            aso.ApplyModifiedPropertiesWithoutUndo();

            var tome = (GameObject)PrefabUtility.InstantiatePrefab(tomePrefab, parent.transform);
            tome.name = "Tomo corrompido";
            tome.transform.position = new Vector3(a.TomeX, a.FloorY, 0f);
            var tso = new SerializedObject(tome.GetComponent<CursedTome>());
            tso.FindProperty("golden").objectReferenceValue = boss.GetComponent<GoldenBoss>();
            tso.FindProperty("goldenBond").objectReferenceValue = boss.GetComponentInChildren<CompanionBond>(true);
            tso.FindProperty("takeDialogue").objectReferenceValue = dialogues.Take;
            tso.FindProperty("sealDialogue").objectReferenceValue = dialogues.Seal;
            tso.FindProperty("curseDialogue").objectReferenceValue = dialogues.Curse;
            tso.FindProperty("villainDialogue").objectReferenceValue = dialogues.Villain;
            tso.FindProperty("villainLeaveDialogue").objectReferenceValue = dialogues.VillainLeave;
            tso.ApplyModifiedPropertiesWithoutUndo();

            // Santuário antes da arena, em cima da laje
            var sanctuary = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.SanctuaryPrefabPath);
            if (sanctuary != null)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(sanctuary, parent.transform);
                s.name = $"Santuário ({a.SanctuaryId})";
                s.transform.position = new Vector3(a.SanctuaryX, a.FloorY, 0f);
                var sso = new SerializedObject(s.GetComponent<Sanctuary>());
                sso.FindProperty("sanctuaryId").stringValue = a.SanctuaryId;
                sso.FindProperty("displayName").stringValue = a.SanctuaryName;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static ArenaGate Gate(GameObject prefab, Transform parent, string name, float x, float floorY, bool closed)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = new Vector3(x, floorY, 0f);
            var gate = go.GetComponent<ArenaGate>();
            var so = new SerializedObject(gate);
            so.FindProperty("startsClosed").boolValue = closed;
            so.ApplyModifiedPropertiesWithoutUndo();
            return gate;
        }

        // ------------------------------------------------------------ prefabs

        static GameObject CreateBossPrefab()
        {
            Directory.CreateDirectory(GoldenArt);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<GoldenBossSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GoldenBossSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var root = new GameObject("GoldenBoss") { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = settings.BodySize;
            box.offset = new Vector2(0f, settings.BodySize.y * 0.5f);

            var visual = Child(root, "Visual", Vector3.zero);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            var auraGo = Child(root, "HealAura", Vector3.zero);
            var aura = auraGo.AddComponent<SpriteRenderer>();
            aura.sprite = AuraSprite();
            aura.color = new Color(1f, 0.85f, 0.4f, 0.4f);
            aura.sortingOrder = 3;
            aura.enabled = false;

            var boss = root.AddComponent<GoldenBoss>();
            var so = new SerializedObject(boss);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("arenaMinX").floatValue = ArenaMinX;
            so.FindProperty("arenaMaxX").floatValue = ArenaMaxX;
            so.FindProperty("lightningHeight").floatValue = 9f;
            so.FindProperty("lightningPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LightningStrike>(LightningPrefabPath);
            so.FindProperty("playerLayers").intValue = LayerMask.GetMask(GameConstants.Layers.Player);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("healAura").objectReferenceValue = aura;
            var idle = Pose("chr_golden_boss_idle", GoldenPose.Idle);
            sr.sprite = idle;
            so.FindProperty("idle").objectReferenceValue = idle;
            so.FindProperty("swingWindup").objectReferenceValue = Pose("chr_golden_boss_swing_windup", GoldenPose.SwingWindup);
            so.FindProperty("swing").objectReferenceValue = Pose("chr_golden_boss_swing", GoldenPose.Swing);
            so.FindProperty("leapCrouch").objectReferenceValue = Pose("chr_golden_boss_leap_crouch", GoldenPose.LeapCrouch);
            so.FindProperty("leapAir").objectReferenceValue = Pose("chr_golden_boss_leap_air", GoldenPose.LeapAir);
            so.FindProperty("heal").objectReferenceValue = Pose("chr_golden_boss_heal", GoldenPose.Heal);
            so.FindProperty("cast").objectReferenceValue = Pose("chr_golden_boss_cast", GoldenPose.Cast);
            so.FindProperty("kneel").objectReferenceValue = Pose("chr_golden_boss_kneel", GoldenPose.Kneel);
            var walk = so.FindProperty("walk");
            walk.arraySize = 2;
            walk.GetArrayElementAtIndex(0).objectReferenceValue = Pose("chr_golden_boss_walk_1", GoldenPose.WalkA);
            walk.GetArrayElementAtIndex(1).objectReferenceValue = Pose("chr_golden_boss_walk_2", GoldenPose.WalkB);
            // arte do Koda (Tools/CharacterSprites/gerar.ps1); sem ela, ficam as poses provisórias acima
            var koda = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(KodaAnimationsPath);
            so.FindProperty("animations").objectReferenceValue = koda;
            if (koda != null && koda.Get("idle") != null) sr.sprite = koda.Get("idle").frames[0].sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Companheiro (depois da maldição): conversa de vínculo e falas seguintes. Desligado até lá.
            var companion = Child(root, "Companion", Vector3.zero);
            companion.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            var talk = companion.AddComponent<BoxCollider2D>();
            talk.isTrigger = true;
            talk.size = new Vector2(3f, 2.2f);
            talk.offset = new Vector2(0f, 1.1f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(Child(companion, "Prompt", new Vector3(0f, 2.8f, 0f)), "E: falar", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            prompt.gameObject.SetActive(false);
            var bond = companion.AddComponent<CompanionBond>();
            var dialogues = DialogueSetup.CreateDialogueAssets();
            var bso = new SerializedObject(bond);
            bso.FindProperty("prompt").objectReferenceValue = prompt;
            bso.FindProperty("bondDialogue").objectReferenceValue = dialogues.Bond;
            bso.FindProperty("bondFollowUp").objectReferenceValue = dialogues.BondFollowUp;
            bso.FindProperty("unlockDialogue").objectReferenceValue = dialogues.Unlock;
            var idleProp = bso.FindProperty("idleDialogues");
            idleProp.arraySize = dialogues.Idle.Length;
            for (int i = 0; i < dialogues.Idle.Length; i++) idleProp.GetArrayElementAtIndex(i).objectReferenceValue = dialogues.Idle[i];
            bso.ApplyModifiedPropertiesWithoutUndo();
            bond.enabled = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateLightningPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LightningPrefabPath));
            var root = new GameObject("LightningStrike");
            var tele = Child(root, "Telegraph", Vector3.zero).AddComponent<SpriteRenderer>();
            tele.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquare);
            tele.color = new Color(1f, 0.95f, 0.6f, 0.15f);
            tele.sortingOrder = 9;
            var bolt = Child(root, "Bolt", Vector3.zero).AddComponent<SpriteRenderer>();
            bolt.sprite = BoltSprite();
            bolt.sortingOrder = 9;
            bolt.enabled = false;
            var strike = root.AddComponent<LightningStrike>();
            var so = new SerializedObject(strike);
            so.FindProperty("telegraph").objectReferenceValue = tele;
            so.FindProperty("bolt").objectReferenceValue = bolt;
            so.FindProperty("playerLayers").intValue = LayerMask.GetMask(GameConstants.Layers.Player);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, LightningPrefabPath);
            Object.DestroyImmediate(root);
        }

        static void CreateGatePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GatePrefabPath));
            var root = new GameObject("ArenaGate") { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);
            var bars = Child(root, "Bars", new Vector3(0f, 2f, 0f));
            var sr = bars.AddComponent<SpriteRenderer>();
            sr.sprite = GateSprite();
            sr.sortingOrder = -1; // atrás dos tiles: some dentro da parede ao abrir
            var gate = root.AddComponent<ArenaGate>();
            var so = new SerializedObject(gate);
            so.FindProperty("bars").objectReferenceValue = bars.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GatePrefabPath);
            Object.DestroyImmediate(root);
        }

        static void CreateTomePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TomePrefabPath));
            Directory.CreateDirectory(ContractorArt);
            AssetDatabase.Refresh();
            var root = new GameObject("CursedTome") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.5f, 2f);
            box.offset = new Vector2(0f, 1f);
            var pedestal = Child(root, "Pedestal", new Vector3(0f, 10f / 16f, 0f)).AddComponent<SpriteRenderer>();
            pedestal.sprite = PedestalSprite();
            pedestal.sortingOrder = 2;
            var book = Child(root, "Book", new Vector3(0f, 1.65f, 0f)).AddComponent<SpriteRenderer>();
            book.sprite = BookSprite();
            book.sortingOrder = 3;
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(Child(root, "Prompt", new Vector3(0f, 2.6f, 0f)), "E: pegar o tomo", new Color32(0xc8, 0xa8, 0xff, 0xff), 30);
            prompt.gameObject.SetActive(false);
            // O contratante (escondido até a cena da maldição), o tomo nas patas dele e a moeda de ouro
            var contractorGo = Child(root, "Contratante", Vector3.zero);
            var contractor = contractorGo.AddComponent<SpriteRenderer>();
            contractor.sprite = ContractorSprite();
            contractor.sortingOrder = 4;
            // arte do Contratante: surge na névoa, pega o tomo com a mão de osso, joga a moeda e some
            var capyArt = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(ContractorAnimationsPath);
            if (capyArt != null)
            {
                var visual = contractorGo.AddComponent<ContractorVisual>();
                var vso = new SerializedObject(visual);
                vso.FindProperty("animations").objectReferenceValue = capyArt;
                vso.ApplyModifiedPropertiesWithoutUndo();
                if (capyArt.Get("idle") != null) contractor.sprite = capyArt.Get("idle").frames[0].sprite;
            }
            var held = Child(contractorGo, "Tomo nas patas", new Vector3(-0.55f, 1.05f, 0f)).AddComponent<SpriteRenderer>();
            held.sprite = book.sprite;
            held.sortingOrder = 5;
            held.enabled = false;
            contractorGo.SetActive(false);
            var coin = Child(root, "Moeda de ouro", new Vector3(-1.4f, 0.05f, 0f));
            var coinSr = coin.AddComponent<SpriteRenderer>();
            coinSr.sprite = CoinSprite();
            coinSr.sortingOrder = 6;
            coin.SetActive(false);

            var tome = root.AddComponent<CursedTome>();
            var so = new SerializedObject(tome);
            so.FindProperty("book").objectReferenceValue = book;
            so.FindProperty("contractor").objectReferenceValue = contractor;
            so.FindProperty("contractorBook").objectReferenceValue = held;
            so.FindProperty("coin").objectReferenceValue = coin.transform;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TomePrefabPath);
            Object.DestroyImmediate(root);
        }

        static GameObject Child(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        // -------------------------------------------------------- barra de vida

        /// <summary>Barra de vida do chefe no topo do HUD (chamado pelo ProgressionSetup.CreateHudPrefab).</summary>
        public static void BuildBossBar(GameObject hudRoot)
        {
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquare);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = (RectTransform)new GameObject("BossBar", typeof(RectTransform)).transform;
            root.SetParent(hudRoot.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -8f);
            root.sizeDelta = new Vector2(220f, 22f);

            var title = UiChild(root, "Title", new Vector2(0f, 0f), new Vector2(220f, 11f), new Vector2(0.5f, 1f)).gameObject.AddComponent<Text>();
            title.font = font;
            title.fontSize = 9;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color32(0xff, 0xe9, 0xa8, 0xff);
            title.raycastTarget = false;

            var frame = UiChild(root, "Frame", new Vector2(0f, -12f), new Vector2(220f, 8f), new Vector2(0.5f, 1f)).gameObject.AddComponent<Image>();
            frame.sprite = white;
            frame.color = new Color32(0x1c, 0x14, 0x10, 0xff);
            frame.raycastTarget = false;
            var trail = Bar(frame.rectTransform, "Trail", white, new Color32(0xff, 0xf0, 0xc0, 0xff));
            var fill = Bar(frame.rectTransform, "Fill", white, new Color32(0xd9, 0xa4, 0x41, 0xff));

            var bar = hudRoot.AddComponent<BossHealthBar>();
            var so = new SerializedObject(bar);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("trail").objectReferenceValue = trail;
            so.FindProperty("title").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
        }

        static RectTransform UiChild(RectTransform parent, string name, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Image Bar(RectTransform parent, string name, Sprite sprite, Color color)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------ sprites

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

        static void Line(System.Action<int, int, Color32> set, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                set(x0, y0, c); set(x0 + 1, y0, c); // traço de 2 px
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        enum GoldenPose { Idle, WalkA, WalkB, SwingWindup, Swing, LeapCrouch, LeapAir, Heal, Cast, Kneel }

        static readonly Color32 Fur = new Color32(0xe0, 0xa8, 0x4e, 0xff);
        static readonly Color32 FurDark = new Color32(0xb9, 0x7a, 0x2e, 0xff);
        static readonly Color32 Armor = new Color32(0xc9, 0xcf, 0xdb, 0xff);
        static readonly Color32 Gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
        static readonly Color32 Edge = new Color32(0x4a, 0x30, 0x12, 0xff);
        static readonly Color32 Eye = new Color32(0x24, 0x18, 0x0c, 0xff);
        static readonly Color32 Wood = new Color32(0x6b, 0x4a, 0x2b, 0xff);
        static readonly Color32 Steel = new Color32(0xb8, 0xbf, 0xcc, 0xff);
        static readonly Color32 Spark = new Color32(0xff, 0xf2, 0x8a, 0xff);

        /// <summary>
        /// Golden chefão, canvas 40×40 (pivot nos pés), olhando para a direita. Mesmo cão do vínculo
        /// (armadura, tabardo dourado, orelha caída); só a marreta e a postura mudam entre os golpes.
        /// </summary>
        static Sprite Pose(string name, GoldenPose pose) => Draw($"{GoldenArt}/{name}.png", 40, 40, set =>
        {
            int k = pose switch { GoldenPose.Heal or GoldenPose.Kneel => -5, GoldenPose.LeapCrouch => -3, _ => 0 };

            // Pernas
            if (pose == GoldenPose.Heal || pose == GoldenPose.Kneel || pose == GoldenPose.LeapCrouch) Rect(set, 14, 0, 25, 2, Armor, Edge);
            else if (pose != GoldenPose.LeapAir)
            {
                int[] legs = pose == GoldenPose.WalkA ? new[] { 15, 16, 23, 24 } : pose == GoldenPose.WalkB ? new[] { 18, 19, 20, 21 } : new[] { 16, 17, 22, 23 };
                foreach (int x in legs) { set(x, 0, Edge); set(x, 1, Edge); }
            }

            int by = pose == GoldenPose.LeapAir ? 3 : 0; // no ar: pernas encolhidas
            Rect(set, 13, 2 + k + by, 26, 22 + k + by, Armor, Edge);                      // armadura
            for (int y = 4 + k + by; y <= 18 + k + by; y++) for (int x = 16; x <= 23; x++) set(x, y, Gold); // tabardo
            int hy = 22 + k + by;
            Rect(set, 11, hy - 1, 13, hy + 7, FurDark, Edge);                                // orelha caída
            Rect(set, 14, hy, 26, hy + 9, Fur, Edge);                                        // cabeça
            Rect(set, 26, hy + 1, 29, hy + 4, Fur, Edge);                                    // focinho
            set(29, hy + 4, Eye);                                                            // nariz
            if (pose == GoldenPose.Heal || pose == GoldenPose.Kneel) { set(21, hy + 5, Edge); set(22, hy + 5, Edge); set(23, hy + 5, Edge); } // olhos fechados
            else { set(22, hy + 6, Eye); set(23, hy + 6, Eye); }

            // Marreta
            switch (pose)
            {
                case GoldenPose.SwingWindup: // erguida atrás da cabeça
                    Line(set, 15, 20, 7, 33, Wood);
                    Rect(set, 2, 32, 11, 38, Steel, Edge);
                    for (int x = 3; x <= 10; x++) set(x, 35, Gold);
                    break;
                case GoldenPose.Swing: // batendo à frente, no chão
                    Line(set, 25, 14, 33, 6, Wood);
                    Rect(set, 32, 0, 39, 8, Steel, Edge);
                    for (int y = 1; y <= 7; y++) set(35, y, Gold);
                    break;
                case GoldenPose.LeapCrouch:
                case GoldenPose.LeapAir: // acima da cabeça, com as duas mãos
                case GoldenPose.Cast:
                    int top = hy + 9;
                    for (int y = top; y <= Mathf.Min(top + 3, 33); y++) { set(20, y, Wood); set(21, y, Wood); }
                    Rect(set, 15, 33, 26, 39, Steel, Edge);
                    for (int x = 16; x <= 25; x++) set(x, 36, Gold);
                    if (pose == GoldenPose.Cast) // faíscas do céu
                        foreach (var (x, y) in new[] { (13, 37), (28, 38), (12, 34), (29, 34), (20, 39) }) set(x, y, Spark);
                    break;
                case GoldenPose.Kneel: // largada no chão
                    for (int x = 27; x <= 35; x++) { set(x, 0, Wood); set(x, 1, Wood); }
                    Rect(set, 35, 0, 39, 4, Steel, Edge);
                    break;
                case GoldenPose.Heal: // cravada à frente, mãos no cabo
                    for (int y = 0; y <= 14; y++) { set(30, y, Wood); set(31, y, Wood); }
                    Rect(set, 28, 13, 33, 18, Steel, Edge);
                    break;
                default: // parado / andando: apoiada no chão à frente
                    for (int y = 0; y <= 14; y++) { set(31, y, Wood); set(32, y, Wood); }
                    Rect(set, 29, 14, 34, 18, Steel, Edge);
                    for (int x = 30; x <= 33; x++) set(x, 16, Gold);
                    break;
            }
        });

        /// <summary>Anel dourado 48×48 (pivot nos pés) que pulsa durante a cura.</summary>
        static Sprite AuraSprite() => Draw($"{GoldenArt}/fx_heal_aura.png", 48, 48, set =>
        {
            for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float d = Mathf.Sqrt((x - 23.5f) * (x - 23.5f) + (y - 23.5f) * (y - 23.5f));
                if (d <= 23f && d >= 17f) set(x, y, new Color32(0xff, 0xe0, 0x80, (byte)(d > 20f ? 255 : 140)));
                else if (d < 17f && (x * 7 + y * 13) % 29 == 0) set(x, y, Spark); // brilhos dentro
            }
        });

        /// <summary>Raio 16×64 (1 × 4 tiles; esticado até o teto): núcleo branco em zigue-zague e bordas amarelas.</summary>
        static Sprite BoltSprite() => Draw($"{EnvArt}/fx_lightning_bolt.png", 16, 64, set =>
        {
            var core = new Color32(0xff, 0xff, 0xff, 0xff);
            var glow = new Color32(0xff, 0xe9, 0x5c, 0xff);
            var halo = new Color32(0xff, 0xe9, 0x5c, 0x60);
            int x = 8;
            for (int y = 63; y >= 0; y--)
            {
                if (y % 7 == 0) x += (y / 7) % 2 == 0 ? 2 : -2;
                for (int dx = -4; dx <= 4; dx++) set(x + dx, y, halo);
                set(x - 1, y, glow); set(x + 2, y, glow);
                set(x, y, core); set(x + 1, y, core);
            }
        });

        /// <summary>Grade 16×64 (1 × 4 tiles): três barras de ferro, travessas e pontas embaixo.</summary>
        static Sprite GateSprite() => Draw($"{EnvArt}/env_arena_gate.png", 16, 64, set =>
        {
            var iron = new Color32(0x4a, 0x4f, 0x5c, 0xff);
            var shine = new Color32(0x7a, 0x80, 0x90, 0xff);
            var dark = new Color32(0x22, 0x24, 0x2c, 0xff);
            foreach (int bx in new[] { 2, 7, 12 })
                for (int y = 2; y < 64; y++) { set(bx, y, dark); set(bx + 1, y, iron); set(bx + 2, y, shine); }
            foreach (int ty in new[] { 14, 34, 54 })
                for (int x = 1; x <= 15; x++) { set(x, ty, dark); set(x, ty + 1, iron); }
            foreach (int bx in new[] { 3, 8, 13 }) { set(bx, 0, iron); set(bx, 1, iron); } // pontas
        });

        /// <summary>
        /// Contratante 24×30 (pivot nos pés, olhando para a direita): capivara gorda num manto roxo de necromante,
        /// olho com o brilho verde da praga e corrente de ouro.
        /// </summary>
        static Sprite ContractorSprite() => Draw(ContractorArt + "/chr_contractor_placeholder.png", 24, 30, set =>
        {
            var robe = new Color32(0x4a, 0x22, 0x66, 0xff);
            var robeEdge = new Color32(0x22, 0x10, 0x2e, 0xff);
            var fur = new Color32(0x8a, 0x5a, 0x3a, 0xff);
            var outline = new Color32(0x2a, 0x16, 0x0c, 0xff);
            var glow = new Color32(0x8f, 0xd1, 0x4f, 0xff);
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
            Rect(set, 4, 0, 18, 18, robe, robeEdge);           // manto (corpo largo de capivara)
            for (int y = 2; y <= 16; y += 3) set(11, y, robeEdge);
            Rect(set, 6, 17, 18, 26, fur, outline);            // cabeça
            Rect(set, 17, 18, 22, 23, fur, outline);           // focinho largo
            set(21, 22, outline); set(21, 21, outline);        // narina
            set(15, 24, glow); set(16, 24, glow);              // olho com o brilho da praga
            set(8, 27, outline); set(9, 27, outline);          // orelhinha
            for (int x = 4; x <= 18; x++) if (x % 2 == 0) set(x, 25 + (x > 11 ? 1 : 0), robe); // capuz
            for (int x = 6; x <= 16; x += 2) set(x, 16, gold); // corrente de ouro
        });

        /// <summary>Moeda de ouro 6×6 com brilho: o "pagamento" do contratante.</summary>
        static Sprite CoinSprite() => Draw(EnvArt + "/item_gold_coin.png", 6, 6, set =>
        {
            var gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
            var shine = new Color32(0xff, 0xf2, 0x8a, 0xff);
            var edge = new Color32(0x8a, 0x62, 0x1e, 0xff);
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
            {
                float d = (x - 2.5f) * (x - 2.5f) + (y - 2.5f) * (y - 2.5f);
                if (d <= 6.5f) set(x, y, d > 4f ? edge : gold);
            }
            set(2, 3, shine); set(3, 4, shine);
        });

        /// <summary>Pedestal de pedra 24×20 com faixa de runas apagadas.</summary>
        static Sprite PedestalSprite() => Draw($"{EnvArt}/env_tome_pedestal.png", 24, 20, set =>
        {
            var stone = new Color32(0x5a, 0x55, 0x6e, 0xff);
            var edge = new Color32(0x2a, 0x26, 0x38, 0xff);
            var rune = new Color32(0x45, 0x3a, 0x55, 0xff);
            Rect(set, 2, 0, 21, 3, stone, edge);    // base
            Rect(set, 6, 3, 17, 16, stone, edge);   // coluna
            Rect(set, 3, 16, 20, 19, stone, edge);  // tampo
            for (int y = 6; y <= 13; y += 2) set(11, y, rune);
            for (int y = 7; y <= 13; y += 2) set(12, y, rune);
        });

        /// <summary>
        /// Tomo 14×12: capa roxa-escura com runas verde-ácido (as cores exclusivas da praga, regra de arte)
        /// e um olho no centro.
        /// </summary>
        static Sprite BookSprite() => Draw($"{EnvArt}/env_cursed_tome.png", 14, 12, set =>
        {
            var cover = new Color32(0x5b, 0x2a, 0x6e, 0xff);
            var edge = new Color32(0x22, 0x0e, 0x2a, 0xff);
            var plague = new Color32(0x8f, 0xd1, 0x4f, 0xff);
            var pages = new Color32(0xe8, 0xe0, 0xc8, 0xff);
            Rect(set, 0, 1, 13, 11, cover, edge);
            for (int x = 1; x <= 12; x++) set(x, 0, pages);           // páginas
            foreach (var (x, y) in new[] { (2, 3), (2, 9), (11, 3), (11, 9), (4, 6), (9, 6) }) set(x, y, plague); // runas
            set(6, 6, plague); set(7, 6, plague); set(6, 7, edge); set(7, 5, edge); // olho
        });
    }
}
