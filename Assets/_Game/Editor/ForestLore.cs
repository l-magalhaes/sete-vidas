using System.Collections.Generic;
using System.IO;
using SeteVidas.Dialogue;
using SeteVidas.World;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Floresta, F5 parte 2: os textos das placas dos Primatas (P-PRIM-xx) e o catálogo de placas do Diário
    /// (<see cref="LoreBook"/>). Roteiro: Docs/Narrativa/floresta-roteiro.md, "Ruínas dos Primatas". Sugerem, não
    /// explicam: o gato não sabe ler a escrita dos Primatas; ele descreve o que vê.
    /// </summary>
    public static class ForestLore
    {
        public const string BookPath = "Assets/_Game/Resources/LoreBook.asset";

        /// <summary>As placas dos Primatas, pela chave (ScriptId).</summary>
        public static Dictionary<string, DialogueDefinition> Create()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var d = new Dictionary<string, DialogueDefinition>
            {
                ["P-PRIM-01"] = DialogueSetup.Conversation("P-PRIM-01", false, 2,
                    (null, "", "Placa de metal, letras que ninguém usa mais. Uma seta. Um desenho de algo com rodas."),
                    (cat, "ironico", "Uma carroça que anda sozinha? Ok.")),
                ["P-PRIM-02"] = DialogueSetup.Conversation("P-PRIM-02", false, 1,
                    (null, "", "Um mapa de linhas coloridas se cruzando, todas terminando em pontos."),
                    (null, "", "Embaixo, uma palavra repetida em toda placa."),
                    (cat, "", "Quem fez isso ia a muitos lugares. E com pressa.")),
                ["P-PRIM-03"] = DialogueSetup.Conversation("P-PRIM-03", false, 1,
                    (null, "", "Vidro que não quebrou em mil anos. Atrás dele, cadeiras viradas para uma parede vazia."),
                    (cat, "", "Sentaram todos para olhar a mesma coisa. E depois sumiram.")),
                ["P-PRIM-04"] = DialogueSetup.Conversation("P-PRIM-04", false, 1,
                    (null, "", "Uma luz pequena ainda pisca no alto da torre. Verde. Apaga. Verde."),
                    (cat, "", "Quem quer que tenha acendido isso não voltou para apagar.")),
                ["P-PRIM-05"] = DialogueSetup.Conversation("P-PRIM-05", false, 1,
                    (null, "", "Uma caixa de metal presa lá no fundo do poço, com portas que se abriam ao meio."),
                    (null, "", "No painel, botões redondos em duas colunas. Quarenta deles."),
                    (cat, "ironico", "Quarenta andares e uma caixa para não subir a pé. Gente esperta.")),
                ["P-PRIM-06"] = DialogueSetup.Conversation("P-PRIM-06", false, 2,
                    (null, "", "Uma carroça de metal com bancos estofados e janelas dos quatro lados."),
                    (null, "", "Na frente, um círculo para segurar e pedais no chão."),
                    (cat, "", "Os Primatas iam de um lugar para outro sentados dentro dessas coisas?")),
                ["P-PRIM-07"] = DialogueSetup.Conversation("P-PRIM-07", false, 1,
                    (null, "", "Pedra talhada à mão, bem mais velha que o concreto das outras ruínas."),
                    (null, "", "Primatas sentados, entalhados lado a lado. Todos olham para o alto, para o mesmo círculo."),
                    (cat, "", "Primeiro pedra, depois concreto. Depois, mato.")),
                ["P-PRIM-08"] = DialogueSetup.Conversation("P-PRIM-08", false, 1,
                    (null, "", "Uma mesa, uma cadeira e uma caneca de barro presa à mesa pelo musgo."),
                    (null, "", "Na parede, um papel com uma lista de horários. O último foi riscado à mão."),
                    (cat, "", "Alguém ficou esperando o último. Ele não veio.")),
                // O Mercado dos Primatas (térreo da C3, pedido do usuário de 15/11/2026): comida guardada para ninguém.
                // A P-PRIM-03 (vitrine) saiu da C3 junto com o desenho dela (ajuste à mão); o texto fica para outro lugar.
                ["P-PRIM-09"] = DialogueSetup.Conversation("P-PRIM-09", false, 1,
                    (null, "", "Prateleiras de metal em fileiras, do chão quase até o teto. As vinhas subiram por todas."),
                    (null, "", "Latas iguais, viradas para a frente, cada uma com o mesmo rosto sorrindo. Nenhuma foi aberta."),
                    (cat, "", "Guardaram comida para mil invernos. E não ficou ninguém com fome.")),
                ["P-PRIM-10"] = DialogueSetup.Conversation("P-PRIM-10", false, 1,
                    (null, "", "Um balcão com uma esteira parada e uma gaveta de metal aberta."),
                    (null, "", "Dentro, discos finos e papéis coloridos, colados um no outro pelo mofo."),
                    (null, "", "Ao lado, gaiolas de arame com rodinhas, encaixadas umas nas outras."),
                    (cat, "ironico", "Pagavam para levar a comida que já estava ali. E ainda empurravam uma gaiola.")),
                // Documentos das cabanas abandonadas (14/11/2026, pedido do usuário): a gente que rezava para a Isyathy, na
                // ordem do caminho. Da devoção ao medo: a oração (A3), as contas (B1), a carta (C3), a última página (E3).
                ["D-DEVOTO-01"] = DialogueSetup.Conversation("D-DEVOTO-01", false, 1,
                    (null, "", "Pregado na estaca: \"Grande Isyathy, que dorme no alto do vale:\""),
                    (null, "", "\"guarde nossa lenha, nossos filhotes e o caminho de volta para casa.\""),
                    (null, "", "\"Deixamos o primeiro pão da estação na pedra lisa. Não pedimos mais nada.\""),
                    (cat, "ironico", "Rezavam para uma cobra. E ainda deixavam pão.")),
                ["D-DEVOTO-02"] = DialogueSetup.Conversation("D-DEVOTO-02", false, 1,
                    (null, "", "Caderno de contas: \"Doze feixes para o mercador de Soluki. Dois para a pedra lisa.\""),
                    (null, "", "\"A guardiã não come pão, dizia minha avó. Mas o pão some todo inverno.\""),
                    (cat, "ironico", "Alguém come esse pão. Aposto que não é a cobra.")),
                ["D-DEVOTO-03"] = DialogueSetup.Conversation("D-DEVOTO-03", false, 1,
                    (null, "", "Uma carta que nunca foi mandada: \"Irmão, a guardiã não responde mais.\""),
                    (null, "", "\"A pedra lisa ficou roxa por baixo do musgo, e as árvores do vale perdem a casca.\""),
                    (null, "", "\"O ancião diz que é castigo, porque rezamos pouco. Eu rezo todas as noites.\""),
                    (cat, "", "Reza não segura praga. Pergunte aos cães da Cidadela.")),
                ["D-DEVOTO-04"] = DialogueSetup.Conversation("D-DEVOTO-04", false, 1,
                    (null, "", "Uma página rasgada: \"Subimos ao ninho com a oferenda, como manda o costume.\""),
                    (null, "", "\"Ela abriu os olhos. Não eram os olhos dela. Eram verdes.\""),
                    (null, "", "\"Corremos. O ancião ficou.\""),
                    (cat, "", "...O ancião ficou.")),
            };
            AssetDatabase.SaveAssets();
            return d;
        }

        /// <summary>
        /// Gera (ou atualiza) o catálogo do Diário com as placas da Fortaleza (capítulo 1) e as da Floresta (capítulo 2).
        /// A Fortaleza e a Floresta chamam; também roda em "Mapa do mundo e Diário".
        /// </summary>
        public static LoreBook BuildBook()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var book = AssetDatabase.LoadAssetAtPath<LoreBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<LoreBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            int n = 0;
            var fortress = CitadelDecorSetup.CreateLore();
            foreach (var o in FortressLayout.Objects())
            {
                if (o.Kind != "lore" || !fortress.TryGetValue(o.Id, out var text)) continue;
                book.Put(o.Id, o.Name, 1, o.X, text); n++;
            }
            var forest = Create();
            foreach (var reg in ForestLayout.Regions())
                foreach (var o in reg.Objects)
                {
                    if (o.Kind != "prilore" || !forest.TryGetValue(o.Id, out var text)) continue;
                    book.Put(o.Id, o.Text, 2, o.X, text); n++;
                }
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sete Vidas] Diário: catálogo com {n} placas ({BookPath}).");
            return book;
        }
    }
}
