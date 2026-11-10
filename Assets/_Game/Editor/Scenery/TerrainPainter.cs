using System.Collections.Generic;
using SeteVidas.World.Scenery;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Terreno natural na cena (plano de assets, Etapa 2): troca o tile provisório pelo tile de solo/rocha que o
    /// <see cref="TerrainAutotile"/> escolhe para cada célula (<see cref="FortressTerrain"/> diz o material) e espalha
    /// tufos e seixos sobre a superfície. Só muda o desenho: os tiles continuam com colisão de quadrado cheio.
    /// Arte: Tools/EnvironmentSprites/gerar.ps1 (TerrainSpriteGen).
    /// </summary>
    public static class TerrainPainter
    {
        public const string TerrainArt = "Assets/_Game/Art/Environments/Exterior/Terrain";
        public const string ExteriorArt = "Assets/_Game/Art/Environments/Exterior";
        const string TileFolder = "Assets/_Game/ScriptableObjects/Environment/Tiles";

        static readonly Dictionary<string, Tile> s_tiles = new Dictionary<string, Tile>();

        /// <summary>Pinta o terreno e cria os detalhes. Devolve quantos tiles trocou.</summary>
        public static int Paint(Tilemap ground, Tilemap climbable, IEnumerable<FortressLayout.SlopeRect> slopes, Transform detailsParent)
        {
            s_tiles.Clear();
            var slopeSolid = new HashSet<Vector2Int>();
            foreach (var s in slopes)
                foreach (var p in TerrainAutotile.SlopePieces(FortressTerrain.MaterialAt(s.X0, s.Y0), s.X0, s.Y0, s.W, s.H, s.RisesRight))
                    if (p.Solid) slopeSolid.Add(new Vector2Int(p.X, p.Y));

            bool Solid(int x, int y)
            {
                var c = new Vector3Int(x, y, 0);
                return ground.HasTile(c) || (climbable != null && climbable.HasTile(c)) || slopeSolid.Contains(new Vector2Int(x, y));
            }

            int painted = 0, missing = 0;
            var detailSprites = new Dictionary<string, Sprite>();
            foreach (var cell in Cells())
            {
                int x = cell.x, y = cell.y;
                if (!ground.HasTile(new Vector3Int(x, y, 0))) continue;
                string mat = FortressTerrain.MaterialAt(x, y);
                if (mat == "") continue;
                var tile = TileFor(TerrainAutotile.TileFor(mat, Solid, FortressTerrain.MaterialAt, x, y));
                if (tile == null) { missing++; continue; }
                ground.SetTile(new Vector3Int(x, y, 0), tile);
                painted++;

                if (detailsParent == null || !FortressTerrain.DetailsAllowed(x, y)) continue;
                var details = Solid(x, y + 1)
                    ? TerrainAutotile.DeepDetails(TerrainAutotile.BandFor(mat, Solid, FortressTerrain.MaterialAt, x, y), x, y)
                    : TerrainAutotile.TopDetails(mat, x, y);
                foreach (var d in details)
                {
                    if (!detailSprites.TryGetValue(d.Sprite, out var sprite))
                        detailSprites[d.Sprite] = sprite = LoadSprite(d.Sprite);
                    if (sprite == null) continue;
                    var go = new GameObject(d.Sprite);
                    go.transform.SetParent(detailsParent, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = SceneryLayers.DefaultOrder(SceneryLayers.TerrainDetail);
                    // pivot central: a base do sprite fica na superfície
                    go.transform.position = new Vector3(d.X, d.Y + sprite.bounds.extents.y, 0f);
                }
            }
            if (missing > 0)
                Debug.LogError($"[Sete Vidas] Terreno: {missing} tile(s) sem arte. Rode Tools/EnvironmentSprites/gerar.ps1 e reimporte.");
            return painted;
        }

        /// <summary>Todas as células cobertas por alguma região de material.</summary>
        static IEnumerable<Vector2Int> Cells()
        {
            var seen = new HashSet<Vector2Int>();
            foreach (var r in FortressTerrain.Regions())
                for (int x = r.X0; x <= r.X1; x++)
                    for (int y = r.Y0; y <= r.Y1; y++)
                        if (seen.Add(new Vector2Int(x, y))) yield return new Vector2Int(x, y);
        }

        /// <summary>Sprite do terreno pelo nome (tiles, rampas e tufos em Terrain/; seixos em Exterior/).</summary>
        public static Sprite LoadSprite(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{TerrainArt}/{name}.png")
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ExteriorArt}/{name}.png");
            if (s == null) Debug.LogError($"[Sete Vidas] Terreno: falta {name}.png. Rode Tools/EnvironmentSprites/gerar.ps1");
            return s;
        }

        /// <summary>Tile (colisão de quadrado cheio) com o sprite do terreno; criado uma vez em ScriptableObjects/Environment/Tiles.</summary>
        public static Tile TileFor(string spriteName)
        {
            if (s_tiles.TryGetValue(spriteName, out var cached) && cached != null) return cached;
            var sprite = LoadSprite(spriteName);
            if (sprite == null) return null;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(TileFolder));
            string path = $"{TileFolder}/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            if (tile.sprite != sprite || tile.colliderType != Tile.ColliderType.Grid)
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid; // a colisão é sempre o quadrado cheio, nunca o desenho
                EditorUtility.SetDirty(tile);
            }
            s_tiles[spriteName] = tile;
            return tile;
        }
    }
}
