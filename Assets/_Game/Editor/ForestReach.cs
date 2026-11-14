using System;
using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Confere o graybox da Floresta (<see cref="ForestLayout"/>) sem o Unity: a partir das chegadas de cada cena, aonde o
    /// gato consegue ir com Ladino + Paladino (andar, pular, cair, escalar vinha, pulo de parede, agarrar borda, pogo em
    /// cacto/espinho/planta, cogumelo, quebrar tronco). Responde: todo baú e toda saída são alcançáveis? Existe algum
    /// lugar alcançável de onde não se volta para nenhuma saída (softlock)?
    ///
    /// Física aproximada com os valores do jogo (PlayerMovementSettings / WallClimbSettings / PlatformRules): pulo 4,
    /// 7 tiles/s, gravidade de subida 2·4/0,38², queda 1,8×, queda máxima 20; parede: pulo 2,5 para longe.
    /// É um "pode ser possível", não uma prova: pulos no limite passam aqui e podem ser difíceis no controle.
    /// C# 5 e sem UnityEngine (roda também no PowerShell: Tools/LevelPreview/checar_floresta.ps1).
    /// </summary>
    public static class ForestReach
    {
        const float Speed = 7f, JumpH = 4f, GUp = 2f * 4f / (0.38f * 0.38f), GDown = GUp * 1.8f, VMax = 20f;
        const float WallJumpH = 2.5f, PogoH = 7.2f, MushroomH = 12f, LedgeHands = 1.25f;
        // Slack: o gato pisa até a meia largura do corpo além da borda, nas duas pontas (0,33 + 0,33) e mais o arredondamento
        // das células: com 0,6 o alcance reprovava pulos que funcionam no jogo (os da Forest_Test_A, testados pelo usuário)
        const float HalfW = 0.33f, BodyH = 1.2f, Slack = 1.6f;

        public class Report
        {
            public string Scene;
            public readonly List<string> Problems = new List<string>();
            public readonly List<string> Notes = new List<string>();
            public int MinX, MinY, W, H;
            /// <summary>0 = nada, 1 = em pé alcançável, 2 = em pé fora de alcance, 3 = softlock.</summary>
            public byte[,] Stand;
            /// <summary>Em pé alcançável pela rota principal (as chegadas de <see cref="ForestLayout.Region.Main"/>).</summary>
            public bool[,] Main;
            internal Grid Grid;
            /// <summary>Pontos de pogo: x, y, 0 = fora de alcance, 1 = rota principal, 2 = outra chegada.</summary>
            internal List<int[]> Pogo = new List<int[]>();
        }

        public static List<Report> CheckAll()
        {
            var list = new List<Report>();
            foreach (var r in ForestLayout.Regions()) list.Add(Check(r));
            list.Add(CheckMap());
            return list;
        }

        /// <summary>O mapa da floresta é um só (o anel): o miolo de uma sala não pode invadir o de outra.</summary>
        public static Report CheckMap()
        {
            var rep = new Report { Scene = "mapa da floresta (salas sobrepostas)" };
            var rooms = new List<KeyValuePair<string, ForestLayout.Room>>();
            foreach (var reg in ForestLayout.Regions())
            {
                if (reg.MapRegion != ForestLayout.ForestMap) continue;
                foreach (var r in reg.Rooms) rooms.Add(new KeyValuePair<string, ForestLayout.Room>(reg.Scene, r));
            }
            for (int i = 0; i < rooms.Count; i++)
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    var a = rooms[i].Value; var b = rooms[j].Value;
                    if (a.InX0 <= b.InX1 && b.InX0 <= a.InX1 && a.InY0 <= b.InY1 && b.InY0 <= a.InY1)
                        rep.Problems.Add(a.Id + " (" + rooms[i].Key + ") invade " + b.Id + " (" + rooms[j].Key + ")");
                }
            return rep;
        }

        // ------------------------------------------------------------------ grade

        internal class Grid
        {
            public int MinX, MinY, W, H;
            public bool[,] Solid, Vine, OneWay, Hazard, Bounce;
            public bool In(int x, int y) { return x >= MinX && y >= MinY && x < MinX + W && y < MinY + H; }
            public bool S(int x, int y) { return In(x, y) && Solid[x - MinX, y - MinY]; }
            public bool Vn(int x, int y) { return In(x, y) && Vine[x - MinX, y - MinY]; }
            public bool Ow(int x, int y) { return In(x, y) && OneWay[x - MinX, y - MinY]; }
            public bool Hz(int x, int y) { return In(x, y) && Hazard[x - MinX, y - MinY]; }
            public bool Bn(int x, int y) { return In(x, y) && Bounce[x - MinX, y - MinY]; }
        }

        static Grid BuildGrid(ForestLayout.Region reg)
        {
            int x0, y0, x1, y1;
            reg.Bounds(out x0, out y0, out x1, out y1);
            var g = new Grid { MinX = x0 - 4, MinY = y0 - 6, W = x1 - x0 + 9, H = y1 - y0 + 12 };
            g.Solid = new bool[g.W, g.H]; g.Vine = new bool[g.W, g.H]; g.OneWay = new bool[g.W, g.H];
            g.Hazard = new bool[g.W, g.H]; g.Bounce = new bool[g.W, g.H];
            foreach (var t in reg.Tiles())
                for (int x = t.X0; x <= t.X1; x++)
                    for (int y = t.Y0; y <= t.Y1; y++)
                    {
                        if (!g.In(x, y)) continue;
                        if (t.Map == ForestLayout.Map.Ground) g.Solid[x - g.MinX, y - g.MinY] = !t.Clear;
                        else if (t.Map == ForestLayout.Map.Climbable) g.Vine[x - g.MinX, y - g.MinY] = !t.Clear;
                    }
            foreach (var o in reg.Objects)
            {
                int cy = (int)Math.Round(o.Y);
                switch (o.Kind)
                {
                    case "branch": case "sway": case "leaf":
                    {
                        float left = o.Kind == "leaf" ? o.X - o.W * 0.5f : o.X;
                        for (int x = (int)Math.Floor(left + 0.01f); x < left + o.W - 0.01f; x++) Set(g.OneWay, g, x, cy);
                        break;
                    }
                    case "rotten":
                        for (int x = (int)Math.Floor(o.X); x < o.X + o.W - 0.01f; x++) Set(g.OneWay, g, x, cy);
                        break;
                    case "mushroom":
                        for (int x = (int)Math.Floor(o.X - 0.875f); x < o.X + 0.875f; x++) { Set(g.Solid, g, x, cy); Set(g.Bounce, g, x, cy + 1); }
                        break;
                    case "spikes": case "gspikes":
                    {
                        float left = o.X - o.W * 0.5f;
                        for (int x = (int)Math.Floor(left + 0.01f); x < left + o.W - 0.01f; x++) Set(g.Hazard, g, x, cy);
                        break;
                    }
                    case "slope":
                    {
                        // rampa em degraus de 1 coluna: a altura no meio de cada coluna; mais íngreme que 1:1 escorrega
                        int sx = (int)Math.Round(o.X), w = (int)o.W, hgt = (int)o.H;
                        bool steep = o.H > o.W + 0.01f;
                        for (int i = 0; i < w; i++)
                        {
                            float hi = (o.Facing > 0 ? i + 0.5f : w - i - 0.5f) * hgt / (float)w;
                            int top = cy + (int)Math.Floor(hi);
                            for (int y = cy; y < top; y++) Set(g.Solid, g, sx + i, y);
                            if (steep) Set(g.Hazard, g, sx + i, top);
                        }
                        break;
                    }
                    case "sc_block":
                        // tampa/portão de atalho: sólido (confere-se com tudo fechado)
                        for (int x = (int)o.X; x < (int)o.X + (int)o.W; x++)
                            for (int y = (int)o.Y; y < (int)o.Y + (int)o.H; y++) Set(g.Solid, g, x, y);
                        break;
                }
            }
            return g;
        }

        static void Set(bool[,] a, Grid g, int x, int y) { if (g.In(x, y)) a[x - g.MinX, y - g.MinY] = true; }

        // ------------------------------------------------------------------ nós

        // Nó: em pé (célula x, pés em y), na parede (lado -1/+1), ou ponto de pogo (cacto, espinho, planta, cogumelo).
        class Node { public int Kind; public int X, Y, Side; public float FX, FY, H; public string Label; }

        const int StandK = 0, WallK = 1, PogoK = 2, GoalK = 3; // GoalK: gatilho de saída no ar (buraco do teto/chão)

        static bool IsStand(Grid g, int x, int y)
        {
            if (g.S(x, y)) return false;
            if (g.Hz(x, y) || g.Bn(x, y)) return false;
            return g.S(x, y - 1) || g.Ow(x, y);
        }

        static bool IsWall(Grid g, int x, int y, int side)
        {
            if (g.S(x, y) || g.S(x, y + 1)) return false;
            return g.Vn(x + side, y) || g.Vn(x + side, y + 1);
        }

        // ------------------------------------------------------------------ arcos

        static float RiseTime(float h) { return h <= 0f ? 0f : (float)Math.Sqrt(2f * h / GUp); }

        /// <summary>Altura relativa ao ápice depois de <paramref name="t"/> s caindo.</summary>
        static float FallDist(float t)
        {
            float tc = VMax / GDown;
            if (t <= tc) return 0.5f * GDown * t * t;
            return 0.5f * GDown * tc * tc + VMax * (t - tc);
        }

        static float FallTime(float d)
        {
            if (d <= 0f) return 0f;
            float tc = VMax / GDown, dc = 0.5f * GDown * tc * tc;
            if (d <= dc) return (float)Math.Sqrt(2f * d / GDown);
            return tc + (d - dc) / VMax;
        }

        static float YAt(float y0, float h, float t)
        {
            float tu = RiseTime(h);
            if (t <= tu) { float v0 = GUp * tu; return y0 + v0 * t - 0.5f * GUp * t * t; }
            return y0 + h - FallDist(t - tu);
        }

        static bool BodyFree(Grid g, float cx, float fy)
        {
            int xa = (int)Math.Floor(cx - HalfW), xb = (int)Math.Floor(cx + HalfW);
            int ya = (int)Math.Floor(fy + 0.05f), yb = (int)Math.Floor(fy + BodyH);
            for (int x = xa; x <= xb; x++)
                for (int y = ya; y <= yb; y++)
                    if (g.S(x, y)) return false;
            return true;
        }

        /// <summary>
        /// Dá para sair de (x0, y0) com ápice <paramref name="h"/> acima e chegar em (tx, ty)? <paramref name="anyPhase"/>:
        /// vale chegar subindo (parede, gatilho); senão só descendo (pousar). <paramref name="dirLock"/> ≠ 0 obriga a
        /// ir para esse lado (pulo de parede).
        /// </summary>
        static bool Arc(Grid g, float x0, float y0, float h, float tx, float ty, bool anyPhase, int dirLock, float maxSide)
        {
            if (ty > y0 + h + 0.001f) return false;
            float dx = tx - x0;
            if (dirLock != 0 && dx * dirLock < -0.5f) return false;
            float tu = RiseTime(h);
            float tDown = tu + FallTime(y0 + h - ty);
            var times = new List<float>();
            times.Add(tDown);
            if (anyPhase && ty >= y0)
            {
                // subindo: y(t) = ty
                float v0 = GUp * tu, a = 0.5f * GUp, disc = v0 * v0 - 4f * a * (ty - y0);
                if (disc >= 0f) times.Add((v0 - (float)Math.Sqrt(disc)) / (2f * a));
            }
            foreach (float T in times)
            {
                if (Math.Abs(dx) > Speed * T + Slack || Math.Abs(dx) > maxSide) continue;
                if (PathClear(g, x0, y0, h, tx, T, false) || PathClear(g, x0, y0, h, tx, T, true)) return true;
            }
            return false;
        }

        static bool PathClear(Grid g, float x0, float y0, float h, float tx, float T, bool late)
        {
            float dx = tx - x0, dist = Math.Abs(dx), dir = Math.Sign(dx);
            float tMove = dist / Speed, tStart = late ? Math.Max(0f, T - tMove) : 0f;
            const float dt = 0.025f;
            for (float t = dt; t < T - 0.001f; t += dt)
            {
                float moved = Math.Min(dist, Math.Max(0f, t - tStart) * Speed);
                float cx = x0 + dir * moved + 0.5f, fy = YAt(y0, h, t);
                if (!BodyFree(g, cx, fy)) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ busca

        public static Report Check(ForestLayout.Region reg)
        {
            var g = BuildGrid(reg);
            var rep = new Report { Scene = reg.Scene, MinX = g.MinX, MinY = g.MinY, W = g.W, H = g.H, Stand = new byte[g.W, g.H], Main = new bool[g.W, g.H], Grid = g };

            // vinha fora da parede: no jogo o tile de vinha tem colisão (vira chão), aqui não: tampa portas sem ninguém ver
            var loose = new List<string>();
            for (int x = g.MinX; x < g.MinX + g.W; x++)
                for (int y = g.MinY; y < g.MinY + g.H; y++)
                    if (g.Vn(x, y) && !g.S(x, y)) loose.Add("(" + x + ", " + y + ")");
            foreach (var o in reg.Objects)
                if (o.Kind == "sc_vine")
                    for (int y = (int)o.Y; y < (int)o.Y + (int)o.H; y++)
                        if (!g.S((int)o.X, y)) loose.Add("cipó " + o.Flag + " (" + (int)o.X + ", " + y + ")");
            if (loose.Count > 0)
                rep.Problems.Add("vinha fora da parede (no jogo vira chão sólido, pode tampar porta): " + string.Join(" ", loose.GetRange(0, Math.Min(6, loose.Count)).ToArray()) + (loose.Count > 6 ? " e mais " + (loose.Count - 6) : ""));

            var nodes = new List<Node>();
            var standId = new Dictionary<long, int>();
            var wallId = new Dictionary<long, int>();
            for (int x = g.MinX; x < g.MinX + g.W; x++)
                for (int y = g.MinY; y < g.MinY + g.H; y++)
                {
                    if (IsStand(g, x, y)) { standId[Key(x, y)] = nodes.Count; nodes.Add(new Node { Kind = StandK, X = x, Y = y, FX = x, FY = y }); }
                    for (int s = -1; s <= 1; s += 2)
                        if (IsWall(g, x, y, s)) { wallId[Key(x, y) * 3 + (s + 1)] = nodes.Count; nodes.Add(new Node { Kind = WallK, X = x, Y = y, Side = s, FX = x, FY = y }); }
                }
            foreach (var o in reg.Objects)
            {
                float px = 0f, py = 0f, h = PogoH; bool pogo = true;
                switch (o.Kind)
                {
                    case "cactus": px = o.X - 0.5f; py = o.Y + 1.2f; break;
                    case "plant": pogo = false; break;   // só aceita pogo na volta do bote: não dá para contar com ela
                    case "mushroom": px = o.X - 0.5f; py = o.Y + 1f; h = MushroomH; break;
                    case "spikes": case "gspikes":
                    {
                        float left = o.X - o.W * 0.5f;
                        for (float x = left; x < left + o.W - 0.01f; x += 1f)
                            nodes.Add(new Node { Kind = PogoK, X = (int)Math.Floor(x), Y = (int)Math.Round(o.Y), FX = (float)Math.Floor(x), FY = o.Y + 0.4f, H = PogoH, Label = o.Name });
                        pogo = false;
                        break;
                    }
                    default: pogo = false; break;
                }
                if (pogo) nodes.Add(new Node { Kind = PogoK, X = (int)Math.Floor(px + 0.5f), Y = (int)Math.Floor(py), FX = px, FY = py, H = h, Label = o.Name });
            }
            var goalOf = new Dictionary<ForestLayout.Obj, List<int>>();
            foreach (var o in reg.Objects)
                if (o.Kind == "exit")
                {
                    // o gato encosta no gatilho com o corpo: basta os pés chegarem a 1 tile da borda de baixo;
                    // gatilho largo (a queda da ponte): um ponto a cada 2 tiles
                    float gy = o.Y - o.H * 0.5f - 1f;
                    var list = new List<int>();
                    float w = Math.Max(1f, o.W);
                    for (float gx = o.X - w * 0.5f + 0.5f; gx <= o.X + w * 0.5f - 0.49f; gx += 2f)
                    {
                        list.Add(nodes.Count);
                        nodes.Add(new Node { Kind = GoalK, X = (int)Math.Floor(gx), Y = (int)Math.Floor(gy), FX = gx - 0.5f, FY = gy, Label = o.Name });
                    }
                    if (list.Count == 0) { list.Add(nodes.Count); nodes.Add(new Node { Kind = GoalK, X = (int)Math.Floor(o.X), Y = (int)Math.Floor(gy), FX = o.X - 0.5f, FY = gy, Label = o.Name }); }
                    goalOf[o] = list;
                }

            // espacial: nós por coluna
            var byCol = new Dictionary<int, List<int>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                List<int> l;
                if (!byCol.TryGetValue(nodes[i].X, out l)) { l = new List<int>(); byCol[nodes[i].X] = l; }
                l.Add(i);
            }

            // origens: chegadas da cena
            var starts = new List<int>();
            var startNames = new List<string>();
            foreach (var o in reg.Objects)
                if (o.Kind == "entrance" && string.IsNullOrEmpty(o.Flag))
                {
                    int id = Nearest(nodes, standId, o.X - 0.5f, o.Y, 2);
                    if (id < 0) rep.Problems.Add("chegada " + o.Id + " não está em cima de um chão (x " + o.X + ", y " + o.Y + ")");
                    else { starts.Add(id); startNames.Add(o.Id); }
                }

            // BFS a partir de todas as chegadas (cada chegada também é testada sozinha para as saídas abaixo)
            var edges = new List<int>[nodes.Count];
            var terminal = new bool[nodes.Count];   // encostou num gatilho de saída: a cena troca, o caminho acaba ali
            foreach (var o in reg.Objects)
                if (o.Kind == "exit" && string.IsNullOrEmpty(o.Link))
                {
                    foreach (int i in NodesIn(nodes, o)) terminal[i] = true;
                    List<int> gl;
                    if (goalOf.TryGetValue(o, out gl)) foreach (int k in gl) terminal[k] = true;
                }
            var reached = Bfs(g, nodes, byCol, standId, wallId, starts, edges, terminal);
            // caminho principal: só a partir da primeira chegada da cena (quem vem andando pela rota da fase)
            var main = new List<int>();
            for (int k = 0; k < starts.Count; k++)
                if (reg.Main.Length == 0 ? k == 0 : System.Array.IndexOf(reg.Main, startNames[k]) >= 0) main.Add(starts[k]);
            var mainReached = Bfs(g, nodes, byCol, standId, wallId, main, new List<int>[nodes.Count], terminal);
            string mainName = reg.Main.Length > 0 ? string.Join(" e ", reg.Main) : (startNames.Count > 0 ? startNames[0] : "?");

            // alvos
            var exitNodes = new List<int>();
            foreach (var o in reg.Objects)
            {
                if (o.Kind == "chest" || o.Kind == "key")
                {
                    string what = (o.Kind == "key" ? "chave " : "baú ") + o.Id + " (" + Room(reg, o) + ")";
                    int id = Nearest(nodes, standId, o.X - 0.5f, o.Y, 2);
                    bool later = !string.IsNullOrEmpty(o.Flag);   // precisa de uma classe que ainda não existe
                    if (id < 0) rep.Problems.Add(what + " não está em cima de um chão");
                    else if (!reached[id]) { if (later) rep.Notes.Add(what + ": só com o " + o.Flag); else rep.Problems.Add(what + " fora de alcance"); }
                    else if (!mainReached[id]) rep.Problems.Add(what + " só alcançável por outra chegada, não pela rota a partir de " + mainName);
                    else rep.Notes.Add(what + " ok");
                }
                else if (o.Kind == "exit" || o.Kind == "sc_lever" || o.Kind == "sanctuary" || o.Kind == "residence")
                {
                    var inside = NodesIn(nodes, o);
                    List<int> goal;
                    if (goalOf.TryGetValue(o, out goal)) inside.AddRange(goal);
                    bool any = false, anyMain = false;
                    foreach (int i in inside) if (mainReached[i]) anyMain = true;
                    // atalho (alavanca) e passagem com flag não contam como saída (podem não estar abertos ainda)
                    bool flagged = o.Kind == "exit" && !string.IsNullOrEmpty(o.Link);
                    bool escape = o.Kind != "sc_lever" && !flagged;
                    foreach (int i in inside) { if (escape) exitNodes.Add(i); if (reached[i]) any = true; }
                    string what = o.Kind == "sanctuary" ? "santuário " + o.Id
                        : o.Kind == "sc_lever" ? "atalho " + o.Flag + (o.Opens ? " (onde abre)" : " (lado fechado)")
                        : o.Kind == "residence" ? "porta da residência " + o.Flag
                        : "saída para " + (string.IsNullOrEmpty(o.Text) ? "esta cena" : o.Text) + "/" + o.Id;
                    if (flagged) what += " (com a flag " + o.Link + ")";
                    bool mustReach = !flagged && !(o.Kind == "sc_lever" && !o.Opens);
                    if (inside.Count == 0) rep.Problems.Add(what + " (" + Room(reg, o) + "): nenhum lugar onde o gato encoste");
                    else if (!any) { if (mustReach) rep.Problems.Add(what + " (" + Room(reg, o) + ") fora de alcance"); else rep.Notes.Add(what + ": só com o atalho aberto"); }
                    else if (!anyMain && mustReach) rep.Problems.Add(what + " (" + Room(reg, o) + ") só alcançável por outra chegada, não pela rota a partir de " + mainName);
                    else rep.Notes.Add(what + " ok");
                }
            }

            // softlock: alcançável mas sem caminho de volta para nenhuma saída/atalho/santuário
            var back = new bool[nodes.Count];
            var rev = new List<int>[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
                if (edges[i] != null) foreach (int j in edges[i]) { if (rev[j] == null) rev[j] = new List<int>(); rev[j].Add(i); }
            var q = new Queue<int>();
            foreach (int e in exitNodes) if (reached[e] && !back[e]) { back[e] = true; q.Enqueue(e); }
            while (q.Count > 0)
            {
                int n = q.Dequeue();
                if (rev[n] == null) continue;
                foreach (int p in rev[n]) if (!back[p]) { back[p] = true; q.Enqueue(p); }
            }
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Kind == PogoK) rep.Pogo.Add(new[] { nodes[i].X, nodes[i].Y, mainReached[i] ? 1 : reached[i] ? 2 : 0 });
            var stuck = new List<Node>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n.Kind != StandK) continue;
                byte v = reached[i] ? (back[i] ? (byte)1 : (byte)3) : (byte)2;
                rep.Stand[n.X - g.MinX, n.Y - g.MinY] = v;
                if (mainReached[i]) rep.Main[n.X - g.MinX, n.Y - g.MinY] = true;
                if (v == 3) stuck.Add(n);
            }
            foreach (var cluster in Clusters(stuck))
                rep.Problems.Add("softlock: " + cluster);
            return rep;
        }

        static string Room(ForestLayout.Region reg, ForestLayout.Obj o)
        {
            int i = o.Name.IndexOf(':');
            return i > 0 ? o.Name.Substring(0, i) : reg.Scene;
        }

        static long Key(int x, int y) { return (long)(x + 100000) * 1048576L + (y + 100000); }

        static int Nearest(List<Node> nodes, Dictionary<long, int> standId, float x, float y, int r)
        {
            int cx = (int)Math.Round(x), cy = (int)Math.Round(y);
            for (int d = 0; d <= r; d++)
                for (int dy = -d; dy <= d; dy++)
                    for (int dx = -d; dx <= d; dx++)
                    {
                        int id;
                        if (standId.TryGetValue(Key(cx + dx, cy + dy), out id)) return id;
                    }
            return -1;
        }

        static List<int> NodesIn(List<Node> nodes, ForestLayout.Obj o)
        {
            float w = o.W > 0f ? o.W : 2f, h = o.H > 0f ? o.H : 2f;
            float x0 = o.X - w * 0.5f - 0.6f, x1 = o.X + w * 0.5f + 0.6f;
            float y0 = o.Kind == "exit" ? o.Y - h * 0.5f - 1.4f : o.Y - 0.6f, y1 = o.Kind == "exit" ? o.Y + h * 0.5f : o.Y + 1.5f;
            var list = new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                float cx = n.FX + 0.5f;
                if (cx >= x0 && cx <= x1 && n.FY >= y0 && n.FY <= y1) list.Add(i);
            }
            return list;
        }

        static bool[] Bfs(Grid g, List<Node> nodes, Dictionary<int, List<int>> byCol, Dictionary<long, int> standId, Dictionary<long, int> wallId, List<int> starts, List<int>[] edges, bool[] terminal)
        {
            var reached = new bool[nodes.Count];
            var q = new Queue<int>();
            foreach (int s in starts) if (!reached[s]) { reached[s] = true; q.Enqueue(s); }
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                if (terminal[i] && !starts.Contains(i)) continue;
                var outs = Neighbors(g, nodes, byCol, standId, wallId, i);
                edges[i] = outs;
                foreach (int j in outs) if (!reached[j]) { reached[j] = true; q.Enqueue(j); }
            }
            return reached;
        }

        static List<int> Neighbors(Grid g, List<Node> nodes, Dictionary<int, List<int>> byCol, Dictionary<long, int> standId, Dictionary<long, int> wallId, int i)
        {
            var n = nodes[i];
            var outs = new List<int>();
            int id;
            if (n.Kind == StandK)
            {
                // andar
                for (int s = -1; s <= 1; s += 2)
                {
                    if (standId.TryGetValue(Key(n.X + s, n.Y), out id)) outs.Add(id);
                    if (wallId.TryGetValue(Key(n.X, n.Y) * 3 + (s + 1), out id)) outs.Add(id);
                }
                // pular (variando a altura) e sair andando da borda
                Jumps(g, nodes, byCol, n.FX, n.FY, JumpH, 0, true, outs, i);
                Jumps(g, nodes, byCol, n.FX, n.FY, JumpH * 0.6f, 0, false, outs, i);
                for (int s = -1; s <= 1; s += 2)
                    if (!g.S(n.X + s, n.Y) && !standId.ContainsKey(Key(n.X + s, n.Y)))
                        Jumps(g, nodes, byCol, n.FX + s * 0.9f, n.FY, 0f, s, false, outs, i);
                // descer pelo galho de mão única (baixo + pulo)
                if (g.Ow(n.X, n.Y) && !g.S(n.X, n.Y - 1)) Jumps(g, nodes, byCol, n.FX, n.FY - 0.6f, 0f, 0, false, outs, i);
            }
            else if (n.Kind == WallK)
            {
                // subir/descer pela vinha, voltar ao chão, subir na borda de cima
                for (int dy = -1; dy <= 1; dy += 2)
                    if (wallId.TryGetValue(Key(n.X, n.Y + dy) * 3 + (n.Side + 1), out id)) outs.Add(id);
                for (int dx = 0; dx <= 1; dx++)
                    for (int dy = 0; dy <= 2; dy++)
                        if (standId.TryGetValue(Key(n.X + dx * n.Side, n.Y + dy), out id)) outs.Add(id);
                // pulo de parede (para longe) e soltar
                Jumps(g, nodes, byCol, n.FX, n.FY, WallJumpH, -n.Side, true, outs, i);
                Jumps(g, nodes, byCol, n.FX - n.Side * 0.3f, n.FY, 0f, 0, true, outs, i);
            }
            else if (n.Kind == PogoK)
            {
                Jumps(g, nodes, byCol, n.FX, n.FY, n.H, 0, true, outs, i);
            }
            return outs;
        }

        /// <summary>Arcos saindo de (x0, y0) com ápice h para todo nó alcançável.</summary>
        static void Jumps(Grid g, List<Node> nodes, Dictionary<int, List<int>> byCol, float x0, float y0, float h, int dirLock, bool grabWalls, List<int> outs, int self)
        {
            float maxSide = 15f;
            int cx = (int)Math.Floor(x0 + 0.5f);
            for (int x = cx - 15; x <= cx + 15; x++)
            {
                List<int> col;
                if (!byCol.TryGetValue(x, out col)) continue;
                foreach (int j in col)
                {
                    if (j == self) continue;
                    var t = nodes[j];
                    if (t.FY > y0 + h + LedgeHands + 0.01f || t.FY < y0 - 60f) continue;
                    bool ok;
                    if (t.Kind == WallK)
                    {
                        if (!grabWalls) continue;
                        ok = Arc(g, x0, y0, h, t.FX, t.FY, true, dirLock, maxSide);
                    }
                    else if (t.Kind == GoalK)
                    {
                        ok = Arc(g, x0, y0, h, t.FX, t.FY, true, dirLock, maxSide);
                    }
                    else if (t.Kind == PogoK)
                    {
                        // estocada: os pés passam por cima do ponto, descendo
                        ok = Arc(g, x0, y0, h, t.FX, t.FY, false, dirLock, maxSide);
                    }
                    else
                    {
                        ok = t.FY <= y0 + h + 0.001f && Arc(g, x0, y0, h, t.FX, t.FY, false, dirLock, maxSide);
                        // agarrar a borda: chão sólido (não galho) até 1,25 acima do ápice, com o lado de chegada livre
                        if (!ok && t.FY > y0 + h * 0.5f && g.S(t.X, t.Y - 1) && !g.Ow(t.X, t.Y))
                        {
                            int side = Math.Sign(x0 - t.FX);
                            if (side != 0 && !g.S(t.X + side, t.Y - 1) && !g.S(t.X + side, t.Y - 2))
                                ok = Arc(g, x0, y0, h, t.FX + side, Math.Min(t.FY - LedgeHands, y0 + h), true, dirLock, maxSide);
                        }
                    }
                    if (ok) outs.Add(j);
                }
            }
        }

        /// <summary>Agrupa nós vizinhos em "região (x0..x1, y0..y1)" para o relatório.</summary>
        static List<string> Clusters(List<Node> stuck)
        {
            var res = new List<string>();
            var used = new bool[stuck.Count];
            for (int i = 0; i < stuck.Count; i++)
            {
                if (used[i]) continue;
                int x0 = stuck[i].X, x1 = x0, y0 = stuck[i].Y, y1 = y0, count = 0;
                var q = new Queue<int>(); q.Enqueue(i); used[i] = true;
                while (q.Count > 0)
                {
                    var n = stuck[q.Dequeue()]; count++;
                    x0 = Math.Min(x0, n.X); x1 = Math.Max(x1, n.X); y0 = Math.Min(y0, n.Y); y1 = Math.Max(y1, n.Y);
                    for (int j = 0; j < stuck.Count; j++)
                        if (!used[j] && Math.Abs(stuck[j].X - n.X) <= 2 && Math.Abs(stuck[j].Y - n.Y) <= 2) { used[j] = true; q.Enqueue(j); }
                }
                res.Add(count + " ponto(s) em x " + x0 + ".." + x1 + ", y " + y0 + ".." + y1);
            }
            return res;
        }

        /// <summary>
        /// Uma sala em texto, 1 caractere por tile, de cima para baixo (para depurar o alcance): # chão, - galho,
        /// ^ perigo, | vinha, o em pé pela rota principal, + em pé por outra chegada, · em pé fora de alcance, ! softlock.
        /// </summary>
        public static string Ascii(string scene, string roomId)
        {
            var reg = ForestLayout.Get(scene);
            var room = reg != null ? reg.FindRoom(roomId) : null;
            if (room == null) return "sala " + roomId + " não existe em " + scene;
            var rep = Check(reg);
            var g = rep.Grid;
            var sb = new System.Text.StringBuilder();
            for (int y = room.Y + room.H - 1; y >= room.Y; y--)
            {
                sb.Append((y - room.Y - 1).ToString().PadLeft(4)).Append(' ');
                for (int x = room.X; x < room.X + room.W; x++)
                {
                    char c = ' ';
                    if (g.S(x, y)) c = '#';
                    else if (g.Hz(x, y)) c = '^';
                    else if (g.Ow(x, y)) c = '-';
                    if (g.Vn(x, y)) c = '|';
                    if (g.In(x, y))
                    {
                        byte st = rep.Stand[x - g.MinX, y - g.MinY];
                        if (st == 1) c = rep.Main[x - g.MinX, y - g.MinY] ? 'o' : '+';
                        else if (st == 2) c = '.';
                        else if (st == 3) c = '!';
                    }
                    foreach (var p in rep.Pogo) if (p[0] == x && p[1] == y) c = p[2] == 1 ? 'P' : p[2] == 2 ? 'p' : '*';
                    sb.Append(c);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>Relatório em texto (console do Unity ou PowerShell).</summary>
        public static string Format(List<Report> reports, bool verbose)
        {
            var sb = new System.Text.StringBuilder();
            int problems = 0;
            foreach (var r in reports)
            {
                sb.AppendLine("== " + r.Scene + (r.Problems.Count == 0 ? ": ok" : ": " + r.Problems.Count + " problema(s)"));
                foreach (var p in r.Problems) sb.AppendLine("  ✗ " + p);
                if (verbose) foreach (var n in r.Notes) sb.AppendLine("  · " + n);
                problems += r.Problems.Count;
            }
            sb.AppendLine(problems == 0 ? "Tudo alcançável, sem softlock." : problems + " problema(s) no total.");
            return sb.ToString();
        }
    }
}
