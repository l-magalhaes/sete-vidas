// Sem "using UnityEngine" e no estilo do C# 5: o desenho dos corações é C# puro, para a prévia fora do Unity
// (Add-Type no PowerShell) e os testes EditMode.
namespace SeteVidas.UI
{
    /// <summary>Estado de um coração no HUD (o mesmo do HealthRules.HeartState, sem depender dele).</summary>
    public enum HeartLook { Full, Half, Empty, Locked }

    /// <summary>
    /// Melhoria 7 (revisada pelo usuário): todos os corações são vermelhos; cada classe veste o coração com um acessório
    /// dela, desenhado por cima. Ladino: capuz da Guilda. Paladino: elmo com crista dourada. Mago: chapéu pontudo com
    /// estrela. Monge: faixa na testa. Bardo: boina com pena. Caçador: flecha atravessada. Druida: broto com flor.
    /// 16×16: o coração de 13×11 fica embaixo (linhas 4–14) para o acessório caber em cima.
    /// Cheio: vermelho com brilho. Meio: metade esquerda (1 hit tomado). Vazio: o interior escuro. Travado: pedra rachada,
    /// sem acessório (o HUD nem mostra o coração travado; só aparece enquanto some na maldição).
    /// Saída: grade de 16×16 de cores em hex ("" = transparente), linha de cima primeiro.
    /// </summary>
    public static class ClassHeartPixels
    {
        public const int Size = 16;
        public const int Classes = 7;
        public const int HeartTop = 4;

        public const string Red = "#d6304a", Shine = "#ff8a9a", Outline = "#3a0f18", Hollow = "#4a2a33";
        const string Stone = "#3b3b48", StoneEdge = "#22222a";

        static readonly string[] Shape =
        {
            "..XXX...XXX..",
            ".XXXXX.XXXXX.",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            ".XXXXXXXXXXX.",
            "..XXXXXXXXX..",
            "...XXXXXXX...",
            "....XXXXX....",
            ".....XXX.....",
            "......X......",
        };

        // Acessórios 16×16 por cima do coração ('.' = nada). Cada letra é uma cor da paleta da classe.
        static readonly string[][] Accessories =
        {
            new[] // Ladino: capuz azul-noite da Guilda por cima dos lobos, com a ponta caída e as abas dos lados
            {
                "................",
                "...........kk...",
                "......kkkkkhHk..",
                "....kkhhHHHHHk..",
                "...khHHHHHHHHHk.",
                "..khH........Hk.",
                ".khH..........k.",
                ".kH.............",
                "..k.............",
                "................", "................", "................", "................", "................", "................", "................",
            },
            new[] // Paladino: elmo pequeno de aço com a crista dourada, sentado entre os lobos
            {
                ".......kk.......",
                "......kGGk......",
                ".....kkGGkk.....",
                "....kSsGGSSk....",
                "...kSsSGGSSSk...",
                "...kkkkkkkkkk...",
                "................", "................", "................", "................", "................", "................", "................", "................", "................", "................",
            },
            new[] // Mago: chapéu pontudo com uma estrela, com a aba em cima dos lobos
            {
                "........kk......",
                ".......kOk......",
                "......kOOk......",
                ".....kOYOk......",
                "....kOYYYOk.....",
                "..kkBBBYBBBkk...",
                "................", "................", "................", "................", "................", "................", "................", "................", "................", "................",
            },
            new[] // Monge: faixa clara logo abaixo dos lobos, com as pontas soltas ao vento
            {
                "................", "................", "................", "................", "................", "................",
                ".kWWWWWWWWWWWWk.",
                "Ww..............",
                ".Ww.............",
                "Ww..............",
                "................", "................", "................", "................", "................", "................",
            },
            new[] // Bardo: boina verde-lago no lobo direito, com uma pena clara saindo para a esquerda
            {
                "....f...........",
                ".....F..........",
                "......F.kkkk....",
                ".......kGGGGkk..",
                "......kGgGGGGGk.",
                ".......kkkkkkkk.",
                "................", "................", "................", "................", "................", "................", "................", "................", "................", "................",
            },
            new[] // Caçador: flecha atravessando o coração, da ponta (embaixo, à esquerda) às penas (em cima, à direita)
            {
                "................",
                ".............FF.",
                "............FFf.",
                "...........AFf..",
                "..........A.....",
                ".........A......",
                "........A.......",
                ".......A........",
                "......A.........",
                ".....A..........",
                "....A...........",
                "..MA............",
                ".MMM............",
                "..M.............",
                "................", "................",
            },
            new[] // Druida: um broto entre os lobos, com duas folhas e uma flor rosa
            {
                "......pPp.......",
                "......PyP.......",
                "......pPp.......",
                ".......g........",
                "...LLl.g.lLL....",
                "....Ll.g.lL.....",
                "................", "................", "................", "................", "................", "................", "................", "................", "................", "................",
            },
        };

        // Paleta dos acessórios: k é sempre o contorno escuro do acessório.
        static readonly string[][] Palettes =
        {
            new[] { "k#141b33", "H#2b3a67", "h#4a63b8" },                                   // Ladino
            new[] { "k#2a2f3a", "S#9aa3b5", "s#d4dae6", "G#f2c94c" },                       // Paladino
            new[] { "k#2a1a10", "O#c4562a", "B#8a3a18", "Y#ffe08a" },                       // Mago
            new[] { "k#3a0f18", "W#f0e6d0", "w#c8b898" },                                   // Monge
            new[] { "k#0c332e", "G#2f9e8f", "g#1c6b60", "F#f2f2e0", "f#c8c8b0" },           // Bardo
            new[] { "A#6b4a24", "M#c8d0dc", "F#f2ecd8", "f#bdb59c" },                       // Caçador
            new[] { "g#2f6b26", "L#5aa84a", "l#2f6b26", "P#f2a0c8", "p#c86a98", "y#ffe08a" }, // Druida
        };

        static int Clamp(int i) { return i < 0 ? 0 : i >= Classes ? Classes - 1 : i; }

        static bool Inside(int c, int r) { return r >= 0 && r < Shape.Length && c >= 0 && c < Shape[0].Length && Shape[r][c] == 'X'; }

        static string PaletteColor(int k, char key)
        {
            foreach (var entry in Palettes[k]) if (entry[0] == key) return entry.Substring(1);
            return "";
        }

        /// <summary>Quantos pixels o acessório da classe tem (testes).</summary>
        public static int AccessoryPixels(int classIndex)
        {
            int n = 0;
            foreach (var row in Accessories[Clamp(classIndex)]) foreach (char c in row) if (c != '.') n++;
            return n;
        }

        /// <summary>O desenho de um coração: [linha de cima → de baixo][coluna], hex ou "".</summary>
        public static string[,] Draw(int classIndex, HeartLook look)
        {
            int k = Clamp(classIndex);
            var px = new string[Size, Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) px[y, x] = "";

            int cols = Shape[0].Length;
            for (int r = 0; r < Shape.Length; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (!Inside(c, r)) continue;
                    bool edge = !Inside(c - 1, r) || !Inside(c + 1, r) || !Inside(c, r - 1) || !Inside(c, r + 1);
                    string color;
                    switch (look)
                    {
                        case HeartLook.Locked:
                            color = edge ? StoneEdge : Stone;
                            if (!edge && (c == 6 && r >= 2 && r <= 7 || c == 5 && r == 4 || c == 7 && r == 6)) color = StoneEdge;
                            break;
                        case HeartLook.Empty: color = edge ? Outline : Hollow; break;
                        case HeartLook.Half: color = edge ? Outline : (c <= cols / 2 ? Red : Hollow); break;
                        default: color = edge ? Outline : Red; break;
                    }
                    if ((look == HeartLook.Full || look == HeartLook.Half) && !edge && r == 2 && (c == 2 || c == 3)) color = Shine;
                    px[HeartTop + r, 1 + c] = color;
                }

            if (look == HeartLook.Locked) return px; // travado: só a pedra
            var acc = Accessories[k];
            for (int y = 0; y < Size && y < acc.Length; y++)
                for (int x = 0; x < Size && x < acc[y].Length; x++)
                {
                    char key = acc[y][x];
                    if (key == '.') continue;
                    string color = PaletteColor(k, key);
                    if (color != "") px[y, x] = color;
                }
            return px;
        }
    }
}
