using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.World
{
    /// <summary>
    /// Para cada parede quebrável da cena (parede rachada da Fortaleza, tronco rachado da Floresta), acha o vão FECHADO
    /// de um dos lados (busca pelas células vazias a partir da parede; se passar de <see cref="MaxCells"/>, aquele lado é
    /// aberto, não é segredo) e o cobre com um tilemap só de desenho, copiando o tile de dentro da parede mais próxima
    /// (a cobertura parece a própria parede). O <see cref="SecretCover"/> tira a cobertura quando a parede quebra ou o gato
    /// entra.
    ///
    /// Roda sozinho ao carregar cada cena (pedido do usuário, 14/11/2026: "ainda está vendo atrás da parede rachada": a
    /// cena dele foi montada antes da cobertura existir), então vale sem remontar a Fortaleza nem a Floresta. Os menus de
    /// montagem também chamam <see cref="Build"/> (a cobertura fica gravada na cena e aqui é pulada).
    /// </summary>
    public static class SecretCovers
    {
        /// <summary>Um segredo maior que isto (células) não é um "cofre": é o resto da fase.</summary>
        const int MaxCells = 220;
        public const int Order = 20;   // na frente das moedas, do baú, das placas e do gato (some quando ele entra)
        const string GroundName = "Ground", BackgroundName = "Background";
        /// <summary>Até onde (passos) a área maior atrás da parede fica coberta: mais que meia tela.</summary>
        const int NearSteps = 18;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()  // Play sem recarregar o domínio
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var ground = FindGround(scene);
            if (ground == null) return;
            Build(ground.layoutGrid != null ? ground.layoutGrid.transform : ground.transform.parent, ground);
        }

        static Tilemap FindGround(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                    if (map.name == GroundName) return map;
            return null;
        }

        /// <summary>Monta as coberturas das paredes quebráveis da cena de <paramref name="parent"/>. Devolve quantas fez.</summary>
        public static int Build(Transform parent, Tilemap ground)
        {
            if (ground == null || parent == null) return 0;
            var scene = parent.gameObject.scene;
            // paredes que já têm cobertura (gravada pelo menu de montagem): não cobre de novo
            var covered = new HashSet<BreakableWall>();
            foreach (var c in Object.FindObjectsByType<SecretCover>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.gameObject.scene == scene && c.Wall != null) covered.Add(c.Wall);

            int made = 0;
            Transform root = null;
            var back = Sibling(ground, BackgroundName);
            foreach (var wall in Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (wall.gameObject.scene != scene || covered.Contains(wall)) continue;
                var col = wall.GetComponent<Collider2D>();
                if (col == null) continue;
                var b = col.bounds;
                var wallCells = new HashSet<Vector3Int>();
                for (int x = Mathf.FloorToInt(b.min.x + 0.05f); x <= Mathf.FloorToInt(b.max.x - 0.05f); x++)
                    for (int y = Mathf.FloorToInt(b.min.y + 0.05f); y <= Mathf.FloorToInt(b.max.y - 0.05f); y++)
                        wallCells.Add(new Vector3Int(x, y, 0));
                int y0 = Mathf.FloorToInt(b.min.y + 0.05f), y1 = Mathf.FloorToInt(b.max.y - 0.05f);
                float wallX = b.center.x;
                foreach (int side in new[] { -1, 1 })
                {
                    int sx = side < 0 ? Mathf.FloorToInt(b.min.x + 0.05f) - 1 : Mathf.FloorToInt(b.max.x - 0.05f) + 1;
                    // 1) vão fechado (cofre): coberto até quebrar ou entrar
                    HashSet<Vector3Int> pocket = null;
                    bool open = false;
                    for (int y = y0; y <= y1 && pocket == null; y++)
                    {
                        var start = new Vector3Int(sx, y, 0);
                        if (ground.HasTile(start) || wallCells.Contains(start)) continue;
                        pocket = Flood(ground, wallCells, start);
                        if (pocket == null) open = true;
                    }
                    int mode = 0;
                    // 2) área maior (uma sala que continua): o interior perto da parede, coberto quando o gato está do outro lado
                    if (pocket == null && open && back != null)
                    {
                        pocket = Near(ground, back, wallCells, sx, y0, y1);
                        mode = side;
                    }
                    if (pocket == null || pocket.Count == 0) continue;
                    if (root == null)
                    {
                        root = new GameObject("Segredos escondidos").transform;
                        root.SetParent(parent, false);
                    }
                    var map = Cover(ground, back, wall.name + (mode == 0 ? "" : side < 0 ? " (esquerda)" : " (direita)"), pocket);
                    if (map == null) continue;
                    var go = new GameObject("Segredo atrás de " + wall.name);
                    go.transform.SetParent(root, false);
                    go.AddComponent<SecretCover>().Configure(wall, map, Bounds(pocket), mode, wallX);
                    made++;
                }
            }
            return made;
        }

        /// <summary>Células vazias ligadas a <paramref name="start"/>; null se o lado é aberto (passou do limite).</summary>
        static HashSet<Vector3Int> Flood(Tilemap ground, HashSet<Vector3Int> wall, Vector3Int start)
        {
            if (ground.HasTile(start) || wall.Contains(start)) return null;
            var seen = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            var dirs = new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (seen.Contains(n) || ground.HasTile(n) || wall.Contains(n)) continue;
                    seen.Add(n);
                    if (seen.Count > MaxCells) return null;
                    queue.Enqueue(n);
                }
            }
            return seen;
        }

        static Tilemap Sibling(Tilemap ground, string name)
        {
            var grid = ground.layoutGrid != null ? ground.layoutGrid.transform : ground.transform.parent;
            if (grid == null) return null;
            foreach (var m in grid.GetComponentsInChildren<Tilemap>(true)) if (m.name == name) return m;
            return null;
        }

        /// <summary>
        /// O interior perto da parede, do lado aberto: células vazias com parede de fundo (é dentro de um prédio; fora, no
        /// céu, não cobre), a até <see cref="NearSteps"/> passos, da base da parede até 6 acima do topo dela.
        /// </summary>
        static HashSet<Vector3Int> Near(Tilemap ground, Tilemap back, HashSet<Vector3Int> wall, int sx, int y0, int y1)
        {
            var seen = new HashSet<Vector3Int>();
            var dist = new Dictionary<Vector3Int, int>();
            var queue = new Queue<Vector3Int>();
            for (int y = y0; y <= y1; y++)
            {
                var c = new Vector3Int(sx, y, 0);
                if (ground.HasTile(c) || wall.Contains(c) || !back.HasTile(c)) continue;
                seen.Add(c); dist[c] = 0; queue.Enqueue(c);
            }
            var dirs = new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (dist[c] >= NearSteps) continue;
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (seen.Contains(n) || n.y < y0 - 1 || n.y > y1 + 6) continue;
                    if (ground.HasTile(n) || wall.Contains(n) || !back.HasTile(n)) continue;
                    seen.Add(n); dist[n] = dist[c] + 1; queue.Enqueue(n);
                }
            }
            return seen;
        }

        /// <summary>
        /// Paredes de fundo que a cobertura repete em vez da pedra/terra de perto (por fora igual ao lado de dentro): a das
        /// cabaninhas da Floresta (ForestTerrain.CabinBackTile) e a do Templo dos Primatas (D6, o nicho da chave).
        /// </summary>
        static readonly string[] InteriorBackPrefixes = { "tile_flo_cabana_fundo_", "tile_pri_templo_fundo_" };

        static bool IsInteriorBack(TileBase t)
        {
            if (t == null) return false;
            foreach (var p in InteriorBackPrefixes) if (t.name.StartsWith(p)) return true;
            return false;
        }
        /// <summary>A janela velha que o ForestPainter põe na parede de dentro de cada cabaninha.</summary>
        const string CabinWindowName = "Janela da cabaninha";

        /// <summary>
        /// O tilemap da cobertura (irmão do chão): cada célula com o tile de dentro da parede mais próxima. Numa cabaninha
        /// da Floresta (pedido do usuário, 14/11/2026: "faça a parte de fora ser igual a de dentro"), a cobertura é a
        /// própria parede de tábuas de dentro, com a janela quebrada repetida por cima; só o baú fica escondido.
        /// </summary>
        static Tilemap Cover(Tilemap ground, Tilemap back, string wallName, HashSet<Vector3Int> cells)
        {
            var grid = ground.layoutGrid != null ? ground.layoutGrid.transform : ground.transform.parent;
            var go = new GameObject("Cobertura: " + wallName);
            go.transform.SetParent(grid, false);
            var map = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = Order;
            int painted = 0;
            bool cabin = false;
            if (back != null)
                foreach (var c in cells)
                {
                    if (IsInteriorBack(back.GetTile(c))) { cabin = true; break; }
                }
            TileBase any = null;   // no meio de uma sala grande (longe das paredes), a mesma pedra de perto da parede
            foreach (var c in cells) { any = cabin ? back.GetTile(c) : InnerTile(ground, c); if (any != null) break; }
            if (cabin) CopyCabinWindows(go.transform, cells);
            foreach (var c in cells)
            {
                var tile = (cabin ? back.GetTile(c) : InnerTile(ground, c)) ?? any;
                if (tile == null) continue;
                map.SetTile(c, tile);
                painted++;
            }
            if (painted == 0)
            {
                if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
                return null;
            }
            return map;
        }

        /// <summary>Repete na cobertura (na frente dela) cada janela de cabaninha que fica dentro do vão coberto.</summary>
        static void CopyCabinWindows(Transform cover, HashSet<Vector3Int> cells)
        {
            var scene = cover.gameObject.scene;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr.gameObject.scene != scene || sr.name != CabinWindowName) continue;
                var p = sr.transform.position;
                if (!cells.Contains(new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), 0))) continue;
                var go = new GameObject(CabinWindowName + " (cobertura)");
                go.transform.SetParent(cover, false);
                go.transform.position = p;
                var copy = go.AddComponent<SpriteRenderer>();
                copy.sprite = sr.sprite;
                copy.sortingOrder = Order + 1;
            }
        }

        /// <summary>O tile de uma célula de parede "de dentro" (as 4 vizinhas também são parede) mais perto de c.</summary>
        static TileBase InnerTile(Tilemap ground, Vector3Int c)
        {
            TileBase fallback = null;
            for (int r = 1; r <= 8; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var n = c + new Vector3Int(dx, dy, 0);
                        var t = ground.GetTile(n);
                        if (t == null) continue;
                        if (fallback == null) fallback = t;
                        if (ground.HasTile(n + Vector3Int.left) && ground.HasTile(n + Vector3Int.right) &&
                            ground.HasTile(n + Vector3Int.up) && ground.HasTile(n + Vector3Int.down)) return t;
                    }
            return fallback;
        }

        static Rect Bounds(HashSet<Vector3Int> cells)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var c in cells) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
            return new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }
    }
}
