using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;
using static SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Serpente da Floresta (Docs/Design/floresta-bestiario.md; pedido do usuário, 13/11/2026: "serpentes pequenas"):
    /// escondida num buraco, só os olhos aparecem. Com o gato a 3 tiles em pé (agachado na folhagem ela não vê), chocalha
    /// (0,4 s) e dá o bote de 3 tiles; fica esticada 0,6 s (pogo e ponto fraco) e volta para o buraco (1,5 s, invulnerável).
    /// <para>Variação que <b>rasteja</b> (<see cref="crawls"/>): anda devagar entre dois pontos entre um bote e outro e
    /// apanha também fora do bote.</para>
    /// O corpo é um filho (<see cref="body"/>) com o colisor dos golpes; ele sai do buraco na direção do gato. Entre o
    /// buraco e a cabeça vão gomos (feitos na hora), para o corpo esticado ser contínuo e voltar deslizando.
    /// </summary>
    public class ForestSnake : ForestEnemy, IQuakeTarget
    {
        protected override string SoundName => "serpente";   // F10
        [SerializeField] Transform body;
        [SerializeField] Sprite hiddenSprite, warnSprite, strikeSprite, crawlSprite;
        [Tooltip("F4c: na toca (os olhos e a piscada), aviso (a língua), rastejando (o corpo ondulando)")]
        [SerializeField] Sprite[] hiddenFrames = new Sprite[0], warnFrames = new Sprite[0], crawlFrames = new Sprite[0];
        [Tooltip("F4c: gomo do corpo (16 × 16, emenda dos lados). Vazio = o feito na hora")]
        [SerializeField] Sprite segmentSprite;
        [Header("Rastejar (opcional)")]
        [SerializeField] bool crawls;
        [SerializeField] float minX, maxX;

        const int SegmentCount = 4;
        /// <summary>Meia largura do desenho do corpo (32 px a 16 px/tile).</summary>
        const float HeadHalf = 1f;

        SnakeState _state;
        float _timer;
        readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();
        static Sprite s_segment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_segment = null; s_wallMask = 0; s_lip = null; s_lipInfected = null; s_heads.Clear(); } // Play sem recarregar o domínio

        public SnakeState State => _state;
        public bool Crawls => crawls;
        public override bool AcceptsPogo => SnakeVulnerable(_state);
        protected override bool Invulnerable => SnakeInvulnerable(_state, crawls);

        /// <summary>
        /// Terremoto do Paladino (pedido de 14/11/2026): o tremor alcança a serpente dentro do buraco e ela morre na hora.
        /// Fora do buraco, leva o dano normal do terremoto.
        /// </summary>
        public void OnQuake(in DamageInfo info)
        {
            if (_state == SnakeState.Hidden || _state == SnakeState.Retreat) KillNow(info);
            else TakeDamage(info);
        }
        /// <summary>Os desenhos da serpente olham para a direita (olho na ponta direita).</summary>
        protected override bool ArtFacesRight => true;

        public void Configure(int facing, bool crawl, float a, float b)
        {
            startFacing = facing;
            crawls = crawl;
            minX = Mathf.Min(a, b);
            maxX = Mathf.Max(a, b);
            maxHealth = SnakeHealth;
        }

        /// <summary>Área de contato para os golpes do gato (tiles): bem maior que o desenho, a serpente é baixinha.</summary>
        const float HitHeight = 1.4f, MinHitWidth = 2.4f, HitMargin = 0.3f;
        BoxCollider2D _hitBox;

        protected override void Awake()
        {
            base.Awake();
            if (body != null) _hitBox = body.GetComponent<BoxCollider2D>();
            var seg = segmentSprite != null ? segmentSprite : SegmentSprite();
            for (int i = 0; i < SegmentCount; i++)
            {
                var go = new GameObject("Gomo " + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = seg;
                sr.sortingOrder = 3;
                sr.enabled = false;
                _segments.Add(sr);
            }
            // a boca da toca (melhorias 14/11/2026): terra levantada POR CIMA da ponta do corpo, que mergulha nela;
            // o corpo esticado sai de dentro da terra em vez de acabar no vazio
            if (!crawls && transform.Find("Toca") != null)
            {
                var lip = new GameObject("Toca: boca (frente)").AddComponent<SpriteRenderer>();
                lip.transform.SetParent(transform, false);
                lip.sprite = LipSprite(Infected.On(this));
                lip.sortingOrder = 5;          // na frente do corpo (4) e dos gomos (3)
                lip.enabled = false;
                _lip = lip;
            }
            // sem toca (a que rasteja): o corpo é uma curva só, do rabo à cabeça, desenhada a cada quadro
            if (crawls && transform.Find("Toca") == null)
            {
                _flesh = new GameObject("Corpo (curva)").AddComponent<SpriteRenderer>();
                _flesh.transform.SetParent(transform, false);
                _fleshTex = new Texture2D(FleshW, FleshH, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "serpente_corpo", hideFlags = HideFlags.DontSave };
                _fleshPx = new Color32[FleshW * FleshH];
                _fleshFill = new bool[FleshW * FleshH];
                var s = Sprite.Create(_fleshTex, new Rect(0, 0, FleshW, FleshH), new Vector2((float)FleshOx / FleshW, (float)FleshOy / FleshH), 16f);
                s.hideFlags = HideFlags.DontSave;
                _flesh.sprite = s;
                _flesh.sortingOrder = 3;       // atrás da cabeça (4)
            }
        }

        void OnDestroy()
        {
            if (_fleshTex != null) Destroy(_fleshTex);
        }

        SpriteRenderer _lip;
        static Sprite s_lip, s_lipInfected;

        /// <summary>
        /// Montinho de terra e musgo da boca da toca (22 × 10 px, pivot embaixo no meio, aberto para a direita):
        /// a cúpula cobre a ponta do corpo, a fresta escura do lado da cabeça é por onde ele sai.
        /// </summary>
        static Sprite LipSprite(bool infected)
        {
            var cached = infected ? s_lipInfected : s_lip;
            if (cached != null) return cached;
            const int w = 22, h = 10;
            Color32 clear = new Color32(0, 0, 0, 0), outline = new Color32(0x14, 0x10, 0x0e, 0xff),
                    mossDark = infected ? new Color32(0x3a, 0x2e, 0x44, 0xff) : new Color32(0x1f, 0x3a, 0x2c, 0xff),
                    moss = infected ? new Color32(0x5c, 0x48, 0x6a, 0xff) : new Color32(0x3c, 0x62, 0x4a, 0xff),
                    mossLight = infected ? new Color32(0x86, 0x6c, 0x96, 0xff) : new Color32(0x6e, 0x9a, 0x7c, 0xff),
                    dirt = new Color32(0x3a, 0x24, 0x20, 0xff), dirtDark = new Color32(0x24, 0x16, 0x14, 0xff),
                    hole = new Color32(0x08, 0x06, 0x08, 0xff), pebble = new Color32(0x6a, 0x58, 0x4a, 0xff);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // cúpula achatada: mais alta no meio-esquerdo, cai para a direita (a boca)
                    float cx = (x + 0.5f - 9f) / 11f, top = (h - 0.5f) * (1f - cx * cx);
                    Color32 c = clear;
                    if (y < top)
                    {
                        float depth = top - y;
                        c = depth < 1f ? outline : depth < 2.2f ? (((x * 3 + y) % 5 == 0) ? mossLight : moss)
                          : depth < 3.4f ? mossDark : ((x + y * 2) % 7 == 0 ? dirtDark : dirt);
                        if (y == 0) c = dirtDark;
                    }
                    // a boca: fresta escura do lado direito, na altura do corpo, com a borda de terra em volta
                    if (x >= 14 && x <= 18 && y >= 1 && y <= 5 && y < top - 0.6f) c = (x == 14 || y == 5) ? dirtDark : hole;
                    px[y * w + x] = c;
                }
            px[2 * w + 4] = pebble; px[1 * w + 9] = pebble; px[3 * w + 11] = pebble;   // pedrinhas na terra
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "serpente_toca_boca", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
            s.hideFlags = HideFlags.DontSave;
            if (infected) s_lipInfected = s; else s_lip = s;
            return s;
        }

        /// <summary>Gomo do corpo (1 tile, a mesma faixa verde do bote), feito na hora: não depende de arte nova.</summary>
        static Sprite SegmentSprite()
        {
            if (s_segment != null) return s_segment;
            const int n = 16;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "serpente_gomo", hideFlags = HideFlags.DontSave
            };
            Color32 green = new Color32(0x4a, 0x68, 0x46, 0xff), light = new Color32(0x9a, 0xc0, 0x5a, 0xff), clear = new Color32(0, 0, 0, 0);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int mid = 6 + (int)(2f * Mathf.Sin(x * 0.5f));
                    px[y * n + x] = Mathf.Abs(y - mid) <= 1 ? (x % 4 == 0 ? light : green) : clear;
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            s_segment = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
            s_segment.hideFlags = HideFlags.DontSave;
            return s_segment;
        }

        protected override void Revive()
        {
            base.Revive();
            _state = SnakeState.Hidden;
            _timer = 0f;
            _room = float.MaxValue;
            Pose(0f);
        }

        /// <summary>Espaço livre (tiles) à frente do buraco, travado no começo do bote.</summary>
        float _room = float.MaxValue;

        Vector2 Eye => (Vector2)transform.position + Vector2.up * 0.3f;

        static int s_wallMask;
        static int WallMask
        {
            get
            {
                if (s_wallMask == 0)
                    s_wallMask = LayerMask.GetMask(SeteVidas.Core.GameConstants.Layers.Ground,
                        SeteVidas.Core.GameConstants.Layers.Climbable, SeteVidas.Core.GameConstants.Layers.Breakable);
                return s_wallMask;
            }
        }

        /// <summary>Distância do buraco até a primeira parede na direção <paramref name="dir"/> (no máximo o bote inteiro).</summary>
        float RoomAhead(int dir)
        {
            float max = 0.4f + SnakeLunge + HeadHalf;
            var hit = Physics2D.Raycast(Eye, new Vector2(dir, 0f), max, WallMask);
            return hit.collider != null ? hit.distance : max;
        }

        protected override void OnDied()
        {
            foreach (var g in _segments) if (g != null) g.enabled = false;
            if (_flesh != null) _flesh.enabled = false;
        }

        /// <summary>Distância horizontal até o gato na altura do buraco (até 1,5 acima); negativa = longe.</summary>
        float PlayerDistance(out int side)
        {
            side = _facing;
            var h = Player;
            if (h == null || h.IsDead) return -1f;
            Vector2 d = h.transform.position - transform.position;
            if (d.y < -0.8f || d.y > 1.5f) return -1f;
            // não vê através de bloco (o gato em cima de um degrau, atrás da quina)
            if (Physics2D.Linecast(Eye, (Vector2)h.transform.position + Vector2.up * 0.5f, WallMask).collider != null) return -1f;
            side = d.x >= 0f ? 1 : -1;
            return Mathf.Abs(d.x);
        }

        protected override ForestSenseProfile SenseProfile => ForestSenseRules.Snake;
        protected override Vector2 EyePosition => Eye;
        protected override Vector3 IconOffset => new Vector3(0f, 1.6f, 0f);

        protected override void Tick(float dt)
        {
            float dist = PlayerDistance(out int side);
            if (!Aware) dist = -1f; // alerta (14/11/2026): só acorda e ataca depois do "!"
            var sil = Player != null ? Player.GetComponent<SeteVidas.Player.ShadowSilhouette>() : null;
            bool hidden = sil != null && sil.IsSilhouette;
            var before = _state;
            _state = StepSnake(_state, ref _timer, dt, dist, hidden, IsInfected);

            if (_state == SnakeState.Warn && before == SnakeState.Hidden)
            {
                _facing = side;
                Sound("aviso", transform.position);   // o sibilo (F10)
            }
            if (_state == SnakeState.Strike && before == SnakeState.Warn)
            {
                _room = RoomAhead(_facing);
                Sound("bote", transform.position);   // F10
            }

            if (_state == SnakeState.Warn && before == SnakeState.Hidden) _room = RoomAhead(_facing);
            if (_state == SnakeState.Hidden) _room = float.MaxValue; // a que rasteja muda de lugar: mede de novo no próximo bote
            if (crawls && _state == SnakeState.Hidden) Crawl(dt);

            float ext = SnakeExtension(_state, _timer, IsInfected);
            Pose(ext);
            if (SnakeBites(_state) && body != null)
            {
                var c = (Vector2)body.position;
                var bite = new Rect(c.x - 0.6f, c.y - 0.4f, 1.2f, 0.9f);
                // infectada: a mordida envenena (bestiário)
                if (IsInfected) HurtAndPoisonIn(bite); else HurtPlayerIn(bite);
            }

            if (spriteRenderer != null)
            {
                // fora do buraco (bote, esticada ou ainda voltando) mostra o corpo; só os olhos quando já entrou
                bool outside = _state == SnakeState.Strike || _state == SnakeState.Stretched || (_state == SnakeState.Retreat && ext > 0.02f);
                if (_flesh != null) Organic(dt, ext, outside);
                else
                {
                    Sprite rest = crawls ? Anim(crawlFrames, crawlSprite, 6f) : Anim(hiddenFrames, hiddenSprite, 2f);
                    Sprite want = outside ? strikeSprite : _state == SnakeState.Warn ? Anim(warnFrames, warnSprite, 7f) : rest;
                    if (want != null) spriteRenderer.sprite = want;
                }
            }
        }

        // ------------------------------------------------------------ corpo orgânico (a serpente sem toca)
        // Pedido do usuário (17/11/2026): "precisa ter o rabo dela quando não tem toca" e "deixe mais orgânica". O corpo
        // é uma curva só (SnakeSpine), do rabo afinando até o pescoço, desenhada em pixel a cada quadro; a cabeça é a
        // ponta dos desenhos de sempre (rastejando, a língua, a boca aberta). Rastejando, a onda corre pelo corpo; no
        // aviso ela se enrola em S e recua a cabeça; no bote o corpo estica e a onda alisa; voltando, enrola de novo.

        const int FleshW = 112, FleshH = 24, FleshOx = 40, FleshOy = 12;
        /// <summary>Coluna (px) onde começa a cabeça nos desenhos de 32 × 16; antes dela é o corpo.</summary>
        const int NeckColumn = 23;
        /// <summary>Linha (px, de cima) do meio da cabeça: rastejando, com a língua, no bote; e a do corpo deitado.</summary>
        const int RowCalm = 10, RowTongue = 8, RowStrike = 7, RowGround = 10;
        SpriteRenderer _flesh;
        Texture2D _fleshTex;
        Color32[] _fleshPx;
        bool[] _fleshFill;
        float _wave, _flick = 2f, _coil;
        int _headRow = RowCalm;
        static readonly Dictionary<Sprite, Sprite> s_heads = new Dictionary<Sprite, Sprite>();

        /// <summary>Só a cabeça (da coluna <see cref="NeckColumn"/> em diante), no mesmo lugar do desenho inteiro.</summary>
        static Sprite HeadOf(Sprite full)
        {
            if (full == null) return null;
            if (s_heads.TryGetValue(full, out var h) && h != null) return h;
            var r = full.rect;
            if (r.width <= NeckColumn) return full;
            var crop = new Rect(r.x + NeckColumn, r.y, r.width - NeckColumn, r.height);
            h = Sprite.Create(full.texture, crop, new Vector2((full.pivot.x - NeckColumn) / crop.width, full.pivot.y / crop.height), full.pixelsPerUnit);
            h.name = full.name + " (cabeça)";
            h.hideFlags = HideFlags.DontSave;
            s_heads[full] = h;
            return h;
        }

        void Organic(float dt, float ext, bool outside)
        {
            bool moving = _state == SnakeState.Hidden && maxX - minX >= 0.5f;
            _wave += dt * (moving ? 7f : outside ? 2.5f : _state == SnakeState.Warn ? 5f : 1.5f);
            float coilTarget = _state == SnakeState.Warn ? 1f : 0f;
            _coil = Mathf.MoveTowards(_coil, coilTarget, dt / (coilTarget > 0f ? SnakeWarn * 0.7f : 0.12f));

            Sprite calm = crawlFrames.Length > 0 && crawlFrames[0] != null ? crawlFrames[0] : crawlSprite;
            Sprite tongue = crawlFrames.Length > 1 && crawlFrames[1] != null ? crawlFrames[1] : calm;
            // de vez em quando, rastejando, ela levanta a cabeça e prova o ar
            _flick -= dt;
            if (_flick < -0.3f) _flick = Random.Range(1.8f, 3.5f);
            bool tasting = _state == SnakeState.Warn ? ((int)(Time.time * 7f) % 2 == 0) : _state == SnakeState.Hidden && _flick < 0f;
            Sprite full = outside ? strikeSprite : tasting ? tongue : calm;
            _headRow = outside ? RowStrike : tasting ? RowTongue : RowCalm;
            var head = HeadOf(full);
            if (head != null) spriteRenderer.sprite = head;
            Pose(ext);
        }

        /// <summary>Desenha o corpo de <see cref="_flesh"/> do rabo até o pescoço da cabeça (em <see cref="body"/>).</summary>
        void DrawFlesh(float ext)
        {
            var head = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (_flesh == null || head == null || body == null) return;
            _flesh.enabled = !IsDead;
            _flesh.flipX = _facing < 0;
            _flesh.color = spriteRenderer.color;
            // tudo em px, do lado para onde ela olha (x para a frente), a partir da raiz
            Vector2 b = body.localPosition * 16f;
            b.x *= _facing;
            float nx = b.x + 0.5f - head.pivot.x;                                  // 1 px dentro da cabeça: sem fresta
            float ny = b.y + (15 - _headRow) + 0.5f - head.pivot.y;
            float tx = 0.4f * 16f + 0.5f - head.pivot.x - SnakeSpine.RestLength + ext * SnakeSpine.TailCreep;
            float ty = 0.3f * 16f + (15 - RowGround) + 0.5f - head.pivot.y;
            float amp = SnakeSpine.Amplitude(ext, _coil), wave = SnakeSpine.Wavelength(ext, _coil);

            System.Array.Clear(_fleshFill, 0, _fleshFill.Length);
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < _fleshPx.Length; i++) _fleshPx[i] = clear;
            int prev = int.MinValue;
            for (int i = 0; i < FleshW; i++)
            {
                float x = i - FleshOx + 0.5f;
                if (x < tx || x > nx) { prev = int.MinValue; continue; }
                float y = SnakeSpine.Y(x, tx, ty, nx, ny, amp, wave, _wave);
                int jc = Mathf.FloorToInt(y + FleshOy);
                int half = SnakeSpine.Half(x, tx);
                int lo = jc - half, hi = jc + half;
                if (prev != int.MinValue) { lo = Mathf.Min(lo, prev); hi = Mathf.Max(hi, prev); }   // sem degrau aberto
                prev = jc;
                int k = ((int)(x - tx) + 600) % 6;
                for (int j = lo; j <= hi; j++)
                {
                    if (j < 0 || j >= FleshH) continue;
                    Color32 c = j == hi ? ScaleTop : j == lo ? ScaleBelly : ScaleBody;
                    if (half > 0 && j == jc && (k == 0 || k == 5)) c = ScaleDark;              // os losangos
                    if (half > 1 && j == jc + 1 && k == 2) c = ScaleDark;
                    _fleshPx[j * FleshW + i] = c;
                    _fleshFill[j * FleshW + i] = true;
                }
            }
            // contorno de 1 px em volta (como o Outline do gerador)
            for (int j = 0; j < FleshH; j++)
                for (int i = 0; i < FleshW; i++)
                {
                    int p = j * FleshW + i;
                    if (_fleshFill[p]) continue;
                    if ((i > 0 && _fleshFill[p - 1]) || (i < FleshW - 1 && _fleshFill[p + 1]) ||
                        (j > 0 && _fleshFill[p - FleshW]) || (j < FleshH - 1 && _fleshFill[p + FleshW]))
                        _fleshPx[p] = ScaleOutline;
                }
            _fleshTex.SetPixels32(_fleshPx);
            _fleshTex.Apply(false);
        }

        static readonly Color32 ScaleOutline = new Color32(0x0e, 0x0c, 0x10, 0xff), ScaleTop = new Color32(0x84, 0x7c, 0x48, 0xff),
            ScaleBody = new Color32(0x5e, 0x5a, 0x32, 0xff), ScaleBelly = new Color32(0xa8, 0x98, 0x6a, 0xff), ScaleDark = new Color32(0x22, 0x1e, 0x12, 0xff);

        /// <summary>A cabeça a <paramref name="ext"/> do bote; os gomos ligam o buraco à cabeça.</summary>
        void Pose(float ext)
        {
            // a ponta da cabeça nunca passa da parede: o bote para encostado nela
            float headX = Mathf.Min(0.4f + ext * SnakeLunge, _room - HeadHalf);
            if (body != null) body.localPosition = new Vector3(_facing * headX, 0.3f, 0f);
            if (_hitBox != null)
            {
                // área dos golpes generosa (pedido do usuário): do buraco até a ponta da cabeça, mais folga em volta
                float back = -Mathf.Max(headX, 0f) - HitMargin, tip = HeadHalf + HitMargin;
                float width = Mathf.Max(MinHitWidth, tip - back);
                float center = width > tip - back ? 0f : (back + tip) * 0.5f;
                _hitBox.size = new Vector2(width, HitHeight);
                _hitBox.offset = new Vector2(_facing * center, 0.15f);
            }
            float from = 0.3f, to = headX - HeadHalf + 0.4f;
            bool show = ext > 0.02f && to > from;
            if (_flesh != null)
            {
                // sem toca: a cabeça recua e levanta no aviso (o bote se arma), balança 1 px rastejando; o corpo é a curva
                show = false;
                float bob = _state == SnakeState.Hidden ? Mathf.Round(Mathf.Sin(_wave * 0.5f)) / 16f : 0f;
                if (body != null) body.localPosition += new Vector3(-_facing * SnakeSpine.CoilPullback * _coil, SnakeSpine.CoilRaise * _coil + bob, 0f);
            }
            for (int i = 0; i < _segments.Count; i++)
            {
                var g = _segments[i];
                if (g == null) continue;
                g.enabled = show;
                if (!show) continue;
                float t = _segments.Count == 1 ? 0.5f : i / (_segments.Count - 1f);
                // com a toca, o 1º gomo afunda (mergulha na terra, atrás da boca) e o 2º desce um pouco: a curva
                // (17/11/2026: afundava 4 px e a ponta do gomo, fora da borda de terra, passava do chão: 1 px basta)
                float dive = _lip == null ? 0f : i == 0 ? 0.0625f : 0f;
                g.transform.localPosition = new Vector3(_facing * Mathf.Lerp(from, to, t), 0.3f - dive, 0f);
                g.flipX = _facing < 0;
            }
            if (_lip != null)
            {
                // a boca só aparece com o corpo para fora; parada, a toca camuflada de sempre basta
                _lip.enabled = show;
                _lip.flipX = _facing < 0;   // a fresta fica do lado da cabeça
                _lip.transform.localPosition = new Vector3(_facing * -0.05f, -0.0625f, 0f);
            }
            if (_flesh != null) DrawFlesh(ext);
        }

        void Crawl(float dt)
        {
            if (maxX - minX < 0.5f) return;
            var p = transform.position;
            p.x += _facing * SnakeCrawlSpeed * dt;
            if (p.x >= maxX) { p.x = maxX; _facing = -1; }
            else if (p.x <= minX) { p.x = minX; _facing = 1; }
            transform.position = p;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0.9f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.4f, new Vector3(SnakeTriggerFor(Infected.On(this)) * 2f, 1.5f, 0f));
            if (crawls) Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0f), new Vector3(maxX, transform.position.y, 0f));
        }
#endif
    }
}
