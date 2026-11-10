using System.Collections.Generic;
using SeteVidas.World;
using SeteVidas.World.Scenery;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Monta enfeites e fundos a partir da biblioteca de cenário (<see cref="EnvLibrary"/>):
    /// apoio (chão/teto/centro), camada, variação/espelho/deslocamento por semente, animação e paralaxe.
    /// </summary>
    public static class DressingBuilder
    {
        const float Px = 1f / 16f;

        /// <summary>Cria todos os itens embaixo de <paramref name="parent"/>. Devolve quantos objetos criou.</summary>
        public static int Build(Transform parent, IEnumerable<FortressDressing.Item> items)
        {
            int created = 0;
            // duas últimas variações por id (regra: nunca 3 iguais seguidas), na ordem dos dados
            var history = new Dictionary<string, (int, int)>();
            foreach (var item in items)
            {
                var def = EnvLibrary.Get(item.AssetId);
                if (def == null) continue;
                int count = Mathf.Max(1, item.Count);
                for (int i = 0; i < count; i++)
                {
                    string name = count > 1 ? $"{item.Name} {i + 1}" : item.Name;
                    var at = new Vector2(item.X + item.Step * i, item.Y);
                    history.TryGetValue(def.Id, out var prev);
                    if (!history.ContainsKey(def.Id)) prev = (-1, -1);
                    int variant = Place(parent, def, name, at, prev.Item1, prev.Item2, item.Variant);
                    history[def.Id] = (variant, prev.Item1);
                    created++;
                }
            }
            return created;
        }

        /// <summary>Um enfeite. Devolve a variação escolhida.</summary>
        public static int Place(Transform parent, EnvAssetDefinition def, string name, Vector2 at, int previous1 = -1, int previous2 = -1,
                                int fixedVariant = -1)
        {
            uint seed = DressingRules.Seed(def.Id, Mathf.RoundToInt(at.x / Px), Mathf.RoundToInt(at.y / Px));
            int variant = fixedVariant >= 0 && fixedVariant < def.variants.Length
                ? fixedVariant
                : DressingRules.PickVariant(seed, def.variants.Length, previous1, previous2);
            if (fixedVariant >= def.variants.Length)
                Debug.LogWarning($"[Sete Vidas] {name}: variação {fixedVariant} não existe em {def.Id} ({def.variants.Length}); usando sorteio.");
            bool flip = DressingRules.PickFlip(seed, def.entry.espelhar);
            int offset = DressingRules.PickOffsetPx(seed, def.entry.posicionamento.deslocamento_px);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = def.variants.Length > 0 ? def.variants[variant] : null;
            sr.flipX = flip;
            sr.sortingOrder = def.SortingOrder;

            // Sprites de cenário têm pivot central: "chao" apoia a base, "teto" pendura pelo topo
            float half = sr.sprite != null ? sr.sprite.bounds.extents.y : 0f;
            string support = def.entry.posicionamento.apoio;
            float dy = support == "chao" ? half : support == "teto" ? -half : 0f;
            go.transform.position = new Vector3(at.x + offset * Px, at.y + dy, 0f);

            if (def.frames.Length > 1 && def.entry.animacao.fps > 0f)
                go.AddComponent<SpriteLoop>().Configure(def.frames, 1f / def.entry.animacao.fps);
            return variant;
        }

        /// <summary>
        /// Camada de fundo (categoria plx) com paralaxe do manifesto. Se "repete", põe 3 cópias lado a lado
        /// e o <see cref="ParallaxLayer"/> as recicla.
        /// </summary>
        public static GameObject Background(Transform parent, string id, string name)
        {
            var def = EnvLibrary.Get(id);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (def == null || def.variants.Length == 0) return go;

            var sprite = def.variants[0];
            bool wrap = def.entry.fundo.repete;
            float width = sprite.bounds.size.x;
            for (int i = wrap ? -1 : 0; i <= (wrap ? 1 : 0); i++)
            {
                var child = new GameObject(wrap ? $"{name} {i + 2}" : name);
                child.transform.SetParent(go.transform, false);
                child.transform.localPosition = new Vector3(i * width, 0f, 0f);
                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = def.SortingOrder;
            }
            go.AddComponent<ParallaxLayer>().Configure(def.Parallax, def.Anchor, wrap ? width : 0f);
            return go;
        }
    }
}
