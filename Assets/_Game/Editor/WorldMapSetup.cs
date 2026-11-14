using System.Collections.Generic;
using System.IO;
using SeteVidas.Core;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Melhorias 14/11/2026: gera o mapa do mundo (estilo Hollow Knight) lendo o tilemap "Ground" de cada cena:
    /// Fortaleza + Estrada + as regiões da Floresta, cada uma no lugar dela. Rode de novo depois de mudar uma cena
    /// (inclusive ajustes à mão na Fortaleza). O resultado vai para <c>Resources/WorldMap.asset</c>.
    /// Na Floresta, cada sala vira uma peça com contorno próprio, afastada das vizinhas (<see cref="Spread"/>), e as
    /// portas e passagens viram traços de ligação: no jogo as salas encostam umas nas outras (o anel), e juntas
    /// ficavam ilegíveis no mapa.
    /// </summary>
    public static class WorldMapSetup
    {
        const string AssetPath = "Assets/_Game/Resources/WorldMap.asset";

        /// <summary>
        /// A Fortaleza fica à esquerda da Estrada: a borda direita dela (x 254) encosta logo antes do começo da Estrada
        /// (x 0), com o chão no mesmo nível (a saída D-FIM e a R1 têm chão em 0).
        /// </summary>
        static readonly Vector2Int FortressOffset = new Vector2Int(-256, 0);

        /// <summary>
        /// Quanto as salas da Floresta se afastam no mapa: o centro de cada sala fica 25% mais longe da origem
        /// (vão de ~10 tiles entre vizinhas; o tamanho das salas não muda).
        /// </summary>
        public const float Spread = 1.25f;

        struct Entry { public string Scene, Path, Region; public Vector2Int Offset; public bool Fortress; }

        /// <summary>Uma saída para outra cena (ou sala): de onde sai e para qual chegada vai.</summary>
        struct Exit { public string Scene; public Vector2 At; public string ToScene, ToEntrance; }

        static List<Entry> Entries()
        {
            var list = new List<Entry>
            {
                new Entry { Scene = "Fortress_MVP", Path = GameConstants.FortressScenePath, Region = "Cidadela do Sol Eterno", Offset = FortressOffset, Fortress = true },
            };
            // a Floresta já usa coordenadas do mundo (todas as cenas no mesmo sistema)
            foreach (var reg in ForestLayout.Regions())
                list.Add(new Entry { Scene = reg.Scene, Path = ForestTestSetup.Folder + "/" + reg.Scene + ".unity", Region = reg.MapRegion, Offset = Vector2Int.zero });
            return list;
        }

        [MenuItem("Tools/Sete Vidas/22. Mapa do mundo: gerar silhuetas (Fortaleza + Floresta)", priority = 70)]
        public static void Bake()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var data = AssetDatabase.LoadAssetAtPath<WorldMapData>(AssetPath);
            bool created = data == null;
            if (created) data = ScriptableObject.CreateInstance<WorldMapData>();
            data.scenes.Clear();
            data.links.Clear();

            int done = 0;
            var missing = new List<string>();
            var exits = new List<Exit>();
            var entrances = new Dictionary<string, Vector2>();   // "cena:id" → posição
            foreach (var e in Entries())
            {
                if (!File.Exists(e.Path)) { missing.Add(e.Scene); continue; }
                var scene = SceneManager.GetSceneByPath(e.Path);
                bool opened = false;
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(e.Path, OpenSceneMode.Additive);
                    opened = true;
                }
                try
                {
                    var pieces = BakeScene(scene, e, exits, entrances);
                    if (pieces.Count > 0) { data.scenes.AddRange(pieces); done++; }
                }
                finally
                {
                    if (opened) EditorSceneManager.CloseScene(scene, true);
                }
            }
            BuildLinks(data, exits, entrances);

            if (created)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                AssetDatabase.CreateAsset(data, AssetPath);
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            string note = missing.Count > 0 ? " Faltam (gere antes): " + string.Join(", ", missing) + "." : "";
            Debug.Log($"[Sete Vidas] Mapa do mundo: {done} cenas, {data.scenes.Count} salas, {data.links.Count} ligações ({AssetPath}).{note} No jogo: M ou o botão Exibir abre o mapa.");
        }

        // ------------------------------------------------------------ uma cena

        static List<WorldMapData.SceneMap> BakeScene(Scene scene, Entry e, List<Exit> exits, Dictionary<string, Vector2> entrances)
        {
            var result = new List<WorldMapData.SceneMap>();
            Tilemap ground = null;
            var rooms = new List<Rect>();
            var pins = new List<WorldMapData.Pin>();   // em coordenadas da cena
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var tm in root.GetComponentsInChildren<Tilemap>(true))
                    if (tm.gameObject.name == "Ground" && ground == null) ground = tm;
                foreach (var r in root.GetComponentsInChildren<MapRoom>(true)) rooms.Add(r.Area);
                foreach (var s in root.GetComponentsInChildren<Sanctuary>(true))
                    pins.Add(new WorldMapData.Pin { kind = SeteVidas.Save.MapMarker.Sanctuary, id = s.Id, x = s.transform.position.x, y = s.transform.position.y });
                foreach (var c in root.GetComponentsInChildren<WoodChest>(true))
                    pins.Add(new WorldMapData.Pin { kind = SeteVidas.Save.MapMarker.Chest, id = c.Id, x = c.transform.position.x, y = c.transform.position.y });
                foreach (var x in root.GetComponentsInChildren<RoomExit>(true))
                    if (!string.IsNullOrEmpty(x.TargetEntrance))
                        exits.Add(new Exit { Scene = e.Scene, At = x.GetComponent<Collider2D>() != null ? (Vector2)x.GetComponent<Collider2D>().bounds.center : (Vector2)x.transform.position,
                            ToScene = string.IsNullOrEmpty(x.TargetScene) ? e.Scene : x.TargetScene, ToEntrance = x.TargetEntrance });
                foreach (var d in root.GetComponentsInChildren<DialogueTrigger>(true))
                {
                    var so = new SerializedObject(d);
                    string toScene = so.FindProperty("loadSceneAfter").stringValue, toEntrance = so.FindProperty("loadEntranceAfter").stringValue;
                    if (!string.IsNullOrEmpty(toScene) && !string.IsNullOrEmpty(toEntrance))
                        exits.Add(new Exit { Scene = e.Scene, At = d.transform.position, ToScene = toScene, ToEntrance = toEntrance });
                }
                foreach (var n in root.GetComponentsInChildren<RoomEntrance>(true))
                    entrances[e.Scene + ":" + n.Id] = n.SpawnPoint;
            }
            if (ground == null) { Debug.LogWarning("[Sete Vidas] Mapa do mundo: \"" + e.Scene + "\" sem tilemap Ground."); return result; }
            ground.CompressBounds();
            System.Func<int, int, bool> solid = (x, y) => ground.HasTile(ground.WorldToCell(new Vector3(x + 0.5f, y + 0.5f, 0f)));

            if (e.Fortress)
            {
                // a Fortaleza é uma peça só: as salas do FortressLayout juntas (as muralhas e a torre se sobrepõem)
                var cb = ground.cellBounds;
                Vector3 o = ground.CellToWorld(new Vector3Int(cb.xMin, cb.yMin, 0));
                int gx0 = Mathf.RoundToInt(o.x), gy0 = Mathf.RoundToInt(o.y), w = cb.size.x, h = cb.size.y;
                var layoutRooms = FortressLayout.MapRooms();
                var space = new bool[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int sx = gx0 + x, sy = gy0 + y;
                        foreach (var m in layoutRooms)
                        {
                            if (sx < m.X0 || sx > m.X1 || sy < m.Y0 || sy > m.Y1) continue;
                            if (m.Whole || !solid(sx, sy)) { space[y * w + x] = true; break; }
                        }
                    }
                // plataformas, saliências do poço e prateleiras somem: as salas ficam inteiras
                WorldMapRules.FillIslands(space, w, h);
                foreach (var m in layoutRooms)
                    if (m.Whole)
                        for (int sy = m.Y0; sy <= m.Y1; sy++)
                            for (int sx = m.X0; sx <= m.X1; sx++)
                            {
                                int x = sx - gx0, y = sy - gy0;
                                if (x >= 0 && y >= 0 && x < w && y < h) space[y * w + x] = true;
                            }
                var piece = MakePiece(e, space, gx0, gy0, w, h, e.Offset, false, pins);
                if (piece != null)
                {
                    // entrar numa sala da Fortaleza revela a sala inteira (a queda do penhasco, as muralhas da hera...)
                    foreach (var m in layoutRooms)
                        piece.reveals.Add(new RectInt(m.X0 + e.Offset.x, m.Y0 + e.Offset.y, m.X1 - m.X0 + 1, m.Y1 - m.Y0 + 1));
                    result.Add(piece);
                }
                return result;
            }

            // Floresta: cada sala é uma peça, afastada das vizinhas
            foreach (var r in rooms)
            {
                int gx0 = Mathf.FloorToInt(r.xMin), gy0 = Mathf.FloorToInt(r.yMin);
                int w = Mathf.CeilToInt(r.xMax) - gx0, h = Mathf.CeilToInt(r.yMax) - gy0;
                var space = new bool[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        space[y * w + x] = !solid(gx0 + x, gy0 + y) && r.Contains(new Vector2(gx0 + x + 0.5f, gy0 + y + 0.5f));
                WorldMapRules.FillIslands(space, w, h, 40);   // raízes e blocos soltos no meio somem
                var delta = WorldMapRules.SpreadDelta(r.center, Spread);
                var roomPins = new List<WorldMapData.Pin>();
                foreach (var p in pins) if (r.Contains(new Vector2(p.x, p.y))) roomPins.Add(p);
                var piece = MakePiece(e, space, gx0, gy0, w, h, e.Offset + delta, true, roomPins);
                if (piece != null) result.Add(piece);
            }
            return result;
        }

        /// <summary>Corta as bordas vazias e monta a peça (pins passam para as coordenadas do mapa).</summary>
        static WorldMapData.SceneMap MakePiece(Entry e, bool[] space, int gx0, int gy0, int w, int h, Vector2Int offset,
            bool wholeRoom, List<WorldMapData.Pin> pins)
        {
            int ax = w, ay = h, bx = -1, by = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (space[y * w + x]) { ax = Mathf.Min(ax, x); ay = Mathf.Min(ay, y); bx = Mathf.Max(bx, x); by = Mathf.Max(by, y); }
            if (bx < 0) return null;
            int tw = bx - ax + 1, th = by - ay + 1;
            var trimmed = new bool[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                    trimmed[y * tw + x] = space[(y + ay) * w + (x + ax)];
            var piece = new WorldMapData.SceneMap
            {
                scene = e.Scene, region = e.Region, offsetX = offset.x, offsetY = offset.y,
                x0 = gx0 + ax + offset.x, y0 = gy0 + ay + offset.y, w = tw, h = th, wholeRoom = wholeRoom,
            };
            foreach (var p in pins)
                piece.pins.Add(new WorldMapData.Pin { kind = p.kind, id = p.id, x = p.x + offset.x, y = p.y + offset.y });
            piece.SetCells(trimmed);
            return piece;
        }

        // ------------------------------------------------------------ ligações

        static void BuildLinks(WorldMapData data, List<Exit> exits, Dictionary<string, Vector2> entrances)
        {
            var linked = new HashSet<long>();
            var pieces = data.scenes;
            // 1) salas da Floresta encostadas (porta na parede em comum): espaço de uma ao lado do espaço da outra
            for (int i = 0; i < pieces.Count; i++)
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    var a = pieces[i]; var b = pieces[j];
                    if (!a.wholeRoom || !b.wholeRoom) continue;
                    if (!Door(a, b, out Vector2 pa, out Vector2 pb)) continue;
                    data.links.Add(new WorldMapData.Link { ax = pa.x, ay = pa.y, bx = pb.x, by = pb.y });
                    linked.Add(Key(i, j));
                }
            // 2) passagens para outra cena (ou sala) que não encostam: a saída liga à chegada
            foreach (var x in exits)
            {
                if (!entrances.TryGetValue(x.ToScene + ":" + x.ToEntrance, out Vector2 to)) continue;
                var a = data.FindAt(x.Scene, x.At);
                var b = data.FindAt(x.ToScene, to);
                if (a == null || b == null || a == b) continue;
                long k = Key(pieces.IndexOf(a), pieces.IndexOf(b));
                if (!linked.Add(k)) continue;
                data.links.Add(new WorldMapData.Link { ax = x.At.x + a.offsetX, ay = x.At.y + a.offsetY, bx = to.x + b.offsetX, by = to.y + b.offsetY });
            }
        }

        static long Key(int i, int j) => i < j ? ((long)i << 32) | (uint)j : ((long)j << 32) | (uint)i;

        /// <summary>
        /// Porta entre duas salas encostadas (coordenadas do jogo, antes de afastar): células de espaço de uma com
        /// espaço da outra na mesma célula ou ao lado. Devolve o meio da porta, no mapa, em cada sala.
        /// </summary>
        static bool Door(WorldMapData.SceneMap a, WorldMapData.SceneMap b, out Vector2 pa, out Vector2 pb)
        {
            pa = pb = default;
            int ax0 = a.x0 - a.offsetX, ay0 = a.y0 - a.offsetY, bx0 = b.x0 - b.offsetX, by0 = b.y0 - b.offsetY;
            if (ax0 > bx0 + b.w || bx0 > ax0 + a.w || ay0 > by0 + b.h || by0 > ay0 + a.h) return false;   // longe
            var doorA = new List<Vector2Int>();
            var doorB = new List<Vector2Int>();
            for (int y = Mathf.Max(ay0, by0 - 1); y < Mathf.Min(ay0 + a.h, by0 + b.h + 1); y++)
                for (int x = Mathf.Max(ax0, bx0 - 1); x < Mathf.Min(ax0 + a.w, bx0 + b.w + 1); x++)
                {
                    if (!a.Space(x + a.offsetX, y + a.offsetY)) continue;
                    foreach (var d in new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        int nx = x + d.x, ny = y + d.y;
                        if (!b.Space(nx + b.offsetX, ny + b.offsetY)) continue;
                        doorA.Add(new Vector2Int(x, y));
                        doorB.Add(new Vector2Int(nx, ny));
                        break;
                    }
                }
            if (doorA.Count == 0) return false;
            int mid = doorA.Count / 2;
            pa = new Vector2(doorA[mid].x + 0.5f + a.offsetX, doorA[mid].y + 0.5f + a.offsetY);
            pb = new Vector2(doorB[mid].x + 0.5f + b.offsetX, doorB[mid].y + 0.5f + b.offsetY);
            return true;
        }
    }
}
