using System;
using System.Collections.Generic;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Regras puras do mapa do mundo (melhorias 14/11/2026; EditMode): o que o gato já explorou (pedaços de
    /// <see cref="Chunk"/>×<see cref="Chunk"/> tiles em volta dele, guardados no save), a compactação da silhueta e
    /// a pintura em pixels no jeito do Hollow Knight: miolo escuro e borda clara onde já passou; onde ainda não
    /// passou, nada aparece (as ligações mostram por onde seguir).
    /// </summary>
    public static class WorldMapRules
    {
        /// <summary>Lado do pedaço explorado (tiles).</summary>
        public const int Chunk = 8;
        /// <summary>Pedaços revelados em volta do gato: 2 para cada lado na horizontal, 1 na vertical (~a tela).</summary>
        public const int RevealX = 2, RevealY = 1;

        // ------------------------------------------------------------ exploração

        public static int ChunkKey(int cx, int cy) => ((cx & 0xFFFF) << 16) | (cy & 0xFFFF);

        public static Vector2Int ChunkOf(Vector2 mapPoint) =>
            new Vector2Int(Mathf.FloorToInt(mapPoint.x / Chunk), Mathf.FloorToInt(mapPoint.y / Chunk));

        public static Vector2Int ChunkOfCell(int mx, int my) =>
            new Vector2Int(FloorDiv(mx, Chunk), FloorDiv(my, Chunk));

        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

        /// <summary>Marca os pedaços em volta do ponto do mapa. Retorna quantos pedaços novos.</summary>
        public static int MarkAround(SaveData data, Vector2 mapPoint, int rx = RevealX, int ry = RevealY)
        {
            if (data == null) return 0;
            data.mapChunks ??= new List<int>();
            var c = ChunkOf(mapPoint);
            int added = 0;
            for (int dx = -rx; dx <= rx; dx++)
                for (int dy = -ry; dy <= ry; dy++)
                {
                    int key = ChunkKey(c.x + dx, c.y + dy);
                    if (data.mapChunks.Contains(key)) continue;
                    data.mapChunks.Add(key);
                    added++;
                }
            return added;
        }

        /// <summary>Marca todos os pedaços do retângulo (a sala inteira). Retorna quantos pedaços novos.</summary>
        public static int MarkRect(SaveData data, int mx0, int my0, int w, int h)
        {
            if (data == null || w <= 0 || h <= 0) return 0;
            data.mapChunks ??= new List<int>();
            var a = ChunkOfCell(mx0, my0);
            var b = ChunkOfCell(mx0 + w - 1, my0 + h - 1);
            int added = 0;
            for (int cx = a.x; cx <= b.x; cx++)
                for (int cy = a.y; cy <= b.y; cy++)
                {
                    int key = ChunkKey(cx, cy);
                    if (data.mapChunks.Contains(key)) continue;
                    data.mapChunks.Add(key);
                    added++;
                }
            return added;
        }

        public static bool Explored(HashSet<int> explored, int mx, int my)
        {
            var c = ChunkOfCell(mx, my);
            return explored != null && explored.Contains(ChunkKey(c.x, c.y));
        }

        public static HashSet<int> ExploredSet(SaveData data) =>
            data?.mapChunks != null ? new HashSet<int>(data.mapChunks) : new HashSet<int>();

        // ------------------------------------------------------------ silhueta compactada

        public static string Pack(bool[] cells)
        {
            if (cells == null || cells.Length == 0) return "";
            var bytes = new byte[(cells.Length + 7) / 8];
            for (int i = 0; i < cells.Length; i++)
                if (cells[i]) bytes[i >> 3] |= (byte)(1 << (i & 7));
            return Convert.ToBase64String(bytes);
        }

        public static bool[] Unpack(string bits, int count)
        {
            var cells = new bool[Math.Max(0, count)];
            if (string.IsNullOrEmpty(bits) || count <= 0) return cells;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(bits); }
            catch (FormatException) { return cells; }
            for (int i = 0; i < count && (i >> 3) < bytes.Length; i++)
                cells[i] = (bytes[i >> 3] & (1 << (i & 7))) != 0;
            return cells;
        }

        /// <summary>
        /// Espaço de uma cena sem salas marcadas (a Fortaleza, aberta): ar com chão a até <paramref name="maxAboveGround"/>
        /// tiles embaixo. Corta o céu, deixa corredores, salões e o caminho sobre os muros.
        /// </summary>
        public static bool[] OpenAirSpace(bool[] solid, int w, int h, int maxAboveGround = 10)
        {
            var space = new bool[w * h];
            for (int x = 0; x < w; x++)
            {
                int sinceGround = int.MaxValue;
                for (int y = 0; y < h; y++)
                {
                    if (solid[y * w + x]) { sinceGround = 0; continue; }
                    if (sinceGround != int.MaxValue) sinceGround++;
                    space[y * w + x] = sinceGround <= maxAboveGround;
                }
            }
            return space;
        }

        /// <summary>
        /// Deixa as salas inteiras (no jeito do Hollow Knight, que não desenha plataformas): pedaços que não são espaço,
        /// cercados de espaço por todos os lados (sem encostar na borda da grade nem em outro bloco maior) e com até
        /// <paramref name="maxSize"/> células, viram espaço. Plataformas, saliências e prateleiras somem; chão, paredes
        /// e degraus ficam.
        /// </summary>
        public static int FillIslands(bool[] space, int w, int h, int maxSize = 60)
        {
            var seen = new bool[w * h];
            var stack = new Stack<int>();
            var part = new List<int>();
            int filled = 0;
            for (int start = 0; start < space.Length; start++)
            {
                if (space[start] || seen[start]) continue;
                part.Clear();
                bool open = false;   // encosta na borda: é rocha de fora, não ilha
                seen[start] = true;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    part.Add(i);
                    int x = i % w, y = i / w;
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1) open = true;
                    if (part.Count > maxSize) open = true;
                    Visit(x + 1, y); Visit(x - 1, y); Visit(x, y + 1); Visit(x, y - 1);
                }
                if (open) continue;
                foreach (int i in part) space[i] = true;
                filled += part.Count;
            }
            return filled;

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                int j = y * w + x;
                if (space[j] || seen[j]) return;
                seen[j] = true;
                stack.Push(j);
            }
        }

        // ------------------------------------------------------------ salas afastadas

        /// <summary>
        /// Deslocamento de uma sala no mapa "explodido": o centro dela se afasta da origem por <paramref name="spread"/>
        /// (1,25 = 25% mais longe), sem mudar o tamanho. Salas que não se sobrepunham continuam sem se sobrepor, e o
        /// vão entre vizinhas cresce com a distância entre elas.
        /// </summary>
        public static Vector2Int SpreadDelta(Vector2 roomCenter, float spread) =>
            new Vector2Int(Mathf.RoundToInt(roomCenter.x * (spread - 1f)), Mathf.RoundToInt(roomCenter.y * (spread - 1f)));

        /// <summary>Pixels do traço de ligação: anda na horizontal de a até x de b, depois na vertical até b.</summary>
        public static List<Vector2Int> LinkPixels(Vector2Int a, Vector2Int b)
        {
            var list = new List<Vector2Int>();
            int sx = Math.Sign(b.x - a.x), sy = Math.Sign(b.y - a.y);
            int x = a.x, y = a.y;
            list.Add(new Vector2Int(x, y));
            while (x != b.x) { x += sx; list.Add(new Vector2Int(x, y)); }
            while (y != b.y) { y += sy; list.Add(new Vector2Int(x, y)); }
            return list;
        }

        // ------------------------------------------------------------ pintura

        /// <summary>Cores de um lugar: miolo explorado, borda explorada, contorno ainda não explorado.</summary>
        public struct Palette { public Color32 Fill, Edge, Ghost; }

        public static Palette PaletteFor(string region)
        {
            region ??= "";
            if (region.StartsWith("Floresta"))
                return new Palette { Fill = C(0x1f, 0x33, 0x2a), Edge = C(0xb4, 0xd6, 0xa8), Ghost = C(0x3c, 0x52, 0x46) };
            if (region.StartsWith("Estrada"))
                return new Palette { Fill = C(0x33, 0x2c, 0x22), Edge = C(0xe2, 0xcf, 0x9c), Ghost = C(0x55, 0x4c, 0x3a) };
            // Cidadela (e o que vier sem nome): pedra azulada
            return new Palette { Fill = C(0x22, 0x2a, 0x3a), Edge = C(0xc6, 0xd2, 0xe6), Ghost = C(0x40, 0x4a, 0x5e) };
        }

        static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 0xff);

        /// <summary>
        /// Pinta o mapa inteiro, 1 pixel por tile (linha a linha de baixo para cima, como o Texture2D), dentro de
        /// <paramref name="bounds"/>. Borda = célula de espaço encostada (nos 4 lados) em algo que não é espaço.
        /// </summary>
        public static Color32[] Paint(WorldMapData map, RectInt bounds, HashSet<int> explored)
        {
            var px = new Color32[bounds.width * bounds.height];
            if (map == null) return px;
            foreach (var s in map.scenes)
            {
                if (s == null || s.w <= 0) continue;
                var pal = PaletteFor(s.region);
                for (int y = 0; y < s.h; y++)
                    for (int x = 0; x < s.w; x++)
                    {
                        int mx = s.x0 + x, my = s.y0 + y;
                        if (!s.Space(mx, my)) continue;
                        int ix = mx - bounds.x, iy = my - bounds.y;
                        if (ix < 0 || iy < 0 || ix >= bounds.width || iy >= bounds.height) continue;
                        bool edge = !s.Space(mx + 1, my) || !s.Space(mx - 1, my) || !s.Space(mx, my + 1) || !s.Space(mx, my - 1);

                        if (!Explored(explored, mx, my)) continue;   // onde o gato não foi, nada aparece
                        Color32 c = edge ? pal.Edge : pal.Fill;
                        if (c.a == 0) continue;
                        px[iy * bounds.width + ix] = c;
                    }
            }
            // as ligações (portas, passagens) por cima, só no vão entre as salas
            if (map.links != null)
                foreach (var l in map.links)
                {
                    var a = new Vector2Int(Mathf.FloorToInt(l.ax), Mathf.FloorToInt(l.ay));
                    var b = new Vector2Int(Mathf.FloorToInt(l.bx), Mathf.FloorToInt(l.by));
                    bool seen = Explored(explored, a.x, a.y) || Explored(explored, b.x, b.y);
                    var sa = SceneAt(map, l.ax, l.ay);
                    var pal = PaletteFor(sa != null ? sa.region : "");
                    if (!seen) continue;   // ligação só a partir de uma sala já vista
                    Color32 c = pal.Edge;
                    foreach (var p in LinkPixels(a, b))
                    {
                        if (AnySpace(map, p.x, p.y)) continue;   // dentro da sala, a sala manda
                        int ix = p.x - bounds.x, iy = p.y - bounds.y;
                        if (ix < 0 || iy < 0 || ix >= bounds.width || iy >= bounds.height) continue;
                        px[iy * bounds.width + ix] = c;
                    }
                }
            return px;
        }

        static bool AnySpace(WorldMapData map, int mx, int my)
        {
            foreach (var s in map.scenes) if (s != null && s.Space(mx, my)) return true;
            return false;
        }

        static WorldMapData.SceneMap SceneAt(WorldMapData map, float x, float y) => SceneAt(map, new Vector2(x, y));

        /// <summary>Em que cena do mapa fica o ponto (o lugar mostrado no título).</summary>
        public static WorldMapData.SceneMap SceneAt(WorldMapData map, Vector2 mapPoint)
        {
            if (map == null) return null;
            int mx = Mathf.FloorToInt(mapPoint.x), my = Mathf.FloorToInt(mapPoint.y);
            foreach (var s in map.scenes)
                if (s != null && mx >= s.x0 && my >= s.y0 && mx < s.x0 + s.w && my < s.y0 + s.h) return s;
            return null;
        }

        /// <summary>Centro da vista: o ponto pedido, preso para o mapa não sair do painel (ou centrado se couber).</summary>
        public static Vector2 ClampCenter(Vector2 center, RectInt bounds, Vector2 panelPixels, float zoom)
        {
            Vector2 half = panelPixels / (2f * Mathf.Max(0.01f, zoom));
            float cx = bounds.width <= half.x * 2f ? bounds.x + bounds.width * 0.5f : Mathf.Clamp(center.x, bounds.x + half.x, bounds.xMax - half.x);
            float cy = bounds.height <= half.y * 2f ? bounds.y + bounds.height * 0.5f : Mathf.Clamp(center.y, bounds.y + half.y, bounds.yMax - half.y);
            return new Vector2(cx, cy);
        }
    }
}
