using System.IO;
using SeteVidas.World;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// As cachoeiras no fundo da cabeceira leste (R5, 19/11/2026, pedido do usuário, com um desenho): caem da montanha
    /// até a névoa, com paralaxe e a água animada. Só desenho (sem colisão). O topo fica escondido atrás da rocha da
    /// montanha (o terreno é desenhado na frente) e o pé some na névoa de perto (ordem −55).
    /// Objeto "waterfall" do ForestLayout: X = o meio, Y = o pé, W = a largura e H = a altura (tiles).
    /// </summary>
    public static class WaterfallSetup
    {
        const string Folder = "Assets/_Game/Art/Environments/Forest/Placeholder";
        /// <summary>Atrás da névoa de perto (−55) e na frente da névoa de longe (−70) e das copas (−90).</summary>
        public const int Order = -60;
        /// <summary>Paralaxe: na horizontal, um pouco atrás do jogo (0,8 parada no mundo); na vertical, parada (o topo fica na montanha).</summary>
        static readonly Vector2 Parallax = new Vector2(0.8f, 1f);
        const int Frames = 8, Period = 64, Step = Period / Frames;
        const float FrameTime = 0.08f;

        public static void Build(Transform parent, ForestLayout.Obj o)
        {
            int w = Mathf.Max(2, Mathf.RoundToInt(o.W)), h = Mathf.Max(4, Mathf.RoundToInt(o.H));
            var frames = FrameSprites(w, h);
            var go = new GameObject(o.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            go.AddComponent<ParallaxLayer>().Configure(Parallax, new Vector2(o.X, o.Y), 0f);
            var art = new GameObject("Água");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = frames.Length > 0 ? frames[0] : null;
            sr.sortingOrder = Order;
            if (frames.Length > 1) art.AddComponent<SpriteLoop>().Configure(frames, FrameTime, Mathf.Repeat(o.X * 0.13f, FrameTime * Frames));
        }

        /// <summary>Os quadros da cachoeira de <paramref name="w"/> × <paramref name="h"/> tiles (gravados de novo: a arte é do código).</summary>
        static Sprite[] FrameSprites(int w, int h)
        {
            var list = new Sprite[Frames];
            int pw = w * 16 + 8, ph = h * 16;
            for (int f = 0; f < Frames; f++)
            {
                string path = $"{Folder}/cachoeira_{w}x{h}_{f}.png";
                var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
                tex.SetPixels32(Pixels(pw, ph, f, w * 7 + h));
                tex.Apply(false);
                File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                list[f] = Import(path);
            }
            return list;
        }

        static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------ o desenho

        // a água de longe, à noite: azuis frios e apagados (o fundo não compete com o gato)
        static readonly Color32 Dark = new Color32(0x2f, 0x4a, 0x63, 0xff);
        static readonly Color32 Mid = new Color32(0x4a, 0x6d, 0x8a, 0xff);
        static readonly Color32 Light = new Color32(0x6f, 0x93, 0xae, 0xff);
        static readonly Color32 High = new Color32(0xa9, 0xc6, 0xd6, 0xff);
        static readonly Color32 Foam = new Color32(0xc4, 0xd8, 0xe0, 0xff);

        static uint Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)a * 374761393u + (uint)b * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        /// <summary>
        /// Um quadro (linha 0 embaixo). As faixas de água descem <see cref="Step"/> px por quadro num padrão que se repete
        /// a cada <see cref="Period"/> px: os <see cref="Frames"/> quadros fecham o ciclo sem emenda. As bordas ondulam
        /// (paradas) e a queda alarga um pouco para baixo; no pé, a espuma pisca.
        /// </summary>
        public static Color32[] Pixels(int pw, int ph, int frame, int seed)
        {
            var px = new Color32[pw * ph];
            float cx = pw * 0.5f, full = (pw - 8) * 0.5f;
            float sa = (seed % 7) * 0.9f, sb = (seed % 11) * 0.6f, sc = (seed % 13) * 0.5f, sd = (seed % 5) * 1.3f;
            int phase = frame * Step;
            for (int y = 0; y < ph; y++)
            {
                int ty = ph - 1 - y;                                    // do topo para baixo
                float down = (float)ty / ph;                            // 0 no topo, 1 no pé
                float half = full * (0.8f + 0.2f * down);
                float left = cx - half + Mathf.Sin(ty * 0.031f + sa) * 1.6f + Mathf.Sin(ty * 0.083f + sb) * 0.9f;
                float right = cx + half + Mathf.Sin(ty * 0.027f + sc) * 1.6f + Mathf.Sin(ty * 0.09f + sd) * 0.9f;
                int foamRows = ph - ty;                                 // linhas até o pé
                for (int x = 0; x < pw; x++)
                {
                    Color32 c = default(Color32);
                    if (x < left - 0.5f || x > right + 0.5f)
                    {
                        // respingos em volta do pé
                        float out_ = x < left ? left - x : x - right;
                        if (foamRows < 36 && out_ < 4f && Hash(x * 3 + frame * 17, ty) % 11 == 0) c = Fade(Foam, 150);
                        px[y * pw + x] = c;
                        continue;
                    }
                    float u = (x - left) / Mathf.Max(1f, right - left);
                    uint col = Hash(x, 17 + seed);
                    int g = (((ty - phase + (int)(col % Period)) % Period) + Period) % Period;
                    int len = 8 + (int)(col % 24);
                    c = Mid;
                    if (g < len) c = g < 3 ? High : Light;
                    else if ((col >> 8) % 5 == 0 && g >= 34 && g < 34 + len / 2) c = Dark;
                    if (u < 0.18f || u > 0.82f) c = Darker(c);           // as laterais mais fundas
                    if (u < 0.07f || u > 0.93f) c = Fade(Dark, 220);     // a borda deixa o céu passar um pouco
                    if (foamRows < 28)
                    {
                        uint n = Hash(x + frame * 31, ty * 7 + frame);
                        int chance = foamRows < 12 ? 2 : foamRows < 20 ? 3 : 6;
                        if (n % (uint)chance == 0) c = Fade(Foam, (byte)(foamRows < 12 ? 230 : 200));
                    }
                    px[y * pw + x] = c;
                }
            }
            return px;
        }

        static Color32 Darker(Color32 c)
        {
            if (Eq(c, High)) return Light;
            if (Eq(c, Light)) return Mid;
            return Dark;
        }

        static bool Eq(Color32 a, Color32 b) { return a.r == b.r && a.g == b.g && a.b == b.b; }
        static Color32 Fade(Color32 c, byte a) { return new Color32(c.r, c.g, c.b, a); }
    }
}
