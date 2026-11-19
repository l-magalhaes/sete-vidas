using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F8, a luta com a Isyathy: põe o <see cref="IsyathyBoss"/> na arena F1 (Forest_F_Heart), com os buracos do chão
    /// (colunas sem plataforma em cima), a Vulpis na plataforma alta da direita e os galhos da arena frágeis (o Rugido
    /// Profundo derruba; voltam sozinhos). Os portões de raízes (o teto e a volta para a E6) são atalhos com a flag
    /// <see cref="ForestLayout.BossFlag"/> no ForestLayout. Roda junto do "Floresta: gerar as cenas" e sozinho pelo menu
    /// "Floresta: chefe Isyathy (sem recriar a cena)". Idempotente.
    /// </summary>
    public static class IsyathyBossSetup
    {
        public const string RootName = "Isyathy (chefe)";
        public const string ArenaRoom = "F1";
        /// <summary>Os buracos do chão da F1 (x locais): as colunas livres entre as plataformas.</summary>
        public static readonly float[] Holes = { 9.5f, 14.5f, 19f, 23.5f, 29f };   // a arena de galhos (ForestLayout v33)
        /// <summary>A luta começa com o gato a 6 tiles da entrada.</summary>
        public const float StartLocalX = 6f;
        /// <summary>A Vulpis depois da luta (ela fica): no galho lateral direito (topo 9).</summary>
        public static readonly Vector2 VulpisLocal = new Vector2(32.5f, 9f);
        /// <summary>Onde a Vulpis chega para atirar: os galhos laterais de topo 9 (ela vem no do lado do gato).</summary>
        public static readonly Vector2[] VulpisSpots = { new Vector2(5.5f, 9f), new Vector2(32.5f, 9f) };

        /// <summary>
        /// As conversas da luta com retrato (18/11/2026, pedido do usuário: "esses diálogos, faça ser no modo porta-retrato,
        /// para não perdermos isso"; antes eram balões que sumiam sozinhos). Docs/Narrativa/floresta-roteiro.md
        /// (D-ISYATHY-65, D-ISYATHY-DERROTA, D-ISYATHY-ESPIRITO-01). Conversa obrigatória: no máximo 6 falas cada.
        /// </summary>
        public static (DialogueDefinition first, DialogueDefinition defeat, DialogueDefinition spirit, DialogueDefinition spiritMore, DialogueDefinition farewell) CreateDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var fox = DialogueSetup.VulpisSpeaker();
            var isy = DialogueSetup.IsyathySpiritSpeaker();
            // rev. 2 (19/11/2026): a fala do gato ao ver a casca saiu do balão e abre a conversa
            var first = DialogueSetup.Conversation("D-ISYATHY-65", true, 2,
                (cat, "surpreso", "O que ela fez?! Não entra nada! A praga cobriu ela inteira!"),
                (fox, "serio", "Segura ela, gato! A praga esta protegendo suas escamas"),
                (cat, "ironico", "Segurar? Ela é do tamanho de uma ponte!"),
                (fox, "serio", "A magia da floresta vai remover essa maldição. Me dá um instante!"));
            var defeat = DialogueSetup.Conversation("D-ISYATHY-DERROTA", true, 1,
                (fox, "triste", "..."),
                (cat, "terno", "Ela parou."),
                (fox, "triste", "Eu passei semanas procurando um jeito de salvar ela."),
                (cat, "serio", "Aquela magia esta corrompendo tudo..."));
            var spirit = DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-01", true, 1,
                (isy, "", "Pequenos amigos..."),
                (cat, "surpreso", "Ela... fala?"),
                (isy, "", "Eu falava antes. Mas a doença não deixava ninguém me ouvir."),
                (isy, "", "Vocês me soltaram. A carne era a prisão da praga. Agora eu sou só floresta."),
                (fox, "triste", "Guardiã... me perdoe."),
                (isy, "", "Raposa fiel. Você não errou nenhuma flecha que precisava errar."));
            var spiritMore = DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-01b", true, 1,
                (isy, "", "Você tambem foi tocado pela mesma magia que eu pequeno Gato, mas ainda esta vivo."),
                (isy, "", "Talvez você consiga impedir que ela se alastre mais"));
            var farewell = DialogueSetup.Conversation("D-ISYATHY-FIM", true, 1,
                (fox, "serio", "O caminho de saída é por ali, subindo. Eu vou ficar por enquanto, mas logo devo ir para a cidade."),
                (cat, "ironico", "Obrigado pelas flechas. Não conte para ninguém que um gato precisou de ajuda."),
                (fox, "sorriso", "Ninguém ia acreditar mesmo."));
            AssetDatabase.SaveAssets();
            return (first, defeat, spirit, spiritMore, farewell);
        }

        /// <summary>
        /// As falas da luta que ainda eram balões (pedido do usuário, 19/11/2026: "passar as falas de balão para assets de
        /// diálogo"): a chegada (D-ISYATHY-00), a 2ª casca (D-ISYATHY-30) e o "agora!" depois de cada flecha forte
        /// (D-ISYATHY-65b, D-ISYATHY-30b). Sem os assets, os balões de antes continuam no código.
        /// </summary>
        public static (DialogueDefinition intro, DialogueDefinition second, DialogueDefinition strike1, DialogueDefinition strike2) CreateFightDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var fox = DialogueSetup.VulpisSpeaker();
            var intro = DialogueSetup.Conversation("D-ISYATHY-00", true, 1,
                (cat, "surpreso", "Como ninguem viu uma cobra desse tamanho?"),
                (cat, "ironico", "Tudo bem. Eu já enfrentei um cachorro de ouro. Isso é só um pouco mais... comprido."));
            var second = DialogueSetup.Conversation("D-ISYATHY-30", true, 1,
                (cat, "surpreso", "A casca de novo! E mais grossa!"),
                (fox, "serio", "De novo! A praga está mais forte agora..."),
                (fox, "serio", "Mantém ela ocupada. Essa vai doer nela."));
            var strike1 = DialogueSetup.Conversation("D-ISYATHY-65b", true, 1,
                (fox, "serio", "Agora, gato!"));
            var strike2 = DialogueSetup.Conversation("D-ISYATHY-30b", true, 1,
                (fox, "triste", "A praga está tomando ela. Mais um pouco e não sobra nada da guardiã!"),
                (cat, "serio", "Então vamos acabar com isso. Por ela."));
            AssetDatabase.SaveAssets();
            return (intro, second, strike1, strike2);
        }

        /// <summary>
        /// O espírito pela floresta (D-ISYATHY-ESPIRITO-02, Docs/Narrativa/floresta-roteiro.md): 02a–02d em rodízio e a 02e
        /// só depois da ponte nova. Conversas opcionais.
        /// </summary>
        public static (DialogueDefinition[] talks, DialogueDefinition afterBridge) CreateSpiritDialogues()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var isy = DialogueSetup.IsyathySpiritSpeaker();
            var talks = new[]
            {
                DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-02a", false, 1,
                    (isy, "", "Antes das árvores, havia gente sem pelo nesta terra. Construíam torres de pedra lisa. Eu guardo o que sobrou delas."),
                    (cat, "", "Os Primatas."),
                    (isy, "", "Vocês dão nomes. Eles também davam.")),
                DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-02b", false, 1,
                    (isy, "", "A raposa ainda chora quando acha que ninguém ouve. Seja gentil com ela."),
                    (cat, "ironico", "Eu? Gentil?"),
                    (isy, "", "Você poupou um cão que caiu na sua frente. Eu sinto essas coisas.")),
                DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-02c", false, 1,
                    (isy, "", "Você carrega uma vida só e anda como quem tem sete."),
                    (cat, "ironico", "Força do hábito.")),
                DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-02d", false, 1,
                    (isy, "", "A praga come a carne, não o que vive dentro dela."),
                    (isy, "", "Lembre disso, pequeno que cai, quando tiver que lutar com quem você conhece."),
                    (cat, "serio", "...Vou lembrar.")),
            };
            var bridge = DialogueSetup.Conversation("D-ISYATHY-ESPIRITO-02e", false, 1,
                (isy, "", "Ouvi martelos na beira do vale. A toupeira barulhenta?"),
                (isy, "", "Ela acordaria até a mim."),
                (cat, "ironico", "Ela acordaria até os Primatas."));
            AssetDatabase.SaveAssets();
            return (talks, bridge);
        }

        public static void Apply(Transform root, ForestLayout.Region reg)
        {
            var old = root.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var r = reg.FindRoom(ArenaRoom);
            if (r == null) { Debug.LogError("[Sete Vidas] Isyathy: falta a sala F1 no ForestLayout."); return; }
            var holes = new float[Holes.Length];
            for (int i = 0; i < holes.Length; i++) holes[i] = r.X + 1 + Holes[i];
            var go = new GameObject(RootName);
            go.transform.SetParent(root, false);
            var boss = go.AddComponent<IsyathyBoss>();
            var vulpis = ForestTestSetup.LoadSprite("vulpis");
            var vulpisAnims = VulpisArtSetup.Load();
            if (vulpis == null && vulpisAnims == null) Debug.LogWarning("[Sete Vidas] Isyathy: falta a arte da Vulpis (a Vulpis da arena fica invisível).");
            boss.Configure(new Rect(r.X, r.Y, r.W, r.H), r.Y + 1, holes, r.X + 1 + StartLocalX,
                new Vector2(r.X + 1 + VulpisLocal.x, r.Y + 1 + VulpisLocal.y), System.Array.ConvertAll(VulpisSpots, s => new Vector2(r.X + 1 + s.x, r.Y + 1 + s.y)), vulpis, vulpisAnims);
            var d = CreateDialogues();
            boss.SetDialogues(d.first, d.defeat, d.spirit, d.spiritMore, d.farewell);
            var f = CreateFightDialogues();
            boss.SetFightDialogues(f.intro, f.second, f.strike1, f.strike2);
            EditorUtility.SetDirty(boss);
            foreach (var p in root.GetComponentsInChildren<OneWayPlatform>(true))
                if (p.name.StartsWith(ArenaRoom + ": galho") && p.GetComponent<FragileBranch>() == null) p.gameObject.AddComponent<FragileBranch>();
        }

        /// <summary>Menu: abre a Forest_F_Heart, põe/atualiza a Isyathy e salva (não recria a cena).</summary>
        public static void PatchScene()
        {
            string path = $"{ForestTestSetup.Folder}/{ForestLayout.F}.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != path)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(path);
            }
            Transform root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.transform.Find(ArenaRoom) != null) { root = go.transform; break; }
            if (root == null) { Debug.LogError("[Sete Vidas] Isyathy: não achei a raiz da Forest_F_Heart (com o grupo \"F1\")."); return; }
            Apply(root, ForestLayout.Regions().Find(x => x.Scene == ForestLayout.F));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Sete Vidas] F8: a Isyathy na arena F1 (os portões de raízes vêm do ForestLayout: gere as cenas se ainda não tem).");
        }
    }
}
