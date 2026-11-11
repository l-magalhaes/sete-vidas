using System.Collections.Generic;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Escolha do tile de terreno natural de cada célula (plano de assets, Etapa 2), pelos vizinhos e pela
    /// profundidade. Regra única usada por três lados: o Unity (TerrainPainter), o gerador de sprites
    /// (Tools/EnvironmentSprites, que desenha todos os nomes listados aqui) e a prévia da cena (Tools/LevelPreview).
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell).
    /// Nome do tile: tile_&lt;material&gt;_&lt;faixa&gt;_&lt;máscara hex&gt;_&lt;variação&gt;.
    /// Faixa: "topo" (céu em cima), "terra" (1 a 2 tiles abaixo), "transicao" (3), "rocha" (mais fundo).
    /// Máscara: lados expostos ao ar (1 = cima, 2 = direita, 4 = baixo, 8 = esquerda).
    /// A colisão não muda: todo tile de chão continua sendo um quadrado cheio (Tile.ColliderType.Grid).
    /// </summary>
    public static class TerrainAutotile
    {
        /// <summary>Solo do exterior: grama, terra e rocha conforme a profundidade.</summary>
        public const string Soil = "ext_solo";
        /// <summary>Rocha natural (pedras do tutorial, paredes de rocha): só rocha, topo iluminado.</summary>
        public const string Rock = "ext_rocha";
        /// <summary>Chão de terra batida do pátio (topo sem grama; embaixo, o mesmo solo do exterior).</summary>
        public const string PackedEarth = "ext_terra_batida";

        // Arquitetura (Etapa 3): uma pedra por época, para a idade da parede contar a história (plano, 1.1)
        /// <summary>Fundação de Golden (ano 423): blocos grandes 32×16, escuros, juntas largas.</summary>
        public const string Foundation = "cid_fundacao";
        /// <summary>Ampliação de Soluki: tijolo militar 16×8.</summary>
        public const string Soluki = "cid_soluki";
        /// <summary>Reforma recente: tijolos novos e claros com argamassa fresca, misturados aos antigos.</summary>
        public const string Reform = "cid_reforma";
        /// <summary>Abandono: tijolos faltando, musgo e rachaduras escuras (nunca douradas).</summary>
        public const string Ruin = "cid_abandono";
        /// <summary>Madeira (arquibancada): tábuas velhas e algumas novas.</summary>
        public const string Wood = "sol_madeira";

        public static readonly string[] Materials = { Soil, Rock, PackedEarth, Foundation, Soluki, Reform, Ruin, Wood };
        public static readonly string[] Masonry = { Foundation, Soluki, Reform, Ruin, Wood };

        public static bool IsMasonry(string material) { return System.Array.IndexOf(Masonry, material) >= 0; }

        public const int Up = 1, Right = 2, Down = 4, Left = 8;

        public delegate bool SolidAt(int x, int y);

        // ------------------------------------------------------------ tiles

        /// <summary>
        /// Faixa pela profundidade: topo (0), terra (1–2), transição (3: terra entrando na rocha com borda
        /// irregular, para não haver linha reta) e rocha (4+). No material rocha: topo e rocha.
        /// </summary>
        public static string Band(string material, int depth)
        {
            if (depth == 0) return "topo";
            if (IsMasonry(material)) return "miolo"; // alvenaria: topo (com a pedra de cobertura) ou miolo
            if (material == Rock) return "rocha";
            if (depth <= 2) return "terra";
            return depth == 3 ? "transicao" : "rocha";
        }

        /// <summary>Estratos: 1 px de queda a cada 4 px e uma linha a cada 16 px; repetem a cada 64 px (4 tiles).</summary>
        public const int StrataPhases = 4;

        static bool Phased(string band) { return band == "rocha" || band == "transicao"; }

        public static int VariantCount(string material, string band)
        {
            if (material == Foundation) return 4; // (metade esquerda/direita do bloco de 32 px) × 2 desgastes
            if (material == Ruin) return 4;       // limpa, um tijolo faltando, rachadura, musgo pesado
            if (IsMasonry(material)) return 2;     // 2 desgastes
            if (band == "topo") return material == Soil || material == PackedEarth ? 3 : 2;
            if (Phased(band)) return StrataPhases; // fase dos estratos pela coluna (x mod 4)
            return 3; // terra: lisa, com seixos, com raiz
        }

        /// <summary>Quantos tiles sólidos há em cima, até 4 (0 = superfície).</summary>
        public static int Depth(SolidAt solid, int x, int y)
        {
            int d = 0;
            while (d < 4 && solid(x, y + d + 1)) d++;
            return d;
        }

        public static int Mask(SolidAt solid, int x, int y)
        {
            int m = 0;
            if (!solid(x, y + 1)) m |= Up;
            if (!solid(x + 1, y)) m |= Right;
            if (!solid(x, y - 1)) m |= Down;
            if (!solid(x - 1, y)) m |= Left;
            return m;
        }

        public static string TileName(string material, string band, int mask, int variant)
        {
            // terra batida só tem o topo próprio; embaixo é o mesmo solo do exterior
            if (material == PackedEarth && band != "topo") material = Soil;
            return "tile_" + material + "_" + band + "_" + mask.ToString("x") + "_" + variant;
        }

        public delegate string MaterialAt(int x, int y);

        /// <summary>
        /// Profundidade contando só células do mesmo material. Embaixo de outro material (ex.: solo sob uma pedra)
        /// conta como 1 a mais: vira terra, e não a rocha funda que formava uma coluna embaixo das pedras.
        /// </summary>
        public static int Depth(SolidAt solid, MaterialAt materialAt, string material, int x, int y)
        {
            int d = 0;
            while (d < 4 && solid(x, y + d + 1))
            {
                if (materialAt(x, y + d + 1) != material) return d + 1;
                d++;
            }
            return d;
        }

        /// <summary>Nome do tile para a célula (x, y) de chão com este material.</summary>
        public static string TileFor(string material, SolidAt solid, MaterialAt materialAt, int x, int y)
        {
            int depth = materialAt == null ? Depth(solid, x, y) : Depth(solid, materialAt, material, x, y);
            string band = Band(material, depth);
            int mask = Mask(solid, x, y);
            if (band != "topo") mask &= ~Up;
            // alvenaria longe de qualquer espaço aberto: interior escuro (as bordas, onde se pisa e encosta, se destacam)
            // em degraus, para não haver corte reto: cor cheia até 2 tiles do ar, meio-tom no 3º, escuro do 4º em diante
            if (band == "miolo" && material != Wood && DeepInside(solid, x, y, InteriorDistance)) // madeira: estrutura fina, sem interior escuro
                return TileName(material, DeepInside(solid, x, y, InteriorDistance + 1) ? "interno" : "meio", 0, Variant(material, "miolo", x, y));
            return TileName(material, band, mask, Variant(material, band, x, y));
        }

        /// <summary>Até esta distância do ar (em passos), a alvenaria fica com a cor cheia.</summary>
        public const int InteriorDistance = 2;

        /// <summary>Todas as células a até <paramref name="distance"/> passos (vizinhança de 4) são sólidas.</summary>
        public static bool DeepInside(SolidAt solid, int x, int y, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
                for (int dy = -distance; dy <= distance; dy++)
                    if (System.Math.Abs(dx) + System.Math.Abs(dy) <= distance && !solid(x + dx, y + dy)) return false;
            return true;
        }

        /// <summary>
        /// Variação: na rocha, a fase dos estratos (x par/ímpar) para as linhas continuarem de um tile para o outro;
        /// no resto, sorteio fixo pela posição, sem repetir a mesma 3 vezes seguidas na fileira.
        /// </summary>
        public static int Variant(string material, string band, int x, int y)
        {
            // fundação: blocos de 32 px em fiadas alternadas (deslocadas 16 px); a paridade diz qual metade do bloco
            if (material == Foundation) return Mod(x + y, 2) * 2 + (int)(Hash(x, y, 19) % 2u);
            if (material == PackedEarth && band != "topo") return Variant(Soil, band, x, y);
            if (Phased(band)) return Mod(x, StrataPhases);
            int count = VariantCount(material, band);
            if (count <= 1) return 0;
            // Colunas em pares (2k, 2k+1): a primeira sorteia, a segunda sorteia entre as OUTRAS. Quaisquer 3 colunas
            // seguidas contêm um par inteiro, então nunca há 3 iguais seguidas (e a regra vale olhando uma célula só).
            int pairStart = x - Mod(x, 2);
            int first = (int)(Hash(pairStart, y, 17) % (uint)count);
            if (x == pairStart) return first;
            int skip = (int)(Hash(x, y, 18) % (uint)(count - 1));
            return (first + 1 + skip) % count;
        }

        /// <summary>Todos os nomes de tile que o gerador precisa desenhar para o material.</summary>
        public static List<string> AllTileNames(string material)
        {
            var names = new List<string>();
            if (IsMasonry(material))
                for (int v = 0; v < VariantCount(material, "miolo"); v++)
                {
                    names.Add(TileName(material, "meio", 0, v));
                    names.Add(TileName(material, "interno", 0, v));
                }
            string[] bands = IsMasonry(material) ? new[] { "topo", "miolo" }
                           : material == PackedEarth ? new[] { "topo" }
                           : new[] { "topo", "terra", "transicao", "rocha" };
            foreach (var band in bands)
            {
                if (material == Rock && (band == "terra" || band == "transicao")) continue;
                for (int mask = 0; mask < 16; mask++)
                {
                    bool up = (mask & Up) != 0;
                    if ((band == "topo") != up) continue;
                    for (int v = 0; v < VariantCount(material, band); v++) names.Add(TileName(material, band, mask, v));
                }
            }
            return names;
        }

        // -------------------------------------- parede do fundo e paredes escaláveis

        /// <summary>Parede do fundo (tilemap Background, sem colisão): a mesma alvenaria, mais escura e sem bordas.</summary>
        public static string BackTileName(string material, int variant) { return "tile_" + material + "_fundo_" + variant; }

        public static string BackTileFor(string material, int x, int y)
        {
            return IsMasonry(material) ? BackTileName(material, Variant(material, "miolo", x, y)) : "";
        }

        /// <summary>
        /// Parede escalável por época: a pedra da época com as ranhuras de apoio e a hera seca (#4f5a2e / #8a9a4e),
        /// a linguagem reservada do gate do Ladino. Lados: só esquerda/direita importam (a face onde o gato agarra).
        /// </summary>
        public static string ClimbTileName(string material, int sideMask, int variant)
        {
            return "tile_" + material + "_escalavel_" + sideMask.ToString("x") + "_" + variant;
        }

        public static string ClimbTileFor(string material, SolidAt solid, int x, int y)
        {
            if (!IsMasonry(material)) return "";
            return ClimbTileName(material, Mask(solid, x, y) & (Left | Right), Variant(material, "miolo", x, y));
        }

        /// <summary>
        /// Sem rachadura: rachadura (grande, escura, de cima a baixo) é a linguagem da parede quebrável. Sem janela tapada
        /// nem nicho (pareciam janelas pequenas competindo com as janelas de verdade da Etapa 6B, que mostram o céu).
        /// </summary>
        public static readonly string[] WallEvents = { "remendo", "mancha" };
        public static string WallEventName(string evento) { return "env_cid_parede_" + evento; }

        /// <summary>Parede quebrável 16×64 na pedra da época: estado "intacta", "danificada" ou "entulho".</summary>
        public static string BreakableWallName(string material, string state)
        {
            string era = material.StartsWith("cid_") ? material.Substring(4) : material;
            return "env_cid_parede_quebravel_" + era + "_" + state;
        }

        /// <summary>
        /// "Acontecimento" na parede do fundo (plano, 4.5: um a cada 8–12 tiles): janela tapada, nicho, remendo de
        /// outra pedra, mancha de água ou rachadura escura. Só em salas com pelo menos 4 de altura livre, na altura de
        /// 2 a 3 tiles acima do chão, e espaçados (no máximo um a cada 3 colunas, ~1 em 10). (x, y) = célula de fundo livre.
        /// </summary>
        public static Detail WallEvent(SolidAt solid, int x, int y)
        {
            if (solid(x, y) || Mod(x, 3) != 0) return null;
            bool floor3 = solid(x, y - 3) && !solid(x, y - 1) && !solid(x, y - 2);
            if (!floor3 || solid(x, y + 1)) return null;
            if (Hash(x, y, 40) % 100 >= 30) return null; // 30% das colunas candidatas (1 a cada 3) ≈ 1 em 10
            string ev = WallEvents[(int)(Hash(x, y, 41) % (uint)WallEvents.Length)];
            return new Detail { Sprite = WallEventName(ev), X = x + 0.5f, Y = y - 1f };
        }

        public static List<string> AllBackAndClimbNames(string material)
        {
            var names = new List<string>();
            if (!IsMasonry(material)) return names;
            int count = VariantCount(material, "miolo");
            for (int v = 0; v < count; v++)
            {
                names.Add(BackTileName(material, v));
                foreach (int side in new[] { 0, Left, Right, Left | Right }) names.Add(ClimbTileName(material, side, v));
            }
            return names;
        }

        /// <summary>
        /// Gárgula no topo de cada vinha (plano, 4.4: a água da chuva sai por ela e alimenta a vinha). Fica na face da
        /// parede, logo acima da última célula escalável, apontando para fora (para o lado do ar).
        /// </summary>
        public static Detail Gargoyle(int x, int topY, bool faceLeft, string material)
        {
            const float px = 1f / 16f;
            bool wood = material == Wood; // na madeira, uma calha de tábuas no lugar da cabeça de pedra
            // a mísula (lado preso) encosta 1 px dentro da parede; o resto sai para o ar
            float half = (wood ? 14f : 24f) * 0.5f * px;
            return new Detail
            {
                Sprite = (wood ? "env_sol_arq_calha_" : "env_sol_arq_gargula_") + (faceLeft ? "l" : "r"),
                X = faceLeft ? x - half + px : x + 1f + half - px,
                Y = topY + (wood ? 6f : 2f) * px,
            };
        }

        public const int PuddleFrames = 8;
        public const float PuddleFps = 8f;
        public static string PuddleFrameName(int frame) { return "env_sol_arq_poca_" + frame; }

        public const int DripFrames = 14;
        public const float DripFps = 12f;
        public static string DripFrameName(int frame) { return "env_sol_arq_gota_" + frame; }

        /// <summary>
        /// Gota animada que cai da boca da gárgula (ou da ponta da calha) ao longo da vinha: sprite 7×48, com o topo na
        /// base da gárgula e o meio na coluna do fio d'água. Use a mesma chamada de <see cref="Gargoyle"/>.
        /// </summary>
        public static Detail GargoyleDrip(int x, int topY, bool faceLeft, string material)
        {
            const float px = 1f / 16f;
            var g = Gargoyle(x, topY, faceLeft, material);
            bool wood = material == Wood;
            float stream = wood ? 4.5f : 7f;                     // distância do fio d'água ao meio do sprite (px)
            return new Detail
            {
                Sprite = DripFrameName(0),
                X = g.X + (faceLeft ? -stream : stream) * px,
                Y = g.Y - 48f * px,
            };
        }

        /// <summary>Atraso do ciclo da gota (s), pela posição: as gárgulas não pingam em sincronia.</summary>
        public static float DripOffset(int x, int y) { return (Hash(x, y, 50) % 1000) / 1000f * DripFrames / DripFps; }

        // ------------------------------------------------------------ rampas

        public class Piece
        {
            /// <summary>Célula (em tiles, no mundo) e nome do sprite (16×16, pivot central).</summary>
            public int X, Y; public string Sprite;
            /// <summary>Cobre metade ou mais da célula (conta como sólida para o tile de baixo).</summary>
            public bool Solid;
        }

        /// <summary>Nome do pedaço de rampa: 11 (45°), 12a/12b (1:2, metade baixa/alta), _r/_l (sobe para a direita/esquerda).</summary>
        public static string SlopePieceName(string kind, bool risesRight)
        {
            return "env_ext_ter_rampa_" + kind + (risesRight ? "_r" : "_l");
        }

        public static readonly string[] SlopePieceKinds = { "11", "12a", "12b" };

        /// <summary>
        /// Pedaços de desenho de uma rampa de solo (só 1:1 e 1:2; outra proporção devolve vazio = placeholder).
        /// Diagonal: um pedaço por coluna; embaixo dela, tiles de terra/rocha como o chão.
        /// </summary>
        public static List<Piece> SlopePieces(string material, int x0, int y0, int w, int h, bool risesRight)
        {
            var list = new List<Piece>();
            if (material != Soil || h <= 0 || (w != h && w != 2 * h)) return list;
            bool half = w == 2 * h;
            for (int c = 0; c < w; c++)
            {
                int col = risesRight ? c : w - 1 - c; // c conta a partir do lado baixo
                int diagonalRow = half ? c / 2 : c;
                string kind = half ? (c % 2 == 0 ? "12a" : "12b") : "11";
                int wx = x0 + col;
                list.Add(new Piece { X = wx, Y = y0 + diagonalRow, Sprite = SlopePieceName(kind, risesRight), Solid = kind != "12a" });
                for (int r = 0; r < diagonalRow; r++)
                {
                    int depth = diagonalRow - r;
                    string band = Band(material, depth);
                    list.Add(new Piece { X = wx, Y = y0 + r, Sprite = TileName(material, band, 0, Variant(material, band, wx, y0 + r)), Solid = true });
                }
            }
            return list;
        }

        // ------------------------------------------------- detalhes na superfície

        public class Detail
        {
            /// <summary>Sprite e posição do meio da BASE do sprite, em tiles (no mundo).</summary>
            public string Sprite; public float X, Y;
        }

        public const int TuftVariants = 4, PebbleVariants = 4;
        public static string TuftName(int v) { return "env_ext_ter_tufo_" + v; }
        public static string PebbleName(int v) { return "env_ext_roc_seixo_" + v; }

        /// <summary>
        /// Tufos de grama e seixos sobre uma célula de superfície (y = célula; a superfície fica em y + 1).
        /// Sorteio fixo pela posição. Os tufos sobem 2 a 5 px acima do chão: quebram a linha reta sem mudar a colisão.
        /// </summary>
        public static List<Detail> TopDetails(string material, int x, int y)
        {
            var list = new List<Detail>();
            const float px = 1f / 16f;
            // alvenaria cuidada: chão limpo; só o abandono tem entulho (seixos)
            if (IsMasonry(material) && material != Ruin) return list;
            int tuftChance = material == Soil ? 62 : material == PackedEarth ? 12 : 0; // no pátio o mato só invade as bordas
            if (Hash(x, y, 3) % 100 < tuftChance)
            {
                int v = (int)(Hash(x, y, 4) % TuftVariants);
                int jitter = (int)(Hash(x, y, 5) % 7) - 3;
                list.Add(new Detail { Sprite = TuftName(v), X = x + 0.5f + jitter * px, Y = y + 1f });
            }
            // flores silvestres raras no solo (sem amarelo: não parecer moeda)
            if (material == Soil && Hash(x, y, 12) % 100 < 5)
            {
                int fv = (int)(Hash(x, y, 13) % FlowerVariants);
                list.Add(new Detail { Sprite = FlowerName(fv), X = x + ((int)(Hash(x, y, 14) % 10) + 3) * px, Y = y + 1f });
            }
            if (Hash(x, y, 6) % 100 < (material == Rock || material == Ruin ? 45 : 30))
            {
                int v = (int)(Hash(x, y, 7) % PebbleVariants);
                int offset = (int)(Hash(x, y, 8) % 12) + 2;
                list.Add(new Detail { Sprite = PebbleName(v), X = x + offset * px, Y = y + 1f });
            }
            return list;
        }

        public const int FlowerVariants = 3, RootVariants = 3;
        public static string FlowerName(int v) { return "env_ext_veg_flor_" + v; }
        public static string RootName(int v) { return "env_ext_veg_raiz_" + v; }
        /// <summary>Tufos têm 3 desenhos: parado e curvado para cada lado (BendOnPass).</summary>
        public static string TuftBentName(int v, bool right) { return TuftName(v) + (right ? "_r" : "_l"); }

        /// <summary>
        /// Raízes pendendo do teto na parte abandonada (a mata entra por cima: plano, 1.2), em ~18% das células de
        /// teto do abandono. (x, y) = célula com o lado de baixo exposto; o topo da raiz fica no teto.
        /// </summary>
        public static List<Detail> CeilingDetails(string material, int x, int y)
        {
            var list = new List<Detail>();
            if (material != Ruin || Hash(x, y, 60) % 100 >= 18) return list;
            int v = (int)(Hash(x, y, 61) % RootVariants);
            int h = 10 + v * 3; // alturas dos desenhos: 10, 13, 16 px
            list.Add(new Detail { Sprite = RootName(v), X = x + ((int)(Hash(x, y, 62) % 9) + 4) / 16f, Y = y - h / 16f });
            return list;
        }

        public const int CrackVariants = 3;
        public static string CrackName(int v) { return "env_ext_roc_fenda_" + v; }

        /// <summary>
        /// Fendas na rocha funda (faixa "rocha"), em ~3% das células: quebram a massa uniforme do penhasco que o gato
        /// vê na queda. Escuras, sem brilho dourado (não confundir com a parede quebrável). Base do sprite na célula.
        /// </summary>
        public static List<Detail> DeepDetails(string band, int x, int y)
        {
            var list = new List<Detail>();
            if (band != "rocha" || Hash(x, y, 9) % 100 >= 3) return list;
            int v = (int)(Hash(x, y, 10) % CrackVariants);
            int jitter = (int)(Hash(x, y, 11) % 7) - 3;
            list.Add(new Detail { Sprite = CrackName(v), X = x + 0.5f + jitter / 16f, Y = y + 1f / 16f });
            return list;
        }

        /// <summary>Faixa da célula (topo, terra, transicao, rocha), com a mesma regra de profundidade do tile.</summary>
        public static string BandFor(string material, SolidAt solid, MaterialAt materialAt, int x, int y)
        {
            return Band(material, materialAt == null ? Depth(solid, x, y) : Depth(solid, materialAt, material, x, y));
        }

        // --------------------------------------------------------------- base

        public static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

        public static uint Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)salt * 982451653u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
    }
}
