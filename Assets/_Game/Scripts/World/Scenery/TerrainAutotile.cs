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
        public static readonly string[] Materials = { Soil, Rock };

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
            if (material == Rock) return "rocha";
            if (depth <= 2) return "terra";
            return depth == 3 ? "transicao" : "rocha";
        }

        /// <summary>Estratos: 1 px de queda a cada 4 px e uma linha a cada 16 px; repetem a cada 64 px (4 tiles).</summary>
        public const int StrataPhases = 4;

        static bool Phased(string band) { return band == "rocha" || band == "transicao"; }

        public static int VariantCount(string material, string band)
        {
            if (band == "topo") return material == Soil ? 3 : 2;
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
            return TileName(material, band, mask, Variant(material, band, x, y));
        }

        /// <summary>
        /// Variação: na rocha, a fase dos estratos (x par/ímpar) para as linhas continuarem de um tile para o outro;
        /// no resto, sorteio fixo pela posição, sem repetir a mesma 3 vezes seguidas na fileira.
        /// </summary>
        public static int Variant(string material, string band, int x, int y)
        {
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
            foreach (var band in new[] { "topo", "terra", "transicao", "rocha" })
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
            if (material == Soil && Hash(x, y, 3) % 100 < 62)
            {
                int v = (int)(Hash(x, y, 4) % TuftVariants);
                int jitter = (int)(Hash(x, y, 5) % 7) - 3;
                list.Add(new Detail { Sprite = TuftName(v), X = x + 0.5f + jitter * px, Y = y + 1f });
            }
            if (Hash(x, y, 6) % 100 < (material == Rock ? 45 : 30))
            {
                int v = (int)(Hash(x, y, 7) % PebbleVariants);
                int offset = (int)(Hash(x, y, 8) % 12) + 2;
                list.Add(new Detail { Sprite = PebbleName(v), X = x + offset * px, Y = y + 1f });
            }
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
