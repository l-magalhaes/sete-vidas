using SeteVidas.Player;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// A arte nova da Vulpis (Tools/CharacterSprites/VulpisSpriteGen.cs, pedido do usuário em 17/11/2026): veste o NPC do
    /// Posto da Vulpis (D5) e a Vulpis da arena da Isyathy (F1) com as animações, sem recriar as cenas.
    /// </summary>
    public static class VulpisArtSetup
    {
        public const string AnimationsPath = "Assets/_Game/ScriptableObjects/Characters/VulpisAnimationSet.asset";

        public static CharacterAnimationSet Load()
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(AnimationsPath);
            if (set == null) Debug.LogWarning("[Sete Vidas] Vulpis: falta o VulpisAnimationSet (rode Tools/CharacterSprites/gerar_vulpis.ps1). Fica o desenho provisório.");
            return set;
        }

        /// <summary>Põe as animações no "Visual" de um NPC da Vulpis (o pivô da arte nova é nos pés).</summary>
        public static bool Dress(Transform npc, CharacterAnimationSet set, bool faceLeft)
        {
            if (npc == null || set == null) return false;
            var visual = npc.Find("Visual");
            if (visual == null) return false;
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null) return false;
            visual.localPosition = Vector3.zero;
            var idle = set.Get("idle");
            if (idle != null && idle.Count > 0) sr.sprite = idle.frames[0].sprite;
            var v = visual.GetComponent<VulpisVisual>();
            if (v == null) v = visual.gameObject.AddComponent<VulpisVisual>();
            v.Configure(set, faceLeft);
            EditorUtility.SetDirty(v);
            EditorUtility.SetDirty(sr);
            return true;
        }

        /// <summary>Menu: o NPC da D5 (Forest_D_Refuge) e a arena da F1 (Forest_F_Heart) com a arte nova.</summary>
        public static void PatchScenes()
        {
            var set = Load();
            if (set == null) return;
            DialogueSetup.VulpisSpeaker();   // os retratos novos (sério, irônico, triste, sorriso) nas conversas
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int npcs = 0;
            var scene = EditorSceneManager.OpenScene($"{ForestTestSetup.Folder}/{ForestLayout.D}.unity");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var bond in root.GetComponentsInChildren<CompanionBond>(true))
                {
                    if (!bond.name.Contains("Vulpis")) continue;
                    var sr = bond.GetComponentInChildren<SpriteRenderer>(true);
                    bool left = sr == null || sr.flipX;
                    if (Dress(bond.transform, set, left)) npcs++;
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            IsyathyBossSetup.PatchScene();
            Debug.Log($"[Sete Vidas] Vulpis: arte nova em {npcs} NPC(s) da {ForestLayout.D} e na arena da Isyathy (F1).");
        }
    }
}
