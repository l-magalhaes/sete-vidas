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
    public class ForestSnake : ForestEnemy
    {
        [SerializeField] Transform body;
        [SerializeField] Sprite hiddenSprite, warnSprite, strikeSprite, crawlSprite;
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
        static void ResetStatics() { s_segment = null; s_wallMask = 0; } // Play sem recarregar o domínio

        public SnakeState State => _state;
        public bool Crawls => crawls;
        public override bool AcceptsPogo => SnakeVulnerable(_state);
        protected override bool Invulnerable => SnakeInvulnerable(_state, crawls);
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
            var seg = SegmentSprite();
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

        protected override void Tick(float dt)
        {
            float dist = PlayerDistance(out int side);
            var sil = Player != null ? Player.GetComponent<SeteVidas.Player.ShadowSilhouette>() : null;
            bool hidden = sil != null && sil.IsSilhouette;
            var before = _state;
            _state = StepSnake(_state, ref _timer, dt, dist, hidden);

            if (_state == SnakeState.Warn && before == SnakeState.Hidden)
            {
                _facing = side;
                SeteVidas.Audio.Sfx.Play("zumbi_sente", transform.position, 0.6f, 1.8f); // chocalho (provisório)
            }
            if (_state == SnakeState.Strike && before == SnakeState.Warn)
            {
                _room = RoomAhead(_facing);
                SeteVidas.Audio.Sfx.Play("zumbi_caca", transform.position, 0.5f, 1.8f); // bote (provisório)
            }

            if (_state == SnakeState.Warn && before == SnakeState.Hidden) _room = RoomAhead(_facing);
            if (_state == SnakeState.Hidden) _room = float.MaxValue; // a que rasteja muda de lugar: mede de novo no próximo bote
            if (crawls && _state == SnakeState.Hidden) Crawl(dt);

            float ext = SnakeExtension(_state, _timer);
            Pose(ext);
            if (SnakeBites(_state) && body != null)
            {
                var c = (Vector2)body.position;
                HurtPlayerIn(new Rect(c.x - 0.6f, c.y - 0.4f, 1.2f, 0.9f));
            }

            if (spriteRenderer != null)
            {
                // fora do buraco (bote, esticada ou ainda voltando) mostra o corpo; só os olhos quando já entrou
                bool outside = _state == SnakeState.Strike || _state == SnakeState.Stretched || (_state == SnakeState.Retreat && ext > 0.02f);
                Sprite rest = crawls ? crawlSprite : hiddenSprite;
                Sprite want = outside ? strikeSprite : _state == SnakeState.Warn ? warnSprite : rest;
                if (want != null) spriteRenderer.sprite = want;
            }
        }

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
            for (int i = 0; i < _segments.Count; i++)
            {
                var g = _segments[i];
                if (g == null) continue;
                g.enabled = show;
                if (!show) continue;
                float t = _segments.Count == 1 ? 0.5f : i / (_segments.Count - 1f);
                g.transform.localPosition = new Vector3(_facing * Mathf.Lerp(from, to, t), 0.3f, 0f);
                g.flipX = _facing < 0;
            }
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
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.4f, new Vector3(SnakeTrigger * 2f, 1.5f, 0f));
            if (crawls) Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0f), new Vector3(maxX, transform.position.y, 0f));
        }
#endif
    }
}
