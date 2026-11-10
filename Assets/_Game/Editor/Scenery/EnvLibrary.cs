using System.Collections.Generic;
using System.IO;
using System.Linq;
using SeteVidas.World.Scenery;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Biblioteca de cenário (plano de assets, seção 3): lê todos os <c>manifest.json</c> em
    /// <c>Art/Environments</c> e mantém um <see cref="EnvAssetDefinition"/> por entrada em
    /// <c>ScriptableObjects/Environment</c>. O manifesto é a fonte de verdade: edite o JSON, não os assets.
    /// Reimporta sozinho quando um manifesto muda e antes de gerar a Fortaleza.
    /// </summary>
    public static class EnvLibrary
    {
        public const string ArtRoot = "Assets/_Game/Art/Environments";
        public const string DefinitionsFolder = "Assets/_Game/ScriptableObjects/Environment";
        const string Menu = "Tools/Sete Vidas/Cenário/";

        static Dictionary<string, EnvAssetDefinition> s_cache;

        [MenuItem(Menu + "Importar biblioteca de cenário", priority = 40)]
        public static void ImportMenu()
        {
            var report = ImportAll();
            Debug.Log($"[Sete Vidas] Biblioteca de cenário: {report.Imported} assets de {report.Manifests} manifestos" +
                      (report.Problems.Count > 0 ? $", {report.Problems.Count} problema(s) (veja os erros acima)." : ", sem problemas."));
        }

        public class Report
        {
            public int Manifests, Imported;
            public readonly List<string> Problems = new List<string>();
        }

        /// <summary>Lê os manifestos, cria/atualiza as definições e apaga as que não estão mais em nenhum manifesto.</summary>
        public static Report ImportAll()
        {
            var report = new Report();
            Directory.CreateDirectory(Path.GetFullPath(DefinitionsFolder));
            var seen = new Dictionary<string, string>(); // id → manifesto

            foreach (string full in Directory.GetFiles(Path.GetFullPath(ArtRoot), "manifest.json", SearchOption.AllDirectories))
            {
                string manifestPath = ToAssetPath(full);
                string dir = Path.GetDirectoryName(manifestPath).Replace('\\', '/');
                EnvManifest manifest;
                try { manifest = EnvManifest.Parse(File.ReadAllText(full)); }
                catch (System.Exception e)
                {
                    Problem(report, $"{manifestPath}: JSON inválido ({e.Message})");
                    continue;
                }
                report.Manifests++;

                foreach (var entry in manifest.assets)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id)) { Problem(report, $"{manifestPath}: entrada sem id"); continue; }
                    if (seen.TryGetValue(entry.id, out string other)) { Problem(report, $"id repetido \"{entry.id}\" em {manifestPath} e {other}"); continue; }
                    seen[entry.id] = manifestPath;
                    foreach (var p in Validate(entry)) Problem(report, $"{entry.id} ({manifestPath}): {p}");

                    var def = LoadOrCreate(entry.id);
                    def.manifestPath = manifestPath;
                    def.entry = entry;
                    def.variants = LoadSprites(dir, ExpandPrefix(dir, entry), entry.id, report);
                    def.frames = LoadSprites(dir, FrameFiles(dir, entry), entry.id, report);
                    EditorUtility.SetDirty(def);
                    report.Imported++;
                }
            }

            // Definições órfãs (a entrada saiu do manifesto)
            foreach (string guid in AssetDatabase.FindAssets("t:EnvAssetDefinition", new[] { DefinitionsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<EnvAssetDefinition>(path);
                if (def == null || !seen.ContainsKey(def.Id)) AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            s_cache = null;
            return report;
        }

        /// <summary>Definição pelo id (null se não existir; erro no Console).</summary>
        public static EnvAssetDefinition Get(string id)
        {
            if (s_cache == null || s_cache.Values.Any(d => d == null))
            {
                s_cache = new Dictionary<string, EnvAssetDefinition>();
                foreach (string guid in AssetDatabase.FindAssets("t:EnvAssetDefinition", new[] { DefinitionsFolder }))
                {
                    var def = AssetDatabase.LoadAssetAtPath<EnvAssetDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                    if (def != null && !string.IsNullOrEmpty(def.Id)) s_cache[def.Id] = def;
                }
            }
            if (s_cache.TryGetValue(id, out var found)) return found;
            Debug.LogError($"[Sete Vidas] Asset de cenário \"{id}\" não está na biblioteca. Confira o manifest.json e rode {Menu}Importar biblioteca de cenário.");
            return null;
        }

        static List<string> Validate(EnvManifestEntry e) => EnvManifest.Validate(e);

        /// <summary>
        /// Quadros da animação. Com quadros_por_variacao = n: para cada variação "..._0.png", os n quadros
        /// "..._0" … "..._{n-1}" em sequência (a montagem pega o bloco da variação escolhida).
        /// </summary>
        static string[] FrameFiles(string dir, EnvManifestEntry entry)
        {
            if (entry.animacao == null) return new string[0];
            int n = entry.animacao.quadros_por_variacao;
            if (n <= 0) return entry.animacao.quadros ?? new string[0];
            var list = new List<string>();
            foreach (var v in ExpandPrefix(dir, entry))
            {
                if (!v.EndsWith("_0.png")) { list.Add(v); continue; }
                string stem = v.Substring(0, v.Length - "0.png".Length);
                for (int i = 0; i < n; i++) list.Add(stem + i + ".png");
            }
            return list.ToArray();
        }

        /// <summary>sprites + todos os PNGs que começam com sprites_prefixo (ordem alfabética), relativos à pasta.</summary>
        static string[] ExpandPrefix(string dir, EnvManifestEntry entry)
        {
            var list = new List<string>(entry.sprites ?? new string[0]);
            if (!string.IsNullOrEmpty(entry.sprites_prefixo))
            {
                string full = Path.GetFullPath($"{dir}/{entry.sprites_prefixo}");
                string folder = Path.GetDirectoryName(full);
                if (Directory.Exists(folder))
                {
                    var found = Directory.GetFiles(folder, Path.GetFileName(full) + "*.png")
                        .Select(f => f.Replace('\\', '/'))
                        .OrderBy(f => f, System.StringComparer.Ordinal);
                    string root = Path.GetFullPath(dir).Replace('\\', '/').TrimEnd('/') + "/";
                    list.AddRange(found.Select(f => f.Substring(root.Length)));
                }
            }
            return list.ToArray();
        }

        static Sprite[] LoadSprites(string dir, string[] files, string id, Report report)
        {
            if (files == null) return new Sprite[0];
            var list = new List<Sprite>();
            foreach (var f in files)
            {
                string path = $"{dir}/{f}";
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null) Problem(report, $"{id}: imagem não encontrada {path}");
                else list.Add(s);
            }
            return list.ToArray();
        }

        static EnvAssetDefinition LoadOrCreate(string id)
        {
            string path = $"{DefinitionsFolder}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnvAssetDefinition>(path);
            if (def != null) return def;
            def = ScriptableObject.CreateInstance<EnvAssetDefinition>();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        static void Problem(Report report, string message)
        {
            report.Problems.Add(message);
            Debug.LogError("[Sete Vidas] Biblioteca de cenário: " + message);
        }

        static string ToAssetPath(string full)
        {
            string root = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/') + "/";
            return full.Replace('\\', '/').Replace(root, "");
        }
    }

    /// <summary>Reimporta a biblioteca quando um manifest.json de cenário muda.</summary>
    class EnvManifestPostprocessor : AssetPostprocessor
    {
        static bool s_pending;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (s_pending) return;
            bool touched = imported.Concat(deleted).Concat(moved)
                .Any(p => p.StartsWith(EnvLibrary.ArtRoot) && p.EndsWith("/manifest.json"));
            if (!touched) return;
            s_pending = true;
            EditorApplication.delayCall += () => { s_pending = false; EnvLibrary.ImportAll(); };
        }
    }
}
