using System.Collections.Generic;
using SeteVidas.Dialogue;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// O Ninho com só 2 santuários (18/11/2026, pedido do usuário): o S-F3B no começo da E1 e o S-F4 na E6, antes do
    /// chefe. Atualiza a Forest_E_Nest já montada sem recriar a cena (os ajustes à mão ficam): tira todos os santuários
    /// da cena, põe os do ForestLayout e encurta a patrulha do infectado da E6 (longe do santuário novo).
    /// Menu "Floresta: Ninho com 2 santuários (sem recriar a cena)".
    /// </summary>
    public static class NestSanctuariesSetup
    {
        public static void PatchScene()
        {
            string path = $"{ForestTestSetup.Folder}/{ForestLayout.E}.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path);
            var reg = ForestLayout.Regions().Find(x => x.Scene == ForestLayout.E);
            Transform root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.transform.Find("E1") != null || go.name.EndsWith("(graybox F2)")) { root = go.transform; break; }
            if (root == null || reg == null) { Debug.LogError("[Sete Vidas] Ninho: não achei a raiz da Forest_E_Nest (gere as cenas)."); return; }

            // 1. tira todos os santuários que estão na cena (os da E2 e da E4, e qualquer outro posto à mão)
            int removed = 0;
            foreach (var s in root.GetComponentsInChildren<Sanctuary>(true))
            {
                if (s == null) continue;
                Object.DestroyImmediate(s.gameObject);
                removed++;
            }

            // 2. põe os do layout (cada um no grupo da sala dele, como no "gerar as cenas")
            var none = new Dictionary<string, DialogueDefinition>();
            var created = new Dictionary<string, Component>();
            var placed = new List<string>();
            foreach (var o in reg.Objects)
            {
                if (o.Kind == "sanctuary")
                {
                    FortressSetup.Place(Group(root, o.Name), new FortressLayout.Obj
                    {
                        Kind = o.Kind, Name = o.Name, X = o.X, Y = o.Y, W = o.W, H = o.H, A = o.A, B = o.B,
                        Facing = o.Facing, Value = o.Value, Id = o.Id, Text = o.Text, Link = o.Link,
                    }, none, created);
                    placed.Add(o.Id);
                }
            }

            // 3. a patrulha dos infectados igual à do layout (a da E6 encurtou)
            int posts = 0;
            foreach (var squad in root.GetComponentsInChildren<SeteVidas.Enemies.Zombie.ZombieSquad>(true))
            {
                var so = new SerializedObject(squad);
                var list = so.FindProperty("posts");
                for (int i = 0; i < list.arraySize; i++)
                {
                    var p = list.GetArrayElementAtIndex(i);
                    string name = p.FindPropertyRelative("name").stringValue;
                    var o = reg.Objects.Find(x => x.Kind == "infected" && x.Name == name);
                    if (o == null) continue;
                    p.FindPropertyRelative("at").vector2Value = new Vector2(o.X, o.Y + 0.05f);
                    p.FindPropertyRelative("minX").floatValue = o.A;
                    p.FindPropertyRelative("maxX").floatValue = o.B;
                    posts++;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Sete Vidas] Ninho: {removed} santuários tirados, postos {placed.Count} ({string.Join(", ", placed)}); {posts} patrulhas de infectado conferidas.");
        }

        static Transform Group(Transform root, string objName)
        {
            string room = objName.Contains(":") ? objName.Substring(0, objName.IndexOf(':')) : "geral";
            foreach (Transform c in root) if (c.name == room) return c;
            var g = new GameObject(room).transform;
            g.SetParent(root, false);
            return g;
        }
    }
}
