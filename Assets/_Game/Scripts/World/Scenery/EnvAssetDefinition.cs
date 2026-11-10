using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Um asset da biblioteca de cenário, importado do <c>manifest.json</c> (não edite este asset: edite o manifesto).
    /// Guarda os sprites já carregados e os metadados (região, função, camada, regras de posicionamento).
    /// </summary>
    public class EnvAssetDefinition : ScriptableObject
    {
        [Tooltip("Pasta do manifesto de onde veio (para achar a entrada)")]
        public string manifestPath;
        public EnvManifestEntry entry = new EnvManifestEntry();
        [Tooltip("Uma por variação")]
        public Sprite[] variants = new Sprite[0];
        [Tooltip("Quadros da animação (vazio = parado)")]
        public Sprite[] frames = new Sprite[0];

        public string Id => entry.id;
        public int SortingOrder => entry.sorting != 0 ? entry.sorting : SceneryLayers.DefaultOrder(entry.camada);
        public bool IsBackground => entry.categoria == "plx";

        /// <summary>Paralaxe [x, y] do manifesto; sem valor = parada no mundo (1, 1).</summary>
        public Vector2 Parallax => Vec(entry.fundo.paralaxe, Vector2.one);
        public Vector2 Anchor => Vec(entry.fundo.ancora, Vector2.zero);

        static Vector2 Vec(float[] v, Vector2 fallback) =>
            v != null && v.Length >= 2 ? new Vector2(v[0], v[1]) : fallback;
    }
}
