using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Fonte em pixel 5×7 (maiúsculas, números, acentos do português) desenhada em código. Cada texto vira um sprite
    /// com contorno escuro de 1 px e as duas linhas de baixo de cada letra um tom mais escuras (o mesmo jeito do logo).
    /// 16 px = 1 tile, como o resto do jogo. Pedido do usuário (12/11/2026): nada de texto na fonte do Unity no mundo.
    /// </summary>
    public static class PixelFont
    {
        public const int Ppu = 16;
        public const int CapHeight = 7;
        public static readonly Color32 Outline = new Color32(0x14, 0x0f, 0x18, 0xff); // o contorno dos ícones do inventário

        static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_cache.Clear(); // Play sem recarregar o domínio

        static readonly Dictionary<char, string[]> s_glyphs = new Dictionary<char, string[]>
        {
            { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'B', new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." } },
            { 'C', new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." } },
            { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
            { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
            { 'F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
            { 'G', new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." } },
            { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'I', new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
            { 'J', new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." } },
            { 'K', new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" } },
            { 'L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" } },
            { 'M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
            { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
            { 'O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'P', new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." } },
            { 'Q', new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" } },
            { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
            { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
            { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
            { 'W', new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" } },
            { 'X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" } },
            { 'Y', new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." } },
            { 'Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" } },
            { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
            { '1', new[] { ".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###" } },
            { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
            { '3', new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." } },
            { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
            { '5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
            { '6', new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." } },
            { '7', new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
            { '8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
            { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." } },
            { '+', new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." } },
            { '-', new[] { "...", "...", "...", "###", "...", "...", "..." } },
            { '!', new[] { "#", "#", "#", "#", "#", ".", "#" } },
            { '?', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." } },
            { '.', new[] { ".", ".", ".", ".", ".", ".", "#" } },
            { ',', new[] { ".", ".", ".", ".", ".", "#", "#" } },
            { ':', new[] { ".", "#", ".", ".", ".", "#", "." } },
            { '\'', new[] { "#", "#", ".", ".", ".", ".", "." } },
            { ' ', new[] { "...", "...", "...", "...", "...", "...", "..." } },
            { '/', new[] { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." } },
            { '↑', new[] { "..#..", ".###.", "#.#.#", "..#..", "..#..", "..#..", "..#.." } },
            { '↓', new[] { "..#..", "..#..", "..#..", "..#..", "#.#.#", ".###.", "..#.." } },
            { '←', new[] { ".....", "..#..", ".#...", "#####", ".#...", "..#..", "....." } },
            { '→', new[] { ".....", "..#..", "...#.", "#####", "...#.", "..#..", "....." } },
        };

        /// <summary>
        /// Carimba o texto (sem contorno) dentro de um desenho já existente: <paramref name="x"/> = coluna da esquerda,
        /// <paramref name="baseY"/> = linha de baixo das letras (y para cima). Usado nos ícones de botão e tecla.
        /// </summary>
        public static void Stamp(Color32[] px, int w, int h, string text, int x, int baseY, Color32 color)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (char raw in text.ToUpperInvariant())
            {
                char b = Base(raw, out _, out _);
                if (!s_glyphs.TryGetValue(b, out var g)) g = s_glyphs['?'];
                int gw = g[0].Length;
                for (int r = 0; r < CapHeight; r++)
                    for (int c = 0; c < gw; c++)
                    {
                        if (g[r][c] != '#') continue;
                        int px0 = x + c, py = baseY + CapHeight - 1 - r;
                        if (px0 >= 0 && py >= 0 && px0 < w && py < h) px[py * w + px0] = color;
                    }
                x += gw + 1;
            }
        }

        // marcas de acento (2 linhas por cima da letra) e a cedilha (2 linhas por baixo)
        static readonly string[] Acute = { "...#.", "..#.." };
        static readonly string[] Grave = { ".#...", "..#.." };
        static readonly string[] Circ = { "..#..", ".#.#." };
        static readonly string[] Tilde = { ".##.#", "#..#." };
        static readonly string[] Cedilla = { "..#..", ".##.." };

        static char Base(char c, out string[] mark, out bool below)
        {
            below = false;
            switch (c)
            {
                case 'Á': mark = Acute; return 'A';
                case 'À': mark = Grave; return 'A';
                case 'Â': mark = Circ; return 'A';
                case 'Ã': mark = Tilde; return 'A';
                case 'É': mark = Acute; return 'E';
                case 'Ê': mark = Circ; return 'E';
                case 'Í': mark = Acute; return 'I';
                case 'Ó': mark = Acute; return 'O';
                case 'Ô': mark = Circ; return 'O';
                case 'Õ': mark = Tilde; return 'O';
                case 'Ú': mark = Acute; return 'U';
                case 'Ç': mark = Cedilla; below = true; return 'C';
                default: mark = null; return c;
            }
        }

        /// <summary>Tem desenho para este caractere (depois de passar para maiúscula)?</summary>
        public static bool Supports(char c) => s_glyphs.ContainsKey(Base(char.ToUpperInvariant(c), out _, out _));

        /// <summary>Largura em pixels do texto (sem o contorno).</summary>
        public static int Measure(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int w = 0;
            foreach (char raw in text.ToUpperInvariant())
            {
                char b = Base(raw, out _, out _);
                if (!s_glyphs.TryGetValue(b, out var g)) g = s_glyphs['?'];
                w += g[0].Length + 1;
            }
            return w - 1;
        }

        /// <summary>
        /// O texto como sprite (com cache). Pivô no meio da linha de base: o sprite "pousa" no ponto dado.
        /// <paramref name="outline"/> com alfa 0 desenha sem contorno.
        /// </summary>
        public static Sprite Render(string text, Color fill, Color outline, float pivotX = 0.5f)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            string up = text.ToUpperInvariant();
            Color32 f = fill, o = outline;
            string key = up + "|" + f.r + "," + f.g + "," + f.b + "," + f.a + "|" + o.r + "," + o.g + "," + o.b + "," + o.a + "|" + pivotX;
            if (s_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            bool hasAccent = false, hasBelow = false;
            foreach (char c in up)
            {
                Base(c, out var m, out bool below);
                if (m != null && !below) hasAccent = true;
                if (below || c == ',') hasBelow = true;
            }
            int top = hasAccent ? 2 : 0, bottom = hasBelow ? 2 : 0;
            int pad = o.a > 0 ? 1 : 0;
            int w = Measure(up) + pad * 2, h = CapHeight + top + bottom + pad * 2;

            // 0 = vazio, 1 = letra, 2 = sombra da letra
            var grid = new byte[w, h];
            int x = pad;
            int baseY = pad + bottom; // linha de baixo das letras (y cresce para cima)
            foreach (char raw in up)
            {
                char b = Base(raw, out var mark, out bool below);
                if (!s_glyphs.TryGetValue(b, out var g)) g = s_glyphs['?'];
                int gw = g[0].Length;
                for (int r = 0; r < CapHeight; r++)
                    for (int c = 0; c < gw; c++)
                        if (g[r][c] == '#') grid[x + c, baseY + CapHeight - 1 - r] = (byte)(r >= CapHeight - 2 ? 2 : 1);
                if (b == ',') { grid[x, baseY - 1] = 1; }
                if (mark != null)
                {
                    int mx = x + (gw - 5) / 2;
                    for (int r = 0; r < 2; r++)
                        for (int c = 0; c < 5; c++)
                        {
                            if (mark[r][c] != '#') continue;
                            int px = mx + c, py = below ? baseY - 1 - r : baseY + CapHeight + 1 - r;
                            if (px >= 0 && px < w && py >= 0 && py < h) grid[px, py] = 1;
                        }
                }
                x += gw + 1;
            }

            var shade = new Color(fill.r * 0.78f, fill.g * 0.72f, fill.b * 0.78f, fill.a);
            var px32 = new Color32[w * h];
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                {
                    byte v = grid[xx, yy];
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (v == 1) c = f;
                    else if (v == 2) c = shade;
                    else if (pad > 0 && Near(grid, xx, yy, w, h)) c = o;
                    px32[yy * w + xx] = c;
                }

            var sprite = MakeSprite(px32, w, h, new Vector2(pivotX, (float)(pad + bottom) / h), "texto " + text);
            s_cache[key] = sprite;
            return sprite;
        }

        static bool Near(byte[,] g, int x, int y, int w, int h)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && g[nx, ny] != 0) return true;
                }
            return false;
        }

        /// <summary>Sprite de pixels prontos (ponto, sem filtro, 16 px por tile).</summary>
        public static Sprite MakeSprite(Color32[] pixels, int w, int h, Vector2 pivot, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, Ppu, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }

        /// <summary>Arredonda para o pixel do mundo (1/16 de tile).</summary>
        public static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x * Ppu) / Ppu, Mathf.Round(p.y * Ppu) / Ppu, p.z);
    }

    /// <summary>
    /// Aviso curto em pixel que sobe e some piscando (moedas, "Salvo", nome da classe). Sobe em passos de 1 px,
    /// para no alto e pisca três vezes antes de sumir, sem transparência (o jeito do pixel art).
    /// </summary>
    public class PixelPopup : MonoBehaviour
    {
        const float RiseTime = 0.3f, HoldTime = 0.55f, BlinkTime = 0.3f;
        const int RisePx = 10;

        SpriteRenderer _text, _icon;
        Vector3 _start;
        float _age;

        /// <summary>Quantos avisos estão na tela (testes).</summary>
        public static int Alive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Alive = 0; // Play sem recarregar o domínio

        public static PixelPopup Spawn(Vector3 position, string text, Color color, Sprite icon = null)
        {
            if (!Application.isPlaying) return null;
            var go = new GameObject("Aviso " + text);
            var popup = go.AddComponent<PixelPopup>();
            popup._start = PixelFont.Snap(position);
            go.transform.position = popup._start;

            var t = new GameObject("Texto").AddComponent<SpriteRenderer>();
            t.transform.SetParent(go.transform, false);
            t.sprite = PixelFont.Render(text, color, PixelFont.Outline);
            t.sortingOrder = 50;
            popup._text = t;

            if (icon != null)
            {
                int textW = PixelFont.Measure(text) + 2;
                int iconW = Mathf.RoundToInt(icon.rect.width);
                // ícone à esquerda, texto à direita, o par centrado no ponto
                float total = (textW + iconW + 1) / (float)PixelFont.Ppu;
                var i = new GameObject("Ícone").AddComponent<SpriteRenderer>();
                i.transform.SetParent(go.transform, false);
                i.sprite = icon;
                i.sortingOrder = 50;
                float iconCenter = -total * 0.5f + iconW * 0.5f / PixelFont.Ppu;
                float iconBottom = icon.bounds.min.y;
                i.transform.localPosition = PixelFont.Snap(new Vector3(iconCenter, -iconBottom, 0f));
                t.transform.localPosition = PixelFont.Snap(new Vector3(-total * 0.5f + (iconW + 1) / (float)PixelFont.Ppu + textW * 0.5f / PixelFont.Ppu, 0f, 0f));
                popup._icon = i;
            }
            Alive++;
            return popup;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime; // continua no hitstop
            // sobe rápido e freia (em pixels inteiros)
            float k = Mathf.Clamp01(_age / RiseTime);
            int px = Mathf.RoundToInt(RisePx * (1f - (1f - k) * (1f - k)));
            transform.position = _start + new Vector3(0f, px / (float)PixelFont.Ppu, 0f);
            bool visible = true;
            if (_age > RiseTime + HoldTime) visible = ((int)((_age - RiseTime - HoldTime) * 20f) & 1) == 0;
            _text.enabled = visible;
            if (_icon != null) _icon.enabled = visible;
            if (_age >= RiseTime + HoldTime + BlinkTime) Destroy(gameObject);
        }

        void OnDestroy() => Alive = Mathf.Max(0, Alive - 1);
    }

    /// <summary>
    /// Anel em pixel que cresce e se desfaz em pontilhado (onda de barulho do alarme, luz do santuário, impactos).
    /// Cada raio é uma textura pequena em cache; a cor vem da tinta do SpriteRenderer.
    /// </summary>
    public class PixelRing : MonoBehaviour
    {
        static readonly Dictionary<int, Sprite> s_rings = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_rings.Clear(); // Play sem recarregar o domínio

        SpriteRenderer _renderer;
        int _from, _to, _thickness;
        float _delay, _duration, _age;
        float _squashY = 1f;
        bool _unscaled;

        /// <summary>Quantos anéis estão vivos (testes).</summary>
        public static int Alive { get; private set; }

        /// <summary>
        /// Anel de <paramref name="fromPx"/> até <paramref name="toPx"/> de raio em <paramref name="duration"/> s,
        /// depois de <paramref name="delay"/> s. <paramref name="squashY"/> &lt; 1 achata (anel no chão).
        /// </summary>
        public static PixelRing Spawn(Vector2 center, int fromPx, int toPx, float duration, Color color,
                                      int thickness = 1, float delay = 0f, int sortingOrder = 12, float squashY = 1f)
        {
            if (!Application.isPlaying) return null;
            var go = new GameObject("Anel");
            go.transform.position = PixelFont.Snap(center);
            var ring = go.AddComponent<PixelRing>();
            ring._renderer = go.AddComponent<SpriteRenderer>();
            ring._renderer.color = color;
            ring._renderer.sortingOrder = sortingOrder;
            ring._renderer.enabled = false;
            ring._from = Mathf.Max(1, fromPx);
            ring._to = Mathf.Max(ring._from, toPx);
            ring._thickness = Mathf.Clamp(thickness, 1, 3);
            ring._duration = Mathf.Max(0.05f, duration);
            ring._delay = Mathf.Max(0f, delay);
            ring._squashY = Mathf.Clamp(squashY, 0.2f, 1f);
            Alive++;
            return ring;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = (_age - _delay) / _duration;
            if (t < 0f) return;
            if (t >= 1f) { Destroy(gameObject); return; }
            float e = 1f - (1f - t) * (1f - t);
            int r = Mathf.RoundToInt(Mathf.Lerp(_from, _to, e));
            int dither = t < 0.55f ? 0 : t < 0.8f ? 1 : 2; // perde pixels no fim, sem transparência
            _renderer.sprite = Ring(r, _thickness, dither, _squashY);
            _renderer.enabled = true;
        }

        void OnDestroy() => Alive = Mathf.Max(0, Alive - 1);

        static Sprite Ring(int r, int thickness, int dither, float squashY)
        {
            int squashKey = Mathf.RoundToInt(squashY * 20f);
            int key = (((r * 4 + thickness) * 4 + dither) * 32) + squashKey;
            if (s_rings.TryGetValue(key, out var s) && s != null) return s;
            int ry = Mathf.Max(1, Mathf.RoundToInt(r * squashY));
            int w = r * 2 + 3, h = ry * 2 + 3;
            var px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - cx), dy = (y + 0.5f - cy) / (ry / (float)r);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool on = d >= r - thickness + 0.5f && d < r + 0.5f;
                    if (on && dither == 1) on = ((x + y) & 1) == 0;
                    if (on && dither == 2) on = (x & 1) == 0 && (y & 1) == 0 || ((x + y) % 4 == 0);
                    px[y * w + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            s = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0.5f), "anel " + r);
            s_rings[key] = s;
            return s;
        }
    }

    /// <summary>Ícones pequenos desenhados em código para os avisos (coração do vínculo).</summary>
    public static class PixelIcons
    {
        static Sprite s_heart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_heart = null; // Play sem recarregar o domínio

        /// <summary>Coração 9×8 com contorno, o mesmo vermelho do HUD.</summary>
        public static Sprite Heart()
        {
            if (s_heart != null) return s_heart;
            string[] map =
            {
                ".oo...oo.",
                "oRRo.oRRo",
                "oRWRoRRRo",
                "oRRRRRRro",
                ".oRRRRro.",
                "..oRRro..",
                "...oro...",
                "....o....",
            };
            s_heart = FromMap(map, "ícone coração");
            return s_heart;
        }

        static Sprite FromMap(string[] map, string name)
        {
            int w = map[0].Length, h = map.Length;
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    Color32 col = map[r][c] switch
                    {
                        'o' => PixelFont.Outline,
                        'R' => new Color32(0xe0, 0x3c, 0x4a, 0xff),
                        'r' => new Color32(0xa8, 0x22, 0x36, 0xff),
                        'W' => new Color32(0xff, 0xc8, 0xd0, 0xff),
                        _ => new Color32(0, 0, 0, 0),
                    };
                    px[(h - 1 - r) * w + c] = col;
                }
            return PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0f), name);
        }
    }
}
