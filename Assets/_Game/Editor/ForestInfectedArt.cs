using System.Collections.Generic;
using System.IO;
using SeteVidas.Enemies;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F4: variante infectada dos inimigos da Floresta no editor. Gera, ao lado de cada sprite, a versão
    /// <c>&lt;nome&gt;_infectado.png</c> com a paleta da praga (Docs/Arte/floresta-guia-visual.md: roxo #5b2a6e e
    /// verde-ácido #8fd14f; olhos verde-ácido e rachaduras que brilham) e põe o componente <see cref="Infected"/> no
    /// inimigo com os pares normal → infectado. Só gera se o arquivo ainda não existe: dá para trocar pela arte de
    /// verdade no mesmo nome.
    /// <para>O turquesa do pogo (flor da Planta, topo do Cacto) não muda: a leitura do pogo vale para os dois.</para>
    /// </summary>
    internal static class ForestInfectedArt
    {
        public const string Suffix = "_infectado";

        static readonly Color32 Dark = new Color32(0x1e, 0x12, 0x26, 0xff);
        static readonly Color32 Mid = new Color32(0x5b, 0x2a, 0x6e, 0xff);
        static readonly Color32 Light = new Color32(0x8a, 0x5a, 0x9a, 0xff);
        static readonly Color32 Acid = new Color32(0x8f, 0xd1, 0x4f, 0xff);
        static readonly Color32 AcidLight = new Color32(0xc8, 0xf0, 0x8a, 0xff);

        /// <summary>
        /// Torna o objeto a variante infectada: gera os sprites e põe o <see cref="Infected"/> com os pares de todos os
        /// sprites usados (os dos desenhos e os dos campos da IA). <paramref name="eyes"/> = de onde saem os pontinhos verdes.
        /// </summary>
        public static Infected Attach(GameObject go, Vector2 eyes)
        {
            var normal = new List<Sprite>();
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>(true)) Add(normal, r.sprite);
            foreach (var c in go.GetComponents<MonoBehaviour>())
            {
                if (c == null || c is Infected) continue;
                var it = new SerializedObject(c).GetIterator();
                while (it.NextVisible(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference) Add(normal, it.objectReferenceValue as Sprite);
            }
            var infected = new List<Sprite>();
            var from = new List<Sprite>();
            foreach (var s in normal)
            {
                var inf = Of(s);
                if (inf == null) continue;
                from.Add(s);
                infected.Add(inf);
            }
            var comp = go.GetComponent<Infected>();
            if (comp == null) comp = go.AddComponent<Infected>();
            comp.Configure(from.ToArray(), infected.ToArray(), eyes);
            EditorUtility.SetDirty(comp);
            return comp;
        }

        static void Add(List<Sprite> list, Sprite s)
        {
            if (s == null || list.Contains(s)) return;
            string path = AssetDatabase.GetAssetPath(s);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png") || path.EndsWith(Suffix + ".png")) return;
            list.Add(s);
        }

        /// <summary>O par infectado de <paramref name="normal"/> (gera o PNG na primeira vez).</summary>
        public static Sprite Of(Sprite normal)
        {
            if (normal == null) return null;
            string src = AssetDatabase.GetAssetPath(normal);
            if (string.IsNullOrEmpty(src) || !src.EndsWith(".png")) return null;
            string dst = src.Substring(0, src.Length - 4) + Suffix + ".png";
            if (!File.Exists(dst))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(src))) { Object.DestroyImmediate(tex); return null; }
                var px = tex.GetPixels32();
                int w = tex.width;
                for (int i = 0; i < px.Length; i++) px[i] = Recolor(px[i], i % w, i / w);
                tex.SetPixels32(px);
                tex.Apply(false);
                File.WriteAllBytes(dst, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
                CopyImport(src, dst);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
        }

        /// <summary>Mesmo pivô e mesma escala do original (o desenho infectado ocupa o mesmo lugar).</summary>
        static void CopyImport(string src, string dst)
        {
            var a = AssetImporter.GetAtPath(src) as TextureImporter;
            var b = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (a == null || b == null) return;
            var s = new TextureImporterSettings();
            a.ReadTextureSettings(s);
            b.SetTextureSettings(s);
            b.spritePixelsPerUnit = a.spritePixelsPerUnit;
            b.spritePivot = a.spritePivot;
            b.SaveAndReimport();
        }

        /// <summary>
        /// Troca de paleta de um pixel (regra pura). Olhos (amarelos, vermelhos ou brancos pequenos e quentes) viram
        /// verde-ácido; o turquesa do pogo fica; o resto vai para a rampa roxa pela luminância, com rachaduras
        /// verde-ácido espalhadas nos tons médios.
        /// </summary>
        internal static Color32 Recolor(Color32 c, int x, int y)
        {
            if (c.a == 0) return c;
            int r = c.r, g = c.g, b = c.b;
            // turquesa / azul do pogo: fica igual (legibilidade do pogo)
            if (b > r + 40 && g > r + 30) return c;
            // olhos: amarelo forte ou vermelho forte
            bool yellow = r > 200 && g > 150 && b < 120;
            bool red = r > 170 && g < 90 && b < 90;
            if (yellow || red) return new Color32(Acid.r, Acid.g, Acid.b, c.a);

            float lum = (0.299f * r + 0.587f * g + 0.114f * b) / 255f;
            Color32 ramp = lum < 0.18f ? Dark : lum < 0.45f ? Color32.Lerp(Dark, Mid, (lum - 0.18f) / 0.27f) : Color32.Lerp(Mid, Light, Mathf.Clamp01((lum - 0.45f) / 0.4f));
            // rachaduras: poucos pixels de tom médio, num padrão fixo
            if (lum > 0.2f && lum < 0.6f && ((x * 7 + y * 13) % 29 == 0 || (x * 11 + y * 5) % 37 == 0))
                return new Color32(lum > 0.4f ? AcidLight.r : Acid.r, lum > 0.4f ? AcidLight.g : Acid.g, lum > 0.4f ? AcidLight.b : Acid.b, c.a);
            var mix = Color32.Lerp(c, ramp, 0.7f);
            mix.a = c.a;
            return mix;
        }
    }
}
