using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.UI
{
    /// <summary>
    /// Ícones de botão e tecla em pixel art, desenhados em código (pedido do usuário, 12/11/2026). Todos com 11 px de
    /// altura e o contorno #140f18 dos ícones do inventário:
    /// <list type="bullet">
    /// <item>Controle (Xbox): A verde, B vermelho, X azul, Y amarelo (círculos com a letra), LB/RB (pílula), LT/RT
    /// (gatilho), Menu (≡), analógico (com a seta da direção) e direcional (com o braço aceso).</item>
    /// <item>Teclado: tecla clara com bisel embaixo e o nome dentro (E, ESPAÇO, SHIFT, ↑...).</item>
    /// <item>Mouse: o mouse com o botão aceso (esquerdo ou direito).</item>
    /// </list>
    /// 1 px do ícone = 1 px da interface (480×270) e 16 px por tile no mundo. Pivô embaixo, no meio.
    /// </summary>
    public static class InputGlyphs
    {
        public const int Height = 11;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Ink = PixelFont.Outline;                              // #140f18
        static readonly Color32 PadBody = new Color32(0x3a, 0x36, 0x46, 0xff);
        static readonly Color32 PadLight = new Color32(0x5e, 0x58, 0x72, 0xff);
        static readonly Color32 PadText = new Color32(0xf4, 0xf0, 0xe8, 0xff);
        static readonly Color32 KeyFace = new Color32(0xe8, 0xe0, 0xd0, 0xff);
        static readonly Color32 KeyBevel = new Color32(0xa8, 0x9c, 0x88, 0xff);
        static readonly Color32 KeyText = new Color32(0x24, 0x1c, 0x14, 0xff);
        static readonly Color32 Lit = new Color32(0xff, 0xe0, 0x8a, 0xff);            // o dourado do cursor do inventário

        // cores dos botões do controle de Xbox (mais escuras embaixo)
        static readonly Color32 GreenA = new Color32(0x5c, 0xb0, 0x3c, 0xff), GreenA2 = new Color32(0x3a, 0x7a, 0x28, 0xff);
        static readonly Color32 RedB = new Color32(0xd8, 0x48, 0x3c, 0xff), RedB2 = new Color32(0x96, 0x2c, 0x26, 0xff);
        static readonly Color32 BlueX = new Color32(0x3a, 0x78, 0xd0, 0xff), BlueX2 = new Color32(0x24, 0x4e, 0x92, 0xff);
        static readonly Color32 YellowY = new Color32(0xe8, 0xb8, 0x30, 0xff), YellowY2 = new Color32(0xa0, 0x7a, 0x1a, 0xff);

        static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_cache.Clear(); // Play sem recarregar o domínio

        /// <summary>O ícone de um ou mais controles ("&lt;Keyboard&gt;/a|&lt;Keyboard&gt;/d" vira as duas teclas lado a lado).</summary>
        public static Sprite ForPaths(string paths)
        {
            if (string.IsNullOrEmpty(paths)) return null;
            if (s_cache.TryGetValue(paths, out var cached) && cached != null) return cached;
            var parts = paths.Split('|');
            var grids = new List<Grid>();
            foreach (var p in parts) grids.Add(Draw(p));
            var g = grids.Count == 1 ? grids[0] : Join(grids, 1);
            var s = PixelFont.MakeSprite(g.Px, g.W, g.H, new Vector2(0.5f, 0f), "botão " + paths);
            s_cache[paths] = s;
            return s;
        }

        // ------------------------------------------------------------ desenho

        class Grid
        {
            public int W, H;
            public Color32[] Px;
            public Grid(int w, int h) { W = w; H = h; Px = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) Px[y * W + x] = c; }
            public Color32 Get(int x, int y) => x >= 0 && y >= 0 && x < W && y < H ? Px[y * W + x] : Clear;
        }

        static Grid Draw(string path)
        {
            int slash = path.IndexOf('/');
            string device = slash > 0 ? path.Substring(0, slash) : "";
            string control = slash > 0 ? path.Substring(slash + 1) : path;
            if (device == "<Gamepad>")
            {
                switch (control)
                {
                    case "buttonSouth": return Face("A", GreenA, GreenA2);
                    case "buttonEast": return Face("B", RedB, RedB2);
                    case "buttonWest": return Face("X", BlueX, BlueX2);
                    case "buttonNorth": return Face("Y", YellowY, YellowY2);
                    case "leftShoulder": return Bumper("LB");
                    case "rightShoulder": return Bumper("RB");
                    case "leftTrigger": return Trigger("LT");
                    case "rightTrigger": return Trigger("RT");
                    case "start": return Menu();
                    case "select": return View();
                    case "dpad": return Dpad(0);
                    case "dpad/up": return Dpad(1);
                    case "dpad/down": return Dpad(2);
                    case "dpad/left": return Dpad(3);
                    case "dpad/right": return Dpad(4);
                    case "rightStick": return Stick("R", 0);
                    case "leftStick/up": return Stick("L", 1);
                    case "leftStick/down": return Stick("L", 2);
                    case "leftStick/left": return Stick("L", 3);
                    case "leftStick/right": return Stick("L", 4);
                    default: return Stick("L", 0);
                }
            }
            if (device == "<Mouse>") return MouseGlyph(control == "rightButton" ? 1 : control == "middleButton" ? 2 : 0);
            return Key(InputPrompts.KeyLabel(control, true));
        }

        /// <summary>Círculo de 11 px com contorno, a cor do botão (mais escura embaixo), brilho e a letra clara.</summary>
        static Grid Face(string letter, Color32 color, Color32 dark)
        {
            var g = new Grid(11, Height);
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 11; x++)
                {
                    float dx = x - 5f, dy = y - 5f, d = dx * dx + dy * dy;
                    if (d > 30.5f) continue;
                    g.Set(x, y, d > 21f ? Ink : y <= 3 ? dark : color);
                }
            g.Set(3, 8, new Color32(0xff, 0xff, 0xff, 0xa0));                          // brilho no alto à esquerda
            int lw = PixelFont.Measure(letter);
            PixelFont.Stamp(g.Px, g.W, g.H, letter, (11 - lw) / 2, 2, PadText);
            return g;
        }

        /// <summary>LB/RB: pílula escura com o texto.</summary>
        static Grid Bumper(string text)
        {
            int tw = PixelFont.Measure(text), w = tw + 6;
            var g = new Grid(w, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x == 0 || x == w - 1) && (y == 0 || y == Height - 1);
                    if (corner) continue;
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == Height - 1;
                    g.Set(x, y, edge ? Ink : y >= Height - 3 ? PadLight : PadBody);
                }
            PixelFont.Stamp(g.Px, g.W, g.H, text, 3, 2, PadText);
            return g;
        }

        /// <summary>LT/RT: gatilho com o alto arredondado e a base reta.</summary>
        static Grid Trigger(string text)
        {
            int tw = PixelFont.Measure(text), w = tw + 6;
            var g = new Grid(w, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < w; x++)
                {
                    // a curva do gatilho: os cantos de cima comidos em 2 px
                    int cut = y >= Height - 1 ? 2 : y >= Height - 2 ? 1 : 0;
                    if (x < cut || x > w - 1 - cut) continue;
                    bool edge = x == cut || x == w - 1 - cut || y == 0 || y == Height - 1;
                    g.Set(x, y, edge ? Ink : y >= Height - 4 ? PadLight : PadBody);
                }
            PixelFont.Stamp(g.Px, g.W, g.H, text, 3, 2, PadText);
            return g;
        }

        static Grid Round(Color32 body)
        {
            var g = new Grid(11, Height);
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 11; x++)
                {
                    float dx = x - 5f, dy = y - 5f, d = dx * dx + dy * dy;
                    if (d > 30.5f) continue;
                    g.Set(x, y, d > 21f ? Ink : y >= 7 ? PadLight : body);
                }
            return g;
        }

        /// <summary>Botão Menu (≡) do Xbox.</summary>
        static Grid Menu()
        {
            var g = Round(PadBody);
            for (int x = 3; x <= 7; x++) { g.Set(x, 3, PadText); g.Set(x, 5, PadText); g.Set(x, 7, PadText); }
            return g;
        }

        /// <summary>Botão Exibir (dois quadradinhos) do Xbox.</summary>
        static Grid View()
        {
            var g = Round(PadBody);
            for (int i = 0; i < 4; i++) { g.Set(3 + i, 6, PadText); g.Set(3, 6 - i, PadText); g.Set(5 + i, 4, PadText); g.Set(8, 4 + i, PadText); }
            return g;
        }

        /// <summary>Analógico: anel escuro com a letra; com direção, ganha a seta ao lado (1 cima, 2 baixo, 3 esq., 4 dir.).</summary>
        static Grid Stick(string letter, int dir)
        {
            var g = new Grid(11, Height);
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 11; x++)
                {
                    float dx = x - 5f, dy = y - 5f, d = dx * dx + dy * dy;
                    if (d > 30.5f) continue;
                    g.Set(x, y, d > 21f ? Ink : d > 12f ? PadLight : PadBody);
                }
            PixelFont.Stamp(g.Px, g.W, g.H, letter, 3, 2, PadText);
            if (dir == 0) return g;
            var arrow = new Grid(7, Height);
            char c = dir == 1 ? '↑' : dir == 2 ? '↓' : dir == 3 ? '←' : '→';
            PixelFont.Stamp(arrow.Px, arrow.W, arrow.H, c.ToString(), 1, 2, Lit);
            OutlineAll(arrow);
            return Join(new List<Grid> { g, arrow }, 0);
        }

        /// <summary>Direcional em cruz; o braço da direção pedida aceso (0 = nenhum).</summary>
        static Grid Dpad(int dir)
        {
            var g = new Grid(11, Height);
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 11; x++)
                {
                    bool arm = (x >= 3 && x <= 7) || (y >= 3 && y <= 7);
                    if (!arm) continue;
                    bool edge = x == 0 || x == 10 || y == 0 || y == 10 || ((x == 3 || x == 7) && (y < 3 || y > 7)) || ((y == 3 || y == 7) && (x < 3 || x > 7));
                    Color32 c = edge ? Ink : PadBody;
                    if (!edge && dir != 0)
                    {
                        bool lit = dir == 1 ? y > 7 : dir == 2 ? y < 3 : dir == 3 ? x < 3 : x > 7;
                        if (lit) c = Lit;
                    }
                    g.Set(x, y, c);
                }
            g.Set(5, 5, PadLight);
            return g;
        }

        /// <summary>Tecla: face clara, bisel escuro embaixo, contorno e o nome dentro.</summary>
        static Grid Key(string label)
        {
            int tw = PixelFont.Measure(label), w = Mathf.Max(11, tw + 6);
            var g = new Grid(w, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x == 0 || x == w - 1) && (y == 0 || y == Height - 1);
                    if (corner) continue;
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == Height - 1;
                    g.Set(x, y, edge ? Ink : y <= 1 ? KeyBevel : KeyFace);
                }
            PixelFont.Stamp(g.Px, g.W, g.H, label, (w - tw) / 2, 2, KeyText);
            return g;
        }

        /// <summary>Mouse 9×11 com o botão aceso (0 esquerdo, 1 direito, 2 rodinha).</summary>
        static Grid MouseGlyph(int button)
        {
            var g = new Grid(9, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < 9; x++)
                {
                    float dx = (x - 4f) / 4.5f, dy = (y - 5f) / 5.5f;
                    if (dx * dx + dy * dy > 1f) continue;
                    float ddx = (x - 4f) / 3.5f, ddy = (y - 5f) / 4.5f;
                    bool edge = ddx * ddx + ddy * ddy > 1f;
                    Color32 c = edge ? Ink : KeyFace;
                    if (!edge && y >= 6)                                                       // metade de cima: os botões
                    {
                        if (x == 4) c = Ink;                                                   // a divisão
                        else if (button == 0 && x < 4) c = Lit;
                        else if (button == 1 && x > 4) c = Lit;
                    }
                    if (!edge && y < 6 && y <= 1) c = KeyBevel;
                    g.Set(x, y, c);
                }
            if (button == 2) { g.Set(4, 7, Lit); g.Set(4, 8, Lit); }
            return g;
        }

        /// <summary>Contorno de 1 px em volta do que já está desenhado.</summary>
        static void OutlineAll(Grid g)
        {
            var add = new List<int>();
            for (int y = 0; y < g.H; y++)
                for (int x = 0; x < g.W; x++)
                {
                    if (g.Get(x, y).a != 0) continue;
                    for (int dy = -1; dy <= 1 && !add.Contains(y * g.W + x); dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (g.Get(x + dx, y + dy).a != 0 && !g.Get(x + dx, y + dy).Equals(Ink)) { add.Add(y * g.W + x); break; }
                }
            foreach (int i in add) g.Px[i] = Ink;
        }

        static Grid Join(List<Grid> parts, int gap)
        {
            int w = -gap, h = 0;
            foreach (var p in parts) { w += p.W + gap; h = Mathf.Max(h, p.H); }
            var g = new Grid(w, h);
            int x0 = 0;
            foreach (var p in parts)
            {
                for (int y = 0; y < p.H; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        var c = p.Px[y * p.W + x];
                        if (c.a != 0) g.Set(x0 + x, y, c);
                    }
                x0 += p.W + gap;
            }
            return g;
        }
    }
}
