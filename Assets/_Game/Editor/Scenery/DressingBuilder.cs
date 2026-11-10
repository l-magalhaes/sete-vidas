using System.Collections.Generic;
using SeteVidas.World;
using SeteVidas.World.Scenery;
using UnityEditor;
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
            {
                // animação por variação (árvores): só o bloco de quadros da variação escolhida
                var frames = def.frames;
                int per = def.entry.animacao.quadros_por_variacao;
                if (per > 0 && frames.Length >= (variant + 1) * per)
                {
                    frames = new Sprite[per];
                    System.Array.Copy(def.frames, variant * per, frames, 0, per);
                }
                // cada um começa num ponto diferente do ciclo: árvores e tochas lado a lado não balançam juntas
                float cycle = frames.Length / def.entry.animacao.fps;
                go.AddComponent<SpriteLoop>().Configure(frames, 1f / def.entry.animacao.fps, DressingRules.Value(seed, 5) * cycle);
            }
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
            bool fill = ColorUtility.TryParseHtmlString(def.entry.fundo.preencher_abaixo ?? "", out var fillColor);
            for (int i = wrap ? -1 : 0; i <= (wrap ? 1 : 0); i++)
            {
                var child = new GameObject(wrap ? $"{name} {i + 2}" : name);
                child.transform.SetParent(go.transform, false);
                child.transform.localPosition = new Vector3(i * width, 0f, 0f);
                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = def.SortingOrder;
                if (fill) FillBelow(child.transform, sprite, fillColor, def.SortingOrder);
            }
            go.AddComponent<ParallaxLayer>().Configure(def.Parallax, def.Anchor, wrap ? width : 0f);
            return go;
        }

        /// <summary>Altura do preenchimento abaixo da camada (tiles): cobre qualquer descida da câmera na fase.</summary>
        const float FillDepth = 200f;
        const string PixelPath = "Assets/_Game/Art/Environments/Placeholder/px_branco.png";

        /// <summary>
        /// Continua a camada para baixo na cor da base dela (campo fundo.preencher_abaixo): com paralaxe, a borda de
        /// baixo do desenho entrava na tela durante a queda do penhasco e aparecia o horizonte laranja do céu.
        /// </summary>
        static void FillBelow(Transform layer, Sprite sprite, Color color, int order)
        {
            var px = WhitePixel();
            if (px == null) return;
            var go = new GameObject("Preenchimento abaixo");
            go.transform.SetParent(layer, false);
            float w = sprite.bounds.size.x, top = -sprite.bounds.extents.y + Px; // 1 px de sobreposição: sem fresta
            go.transform.localPosition = new Vector3(0f, top - FillDepth * 0.5f, 0f);
            go.transform.localScale = new Vector3(w / px.bounds.size.x, FillDepth / px.bounds.size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = px;
            sr.color = color;
            sr.sortingOrder = order;
        }

        /// <summary>Sprite branco 1×1 px (pivot central), criado uma vez; pintado pela cor do SpriteRenderer.</summary>
        static Sprite WhitePixel()
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
            if (s != null) return s;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(PixelPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PixelPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        }
    }
}
