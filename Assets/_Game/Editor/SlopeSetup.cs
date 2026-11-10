using SeteVidas.World.Scenery;
using System.IO;
using System.Linq;
using SeteVidas.Core;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Rampas: um triângulo de colisão (PolygonCollider2D) na layer Ground, mais um desenho placeholder com o
    /// tamanho final. Usado pela Playground (trecho L) e pela Fortaleza (<see cref="FortressLayout.Slopes"/>).
    /// O controlador anda em rampas de até <c>MaxSlopeAngle</c> (46°): 1:1 e 1:2 são andáveis; 2:1 vira parede.
    /// Spec: Docs/Design/controlador-gato.md, "Rampas".
    /// </summary>
    public static class SlopeSetup
    {
        const string Folder = "Assets/_Game/Art/Environments/Placeholder";
        static readonly Color32 Fill = new Color32(0x5a, 0x55, 0x6e, 0xff);   // mesma pedra do tile de chão
        static readonly Color32 Border = new Color32(0x3a, 0x36, 0x4a, 0xff);
        static readonly Color32 Rim = new Color32(0x77, 0x72, 0x8c, 0xff);    // luz no topo da rampa

        /// <summary>
        /// Rampa com a base em <paramref name="y0"/>, ocupando x <paramref name="x0"/>..x0+width.
        /// <paramref name="risesRight"/> = o lado alto fica à direita. Encoste o lado alto num bloco de tiles
        /// com topo em y0+height para a emenda ficar lisa.
        /// </summary>
        /// <param name="material">Material do terreno (ex.: <see cref="TerrainAutotile.Soil"/>): usa os pedaços de arte final
        /// do <see cref="TerrainAutotile.SlopePieces"/>. Vazio, ou proporção sem arte = triângulo placeholder.</param>
        internal static GameObject Create(Transform parent, string name, int x0, int y0, int width, int height, bool risesRight, string material = "")
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x0, y0, 0f);
            go.layer = LayerMask.NameToLayer(GameConstants.Layers.Ground);
            go.isStatic = true;

            var poly = go.AddComponent<PolygonCollider2D>();
            poly.points = risesRight
                ? new[] { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, height) }
                : new[] { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(0f, height) };

            var art = new GameObject("Visual");
            art.transform.SetParent(go.transform, false);

            var pieces = string.IsNullOrEmpty(material)
                ? new System.Collections.Generic.List<TerrainAutotile.Piece>()
                : TerrainAutotile.SlopePieces(material, x0, y0, width, height, risesRight);
            if (pieces.Count > 0)
            {
                // arte final: um sprite 16×16 por célula (diagonal + terra/rocha embaixo), pivot central
                foreach (var p in pieces)
                {
                    var piece = new GameObject(p.Sprite);
                    piece.transform.SetParent(art.transform, false);
                    piece.transform.position = new Vector3(p.X + 0.5f, p.Y + 0.5f, 0f);
                    var psr = piece.AddComponent<SpriteRenderer>();
                    psr.sprite = TerrainPainter.LoadSprite(p.Sprite);
                    psr.sortingOrder = 0;
                }
                return go;
            }

            art.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f); // sprites de cenário têm pivot no centro
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = SlopeSprite(width, height, risesRight);
            sr.sortingOrder = 0;
            return go;
        }

        /// <summary>Triângulo de pedra com contorno e luz na diagonal. Regravado só se o desenho mudar.</summary>
        static Sprite SlopeSprite(int width, int height, bool risesRight)
        {
            string path = $"{Folder}/env_slope_placeholder_{width}x{height}_{(risesRight ? "r" : "l")}.png";
            int px = GameConstants.TileSize;
            int w = width * px, h = height * px;
            var pixels = new Color32[w * h];

            // dentro do triângulo? (o pixel conta se o centro dele estiver abaixo da diagonal)
            bool Inside(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                float fx = risesRight ? x + 0.5f : w - (x + 0.5f);
                return y + 0.5f <= fx * h / w;
            }

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!Inside(x, y)) continue;
                bool diagonal = !Inside(x, y + 1);
                bool edge = y == 0 || !Inside(x - 1, y) || !Inside(x + 1, y);
                pixels[y * w + x] = diagonal ? Rim : edge ? Border : Fill;
                if (!diagonal && !Inside(x, y + 2)) pixels[y * w + x] = Border; // contorno logo abaixo da luz
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            Directory.CreateDirectory(Path.GetFullPath(Folder));
            string fullPath = Path.GetFullPath(path);
            bool changed = !File.Exists(fullPath) || !File.ReadAllBytes(fullPath).SequenceEqual(png);
            if (changed || AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                File.WriteAllBytes(fullPath, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
