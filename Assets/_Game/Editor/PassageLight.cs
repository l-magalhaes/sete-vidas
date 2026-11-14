using System.IO;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Luz de passagem (pedidos do usuário, 13/11/2026): toda saída para outra cena ou cenário tem um brilho suave só no
    /// vão da passagem, para o jogador ler "isto leva a outro lugar". Minimalista: nada de faixa cheia, o gato continua
    /// visível. Pixel art pontilhada (ordem 25), tingida pela cena: dourado na Fortaleza e na estrada, verde-claro na Floresta.
    /// Os sprites são brancos (o tom vem do SpriteRenderer.color) e ficam em Art/Environments/Common/Passagem.
    /// </summary>
    public static class PassageLight
    {
        const string Folder = "Assets/_Game/Art/Environments/Common/Passagem";
        public const int Order = 25;

        public static readonly Color Gold = new Color32(0xff, 0xef, 0xc4, 0xff);
        public static readonly Color Forest = new Color32(0xd8, 0xff, 0xcc, 0xff);

        /// <summary>
        /// Brilho suave só no vão da passagem (pedido do usuário, 13/11/2026: "algo minimalista, só na passagem"): mais
        /// forte na borda clara (a abertura) e sumindo para dentro da sala e para cima/baixo. Sem faixa cheia: o gato
        /// continua visível. <paramref name="x0"/>..<paramref name="x1"/> = até onde a luz entra; <paramref name="height"/>
        /// = altura do vão. <paramref name="beyond"/> = quanto a luz vaza para fora da abertura (no máximo 1 tile).
        /// </summary>
        public static void Horizontal(Transform parent, string name, float x0, float x1, float cy, float height, Color tint, bool brightRight, float beyond = 1f)
        {
            float spill = Mathf.Clamp(beyond, 0f, 1f);
            int w = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(x1 - x0) + spill));
            int h = Mathf.Max(2, Mathf.RoundToInt(height));
            float edge = brightRight ? Mathf.Max(x0, x1) + spill : Mathf.Min(x0, x1) - spill;
            float cx = brightRight ? edge - w * 0.5f : edge + w * 0.5f;
            var sr = Renderer(parent, name, Soft(w, h), new Vector3(cx, cy, 0f), Vector2.zero, tint);
            sr.flipX = !brightRight;
        }

        /// <summary>O mesmo brilho num buraco do teto (<paramref name="brightUp"/>) ou do chão.</summary>
        public static void Vertical(Transform parent, string name, float cx, float width, float y0, float y1, Color tint, bool brightUp, float beyond = 1f)
        {
            float spill = Mathf.Clamp(beyond, 0f, 1f);
            int len = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(y1 - y0) + spill));
            int w = Mathf.Max(2, Mathf.RoundToInt(width));
            float edge = brightUp ? Mathf.Max(y0, y1) + spill : Mathf.Min(y0, y1) - spill;
            float cy = brightUp ? edge - len * 0.5f : edge + len * 0.5f;
            var sr = Renderer(parent, name, Soft(len, w), new Vector3(cx, cy, 0f), Vector2.zero, tint);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, brightUp ? 90f : -90f); // a borda clara (direita) vira para cima
        }

        /// <summary>Clarão em volta de uma porta/cipó de atalho (pés em x, y), atrás do gato.</summary>
        public static void Glow(Transform parent, string name, float x, float y, Color tint)
        {
            var sr = Renderer(parent, name, GlowSprite(), new Vector3(x, y + 1.5f, 0f), Vector2.zero, tint);
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -2; // atrás do gato e da porta: ele não some, só fica iluminado
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, Vector3 at, Vector2 size, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (size != Vector2.zero)
            {
                sr.drawMode = SpriteDrawMode.Tiled;   // o gradiente tem a largura certa; repete só no outro eixo
                sr.size = size;
            }
            sr.color = tint;
            sr.sortingOrder = Order;
            return sr;
        }

        // ------------------------------------------------------------ sprites (gerados uma vez)

        static readonly int[,] Bayer = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

        /// <summary>
        /// Brilho de <paramref name="w"/> × <paramref name="h"/> tiles, claro na borda direita e no meio da altura, sumindo
        /// para a esquerda e para as pontas. Degradê liso (como a referência do usuário), em degraus de 1/32 para não
        /// manchar, e nunca opaco (no máximo 50%).
        /// </summary>
        static Sprite Soft(int w, int h) => Make($"luz_passagem_lisa_{w}x{h}", w * 16, h * 16, (x, y, pw, ph) =>
        {
            float u = 1f - (x + 0.5f) / pw;                        // 0 na borda clara, 1 lá dentro
            float v = Mathf.Abs((y + 0.5f) / ph * 2f - 1f);        // 0 no meio do vão, 1 nas pontas
            float a = Mathf.Pow(1f - u, 2.2f) * Mathf.Pow(Mathf.Max(0f, 1f - v * v), 1.5f) * 0.5f;
            a = Mathf.Round(a * 32f) / 32f;
            return a <= 0f ? Color.clear : new Color(1f, 1f, 1f, a);
        });

        static Sprite GlowSprite() => Make("luz_porta", 48, 64, (x, y, w, h) =>
        {
            float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - d) * 0.6f;
            float threshold = (Bayer[y & 3, x & 3] + 0.5f) / 16f * 0.6f;
            return a > threshold ? new Color(1f, 1f, 1f, a < 0.3f ? 0.3f : 0.5f) : Color.clear;
        });

        delegate Color Painter(int x, int y, int w, int h);

        static Sprite Make(string name, int w, int h, Painter paint)
        {
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Folder);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        px[y * w + x] = paint(x, y, w, h);
                tex.SetPixels(px);
                tex.Apply(false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spritePixelsPerUnit = 16;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.mipmapEnabled = false;
                    imp.wrapMode = TextureWrapMode.Clamp;
                    var s = new TextureImporterSettings();
                    imp.ReadTextureSettings(s);
                    s.spriteMeshType = SpriteMeshType.FullRect; // necessário para o modo Tiled
                    imp.SetTextureSettings(s);
                    imp.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
