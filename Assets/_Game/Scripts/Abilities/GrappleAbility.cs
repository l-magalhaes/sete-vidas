using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>
    /// Flecha com corda do Caçador (F4b; como o gancho de Ocarina of Time): mira no direcional (8 direções; parado,
    /// para a frente) com mira automática no gancho mais próximo dentro de 30°. Se pegou num <see cref="GrappleAnchor"/>,
    /// a corda puxa o gato até ele; chegando, fica pendurado 0,4 s e pode pular (pulo cheio) ou dar dash. Sem gancho
    /// na mira, a corda vai até a parede ou o alcance e volta (não tira o controle). Vale em qualquer classe depois do
    /// vínculo com a Vulpis. Botão: Special (botão direito do mouse / Y).
    /// </summary>
    public class GrappleAbility : PlayerAbility
    {
        const float Buffer = 0.12f;
        /// <summary>Sem avanço no puxão por este tempo: travou (quina, teto) e solta.</summary>
        const float StuckTime = 0.12f;

        enum Phase { None, Shoot, Pull, Hang }

        Phase _phase;
        float _buffer;
        GrappleAnchor _anchor;
        Vector2 _tip;
        float _timer, _stuck, _lastDist;
        LineRenderer _rope;
        // corda que errou (só visual): vai e volta
        Vector2 _missEnd;
        float _missT = -1f;

        public bool IsGrappling => Player != null && Player.ActiveAbility == this;
        public bool IsHanging => IsGrappling && _phase == Phase.Hang;
        public bool IsPulling => IsGrappling && (_phase == Phase.Pull || _phase == Phase.Shoot);
        /// <summary>O arquinho do gancho está na mão (tirando e voltando a corrente): o arco das costas some.</summary>
        public bool BowInHand => (_bow != null && _bow.enabled) || IsGrappling || _missT >= 0f;

        static int s_wallMask = -1;
        static Material s_ropeMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_wallMask = -1; s_ropeMat = null; s_bowSprite = s_headSprite = null; s_chainTex = null; } // Play sem recarregar o domínio

        static int WallMask
        {
            get
            {
                if (s_wallMask < 0)
                    s_wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
                return s_wallMask;
            }
        }

        static readonly Color RopeColor = new Color32(0xc4, 0x98, 0x60, 0xff);

        // ---------------------------------------------------------------- visual (melhorias 4, 14/11/2026)
        // A flecha com corrente sai de um arco na mão do gato (em qualquer classe): a corrente chicoteia enquanto voa,
        // vibra ao cravar, fica esticada no puxão e, se errar, volta frouxa.

        const int Links = 18;
        const float ShotWave = 0.28f, SnapWave = 0.2f, SnapTime = 0.22f, MissSag = 0.45f, MissTime = 0.32f;

        SpriteRenderer _bow, _head;
        Vector2 _aim = Vector2.right;
        float _hookedAt = -1f;
        static Sprite s_bowSprite, s_headSprite;
        static Texture2D s_chainTex;

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Corrente do gancho");
            go.transform.SetParent(transform, false);
            _rope = go.AddComponent<LineRenderer>();
            if (s_ropeMat == null)
            {
                s_ropeMat = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
                s_ropeMat.mainTexture = ChainTexture();
            }
            _rope.sharedMaterial = s_ropeMat;
            _rope.textureMode = LineTextureMode.Tile;
            _rope.textureScale = new Vector2(4f, 1f);        // 4 elos por tile
            _rope.positionCount = Links;
            _rope.widthMultiplier = 3f / 16f;
            _rope.startColor = _rope.endColor = Color.white;
            _rope.useWorldSpace = true;
            _rope.sortingOrder = 6;
            _rope.numCapVertices = 0;
            _rope.enabled = false;

            _bow = NewSprite("Arco do gancho", BowSprite(), 8);
            _head = NewSprite("Ponta do gancho", HeadSprite(), 7);
        }

        SpriteRenderer NewSprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        /// <summary>Onde fica o arco: na frente do peito, para o lado da mira (a corrente sai daqui).</summary>
        Vector2 BowPoint(Vector2 aim) => Player.Body.position + new Vector2(0f, Player.CurrentHeight * 0.6f) + aim * 0.45f;
        Vector2 Hand => BowPoint(_aim);
        /// <summary>Onde ficam os pés do gato pendurado no gancho (as patas no gancho).</summary>
        Vector2 HangFeet(Vector2 anchor) => anchor + new Vector2(0f, -Player.Settings.StandingHeight * 0.9f);

        void Update()
        {
            if (Player == null || Player.Input == null) return;
            if (Player.Input.SpecialPressedThisFrame && HunterRules.MovesUnlocked(SaveManager.Data)) _buffer = Buffer;
            DrawRope();
        }

        void FixedUpdate()
        {
            _buffer -= Time.fixedDeltaTime;
            if (_missT >= 0f) _missT += Time.fixedDeltaTime;
        }

        public override bool TryBegin(bool grounded)
        {
            if (_buffer <= 0f || Player.IsCrouching) return false;
            _buffer = 0f;
            var input = Player.Input;
            Vector2 aim = HunterRules.AimDirection(input.MoveX, input.MoveY, Player.FacingDirection, grounded);
            if (aim.x != 0f) Player.Face(aim.x > 0f ? 1 : -1);
            _aim = aim;
            Vector2 from = Hand;
            _anchor = FindAnchor(from, aim);
            if (_anchor != null) _aim = (_anchor.Point - from).normalized;   // a mira automática vira o arco para o gancho
            _hookedAt = -1f;
            HitSparks.Burst(from, 4, Color.white, RopeColor, 1.5f, 3f, 0.15f, 0f, 0.15f); // estalo da corda do arco
            SeteVidas.Audio.Sfx.Play("flecha", from, 0.9f, 0.8f); // provisório: a flecha com corda
            if (_anchor == null)
            {
                // errou: a corda vai até a parede (ou o alcance) e volta; o gato segue livre
                var hit = Physics2D.Raycast(from, aim, HunterRules.GrappleRange, WallMask);
                _missEnd = hit.collider != null ? hit.point : from + aim * HunterRules.GrappleRange;
                _missT = 0f;
                if (hit.collider != null) SeteVidas.Audio.Sfx.Play("adaga_tink", hit.point, 0.6f, 1.3f);
                return false;
            }
            _missT = -1f;
            _phase = Phase.Shoot;
            _tip = from;
            Player.Timers.Consume();
            Player.Body.linearVelocity = Vector2.zero;
            return true;
        }

        /// <summary>O gancho que a mira pega: no alcance, no cone e sem parede no caminho.</summary>
        GrappleAnchor FindAnchor(Vector2 from, Vector2 aim)
        {
            GrappleAnchor best = null;
            float bestScore = float.MaxValue;
            var all = GrappleAnchor.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a == null || !a.isActiveAndEnabled) continue;
                float s = HunterRules.AnchorScore(from, aim, a.Point);
                if (s < 0f || s >= bestScore) continue;
                // o gancho fica na ponta da parede: o raio para um pouco antes dele
                Vector2 d = a.Point - from;
                var block = Physics2D.Raycast(from, d.normalized, Mathf.Max(0f, d.magnitude - 0.45f), WallMask);
                if (block.collider != null) continue;
                best = a;
                bestScore = s;
            }
            return best;
        }

        public override bool Step(float dt)
        {
            var body = Player.Body;
            if (_anchor == null) { Release(); return false; }
            Vector2 anchor = _anchor.Point;
            switch (_phase)
            {
                case Phase.Shoot:
                    body.linearVelocity = Vector2.zero; // o gato espera a flecha cravar (bem rápido)
                    _tip = Vector2.MoveTowards(_tip, anchor, HunterRules.GrappleShotSpeed * dt);
                    if ((_tip - anchor).sqrMagnitude < 0.0001f)
                    {
                        _phase = Phase.Pull;
                        _stuck = 0f;
                        _lastDist = float.MaxValue;
                        _anchor.OnHooked();
                        _hookedAt = Time.time;
                        CameraShake.Shake(1f, 0.08f);
                        HitSparks.Burst(anchor, 6, Color.white, new Color32(0xc8, 0xd0, 0xdc, 0xff), 2f, 5f, 0.2f, 10f, 0.25f);
                        SeteVidas.Audio.Sfx.Play("flecha_crava", anchor, 0.9f, 1.1f);
                    }
                    return true;
                case Phase.Pull:
                {
                    Vector2 target = HangFeet(anchor);
                    Vector2 v = HunterRules.PullVelocity(body.position, target);
                    float dist = Vector2.Distance(body.position, target);
                    if (v == Vector2.zero) { StartHang(target); return true; }
                    // não está chegando (quina, teto): solta
                    if (dist > _lastDist - 0.01f) _stuck += dt; else _stuck = 0f;
                    _lastDist = dist;
                    if (_stuck >= StuckTime) { Release(); return false; }
                    // não passa do alvo neste passo
                    if (v.magnitude * dt > dist) v = (target - body.position) / dt;
                    body.linearVelocity = v;
                    if (Mathf.Abs(v.x) > 0.1f) Player.Face(v.x > 0f ? 1 : -1);
                    _aim = (anchor - body.position).normalized;
                    // rastro de vento no puxão
                    if (Random.value < 0.5f) HitSparks.Rise(body.position + new Vector2(Random.Range(-0.3f, 0.3f), Player.CurrentHeight * Random.Range(0.2f, 0.8f)), new Color(1f, 1f, 1f, 0.6f), 0.5f, 0.2f);
                    return true;
                }
                default: // pendurado
                    body.linearVelocity = Vector2.zero;
                    _timer -= dt;
                    if (Player.Timers.HasBufferedJump)
                    {
                        Player.Timers.Consume();
                        float vx = Player.Input.MoveX * Player.Settings.MoveSpeed;
                        Release();
                        Player.StartJump(new Vector2(vx, Player.Settings.LaunchVelocity(dt)));
                        SeteVidas.Audio.Sfx.Play("gato_pulo", body.position);
                        return false;
                    }
                    // dash pedido: solta e deixa o DashAbility pegar no próximo passo
                    var dash = GetComponent<DashAbility>();
                    if ((dash != null && dash.WantsDash) || _timer <= 0f) { Release(); return false; }
                    return true;
            }
        }

        void StartHang(Vector2 feet)
        {
            _phase = Phase.Hang;
            _timer = HunterRules.GrappleHang;
            Player.Body.position = feet;
            Player.Body.linearVelocity = Vector2.zero;
            var dash = GetComponent<DashAbility>();
            if (dash != null) dash.RefillAir();
            HitSparks.Burst(_anchor.Point, 5, RopeColor, Color.white, 1f, 2.5f, 0.25f, 4f, 0.2f);
        }

        void Release()
        {
            _phase = Phase.None;
            _anchor = null;
        }

        public override void OnInterrupted() => Release();

        void DrawRope()
        {
            if (_rope == null) return;
            if (IsGrappling && _anchor != null)
            {
                Vector2 from = Hand, anchor = _anchor.Point;
                if (_phase == Phase.Shoot)
                {
                    float total = Mathf.Max(0.01f, Vector2.Distance(from, anchor));
                    float k = Mathf.Clamp01(Vector2.Distance(from, _tip) / total);
                    DrawChain(from, _tip, ShotWave * (1f - k), 0f, 3f, 40f);       // chicoteia e estica enquanto voa
                }
                else
                {
                    float since = _hookedAt >= 0f ? Time.time - _hookedAt : 99f;
                    float snap = since < SnapTime ? SnapWave * (1f - since / SnapTime) : 0f;
                    DrawChain(from, anchor, snap, _phase == Phase.Hang ? 0.06f : 0f, 5f, 70f); // vibra ao cravar
                }
                ShowBow(from, true);
                return;
            }
            // corrente que errou: vai chicoteando, bate e volta frouxa
            if (_missT >= 0f && _missT < MissTime)
            {
                float half = MissTime * 0.5f;
                Vector2 from = Hand;
                bool going = _missT < half;
                float k = going ? _missT / half : 1f - (_missT - half) / half;
                Vector2 tip = Vector2.Lerp(from, _missEnd, k);
                if (going) DrawChain(from, tip, ShotWave * (1f - k), 0f, 3f, 40f);
                else DrawChain(from, tip, 0.05f, MissSag * k, 2f, 20f);
                ShowBow(from, true);
                return;
            }
            _missT = -1f;
            _rope.enabled = false;
            if (_head != null) _head.enabled = false;
            ShowBow(Vector2.zero, false);
        }

        /// <summary>
        /// Corrente de <paramref name="a"/> a <paramref name="b"/>: onda que corre (<paramref name="wave"/> de amplitude,
        /// <paramref name="cycles"/> ondas, <paramref name="speed"/> rad/s) e barriga para baixo (<paramref name="sag"/>);
        /// as pontas ficam presas. A ponta do gancho vai no fim, virada para onde a corrente aponta.
        /// </summary>
        void DrawChain(Vector2 a, Vector2 b, float wave, float sag, float cycles, float speed)
        {
            _rope.enabled = true;
            Vector2 d = b - a;
            Vector2 n = d.sqrMagnitude > 1e-6f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
            float t0 = Time.time * speed;
            Vector2 prev = a;
            for (int i = 0; i < Links; i++)
            {
                float t = i / (float)(Links - 1);
                float env = Mathf.Sin(t * Mathf.PI);                           // presa nas pontas
                Vector2 p = a + d * t + n * (wave * env * Mathf.Sin(t * cycles * Mathf.PI * 2f - t0)) + Vector2.down * (sag * env);
                _rope.SetPosition(i, p);
                if (i == Links - 2) prev = p;
            }
            if (_head == null) return;
            _head.enabled = true;
            _head.transform.position = b;
            Vector2 dir = b - prev;
            if (dir.sqrMagnitude < 1e-6f) dir = d;
            _head.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void ShowBow(Vector2 at, bool show)
        {
            if (_bow == null) return;
            _bow.enabled = show;
            if (!show) return;
            _bow.transform.position = at;
            _bow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg);
        }

        // ---------------------------------------------------------------- arte em código (provisória)

        static readonly Color32 Clear = new Color32(0, 0, 0, 0), Iron = new Color32(0xc8, 0xd0, 0xdc, 0xff),
            IronDark = new Color32(0x5a, 0x60, 0x70, 0xff), Wood = new Color32(0x96, 0x6e, 0x40, 0xff),
            WoodDark = new Color32(0x52, 0x26, 0x1e, 0xff), BowString = new Color32(0xf2, 0xec, 0xd8, 0xff);

        /// <summary>Elos da corrente: 4 × 3 px (um elo de pé, um deitado), repetido ao longo da linha.</summary>
        static Texture2D ChainTexture()
        {
            if (s_chainTex != null) return s_chainTex;
            s_chainTex = new Texture2D(4, 3, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "corrente_gancho", hideFlags = HideFlags.DontSave
            };
            // linha 0 = embaixo
            var px = new[]
            {
                IronDark, Iron, Clear, IronDark,
                Iron, Clear, Iron, Iron,
                IronDark, Iron, Clear, IronDark,
            };
            s_chainTex.SetPixels32(px);
            s_chainTex.Apply(false);
            return s_chainTex;
        }

        /// <summary>Arco pequeno 5 × 11 px, de lado, com a barriga para a direita (gira com a mira).</summary>
        static Sprite BowSprite()
        {
            if (s_bowSprite != null) return s_bowSprite;
            const int w = 5, h = 11;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int dy = Mathf.Abs(y - 5);
                    int arcX = dy <= 1 ? 4 : dy <= 3 ? 3 : dy == 4 ? 2 : 1;   // a curva da madeira
                    Color32 c = Clear;
                    if (x == arcX) c = dy == 0 ? WoodDark : Wood;
                    else if (x == 0 && dy <= 4) c = BowString;                    // a corda
                    px[y * w + x] = c;
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_bowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.3f, 0.5f), 16f);
            s_bowSprite.hideFlags = HideFlags.DontSave;
            return s_bowSprite;
        }

        /// <summary>Ponta da flecha com gancho 7 × 5 px (ferro, com as farpas para trás), apontando para a direita.</summary>
        static Sprite HeadSprite()
        {
            if (s_headSprite != null) return s_headSprite;
            string[] rows =
            {
                ".#.....",
                "..#..##",
                "#######",
                "..#..##",
                ".#.....",
            };
            const int w = 7, h = 5;
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - r) * w + x] = rows[r][x] == '#' ? (r == 2 ? Iron : IronDark) : Clear;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_headSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.75f, 0.5f), 16f);
            s_headSprite.hideFlags = HideFlags.DontSave;
            return s_headSprite;
        }
    }
}
