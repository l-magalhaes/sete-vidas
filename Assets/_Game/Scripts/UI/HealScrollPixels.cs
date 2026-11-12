// Sem "using UnityEngine" e no estilo do C# 5: desenho puro, para a prévia fora do Unity e os testes EditMode.
namespace SeteVidas.UI
{
    /// <summary>
    /// Melhoria 11: o ícone da magia de cura é um pergaminho enrolado de 16×16 com o símbolo da magia (a pata de luz
    /// dourada do Koda) no canto de baixo, à direita. Os usos são lacres de cera de 5×5 na borda de baixo: aceso ou
    /// apagado. Sem usos, o pergaminho fica desbotado. Saída: grade de cores em hex ("" = transparente), de cima para baixo.
    /// </summary>
    public static class HealScrollPixels
    {
        public const int Size = 16, SealSize = 5;

        static readonly string[] Scroll =
        {
            "................",
            "..kkkkkkkkkkkk..",
            ".kRRRRRRRRRRRRk.",
            ".krrrrrrrrrrrrk.",
            "..kPPPPPPPPPPk..",
            "..kPllllllPPPk..",
            "..kPPPPPPPPPPk..",
            "..kPlllllPPPPk..",
            "..kPPPPPPGPGPk..",
            "..kPllllGPGPGk..",
            "..kPPPPPPGGGPk..",
            "..kPPPPPGGGGGk..",
            ".kRRRRRRRGGGRRk.",
            ".krrrrrrrrrrrrk.",
            "..kkkkkkkkkkkk..",
            "................",
        };

        static readonly string[] Seal =
        {
            ".kkk.",
            "kWwWk",
            "kwHwk",
            "kWwWk",
            ".kkk.",
        };

        static string ScrollColor(char key, bool faded)
        {
            switch (key)
            {
                case 'k': return faded ? "#2a2622" : "#3a2a18";
                case 'R': return faded ? "#7a7064" : "#c8a468";
                case 'r': return faded ? "#5a5248" : "#8a6a3a";
                case 'P': return faded ? "#8a8274" : "#ecdcb0";
                case 'l': return faded ? "#6e6858" : "#b8a074";
                case 'G': return faded ? "#6e6858" : "#e0b04a";   // a pata de luz dourada
                default: return "";
            }
        }

        static string SealColor(char key, bool lit)
        {
            switch (key)
            {
                case 'k': return lit ? "#3a0f10" : "#241c1c";
                case 'W': return lit ? "#a8323a" : "#4a3c3c";
                case 'w': return lit ? "#7a1e26" : "#3a3030";
                case 'H': return lit ? "#ffd34d" : "#5a4a48";     // a gotinha dourada da pata no meio do lacre
                default: return "";
            }
        }

        static string[,] Grid(string[] map, int size, System.Func<char, string> color)
        {
            var px = new string[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y, x] = y < map.Length && x < map[y].Length && map[y][x] != '.' ? color(map[y][x]) : "";
            return px;
        }

        /// <summary>O pergaminho (cheio ou desbotado, sem usos).</summary>
        public static string[,] DrawScroll(bool faded) { return Grid(Scroll, Size, delegate (char c) { return ScrollColor(c, faded); }); }

        /// <summary>Um lacre (aceso = uso disponível; apagado = gasto).</summary>
        public static string[,] DrawSeal(bool lit) { return Grid(Seal, SealSize, delegate (char c) { return SealColor(c, lit); }); }
    }
}
