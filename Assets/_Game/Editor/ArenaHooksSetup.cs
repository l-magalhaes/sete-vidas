using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Os ganchos da arena F1 (18/11/2026, pedido do usuário: saiu o do meio, embaixo do galho podre de cima; entraram
    /// dois nas pontas do galho do topo). Atualiza a Forest_F_Heart já montada sem recriar a cena: tira os ganchos do
    /// grupo F1 e põe os do ForestLayout. Menu "Floresta: ganchos da arena F1 (sem recriar a cena)".
    /// </summary>
    public static class ArenaHooksSetup
    {
        const string Room = "F1";

        public static void PatchScene()
        {
            string path = $"{ForestTestSetup.Folder}/{ForestLayout.F}.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path);
            var reg = ForestLayout.Regions().Find(x => x.Scene == ForestLayout.F);
            Transform group = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                var g = go.transform.Find(Room);
                if (g != null && g.GetComponentInChildren<SeteVidas.World.GrappleAnchor>(true) != null) { group = g; break; }
                if (g != null && group == null) group = g;
            }
            if (group == null || reg == null) { Debug.LogError("[Sete Vidas] Arena: não achei o grupo F1 na Forest_F_Heart (gere as cenas)."); return; }

            int removed = 0;
            foreach (var a in group.GetComponentsInChildren<SeteVidas.World.GrappleAnchor>(true))
            {
                if (a == null) continue;
                Object.DestroyImmediate(a.gameObject);
                removed++;
            }
            int made = 0;
            foreach (var o in reg.Objects)
                if (o.Kind == "hook" && o.Name.StartsWith(Room + ":")) { ForestTestSetup.Hook(group, o.Name, o.X, o.Y); made++; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Sete Vidas] Arena F1: {removed} ganchos tirados, {made} postos (os dois lados de cima e as pontas do galho do topo).");
        }
    }
}
