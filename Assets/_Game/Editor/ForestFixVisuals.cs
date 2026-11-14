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
            UpgradePlatformArt(report);             // antes de tudo: o desenho das plataformas ficou mais alto (F5)
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
                case OneWayPlatform _: return new Vector3(0f, ForestTestSetup.PlatformArtOffset, 0f);
                case CrumblingPlatform cp when IsRottenBranch(cp): return new Vector3(0f, ForestTestSetup.RottenArtOffset, 0f);
                case BounceMushroom _: return new Vector3(0f, 0.5f, 0f);
                case WoodChest _: return new Vector3(0f, 0.5f, 0f);
                case BreakableWall w: return new Vector3(0f, w.GetComponent<BoxCollider2D>().size.y * 0.5f, 0f); // tronco alto: no meio
                default: return Vector3.zero; // cacto (e as plataformas que caem da Fortaleza)
            }
        }

        /// <summary>
        /// Cenas montadas antes da arte da F5 (as Forest_Test): o desenho do galho, da folha e do galho podre tinha 6–8 px
        /// de altura. Passa para o tamanho e a posição novos SEM mexer na colisão (só se o desenho ainda está onde o
        /// ForestTestSetup antigo punha; se você o arrastou, o passo seguinte junta desenho e colisão como antes).
        /// </summary>
        static void UpgradePlatformArt(List<string> report)
        {
            foreach (var c in Object.FindObjectsByType<OneWayPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Upgrade(c, -0.125f, 0.375f, ForestTestSetup.PlatformArtOffset, report);
            foreach (var c in Object.FindObjectsByType<CrumblingPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (IsRottenBranch(c)) Upgrade(c, 0f, 0.5f, ForestTestSetup.RottenArtOffset, report);
        }

        /// <summary>Galho podre da Floresta (a Fortaleza usa o mesmo componente com outro desenho).</summary>
        static bool IsRottenBranch(Component c)
        {
            var visual = c.transform.Find("Visual");
            var sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            return sr != null && sr.sprite != null && sr.sprite.name.StartsWith("galho_podre");
        }

        static void Upgrade(Component c, float oldY, float oldH, float newY, List<string> report)
        {
            var visual = c.transform.Find("Visual");
            var sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (sr == null || sr.drawMode == SpriteDrawMode.Simple) return;
            if (Mathf.Abs(sr.size.y - oldH) > 0.01f || Mathf.Abs(visual.localPosition.y - oldY) > 0.01f) return;
            Undo.RecordObject(visual, "Arte nova das plataformas");
            Undo.RecordObject(sr, "Arte nova das plataformas");
            visual.localPosition = new Vector3(visual.localPosition.x, newY, visual.localPosition.z);
            sr.size = new Vector2(sr.size.x, ForestTestSetup.PlatformArtHeight);
            if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            report.Add($"{c.name}: desenho novo (12 px de altura), colisão no mesmo lugar");
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
