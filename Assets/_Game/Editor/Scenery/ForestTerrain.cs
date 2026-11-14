using System.Collections.Generic;
using SeteVidas.World.Scenery;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Cenário da Floresta (F5) como DADOS: material de cada sala, quanto a praga já tomou o lugar e a grade da cena com a
    /// "massa da floresta" em volta das salas. Usado pelo Unity (<see cref="ForestPainter"/>) e pela prévia sem o Unity
    /// (Tools/LevelPreview/ForestArtPreview). Só muda o DESENHO: a colisão continua a do <see cref="ForestLayout"/>.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell).
    /// </summary>
    public static class ForestTerrain
    {
        // ------------------------------------------------------------ dados por sala

        /// <summary>Ruínas dos Primatas: concreto (a estação, a torre, o prédio, a estrada negra e a coluna).</summary>
        static readonly string[] ConcreteRooms = { "A5", "B4", "C3", "C5", "G2" };
        /// <summary>O Templo dos Primatas (onde fica a Chave do Templo): pedra com grama e flores.</summary>
        static readonly string[] TempleRooms = { "D6" };
        /// <summary>Tomadas pela praga: o Bosque Morto, o Ninho e o Coração (o chão já é o da praga).</summary>
        static readonly string[] BlightRooms = { "C7", "E1", "E2", "E3", "E4", "E4p", "E5", "E6", "F1" };
        /// <summary>Onde a praga está chegando (brotos raros, poeira roxa): do meio do Vale ao Refúgio e a subida final.</summary>
        static readonly string[] SpreadingRooms = { "C4", "C4p", "C5", "C6", "D1", "D2", "D3", "D4", "D5", "D6", "G1", "G2", "G3" };

        /// <summary>
        /// A estrada saindo da Fortaleza (R1, R2) usa o chão de fora da Fortaleza (ext_solo, com a rocha roxa embaixo): as
        /// duas fases parecem ligadas (pedido do usuário, 14/11/2026). Da cabeceira da ponte em diante, a floresta.
        /// </summary>
        static readonly string[] FortressSoilRooms = { "R1", "R2" };

        public static bool IsFortressSoil(string material) { return material == TerrainAutotile.Soil; }

        /// <summary>Tile de chão da célula: a regra da floresta, ou a da Fortaleza no chão de fora dela.</summary>
        public static string GroundTile(string material, TerrainAutotile.SolidAt solid, int x, int y)
        {
            return IsFortressSoil(material) ? TerrainAutotile.TileFor(material, solid, null, x, y) : ForestAutotile.TileFor(material, solid, x, y);
        }

        /// <summary>Material da floresta para vinha, parede do fundo, rampas e enfeites (no chão da Fortaleza: o solo).</summary>
        public static string DecorMaterial(string material) { return IsFortressSoil(material) ? ForestAutotile.Soil : material; }

        /// <summary>Enfeites em cima do chão (tufos da Fortaleza no chão dela; samambaias, fungos e brotos na floresta).</summary>
        public static List<TerrainAutotile.Detail> TopDetailsFor(string material, int blight, int x, int y)
        {
            return IsFortressSoil(material) ? TerrainAutotile.TopDetails(material, x, y) : ForestAutotile.TopDetails(material, blight, x, y);
        }

        public static List<TerrainAutotile.Detail> CeilingDetailsFor(string material, int x, int y)
        {
            return IsFortressSoil(material) ? new List<TerrainAutotile.Detail>() : ForestAutotile.CeilingDetails(material, x, y);
        }

        public static string MaterialOf(string roomId)
        {
            if (System.Array.IndexOf(FortressSoilRooms, roomId) >= 0) return TerrainAutotile.Soil;
            if (System.Array.IndexOf(ConcreteRooms, roomId) >= 0) return ForestAutotile.Concrete;
            if (System.Array.IndexOf(TempleRooms, roomId) >= 0) return ForestAutotile.Temple;
            if (System.Array.IndexOf(BlightRooms, roomId) >= 0) return ForestAutotile.Blight;
            return ForestAutotile.Soil;
        }

        /// <summary>0 sem praga, 1 chegando, 2 tomado (plano, F5: "a corrupção cresce em direção ao Coração").</summary>
        public static int BlightOf(string roomId)
        {
            if (System.Array.IndexOf(BlightRooms, roomId) >= 0) return 2;
            if (System.Array.IndexOf(SpreadingRooms, roomId) >= 0) return 1;
            return 0;
        }

        /// <summary>
        /// A estrada fica ao ar livre: a massa só preenche embaixo da estrada (o abismo da ponte fica aberto pelo recorte da
        /// abertura). Na floresta, preenche tudo o que está fora das salas.
        /// </summary>
        public static bool FillsVoid(string scene, int x, int y)
        {
            if (scene == ForestLayout.Road) return y < 0;    // a estrada continua no mesmo nível além das salas
            return true;
        }

        /// <summary>Margem em volta das salas (tiles): a câmera (30 × 17 tiles) nunca vê além da massa.</summary>
        public const int MarginX = 24, MarginY = 16;
        /// <summary>Quanto uma abertura na casca de uma sala (porta, buraco, saída) avança para fora dela, aberta.</summary>
        public const int CarveDepth = 14;

        // ------------------------------------------------------------ a grade da cena

        public class Model
        {
            public ForestLayout.Region Region;
            public int X0, Y0, X1, Y1;
            readonly HashSet<long> _ground = new HashSet<long>(), _climb = new HashSet<long>(), _back = new HashSet<long>();
            readonly HashSet<long> _slope = new HashSet<long>(), _carved = new HashSet<long>();
            public readonly List<SlopeInfo> Slopes = new List<SlopeInfo>();

            public static long Key(int x, int y) { return ((long)x << 32) ^ (uint)y; }

            public bool Ground(int x, int y) { return _ground.Contains(Key(x, y)); }
            public bool Climb(int x, int y) { return _climb.Contains(Key(x, y)); }
            public bool Back(int x, int y) { return _back.Contains(Key(x, y)); }
            /// <summary>Tem colisão (chão, vinha ou o pedaço cheio de uma rampa).</summary>
            public bool Real(int x, int y) { long k = Key(x, y); return _ground.Contains(k) || _climb.Contains(k) || _slope.Contains(k); }

            public ForestLayout.Room RoomAt(int x, int y)
            {
                ForestLayout.Room found = null;
                foreach (var r in Region.Rooms)
                    if (x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H) found = r; // a última listada vale (casca compartilhada)
                return found;
            }

            public bool InRoom(int x, int y) { return RoomAt(x, y) != null; }

            /// <summary>
            /// Na estrada (ao ar livre), a casca das salas acima do chão (teto e paredes entre as salas) só tem colisão, sem
            /// desenho (pedido do usuário, 14/11/2026: "não precisa ter assets fechando o céu"). O chão continua.
            /// </summary>
            public bool Hidden(int x, int y)
            {
                if (Region.Scene != ForestLayout.Road || !_ground.Contains(Key(x, y))) return false;
                foreach (var r in Region.Rooms)
                {
                    if (x < r.X || x >= r.X + r.W || y < r.Y || y >= r.Y + r.H) continue;
                    if (y == r.Y) return false;                                           // o chão da sala
                    if (x == r.X || x == r.X + r.W - 1 || y == r.Y + r.H - 1) return true;  // paredes e teto
                }
                return false;
            }

            /// <summary>Célula de massa: fora das salas, fora das aberturas e onde a cena preenche.</summary>
            public bool Mass(int x, int y)
            {
                return !InRoom(x, y) && !_carved.Contains(Key(x, y)) && FillsVoid(Region.Scene, x, y);
            }

            /// <summary>Sólido para o desenho: o chão de verdade e a massa (as bordas da sala não ficam expostas para a massa).</summary>
            public bool Visual(int x, int y)
            {
                if (_vis != null && x >= _vx && y >= _vy && x < _vx + _vw && y < _vy + _vh) return _vis[(y - _vy) * _vw + (x - _vx)];
                return Real(x, y) || Mass(x, y);
            }

            bool[] _vis;
            int _vx, _vy, _vw, _vh;

            /// <summary>Guarda <see cref="Visual"/> numa grade (a regra dos tiles consulta cada célula muitas vezes).</summary>
            internal void Freeze()
            {
                _vis = null;
                int pad = 6;
                int vx = X0 - pad, vy = Y0 - pad, vw = X1 - X0 + 1 + 2 * pad, vh = Y1 - Y0 + 1 + 2 * pad;
                var grid = new bool[vw * vh];
                for (int y = 0; y < vh; y++)
                    for (int x = 0; x < vw; x++)
                        grid[y * vw + x] = (Real(vx + x, vy + y) && !Hidden(vx + x, vy + y)) || Mass(vx + x, vy + y);
                _vx = vx; _vy = vy; _vw = vw; _vh = vh; _vis = grid;
            }

            /// <summary>Material da célula: o da sala; fora delas, o da sala mais próxima.</summary>
            public string MaterialAt(int x, int y)
            {
                var r = RoomAt(x, y) ?? Nearest(x, y);
                return r == null ? ForestAutotile.Soil : MaterialOf(r.Id);
            }

            public int BlightAt(int x, int y)
            {
                var r = RoomAt(x, y) ?? Nearest(x, y);
                return r == null ? 0 : BlightOf(r.Id);
            }

            ForestLayout.Room Nearest(int x, int y)
            {
                ForestLayout.Room best = null;
                int bestD = int.MaxValue;
                foreach (var r in Region.Rooms)
                {
                    int dx = x < r.X ? r.X - x : x >= r.X + r.W ? x - (r.X + r.W - 1) : 0;
                    int dy = y < r.Y ? r.Y - y : y >= r.Y + r.H ? y - (r.Y + r.H - 1) : 0;
                    int d = dx > dy ? dx : dy;
                    if (d < bestD) { bestD = d; best = r; }
                }
                return best;
            }

            // ---- cabaninhas atrás das portas rachadas
            readonly Dictionary<long, string> _cabin = new Dictionary<long, string>();
            readonly HashSet<long> _cabinInside = new HashSet<long>();
            /// <summary>Papel da célula de chão na cabaninha: "parede", "telhado", "piso" ou null.</summary>
            public string Cabin(int x, int y) { string r; return _cabin.TryGetValue(Key(x, y), out r) ? r : null; }
            /// <summary>Dentro de uma cabaninha (o vão atrás da porta rachada): parede de fundo de tábuas.</summary>
            public bool CabinInside(int x, int y) { return _cabinInside.Contains(Key(x, y)); }
            internal void SetCabin(int x, int y, string role) { _cabin[Key(x, y)] = role; }
            internal void AddCabinInside(int x, int y) { _cabinInside.Add(Key(x, y)); }
            /// <summary>Lado de dentro de cada cabaninha: {x0, y0, x1, y1} (células), para a janela no meio da parede.</summary>
            public readonly List<int[]> CabinBoxes = new List<int[]>();

            internal void SetGround(int x, int y, bool on) { if (on) _ground.Add(Key(x, y)); else _ground.Remove(Key(x, y)); }
            internal void SetClimb(int x, int y, bool on) { if (on) _climb.Add(Key(x, y)); else _climb.Remove(Key(x, y)); }
            internal void SetBack(int x, int y, bool on) { if (on) _back.Add(Key(x, y)); else _back.Remove(Key(x, y)); }
            internal void AddSlope(int x, int y) { _slope.Add(Key(x, y)); }
            internal void Carve(int x, int y) { _carved.Add(Key(x, y)); }
        }

        public class SlopeInfo
        {
            public string Name; public int X0, Y0, W, H; public bool RisesRight; public string Material;
        }

        /// <summary>Monta a grade da cena a partir do ForestLayout (tiles na ordem: cascas, miolo, portas).</summary>
        public static Model Build(ForestLayout.Region reg)
        {
            var m = new Model { Region = reg };
            int bx0, by0, bx1, by1;
            reg.Bounds(out bx0, out by0, out bx1, out by1);
            m.X0 = bx0 - MarginX; m.Y0 = by0 - MarginY; m.X1 = bx1 + MarginX; m.Y1 = by1 + MarginY;

            foreach (var t in reg.Tiles())
                for (int x = t.X0; x <= t.X1; x++)
                    for (int y = t.Y0; y <= t.Y1; y++)
                    {
                        if (t.Map == ForestLayout.Map.Climbable) m.SetClimb(x, y, !t.Clear);
                        else if (t.Map == ForestLayout.Map.Background) m.SetBack(x, y, !t.Clear);
                        else
                        {
                            m.SetGround(x, y, !t.Clear);
                            if (t.Clear) m.SetClimb(x, y, false); // porta aberta na casca: some a vinha também
                        }
                    }

            foreach (var o in reg.Objects)
            {
                if (o.Kind != "slope") continue;
                int x0 = (int)System.Math.Round(o.X), y0 = (int)System.Math.Round(o.Y);
                var s = new SlopeInfo { Name = o.Name, X0 = x0, Y0 = y0, W = (int)o.W, H = (int)o.H, RisesRight = o.Facing > 0 };
                s.Material = m.MaterialAt(x0, y0);
                m.Slopes.Add(s);
                foreach (var p in ForestAutotile.SlopePieces(s.Material, s.X0, s.Y0, s.W, s.H, s.RisesRight))
                    if (p.Solid) m.AddSlope(p.X, p.Y);
            }

            CarveOpenings(m);
            Cabins(m);
            m.Freeze();
            return m;
        }

        /// <summary>
        /// Aberturas na casca de uma sala que dão para fora de todas as salas (saídas para outra cena, o teto aberto do
        /// Fundo do Vale, o abismo da ponte): a massa abre um corredor de <see cref="CarveDepth"/> tiles para fora, para o
        /// gato não parecer entrar numa parede.
        /// </summary>
        static void CarveOpenings(Model m)
        {
            foreach (var r in m.Region.Rooms)
            {
                for (int x = r.X; x < r.X + r.W; x++)
                {
                    Carve(m, x, r.Y, 0, -1);
                    Carve(m, x, r.Y + r.H - 1, 0, 1);
                }
                for (int y = r.Y; y < r.Y + r.H; y++)
                {
                    Carve(m, r.X, y, -1, 0);
                    Carve(m, r.X + r.W - 1, y, 1, 0);
                }
            }
        }

        static void Carve(Model m, int x, int y, int dx, int dy)
        {
            if (m.Real(x, y)) return;            // a casca está fechada aqui
            for (int i = 1; i <= CarveDepth; i++)
            {
                int cx = x + dx * i, cy = y + dy * i;
                if (m.InRoom(cx, cy)) return;    // a abertura dá em outra sala
                m.Carve(cx, cy);
            }
        }

        /// <summary>
        /// Tile de uma célula de massa: logo abaixo de um chão (até <see cref="ForestAutotile.MassGroundDepth"/>), continua o
        /// chão (terra, transição); no resto, a massa. "" = nada (ar).
        /// </summary>
        public static string MassTile(Model m, int x, int y)
        {
            if (!m.Mass(x, y)) return "";
            string mat = m.MaterialAt(x, y);
            if (IsFortressSoil(mat)) return TerrainAutotile.TileFor(mat, m.Visual, null, x, y);
            int depth = ForestAutotile.Depth(m.Visual, x, y);
            if (depth <= ForestAutotile.MassGroundDepth || HasAirNear(m, x, y))
                return ForestAutotile.TileFor(mat, m.Visual, x, y);
            return ForestAutotile.MassTileFor(mat, x, y);
        }

        // ------------------------------------------------------------ poços das saídas verticais

        /// <summary>
        /// O poço aberto na massa por uma saída para cima ou para baixo (pedido do usuário, 14/11/2026: o buraco ficava
        /// transparente). Leva parede de fundo (terra escura), vinhas nas duas laterais e a luz da passagem dentro dele.
        /// </summary>
        public class Shaft
        {
            public ForestLayout.Obj Exit;
            /// <summary>Colunas do buraco e as linhas do poço (da casca da sala até o fim do recorte).</summary>
            public int X0, X1, Y0, Y1;
            public bool Up;
            /// <summary>Linha da casca (o teto ou o chão da sala) onde o buraco começa.</summary>
            public int Shell;
        }

        public static List<Shaft> Shafts(Model m)
        {
            var list = new List<Shaft>();
            foreach (var o in m.Region.Objects)
            {
                if (o.Kind != "exit" || (o.Facing != 2 && o.Facing != -2)) continue;
                bool up = o.Facing == 2;
                var r = m.RoomAt((int)System.Math.Floor(o.X), (int)System.Math.Floor(up ? o.Y : o.Y + 3f));
                if (r == null) continue;
                var s = new Shaft { Exit = o, Up = up };
                s.X0 = (int)System.Math.Round(o.X - o.W * 0.5f); s.X1 = s.X0 + (int)System.Math.Round(o.W) - 1;
                s.Shell = up ? r.Y + r.H - 1 : r.Y;
                s.Y0 = up ? s.Shell : s.Shell - CarveDepth;
                s.Y1 = up ? s.Shell + CarveDepth : s.Shell;
                list.Add(s);
            }
            return list;
        }

        // ------------------------------------------------------------ sombras (esconderijos)

        /// <summary>
        /// Uma zona de sombra da furtividade e QUEM a faz (regra do usuário, 14/11/2026: toda sombra tem um objeto que a
        /// projeta, e o desenho da sombra segue a forma dele). Debaixo de teto ou saliência, o próprio teto; no resto, uma
        /// árvore no fundo com a copa por cima da zona e o tronco dentro dela. A zona de jogo (o retângulo) não muda.
        /// </summary>
        public class ShadowInfo
        {
            public ForestLayout.Obj Obj;
            /// <summary>Canto de baixo à esquerda e tamanho, em tiles.</summary>
            public int X0, Y0, W, H;
            /// <summary>Teto ou saliência logo acima: ele faz a sombra (sem árvore).</summary>
            public bool Roof;
            /// <summary>Tiles de tronco abaixo da zona até o chão (a zona pode estar num degrau).</summary>
            public int Below;
            public bool Blight;

            /// <summary>Desenho da sombra (W × H tiles): a borda de cima segue a copa da árvore (ou reta, embaixo do teto).</summary>
            public string ShadeName { get { return "flo_sombra_" + (Roof ? "teto" : "copa") + "_" + W + "x" + H + (Roof ? "" : "_b" + Below); } }
            /// <summary>Altura do desenho da sombra (tiles): da zona até o chão embaixo dela (a copa faz sombra até o chão).</summary>
            public int ShadeH { get { return H + (Roof ? 0 : Below); } }
            /// <summary>Árvore que faz a sombra ("" embaixo de teto): (W + 2) × (H + Crown + Below) tiles.</summary>
            public string TreeName { get { return Roof ? "" : "flo_arvore_sombra_" + W + "x" + H + "_b" + Below + (Blight ? "_praga" : ""); } }
            /// <summary>Tiles da árvore acima da zona (a copa).</summary>
            public const int Crown = 6;
        }

        public static List<ShadowInfo> Shadows(Model m)
        {
            var list = new List<ShadowInfo>();
            foreach (var o in m.Region.Objects)
            {
                if (o.Kind != "shadow") continue;
                var s = new ShadowInfo { Obj = o };
                s.W = (int)System.Math.Round(o.W); s.H = (int)System.Math.Round(o.H);
                s.X0 = (int)System.Math.Round(o.X - o.W * 0.5f); s.Y0 = (int)System.Math.Round(o.Y - o.H * 0.5f);
                int roofed = 0;
                for (int x = s.X0; x < s.X0 + s.W; x++)
                    for (int y = s.Y0 + s.H; y <= s.Y0 + s.H + 2; y++)
                        if (m.Real(x, y)) { roofed++; break; }
                s.Roof = roofed * 10 >= s.W * 6;
                if (!s.Roof)
                {
                    int trunkX = s.X0 + TrunkTile(s.W);
                    int below = 0;
                    while (below < 12 && !m.Real(trunkX, s.Y0 - 1 - below)) below++;
                    s.Below = below >= 12 ? 0 : below;
                }
                s.Blight = m.BlightAt(s.X0, s.Y0) >= 2;
                list.Add(s);
            }
            return list;
        }

        /// <summary>Coluna (tile, a partir da esquerda da zona) do tronco principal: um pouco à esquerda do meio.</summary>
        public static int TrunkTile(int w) { return (int)System.Math.Floor(w * 0.4); }

        /// <summary>
        /// Cabaninhas (pedido do usuário, 14/11/2026: "todas poderiam ser uma cabaninha pequena abandonada com a porta de
        /// madeira rachada"): o vão FECHADO atrás de cada tronco rachado (agora a porta) vira o lado de dentro (parede de
        /// fundo de tábuas) e o chão em volta dele vira a cabaninha: telhado em cima, piso embaixo, parede de tábuas nos lados.
        /// Só o desenho muda.
        /// </summary>
        static void Cabins(Model m)
        {
            const int MaxCells = 160;
            foreach (var o in m.Region.Objects)
            {
                if (o.Kind != "trunk") continue;
                int tx = (int)System.Math.Floor(o.X), ty = (int)System.Math.Floor(o.Y), th = o.H > 0 ? (int)System.Math.Round(o.H) : 4;
                HashSet<long> best = null;
                foreach (int side in new[] { -1, 1 })
                    for (int y = ty; y < ty + th; y++)
                    {
                        int sx = tx + side;
                        if (m.Real(sx, y)) continue;
                        var pocket = new HashSet<long>();
                        var queue = new Queue<int[]>();
                        pocket.Add(Model.Key(sx, y)); queue.Enqueue(new[] { sx, y });
                        bool open = false;
                        while (queue.Count > 0 && !open)
                        {
                            var c = queue.Dequeue();
                            int[][] dirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
                            foreach (var d in dirs)
                            {
                                int nx = c[0] + d[0], ny = c[1] + d[1];
                                if (nx == tx && ny >= ty && ny < ty + th) continue;          // a porta
                                if (m.Real(nx, ny) || pocket.Contains(Model.Key(nx, ny))) continue;
                                pocket.Add(Model.Key(nx, ny)); queue.Enqueue(new[] { nx, ny });
                                if (pocket.Count > MaxCells) { open = true; break; }
                            }
                        }
                        if (!open && (best == null || pocket.Count < best.Count)) best = pocket;
                        break;
                    }
                if (best == null) continue;
                int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = int.MinValue, by1 = int.MinValue;
                foreach (var k in best)
                {
                    int x = (int)(k >> 32), y = (int)(uint)k;
                    m.AddCabinInside(x, y);
                    bx0 = System.Math.Min(bx0, x); bx1 = System.Math.Max(bx1, x);
                    by0 = System.Math.Min(by0, y); by1 = System.Math.Max(by1, y);
                }
                m.CabinBoxes.Add(new[] { bx0, by0, bx1, by1 });
                // pedido do usuário (14/11/2026, com esboço): a cabana é uma ESTRUTURA de madeira, não um bloco de tiles.
                // Esteios de tora nas paredes e nos cantos, viga com telhadinho em cima; o chão embaixo continua o da
                // floresta (a cabana fica em pé sobre ele).
                foreach (var k in best)
                {
                    int x = (int)(k >> 32), y = (int)(uint)k;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (!m.Ground(nx, ny) || (nx == tx && ny >= ty && ny < ty + th)) continue;
                            if (ny < by0) continue;                                        // o chão: da floresta
                            string role = ny > by1 || m.CabinInside(nx, ny - 1) ? "telhado" : "parede";
                            if (role == "telhado" && (nx < bx0 || nx > bx1)) role = "canto";   // o esteio sobe até a viga
                            string old = m.Cabin(nx, ny);
                            if (old == null || old == "parede") m.SetCabin(nx, ny, role);
                        }
                }
            }
        }

        /// <summary>Tile da cabaninha para a célula (variação sorteada pela posição).</summary>
        public static string CabinTile(string role, int x, int y)
        {
            // por fora, as mesmas tábuas em pé de dentro (pedido do usuário, 14/11/2026: "deixe a parte externa igual a
            // interna"); muda só de uma coluna para a outra, sem sorteio
            if (role == "parede") return "tile_flo_cabana_tabua_" + TerrainAutotile.Mod(x, 2);
            if (role == "canto") role = "telhado";                                          // o telhado cobre a casa toda
            return "tile_flo_cabana_" + role + "_" + TerrainAutotile.Mod(x, 2);
        }

        public static string CabinBackTile(int x, int y)
        {
            // tábuas em pé: a mesma fileira de cima a baixo (sem sorteio que repete quadradinho), muda só de uma coluna
            // para a outra; uma tábua faltando de vez em quando
            int v = TerrainAutotile.Mod(x, 2);
            if (TerrainAutotile.Hash(x, y / 3, 152) % 100u < 8u) v = 2;
            return "tile_flo_cabana_fundo_" + v;
        }

        /// <summary>Encosta numa abertura (o corredor recortado): a borda continua o chão, com contorno.</summary>
        static bool HasAirNear(Model m, int x, int y)
        {
            return !m.Visual(x + 1, y) || !m.Visual(x - 1, y) || !m.Visual(x, y - 1);
        }
    }
}
