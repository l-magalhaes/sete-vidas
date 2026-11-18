using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O corpo inteiro da Isyathy no Ninho, desenhado a cada quadro (<see cref="SerpentRaster"/> + <see cref="IsyathyArt"/>):
    /// a cabeça fica no ponto <see cref="S"/> do trilho (<see cref="SetPath"/>) e as fatias do corpo vão atrás dela pelo
    /// mesmo trilho, com uma onda correndo (<see cref="Amplitude"/>, <see cref="Phase"/>). Antes do começo do trilho, o
    /// corpo continua reto (o fundo da terra). Na boca do buraco (<see cref="SetClip"/>), o que está abaixo do chão
    /// não aparece. Fica na frente do chão (ordem 2), atrás da borda da cratera (3) e do gato.
    /// </summary>
    public class SerpentRig : MonoBehaviour
    {
        const int TexW = 640, TexH = 480;   // 40 × 30 tiles (cabe a arena da luta inteira: do teto ao chão)
        public const int Order = 2;

        /// <summary>
        /// Comprimento do corpo (tiles). A da luta é mais comprida (IsyathyBossRules.BodyLength):
        /// desce do teto até o chão sem o rabo sair da terra. Mudar antes de mostrar (as fatias são feitas de novo).
        /// </summary>
        public float Length = BurrowRules.BodyLength;

        /// <summary>
        /// Até onde a cabeça pode ir no trilho (<see cref="S"/> nunca passa disso): com o buraco no ponto H do trilho,
        /// H + comprimento − o pedaço que fica sempre na terra (pedido do usuário, 18/11/2026: "o corpo nunca sai inteiro").
        /// </summary>
        public float MaxS = float.PositiveInfinity;

        float _s;
        public float S { get => _s; set => _s = Mathf.Min(value, MaxS); }
        public float Amplitude, Phase;
        public int Frame = IsyathyArt.FrameSleep;
        public bool Glow = true;

        SerpentRaster _raster;
        Texture2D _tex;
        SpriteRenderer _sr;
        Vector2[] _path = new Vector2[0];
        SerpentDisc[] _discs;
        Vector2[] _pts;
        readonly List<Vector2> _holes = new List<Vector2>();   // as bocas de buraco abertas (superfície, mundo)
        static readonly Dictionary<int, uint[]> s_heads = new Dictionary<int, uint[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_heads.Clear(); }   // Play sem recarregar o domínio

        public Vector2[] Path => _path;
        public float PathLength { get; private set; }

        public bool Visible
        {
            get => _sr != null && _sr.enabled;
            set { Build(); _sr.enabled = value; }
        }

        void Awake() => Build();

        void Build()
        {
            if (_sr != null) return;
            _raster = new SerpentRaster(TexW, TexH);
            _tex = new Texture2D(TexW, TexH, TextureFormat.BGRA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "isyathy_corpo", hideFlags = HideFlags.DontSave };
            var clear = new uint[TexW * TexH];
            _tex.SetPixelData(clear, 0);
            _tex.Apply(false);
            var go = new GameObject("Isyathy (desenho)");
            go.transform.SetParent(transform, false);
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = Sprite.Create(_tex, new Rect(0, 0, TexW, TexH), Vector2.zero, 16f);
            _sr.sprite.hideFlags = HideFlags.DontSave;
            _sr.sortingOrder = Order;
            _sr.enabled = false;
            Slices();
        }

        /// <summary>As fatias para o <see cref="Length"/> atual (de novo se ele mudou).</summary>
        void Slices()
        {
            int n = Mathf.CeilToInt(Length / BurrowRules.DiscSpacing) + 1;
            if (_discs != null && _discs.Length == n) return;
            _discs = new SerpentDisc[n];
            _pts = new Vector2[n];
        }

        void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }

        public void SetPath(Vector2[] path)
        {
            _path = path ?? new Vector2[0];
            PathLength = BurrowRules.Length(_path);
        }

        /// <summary>A boca do buraco (a superfície, mundo): abaixo dela, perto do buraco, o corpo some.</summary>
        public void SetClip(Vector2 hole) { _holes.Clear(); _holes.Add(hole); }
        /// <summary>Mais uma boca de buraco (a varredura entra num e sai no outro).</summary>
        public void AddClip(Vector2 hole) { _holes.Add(hole); }
        public void ClearClip() { _holes.Clear(); }

        /// <summary>A cor do desenho (o golpe pisca, o espírito, a morte).</summary>
        public Color Tint { get { Build(); return _sr.color; } set { Build(); _sr.color = value; } }

        /// <summary>A ordem de desenho (a cena da flecha da Vulpis a põe por cima do escurecido).</summary>
        public int SortingOrder { get { Build(); return _sr.sortingOrder; } set { Build(); _sr.sortingOrder = value; } }

        Rect _room;
        bool _hasRoom;

        /// <summary>O interior da sala (mundo): o corpo só aparece dentro dele (a luta: sai do chão, das paredes e do teto).</summary>
        public void SetRoom(Rect inside) { _room = inside; _hasRoom = true; }
        public void ClearRoom() { _hasRoom = false; }
        public bool HasRoom => _hasRoom;

        /// <summary>O ponto está dentro da terra em volta da sala (mais que <paramref name="margin"/> tiles para fora).</summary>
        public bool OutsideRoom(Vector2 p, float margin)
        {
            if (!_hasRoom) return false;
            return p.x < _room.xMin - margin || p.x > _room.xMax + margin || p.y < _room.yMin - margin || p.y > _room.yMax + margin;
        }

        bool UnderHole(Vector2 p, float margin)
        {
            if (OutsideRoom(p, margin)) return true;
            foreach (var h in _holes)
                if (p.y < h.y - margin && Mathf.Abs(p.x - h.x) < BurrowRules.HoleHalfWidth + 1f) return true;
            return false;
        }

        /// <summary>
        /// A distância (tiles) do ponto até a pele do corpo (negativa = dentro), na pose do último <see cref="Render"/>;
        /// o que está embaixo da terra não conta. Para o corpo ferir o gato (a varredura).
        /// </summary>
        public float BodyDistance(Vector2 p)
        {
            if (!Visible || _pts == null) return float.MaxValue;
            float best = float.MaxValue;
            for (int k = 0; k < _pts.Length; k += 2)
            {
                float r = BurrowRules.Radius(k * BurrowRules.DiscSpacing, Length) / 16f;
                if (UnderHole(_pts[k], r * 0.5f)) continue;
                best = Mathf.Min(best, Vector2.Distance(p, _pts[k]) - r);
            }
            return best;
        }

        bool _flip;

        /// <summary>
        /// A cabeça espelhada (olhando para a esquerda). Com folga: apontando quase na vertical (descendo do teto, subindo do
        /// chão), ela mantém o lado de antes. Sem isso, o desenho virava de ponta-cabeça a cada quadro (bug relatado em
        /// 17/11/2026: "animações estranhas").
        /// </summary>
        bool Flipped(float dirX) => dirX < -FlipSlack || (dirX <= FlipSlack && _flip);
        const float FlipSlack = 0.3f;

        public Vector2 Neck => BurrowRules.PointAt(_path, S);

        /// <summary>Para onde a cabeça aponta: a corda do último pedaço do pescoço (vira suave nas quinas do trilho).</summary>
        public Vector2 HeadDirection
        {
            get
            {
                Vector2 d = BurrowRules.PointAt(_path, S) - BurrowRules.PointAt(_path, S - 0.7f);
                return d.sqrMagnitude > 1e-6f ? d.normalized : BurrowRules.DirectionAt(_path, S);
            }
        }

        /// <summary>A ponta do focinho (mundo): por onde ela morde.</summary>
        public Vector2 Snout
        {
            get
            {
                Vector2 dir = HeadDirection;
                var local = new Vector2(IsyathyArt.SnoutX - IsyathyArt.NeckX, IsyathyArt.NeckY - IsyathyArt.SnoutY) / 16f;
                if (Flipped(dir.x)) local.y = -local.y;
                float a = Mathf.Atan2(dir.y, dir.x);
                return Neck + new Vector2(local.x * Mathf.Cos(a) - local.y * Mathf.Sin(a), local.x * Mathf.Sin(a) + local.y * Mathf.Cos(a));
            }
        }

        static uint[] HeadPixels(int frame, bool glow)
        {
            int key = frame * 2 + (glow ? 1 : 0);
            if (!s_heads.TryGetValue(key, out var px)) { px = IsyathyArt.Head(frame, glow); s_heads[key] = px; }
            return px;
        }

        /// <summary>Desenha a pose atual (chamar depois de mudar <see cref="S"/>, a onda ou o quadro).</summary>
        public void Render()
        {
            Build();
            if (!_sr.enabled || _path.Length < 2) return;
            Slices();
            int n = _discs.Length;
            for (int k = 0; k < n; k++)
            {
                float d = k * BurrowRules.DiscSpacing;
                Vector2 p = BurrowRules.PointAt(_path, S - d);
                Vector2 dir = BurrowRules.DirectionAt(_path, S - d);
                _pts[k] = p + new Vector2(-dir.y, dir.x) * BurrowRules.Wave(d, Amplitude, Phase);
            }
            Vector2 neck = _pts[0] * 16f;
            float minX = neck.x - 80f, maxX = neck.x + 80f, minY = neck.y - 80f, maxY = neck.y + 80f;
            int m = 0;
            for (int k = n - 1; k >= 0; k--)
            {
                float d = k * BurrowRules.DiscSpacing;
                float r = BurrowRules.Radius(d, Length);
                Vector2 p = _pts[k];
                // fundo na terra, embaixo do buraco: nem entra na conta
                if (UnderHole(p, r / 16f + 0.1f)) continue;
                Vector2 dir = _pts[Mathf.Max(0, k - 1)] - _pts[Mathf.Min(n - 1, k + 1)];
                dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : BurrowRules.DirectionAt(_path, S - d);
                var px = p * 16f;
                _discs[m++] = new SerpentDisc { X = px.x, Y = px.y, DirX = dir.x, DirY = dir.y, R = r, U = d * 16f };
                minX = Mathf.Min(minX, px.x - r); maxX = Mathf.Max(maxX, px.x + r);
                minY = Mathf.Min(minY, px.y - r); maxY = Mathf.Max(maxY, px.y + r);
            }
            // o quadro: tudo o que couber, sempre com a cabeça dentro
            float ox = Mathf.Floor(minX) - 2f, oy = Mathf.Floor(minY) - 2f;
            if (maxX - ox > TexW) ox = Mathf.Clamp(ox, Mathf.Ceil(neck.x + 82f) - TexW, Mathf.Floor(neck.x - 82f));
            if (maxY - oy > TexH) oy = Mathf.Clamp(oy, Mathf.Ceil(neck.y + 82f) - TexH, Mathf.Floor(neck.y - 82f));
            _raster.Begin(ox, oy);
            _raster.SetClip(false, 0f, 0f, 0f);
            _raster.SetRoom(_hasRoom, _room.xMin * 16f, _room.yMin * 16f, _room.xMax * 16f, _room.yMax * 16f);
            foreach (var h in _holes) _raster.AddClip((h.x - BurrowRules.HoleHalfWidth) * 16f, (h.x + BurrowRules.HoleHalfWidth) * 16f, h.y * 16f);
            _raster.Body(_discs, m);
            Vector2 hd = HeadDirection;
            _flip = Flipped(hd.x);
            _raster.Head(HeadPixels(Frame, Glow), IsyathyArt.HeadW, IsyathyArt.HeadH, IsyathyArt.NeckX, IsyathyArt.NeckY,
                neck.x, neck.y, Mathf.Atan2(hd.y, hd.x), _flip);
            _raster.Finish();
            _tex.SetPixelData(_raster.Px, 0);
            _tex.Apply(false);
            _sr.transform.position = new Vector3(ox / 16f, oy / 16f, 0f);
        }
    }
}
