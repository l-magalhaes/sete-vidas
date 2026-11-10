using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O tomo sugando as vidas do gato (cena da maldição): o livro sai da pata, flutua aberto com as páginas
    /// virando e uma aura roxa, e puxa fiapos vermelhos de vida do corpo caído. A cada vida perdida, um coração
    /// inteiro é arrancado e voa até ele. No golpe do Koda, o tomo é repelido, fecha e cai no chão.
    /// Toda a arte é desenhada em código (pixel art, 16 px por unidade): não precisa de arquivo nem de prefab.
    /// </summary>
    public class TomeDrainFx : MonoBehaviour
    {
        const float Ppu = 16f;

        // paleta: capa e praga como o tomo do pedestal; vida em vermelho de coração
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Ink = new Color32(0x1a, 0x1c, 0x26, 0xff);
        static readonly Color32 Cover = new Color32(0x5b, 0x2a, 0x6e, 0xff);
        static readonly Color32 CoverDark = new Color32(0x3a, 0x17, 0x48, 0xff);
        static readonly Color32 Page = new Color32(0xe8, 0xe0, 0xc8, 0xff);
        static readonly Color32 PageShade = new Color32(0xb8, 0xab, 0x8c, 0xff);
        static readonly Color32 Plague = new Color32(0x8f, 0xd1, 0x4f, 0xff);
        static readonly Color32 PlagueLight = new Color32(0xd4, 0xf5, 0x9a, 0xff);
        static readonly Color32 Life = new Color32(0xe8, 0x47, 0x5a, 0xff);
        static readonly Color32 LifeLight = new Color32(0xff, 0xb3, 0xb8, 0xff);
        static readonly Color32 White = new Color32(0xff, 0xff, 0xff, 0xff);
        static readonly Color DrainedPurple = new Color32(0xb0, 0x6a, 0xe0, 0xff);

        SpriteRenderer _book, _aura;
        Sprite[] _open;       // páginas virando (loop)
        Sprite _shut;
        Sprite[] _wisp;       // fiapo de vida: forte → fraco
        Sprite _heart, _mote;
        int _layer, _order;

        Vector3 _hover;
        float _bump, _flash;
        bool _floating, _repelled;

        class Particle { public SpriteRenderer R; public Vector3 A, B, C; public float T, Duration, Size; public bool Heart; }
        readonly List<Particle> _live = new List<Particle>();
        readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();
        float _emitAccum;

        /// <summary>De onde sai a vida (o peito do gato caído, no mundo).</summary>
        public System.Func<Vector3> Source { get; set; }
        /// <summary>Puxando os fiapos de vida agora.</summary>
        public bool Draining { get; set; }
        public bool Visible => _book != null && _book.enabled;
        public Vector3 BookPosition => _book != null ? _book.transform.position : transform.position;

        public static TomeDrainFx Create(Transform parent, int sortingLayerId, int sortingOrder)
        {
            var go = new GameObject("Tomo sugando a vida");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<TomeDrainFx>();
            fx._layer = sortingLayerId;
            fx._order = sortingOrder;
            fx.Build();
            return fx;
        }

        void Build()
        {
            _open = new Sprite[4];
            for (int i = 0; i < 4; i++) _open[i] = Make("fx_tome_open_" + i, BookOpen(i), 22, 16, new Vector2(0.5f, 0.5f));
            _shut = Make("fx_tome_shut", BookShut(), 16, 8, new Vector2(0.5f, 0f));
            _wisp = new Sprite[3];
            for (int i = 0; i < 3; i++) _wisp[i] = Make("fx_life_wisp_" + i, Wisp(i), 5, 5, new Vector2(0.5f, 0.5f));
            _heart = Make("fx_life_heart", Heart(), 9, 8, new Vector2(0.5f, 0.5f));
            _mote = Make("fx_plague_mote", Mote(), 3, 3, new Vector2(0.5f, 0.5f));

            _aura = NewRenderer("Aura", Make("fx_tome_aura", Aura(40), 40, 40, new Vector2(0.5f, 0.5f)), _order);
            _book = NewRenderer("Livro", _open[0], _order + 1);
            _aura.enabled = _book.enabled = false;
        }

        // ------------------------------------------------------------ cena

        /// <summary>O tomo escapa da pata (<paramref name="from"/>) e sobe girando até <paramref name="hover"/>, abrindo.</summary>
        public IEnumerator Rise(Vector3 from, Vector3 hover, float duration)
        {
            _hover = hover;
            _repelled = false;
            _book.enabled = _aura.enabled = true;
            _book.transform.rotation = Quaternion.identity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                _book.transform.position = Vector3.Lerp(from, hover, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.3f;
                _book.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
                _book.sprite = _open[(int)(t * 20f) % _open.Length];
                _aura.transform.position = _book.transform.position;
                _aura.transform.localScale = Vector3.one * k;
                yield return null;
            }
            _book.transform.localScale = Vector3.one;
            _floating = true;
        }

        /// <summary>Arranca uma vida inteira: um coração sai do peito e voa até o tomo, que pulsa ao engolir.</summary>
        public void TearHeart()
        {
            if (!_floating || Source == null) return;
            Spawn(Source(), _heart, 0.55f, 1f, true);
            // um estouro de fiapos junto com o coração
            for (int i = 0; i < 6; i++) Spawn(Source() + (Vector3)(Random.insideUnitCircle * 0.3f), _wisp[0], Random.Range(0.4f, 0.6f), 1f, false);
        }

        /// <summary>
        /// O golpe sagrado do Koda: o tomo é jogado para trás, fecha e cai no chão em <paramref name="ground"/>.
        /// </summary>
        public IEnumerator Repel(Vector3 ground, float duration = 0.55f)
        {
            Draining = false;
            _floating = false;
            _repelled = true;
            Vector3 from = _book.transform.position;
            Vector3 peak = new Vector3((from.x + ground.x) * 0.5f, Mathf.Max(from.y, ground.y) + 1.2f, from.z);
            _book.sprite = _shut;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                var p = Bezier(from, peak, ground, k);
                _book.transform.position = p;
                _book.transform.rotation = Quaternion.Euler(0f, 0f, (1f - k) * 540f * Mathf.Sign(from.x - ground.x));
                var c = _aura.color; c.a = 1f - k; _aura.color = c;
                _aura.transform.position = p;
                yield return null;
            }
            _book.transform.position = ground;
            _book.transform.rotation = Quaternion.identity;
            _aura.enabled = false;
            // quica de leve ao cair
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                _book.transform.position = ground + Vector3.up * Mathf.Sin(t / 0.18f * Mathf.PI) * 0.15f;
                yield return null;
            }
            _book.transform.position = ground;
        }

        /// <summary>Some (o Contratante pega o tomo).</summary>
        public void Hide()
        {
            Draining = false;
            _floating = false;
            if (_book != null) _book.enabled = false;
            if (_aura != null) _aura.enabled = false;
        }

        // ------------------------------------------------------------ quadro

        void Update()
        {
            float dt = Time.deltaTime;
            if (_floating)
            {
                // flutua, páginas virando, aura pulsando; cresce ao engolir um coração
                _bump = Mathf.MoveTowards(_bump, 0f, dt * 2.5f);
                _flash = Mathf.MoveTowards(_flash, 0f, dt * 4f);
                Vector3 p = _hover + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);
                _book.transform.position = p;
                _book.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.2f) * 4f);
                _book.transform.localScale = Vector3.one * (1f + _bump * 0.25f);
                _book.sprite = _open[(int)(Time.time * (Draining ? 9f : 4f)) % _open.Length];
                _book.color = Color.Lerp(Color.white, new Color(1.5f, 1.2f, 1.6f), _flash);
                _aura.transform.position = p;
                _aura.transform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(Time.time * 5f) + _bump * 0.5f);
                _aura.color = new Color(1f, 1f, 1f, 0.55f + 0.2f * Mathf.Sin(Time.time * 4f) + _bump * 0.3f);
            }

            if (Draining && _floating && Source != null)
            {
                _emitAccum += dt * 16f;
                while (_emitAccum >= 1f)
                {
                    _emitAccum -= 1f;
                    var from = Source() + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(-0.15f, 0.25f), 0f);
                    Spawn(from, _wisp[0], Random.Range(0.55f, 0.85f), Random.Range(0.8f, 1.1f), false);
                }
                // brasas da praga orbitando o livro
                if (Random.value < dt * 6f)
                {
                    var a = Random.value * Mathf.PI * 2f;
                    var p0 = BookPosition + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.1f;
                    var pr = Spawn(p0, _mote, 0.7f, 1f, false);
                    pr.B = BookPosition + new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f) * 0.9f;
                }
            }

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var q = _live[i];
                q.T += dt;
                float k = Mathf.Clamp01(q.T / q.Duration);
                if (q.R.sprite != _mote) q.C = BookPosition; // o alvo acompanha o livro flutuando
                q.R.transform.position = Bezier(q.A, q.B, q.C, k * k * (3f - 2f * k));
                if (q.Heart)
                {
                    q.R.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, k) * (1f + 0.15f * Mathf.Sin(q.T * 30f));
                    q.R.color = Color.Lerp(Color.white, DrainedPurple, k);
                }
                else if (q.R.sprite == _mote)
                {
                    q.R.color = new Color(1f, 1f, 1f, 1f - k);
                }
                else
                {
                    q.R.sprite = _wisp[Mathf.Min(_wisp.Length - 1, (int)(k * _wisp.Length))];
                    q.R.transform.localScale = Vector3.one * q.Size;
                    q.R.color = Color.Lerp(Color.white, DrainedPurple, k);
                }
                if (k >= 1f)
                {
                    if (q.Heart && _floating) { _bump = 1f; _flash = 1f; }
                    Recycle(q.R);
                    _live.RemoveAt(i);
                }
            }
        }

        Particle Spawn(Vector3 from, Sprite sprite, float duration, float size, bool heart)
        {
            var r = _pool.Count > 0 ? _pool.Pop() : NewRenderer("Fiapo", sprite, _order + 2);
            r.enabled = true;
            r.sprite = sprite;
            r.color = Color.white;
            r.transform.localScale = Vector3.one * size;
            r.transform.position = from;
            Vector3 to = BookPosition;
            // curva: sobe e se abre para um lado antes de entrar no livro
            Vector3 mid = (from + to) * 0.5f;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.forward);
            Vector3 ctrl = mid + side * Random.Range(-1.1f, 1.1f) + Vector3.up * Random.Range(0.1f, 0.6f);
            var p = new Particle { R = r, A = from, B = ctrl, C = to, Duration = duration, Size = size, Heart = heart };
            _live.Add(p);
            return p;
        }

        void Recycle(SpriteRenderer r) { r.enabled = false; _pool.Push(r); }

        void OnDisable()
        {
            foreach (var q in _live) Recycle(q.R);
            _live.Clear();
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = _layer;
            sr.sortingOrder = order;
            return sr;
        }

        static Sprite Make(string name, Color32[] px, int w, int h, Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            tex.SetPixels32(px);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, Ppu);
            s.name = name;
            return s;
        }

        // ------------------------------------------------------------ arte

        // As grades são desenhadas com y para baixo (como no editor de pixel) e viradas na hora de gravar.
        class Grid
        {
            public readonly int W, H; readonly Color32[] _px;
            public Grid(int w, int h) { W = w; H = h; _px = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) _px[(H - 1 - y) * W + x] = c; }
            public bool Filled(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && _px[(H - 1 - y) * W + x].a > 0;
            public void Outline(Color32 c)
            {
                var add = new List<Vector2Int>();
                for (int x = 0; x < W; x++)
                    for (int y = 0; y < H; y++)
                        if (!Filled(x, y) && (Filled(x - 1, y) || Filled(x + 1, y) || Filled(x, y - 1) || Filled(x, y + 1))) add.Add(new Vector2Int(x, y));
                foreach (var p in add) Set(p.x, p.y, c);
            }
            public Color32[] Pixels => _px;
        }

        /// <summary>Livro aberto de frente, 22×16. Frames 1–3: uma página levanta da lombada e vira.</summary>
        static Color32[] BookOpen(int frame)
        {
            var g = new Grid(22, 16);
            const int spine = 11;
            // capas por baixo, abertas em V raso
            for (int x = 1; x <= 20; x++)
            {
                int top = 11 + (Mathf.Abs(x - spine) <= 1 ? 1 : 0);
                g.Set(x, top, Cover); g.Set(x, top + 1, CoverDark);
            }
            // páginas: um pouco mais altas perto da lombada, com um vinco no meio
            for (int x = 2; x <= 19; x++)
            {
                if (x == spine) continue;
                int d = Mathf.Abs(x - spine);
                int top = d <= 1 ? 5 : (d >= 7 ? 5 : 4);
                for (int y = top; y <= 10; y++) g.Set(x, y, y == 10 || d == 1 ? PageShade : Page);
            }
            for (int y = 6; y <= 11; y++) g.Set(spine, y, CoverDark);
            // linhas de texto
            for (int x = 3; x <= 18; x++)
            {
                if (Mathf.Abs(x - spine) <= 1) continue;
                if (((x * 7) % 5) != 0) g.Set(x, 6, PageShade);
                if (((x * 3) % 4) != 1) g.Set(x, 9, PageShade);
            }
            // runas da praga, uma em cada página; acendem alternadas
            Color32 a = frame % 2 == 0 ? PlagueLight : Plague, b = frame % 2 == 0 ? Plague : PlagueLight;
            Rune(g, 6, 7, a);
            Rune(g, 16, 7, b);
            // página virando
            if (frame > 0)
            {
                int tipX = frame == 1 ? 6 : frame == 2 ? spine : 16, tipY = frame == 2 ? 0 : 2;
                foreach (var p in Line(spine, 9, tipX, tipY))
                {
                    g.Set(p.x, p.y, Page);
                    g.Set(p.x + (tipX < spine ? 1 : tipX > spine ? -1 : 1), p.y, PageShade);
                }
            }
            g.Outline(Ink);
            return g.Pixels;
        }

        static void Rune(Grid g, int x, int y, Color32 c)
        {
            g.Set(x, y, c); g.Set(x - 1, y, c); g.Set(x + 1, y, c); g.Set(x, y - 1, c); g.Set(x, y + 1, c);
        }

        /// <summary>Fechado e deitado, 16×8 (base embaixo, como o do pedestal).</summary>
        static Color32[] BookShut()
        {
            var g = new Grid(16, 8);
            for (int x = 1; x <= 14; x++)
            {
                g.Set(x, 2, Cover); g.Set(x, 3, Cover); g.Set(x, 4, Cover);
                g.Set(x, 5, x == 1 || x == 14 ? CoverDark : Page);
                g.Set(x, 6, CoverDark);
            }
            g.Set(7, 3, Plague); g.Set(8, 3, Plague); g.Set(4, 3, Plague); g.Set(11, 3, Plague);
            g.Outline(Ink);
            return g.Pixels;
        }

        /// <summary>Fiapo de vida 5×5: cruz brilhante → losango → ponto.</summary>
        static Color32[] Wisp(int stage)
        {
            var g = new Grid(5, 5);
            if (stage == 0)
            {
                g.Set(2, 0, Life); g.Set(2, 4, Life); g.Set(0, 2, Life); g.Set(4, 2, Life);
                g.Set(1, 2, LifeLight); g.Set(3, 2, LifeLight); g.Set(2, 1, LifeLight); g.Set(2, 3, LifeLight);
                g.Set(2, 2, White);
            }
            else if (stage == 1)
            {
                g.Set(2, 1, Life); g.Set(1, 2, Life); g.Set(3, 2, Life); g.Set(2, 3, Life); g.Set(2, 2, LifeLight);
            }
            else g.Set(2, 2, LifeLight);
            return g.Pixels;
        }

        /// <summary>Coração arrancado 9×8, com brilho branco e contorno.</summary>
        static Color32[] Heart()
        {
            string[] rows =
            {
                ".........",
                "..XX.XX..",
                ".XhXXXXX.",
                ".XXXXXXX.",
                "..XXXXX..",
                "...XXX...",
                "....X....",
                ".........",
            };
            var g = new Grid(9, 8);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < 9; x++)
                {
                    char c = rows[y][x];
                    if (c == 'X') g.Set(x, y, Life);
                    else if (c == 'h') g.Set(x, y, White);
                }
            g.Outline(Ink);
            return g.Pixels;
        }

        static Color32[] Mote()
        {
            var g = new Grid(3, 3);
            g.Set(1, 0, Plague); g.Set(0, 1, Plague); g.Set(2, 1, Plague); g.Set(1, 2, Plague); g.Set(1, 1, PlagueLight);
            return g.Pixels;
        }

        /// <summary>Aura roxa em pontilhado (mais densa no centro), 40×40.</summary>
        static Color32[] Aura(int size)
        {
            var g = new Grid(size, size);
            float c = (size - 1) * 0.5f;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    if (d > 1f) continue;
                    // três anéis de pontilhado: cheio no meio, xadrez, e esparso na borda
                    bool on = d < 0.18f || (d < 0.7f && ((x + y) & 1) == 0) || (((x & 1) == 0) && ((y & 1) == 0));
                    if (!on) continue;
                    byte alpha = (byte)Mathf.Lerp(120f, 30f, d);
                    g.Set(x, y, new Color32(0x6b, 0x2a, 0x8a, alpha));
                }
            return g.Pixels;
        }

        static IEnumerable<Vector2Int> Line(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                yield return new Vector2Int(x0, y0);
                if (x0 == x1 && y0 == y1) yield break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
    }
}
