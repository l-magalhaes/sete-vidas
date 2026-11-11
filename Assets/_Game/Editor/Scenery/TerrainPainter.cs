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
        /// <summary>Baldes e tinas no chão: a gárgula em cima deles não ganha poça (a água cai dentro). Mundo, em tiles.</summary>
        public static readonly List<Vector2> PuddleBlockers = new List<Vector2>();
        /// <summary>Poças postas no último Paint.</summary>
        public static int Puddles { get; private set; }
        /// <summary>Tinas de água postas no último Paint (gárgulas de fora).</summary>
        public static int Tubs { get; private set; }
        /// <summary>Gárgula com o chão abaixo desta altura é de fora (pátio e muralhas): ganha tina em vez de poça.</summary>
        public const float TubMaxFloor = 30f;
        const string TubSpritePath = "Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_balde_agua.png";

        /// <summary>Monta a lista de baldes e tinas da Fortaleza (FortressLayout e FortressDressing).</summary>
        public static void CollectFortressBuckets()
        {
            PuddleBlockers.Clear();
            foreach (var o in FortressLayout.Objects())
                if (o.Kind == "noisy" && o.Text == "balde") PuddleBlockers.Add(new Vector2(o.X, o.Y));
            foreach (var i in FortressDressing.Items())
                if (i.AssetId != null && i.AssetId.Contains("balde")) PuddleBlockers.Add(new Vector2(i.X, i.Y));
        }

        public static int Paint(Tilemap ground, Tilemap climbable, Tilemap background, IEnumerable<FortressLayout.SlopeRect> slopes, Transform detailsParent)
        {
            s_tiles.Clear();
            Puddles = 0;
            Tubs = 0;
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

            Sprite[] puddleFrames = null; // quadros da poça (a da gárgula e a da água que a tina derrama)
            // tina de água embaixo das gárgulas de fora (pedido do usuário): o gato passa por ela e ela tomba; duas no mesmo fio ficam lado a lado
            Sprite tubSprite = null;
            var tubs = new List<float>();
            bool Tub(float x, float surface)
            {
                if (tubSprite == null) tubSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TubSpritePath);
                if (tubSprite == null) { Debug.LogError($"[Sete Vidas] Falta {TubSpritePath}. Rode Tools/EnvironmentSprites/gerar.ps1"); return false; }
                float w = tubSprite.bounds.size.x, h = tubSprite.bounds.size.y;
                foreach (float other in tubs)
                    if (Mathf.Abs(other - x) < w + 0.1f) x = other + (x >= other ? 1f : -1f) * (w + 0.1f);
                tubs.Add(x);
                var go = new GameObject("Tina de água");
                go.transform.SetParent(detailsParent, false);
                go.layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Interactable); // só o gato a toca (gatilho)
                go.transform.position = new Vector3(Mathf.Round(x * 16f) / 16f, surface + h * 0.5f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tubSprite;
                sr.sortingOrder = 2;
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic; // não é empurrada nem é chão: só tomba quando o gato passa
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(w - 0.04f, h - 0.02f);
                go.AddComponent<SeteVidas.World.WaterTub>().Configure(sr, puddleFrames);
                Tubs++;
                return true;
            }

            // poça no chão embaixo do fio d'água da gárgula (pedido do usuário), menos onde já há um balde ou uma tina
            void Puddle(TerrainAutotile.Detail drip, int topY, float offset)
            {
                if (detailsParent == null) return;
                int col = Mathf.FloorToInt(drip.X);
                int floor = int.MinValue;
                for (int yy = topY - 1; yy > topY - 80; yy--)
                    if (Solid(col, yy)) { floor = yy; break; }
                if (floor == int.MinValue) return;
                float surface = floor + 1;
                foreach (var b in PuddleBlockers)
                    if (Mathf.Abs(b.x - drip.X) < 0.9f && Mathf.Abs(b.y - surface) < 0.6f) return;
                if (puddleFrames == null)
                {
                    puddleFrames = new Sprite[TerrainAutotile.PuddleFrames];
                    for (int i = 0; i < puddleFrames.Length; i++) puddleFrames[i] = LoadSprite(TerrainAutotile.PuddleFrameName(i));
                }
                if (puddleFrames[0] == null) return;
                const float px = 1f / 16f;

                // gárgula de fora (pátio e muralhas, chão abaixo de TubMaxFloor): a Ordem guarda a água da chuva numa tina;
                // dentro do castelo, a água pinga no chão e faz a poça
                if (surface < TubMaxFloor && Tub(drip.X, surface)) return;

                var go = new GameObject("env_sol_arq_poca");
                go.transform.SetParent(detailsParent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = puddleFrames[0];
                sr.sortingOrder = 2; // por cima do chão e da vinha, atrás do gato
                // assentada no chão: a linha de baixo entra 1 px no tile, no grid de pixel
                float cx = Mathf.Round(drip.X * 16f) / 16f;
                go.transform.position = new Vector3(cx, surface + puddleFrames[0].bounds.extents.y - px, 0f);
                go.AddComponent<SeteVidas.World.SpriteLoop>().Configure(puddleFrames, 1f / TerrainAutotile.PuddleFps, offset);
                Puddles++;
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
                        var drip = TerrainAutotile.GargoyleDrip(x, y, airLeft, mat);
                        Drip(drip, TerrainAutotile.DripOffset(x, y));
                        Puddle(drip, y, TerrainAutotile.DripOffset(x, y));
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
