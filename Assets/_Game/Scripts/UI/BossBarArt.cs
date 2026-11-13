using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.UI
{
    /// <summary>
    /// A barra de vida do chefe em pixel art, desenhada em código (pedido do usuário, 12/11/2026: "mais condizente com
    /// o design do jogo"). Medalhão com a pata dourada de Koda à esquerda, moldura dourada com bisel e ponta de lança,
    /// enchimento dourado em faixas com marcas a cada 10%, a marca da fase furiosa, a parte clara do dano recente e o
    /// nome na fonte em pixel. Na fase furiosa o ouro vira brasa. Sem fades: só troca de cor por pixel.
    /// </summary>
    public static class BossBarArt
    {
        public const int Width = 200, Height = 26;

        /// <summary>Linhas do desenho (y para baixo, a partir do topo).</summary>
        public const int BarTop = 12, InnerTop = 16, InnerRows = 6;
        /// <summary>Colunas do enchimento: logo depois do medalhão até antes da ponta.</summary>
        public const int FillLeft = 15, FillRight = Width - 8;
        public static int FillLength => FillRight - FillLeft + 1;

        public struct State
        {
            public float Health;     // 0..1
            public float Trail;      // 0..1 (>= Health): o dano que acabou de entrar
            public float PhaseAt;    // 0..1: onde começa a fase furiosa (0 = sem marca)
            public bool Furious;
            public bool Flash;       // o golpe acabou de entrar: o enchimento pisca branco
            public int Glint;        // coluna do brilho que passa no ouro (-1 = sem brilho)
            public string Name;
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Outline = PixelFont.Outline;
        static readonly Color32 GoldHi = Hex(0xffe08a), Gold = Hex(0xd9a441), GoldLo = Hex(0xa8782a), GoldDeep = Hex(0x6e4c1c);
        static readonly Color32 Trough = Hex(0x2a1d16), TroughShadow = Hex(0x1a120e);
        static readonly Color32[] FillGold = { Hex(0xfff2b0), Hex(0xffd25e), Hex(0xffd25e), Hex(0xd9a441), Hex(0xd9a441), Hex(0xa8782a) };
        static readonly Color32[] FillEmber = { Hex(0xffd0a0), Hex(0xf08a3c), Hex(0xf08a3c), Hex(0xd0502c), Hex(0xd0502c), Hex(0x8a2a1c) };
        static readonly Color32[] FillTrail = { Hex(0xfff8e0), Hex(0xfff0c0), Hex(0xfff0c0), Hex(0xf0dca0), Hex(0xf0dca0), Hex(0xd8c088) };
        static readonly Color32 White = Hex(0xffffff), Sheen = Hex(0xfffbe8), Ember = Hex(0xd0502c), EmberHi = Hex(0xff8a4a);
        static readonly Color32 MedalInner = Hex(0x2a2040), MedalInnerFurious = Hex(0x5a1a14);

        static Color32 Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8 & 0xff), (byte)(rgb & 0xff), 0xff);

        /// <summary>Quantas colunas do enchimento estão cheias para a fração <paramref name="f"/>.</summary>
        public static int Columns(float f) => Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(f) * FillLength - 0.001f), 0, FillLength);

        /// <summary>Desenha a barra inteira em <paramref name="px"/> (Width × Height, linhas de baixo para cima, como a textura).</summary>
        public static void Draw(Color32[] px, State s)
        {
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
            DrawFrame(px, s);
            DrawFill(px, s);
            DrawMedallion(px, s.Furious);
            DrawName(px, s);
        }

        static void Set(Color32[] px, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            px[(Height - 1 - y) * Width + x] = c;
        }

        /// <summary>A cor no ponto (y para baixo). Para os testes.</summary>
        public static Color32 Get(Color32[] px, int x, int y) => px[(Height - 1 - y) * Width + x];

        static void DrawFrame(Color32[] px, State s)
        {
            int top = InnerTop - 2, bottom = InnerTop + InnerRows + 1; // contorno de cima e de baixo
            for (int x = 10; x <= FillRight; x++)
            {
                Set(px, x, top, Outline);
                Set(px, x, top + 1, GoldHi);
                for (int r = 0; r < InnerRows; r++) Set(px, x, InnerTop + r, r == 0 ? TroughShadow : Trough);
                Set(px, x, bottom - 1, GoldLo);
                Set(px, x, bottom, Outline);
            }
            // marcas a cada 10% no bisel
            for (int k = 1; k < 10; k++)
            {
                int x = FillLeft + Mathf.RoundToInt(k * FillLength / 10f);
                Set(px, x, top + 1, Gold);
                Set(px, x, bottom - 1, GoldDeep);
            }
            // ponta de lança à direita
            int mid = InnerTop + InnerRows / 2; // 19
            for (int i = 1; i <= 7; i++)
            {
                int x = FillRight + i, half = 5 - i * 5 / 7 + 0;
                if (half < 0) half = 0;
                for (int y = mid - half - 1; y <= mid + half; y++)
                {
                    bool edge = y == mid - half - 1 || y == mid + half;
                    Set(px, x, y, edge ? Outline : (y < mid ? GoldHi : (y == mid ? Gold : GoldLo)));
                }
                if (i == 7) { Set(px, x, mid - 1, Outline); Set(px, x, mid, Outline); }
            }
            // marca da fase furiosa: um losango de brasa cravado na moldura
            if (s.PhaseAt > 0f && s.PhaseAt < 1f)
            {
                int x = FillLeft + Mathf.RoundToInt(s.PhaseAt * FillLength);
                int cy = top - 1;
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int d = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (d > 3) continue;
                        Set(px, x + dx, cy + dy, d == 3 ? Outline : (d == 2 ? (dy < 0 || dx < 0 && dy == 0 ? EmberHi : Ember) : (d == 1 ? Ember : EmberHi)));
                    }
            }
        }

        static void DrawFill(Color32[] px, State s)
        {
            int fill = Columns(s.Health), trail = Mathf.Max(fill, Columns(s.Trail));
            var band = s.Furious ? FillEmber : FillGold;
            for (int c = 0; c < trail; c++)
            {
                int x = FillLeft + c;
                bool lit = c < fill;
                for (int r = 0; r < InnerRows; r++)
                {
                    Color32 col = lit ? (s.Flash ? White : band[r]) : FillTrail[r];
                    if (lit && !s.Flash && c == fill - 1 && r < InnerRows - 1) col = band[0];      // borda da frente acesa
                    if (lit && !s.Flash && s.Glint >= 0)
                    {
                        int g = s.Glint - r / 2;                                                // brilho em diagonal
                        if ((c == g || c == g + 1) && r < 5) col = Sheen;
                    }
                    Set(px, x, InnerTop + r, col);
                }
            }
            // o corte da fase furiosa também no fundo vazio da barra
            if (s.PhaseAt > 0f && s.PhaseAt < 1f)
            {
                int c = Mathf.RoundToInt(s.PhaseAt * FillLength);
                if (c >= trail) for (int r = 1; r < InnerRows; r++) Set(px, FillLeft + c, InnerTop + r, MedalInnerFurious);
            }
        }

        static void DrawMedallion(Color32[] px, bool furious)
        {
            const float cx = 7.5f, cy = 19f;
            for (int y = BarTop; y < Height; y++)
                for (int x = 0; x < 15; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 7.2f) continue;
                    Color32 c;
                    if (d > 6.2f) c = Outline;
                    else if (d > 4.7f) c = dx + dy < -1.5f ? GoldHi : (dx + dy > 2.5f ? GoldLo : Gold);
                    else c = furious ? MedalInnerFurious : MedalInner;
                    Set(px, x, y, c);
                }
            // a pata dourada de Koda (vira brasa na fúria)
            string[] paw =
            {
                "..#.#..",
                "#.#.#.#",
                "#.....#",
                "..###..",
                ".#####.",
                ".#####.",
                "..###..",
            };
            Color32 hi = furious ? EmberHi : FillGold[1], lo = furious ? Ember : Gold;
            for (int r = 0; r < paw.Length; r++)
                for (int c = 0; c < paw[r].Length; c++)
                    if (paw[r][c] == '#') Set(px, 4 + c, 15 + r, r >= 5 ? lo : hi);
        }

        static void DrawName(Color32[] px, State s)
        {
            if (string.IsNullOrEmpty(s.Name)) return;
            var sprite = PixelFont.Render(s.Name, s.Furious ? (Color)EmberHi : (Color)GoldHi, (Color)Outline);
            var tex = sprite.texture;
            int tw = tex.width, th = tex.height;
            var src = tex.GetPixels32();
            int center = (FillLeft + FillRight) / 2;
            int left = center - tw / 2, top = Mathf.Max(0, BarTop - 1 - th);
            for (int ty = 0; ty < th; ty++)
                for (int tx = 0; tx < tw; tx++)
                {
                    var c = src[ty * tw + tx];
                    if (c.a == 0) continue;
                    Set(px, left + tx, top + (th - 1 - ty), c);
                }
            // ornamentos: um fio dourado com um losango de cada lado do nome
            int row = top + th / 2;
            for (int i = 0; i < 18; i++)
            {
                Set(px, left - 3 - i, row, i == 17 ? GoldHi : GoldLo);
                Set(px, left + tw + 2 + i, row, i == 17 ? GoldHi : GoldLo);
            }
            Diamond(px, left - 23, row); Diamond(px, left + tw + 22, row);
        }

        static void Diamond(Color32[] px, int x, int y)
        {
            Set(px, x, y - 2, Outline); Set(px, x - 1, y - 1, Outline); Set(px, x + 1, y - 1, Outline);
            Set(px, x - 2, y, Outline); Set(px, x + 2, y, Outline); Set(px, x - 1, y + 1, Outline); Set(px, x + 1, y + 1, Outline); Set(px, x, y + 2, Outline);
            Set(px, x, y - 1, GoldHi); Set(px, x - 1, y, GoldHi); Set(px, x, y, Gold); Set(px, x + 1, y, GoldLo); Set(px, x, y + 1, GoldLo);
        }
    }
}
