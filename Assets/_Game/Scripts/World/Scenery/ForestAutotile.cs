using System.Collections.Generic;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Escolha do tile de cada célula da Floresta Silenciosa (F5, Docs/Floresta/plano-implementacao.md), pelos vizinhos e
    /// pela profundidade. Mesma ideia do <see cref="TerrainAutotile"/> da Fortaleza, com materiais próprios. Regra única
    /// usada pelo Unity (ForestPainter), pelo gerador de sprites (Tools/EnvironmentSprites/ForestSpriteGen) e pela prévia
    /// (Tools/LevelPreview/ForestArtPreview).
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell).
    /// Nome do tile: tile_&lt;material&gt;_&lt;faixa&gt;_&lt;máscara hex&gt;_&lt;variação&gt;. A colisão não muda: todo tile
    /// de chão continua sendo um quadrado cheio.
    /// </summary>
    public static class ForestAutotile
    {
        /// <summary>Chão da floresta: musgo e folhas em cima, terra escura com raízes, rocha fria no fundo.</summary>
        public const string Soil = "flo_solo";
        /// <summary>Chão tomado pela praga (Ninho e Coração): musgo morto arroxeado, veias roxas na terra.</summary>
        public const string Blight = "flo_praga";
        /// <summary>Ruínas dos Primatas: lajes de concreto retas, musgo por cima, ferrugem e vergalhão.</summary>
        public const string Concrete = "pri_concreto";

        /// <summary>
        /// Templo dos Primatas (D6, pedido do usuário, 14/11/2026: "parecido com a imagem"): blocos de pedra cinza em
        /// fiadas desencontradas, grama viva por cima caindo pela borda, flores no gramado.
        /// </summary>
        public const string Temple = "pri_templo";

        public static readonly string[] Materials = { Soil, Blight, Concrete, Temple };

        public const int Up = TerrainAutotile.Up, Right = TerrainAutotile.Right, Down = TerrainAutotile.Down, Left = TerrainAutotile.Left;

        public static bool IsConcrete(string material) { return material == Concrete; }
        /// <summary>Material de blocos (concreto ou pedra do templo): topo e miolo, sem terra nem rocha, sem troncos.</summary>
        public static bool IsBlock(string material) { return material == Concrete || material == Temple; }

        // ------------------------------------------------------------ chão

        /// <summary>
        /// Faixa pela profundidade: topo (0), terra (1–2), transição (3) e rocha (4+). Concreto: topo (laje com musgo)
        /// ou miolo (o bloco).
        /// </summary>
        public static string Band(string material, int depth)
        {
            if (depth == 0) return "topo";
            if (IsBlock(material)) return "miolo";
            if (depth <= 2) return "terra";
            return depth == 3 ? "transicao" : "rocha";
        }

        public const int StrataPhases = TerrainAutotile.StrataPhases;

        static bool Phased(string band) { return band == "rocha" || band == "transicao"; }

        public static int VariantCount(string material, string band)
        {
            if (band == "topo") return IsConcrete(material) ? 2 : 3;   // templo: 3 (grama lisa, com tufo, caindo)
            if (Phased(band)) return StrataPhases;
            return 3; // terra: lisa, com seixos, com raiz; concreto: liso, rachado, com ferrugem
        }

        public static string TileName(string material, string band, int mask, int variant)
        {
            return "tile_" + material + "_" + band + "_" + mask.ToString("x") + "_" + variant;
        }

        /// <summary>Nome do tile para a célula (x, y) de chão com este material.</summary>
        /// <summary>
        /// Quantos tiles sólidos há em cima, até 4 (0 = superfície). Um tronco em cima não conta como chão: embaixo dele
        /// o chão continua terra (o tronco entra no chão), em vez de virar uma coluna de rocha funda no meio do piso.
        /// </summary>
        public static int Depth(TerrainAutotile.SolidAt solid, int x, int y)
        {
            int d = 0;
            while (d < 4 && solid(x, y + d + 1))
            {
                if (IsTrunk(solid, x, y + d + 1)) return d + 1;
                d++;
            }
            return d;
        }

        public static string TileFor(string material, TerrainAutotile.SolidAt solid, int x, int y)
        {
            string band = Band(material, Depth(solid, x, y));
            int mask = TerrainAutotile.Mask(solid, x, y);
            if (band != "topo") mask &= ~Up;
            if (band != "topo" && !IsBlock(material) && IsTrunk(solid, x, y))
                return TrunkTileName(material, mask & (Left | Right), (int)(TerrainAutotile.Hash(x, y / 3, 120) % (uint)TrunkVariants));
            return TileName(material, band, mask, Variant(material, band, x, y));
        }

        // ------------------------------------------------------------ troncos (colunas finas de chão)

        public const int TrunkVariants = 3;

        /// <summary>
        /// Coluna de chão de 1 ou 2 tiles de largura e pelo menos 3 de altura (as do desenho do usuário, as paredes entre
        /// duas salas): é um tronco, não um paredão de rocha. Casca com veio vertical, luar à esquerda.
        /// </summary>
        public static bool IsTrunk(TerrainAutotile.SolidAt s, int x, int y)
        {
            bool l1 = s(x - 1, y), r1 = s(x + 1, y);
            bool narrow = (!l1 && !r1) || (!l1 && r1 && !s(x + 2, y)) || (!r1 && l1 && !s(x - 2, y));
            if (!narrow) return false;
            bool up = s(x, y + 1), down = s(x, y - 1);
            return (up && down) || (up && s(x, y + 2)) || (down && s(x, y - 2));
        }

        public static string TrunkTileName(string material, int sideMask, int variant)
        {
            return "tile_" + (material == Blight ? Blight : Soil) + "_tronco_" + sideMask.ToString("x") + "_" + variant;
        }

        /// <summary>Sorteio fixo pela posição, sem a mesma variação 3 vezes seguidas na fileira (como na Fortaleza).</summary>
        public static int Variant(string material, string band, int x, int y)
        {
            if (Phased(band)) return TerrainAutotile.Mod(x, StrataPhases);
            int count = VariantCount(material, band);
            if (count <= 1) return 0;
            // miolo e terra (pedido do usuário, 14/11/2026: "padrões se repetindo muito"): a lisa é a maioria e as marcadas
            // (rachadura, ferrugem, raiz, seixos) ficam raras e espalhadas, sem grade visível
            if (band == "miolo" || band == "terra")
            {
                uint r = TerrainAutotile.Hash(x, y, 131) % 100u;
                if (r < 70u) return 0;
                return 1 + (int)(TerrainAutotile.Hash(x, y, 132) % (uint)(count - 1));
            }
            int pairStart = x - TerrainAutotile.Mod(x, 2);
            int first = (int)(TerrainAutotile.Hash(pairStart, y, 117) % (uint)count);
            if (x == pairStart) return first;
            int skip = (int)(TerrainAutotile.Hash(x, y, 118) % (uint)(count - 1));
            return (first + 1 + skip) % count;
        }

        /// <summary>Todos os nomes de tile de chão que o gerador precisa desenhar para o material.</summary>
        public static List<string> AllTileNames(string material)
        {
            var names = new List<string>();
            string[] bands = IsBlock(material) ? new[] { "topo", "miolo" } : new[] { "topo", "terra", "transicao", "rocha" };
            foreach (var band in bands)
                for (int mask = 0; mask < 16; mask++)
                {
                    bool up = (mask & Up) != 0;
                    if ((band == "topo") != up) continue;
                    for (int v = 0; v < VariantCount(material, band); v++) names.Add(TileName(material, band, mask, v));
                }
            return names;
        }

        // ------------------------------------------------------------ vinha (parede escalável)

        public const int ClimbVariants = 3;

        /// <summary>
        /// Vinha da floresta: casca de tronco (ou concreto, nas ruínas) coberta pela hera de escalar (#4f5a2e / #8a9a4e, a
        /// linguagem reservada do gate do Ladino, a mesma da Fortaleza). Lado: a face exposta (onde o gato agarra).
        /// </summary>
        public static string ClimbTileName(string material, int sideMask, int variant)
        {
            return "tile_" + ClimbFamily(material) + "_escalavel_" + sideMask.ToString("x") + "_" + variant;
        }

        /// <summary>Solo e praga usam a mesma vinha (tronco); o concreto tem a sua.</summary>
        public static string ClimbFamily(string material) { return IsBlock(material) ? Concrete : Soil; }

        public static string ClimbTileFor(string material, TerrainAutotile.SolidAt solid, int x, int y)
        {
            int side = TerrainAutotile.Mask(solid, x, y) & (Left | Right);
            int v = (int)(TerrainAutotile.Hash(x, y / 2, 130) % (uint)ClimbVariants);
            return ClimbTileName(material, side, v);
        }

        // ------------------------------------------------------------ massa da floresta e paredes do fundo

        public const int MassVariants = 4;

        /// <summary>
        /// Massa da floresta: o que fica fora das salas (terra funda, raízes e troncos emaranhados, quase preto), no tilemap
        /// do fundo, sem colisão. Assim as salas leem como clareiras abertas na mata, e não como caixas no céu.
        /// </summary>
        public static string MassTileName(string material, int variant)
        {
            return "tile_" + (material == Blight ? Blight : Soil) + "_massa_" + variant;
        }

        public static string MassTileFor(string material, int x, int y)
        {
            // a lisa é a maioria; as com raiz/pedra ficam espalhadas (sem padrão repetido)
            if (TerrainAutotile.Hash(x, y, 140) % 100u < 55u) return MassTileName(material, 0);
            return MassTileName(material, 1 + (int)(TerrainAutotile.Hash(x, y, 141) % (uint)(MassVariants - 1)));
        }

        /// <summary>
        /// Até esta profundidade (tiles sólidos em cima, contando a massa como sólida), a massa logo abaixo de um chão
        /// continua o chão (terra e transição); mais fundo, vira massa. O chão da sala parece ter espessura.
        /// </summary>
        public const int MassGroundDepth = 3;

        public const int BackVariants = 2;

        /// <summary>Parede do fundo DENTRO de uma sala (posto abandonado, fachada da cabana): tábuas ou concreto escuro.</summary>
        public static string BackTileName(string material, int variant)
        {
            return "tile_" + (IsConcrete(material) ? Concrete : material == Temple ? Temple : Soil) + "_fundo_" + variant;
        }

        public static string BackTileFor(string material, int x, int y)
        {
            return BackTileName(material, (int)(TerrainAutotile.Hash(x, y, 150) % (uint)BackVariants));
        }

        /// <summary>Todos os nomes de vinha, massa e fundo que o gerador precisa desenhar.</summary>
        public static List<string> AllExtraNames()
        {
            var names = new List<string>();
            foreach (var fam in new[] { Soil, Concrete })
            {
                foreach (int side in new[] { 0, Left, Right, Left | Right })
                    for (int v = 0; v < ClimbVariants; v++) names.Add(ClimbTileName(fam, side, v));
                for (int v = 0; v < BackVariants; v++) names.Add(BackTileName(fam, v));
            }
            for (int v = 0; v < BackVariants; v++) names.Add(BackTileName(Temple, v));
            foreach (var m in new[] { Soil, Blight })
            {
                for (int v = 0; v < MassVariants; v++) names.Add(MassTileName(m, v));
                foreach (int side in new[] { Left, Right, Left | Right })
                    for (int v = 0; v < TrunkVariants; v++) names.Add(TrunkTileName(m, side, v));
                foreach (var kind in TerrainAutotile.SlopePieceKinds)
                    foreach (bool right in new[] { true, false }) names.Add(SlopePieceName(m, kind, right));
            }
            return names;
        }

        // ------------------------------------------------------------ rampas

        /// <summary>Pedaço de rampa: 11 (45°), 12a/12b (1:2, metade baixa/alta), _r/_l (sobe para a direita/esquerda).</summary>
        public static string SlopePieceName(string material, string kind, bool risesRight)
        {
            return "env_" + (material == Blight ? "flo_praga" : "flo") + "_ter_rampa_" + kind + (risesRight ? "_r" : "_l");
        }

        /// <summary>
        /// Pedaços de desenho de uma rampa de solo ou praga (só 1:1 e 1:2; outra proporção ou o concreto devolve vazio =
        /// triângulo provisório). Um pedaço por coluna na diagonal; embaixo dela, os tiles de terra como o chão.
        /// </summary>
        public static List<TerrainAutotile.Piece> SlopePieces(string material, int x0, int y0, int w, int h, bool risesRight)
        {
            var list = new List<TerrainAutotile.Piece>();
            if (IsBlock(material) || h <= 0 || (w != h && w != 2 * h)) return list;
            bool half = w == 2 * h;
            for (int c = 0; c < w; c++)
            {
                int col = risesRight ? c : w - 1 - c;
                int diagonalRow = half ? c / 2 : c;
                string kind = half ? (c % 2 == 0 ? "12a" : "12b") : "11";
                int wx = x0 + col;
                list.Add(new TerrainAutotile.Piece { X = wx, Y = y0 + diagonalRow, Sprite = SlopePieceName(material, kind, risesRight), Solid = kind != "12a" });
                for (int r = 0; r < diagonalRow; r++)
                {
                    string band = Band(material, diagonalRow - r);
                    list.Add(new TerrainAutotile.Piece { X = wx, Y = y0 + r, Sprite = TileName(material, band, 0, Variant(material, band, wx, y0 + r)), Solid = true });
                }
            }
            return list;
        }

        // ------------------------------------------------------------ detalhes

        public const int FernVariants = 4, FungusVariants = 3, SproutVariants = 3, MossVariants = 3, DebrisVariants = 3;
        public const int HangingRootVariants = 3, RebarVariants = 2;

        public static string FernName(string material, int v) { return "env_flo_ter_samambaia" + (material == Blight ? "_praga_" : "_") + v; }
        /// <summary>Samambaias têm 3 desenhos: parada e curvada para cada lado (BendOnPass, como o tufo da Fortaleza).</summary>
        public static string FernBentName(string baseName, bool right) { return baseName + (right ? "_r" : "_l"); }
        public static string FungusName(int v) { return "env_flo_veg_fungo_" + v; }
        public static string SproutName(int v) { return "env_flo_praga_broto_" + v; }
        public static string MossName(int v) { return "env_pri_ter_musgo_" + v; }
        public static string DebrisName(int v) { return "env_pri_ter_entulho_" + v; }
        public static string HangingRootName(string material, int v) { return "env_flo_veg_raiz_pendente" + (material == Blight ? "_praga_" : "_") + v; }
        public static string RebarName(int v) { return "env_pri_ter_vergalhao_" + v; }
        public const int GrassVariants = 3, FlowerVariants = 4;
        public static string GrassName(int v) { return "env_pri_tem_capim_" + v; }
        public static string FlowerName(int v) { return "env_pri_tem_flor_" + v; }

        public static bool IsFern(string sprite) { return sprite.StartsWith("env_flo_ter_samambaia"); }
        public static bool IsGlowing(string sprite) { return sprite.StartsWith("env_flo_veg_fungo_") || sprite.StartsWith("env_flo_praga_broto_"); }

        /// <summary>
        /// Detalhes sobre uma célula de superfície (y = célula; a superfície fica em y + 1). <paramref name="blight"/>: 0 sem
        /// praga, 1 praga chegando (brotos raros), 2 tomado (o material já é o da praga). Sorteio fixo pela posição.
        /// </summary>
        public static List<TerrainAutotile.Detail> TopDetails(string material, int blight, int x, int y)
        {
            var list = new List<TerrainAutotile.Detail>();
            const float px = 1f / 16f;
            if (IsConcrete(material))
            {
                if (TerrainAutotile.Hash(x, y, 160) % 100 < 35)
                    list.Add(new TerrainAutotile.Detail { Sprite = MossName((int)(TerrainAutotile.Hash(x, y, 161) % MossVariants)), X = x + 0.5f + ((int)(TerrainAutotile.Hash(x, y, 162) % 7) - 3) * px, Y = y + 1f });
                if (TerrainAutotile.Hash(x, y, 163) % 100 < 22)
                    list.Add(new TerrainAutotile.Detail { Sprite = DebrisName((int)(TerrainAutotile.Hash(x, y, 164) % DebrisVariants)), X = x + ((int)(TerrainAutotile.Hash(x, y, 165) % 10) + 3) * px, Y = y + 1f });
                return list;
            }
            if (material == Temple)
            {
                // gramado do templo: tufos de capim e flores rosa e brancas (sem amarelo: é a cor da rachadura)
                if (TerrainAutotile.Hash(x, y, 190) % 100 < 45)
                    list.Add(new TerrainAutotile.Detail { Sprite = GrassName((int)(TerrainAutotile.Hash(x, y, 191) % GrassVariants)), X = x + 0.5f + ((int)(TerrainAutotile.Hash(x, y, 192) % 7) - 3) * px, Y = y + 1f });
                if (TerrainAutotile.Hash(x, y, 193) % 100 < 38)
                    list.Add(new TerrainAutotile.Detail { Sprite = FlowerName((int)(TerrainAutotile.Hash(x, y, 194) % FlowerVariants)), X = x + ((int)(TerrainAutotile.Hash(x, y, 195) % 10) + 3) * px, Y = y + 1f });
                return list;
            }
            int fernChance = material == Blight ? 40 : 58;
            if (TerrainAutotile.Hash(x, y, 166) % 100 < fernChance)
                list.Add(new TerrainAutotile.Detail { Sprite = FernName(material, (int)(TerrainAutotile.Hash(x, y, 167) % FernVariants)), X = x + 0.5f + ((int)(TerrainAutotile.Hash(x, y, 168) % 7) - 3) * px, Y = y + 1f });
            // fungos bioluminescentes (azul do fungo, nunca o turquesa do pogo): raros, mais onde a mata é saudável
            if (material == Soil && TerrainAutotile.Hash(x, y, 169) % 100 < 6)
                list.Add(new TerrainAutotile.Detail { Sprite = FungusName((int)(TerrainAutotile.Hash(x, y, 170) % FungusVariants)), X = x + ((int)(TerrainAutotile.Hash(x, y, 171) % 10) + 3) * px, Y = y + 1f });
            // brotos da praga: chegando (1) raros; tomado (2) frequentes
            int sprout = blight >= 2 ? 12 : blight == 1 ? 4 : 0;
            if (TerrainAutotile.Hash(x, y, 172) % 100 < sprout)
                list.Add(new TerrainAutotile.Detail { Sprite = SproutName((int)(TerrainAutotile.Hash(x, y, 173) % SproutVariants)), X = x + ((int)(TerrainAutotile.Hash(x, y, 174) % 10) + 3) * px, Y = y + 1f });
            return list;
        }

        /// <summary>
        /// Raízes pendendo do teto (solo e praga) ou vergalhão (concreto), numa parte das células de teto. (x, y) = célula
        /// com o lado de baixo exposto; o topo do sprite fica no teto. Altura dos desenhos: 10 + 4·v px.
        /// </summary>
        public static List<TerrainAutotile.Detail> CeilingDetails(string material, int x, int y)
        {
            var list = new List<TerrainAutotile.Detail>();
            if (IsConcrete(material))
            {
                if (TerrainAutotile.Hash(x, y, 180) % 100 >= 14) return list;
                int r = (int)(TerrainAutotile.Hash(x, y, 181) % RebarVariants);
                list.Add(new TerrainAutotile.Detail { Sprite = RebarName(r), X = x + ((int)(TerrainAutotile.Hash(x, y, 182) % 9) + 4) / 16f, Y = y - RebarHeight(r) / 16f });
                return list;
            }
            if (TerrainAutotile.Hash(x, y, 183) % 100 >= 22) return list;
            int v = (int)(TerrainAutotile.Hash(x, y, 184) % HangingRootVariants);
            list.Add(new TerrainAutotile.Detail { Sprite = HangingRootName(material, v), X = x + ((int)(TerrainAutotile.Hash(x, y, 185) % 9) + 4) / 16f, Y = y - HangingRootHeight(v) / 16f });
            return list;
        }

        public static int HangingRootHeight(int v) { return 10 + 4 * v; }
        public static int RebarHeight(int v) { return 6 + 3 * v; }

        /// <summary>Todos os sprites de detalhe que o gerador precisa desenhar (com as samambaias curvadas).</summary>
        public static List<string> AllDetailNames()
        {
            var names = new List<string>();
            foreach (var m in new[] { Soil, Blight })
            {
                for (int v = 0; v < FernVariants; v++)
                {
                    string f = FernName(m, v);
                    names.Add(f); names.Add(FernBentName(f, false)); names.Add(FernBentName(f, true));
                }
                for (int v = 0; v < HangingRootVariants; v++) names.Add(HangingRootName(m, v));
            }
            for (int v = 0; v < FungusVariants; v++) names.Add(FungusName(v));
            for (int v = 0; v < SproutVariants; v++) names.Add(SproutName(v));
            for (int v = 0; v < MossVariants; v++) names.Add(MossName(v));
            for (int v = 0; v < DebrisVariants; v++) names.Add(DebrisName(v));
            for (int v = 0; v < RebarVariants; v++) names.Add(RebarName(v));
            for (int v = 0; v < GrassVariants; v++) names.Add(GrassName(v));
            for (int v = 0; v < FlowerVariants; v++) names.Add(FlowerName(v));
            return names;
        }
    }
}
