using System;
using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Floresta Silenciosa: o graybox das salas como DADOS, uma região por cena (Docs/Level/floresta-mapa.md).
    /// O <c>ForestGrayboxSetup</c> monta as cenas daqui; <c>ForestReach</c> confere o alcance (baús, saídas, softlock) e
    /// <c>Tools/LevelPreview/checar_floresta.ps1</c> desenha o mapa num PNG, os dois sem abrir o Unity.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell).
    /// Coordenadas GLOBAIS em tiles, com a posição de cada sala escrita à mão: a floresta é um ANEL (rev. 4, pedido do
    /// usuário de 13/11/2026). O gato cai no fundo (A), sobe pelo leste (B), volta para o oeste por cima (C), sobe a
    /// árvore gigante (D) e segue pelo alto (E, F). Como as salas de depois passam por cima das de antes, os atalhos são
    /// passagens de verdade (cipó que desce, alçapão), não portas que teletransportam.
    /// Dentro de cada sala os helpers usam coordenadas LOCAIS: (0, 0) = primeiro tile livre acima do chão, no canto
    /// esquerdo. Sala = retângulo externo com 1 tile de parede/chão/teto; salas vizinhas dividem a parede (a próxima
    /// começa na última coluna/linha da anterior). "Chão com topo em Y" = tiles sólidos até Y-1; objetos com os pés em Y.
    /// Métricas (Docs/Level/metricas.md): pulo 4 (degraus de até 3), vão até 4, agarrar borda 5, chaminé de pulo de
    /// parede com 3 de largura (vinhas dos dois lados), pogo 7,2, cogumelo 12.
    /// </summary>
    public static class ForestLayout
    {
        /// <summary>Suba quando mudar o graybox (o menu 21 recria as cenas).</summary>
        public const int Version = 9;

        public const string Road = "Forest_Road";
        public const string A = "Forest_A_Roots";
        public const string B = "Forest_B_Thorns";
        public const string C = "Forest_C_Shadows";
        public const string D = "Forest_D_Refuge";
        public const string E = "Forest_E_Nest";
        public const string F = "Forest_F_Heart";
        /// <summary>Interior da casa da Vulpis (a cabana da Forest_Test_C, agora dentro da fase).</summary>
        public const string Cabin = "Forest_D_Cabin";

        public const string ForestMap = "Floresta Silenciosa";
        public const string RoadMap = "Estrada de Soluki";
        public const string BridgeFlag = "ponte_reconstruida";

        // atalhos (flags no save): cada um abre do lado mais adiantado da fase
        public const string AtRavina = "atalho_ravina";   // C6 (Ravina) → A2 (Raízes Gêmeas): alçapão + cipó
        public const string AtLago = "atalho_lago";       // C4 (Caverna do Lago) → A6 (Galhos Baixos): alçapão + poço com cipó
        public const string AtTorre = "atalho_torre";     // B4 (Torre): cipó do alto até o pé da torre
        public const string AtPatamar = "atalho_patamar"; // B1 (Pé do Bosque): cipó do patamar até o chão
        public const string AtRaiz = "atalho_raiz";       // E2 (Raiz que Respira): cipó do alto até a E1
        public const string AtMusgo = "atalho_musgo";     // E4 (S-F4) → C1 (Entrada do Vale): alçapão + poço

        public enum Map { Ground, Climbable, Background }

        public class Tile { public Map Map; public bool Clear; public int X0, Y0, X1, Y1; }

        public class Obj
        {
            public string Kind, Name, Id, Text, Link;
            public float X, Y, W, H, A, B, C;
            public int Facing = 1, Value;
            /// <summary>Atalho: flag do atalho. Baú: classe que falta para alcançá-lo (vazio = alcançável hoje).</summary>
            public string Flag; public bool Opens;
        }

        public class Room
        {
            public string Id, Name; public int X, Y, W, H; public bool Optional;
            public int InX0 { get { return X + 1; } }
            public int InY0 { get { return Y + 1; } }
            public int InX1 { get { return X + W - 2; } }
            public int InY1 { get { return Y + H - 2; } }
        }

        public class Region
        {
            public string Scene, Label, MapRegion; public uint Sky;
            public float SpawnX, SpawnY;
            /// <summary>Chegadas da rota principal (vazio = a primeira chegada). A estrada tem duas: a ponte caiu.</summary>
            public string[] Main = new string[0];
            public readonly List<Room> Rooms = new List<Room>();
            public readonly List<Tile> Shell = new List<Tile>(), Inner = new List<Tile>(), Late = new List<Tile>();
            public readonly List<Obj> Objects = new List<Obj>();

            /// <summary>Na ordem de aplicação: cascas das salas, miolo, portas.</summary>
            public List<Tile> Tiles()
            {
                var all = new List<Tile>(Shell);
                all.AddRange(Inner);
                all.AddRange(Late);
                return all;
            }

            public Room FindRoom(string id) { foreach (var r in Rooms) if (r.Id == id) return r; return null; }

            public void Bounds(out int x0, out int y0, out int x1, out int y1)
            {
                x0 = int.MaxValue; y0 = int.MaxValue; x1 = int.MinValue; y1 = int.MinValue;
                foreach (var r in Rooms)
                {
                    if (r.X < x0) x0 = r.X; if (r.Y < y0) y0 = r.Y;
                    if (r.X + r.W - 1 > x1) x1 = r.X + r.W - 1; if (r.Y + r.H - 1 > y1) y1 = r.Y + r.H - 1;
                }
            }
        }

        static List<Region> s_cache;

        /// <summary>As 7 cenas, da estrada ao Coração.</summary>
        public static List<Region> Regions()
        {
            if (s_cache != null) return s_cache;
            var list = new List<Region>();
            s_cache = list; // cada região usa as anteriores (Get) enquanto a lista enche
            list.Add(BuildRoad());
            list.Add(BuildA());
            list.Add(BuildB());
            list.Add(BuildC());
            list.Add(BuildD());
            list.Add(BuildE());
            list.Add(BuildF());
            foreach (var reg in list) VinesToTheTop(reg);
            return list;
        }

        /// <summary>Quantos tiles sólidos acima do fim da vinha ainda são completados (mais que isso é parede de verdade).</summary>
        const int VineTopGap = 3;

        /// <summary>
        /// Pedido do usuário (13/11/2026): vinha ou cipó tem de ir até o ÚLTIMO tile do bloco. Se a vinha acaba antes do
        /// topo, a escalada para embaixo do resto do bloco (não acha a borda) e o gato não sobe nem pula direito.
        /// Para cada vinha (tile escalável ou cipó de atalho) que termina com até <see cref="VineTopGap"/> tiles sólidos
        /// acima e ar logo depois, com o lado de onde se escala livre nessa altura, a vinha sobe até o último sólido.
        /// Paredes que continuam (lado de escalar tapado, ou bloco alto) ficam como estão: as vinhas "baixas" de propósito
        /// (B2) não mudam.
        /// </summary>
        static void VinesToTheTop(Region reg)
        {
            int x0, y0, x1, y1;
            reg.Bounds(out x0, out y0, out x1, out y1);
            x0 -= 1; y0 -= 1; x1 += 1; y1 += VineTopGap + 2;
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            var solid = new bool[w, h];
            var vine = new bool[w, h];
            foreach (var t in reg.Tiles())
                for (int x = Math.Max(t.X0, x0); x <= Math.Min(t.X1, x1); x++)
                    for (int y = Math.Max(t.Y0, y0); y <= Math.Min(t.Y1, y1); y++)
                    {
                        if (t.Map == Map.Ground) solid[x - x0, y - y0] = !t.Clear;
                        else if (t.Map == Map.Climbable) vine[x - x0, y - y0] = !t.Clear;
                    }
            var inRoom = new bool[w, h];
            foreach (var r in reg.Rooms)
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int y = r.Y; y < r.Y + r.H; y++)
                        if (x >= x0 && x <= x1 && y >= y0 && y <= y1) inRoom[x - x0, y - y0] = true;
            // fora das salas conta como ar acima do bloco (saída para outra cena), mas nunca como lugar para o gato ficar
            Func<int, int, bool> S = (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1 && solid[x - x0, y - y0];
            Func<int, int, bool> V = (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1 && vine[x - x0, y - y0];
            Func<int, int, bool> Free = (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1 && inRoom[x - x0, y - y0] && !solid[x - x0, y - y0];

            // quantos tiles a coluna x sobe a partir do topo da vinha em top (0 = nada a fazer)
            Func<int, int, bool, int> Extra = (x, top, stopAtVine) =>
            {
                int n = 0;
                while (n <= VineTopGap && S(x, top + n + 1) && !(stopAtVine && V(x, top + n + 1))) n++;
                if (n == 0 || n > VineTopGap) return 0;
                // o gato precisa caber do lado, em alguma face, ao longo do trecho que falta (e na altura do fim da vinha)
                for (int side = -1; side <= 1; side += 2)
                {
                    bool free = Free(x + side, top);
                    for (int k = 1; k <= n && free; k++) free = Free(x + side, top + k);
                    if (free) return n;
                }
                return 0;
            };

            var add = new List<Tile>();
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y < y1; y++)
                {
                    if (!V(x, y) || V(x, y + 1)) continue; // só o topo de cada trecho
                    int n = Extra(x, y, true);
                    if (n > 0) add.Add(new Tile { Map = Map.Climbable, X0 = x, Y0 = y + 1, X1 = x, Y1 = y + n });
                }
            reg.Late.AddRange(add);

            // cipós dos atalhos (objetos): o mesmo, com o colisor esticado até o último tile
            foreach (var o in reg.Objects)
            {
                if (o.Kind != "sc_vine") continue;
                int x = (int)Math.Round(o.X), top = (int)Math.Round(o.Y + o.H) - 1;
                int n = Extra(x, top, false);
                if (n > 0) o.H += n;
            }
        }

        public static Region Get(string scene) { foreach (var r in Regions()) if (r.Scene == scene) return r; return null; }

        // =========================================================== construtor (estado da sala atual)

        static Region R;
        static Room S;

        static Region Begin(string scene, string label, string mapRegion, uint sky)
        {
            R = new Region { Scene = scene, Label = label, MapRegion = mapRegion, Sky = sky };
            return R;
        }

        /// <summary>Abre uma sala: casca sólida no retângulo inteiro e o miolo vazio.</summary>
        static Room Sala(string id, string name, int x, int y, int w, int h, bool optional = false)
        {
            S = new Room { Id = id, Name = name, X = x, Y = y, W = w, H = h, Optional = optional };
            R.Rooms.Add(S);
            R.Shell.Add(new Tile { Map = Map.Ground, X0 = x, Y0 = y, X1 = x + w - 1, Y1 = y + h - 1 });
            R.Shell.Add(new Tile { Map = Map.Ground, Clear = true, X0 = x + 1, Y0 = y + 1, X1 = x + w - 2, Y1 = y + h - 2 });
            return S;
        }

        static int GX(int lx) { return S.X + 1 + lx; }
        static int GY(int ly) { return S.Y + 1 + ly; }
        static float FX(float lx) { return S.X + 1 + lx; }
        static float FY(float ly) { return S.Y + 1 + ly; }
        /// <summary>Topo do miolo da sala (última linha livre, local).</summary>
        static int Top { get { return S.H - 3; } }
        /// <summary>Última coluna livre (local).</summary>
        static int Right { get { return S.W - 3; } }

        /// <summary>Chão sólido (coordenadas locais, cantos inclusivos).</summary>
        static void G(int x0, int y0, int x1, int y1) { R.Inner.Add(new Tile { Map = Map.Ground, X0 = GX(x0), Y0 = GY(y0), X1 = GX(x1), Y1 = GY(y1) }); }
        static void Cl(int x0, int y0, int x1, int y1) { R.Inner.Add(new Tile { Map = Map.Ground, Clear = true, X0 = GX(x0), Y0 = GY(y0), X1 = GX(x1), Y1 = GY(y1) }); }
        /// <summary>Vinha (escalável) num tile sólido: o gato escala encostado nela.</summary>
        static void V(int x0, int y0, int x1, int y1) { R.Inner.Add(new Tile { Map = Map.Climbable, X0 = GX(x0), Y0 = GY(y0), X1 = GX(x1), Y1 = GY(y1) }); }
        /// <summary>Tronco com vinha: sólido e escalável.</summary>
        static void GV(int x0, int y0, int x1, int y1) { G(x0, y0, x1, y1); V(x0, y0, x1, y1); }
        static void Back(int x0, int y0, int x1, int y1) { R.Inner.Add(new Tile { Map = Map.Background, X0 = GX(x0), Y0 = GY(y0), X1 = GX(x1), Y1 = GY(y1) }); }
        /// <summary>Vinha na parede da direita / da esquerda da sala.</summary>
        static void VE(int y0, int y1) { R.Inner.Add(new Tile { Map = Map.Climbable, X0 = S.X + S.W - 1, Y0 = GY(y0), X1 = S.X + S.W - 1, Y1 = GY(y1) }); }
        static void VW(int y0, int y1) { R.Inner.Add(new Tile { Map = Map.Climbable, X0 = S.X, Y0 = GY(y0), X1 = S.X, Y1 = GY(y1) }); }

        /// <summary>
        /// Terreno orgânico: a partir da coluna <paramref name="lx"/>, cada número é o topo do chão naquela coluna
        /// (0 = sem nada). Colunas iguais viram um bloco só.
        /// </summary>
        static void Terrain(int lx, params int[] tops)
        {
            int i = 0;
            while (i < tops.Length)
            {
                int j = i;
                while (j + 1 < tops.Length && tops[j + 1] == tops[i]) j++;
                if (tops[i] > 0) G(lx + i, 0, lx + j, tops[i] - 1);
                i = j + 1;
            }
        }

        /// <summary>Raízes penduradas do teto: cada número é a linha de baixo da raiz naquela coluna (0 = nada).</summary>
        static void Roof(int lx, params int[] bottoms)
        {
            for (int i = 0; i < bottoms.Length; i++)
                if (bottoms[i] > 0) G(lx + i, bottoms[i], lx + i, Top);
        }

        /// <summary>Massa de raiz/terra flutuante (topo em y1 + 1), com os cantos de baixo arredondados.</summary>
        static void Blob(int x0, int y0, int x1, int y1)
        {
            if (x1 - x0 >= 2 && y1 > y0)
            {
                G(x0, y0 + 1, x1, y1);
                G(x0 + 1, y0, x1 - 1, y0);
            }
            else G(x0, y0, x1, y1);
        }

        static void DoorE(int y0, int y1) { R.Late.Add(new Tile { Map = Map.Ground, Clear = true, X0 = S.X + S.W - 1, Y0 = GY(y0), X1 = S.X + S.W - 1, Y1 = GY(y1) }); }
        static void DoorW(int y0, int y1) { R.Late.Add(new Tile { Map = Map.Ground, Clear = true, X0 = S.X, Y0 = GY(y0), X1 = S.X, Y1 = GY(y1) }); }
        /// <summary>
        /// Buraco no teto para a sala de cima (mesma cena). Leva um galho de mão única no nível do chão de cima:
        /// o gato atravessa pulando por baixo e fica em pé nele; baixo + pulo desce.
        /// </summary>
        static void DoorN(int x0, int x1) { DoorN(x0, x1, true); }
        static void DoorN(int x0, int x1, bool branch)
        {
            R.Late.Add(new Tile { Map = Map.Ground, Clear = true, X0 = GX(x0), Y0 = S.Y + S.H - 1, X1 = GX(x1), Y1 = S.Y + S.H - 1 });
            if (branch) R.Objects.Add(new Obj { Kind = "branch", Name = S.Id + ": galho do buraco do teto", X = GX(x0), Y = S.Y + S.H, W = x1 - x0 + 1 });
        }
        static void DoorS(int x0, int x1) { R.Late.Add(new Tile { Map = Map.Ground, Clear = true, X0 = GX(x0), Y0 = S.Y, X1 = GX(x1), Y1 = S.Y }); }

        static Obj O(string kind, string name, float lx, float ly)
        {
            var o = new Obj { Kind = kind, Name = S.Id + ": " + name, X = FX(lx), Y = FY(ly) };
            R.Objects.Add(o);
            return o;
        }

        // ------------------------------------------------------------ plataformas
        /// <summary>Galho de mão única: <paramref name="lx"/> = ponta esquerda, topo em <paramref name="ly"/>.</summary>
        static void Branch(float lx, float ly, float w) { O("branch", "galho", lx, ly).W = w; }
        static void Sway(float lx, float ly, float w) { O("sway", "galho que balança", lx, ly).W = w; }
        /// <summary>Folha larga (3 de largura) centrada em <paramref name="cx"/>.</summary>
        static void Leaf(float cx, float ly) { O("leaf", "folha larga", cx, ly).W = 3f; }
        /// <summary>Galho podre (2 de largura) com a ponta esquerda em <paramref name="lx"/>.</summary>
        static void Rotten(float lx, float ly) { O("rotten", "galho podre", lx, ly).W = 2f; }
        static void Mushroom(float cx, float ly) { O("mushroom", "cogumelo", cx, ly); }
        /// <summary>Cacto Flutuante (bola de espinhos) com o centro em (cx, cy); <paramref name="tx"/>/<paramref name="ty"/> = vaivém.</summary>
        static void Cactus(float cx, float cy, float tx = 0f, float ty = 0f, float period = 3f)
        {
            var o = O("cactus", "cacto", cx, cy);
            o.A = tx; o.B = ty; o.C = period;
        }
        static void Spikes(float lx, float ly, int w) { var o = O("spikes", "espinhos", lx + w * 0.5f, ly); o.W = w; }
        /// <summary>Espinhos de um grupo de armadilhas (o botão do grupo desliga).</summary>
        static void SpikesG(float lx, float ly, int w, string group) { var o = O("gspikes", "espinhos", lx + w * 0.5f, ly); o.W = w; o.Text = group; }

        // ------------------------------------------------------------ objetos
        /// <summary>Tronco rachado (1 × 4) na coluna <paramref name="lx"/>.</summary>
        static void Trunk(int lx, int ly, string id) { O("trunk", "tronco rachado", lx + 0.5f, ly).Id = id; }
        /// <summary>Baú de madeira. <paramref name="later"/> = classe que falta para alcançá-lo (o alcance só anota).</summary>
        static void Chest(float cx, float ly, string id, int coins = 0, string later = null)
        {
            var o = O("chest", "baú " + id, cx, ly);
            o.Id = id; o.Value = coins; o.Flag = later;
        }
        static void Coin(float cx, float ly, int n = 1)
        {
            var o = O("coin", "moeda", cx, ly + 0.6f);
            o.Id = "gb_" + S.Id.ToLowerInvariant() + "_moeda_" + R.Objects.Count; o.Value = n;
        }
        static void Coins(float lx, float ly, int count, float step = 1.5f)
        {
            for (int i = 0; i < count; i++) Coin(lx + i * step, ly);
        }
        static void Sign(float cx, float ly, string text) { O("sign", "placa", cx, ly + 0.2f).Text = text; }
        static void Sanctuary(float cx, float ly, string id, string name)
        {
            var o = O("sanctuary", "santuário " + id, cx, ly);
            o.Id = id; o.Text = name;
        }
        static void Key(float cx, float cy, string id) { O("key", "chave " + id, cx, cy).Id = id; }
        /// <summary>Porta de residência (o interior é outra cena); a chegada de volta (<paramref name="doorId"/>) fica ao lado.</summary>
        static void Residence(float cx, float ly, string scene, string verb, string doorId, string keyId)
        {
            var o = O("residence", "porta " + doorId, cx, ly);
            o.Text = scene; o.Link = verb; o.Flag = doorId; o.Id = keyId;   // o interior tem a chegada "entrada"
        }

        // ------------------------------------------------------------ inimigos
        /// <summary>Templário zumbi (a praga infectou a Ordem) patrulhando de a a b (x locais); <paramref name="shield"/> = de escudo.</summary>
        static void Infected(float cx, float ly, int facing, float a, float b, bool shield)
        {
            var o = O("infected", shield ? "Templário zumbi (escudo)" : "Templário zumbi (espada)", cx, ly);
            o.Facing = facing; o.A = FX(a); o.B = FX(b); o.Value = shield ? 1 : 0;
        }
        /// <summary>Serpente escondida num buraco (bote de 3 tiles), virada para <paramref name="facing"/>.</summary>
        static void Snake(float cx, float ly, int facing) { O("snake", "serpente (buraco)", cx, ly).Facing = facing; }
        /// <summary>Serpente que rasteja de <paramref name="a"/> a <paramref name="b"/> (x locais) entre um bote e outro.</summary>
        static void Crawler(float cx, float ly, float a, float b)
        {
            var o = O("snake", "serpente (rasteja)", cx, ly);
            o.Value = 1; o.A = FX(a); o.B = FX(b);
        }
        /// <summary>Morcego pendurado com o poleiro em (cx, y); <paramref name="wake"/> &gt; 0 = alcance de acordar.</summary>
        static Obj Bat(float cx, float y, float wake = 0f) { var o = O("bat", "morcego", cx, y); o.A = wake; return o; }
        static void Dry(float cx, float ly, int facing) { O("dry", "Galho Seco", cx, ly).Facing = facing; }
        static Obj Sprout(float cx, float ly, int facing) { var o = O("sprout", "Broto", cx, ly); o.Facing = facing; return o; }
        /// <summary>Planta Carnívora: 0 = no chão; 90 = na parede da direita (virada para a esquerda); -90 = na da esquerda; 180 = no teto.</summary>
        static void Plant(float cx, float ly, float angle = 0f) { O("plant", "planta carnívora", cx, ly).A = angle; }

        // ------------------------------------------------------------ furtividade e armadilhas
        /// <summary>Sombra (esconderijo) com o canto de baixo à esquerda em (lx, ly).</summary>
        static void Shadow(float lx, float ly, float w, float h)
        {
            var o = O("shadow", "sombra", lx + w * 0.5f, ly + h * 0.5f);
            o.W = w; o.H = h;
        }
        static void Bush(float cx, float ly) { O("bush", "folhagem alta", cx, ly).Text = "matinho"; }
        /// <summary>Serra correndo de <paramref name="a"/> a <paramref name="b"/> (x locais) sobre um chão com topo em <paramref name="ly"/>.</summary>
        static void Saw(float a, float b, float ly, string group = null)
        {
            var o = O(group == null ? "saw" : "gsaw", "serra", (a + b) * 0.5f, ly + 0.55f);
            o.A = FX(a); o.B = FX(b); o.Text = group;
        }
        /// <summary>Serra que sobe e desce na coluna <paramref name="cx"/>, de <paramref name="a"/> a <paramref name="b"/> (y locais).</summary>
        static void VSaw(float cx, float a, float b, string group)
        {
            var o = O("vsaw", "serra (sobe e desce)", cx, a);
            o.A = FY(a); o.B = FY(b); o.Text = group;
        }
        /// <summary>Lâmina pendular com o pivô em (cx, pivotY) e haste de <paramref name="length"/> tiles.</summary>
        static void Blade(float cx, float pivotY, float length, float phase = 0f, string group = null)
        {
            var o = O("blade", "lâmina pendular", cx, pivotY);
            o.H = length; o.A = phase; o.Text = group;
        }
        /// <summary>Botão azul: desliga para sempre as armadilhas do grupo.</summary>
        static void Switch(float cx, float ly, string group) { O("switch", "botão das armadilhas", cx, ly).Text = group; }
        static void Pool(float cx, float ly, int w) { O("pool", "água", cx, ly).W = w; }

        // ------------------------------------------------------------ arena (da Forest_Test_C)
        static void Arena(string id, float cx, float cy, float w, float h)
        {
            var o = O("arena", "arena " + id, cx, cy);
            o.Id = id; o.W = w; o.H = h;
        }
        static void ArenaGate(string id, float cx, float ly) { O("arena_gate", "portão da arena", cx, ly).Link = id; }
        /// <summary>Marca o inimigo como da segunda leva da arena <paramref name="id"/> (fica desligado até a hora).</summary>
        static void Wave2(Obj o, string id) { o.Link = "onda:" + id; }

        // ------------------------------------------------------------ atalhos de verdade (pedido do usuário, 13/11/2026)
        /// <summary>
        /// Onde se abre o atalho (o lado mais adiantado): "E: <paramref name="verb"/>" com o gato do lado
        /// (<paramref name="sideX"/>, <paramref name="sideY"/>) do ponto.
        /// </summary>
        static void Lever(string flag, float cx, float ly, int sideX, int sideY, string verb)
        {
            var o = O("sc_lever", "atalho " + flag + " (abre)", cx, ly);
            o.Flag = flag; o.Opens = true; o.A = sideX; o.B = sideY; o.Text = verb;
        }
        /// <summary>O lado fechado: com E, o gato comenta (<paramref name="line"/>).</summary>
        static void Closed(string flag, float cx, float ly, string line)
        {
            var o = O("sc_lever", "atalho " + flag + " (fechado)", cx, ly);
            o.Flag = flag; o.Opens = false; o.Text = line;
        }
        /// <summary>Cipó que aparece aberto: na coluna <paramref name="lx"/> (-1 = parede da esquerda; Right+1 = da direita), de y0 a y1.</summary>
        static void ScVine(string flag, int lx, int y0, int y1)
        {
            var o = new Obj { Kind = "sc_vine", Name = S.Id + ": cipó do atalho " + flag, X = GX(lx), Y = GY(y0), W = 1f, H = y1 - y0 + 1, Flag = flag };
            R.Objects.Add(o);
        }
        /// <summary>Tampa/portão (sólido) enquanto fechado: tiles locais de (x0, y0) a (x1, y1).</summary>
        static void ScBlock(string flag, int x0, int y0, int x1, int y1, string look)
        {
            var o = new Obj { Kind = "sc_block", Name = S.Id + ": " + look + " (" + flag + ")", X = GX(x0), Y = GY(y0), W = x1 - x0 + 1, H = y1 - y0 + 1, Flag = flag, Text = look };
            R.Objects.Add(o);
        }
        /// <summary>Cipó enrolado (desenho do lado fechado), centrado em (cx, cy).</summary>
        static void ScRolled(string flag, float cx, float cy) { O("sc_rolled", "cipó enrolado (" + flag + ")", cx, cy).Flag = flag; }

        // ------------------------------------------------------------ passagens
        /// <summary>Chegada. Com <paramref name="flag"/>: só se chega aqui com aquele atalho aberto (o alcance não parte dela).</summary>
        static void Entrance(string id, float cx, float ly, int facing, string flag = null)
        {
            var o = O("entrance", "chegada " + id, cx, ly);
            o.Id = id; o.Facing = facing; o.Flag = flag;
        }

        /// <summary>Saída para outra cena pela parede da direita (porta de 4 de altura a partir de <paramref name="ly"/>).</summary>
        static void ExitE(int ly, string scene, string entrance)
        {
            DoorE(ly, ly + 3);
            var o = O("exit", "saída → " + scene, S.W - 2 + 1.5f, ly + 2f);
            o.Facing = 1;   // direção da passagem (luz): 1 leste, -1 oeste, 2 cima, -2 baixo, 0 no meio da sala
            o.W = 2f; o.H = 4f; o.Text = scene; o.Id = entrance;
        }
        static void ExitW(int ly, string scene, string entrance)
        {
            DoorW(ly, ly + 3);
            var o = O("exit", "saída ← " + scene, -1.5f, ly + 2f);
            o.Facing = -1;
            o.W = 2f; o.H = 4f; o.Text = scene; o.Id = entrance;
        }
        /// <summary>Saída para cima (buraco no teto de x0 a x1). Com <paramref name="flag"/>, só depois de aberto o atalho.</summary>
        static void ExitN(int x0, int x1, string scene, string entrance, string flag = null)
        {
            DoorN(x0, x1, false);
            var o = O("exit", "saída ↑ " + scene, (x0 + x1 + 1) * 0.5f, S.H - 3 + 0.5f);
            o.Facing = 2;
            o.W = x1 - x0 + 1; o.H = 2.5f; o.Text = scene; o.Id = entrance; o.Link = flag;
        }
        /// <summary>Saída para baixo (buraco no chão de x0 a x1): o gato cai.</summary>
        static void ExitS(int x0, int x1, string scene, string entrance, string flag = null)
        {
            DoorS(x0, x1);
            var o = O("exit", "saída ↓ " + scene, (x0 + x1 + 1) * 0.5f, -2f);
            o.Facing = -2;
            o.W = x1 - x0 + 1; o.H = 2f; o.Text = scene; o.Id = entrance; o.Link = flag;
        }
        /// <summary>Gatilho de troca de cena no meio da sala (queda da ponte, corda com flag).</summary>
        static void Trigger(float cx, float cy, float w, float h, string scene, string entrance, string flag)
        {
            var o = O("exit", "passagem → " + scene, cx, cy);
            o.Facing = 0;
            o.W = w; o.H = h; o.Text = scene; o.Id = entrance; o.Link = flag;
        }

        /// <summary>Rampa (triângulo) com a base em ly, de lx a lx+w; o lado alto (ly+h) à direita se <paramref name="risesRight"/>.
        /// Andáveis: 1:1 e 1:2. Mais íngreme que 1:1 = o gato escorrega. Encoste o lado alto num bloco com topo em ly+h.</summary>
        static void Slope(int lx, int ly, int w, int h, bool risesRight)
        {
            var o = O("slope", "rampa", lx, ly);
            o.W = w; o.H = h; o.Facing = risesRight ? 1 : -1;
        }
        /// <summary>Gancho (classe Caçador, no futuro): o caminho de volta pelo alto.</summary>
        static void Hook(float cx, float cy) { O("hook", "gancho (Caçador)", cx, cy); }
        static void Hooks(float y, params float[] xs) { foreach (float x in xs) Hook(x, y); }

        // =========================================================== Estrada de Soluki (F3)

        public const string FortressScene = "Fortress_MVP";
        public const string FallenFlag = "ponte_caiu";   // = BridgeCollapse.FallenFlag (runtime)

        /// <summary>Um da horda da ponte: só nasce quando a fuga começa, já correndo atrás do gato.</summary>
        static void Horde(float cx, float ly) { O("horde", "horda", cx, ly).Facing = 1; }
        static void Torch(float cx, float ly) { O("torch", "tocha", cx, ly); }
        /// <summary>Fala curta do Lion ao chegar na cena (uma vez, flag <paramref name="id"/>).</summary>
        static void Bark(string id, float wait, params string[] lines)
        {
            var o = O("bark", "fala ao chegar", 0f, 0f);
            o.Id = id; o.A = wait; o.Text = string.Join("|", lines);
        }

        static Region BuildRoad()
        {
            var reg = Begin(Road, "Estrada de Soluki", RoadMap, 0x1c2038ff);
            reg.Main = new[] { "R1_oeste", "R5_sul" };   // sem a ponte, cada cabeceira só se alcança pelo seu lado

            // R1 Saída da Fortaleza: a estrada, o posto abandonado com tochas, um infectado de espada
            var r1 = Sala("R1", "Saída da Fortaleza", 0, -1, 30, 17);
            Entrance("R1_oeste", 4f, 0f, 1);
            reg.SpawnX = FX(4f); reg.SpawnY = FY(0f);
            ExitW(0, FortressScene, "fortaleza_estrada");
            Back(9, 0, 14, 4); G(8, 5, 15, 5);                       // posto abandonado (parede ao fundo, telhado)
            Torch(8.5f, 0f); Torch(14.5f, 0f);
            Sign(11.5f, 0f, "posto abandonado");
            Shadow(9, 0, 6, 5);                                       // dentro do posto: esconderijo
            G(19, 0, 21, 1);                                          // carroça tombada
            Infected(24f, 0f, -1, 17f, 27f, shield: false);
            Bark("estrada_chegou", 0.8f, "Estrada de Soluki.", "Miaravéu fica do outro lado da ponte.");
            DoorE(0, 3);

            // R2 Posto de Vigia: torre caída de 3 andares; o de ESCUDO embaixo (de frente ele bloqueia: chegar por cima,
            // cair atrás dele, ou a marreta)
            var r2 = Sala("R2", "Posto de Vigia", r1.X + r1.W - 1, -1, 45, 25);
            G(10, 6, 22, 6); G(12, 12, 20, 12); G(14, 18, 18, 18);
            Branch(5, 3, 4); Branch(7, 9, 3); Branch(9, 15, 3);
            Torch(11f, 7f); Torch(21f, 7f);
            Infected(16f, 13f, 1, 13f, 19f, shield: false);
            Infected(32f, 0f, -1, 24f, 40f, shield: true);
            Sign(6f, 0f, "escudo: de frente ele defende. Pelas costas, ou a marreta.");
            Shadow(23, 0, 5, 5); Bush(42f, 0f);
            DoorE(0, 3);

            // R3 Cabeceira oeste: marco da ponte; a corda (AT4) só depois da ponte nova
            var r3 = Sala("R3", "Cabeceira oeste", r2.X + r2.W - 1, -1, 30, 17);
            Sign(12f, 0f, "Ponte do Último Marco");
            Torch(9f, 0f); Torch(25f, 0f);
            Branch(2, 3, 4);
            Sign(4f, 3f, "corda ↓ (ponte nova)");
            Trigger(4.5f, 4f, 1f, 2f, A, "A1_corda", BridgeFlag);
            Entrance("R3_corda", 7f, 0f, 1);
            // a horda: nasce aqui atrás quando o gato pisa na ponte
            Horde(16f, 0f); Horde(19f, 0f); Horde(22f, 0f); Horde(25f, 0f);
            DoorE(0, 3);

            // R4 Ponte do Último Marco: de pedra, inteira até o gato chegar ao meio; aí o centro desaba (cutscene 1)
            var r4 = Sala("R4", "Ponte do Último Marco", r3.X + r3.W - 1, -1, 60, 17);
            DoorS(16, 41);
            var bridge = O("bridge", "ponte de pedra (trecho que desaba)", 16f, 0f);
            bridge.W = 26f; bridge.A = FX(6f); bridge.B = FX(28f);
            Trigger(29f, -3f, 28f, 3f, A, "A1_queda", FallenFlag);   // a queda leva ao fundo do vale (depois que caiu)
            Sign(4f, 0f, "a ponte range...");
            DoorE(0, 3);
            // R5 Cabeceira leste: Tobi, S-F5, saída para Miaravéu; o buraco desce para a G3
            Sala("R5", "Cabeceira leste", r4.X + r4.W - 1, -1, 30, 17);
            Sanctuary(6f, 0f, "S-F5", "Cabeceira leste");
            Sign(13f, 0f, "Tobi (F9): 12 madeiras + 20 moedas");
            Sign(25f, 0f, "Miaravéu, adiante → (fim da fase)");
            Entrance("R5_sul", 17f, 0f, -1);
            ExitS(20, 23, F, "G3_topo");
            return reg;
        }

        /// <summary>Gatilho com posição global (uso interno dos helpers acima).</summary>
        static void TriggerAt(float gx, float gy, float w, float h, string scene, string entrance, string flag)
        {
            var o = new Obj { Kind = "exit", Name = S.Id + ": passagem → " + scene, X = gx, Y = gy, W = w, H = h, Text = scene, Id = entrance, Link = flag, Facing = 0 };
            R.Objects.Add(o);
        }

        // =========================================================== A — Raízes da Queda (fundo do vale, intensidade baixa)
        // A faixa de baixo do anel, indo para o leste. A3 é a Forest_Test_A (desenho do usuário), inteira.

        static Region BuildA()
        {
            var reg = Begin(A, "A — Raízes da Queda", ForestMap, 0x161a38ff);
            reg.Main = new[] { "A1_queda" };

            // A1 Fundo do Vale: onde o gato acorda. Morros, raízes no alto (cogumelo no morro), o primeiro templário
            // zumbi e uma serpente entre as raízes.
            Sala("A1", "Fundo do Vale", 100, -215, 60, 25);
            Entrance("A1_queda", 27f, 0f, 1);
            Entrance("A1_corda", 3f, 0f, 1);
            reg.SpawnX = FX(27f); reg.SpawnY = FY(0f);
            Sign(9f, 0f, "↑ lá no alto, a ponte");
            Bark("acordou_no_vale", 1.2f, "Ugh. Caí de novo.", "Do alto da ponte até o fundo do vale. E inteiro.", "Seis vidas. Por enquanto.");
            Branch(0, 3, 2);   // a corda (AT4) para a estrada, só com a ponte nova
            TriggerAt(FX(0.5f), FY(4f), 1f, 2f, Road, "R3_corda", BridgeFlag);
            Terrain(12, 1, 1, 2, 2, 1);
            Snake(20f, 0f, -1);
            Terrain(32, 1, 2, 2, 3, 3, 2, 1, 1);                   // morro (topo 3 em 35..36)
            Blob(5, 5, 11, 6);                                      // raiz no alto (topo 7): agarra a borda do morrinho
            Branch(16, 9, 4);
            Blob(21, 11, 29, 12); Coins(23f, 13f, 4);               // topo 13
            Mushroom(35.5f, 3f);                                    // do morro: até 16
            Blob(39, 14, 46, 15); Coins(40f, 16f, 4);               // topo 16
            Bat(43f, 22.6f);
            Infected(49f, 0f, -1, 41f, 56f, false);
            Shadow(42, 0, 4, 4); Bush(44f, 0f);
            Shadow(52, 0, 5, 5);
            DoorE(0, 3);

            // A2 Raízes Gêmeas: S-F1 entre duas raízes; arco de raiz por cima; tutorial da serpente; a raiz pendurada
            // por onde desce o cipó da Ravina (AT1, abre lá em cima, na C6)
            Sala("A2", "Raízes Gêmeas", 159, -215, 45, 25);
            Terrain(4, 2, 3, 3, 2);
            Terrain(15, 2, 3, 3, 2);
            Sanctuary(11.5f, 0f, "S-F1", "Raízes Gêmeas");
            Branch(0, 6, 3);
            Blob(3, 8, 19, 9);                                       // arco (topo 10)
            Sign(11f, 10f, "P-PRIM-01 (placa dos Primatas)");
            Coins(14f, 10f, 3);
            Sign(19.5f, 0f, "serpente: espere o bote e acerte enquanto ela estica");
            Bush(21f, 0f);
            Snake(24.5f, 0f, -1);
            G(32, 4, 33, Top);                                       // raiz pendurada (passa-se por baixo)
            ScVine(AtRavina, 32, 4, Top);
            ScRolled(AtRavina, 31.5f, Top - 1f);
            Closed(AtRavina, 31f, 0f, "Um cipó enrolado lá no alto. Alguém precisa soltar.");
            Branch(28, Top - 4, 3);                                  // chegada de quem desce pelo alçapão
            Entrance("A2_alcapao", 29.5f, Top - 4, -1, AtRavina);
            ExitN(30, 31, C, "C6_alcapao", AtRavina);                // x global 190..191 = o alçapão da C6
            Infected(38f, 0f, -1, 34f, 42f, false);
            Shadow(35, 0, 4, 4);
            DoorE(0, 3);

            // A3 O Primeiro Tronco = Forest_Test_A (desenho do usuário): alcova do tronco (W01), o poço de galhos (W02 no
            // topo) e o lado dos cactos até a porta no alto. Coordenadas da cena de teste + 6 em x.
            Sala("A3", "O Primeiro Tronco", 203, -215, 71, 42);
            DoorW(0, 3);
            G(15, 4, 23, 4); G(11, 0, 12, 1); G(23, 0, 23, 3);
            Trunk(16, 0, "gb_A3_tronco");
            Chest(20f, 0f, "W01");
            G(27, 4, 27, 38);
            G(43, 0, 43, 34);
            Mushroom(30f, 0f);
            Cactus(29.5f, 14f, 0f, 1f);
            for (int i = 0; i < 6; i++) Branch(i % 2 == 0 ? 31f : 36f, 3 + i * 3, 5f);
            Leaf(34.5f, 21f);
            Sway(38, 24, 4);
            Rotten(32, 27);
            Cactus(37.5f, 29.3f, 0f, 1f);
            G(28, 32, 34, 32);
            Chest(30f, 33f, "W02", 5);
            G(40, 34, 48, 34);
            Spikes(44, 0, 24);
            Cactus(51.5f, 30f, 1f, 0f);
            Cactus(46.2f, 24f, 0f, 0.5f, 2.5f);
            Cactus(57.1f, 15f);
            Cactus(51.5f, 10.8f, 0.75f, 0f);
            Cactus(55.3f, 5.5f, 0f, 0.3f, 2.5f);
            G(58, 25, 64, 25);
            Branch(50.3f, 22f, 4f);
            Mushroom(58.7f, 26f); Mushroom(66f, 18f);
            Mushroom(47f, 0f); Mushroom(61.5f, 0f); Mushroom(67.7f, 0f);
            G(60, 17, 68, 17);
            Sign(68.1f, 18f, "Saída →");
            DoorE(18, 21);

            // A4 Poço das Raízes: dentro de uma raiz oca. Da chegada (18) dá para descer ao fundo (Estação, serpente) ou
            // subir até a A6: raiz → cacto (pogo) → prateleira → vinha da parede. O cogumelo do fundo leva de volta.
            Sala("A4", "Poço das Raízes", 273, -215, 30, 45);
            G(0, 17, 4, 17);                                         // chegada da A3 (topo 18)
            Terrain(0, 1, 1, 1, 2, 2);
            Spikes(8, 0, 4);
            Terrain(12, 2, 3, 4, 6, 6, 6, 4, 3, 2);                  // raiz grossa no fundo (topo 6)
            Mushroom(16f, 6f);                                       // até 19
            Snake(24.5f, 0f, -1);
            Blob(19, 16, 24, 17);                                    // pouso do cogumelo (topo 18)
            Blob(9, 19, 13, 20);                                     // topo 21
            Plant(0.1f, 26f, -90f);                                  // na parede da esquerda
            Cactus(17.5f, 21.5f, 0f, 0.4f);                          // pogo até a prateleira
            G(20, 28, 27, 28);                                       // prateleira (topo 29)
            Shadow(21, 29, 5, 4);
            VE(29, 34);                                              // vinha até a porta da A6
            Branch(15, 31, 3); Leaf(10.5f, 32f); Rotten(7, 35);
            Blob(1, 37, 6, 37); Coins(2f, 38f, 3);                   // nicho no alto (topo 38)
            Bat(14f, Top + 0.6f); Bat(22f, Top + 0.6f);
            Sign(2f, 18f, "olhos da Isyathy no fundo (F5)");
            DoorE(0, 3);
            DoorE(36, 39);

            // A5 Estação dos Primatas (opcional): plataforma, vagões em 2 andares, W03 no teto (chaminé de vinhas)
            Sala("A5", "Estação dos Primatas", 302, -215, 60, 25, optional: true);
            G(7, 0, 28, 2);                                          // plataforma da estação (topo 3)
            G(31, 0, 41, 1); G(31, 6, 41, 6);                        // vagão A: piso (topo 2) e teto (topo 7)
            G(44, 0, 53, 1); G(44, 7, 53, 7); G(44, 14, 54, 14); G(54, 5, 54, 14); // vagão B: 2 andares
            V(54, 5, 54, 14); VE(1, 20);                             // chaminé de 3 (x 55..57)
            Chest(49f, 15f, "W03", 5);
            Sign(12f, 3f, "P-PRIM-02");
            Infected(18f, 3f, 1, 9f, 27f, false);
            Plant(29.5f, 0f);
            Shadow(32, 2, 9, 4);
            Spikes(42, 0, 2);
            Saw(32f, 40f, 7f);
            Coins(45f, 8f, 4);
            Crawler(4f, 0f, 1f, 6f);

            // A6 Galhos Baixos: travessia para o leste em 3 alturas; do teto desce o poço do Lago (AT5, abre na C4)
            Sala("A6", "Galhos Baixos", 302, -191, 60, 34);
            G(0, 11, 5, 11);                                         // chegada da A4 (topo 12)
            ScVine(AtLago, -1, 16, Top);                             // cipó do poço, na parede da esquerda
            ScRolled(AtLago, 0.5f, Top - 1f);
            Closed(AtLago, 1.5f, 12f, "O poço lá em cima está fechado. Deve abrir do outro lado.");
            Branch(0, Top - 4, 3);
            Entrance("A6_poco", 1.5f, Top - 4, 1, AtLago);
            ExitN(0, 2, C, "C4p_baixo", AtLago);
            // chão: furtivo, com o templário, o Galho Seco e o vão de espinhos
            Terrain(8, 1, 1, 2, 2, 2, 1);
            Shadow(14, 0, 5, 4); Bush(16f, 0f);
            Infected(26f, 0f, 1, 20f, 33f, false);
            Shadow(34, 0, 2, 6);
            Dry(37f, 0f, -1);
            Spikes(40, 0, 3);
            Mushroom(45f, 0f);                                       // até 13
            Snake(50.5f, 0f, -1);
            // meio: galhos que balançam, folhas e um galho podre
            Sway(8, 13, 4); Leaf(15.5f, 13f); Sway(20, 14, 4); Rotten(26, 14); Sway(30, 15, 4); Leaf(37.5f, 14f); Branch(42, 15, 4);
            Roof(20, 26, 24, 23, 24, 26);                            // raízes penduradas
            Bat(22f, 22.4f);
            // alto: galhos com uma lâmina pendular
            Branch(9, 17, 3); Branch(14, 20, 4); Branch(23, 20, 3); Branch(30, 21, 4); Branch(38, 20, 4); Branch(45, 19, 4);
            Blade(33.5f, Top + 1f, 9f);
            Coins(15f, 20f, 3);
            // torre de raiz com vinha na face até a saída (topo 18); W04 escondido acima da porta
            G(52, 0, 57, 10); V(52, 1, 52, 10);
            Blob(48, 14, 51, 15);
            G(52, 17, 57, 17);
            Branch(54, 22, 3); G(47, 25, 53, 25);               // nicho acima da porta (topo 26)
            Chest(50f, 26f, "W04");
            Entrance("A6_leste", 55f, 18f, -1);
            ExitE(18, B, "B1_oeste");
            return reg;
        }

        // =========================================================== B — Bosque dos Espinhos (pogo, subindo pelo leste)
        // B1 é a Forest_Test_B (desenho do usuário).

        static Region BuildB()
        {
            var reg = Begin(B, "B — Bosque dos Espinhos", ForestMap, 0x142038ff);
            reg.Main = new[] { "B1_oeste" };

            // B1 Pé do Bosque = Forest_Test_B: abrigo com o tronco (W05), correntes de cactos, plantas, patamar com Galho
            // Seco e Broto. A "porta interna" da cena de teste virou um atalho de verdade: do patamar, solta-se o cipó.
            Sala("B1", "Pé do Bosque", 361, -173, 77, 30);
            Entrance("B1_oeste", 8f, 0f, 1);
            reg.SpawnX = FX(8f); reg.SpawnY = FY(0f);
            ExitW(0, A, "A6_leste");
            Sanctuary(14f, 0f, "S-F1B", "Pé do Bosque");
            G(0, 17, 19, 17);
            G(0, 22, 7, 22);
            Trunk(7, 18, "gb_B1_tronco");
            Chest(3f, 18f, "W05");
            Dry(10.5f, 18f, 1); Dry(16.5f, 18f, 1);
            Cactus(23.5f, 2.8f); Cactus(27.5f, 8.3f); Cactus(24f, 14.8f);
            Cactus(32f, 2.8f);
            Cactus(46.5f, 11.5f);
            Branch(38.8f, 9f, 5f); Branch(35.8f, 6f, 5f);
            Plant(54f, 0f); Plant(74.9f, 21f, 90f);
            Branch(63.5f, 3f, 2f);
            G(66, 5, 74, 5);
            Coin(67.7f, 6f); Coin(71.8f, 6f); Coin(68.6f, 0.4f); Coin(70.3f, 0.4f); Coin(72.6f, 0.4f);
            G(50, 14, 72, 14);                                       // patamar (topo 15), com o buraco do cipó em 73..74
            ScBlock(AtPatamar, 73, 14, 74, 14, "tampa de raízes");
            ScVine(AtPatamar, Right + 1, 0, 13);
            ScRolled(AtPatamar, 74f, 13f);
            Lever(AtPatamar, 73.5f, 15f, 0, 1, "soltar o cipó");
            Closed(AtPatamar, 74f, 0f, "Um cipó enrolado lá em cima, no patamar.");
            Dry(55f, 15f, -1); Sprout(64f, 15f, -1);
            DoorE(15, 18);

            // B2 Escada de Cactos: vinhas em trechos que NÃO se ligam: entre um e outro, só o cacto (pogo). Planta na
            // parede, serpente que rasteja no chão, W06 num nicho do alto.
            Sala("B2", "Escada de Cactos", 437, -158, 30, 40);
            Crawler(8f, 0f, 3f, 12f);
            Mushroom(15f, 0f);                                       // até 13: moedas
            Blob(18, 12, 23, 12); Coins(18.6f, 13f, 4, 1.4f);         // a plataforma da direita, cheia (pedido de 13/11)
            Spikes(24, 0, 3);
            VW(4, 12);                                               // vinha baixa (até 12), acima da porta
            Cactus(3.5f, 12f);                                       // do alto da vinha: pogo até 20
            GV(7, 14, 9, 24);                                        // tronco com vinha nas duas faces (topo 25)
            GV(8, 9, 9, 13);                                         // raiz com vinha embaixo do tronco: o pulo do cogumelo alcança
            Plant(Right + 0.9f, 18f, 90f);
            Cactus(13.5f, 26f, 1.5f, 0f);                            // do tronco: pogo até a saída
            G(17, 28, Right, 28);                                    // saída (topo 29)
            Blob(2, 30, 8, 31); Chest(5f, 32f, "W06");               // nicho (topo 32)
            Bat(7f, Top + 0.6f); Bat(20f, Top + 0.6f);
            Coins(15f, 13f, 2);
            DoorE(29, 32);

            // B3 Jardim Carnívoro: chão de raízes com plantas, serpente e Galho Seco; pela copa (folha, galho, cogumelo)
            // até a plataforma do W07, vigiada por uma planta pendurada do teto
            Sala("B3", "Jardim Carnívoro", 466, -129, 60, 25);
            Terrain(6, 1, 2, 3, 3, 2, 1);
            Plant(13f, 0f);
            Terrain(16, 2, 2, 4, 4, 4, 4, 2, 2);
            Snake(26f, 0f, -1);
            Mushroom(28.5f, 0f);                                     // até 13
            Terrain(31, 1, 1, 3, 5, 5, 5, 5, 3, 1, 1);
            Plant(35.5f, 5f);
            Shadow(41, 0, 3, 6);
            Dry(45f, 0f, -1);
            Terrain(48, 1, 2, 2, 1);
            Crawler(54f, 0f, 53f, 57f);
            Leaf(25.5f, 8f); Branch(29, 12, 4);
            Blob(34, 15, 41, 16); Chest(38f, 17f, "W07");
            Plant(38f, Top + 1f, 180f);
            Branch(44, 19, 4); Coins(45f, 19f, 3);
            Bat(20f, Top + 0.6f); Bat(52f, Top + 0.6f);
            DoorE(0, 3);

            // B4 Torre Enferrujada: destroços de ferro em zigue-zague, cactos e galhos podres; planta na parede.
            // No alto, solta-se o cipó que desce até o pé da torre (AT2: a volta fica rápida).
            Sala("B4", "Torre Enferrujada", 525, -129, 20, 50);
            Terrain(13, 2, 3, 3, 3);
            Blob(6, 6, 10, 7);                                       // topo 8
            Cactus(14f, 10f);                                        // pogo até 18,4
            G(4, 15, 9, 16);                                         // viga (topo 17)
            Rotten(11, 20); Rotten(13, 23);
            Plant(Right + 0.9f, 22f, 90f);
            G(5, 25, 9, 25);                                         // viga (topo 26)
            Cactus(10f, 28f, 1.2f, 0f);                              // pogo até 36,4
            G(11, 34, Right, 35);                                    // viga (topo 36)
            Bat(14f, 33.6f);
            Branch(5, 40, 4);
            G(1, 43, 4, 43);                                         // topo (44): porta para a Copa
            ScVine(AtTorre, -1, 4, 43);                              // começa acima da porta do Jardim
            ScRolled(AtTorre, 0.5f, 42f);
            Lever(AtTorre, 2f, 44f, 0, 1, "soltar o cipó");
            Closed(AtTorre, 0.5f, 0f, "Um cipó enrolado lá no alto da torre.");
            Sanctuary(6f, 0f, "S-F1C", "Pé da Torre");                // o cipó do alto (AT) desce até aqui
            DoorE(0, 3);                                             // para o Desafio (opcional)
            DoorW(44, 47);

            // B7 Desafio dos Espinhos (opcional): só pogo, chão de espinhos; W08 no alto
            Sala("B7", "Desafio dos Espinhos", 544, -129, 30, 50, optional: true);
            Sign(2f, 0f, "Desafio dos Espinhos: cair volta ao começo");
            Spikes(6, 0, 22);
            Mushroom(3.5f, 0f);
            Cactus(8.5f, 10f); Cactus(13.5f, 16f, 0.8f, 0f); Cactus(19.5f, 22f); Cactus(24.5f, 28f, 0f, 0.6f);
            Cactus(19.5f, 33f); Cactus(13.5f, 38f, 1f, 0f);
            G(2, 43, 8, 43); Chest(5f, 44f, "W08", 5);
            Plant(Right + 0.9f, 25f, 90f);

            // B5 Copa Baixa: indo para o oeste por cima do Jardim. Chão com dois templários (um de escudo), sombras,
            // matos e uma serpente; na copa, galhos largos e morcegos nas raízes penduradas
            Sala("B5", "Copa Baixa", 466, -85, 60, 25);
            Terrain(3, 1, 2, 2, 1);
            Infected(15f, 0f, 1, 8f, 24f, false);
            Shadow(26, 0, 5, 4); Bush(28f, 0f);
            Snake(33f, 0f, -1);
            Infected(44f, 0f, -1, 37f, 52f, true);
            Shadow(52, 0, 4, 4);
            Branch(6, 4, 6); Branch(15, 7, 5); Branch(24, 9, 6); Branch(33, 8, 5); Branch(41, 10, 6); Branch(50, 5, 5);
            Roof(8, 18, 16, 15, 16, 18); Bat(10f, 14.4f);
            Roof(30, 17, 15, 14, 15, 17); Bat(32f, 13.4f);
            Roof(46, 19, 17, 16, 17, 19); Bat(48f, 15.4f);
            Coins(25f, 9f, 3); Coins(42f, 10f, 3);
            Mushroom(20f, 0f);
            Entrance("B5_oeste", 3f, 0f, 1);
            ExitW(0, C, "C1_leste");
            DoorE(6, 9);                                             // para o Mirante (opcional)

            // B6 Mirante da Copa (respiro): a vista, moedas e um cogumelo até o galho mais alto
            Sala("B6", "Mirante da Copa", 525, -80, 25, 20, optional: true);
            G(0, 0, 22, 0);
            Sign(12f, 1f, "Mirante da Copa: a ponte, lá no alto");
            Coins(6f, 1f, 5);
            Mushroom(19f, 1f);
            Blob(10, 13, 16, 13); Coins(11f, 14f, 4);
            return reg;
        }

        // =========================================================== C — Vale das Sombras (furtividade, para o oeste)
        // Passa por cima da A e da B: daqui saem dois atalhos para baixo (Lago → A6, Ravina → A2).

        static Region BuildC()
        {
            var reg = Begin(C, "C — Vale das Sombras", ForestMap, 0x0e1228ff);
            reg.Main = new[] { "C1_leste" };

            // C1 Entrada do Vale: escuro, folhagem alta, templários, serpentes e plantas; pela copa, galhos.
            // No teto, o poço do Musgo (AT6: abre lá em cima, na E4)
            Sala("C1", "Entrada do Vale", 407, -85, 60, 25);
            Entrance("C1_leste", 55f, 0f, -1);
            reg.SpawnX = FX(55f); reg.SpawnY = FY(0f);
            ExitE(0, B, "B5_oeste");
            Terrain(44, 1, 1, 2, 2, 2, 1);
            Shadow(46, 2, 5, 5);
            Infected(37f, 0f, -1, 27f, 42f, false);
            Shadow(28, 0, 5, 5); Bush(30f, 0f);
            Snake(22f, 0f, 1);
            Plant(16f, 0f);
            Infected(7f, 0f, 1, 2f, 13f, true);
            Shadow(0, 0, 3, 5);
            Branch(48, 4, 4); Branch(41, 6, 4); Branch(32, 8, 4); Branch(27, 9, 4); Rotten(23, 9); Branch(15, 8, 4); Branch(8, 6, 4); Branch(2, 4, 4);
            Bat(29f, Top + 0.6f); Bat(12f, Top + 0.6f);
            Coins(28f, 9f, 3);
            // poço do Musgo: raiz pendurada com o cipó, o buraco no teto em 33..35
            G(36, 6, 37, Top);
            ScVine(AtMusgo, 36, 6, Top);
            ScRolled(AtMusgo, 35.5f, Top - 1f);
            Closed(AtMusgo, 35f, 0f, "Tem um poço lá em cima. Fechado.");
            Branch(32, Top - 4, 3);
            Entrance("C1_musgo", 33.5f, Top - 4, -1, AtMusgo);
            ExitN(33, 35, E, "E4p_baixo", AtMusgo);
            DoorW(0, 3);

            // C2 Raízes Caídas: descida entre raízes. Vinhas nas paredes para voltar; um Galho Seco no caminho e um
            // nicho com o W09.
            Sala("C2", "Raízes Caídas", 378, -140, 30, 62);
            G(22, 54, Right, 54);                                    // chegada da C1 (topo 55)
            Blob(12, 50, 17, 51);                                    // topo 52
            Branch(4, 47, 5);
            Blob(14, 41, 20, 42); Plant(18f, 43f);                   // topo 43
            Rotten(7, 39);
            VW(10, 36);
            G(0, 30, 6, 30); Snake(4f, 31f, 1);                      // topo 31
            Blob(15, 23, 23, 24); Dry(19f, 25f, -1);                 // topo 25
            Branch(4, 17, 4);
            VE(3, 22);
            G(22, 10, Right, 10); Chest(25f, 11f, "W09");            // nicho (topo 11)
            Mushroom(18f, 0f);
            Spikes(20, 0, Right - 19);                               // até a parede
            Crawler(7f, 0f, 2f, 12f);
            Bat(8f, 44.4f); Bat(24f, 30f);
            DoorE(55, 58);
            DoorW(0, 3);

            // C3 Prédio Soterrado: entra-se por baixo; andares com templários, sombras e a escada de galhos; sai-se no
            // primeiro andar. No último, o escritório com o tronco rachado e o W10.
            Sala("C3", "Prédio Soterrado", 334, -140, 45, 45);
            G(0, 8, 36, 8);                                          // andar 1 (topo 9)
            Branch(38, 4, 4);
            G(6, 17, Right, 17);                                     // andar 2 (topo 18)
            Branch(1, 13, 4);
            G(0, 26, 36, 26);                                        // andar 3 (topo 27)
            Branch(38, 22, 4);
            G(6, 35, Right, 35);                                     // andar 4 (topo 36)
            Branch(1, 31, 4);
            Infected(20f, 0f, 1, 8f, 32f, false); Shadow(33, 0, 4, 5); Shadow(2, 0, 4, 5);
            Infected(18f, 9f, -1, 8f, 30f, true); Shadow(31, 9, 5, 5); Bush(12f, 9f);
            Sign(22f, 9f, "P-PRIM-03");
            Infected(24f, 18f, 1, 10f, 36f, false); Shadow(8, 18, 4, 5); Plant(40f, 18f);
            Dry(20f, 27f, -1); Shadow(28, 27, 6, 5); Snake(10f, 27f, 1);
            Trunk(24, 36, "gb_C3_tronco"); Chest(30f, 36f, "W10"); Shadow(33, 36, 5, 4);
            Coins(12f, 36f, 3);
            DoorE(0, 3);
            DoorW(9, 12);

            // C4 Caverna do Lago: lago raso com templários na água, ilha com o W11 (rota furtiva); cogumelos e folhas.
            // No chão, o alçapão do poço do Lago (AT5): aberto daqui, desce até a A6
            Sala("C4", "Caverna do Lago", 275, -140, 60, 25);
            G(50, 0, Right, 8); V(50, 1, 50, 8);                     // chegada da C3 (topo 9)
            Sanctuary(54f, 9f, "S-F1D", "Caverna do Lago");             // no patamar da chegada, longe da água
            Pool(13.5f, 0f, 24);                                     // água nas células 2..25
            G(0, 0, 1, 0); G(26, 0, 26, 0);                          // margens: a água não acaba no ar
            Terrain(11, 2, 3, 3, 3, 2); Chest(13f, 3f, "W11"); Bush(14.5f, 3f); Shadow(11, 3, 5, 4);
            Infected(20f, 0f, -1, 17f, 25f, false); Infected(6f, 0f, 1, 3f, 10f, false);
            DoorS(27, 29);
            ScBlock(AtLago, 27, -1, 29, -1, "tampa de raízes");
            Lever(AtLago, 28f, 0f, 0, 1, "abrir o alçapão");
            Sign(24.5f, 0f, "o lago escoa por aqui");
            Mushroom(34f, 0f); Mushroom(45f, 0f);
            Snake(40f, 0f, -1);
            Leaf(37.5f, 8f); Branch(30, 10, 3); Branch(42, 11, 4); Leaf(21.5f, 9f); Branch(14, 11, 4); Branch(6, 9, 4);
            Coins(15f, 11f, 3);
            Bat(36f, Top + 0.6f);
            Shadow(44, 0, 4, 4);
            DoorE(9, 12);
            DoorW(0, 3);

            // C4p Poço do Lago: o poço estreito entre a caverna e a A6 (cipó na parede da esquerda)
            Sala("C4p", "Poço do Lago", 302, -158, 5, 19);
            ScVine(AtLago, -1, 0, Top);
            Branch(0, 2, 3);
            Entrance("C4p_baixo", 1f, 2f, 1, AtLago);
            ExitS(0, 2, A, "A6_poco", AtLago);

            // C5 Escombros da Estrada Negra: a estrada velha tomada pelo mato; carcaças, templários (um de escudo),
            // serpentes e plantas. No chão, o buraco para o Bosque Morto (opcional); por cima, galhos.
            Sala("C5", "Escombros da Estrada Negra", 201, -140, 75, 25);
            Terrain(66, 1, 1, 2, 2, 2, 1, 1);
            G(55, 0, 60, 2); Shadow(61, 2, 4, 4);
            Infected(48f, 0f, -1, 42f, 53f, true); Shadow(42, 0, 4, 4);
            DoorS(38, 41); Sign(36f, 0f, "↓ Bosque Morto");
            G(29, 0, 34, 3); Snake(27f, 0f, 1);
            Infected(19f, 0f, 1, 12f, 25f, false);
            Bush(14f, 0f); Shadow(12, 0, 4, 5);
            Plant(8f, 0f);
            Crawler(3f, 0f, 1f, 5f);
            Branch(52, 7, 4); Branch(44, 9, 4); Branch(36, 10, 4); Rotten(31, 10); Branch(24, 9, 4); Branch(16, 8, 4); Branch(8, 6, 4);
            Mushroom(63f, 0f);
            Coins(37f, 10f, 3); Coins(17f, 8f, 3);
            Bat(46f, Top + 0.6f); Bat(20f, Top + 0.6f);
            DoorE(0, 3);
            DoorW(0, 3);

            // C7 Bosque Morto (opcional): cai-se de cima; desce-se por galhos podres até o W12 e volta-se pela vinha
            Sala("C7", "Bosque Morto", 230, -173, 30, 34, optional: true);
            Blob(7, 28, 15, 29);                                     // pouso (topo 30)
            Rotten(17, 26); Rotten(21, 22);
            G(22, 17, Right, 17); Sprout(25f, 18f, -1);              // topo 18
            Rotten(16, 14); Rotten(11, 11);
            Plant(Right + 0.9f, 8f, 90f);
            Spikes(5, 0, 6);
            Crawler(16f, 0f, 13f, 22f);
            Chest(25f, 0f, "W12", 5);
            VW(1, 25);
            Branch(0, 27, 4);
            Bat(12f, 20f);

            // C6 Ravina: desce-se das Escombros até o fundo (S-F2). No meio, a passagem para a árvore gigante (D1);
            // no chão, o alçapão para a A2 (AT1).
            Sala("C6", "Ravina", 172, -191, 30, 57);
            G(22, 50, Right, 50);                                    // chegada das Escombros (topo 51)
            Blob(12, 44, 18, 45);                                    // topo 46
            Branch(3, 40, 4);
            Blob(13, 34, 20, 35); Plant(18f, 36f);                   // topo 36
            Rotten(8, 31);
            G(1, 26, 6, 26);                                         // topo 27
            Bat(4f, 25.4f);
            G(1, 20, 5, 20);                                         // porta da árvore (topo 21)
            VW(1, 19);                                               // vinha do fundo até a porta
            VE(5, 30);
            Blob(20, 12, Right, 13);                                 // topo 14
            Sanctuary(6f, 0f, "S-F2", "Ravina");
            Entrance("C6_alcapao", 14.5f, 0f, -1);
            DoorS(17, 18);
            ScBlock(AtRavina, 17, -1, 18, -1, "tampa de raízes");
            Lever(AtRavina, 17.5f, 0f, 0, 1, "abrir o alçapão");
            ExitS(17, 18, A, "A2_alcapao", AtRavina);
            Entrance("C6_oeste", 1.5f, 21f, 1);
            ExitW(21, D, "D1_leste");
            DoorE(51, 54);
            return reg;
        }

        // =========================================================== D — Refúgio da Vulpis (a árvore gigante)
        // D3 é a Forest_Test_C (desenho do usuário): o hub com a cabana da Vulpis, a arena e o alto.

        static Region BuildD()
        {
            var reg = Begin(D, "D — Refúgio da Vulpis", ForestMap, 0x141a34ff);
            reg.Main = new[] { "D1_leste" };

            // D1 Trilha das Flechas: a árvore gigante. Flechas cravadas viram degraus; vinhas em trechos; morcegos,
            // planta e uma serpente num galho largo.
            Sala("D1", "Trilha das Flechas", 143, -170, 30, 60);
            Entrance("D1_leste", 25f, 0f, -1);
            reg.SpawnX = FX(25f); reg.SpawnY = FY(0f);
            ExitE(0, C, "C6_oeste");
            G(8, 0, 15, 50);                                         // o tronco (topo 51)
            V(15, 8, 15, 18); V(8, 26, 8, 38);
            Mushroom(21f, 0f);                                       // até 13
            Branch(16, 12, 2); Branch(20, 16, 3);
            Branch(24, 20, 3); Branch(23, 23, 3); Branch(23, 26, 3);
            Plant(Right + 0.9f, 25f, 90f);
            Blob(17, 27, 22, 28); Snake(20f, 29f, -1);               // galho largo (topo 29)
            Branch(16, 32, 2);
            for (int y = 3; y <= 15; y += 6) { Branch(1, y, 3); Branch(5, y + 3, 3); }   // lado oeste: flechas desde o chão
            Branch(5, 21, 3); Branch(2, 24, 3); Branch(5, 27, 3);
            for (int y = 30; y <= 48; y += 6) { Branch(0, y, 2); Branch(5, y + 3, 3); }
            Bat(3f, 40f);
            Branch(16, 36, 2); Branch(20, 40, 3); Branch(16, 44, 2); Branch(21, 48, 3); Branch(17, 51, 2);
            G(20, 53, Right, 53);                                    // saída (topo 54)
            Coins(23f, 41f, 2);
            DoorE(54, 57);

            // D2 O Resgate: galho largo da cutscene da Vulpis (F6); trilha com templários, sombras e serpente
            Sala("D2", "O Resgate", 172, -116, 34, 17);
            Sign(4f, 0f, "flechas cravadas pelo caminho");
            Sign(16f, 0f, "Vulpis no galho alto (F6)");
            Branch(6, 5, 4); Branch(13, 7, 4); Branch(21, 8, 4);
            Infected(17f, 0f, 1, 9f, 26f, false);
            Shadow(5, 0, 4, 4); Shadow(26, 0, 4, 4);
            Snake(29.5f, 0f, -1);
            Bush(11f, 0f);
            DoorE(0, 3);

            // D3 Refúgio = Forest_Test_C (desenho do usuário): coordenadas da cena de teste + 6 em x
            const string traps = "D3_baixo";
            Sala("D3", "Refúgio", 205, -116, 87, 44);
            DoorW(0, 3);
            G(27, 5, 27, 11); G(27, 17, 27, 27);
            GV(22, 3, 22, 28);                                       // pilar de vinhas
            GV(26, 5, 26, 11); GV(26, 17, 26, 27);
            Branch(23, 12, 4);
            G(0, 11, 8, 11); Hook(9f, 11.4f);
            Bat(3.8f, 10.4f, 12f); Bat(6.7f, 10.4f, 12f);
            G(15, 17, 21, 17); Hook(14.2f, 16.8f); Bat(18.4f, 16.4f);
            G(0, 23, 9, 23); Hook(10f, 23.2f);
            Bat(0.7f, 22.4f); Bat(5f, 22.4f);
            Chest(3.5f, 24f, "W15", 5, "Arqueiro");
            Bat(5f, 27.4f); Bat(11f, 27.4f); Bat(16.5f, 27.4f);
            G(0, 28, 21, 28);
            Back(3, 29, 17, 35); G(2, 36, 18, 36);
            Residence(12f, 29f, Cabin, "entrar", "cabana_vulpis", "cabana");
            Entrance("cabana_vulpis", 14f, 29f, 1);
            Sign(9f, 33.4f, "Casa da Vulpis");
            Plant(29f, 0f);
            G(34, 5, 36, 11); G(37, 9, 47, 11);
            Blade(35.5f, 5f, 3.5f, 0f, traps); Blade(45.5f, 9f, 4.6f, 0.5f, traps);
            G(42, 0, 48, 2);
            SpikesG(49.5f, 0f, 7, traps);
            G(57, 0, 58, 3); G(59, 0, 64, 6); G(65, 0, 66, 3);
            Saw(59.5f, 63.5f, 7f, traps);
            VSaw(53f, 1.2f, 10.2f, traps);
            Sanctuary(69.5f, 0f, "S-F3", "Refúgio");
            Switch(73.7f, 0f, traps);
            G(72, 6, 84, 11);
            Plant(77.5f, 6f, 180f); Plant(Right + 0.9f, 1.8f, 90f);
            Key(80.4f, 0.6f, "cabana");
            Coin(77f, 0.8f);
            G(27, 12, 84, 12); G(28, 24, 84, 24); G(68, 17, 68, 23);
            ArenaGate("D3_arena", 28.5f, 13f); ArenaGate("D3_arena", 68.5f, 13f);
            Plant(40.6f, 13f); Plant(51.8f, 13f); Plant(60.6f, 13f);
            Arena("D3_arena", 48f, 18.5f, 40f, 11f);
            Wave2(Sprout(46f, 13f, -1), "D3_arena");
            Wave2(Bat(34f, 23.4f, 40f), "D3_arena"); Wave2(Bat(56f, 23.4f, 40f), "D3_arena"); Wave2(Bat(64f, 23.4f, 40f), "D3_arena");
            Chest(78f, 13f, "W13", 10);
            Dry(35.6f, 25f, -1); Sprout(54f, 25f, -1);
            G(41, 28, 47, 28); G(51, 32, 66, 32);
            GV(66, 33, 66, 41);
            G(80, 27, 84, 27); Chest(82.5f, 28f, "W14");
            Coin(76f, 25.6f); Coin(77f, 26.8f); Coin(78f, 28f);
            Sign(63f, 34f, "saída ↑");
            DoorN(61, 65, false);

            // D4 Mirante: o topo da árvore; a Isyathy enrolada ao longe. Templário, serpente e um vão de espinhos
            Sala("D4", "Mirante", 240, -73, 45, 17);
            Sign(20f, 0f, "a Isyathy enrolada no alto (F5)");
            Shadow(10, 0, 4, 4);
            Spikes(33, 0, 3); Cactus(34.5f, 4f, 0f, 0.4f);
            Infected(15f, 0f, 1, 2f, 22f, false);
            Snake(38f, 0f, -1);
            Branch(5, 5, 4); Branch(12, 8, 4); Coins(13f, 8f, 3);
            Entrance("D4_leste", 40f, 0f, -1);
            ExitE(0, E, "E1_oeste");
            return reg;
        }

        // =========================================================== E — Ninho Adormecido (furtividade, subindo)

        static Region BuildE()
        {
            var reg = Begin(E, "E — Ninho Adormecido", ForestMap, 0x10142aff);
            reg.Main = new[] { "E1_oeste" };
            var d4 = Get(D).FindRoom("D4");

            // E1–E4: o caminho até o chefe dormindo. Movimento encadeado (pedido do usuário, 13/11/2026): rampa + pogo,
            // rampa + pulo para grudar na vinha, rampa que cai em outra rampa e pula numa plataforma temporária.
            // Ganchos por toda parte: com o Caçador, o caminho de volta pelo alto.

            // E1 Sulcos: rampa → pogo nos espinhos → rampa → descida → pulo até a vinha do pilar
            var e1 = Sala("E1", "Sulcos", d4.X + d4.W - 1, d4.Y, 60, 25);
            Entrance("E1_oeste", 3f, 0f, 1);
            reg.SpawnX = FX(3f); reg.SpawnY = FY(0f);
            ExitW(0, D, "D4_leste");
            Sign(1f, 0f, "medidor de alerta da Isyathy (F7)");
            Slope(6, 0, 8, 4, true); G(14, 0, 19, 3);                    // rampa 1:2 até o primeiro patamar (topo 4)
            Spikes(20, 0, 6); Sign(18f, 4f, "pogo nos espinhos ↓");     // vão de 6: só quicando
            G(26, 0, 29, 3); Slope(26, 4, 4, 4, true); G(30, 0, 37, 7);  // rampa 1:1 até o segundo (topo 8)
            Slope(38, 4, 4, 4, false); G(38, 0, 41, 3); G(42, 0, 47, 3); // desce correndo...
            G(50, 0, 51, 13); V(50, 1, 50, 13); V(51, 1, 51, 13);        // ...e pula para a vinha do pilar
            G(54, 9, 57, 9);                                             // saída (topo 10); o cogumelo fica ao lado, em x 52..53
            Shadow(15, 4, 4, 4); Shadow(31, 8, 4, 4); Shadow(43, 4, 4, 4); Shadow(54, 10, 4, 4); Shadow(52, 0, 6, 5);
            Bush(17f, 4f);
            Infected(34f, 8f, -1, 30f, 37f, false); Infected(45f, 4f, 1, 42f, 47f, false);
            Snake(15.5f, 4f, -1);                                        // no patamar 1 (plano), de frente para quem sobe a rampa
            Plant(48.5f, 0f);
            Mushroom(52.9f, 0f);
            Sign(10f, 0f, "ossadas");
            Hooks(16f, 10f, 23f, 34f, 46f, 55f);
            DoorE(10, 13);

            // E2 A Raiz que Respira: subida com patamares; rampas que jogam para a vinha; quedas nunca matam.
            // No alto, solta-se o cipó que desce até a E1 (AT: a volta fica rápida).
            var e2 = Sala("E2", "A Raiz que Respira", e1.X + e1.W - 1, e1.Y + 10, 30, 51);
            Terrain(0, 1, 1, 2, 2, 1);
            Blob(5, 3, 9, 4); Branch(1, 8, 4);
            G(10, 14, 16, 14); Slope(13, 15, 3, 3, true); G(16, 15, 16, 17); // patamar 1 com rampa (topo 18)
            Cactus(5.5f, 16f);                                                // pogo até ~23: alcança o galho de 22
            G(12, 29, 18, 29); Slope(15, 30, 3, 3, true); G(18, 30, 18, 32); // patamar 2 com rampa (topo 33)
            Rotten(21, 34);                                                   // plataforma temporária até a vinha
            VE(1, 17); VE(23, 44); Sway(22, 19, 4);                 // galho perto da parede: ponte entre as duas vinhas
            Mushroom(25f, 0f);
            Plant(0.1f, 24f, -90f);
            Branch(5, 22, 4); Branch(8, 25, 3);
            Branch(13, 44, 4); Branch(19, 45, 4); Branch(24, 45, 3);
            Shadow(0, 26, 5, 8); Shadow(11, 15, 2, 4);
            Sign(14f, 30f, "a raiz se mexe (D-NINHO-01)");
            // (sem serpente no patamar 2: pedido do usuário, 14/11/2026)
            Hook(22f, 10f); Hook(14f, 24f); Hook(18f, 38f); Hook(10f, 46f); Hook(5f, 18f);
            // o vão da vinha da direita (18..22): lá de cima, solta-se o cipó que o fecha; a parede inteira vira subida
            ScVine(AtRaiz, Right + 1, 18, 22);
            ScRolled(AtRaiz, Right + 0.5f, 23f);
            Lever(AtRaiz, 25.5f, 45f, 0, 1, "soltar o cipó");
            Closed(AtRaiz, 26f, 0f, "A vinha some no meio da parede. Tem um cipó enrolado lá em cima.");
            Sanctuary(13f, 0f, "S-F3B", "Raiz que Respira");           // no pé da subida; o cipó do alto volta para cá
            DoorE(45, 48);

            // E3 Travessia das Escamas: por cima (rampa que cai em outra rampa e pula na plataforma temporária) ou
            // por baixo (tronco, curta e barulhenta, W17)
            var e3 = Sala("E3", "Travessia das Escamas", e2.X + e2.W - 1, e2.Y + 45, 60, 34);
            Branch(1, 3, 4); Branch(5, 6, 3);
            G(8, 7, 46, 7); Spikes(18, 8, 6); Spikes(30, 8, 4); Spikes(39, 8, 4);
            Branch(14, 11, 4); Branch(20, 14, 4); G(25, 16, 28, 16);          // sobe até o alto (topo 17)
            Slope(29, 13, 4, 4, false); G(29, 12, 32, 12);                   // rampa 1 desce de 17 a 13...
            Slope(34, 9, 3, 3, false); G(34, 8, 36, 8);                      // ...cai na rampa 2 (12 a 9)...
            Rotten(39, 11);                                                  // ...e pula na plataforma temporária
            Mushroom(44f, 8f);
            Trunk(20, 0, "gb_E3_tronco"); Chest(24f, 0f, "W17");
            Shadow(28, 0, 10, 5); Infected(36f, 0f, -1, 27f, 44f, false);
            Crawler(12f, 0f, 9f, 18f);
            Branch(47, 3, 4); Branch(47, 6, 4); G(51, 7, 57, 7);
            Sign(10f, 0f, "por baixo: curto e barulhento (+60 de alerta)");
            Hooks(26f, 12f, 20f, 30f, 40f, 50f); Hook(54f, 14f); Hook(8f, 18f);
            Bat(16f, Top + 0.6f); Bat(42f, Top + 0.6f);
            DoorE(8, 11);

            // E4 Clareira do Musgo: S-F4 (o último santuário antes do chefe). No chão, o alçapão do poço do Musgo:
            // aberto daqui, desce até a Entrada do Vale (C1)
            var e4 = Sala("E4", "Clareira do Musgo", e3.X + e3.W - 1, e3.Y + 8, 30, 16);
            Sanctuary(5f, 0f, "S-F4", "Clareira do Musgo");
            Sign(16f, 0f, "daqui em diante não há volta");
            Branch(13, 3, 4); Branch(17, 6, 4); G(22, 8, 27, 8);
            Shadow(19, 0, 4, 4);
            Hooks(12f, 6f, 15f, 24f);
            int musgo = 441 - (e4.X + 1);                             // o poço (x global 441..443)
            DoorS(musgo, musgo + 2);
            ScBlock(AtMusgo, musgo, -1, musgo + 2, -1, "tampa de musgo");
            Lever(AtMusgo, musgo + 1.5f, 0f, 0, 1, "abrir o alçapão");
            DoorE(9, 12);

            // E4p Poço do Musgo: o poço comprido até o teto da C1 (cipó na parede da direita)
            int pTop = e4.Y;                                          // a linha de baixo da E4
            var c1 = Get(C).FindRoom("C1");
            int pBottom = c1.Y + c1.H - 1;                            // o teto da C1
            Sala("E4p", "Poço do Musgo", 440, pBottom, 5, pTop - pBottom + 1);
            ScVine(AtMusgo, Right + 1, 0, Top);
            for (int y = 8; y < Top - 2; y += 10) Branch(0, y, 2);    // descansos (mão única)
            Branch(0, 2, 3);
            Entrance("E4p_baixo", 1f, 2f, 1, AtMusgo);
            ExitS(0, 2, C, "C1_musgo", AtMusgo);
            // o buraco de cima é o alçapão da E4 (mesma cena)
            R.Late.Add(new Tile { Map = Map.Ground, Clear = true, X0 = GX(0), Y0 = S.Y + S.H - 1, X1 = GX(2), Y1 = S.Y + S.H - 1 });

            // E5 Olho no Fundo: o chefe dorme ao fundo. Cai-se do alto (sem volta) e a furtividade vai até a arena.
            // Zigue-zague diante da cabeça; W18 no nicho embaixo do focinho.
            var e5 = Sala("E5", "Olho no Fundo", e4.X + e4.W - 1, e4.Y, 45, 34);
            G(0, 8, 14, 8);                                                    // chegada no alto do focinho (topo 9)
            Sign(3f, 9f, "sem volta: só para a frente");
            Sign(30f, 0f, "a cabeça dorme ao fundo (F7)");
            Chest(5f, 0f, "W18"); Shadow(0, 0, 15, 8);
            Infected(9f, 0f, 1, 3f, 13f, false); Infected(30f, 0f, -1, 17f, 40f, true);
            Shadow(18, 0, 4, 5); Shadow(36, 0, 5, 5);
            Snake(24f, 0f, 1);
            for (int i = 0; i < 9; i++) Branch(i % 2 == 0 ? 32 : 24, 3 + i * 3, 5);
            Plant(Right + 0.9f, 14f, 90f);
            G(37, 27, 42, 27);
            DoorE(28, 31);

            // E6 Antessala: a música some; ainda furtividade (sem santuário: a volta é só depois da Isyathy)
            var e6 = Sala("E6", "Antessala", e5.X + e5.W - 1, e5.Y + 28, 30, 17);
            Sign(6f, 0f, "a música some");
            Infected(15f, 0f, 1, 9f, 22f, false);
            Shadow(3, 0, 4, 4); Shadow(17, 0, 4, 4); Bush(24f, 0f);
            Crawler(8f, 0f, 5f, 12f);
            ExitE(0, F, "F1_oeste");
            return reg;
        }

        // =========================================================== F — Coração e a subida final (G1–G3)

        static Region BuildF()
        {
            var reg = Begin(F, "F — Coração da Floresta", ForestMap, 0x181430ff);
            reg.Main = new[] { "F1_oeste" };
            var e6 = Get(E).FindRoom("E6");

            // F1 Coração da Floresta: arena em 3 alturas (a Isyathy e os portões vêm na F8)
            var f1 = Sala("F1", "Coração da Floresta", e6.X + e6.W - 1, e6.Y, 40, 28);
            Entrance("F1_oeste", 3f, 0f, 1);
            reg.SpawnX = FX(3f); reg.SpawnY = FY(0f);
            // sem saída para o oeste: da E5 até aqui não há volta (pedido do usuário, 13/11/2026)
            Sign(19f, 0f, "Isyathy, a Devoradora Silenciosa (F8)");
            for (int y = 3; y <= 15; y += 6) { G(3, y - 1, 8, y - 1); G(29, y - 1, 34, y - 1); }   // laterais: topos 3, 9, 15
            for (int y = 6; y <= 18; y += 6) { G(10, y - 1, 15, y - 1); G(22, y - 1, 27, y - 1); } // meio: topos 6, 12, 18
            Branch(16, 21, 6); Branch(15, 24, 8);
            DoorN(17, 20);

            // G1 Clareira do Espírito: menos névoa, vagalumes; W19 num desvio
            var g1 = Sala("G1", "Clareira do Espírito", f1.X, f1.Y + f1.H - 1, 45, 25);
            Branch(3, 4, 4); Branch(7, 7, 3); G(0, 9, 5, 9); Chest(2.5f, 10f, "W19", 5);
            Sign(26f, 0f, "o espírito da Isyathy (F8)");
            Coins(20f, 0f, 4);
            // o cogumelo fica AO LADO da plataforma (antes estava embaixo e o gato batia a cabeça nela)
            Mushroom(31.5f, 0f); Blob(33, 11, 40, 11); Coins(34f, 12f, 4);   // topo 12
            DoorE(0, 3);

            // G2 Coluna dos Primatas: poço de elevador. Sobe-se só pelos cogumelos (esquerda); a direita, que era uma
            // segunda subida igual (vinhas), virou um poço de cactos com moedas num nicho, fechado por cima (14/11/2026)
            var g2 = Sala("G2", "Coluna dos Primatas", g1.X + g1.W - 1, g1.Y, 20, 51);
            G(4, 4, 14, 46);                                            // a coluna (topo 47)
            // cogumelos em zigue-zague: nenhum fica embaixo do patamar seguinte (o gato batia a cabeça)
            Mushroom(1f, 0f);
            G(2, 10, 3, 10); Mushroom(3f, 11f);
            G(0, 21, 1, 21); Mushroom(1f, 22f);
            G(2, 32, 3, 32); Mushroom(3f, 33f);
            G(0, 44, 1, 44);
            Cactus(15.5f, 2.5f); Cactus(15.5f, 8.5f, 0f, 0.5f); Cactus(15.5f, 14f);  // pogo em fila (5,5 entre eles)
            G(16, 19, 17, 19); Coin(16.5f, 20f, 3); Coin(17.5f, 21f, 3);         // nicho (topo 20): moedas de 3
            G(15, 24, 17, 46);                                                 // fechado acima do nicho
            Bat(2.5f, 27f);                                                    // o morcego passa para os cogumelos
            Sign(8f, 47f, "P-PRIM-05");
            DoorN(8, 11);

            // G3 Sob a Ponte: a última subida pelo pilar leste; aurora no topo; sai na cabeceira leste (R5)
            var g3 = Sala("G3", "Sob a Ponte", g2.X - 5, g2.Y + g2.H - 1, 30, 51);
            for (int i = 0; i < 14; i++) Branch(i % 2 == 0 ? 23 : 18, 3 + i * 3, 4);
            VW(1, 40); Branch(2, 42, 4);
            G(8, 43, 17, 43);
            Bat(6f, 25f); Bat(24f, 20f);
            Sign(12f, 44f, "aurora ↑");
            Entrance("G3_topo", 9f, 44f, 1);
            ExitN(10, 15, Road, "R5_sul");
            return reg;
        }
    }
}
