using SeteVidas.Dialogue;
using SeteVidas.World;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F6, o resgate da D2 (pedido do usuário, 19/11/2026; Docs/Narrativa/floresta-roteiro.md, D-VULPIS-01): o Galho
    /// Seco infectado da cena, a <see cref="VulpisRescue"/> com a Vulpis animada e a conversa. Roda junto do "Floresta:
    /// gerar as cenas" (objeto "rescue" do ForestLayout).
    /// </summary>
    public static class VulpisRescueSetup
    {
        /// <summary>
        /// D-VULPIS-01 (obrigatória, no máximo 6 falas): o primeiro relance dela, com o aviso de que há coisa maior
        /// acordando na floresta (a Isyathy; o nome e a história ficam para o vínculo da D5).
        /// </summary>
        public static DialogueDefinition CreateDialogue()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var fox = DialogueSetup.VulpisSpeaker();
            var d = DialogueSetup.Conversation("D-VULPIS-01", true, 1,
                (fox, "ironico", "Você escolheu uma péssima noite para passear por estas bandas."),
                (cat, "ironico", "Eu não escolhi. Eu caí. É diferente."),
                (fox, "ironico", "Então sua sorte é ainda pior do que parece."),
                (fox, "serio", "E ande baixo daqui para a frente. Tem coisa muito maior que esse galho acordando nesta floresta."),
                (cat, "ironico", "Maior que um tronco que dá socos? Que ótimo."),
                (fox, "serio", "Me procure no posto, depois do Refúgio. Se ainda estiver inteiro."));
            AssetDatabase.SaveAssets();
            return d;
        }

        /// <summary>
        /// D-VULPIS-CHAVE (pedido do usuário, 19/11/2026): de volta ao posto com a Chave do Templo, ela diz que encontra o
        /// gato no templo e vai na frente, pelas copas (<see cref="VulpisDeparture"/>).
        /// </summary>
        public static DialogueDefinition CreateKeyDialogue()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var fox = DialogueSetup.VulpisSpeaker();
            var d = DialogueSetup.Conversation("D-VULPIS-CHAVE", true, 1,
                (fox, "surpreso", "A Chave do Templo! Você achou mesmo."),
                (cat, "ironico", "Achar coisas trancadas é o meu trabalho. Abrir também."),
                (fox, "serio", "Então não temos tempo a perder. O templo fica no alto do Mirante, e o ninho logo depois dele."),
                (fox, "serio", "Vou na frente, pelas copas, ver se o caminho está limpo. Te encontro no templo."),
                (cat, "ironico", "Pelas copas. Claro. Os gatos vão pelo chão, como gente normal."));
            AssetDatabase.SaveAssets();
            return d;
        }

        /// <summary>Põe a saída da Vulpis no NPC do posto (D5).</summary>
        public static void AddDeparture(GameObject npc)
        {
            var dep = npc.GetComponent<VulpisDeparture>() ?? npc.AddComponent<VulpisDeparture>();
            dep.Configure(CreateKeyDialogue());
            EditorUtility.SetDirty(dep);
        }

        public static void Build(Transform parent, ForestLayout.Obj o)
        {
            var root = new GameObject(o.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(o.X, o.Y, 0f);
            // a criatura: um Galho Seco infectado, disfarçado, virado para o gato que chega
            var branch = ForestTestSetup.DryBranchEnemy(root.transform, o.Name + ": Galho Seco (infectado)", o.X, o.Y, o.Facing);
            ForestInfectedArt.Attach(branch.gameObject, new Vector2(0f, 3f));
            var rescue = root.AddComponent<VulpisRescue>();
            rescue.Configure(branch, o.A, new Vector2(o.B, o.C), VulpisArtSetup.Load(), CreateDialogue());
            EditorUtility.SetDirty(rescue);
        }
    }
}
