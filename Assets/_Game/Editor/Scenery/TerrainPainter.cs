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
        public const string ArchitectureArt = "Assets/_Game/Art/Environments/Citadel/Architecture";
        public const string VegetationArt = "Assets/_Game/Art/Environments/Exterior/Vegetation";
        const string TileFolder = "Assets/_Game/ScriptableObjects/Environment/Tiles";

        static readonly Dictionary<string, Tile> s_tiles = new Dictionary<string, Tile>();
        /// <summary>Acontecimentos na parede do fundo: por cima dela (-10) e atrás dos enfeites (-5).</summary>
        const int WallEventOrder = -9;

        /// <summary>
        /// Pinta o terreno e a arquitetura (chão, parede do fundo e paredes escaláveis por época) e cria os detalhes
        /// (tufos, seixos, fendas e a gárgula em cima de cada vinha). Devolve quantos tiles trocou.
        /// </summary>
        public static int Paint(Tilemap ground, Tilemap climbable, Tilemap background, IEnumerable<FortressLayout.SlopeRect> slopes, Transform detailsParent)
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

            void Detail(TerrainAutotile.Detail d, int order = 1)
            {
                if (detailsParent == null) return;
                if (!detailSprites.TryGetValue(d.Sprite, out var sprite))
                    detailSprites[d.Sprite] = sprite = LoadSprite(d.Sprite);
                if (sprite == null) return;
                var go = new GameObject(d.Sprite);
                go.transform.SetParent(detailsParent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order; // 1 = terreno_detalhe; -9 = acontecimento na parede do fundo
                // pivot central: a base do sprite fica na superfície
                go.transform.position = new Vector3(d.X, d.Y + sprite.bounds.extents.y, 0f);

                // tufo de grama: curva quando o gato passa (Etapa 4)
                if (d.Sprite.StartsWith("env_ext_ter_tufo_"))
                {
                    var left = LoadCached(d.Sprite + "_l");
                    var right = LoadCached(d.Sprite + "_r");
                    if (left != null && right != null) go.AddComponent<BendOnPass>().Configure(sr, sprite, left, right);
                }
            }

            Sprite LoadCached(string name)
            {
                if (!detailSprites.TryGetValue(name, out var s)) detailSprites[name] = s = LoadSprite(name);
                return s;
            }

            // gota animada da gárgula: quadros env_sol_arq_gota_0..13 num SpriteLoop com atraso próprio
            Sprite[] dripFrames = null;
            void Drip(TerrainAutotile.Detail d, float offset)
            {
                if (detailsParent == null) return;
                if (dripFrames == null)
                {
                    dripFrames = new Sprite[TerrainAutotile.DripFrames];
                    for (int i = 0; i < dripFrames.Length; i++) dripFrames[i] = LoadSprite(TerrainAutotile.DripFrameName(i));
                }
                if (dripFrames[0] == null) return;
                var go = new GameObject("env_sol_arq_gota");
                go.transform.SetParent(detailsParent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dripFrames[0];
                sr.sortingOrder = 2; // por cima da vinha (1), atrás do gato (5)
                go.transform.position = new Vector3(d.X, d.Y + dripFrames[0].bounds.extents.y, 0f);
                go.AddComponent<SeteVidas.World.SpriteLoop>().Configure(dripFrames, 1f / TerrainAutotile.DripFps, offset);
            }

            bool Set(Tilemap map, int x, int y, string spriteName)
            {
                if (string.IsNullOrEmpty(spriteName)) return false;
                var tile = TileFor(spriteName);
                if (tile == null) { missing++; return false; }
                map.SetTile(new Vector3Int(x, y, 0), tile);
                painted++;
                return true;
            }

            foreach (var cell in Cells())
            {
                int x = cell.x, y = cell.y;
                var c = new Vector3Int(x, y, 0);
                string mat = FortressTerrain.MaterialAt(x, y);
                if (mat == "") continue;

                // parede do fundo (interior): a mesma época, escura
                if (background != null && background.HasTile(c) && Set(background, x, y, TerrainAutotile.BackTileFor(mat, x, y)))
                {
                    var ev = TerrainAutotile.WallEvent(Solid, x, y);
                    if (ev != null) Detail(ev, WallEventOrder);
                }

                // parede escalável por época, com a gárgula (ou calha) em cima de cada vinha
                if (climbable != null && climbable.HasTile(c) && Set(climbable, x, y, TerrainAutotile.ClimbTileFor(mat, Solid, x, y))
                    && !climbable.HasTile(new Vector3Int(x, y + 1, 0)))
                {
                    bool airLeft = !Solid(x - 1, y), airRight = !Solid(x + 1, y);
                    if (airLeft || airRight)
                    {
                        Detail(TerrainAutotile.Gargoyle(x, y, airLeft, mat));
                        Drip(TerrainAutotile.GargoyleDrip(x, y, airLeft, mat), TerrainAutotile.DripOffset(x, y));
                    }
                }

                if (!ground.HasTile(c)) continue;
                if (!Set(ground, x, y, TerrainAutotile.TileFor(mat, Solid, FortressTerrain.MaterialAt, x, y))) continue;
                if (!FortressTerrain.DetailsAllowed(x, y)) continue;
                var details = Solid(x, y + 1)
                    ? TerrainAutotile.DeepDetails(TerrainAutotile.BandFor(mat, Solid, FortressTerrain.MaterialAt, x, y), x, y)
                    : TerrainAutotile.TopDetails(mat, x, y);
                foreach (var d in details) Detail(d);
                if (!Solid(x, y - 1)) foreach (var d in TerrainAutotile.CeilingDetails(mat, x, y)) Detail(d); // raízes no teto
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
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ExteriorArt}/{name}.png")
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArchitectureArt}/{name}.png")
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{VegetationArt}/{name}.png");
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
