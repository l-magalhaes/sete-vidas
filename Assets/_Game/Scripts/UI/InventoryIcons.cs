using System.Collections.Generic;

// Sem "using UnityEngine" e no estilo do C# 5: desenho puro, para a prévia fora do Unity e os testes EditMode.
namespace SeteVidas.UI
{
    /// <summary>Os ícones 16×16 do inventário (Docs/Design/inventario.md).</summary>
    public enum InventoryIcon
    {
        Rogue, Paladin, Mage, Monk, Bard, Hunter, Druid,
        Climb, Sneak, Backstab, DownStab, DoubleJump, Dash, Anchor,
        Coin, Page, TabAbilities, TabItems, TabDiary,
        Wood, Map, Key,
    }

    /// <summary>Como o quadrado de um slot é desenhado.</summary>
    public enum SlotLook { Normal, Equipped, Empty }

    /// <summary>Molduras 9-slice do inventário.</summary>
    public enum PanelLook { Main, Inset, Tab, TabActive }

    /// <summary>
    /// A arte do inventário em pixel: ícones 16×16 (contorno automático de 1 px), a silhueta "?" das coisas ainda não
    /// recuperadas, o quadrado do slot (22×22, com bisel: borda clara em cima, fundo afundado), o cursor de cantos
    /// dourados (26×26) e as molduras 9-slice dos painéis e das abas.
    /// Saída: grade de cores em hex ("" = transparente), linha de cima primeiro.
    /// </summary>
    public static class InventoryIcons
    {
        public const int Size = 16, SlotSize = 22, CursorSize = 26;
        /// <summary>Lado e borda (px) das molduras 9-slice: principal, painel interno e aba.</summary>
        public const int PanelSize = 16, PanelBorder = 5, InsetSize = 12, InsetBorder = 4, TabSize = 12, TabBorder = 4;

        public const string Outline = "#140f18";
        const string LockedFill = "#2e2638", LockedMark = "#a898b8";

        // ------------------------------------------------------------ ícones

        public static string[,] Draw(InventoryIcon icon)
        {
            var g = new char[Size, Size];
            Clear(g);
            string pal;
            switch (icon)
            {
                case InventoryIcon.Rogue: pal = Dagger(g, false); break;
                case InventoryIcon.Backstab: pal = Dagger(g, true); break;
                case InventoryIcon.Paladin: pal = Hammer(g); break;
                case InventoryIcon.Mage: pal = Flame(g); break;
                case InventoryIcon.Monk: pal = Staff(g); break;
                case InventoryIcon.Bard: pal = Note(g); break;
                case InventoryIcon.Hunter: pal = Bow(g); break;
                case InventoryIcon.Druid: pal = Sprout(g); break;
                case InventoryIcon.Climb: pal = Claws(g); break;
                case InventoryIcon.Sneak: pal = ClosedEye(g); break;
                case InventoryIcon.DownStab: pal = DownDagger(g); break;
                case InventoryIcon.DoubleJump: pal = Chevrons(g); break;
                case InventoryIcon.Dash: pal = DashArrow(g); break;
                case InventoryIcon.Anchor: pal = Ring(g); break;
                case InventoryIcon.Coin: pal = CoinFace(g); break;
                case InventoryIcon.Page: pal = Paper(g); break;
                case InventoryIcon.TabAbilities: pal = Paw(g); break;
                case InventoryIcon.TabItems: pal = Pouch(g); break;
                case InventoryIcon.Wood: pal = Planks(g); break;
                case InventoryIcon.Map: pal = FoldedMap(g); break;
                case InventoryIcon.Key: pal = KeyShape(g); break;
                default: pal = Book(g); break;
            }
            return Colorize(g, Size, Size, ParsePalette(pal), true);
        }

        /// <summary>A forma do ícone toda escura com um "?" no meio: a promessa do que ainda falta.</summary>
        public static string[,] Silhouette(InventoryIcon icon)
        {
            var src = Draw(icon);
            var px = new string[Size, Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y, x] = src[y, x] == "" ? "" : src[y, x] == Outline ? Outline : LockedFill;
            Stamp(px, Question, 6, 5, LockedMark);
            return px;
        }

        /// <summary>Qualquer grade 16×16 (coração, pergaminho) virada silhueta com "?".</summary>
        public static string[,] Silhouette(string[,] src)
        {
            int h = src.GetLength(0), w = src.GetLength(1);
            var px = new string[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y, x] = src[y, x] == "" ? "" : LockedFill;
            Stamp(px, Question, w / 2 - 2, h / 2 - 3, LockedMark);
            return px;
        }

        static readonly string[] Question = { "###", "..#", ".#.", "...", ".#." };

        static void Stamp(string[,] px, string[] map, int ox, int oy, string color)
        {
            for (int y = 0; y < map.Length; y++)
                for (int x = 0; x < map[y].Length; x++)
                    if (map[y][x] == '#') px[oy + y, ox + x] = color;
        }

        // adaga do Ladino: lâmina na diagonal (ponta em cima, à direita); "costas": com o brilho do golpe
        static string Dagger(char[,] g, bool burst)
        {
            for (int y = 2; y <= 8; y++) { P(g, 15 - y, y, 's'); P(g, 16 - y, y, 'S'); }
            P(g, 14, 1, 'w');
            L(g, 4, 7, 8, 11, 'G'); P(g, 4, 7, 'g'); P(g, 8, 11, 'o'); P(g, 7, 9, 'G');
            for (int y = 10; y <= 12; y++) { P(g, 15 - y, y, 'b'); P(g, 16 - y, y, 'B'); }
            P(g, 2, 13, 'G'); P(g, 3, 13, 'o');
            if (burst)
            {
                // estrela de impacto à esquerda, em cima
                P(g, 4, 3, 'w'); P(g, 3, 3, 'y'); P(g, 5, 3, 'y'); P(g, 4, 2, 'y'); P(g, 4, 4, 'y');
                P(g, 2, 3, 'Y'); P(g, 6, 3, 'Y'); P(g, 4, 1, 'Y'); P(g, 4, 5, 'Y');
            }
            return "w=#ffffff s=#d8e0ec S=#7a86a0 G=#e0b04a g=#ffe08a o=#a8702a b=#5a74b8 B=#2b3a67 y=#ffe08a Y=#e0b04a";
        }

        // marreta do Paladino: cabeça de aço com a faixa dourada, cabo de madeira
        static string Hammer(char[,] g)
        {
            R(g, 2, 2, 12, 5, 'S'); R(g, 2, 2, 12, 1, 's'); R(g, 2, 6, 12, 1, 'd'); P(g, 3, 3, 'w');
            R(g, 7, 2, 2, 5, 'G'); P(g, 7, 2, 'g'); P(g, 8, 6, 'o');
            R(g, 7, 7, 2, 7, 'b'); R(g, 8, 7, 1, 7, 'B'); P(g, 7, 10, 'B'); P(g, 7, 12, 'B');
            R(g, 6, 14, 4, 1, 'G');
            return "w=#ffffff s=#d8e0ec S=#9aa4b8 d=#5a6278 G=#f2d04b g=#fff4c2 o=#a8702a b=#a8703a B=#6a4422";
        }

        // fogo do Mago
        static string Flame(char[,] g)
        {
            M(g, 0, 0, new[]
            {
                "................",
                "........F.......",
                ".......FF.......",
                ".......FOF......",
                "......FOOF..F...",
                ".....FOOOF.FF...",
                ".....FOOYOFOF...",
                "....FOOYYOOOF...",
                "...FOOYYYOOOF...",
                "...FOYYWYYOOF...",
                "..FOOYWWWYOOF...",
                "..FOYYWWWYYOF...",
                "..FOYYWWWYYOF...",
                "...FOYYYYYOF....",
                "....FFOOOFF.....",
            });
            return "F=#c8402a O=#f08a3a Y=#ffd04a W=#fff4dc";
        }

        // bastão do Monge, com as faixas vermelhas
        static string Staff(char[,] g)
        {
            for (int y = 2; y <= 13; y++)
            {
                bool wrap = y == 3 || y == 4 || y == 7 || y == 8 || y == 11 || y == 12;
                P(g, 15 - y, y, wrap ? 'R' : 'b');
                P(g, 16 - y, y, wrap ? 'r' : 'B');
            }
            P(g, 3, 4, 'w'); P(g, 2, 5, 'Y'); P(g, 4, 3, 'Y');
            P(g, 12, 11, 'w'); P(g, 13, 10, 'Y'); P(g, 11, 12, 'Y');
            return "b=#c89058 B=#8a5a32 R=#e04050 r=#a02634 w=#ffffff Y=#ffd04a";
        }

        // nota musical do Bardo
        static string Note(char[,] g)
        {
            M(g, 0, 0, new[]
            {
                "................",
                "................",
                ".......GG.......",
                ".......GgGG.....",
                ".......G.ggGG...",
                ".......G...gGG..",
                ".......G....gG..",
                ".......G.....G..",
                ".......G....G...",
                ".......G........",
                ".......G........",
                "....gGGG........",
                "...gGGGG........",
                "...GGGGd........",
                "....GGd.........",
            });
            return "G=#3aa89a g=#8ae0c8 d=#1e6a60";
        }

        // arco do Caçador, com a flecha armada
        static string Bow(char[,] g)
        {
            int[] arc = { 6, 1, 7, 2, 8, 3, 9, 4, 9, 5, 10, 6, 10, 7, 10, 8, 10, 9, 9, 10, 9, 11, 8, 12, 7, 13, 6, 14 };
            for (int i = 0; i < arc.Length; i += 2) { P(g, arc[i], arc[i + 1], 'b'); P(g, arc[i] + 1, arc[i + 1], 'B'); }
            L(g, 5, 2, 3, 7, 'w'); L(g, 3, 8, 5, 13, 'w');
            L(g, 4, 8, 12, 8, 'a');
            P(g, 13, 7, 's'); P(g, 13, 8, 's'); P(g, 13, 9, 's'); P(g, 14, 8, 'S');
            P(g, 2, 7, 'R'); P(g, 2, 9, 'R'); P(g, 1, 7, 'r'); P(g, 1, 9, 'r');
            return "b=#d89a50 B=#8a5a32 w=#e8dcc0 a=#c8a468 s=#d8e0ec S=#8a94a8 R=#e04050 r=#a02634";
        }

        // broto do Druida, com a flor rosa
        static string Sprout(char[,] g)
        {
            M(g, 0, 0, new[]
            {
                "................",
                "................",
                ".......pp.......",
                "......pPPp......",
                ".......ppe......",
                "..LLL....e.LLL..",
                ".LlllL...eLlllL.",
                ".LllllL..eLllllL",
                "..LllllLLeLlllL.",
                "...LLLLL.eLLLL..",
                ".........e......",
                ".........e......",
                "......ddDDdd....",
                ".....dDDDDDDd...",
            });
            return "p=#e87aa8 P=#ffd0e0 e=#6a9a3a L=#3e7a32 l=#8ac85a d=#8a5a32 D=#5a3a22";
        }

        // escalar: as três garras riscando a pedra
        static string Claws(char[,] g)
        {
            L(g, 2, 4, 5, 13, 'w'); L(g, 3, 4, 6, 13, 'S');
            L(g, 6, 2, 9, 11, 'w'); L(g, 7, 2, 10, 11, 'S');
            L(g, 10, 3, 13, 12, 'w'); L(g, 11, 3, 14, 12, 'S');
            return "w=#f4ecdc S=#8a94a8";
        }

        // furtividade: o olho fechado e a lua
        static string ClosedEye(char[,] g)
        {
            int[] lid = { 2, 8, 3, 9, 4, 10, 5, 10, 6, 11, 7, 11, 8, 11, 9, 11, 10, 10, 11, 10, 12, 9, 13, 8 };
            for (int i = 0; i < lid.Length; i += 2) P(g, lid[i], lid[i + 1], 'w');
            P(g, 4, 11, 'w'); P(g, 3, 12, 'w'); P(g, 7, 12, 'w'); P(g, 7, 13, 'w'); P(g, 11, 11, 'w'); P(g, 12, 12, 'w');
            Disc(g, 10, 4, 3, 'y'); Disc(g, 12, 3, 3, '.'); P(g, 8, 3, 'Y'); P(g, 9, 6, 'Y');
            return "w=#c8d0e8 y=#f2d04b Y=#fff4c2";
        }

        // estocada para baixo: a adaga de ponta para baixo, com o rastro
        static string DownDagger(char[,] g)
        {
            R(g, 6, 1, 4, 1, 'G'); R(g, 7, 2, 2, 2, 'b'); P(g, 8, 2, 'B'); P(g, 8, 3, 'B');
            R(g, 4, 4, 8, 1, 'G'); P(g, 4, 4, 'g'); P(g, 11, 4, 'o');
            R(g, 7, 5, 1, 8, 's'); R(g, 8, 5, 1, 8, 'S'); P(g, 7, 13, 'w');
            L(g, 3, 7, 3, 10, 'm'); L(g, 12, 7, 12, 10, 'm'); L(g, 2, 11, 2, 12, 'm'); L(g, 13, 11, 13, 12, 'm');
            return "G=#e0b04a g=#ffe08a o=#a8702a b=#5a74b8 B=#2b3a67 s=#d8e0ec S=#7a86a0 w=#ffffff m=#8a94a8";
        }

        static string Chevrons(char[,] g)
        {
            for (int k = 0; k < 2; k++)
            {
                int top = 2 + k * 6;
                L(g, 3, top + 5, 7, top + 1, 'w'); L(g, 8, top + 1, 12, top + 5, 'w');
                L(g, 3, top + 6, 7, top + 2, 'S'); L(g, 8, top + 2, 12, top + 6, 'S');
            }
            return "w=#e8f0ff S=#8a94c8";
        }

        static string DashArrow(char[,] g)
        {
            L(g, 1, 5, 5, 5, 'S'); L(g, 2, 8, 7, 8, 'S'); L(g, 1, 11, 5, 11, 'S');
            for (int y = 3; y <= 13; y++)
            {
                int half = 5 - System.Math.Abs(y - 8);
                for (int x = 8; x <= 8 + half; x++) P(g, x, y, y < 8 ? 'w' : 's');
            }
            return "w=#e8f0ff s=#b8c4e8 S=#8a94c8";
        }

        static string Ring(char[,] g)
        {
            Disc(g, 8, 7, 5, 'S'); Disc(g, 8, 7, 2, '.');
            P(g, 5, 4, 's'); P(g, 6, 3, 's'); P(g, 4, 5, 's'); P(g, 4, 6, 's');
            R(g, 7, 12, 2, 3, 'd'); P(g, 7, 12, 'S');
            return "S=#9aa4b8 s=#e8f0ff d=#5a6278";
        }

        static string CoinFace(char[,] g)
        {
            Disc(g, 8, 8, 6, 'o'); Disc(g, 8, 8, 5, 'G'); Disc(g, 8, 8, 3, 'g');
            M(g, 6, 6, new[] { ".o.o.", "o...o", "..o..", ".ooo." });   // a pata da Cidadela cunhada
            P(g, 4, 5, 'w'); P(g, 5, 4, 'w');
            return "o=#a8702a G=#e0b04a g=#f2d04b w=#fff4c2";
        }

        static string Paper(char[,] g)
        {
            R(g, 3, 2, 10, 12, 'P'); P(g, 12, 2, '.'); P(g, 11, 2, '.'); P(g, 12, 3, '.'); P(g, 11, 3, 'l');
            L(g, 5, 5, 10, 5, 'l'); L(g, 5, 7, 9, 7, 'l'); L(g, 5, 9, 10, 9, 'l'); L(g, 5, 11, 8, 11, 'l');
            R(g, 3, 13, 10, 1, 'r');
            return "P=#ecdcb0 l=#a88a5a r=#c8a468";
        }

        static string Paw(char[,] g)
        {
            Disc(g, 8, 11, 3, 'p'); P(g, 7, 10, 'L');
            Disc(g, 3, 7, 1, 'p'); Disc(g, 6, 4, 1, 'p'); Disc(g, 10, 4, 1, 'p'); Disc(g, 13, 7, 1, 'p');
            return "p=#f4d6a3 L=#fff4dc";
        }

        static string Pouch(char[,] g)
        {
            Disc(g, 8, 9, 5, 'b'); R(g, 3, 8, 11, 4, 'b'); R(g, 7, 3, 3, 3, 'B');
            R(g, 6, 5, 5, 1, 'G'); P(g, 5, 3, 'G'); P(g, 6, 4, 'G'); P(g, 10, 4, 'G'); P(g, 11, 3, 'G'); P(g, 10, 5, 'o');
            P(g, 5, 9, 'l'); P(g, 5, 10, 'l'); P(g, 6, 8, 'l');
            R(g, 4, 13, 9, 1, 'B');
            return "b=#a8703a B=#6a4422 l=#d89a50 G=#e0b04a o=#a8702a";
        }

        /// <summary>Chave dourada: argola à esquerda, haste e dois dentes.</summary>
        static string KeyShape(char[,] g)
        {
            Disc(g, 4, 8, 3, 'G'); Disc(g, 4, 8, 1, '.');
            R(g, 7, 7, 7, 2, 'G'); R(g, 11, 9, 1, 2, 'G'); R(g, 13, 9, 1, 3, 'G');
            P(g, 3, 6, 'w'); L(g, 8, 7, 12, 7, 'w');
            return "G=#e0b04a w=#fff4c2";
        }

        /// <summary>Aba Mapa: papel dobrado em três, com uma trilha pontilhada e um X.</summary>
        static string FoldedMap(char[,] g)
        {
            R(g, 2, 3, 4, 10, 'p'); R(g, 6, 4, 4, 10, 'q'); R(g, 10, 3, 4, 10, 'p');
            P(g, 4, 6, 't'); P(g, 5, 8, 't'); P(g, 7, 9, 't'); P(g, 8, 7, 't'); P(g, 10, 8, 't');
            P(g, 11, 5, 'x'); P(g, 13, 7, 'x'); P(g, 12, 6, 'x'); P(g, 11, 7, 'x'); P(g, 13, 5, 'x');
            return "p=#ecdcb0 q=#c8b48a t=#8a6a40 x=#e0b04a";
        }

        /// <summary>Madeira de Reconstrução: duas tábuas empilhadas, com veio e um prego.</summary>
        static string Planks(char[,] g)
        {
            R(g, 2, 9, 12, 3, 'w'); R(g, 2, 11, 12, 1, 'd'); L(g, 4, 10, 8, 10, 'v');
            R(g, 3, 5, 11, 3, 'l'); R(g, 3, 7, 11, 1, 'w'); L(g, 6, 6, 11, 6, 'w');
            P(g, 12, 6, 'n'); P(g, 3, 10, 'n');
            return "w=#965f40 d=#52261e v=#c49860 l=#c49860 n=#c0c6d2";
        }

        static string Book(char[,] g)
        {
            R(g, 3, 2, 10, 12, 'b'); R(g, 3, 2, 2, 12, 'B'); R(g, 12, 3, 1, 10, 'P');
            M(g, 7, 6, new[] { ".G.", "GgG", ".G." });
            R(g, 5, 2, 7, 1, 'l');
            return "b=#8a3a3a B=#5a2228 P=#ecdcb0 G=#e0b04a g=#fff4c2 l=#b85a50";
        }

        // ------------------------------------------------------------ slot, cursor e molduras

        /// <summary>O quadrado de 22×22: contorno, borda clara em cima e escura embaixo, fundo afundado.</summary>
        public static string[,] Slot(SlotLook look)
        {
            string light, dark, sunk, lip, fill;
            switch (look)
            {
                case SlotLook.Equipped: light = "#ffe08a"; dark = "#a8702a"; sunk = "#110d16"; lip = "#4a3a24"; fill = "#2a2018"; break;
                case SlotLook.Empty: light = "#3a3044"; dark = "#221b2a"; sunk = "#0e0b12"; lip = "#241d2c"; fill = "#16121c"; break;
                default: light = "#5e5068"; dark = "#2e2538"; sunk = "#110d16"; lip = "#3a3046"; fill = "#1d1724"; break;
            }
            int n = SlotSize;
            var px = new string[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int edge = System.Math.Min(System.Math.Min(x, y), System.Math.Min(n - 1 - x, n - 1 - y));
                    bool corner = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                    string c;
                    if (corner) c = "";
                    else if (edge == 0) c = Outline;
                    else if (edge == 1) c = (y == 1 || x == 1) && !(y == n - 2 || x == n - 2) ? light : dark;
                    else if (edge == 2) c = (y == 2 || x == 2) && !(y == n - 3 || x == n - 3) ? sunk : lip;
                    else c = fill;
                    px[y, x] = c;
                }
            return px;
        }

        /// <summary>O cursor: quatro cantos dourados (26×26), por fora do slot.</summary>
        public static string[,] Cursor()
        {
            int n = CursorSize;
            var px = new string[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    if (Gold(x, y, n)) { int dx = System.Math.Min(x, n - 1 - x), dy = System.Math.Min(y, n - 1 - y); px[y, x] = dx + dy <= 3 ? "#fff4c2" : "#ffd04a"; continue; }
                    bool near = Gold(x - 1, y, n) || Gold(x + 1, y, n) || Gold(x, y - 1, n) || Gold(x, y + 1, n);
                    px[y, x] = near ? Outline : "";
                }
            return px;
        }

        /// <summary>O "L" dourado de cada canto do cursor: 2 px de grossura, 6 de braço.</summary>
        static bool Gold(int x, int y, int n)
        {
            if (x < 0 || y < 0 || x >= n || y >= n) return false;
            const int arm = 7;
            int dx = System.Math.Min(x, n - 1 - x), dy = System.Math.Min(y, n - 1 - y);
            return (dx >= 1 && dx <= 2 && dy >= 1 && dy < arm) || (dy >= 1 && dy <= 2 && dx >= 1 && dx < arm);
        }

        /// <summary>Moldura 9-slice (a borda é <see cref="PanelBorder"/>, <see cref="InsetBorder"/> ou <see cref="TabBorder"/>).</summary>
        public static string[,] Panel(PanelLook look)
        {
            int n;
            switch (look)
            {
                case PanelLook.Main: n = PanelSize; break;
                case PanelLook.Inset: n = InsetSize; break;
                default: n = TabSize; break;
            }
            var px = new string[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int edge = System.Math.Min(System.Math.Min(x, y), System.Math.Min(n - 1 - x, n - 1 - y));
                    bool corner = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                    bool top = y <= x && y <= n - 1 - x && y <= n - 1 - y;   // a borda de cima pega a luz
                    string c;
                    switch (look)
                    {
                        case PanelLook.Main:
                            if (corner) c = "";
                            else if (edge == 0) c = "#0b0910";
                            else if (edge == 1) c = top ? "#e8c880" : "#a8844a";            // filete dourado
                            else if (edge == 2) c = "#3a2c22";
                            else if (edge == 3) c = top ? "#3e3248" : "#2a2232";
                            else c = "#221b29";
                            // rebites dourados nos cantos
                            if ((x == 2 || x == n - 3) && (y == 2 || y == n - 3)) c = "#ffe08a";
                            break;
                        case PanelLook.Inset:
                            if (corner) c = "";
                            else if (edge == 0) c = "#0e0b12";
                            else if (edge == 1) c = top || x == 1 ? "#0e0b12" : "#3a3046";  // afundado: sombra em cima, luz embaixo
                            else c = "#18131e";
                            break;
                        case PanelLook.TabActive:
                            if (corner) c = "";
                            else if (edge == 0) c = "#0b0910";
                            else if (edge == 1) c = top ? "#ffe08a" : "#c8a468";
                            else if (edge == 2 && top) c = "#5a4a66";
                            else c = "#3e3248";
                            break;
                        default:
                            if (corner) c = "";
                            else if (edge == 0) c = "#0b0910";
                            else if (edge == 1) c = top ? "#4a3e56" : "#2a2232";
                            else c = "#2a2232";
                            break;
                    }
                    px[y, x] = c;
                }
            return px;
        }

        // ------------------------------------------------------------ desenho

        static void Clear(char[,] g) { for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) g[y, x] = '.'; }

        static void P(char[,] g, int x, int y, char c) { if (x >= 0 && x < Size && y >= 0 && y < Size) g[y, x] = c; }

        static void R(char[,] g, int x, int y, int w, int h, char c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) P(g, i, j, c); }

        static void L(char[,] g, int x0, int y0, int x1, int y1, char c)
        {
            int dx = System.Math.Abs(x1 - x0), dy = -System.Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                P(g, x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static void Disc(char[,] g, int cx, int cy, int r, char c)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + r) P(g, x, y, c);
        }

        static void M(char[,] g, int ox, int oy, string[] map)
        {
            for (int y = 0; y < map.Length; y++)
                for (int x = 0; x < map[y].Length; x++)
                    if (map[y][x] != '.') P(g, ox + x, oy + y, map[y][x]);
        }

        static Dictionary<char, string> ParsePalette(string pal)
        {
            var d = new Dictionary<char, string>();
            foreach (var part in pal.Split(' '))
                if (part.Length > 2 && part[1] == '=') d[part[0]] = part.Substring(2);
            return d;
        }

        static string[,] Colorize(char[,] g, int w, int h, Dictionary<char, string> pal, bool outline)
        {
            var px = new string[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char k = g[y, x];
                    string c;
                    if (k != '.' && pal.TryGetValue(k, out c)) { px[y, x] = c; continue; }
                    bool near = outline && (On(g, x - 1, y, w, h) || On(g, x + 1, y, w, h) || On(g, x, y - 1, w, h) || On(g, x, y + 1, w, h));
                    px[y, x] = near ? Outline : "";
                }
            return px;
        }

        static bool On(char[,] g, int x, int y, int w, int h) { return x >= 0 && y >= 0 && x < w && y < h && g[y, x] != '.'; }
    }

    /// <summary>Navegação na grade do inventário: anda slot a slot e para nas bordas (não dá a volta).</summary>
    public static class InventoryGrid
    {
        /// <summary>Novo índice depois de andar (dx, dy). Só existem <paramref name="count"/> slots com coisa.</summary>
        public static int Move(int index, int dx, int dy, int columns, int count)
        {
            if (count <= 0 || columns <= 0) return 0;
            if (index < 0 || index >= count) index = 0;
            int col = index % columns, row = index / columns;
            int nc = col + dx, nr = row + dy;
            if (nc < 0 || nc >= columns || nr < 0) return index;
            int next = nr * columns + nc;
            if (next < count) return next;
            // embaixo não tem nada nessa coluna: vai para o último da linha de baixo, se ela existir
            if (dy > 0 && nr * columns < count) return count - 1;
            return index;
        }

        /// <summary>
        /// Igual a <see cref="Move"/>, mas cada item mora numa casa da grade (<paramref name="cells"/>[i] = linha × colunas
        /// + coluna, em ordem crescente) e pode haver linhas sem item (o título de um capítulo do Diário). Para os lados,
        /// só anda se a casa vizinha tiver item; para cima ou para baixo, pula até a próxima linha com item e escolhe a
        /// coluna mais perto. Retorna o índice do item.
        /// </summary>
        public static int MoveCells(int[] cells, int index, int dx, int dy, int columns)
        {
            if (cells == null || cells.Length == 0 || columns <= 0) return 0;
            if (index < 0 || index >= cells.Length) index = 0;
            int col = cells[index] % columns, row = cells[index] / columns;
            if (dx != 0)
            {
                int nc = col + dx;
                if (nc < 0 || nc >= columns) return index;
                for (int i = 0; i < cells.Length; i++) if (cells[i] == row * columns + nc) return i;
                return index;
            }
            if (dy == 0) return index;
            int lastRow = cells[cells.Length - 1] / columns;
            for (int r = row + dy; r >= 0 && r <= lastRow; r += dy)
            {
                int best = -1, bestDist = int.MaxValue;
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] / columns != r) continue;
                    int d = System.Math.Abs(cells[i] % columns - col);
                    if (d < bestDist) { best = i; bestDist = d; }
                }
                if (best >= 0) return best;
            }
            return index;
        }
    }
}
