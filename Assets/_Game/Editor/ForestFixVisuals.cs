using System.Collections.Generic;
using SeteVidas.World;
using SeteVidas.World.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Conserta objetos em que só o desenho (o filho "Visual") foi arrastado na cena, deixando a colisão para trás:
    /// leva o objeto inteiro até onde o desenho está e devolve o desenho à posição certa dentro dele. Tem Desfazer.
    /// (Os componentes da Floresta agora têm [SelectionBase]: clicar no desenho já seleciona o objeto inteiro.)
    /// </summary>
    public static class ForestFixVisuals
    {
        [MenuItem("Tools/Sete Vidas/20b. Floresta: juntar desenho e colisão (cena aberta)", priority = 71)]
        public static void Fix()
        {
            var report = new List<string>();
            Visit<OneWayPlatform>(report);          // galhos, folha larga, galho que balança
            Visit<FloatingCactus>(report);
            Visit<BounceMushroom>(report);
            Visit<WoodChest>(report);
            Visit<CrumblingPlatform>(report);       // galho podre
            Visit<BreakableWall>(report);           // tronco rachado (o prefab)

            if (report.Count == 0)
            {
                Debug.Log("[Sete Vidas] Nada a juntar: todo desenho está com a sua colisão.");
                return;
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Sete Vidas] {report.Count} objeto(s) consertado(s) (salve a cena):\n" + string.Join("\n", report));
        }

        /// <summary>Onde o desenho fica dentro de cada tipo de objeto (como o ForestTestSetup e os prefabs montam).</summary>
        static Vector3 Expected(Component c)
        {
            switch (c)
            {
                case OneWayPlatform _: return new Vector3(0f, -0.125f, 0f);
                case BounceMushroom _: return new Vector3(0f, 0.5f, 0f);
                case WoodChest _: return new Vector3(0f, 0.5f, 0f);
                case BreakableWall _: return new Vector3(0f, 2f, 0f);
                default: return Vector3.zero; // cacto, galho podre
            }
        }

        static void Visit<T>(List<string> report) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var visual = c.transform.Find("Visual");
                if (visual == null) continue;
                Vector3 delta = visual.localPosition - Expected(c);
                if (delta.sqrMagnitude < 0.0001f) continue;
                Undo.RecordObject(c.transform, "Juntar desenho e colisão");
                Undo.RecordObject(visual, "Juntar desenho e colisão");
                Vector3 worldVisual = visual.position;
                c.transform.position += c.transform.TransformVector(delta);
                visual.position = worldVisual;                    // o desenho fica onde você pôs
                visual.localPosition = Expected(c);               // e volta a ficar certo dentro do objeto
                if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c.transform);
                report.Add($"{c.name}: objeto movido {delta.x:+0.00;-0.00} , {delta.y:+0.00;-0.00} tiles");
            }
        }
    }
}
