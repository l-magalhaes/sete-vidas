using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F9: a Tobi na cabeceira leste (R4, junto da ponte caída) e a ponte de madeira que ela constrói
    /// (<see cref="RebuiltBridge"/>, escondida até a flag). As conversas vêm de Docs/Narrativa/floresta-roteiro.md
    /// (D-TOBI-01 a 05). Roda junto do "Floresta: gerar as cenas" (objetos "tobi" e "bridge_wood" do ForestLayout) e
    /// sozinho pelo menu "Floresta: Tobi e a ponte F9 (sem recriar a cena)".
    /// </summary>
    public static class TobiSetup
    {
        public const string AnimationsPath = "Assets/_Game/ScriptableObjects/Characters/TobiAnimationSet.asset";
        const string NpcName = "Tobi", BridgeName = "Ponte de madeira (Tobi)";

        // ------------------------------------------------------------ as conversas

        public static (DialogueDefinition[] talks, DialogueDefinition[] after) CreateDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var tobi = DialogueSetup.TobiSpeaker();
            var talks = new[]
            {
                DialogueSetup.Conversation("D-TOBI-01", true, 1,
                    (tobi, "animada", "Olha só essa tragédia! Uma ponte inteirinha destruída!"),
                    (cat, "", "Você consegue consertar?"),
                    (tobi, "orgulhosa", "Conseguir? Meu caro, eu sou uma artista da engenharia!"),
                    (tobi, "gananciosa", "Agora... arte custa dinheiro. E madeira. Muita madeira!"),
                    (tobi, "gananciosa", "Doze tábuas de lei e vinte moedas de ouro. Nem uma lasca a menos.")),
                DialogueSetup.Conversation("D-TOBI-01b", true, 1,
                    (cat, "ironico", "Eu já estou do lado certo. Para que eu quero ponte?"),
                    (tobi, "orgulhosa", "Para voltar sem cair, para começar! E quem sabe o que tem do outro lado daqui a umas luas? Uma ponte é um caminho para sempre."),
                    (cat, "ironico", "Eu caí dessa ponte. Devia ganhar desconto."),
                    (tobi, "ofendida", "Desconto é para quem cai de pé. Ah... você caiu de pé? Bom, não muda nada.")),
                DialogueSetup.Conversation("D-TOBI-02", false, 1,
                    (tobi, "ofendida", "Faltam " + SeteVidas.Progression.TobiRules.CountToken + ". E eu não trabalho fiado!"),
                    (tobi, "animada", "Procura nos baús da floresta. Quem guarda madeira boa nunca deixa no chão.")),
                DialogueSetup.Conversation("D-TOBI-03", false, 1,
                    (tobi, "ofendida", "Madeira eu tenho, pagamento não. Vinte moedas, gato. Arte não é caridade.")),
                DialogueSetup.Conversation("D-TOBI-04a", true, 1,
                    (tobi, "gananciosa", "Doze tábuas e vinte moedas! Agora sim, fala a minha língua!")),
                DialogueSetup.Conversation("D-TOBI-04b", true, 1,
                    (tobi, "animada", "Segura... segura... segurei!")),
                DialogueSetup.Conversation("D-TOBI-04c", true, 1,
                    (tobi, "gananciosa", "Essa aqui é a taxa de material.")),
                DialogueSetup.Conversation("D-TOBI-04d", true, 1,
                    (tobi, "orgulhosa", "Garantia de... uns cinquenta anos. Talvez."),
                    (cat, "ironico", "\"Talvez\" é a palavra que eu menos queria ouvir em cima de um abismo."),
                    (cat, "ironico", "Se ela cair, pelo menos eu já sei o caminho.")),
                // 19/11/2026 (pedido do usuário): o convite para a lojinha em Miaravéu, na primeira conversa depois do encontro
                DialogueSetup.Conversation("D-TOBI-06", false, 1,
                    (tobi, "animada", "Ah! Antes que eu me esqueça, gato!"),
                    (tobi, "orgulhosa", "Eu tenho uma lojinha em Miaravéu. Ferramentas, consertos, bugigangas de primeira. Tudo feito por estas mãos!"),
                    (cat, "ironico", "Deixa eu adivinhar. Nada de graça."),
                    (tobi, "gananciosa", "De graça, só o conselho. Mas para quem cai de ponte, eu faço um precinho especial."),
                    (tobi, "animada", "Quando chegar na cidade, passa lá! Procura a placa com uma chave inglesa. A maior da rua, claro.")),
            };
            var after = new[]
            {
                DialogueSetup.Conversation("D-TOBI-05a", false, 2,
                    (tobi, "orgulhosa", "Abri o poço da escada no começo da ponte, lá do lado da Fortaleza. Para descer na floresta sem quebrar o pescoço. De nada."),
                    (cat, "ironico", "Descer sem cair. Que conceito.")),
                DialogueSetup.Conversation("D-TOBI-05b", false, 1,
                    (tobi, "animada", "Se for a Miaravéu, diga que a Tobi faz telhados também. Preço de amiga. Quase.")),
                DialogueSetup.Conversation("D-TOBI-05c", false, 1,
                    (cat, "", "Primeira coisa que eu ajudo a consertar em vez de quebrar."),
                    (tobi, "ofendida", "Não se acostume.")),
            };
            AssetDatabase.SaveAssets();
            return (talks, after);
        }

        // ------------------------------------------------------------ a Tobi

        /// <summary>A Tobi (objeto "tobi" do layout): E fala; o encontro acontece sozinho; a aurora na cabeceira leste.</summary>
        public static void Npc(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject(NpcName) { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3.4f, 2.4f);
            box.offset = new Vector2(0f, 1.2f);
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var look = visualGo.AddComponent<VulpisVisual>();
            var set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(AnimationsPath);
            if (set == null) Debug.LogWarning("[Sete Vidas] Tobi: falta o TobiAnimationSet (rode Tools/CharacterSprites/gerar_tobi.ps1).");
            else
            {
                var idle = set.Get("idle");
                if (idle != null && idle.Count > 0) sr.sprite = idle.frames[0].sprite;
                look.Configure(set, o.Facing < 0, NpcName);
            }
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: falar", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);
            var (talks, after) = CreateDialogues();
            var road = ForestLayout.Regions().Find(x => x.Scene == ForestLayout.Road);
            var r5 = road != null ? road.FindRoom("R5") : null;
            var aurora = r5 != null ? new Rect(r5.X, r5.Y, r5.W, r5.H) : new Rect(o.X, o.Y, 30f, 17f);
            var npc = go.AddComponent<TobiNpc>();
            npc.Configure(talks, after, aurora, prompt, look);
            EditorUtility.SetDirty(npc);
        }

        // ------------------------------------------------------------ a ponte de madeira

        /// <summary>
        /// A ponte de madeira (objeto "bridge_wood": <c>X</c> é a ponta oeste, <c>W</c> o comprimento): blocos de 2 tiles,
        /// como a de pedra, com o desenho variado (nada de padrão repetido) e a corda no começo, do lado da Fortaleza.
        /// </summary>
        public static void Bridge(Transform parent, ForestLayout.Obj o)
        {
            var root = new GameObject(BridgeName);
            root.transform.SetParent(parent, false);
            var art = new Sprite[4];
            for (int v = 0; v < art.Length; v++)
            {
                art[v] = ForestTestSetup.LoadSprite("ponte_madeira_" + v);
                if (art[v] == null) Debug.LogWarning($"[Sete Vidas] Tobi: falta ponte_madeira_{v}.png (rode Tools/CharacterSprites/gerar_tobi.ps1).");
            }
            int n = Mathf.Max(1, Mathf.RoundToInt(o.W / 2f));
            var blocks = new Transform[n];
            int prev = -1;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"Tábuas {i + 1}") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Ground) };
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(o.X + i * 2f + 1f, o.Y - 0.5f, 0f);
                go.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, RebuiltBridge.VisualOffset, 0f);
                var sr = visual.AddComponent<SpriteRenderer>();
                // sorteio fixo (o mesmo a cada geração), sem repetir o vizinho
                int v = (int)(Mathf.PerlinNoise(i * 0.61f, 3.7f) * 4f) % 4;
                if (v == prev) v = (v + 1 + i % 3) % 4;
                prev = v;
                sr.sprite = art[v];
                sr.sortingOrder = 1;
                blocks[i] = go.transform;
            }
            // (a corda pendurada saiu em 18/11/2026: o atalho para o vale é a escada de madeira do ForestLayout, na R4)
            root.AddComponent<RebuiltBridge>().Configure(blocks, null);
        }

        // ------------------------------------------------------------ menu

        /// <summary>Menu: abre a Forest_Road, põe/atualiza a Tobi e a ponte de madeira e salva (não recria a cena).</summary>
        public static void PatchScene()
        {
            string path = $"{ForestTestSetup.Folder}/{ForestLayout.Road}.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path);
            var road = ForestLayout.Regions().Find(x => x.Scene == ForestLayout.Road);
            Transform root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.transform.Find("R4") != null || go.GetComponentInChildren<BridgeCollapse>(true) != null) { root = go.transform; break; }
            if (root == null || road == null) { Debug.LogError("[Sete Vidas] Tobi: não achei a raiz da Forest_Road (gere as cenas)."); return; }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && (t.name == NpcName || t.name == BridgeName) && t.parent != null) Object.DestroyImmediate(t.gameObject);
            int made = 0;
            foreach (var o in road.Objects)
            {
                if (o.Kind == "tobi") { Npc(root, o); made++; }
                else if (o.Kind == "bridge_wood") { Bridge(root, o); made++; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Sete Vidas] F9: a Tobi e a ponte de madeira na {ForestLayout.Road} ({made} objetos). Falta: o ForestLayout v34 (gere as cenas) se não apareceu nada.");
        }
    }
}
