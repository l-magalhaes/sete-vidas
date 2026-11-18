using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEngine;
using R = SeteVidas.Enemies.Boss.IsyathyBossRules;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// A Isyathy, a Devoradora Silenciosa (F8; plano em Docs/Floresta/plano-implementacao.md). A luta é na arena F1
    /// (Forest_F_Heart). Ela é a mesma anaconda corrompida do Ninho (<see cref="SerpentRig"/>) e luta de dentro da terra.
    /// Refeita em 17/11/2026 (pedido do usuário: mais fluida e previsível, como os chefes-serpente que andam pelas bordas):
    /// <list type="number">
    /// <item><b>Anda</b> dentro da terra pelas quatro bordas da arena (chão, paredes e teto): um montinho de terra corre pela
    /// superfície, rachando (no chão, encostar nele fere).</item>
    /// <item><b>Avisa</b> (a superfície racha e brilha), <b>sai e ataca</b>: o bote do chão, o avanço da parede, a descida
    /// do teto; na fase II, o Arco (a cabeça sai de um vão e o rabo de outro: dá para bater dos dois lados) e o Cuspe (três
    /// bolas que viram poças de veneno). O corpo nunca sai inteiro: o que está fora da sala (na terra) não aparece.</item>
    /// <item><b>Espera</b>, cansada: só aí a cabeça (e o rabo, no Arco) leva dano. Depois volta para a terra.</item>
    /// </list>
    /// Ao cruzar 65% e 30%, a praga a cobre (a Casca da Praga: imune por <see cref="IsyathyBossRules.ShellTime"/>); então o
    /// jogo pausa, a câmera vai até a Vulpis com zoom, ela pula e atira a flecha forte; a câmera segue a flecha até a cabeça,
    /// a casca quebra, o zoom volta e a luta continua com a cabeça atordoada.
    /// Morta: o último rugido, a cabeça desaba, o brilho dos olhos apaga; o espírito sobe e fala (D-ISYATHY-DERROTA e
    /// D-ISYATHY-ESPIRITO-01) e a flag <see cref="DeadFlag"/> abre a arena (o teto para a subida G1–G3 e a volta para a
    /// E6, atalhos com essa flag no ForestLayout). Morrer na luta reinicia tudo (o gato volta ao santuário).
    /// </summary>
    public class IsyathyBoss : MonoBehaviour, IBossBarSource
    {
        public const string DeadFlag = IsyathySighting.DeadFlag;
        const string Title = "Isyathy, a Devoradora Silenciosa";

        [Tooltip("A arena (mundo): a sala F1, com as paredes")]
        [SerializeField] Rect arena;
        [Tooltip("A altura do chão da arena (mundo)")]
        [SerializeField] float floorY;
        [Tooltip("Os vãos (x, mundo): colunas sem plataforma, de onde ela sai do chão e desce do teto")]
        [SerializeField] float[] holesX = new float[0];
        [Tooltip("A luta começa com o gato passando deste x (já dentro da arena)")]
        [SerializeField] float startX;
        [Tooltip("Onde a Vulpis fica (os pés, mundo), no alto de uma plataforma")]
        [SerializeField] Vector2 vulpisAt;
        [Tooltip("Onde a Vulpis chega para atirar (os pés, mundo): as plataformas dos lados; ela vem na mais perto do gato")]
        [SerializeField] Vector2[] vulpisSpots = new Vector2[0];
        [SerializeField] Sprite vulpisSprite;
        [Tooltip("As animações da Vulpis (VulpisAnimationSet); sem elas, fica o sprite parado")]
        [SerializeField] SeteVidas.Player.CharacterAnimationSet vulpisAnimations;

        enum State { Waiting, Intro, Fighting, Dying, Dead }

        State _state;
        int _hp = R.MaxHealth;
        int _phase = 1;
        IsyathyMove _last = IsyathyMove.Spit;
        bool _exposed, _stunned, _tailOut;
        bool _shell, _cinematic;
        float _shellSince;
        float _tailS;
        float _u;                 // onde ela está no perímetro (tiles)
        float _flashUntil;
        VulpisVisual _fox;
        SpriteRenderer _dim;
        int _rigOrder = SerpentRig.Order, _foxOrder = 4;
        SerpentRig _rig;
        IsyathyHead _head, _tail;
        BoxCollider2D _headCol, _tailCol;
        readonly List<CircleCollider2D> _bodyCols = new List<CircleCollider2D>();
        /// <summary>Quantas bolas de golpe no corpo (do pescoço para trás, a cada <see cref="BodyStep"/> tiles).</summary>
        const int BodyBoxes = 15;   // 18/11/2026: o corpo todo (do pescoço ao rabo, 32 tiles) leva dano
        const float BodyStep = 2.1f;
        SpriteRenderer _vulpis, _spirit;
        Coroutine _fight;
        IsyathyBurrow.Crater _craterA, _craterB, _impact;
        SpriteRenderer _mound;
        readonly List<SpriteRenderer> _cracks = new List<SpriteRenderer>();
        readonly List<float> _crackBorn = new List<float>();
        float _sinceCrack;
        PlayerHealth _health;
        PlayerController _player;
        static Sprite s_mound, s_crack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_mound = null; s_crack = null; s_plate = null; s_runes = null; }   // Play sem recarregar o domínio

        // ------------------------------------------------------------ barra do chefe

        public bool IsFighting => _state == State.Intro || _state == State.Fighting;
        public int Health => _hp;
        public int MaxHealth => R.MaxHealth;
        public float SecondPhaseFraction => R.PhaseTwoAt;
        public bool InSecondPhase => _phase >= 2;
        public string BarTitle => Title;
        public bool IsDead => _state == State.Dead || _state == State.Dying;
        /// <summary>A cabeça está fora da terra, dentro da arena (aceita pogo).</summary>
        public bool HeadOut => _rig != null && _rig.Visible && !_rig.OutsideRoom(_rig.Neck, -0.3f);
        /// <summary>O rabo está para fora (o Arco).</summary>
        public bool TailOut => _tailOut && _rig != null && _rig.Visible;

        /// <summary>O interior da arena (sem as paredes, o chão e o teto): fora dele é terra.</summary>
        Rect Inside => new Rect(arena.xMin + 1f, floorY, arena.width - 2f, arena.yMax - 1f - floorY);

        // as conversas com retrato (18/11/2026, pedido do usuário: "no modo porta-retrato, para não perdermos isso")
        [SerializeField] SeteVidas.Dialogue.DialogueDefinition talkFirstArrow, talkDefeat, talkSpirit, talkSpiritMore, talkFarewell;

        /// <summary>As conversas da luta (o editor cria: IsyathyBossSetup.CreateDialogues). Sem elas, ficam os balões.</summary>
        public void SetDialogues(SeteVidas.Dialogue.DialogueDefinition firstArrow, SeteVidas.Dialogue.DialogueDefinition defeat,
            SeteVidas.Dialogue.DialogueDefinition spirit, SeteVidas.Dialogue.DialogueDefinition spiritMore, SeteVidas.Dialogue.DialogueDefinition farewell)
        {
            talkFirstArrow = firstArrow; talkDefeat = defeat; talkSpirit = spirit; talkSpiritMore = spiritMore; talkFarewell = farewell;
        }

        /// <summary>
        /// Abre a conversa com retrato e espera ela fechar (o jogo pausa nela). Devolve false (na hora) se não deu para
        /// abrir: aí quem chamou usa os balões.
        /// </summary>
        IEnumerator Talk(SeteVidas.Dialogue.DialogueDefinition d, System.Action<bool> opened)
        {
            var runner = SeteVidas.Dialogue.DialogueRunner.Active;
            var input = FindPlayer() ? _player.GetComponent<PlayerInputReader>() : null;
            bool done = false;
            bool ok = d != null && runner != null && runner.Play(d, input, () => done = true);
            opened?.Invoke(ok);
            if (!ok) yield break;
            while (!done) yield return null;
        }

        public void Configure(Rect room, float floor, float[] holes, float start, Vector2 vulpisFeet, Vector2[] vulpisPlaces, Sprite vulpis, SeteVidas.Player.CharacterAnimationSet vulpisAnims)
        {
            arena = room; floorY = floor; holesX = holes; startX = start; vulpisAt = vulpisFeet; vulpisSpots = vulpisPlaces; vulpisSprite = vulpis; vulpisAnimations = vulpisAnims;
        }

        static bool Flag(string f) => SaveManager.Data != null && SaveManager.Data.HasFlag(f);

        void Start()
        {
            var rigGo = new GameObject("Isyathy (corpo)");
            rigGo.transform.SetParent(transform, false);
            _rig = rigGo.AddComponent<SerpentRig>();
            _rig.SetRoom(Inside);
            _rig.Length = R.BodyLength;   // mais comprida que a do Ninho: do teto ao chão, com o rabo na terra
            // a caixa da cabeça cobre o desenho (do pescoço ao focinho, 3,8 tiles; a altura da cabeça, 2,2)
            _headCol = HitBox("Isyathy (cabeça)", new Vector2(3.6f, 2.2f), out _head, IsyathyHead.Part.Head);
            _tailCol = HitBox("Isyathy (rabo)", new Vector2(1.4f, 2.6f), out _tail, IsyathyHead.Part.Tail);
            for (int i = 0; i < BodyBoxes; i++)
            {
                var go = new GameObject("Isyathy (corpo " + i + ")") { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
                go.transform.SetParent(transform, false);
                var c = go.AddComponent<CircleCollider2D>();
                c.isTrigger = true;
                c.enabled = false;
                go.AddComponent<IsyathyHead>().Bind(this, IsyathyHead.Part.Body);
                _bodyCols.Add(c);
                _solids.Add(Solid("Isyathy (corpo sólido " + i + ")"));
            }
            _headSolid = Solid("Isyathy (cabeça sólida)");
            _craterA = new IsyathyBurrow.Crater(transform, "Cratera da cabeça");
            _craterB = new IsyathyBurrow.Crater(transform, "Cratera do rabo");
            _impact = new IsyathyBurrow.Crater(transform, "Sombra da descida");
            BuildMound();
            BuildVulpis();
            _u = R.ParamOf(Inside, Hole(CenterHole()));
            if (Flag(DeadFlag)) { _state = State.Dead; ShowVulpis(true); }
            else { _state = State.Waiting; ShowVulpis(false); }
        }

        BoxCollider2D HitBox(string name, Vector2 size, out IsyathyHead part, IsyathyHead.Part kind)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            go.transform.SetParent(transform, false);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            col.enabled = false;
            part = go.AddComponent<IsyathyHead>();
            part.Bind(this, kind);
            return col;
        }

        void OnEnable() => GameEvents.WorldReset += OnWorldReset;

        void OnDisable()
        {
            GameEvents.WorldReset -= OnWorldReset;
            EndCinematic();
            ReleaseIntro();
            if (ReferenceEquals(BossBarSource.Active, this)) BossBarSource.Active = null;
        }

        /// <summary>O gato morreu (ou descansou) no meio da luta: tudo volta ao começo.</summary>
        void OnWorldReset()
        {
            if (_state == State.Dead || _state == State.Dying) return;
            StopAllCoroutines();
            _fight = null;
            _hp = R.MaxHealth; _phase = 1; _last = IsyathyMove.Spit;
            _exposed = _stunned = _shell = _tailOut = _barrierOn = false;
            if (_circle != null) Destroy(_circle.gameObject);
            EndCinematic();
            ReleaseIntro();
            _foxReady = false;
            if (_fox != null) { _fox.transform.position = vulpisAt; _fox.Release(); }
            if (_vulpis != null) _vulpis.color = Color.white;
            if (_rig != null) { _rig.Visible = false; _rig.ClearClip(); _rig.Tint = Color.white; }
            if (_headCol != null) _headCol.enabled = false;
            if (_tailCol != null) _tailCol.enabled = false;
            foreach (var c in _bodyCols) c.enabled = false;
            HideCraters();
            ShowMound(false);
            foreach (var p in FindObjectsByType<PoisonPuddle>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            ShowVulpis(false);
            _u = R.ParamOf(Inside, Hole(CenterHole()));
            _state = State.Waiting;
            if (ReferenceEquals(BossBarSource.Active, this)) BossBarSource.Active = null;
        }

        bool FindPlayer()
        {
            var h = PlayerHealth.Active;
            if (h != _health) { _health = h; _player = h != null ? h.GetComponent<PlayerController>() : null; }
            return _health != null && _player != null && !_health.IsDead;
        }

        Vector2 Cat => _player.Body.position + Vector2.up * (_player.CurrentHeight * 0.5f);
        Vector2 Feet => _player.Body.position;

        void Update()
        {
            if (_state == State.Waiting && FindPlayer() && arena.Contains(Cat) && Cat.x > startX && !CutsceneRunner.IsPlaying)
                StartCoroutine(Intro());
            if (_state == State.Fighting) RecordTrail(); else _trail.Clear();
            if (_rig == null) return;
            // a cabeça (o gatilho dos golpes) segue o desenho, girando com ela (sai da parede e do teto)
            bool out_ = HeadOut && _state != State.Dead;
            _headCol.enabled = out_;
            if (out_)
            {
                Vector2 d = _rig.HeadDirection;
                _headCol.transform.position = (_rig.Neck + _rig.Snout) * 0.5f;
                _headCol.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            bool headSolid = out_ && R.BodyIsSolid && _state == State.Fighting && !_cinematic && !_rig.OutsideRoom((_rig.Neck + _rig.Snout) * 0.5f, -0.4f);
            PlaceSolid(_headSolid, headSolid, (_rig.Neck + _rig.Snout) * 0.5f, 0.8f);
            bool tail = TailOut && _state == State.Fighting;
            _tailCol.enabled = tail;
            if (tail) _tailCol.transform.position = BurrowRules.PointAt(_rig.Path, _tailS);
            UpdateBodyBoxes();
            if (Time.time > _flashUntil && _rig.Visible && _state != State.Dying && !_cinematic) _rig.Tint = _shell ? ShellTint() : PhaseTint();
            if (_shell && _rig.Visible && Time.timeScale > 0f && Random.value < Time.deltaTime * 14f)
                HitSparks.One(_rig.Neck + Random.insideUnitCircle * 0.9f, new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 3f)), ForestMagic.Shell, 0.5f, 0f);
            FadeCracks();
            FoxReadyGlow();
            UpdateBarrier();
        }

        Color PhaseTint() => _phase >= 3 ? new Color(0.92f, 0.78f, 1f) : _phase == 2 ? new Color(0.97f, 0.88f, 1f) : Color.white;

        /// <summary>A Casca da Praga: o corpo pulsa num roxo escuro e brilhante.</summary>
        Color ShellTint() => Color.Lerp(PhaseTint(), new Color(0.74f, 0.46f, 1f), 0.55f + 0.3f * Mathf.Sin(Time.time * 7f));

        // ------------------------------------------------------------ a entrada e a luta

        /// <summary>
        /// A apresentação (18/11/2026, pedido do usuário: "ao entrar na arena, foco no chefe, ela ruge e uma animação de
        /// apresentação antes da luta"): o controle trava e a câmera vai até o meio da arena com zoom; o chão treme e racha;
        /// ela sobe devagar, imensa, de olhos fechados, abre os olhos e encara o gato; ruge (a câmera treme e dá um soco de
        /// zoom, ondas roxas saem da boca, terra cai do teto); o letreiro com o nome dela entra; a câmera volta para o gato,
        /// que comenta, a barra dela aparece e a luta começa.
        /// </summary>
        IEnumerator Intro()
        {
            _state = State.Intro;
            Vector2 hole = Hole(CenterHole());
            PlayerInputReader input = null;
            if (FindPlayer())
            {
                input = _player.GetComponent<PlayerInputReader>();
                if (input != null) input.SetLock(this, true);
                _health.Shielded = true;
            }
            bool cam = CinematicCamera.Begin();
            Vector2 home = cam ? CinematicCamera.Home : hole;
            Vector2 focus = hole + new Vector2(0f, 7f);
            try
            {
                // 1. a câmera vai até ela (o chão já treme)
                CameraShake.Shake(1.2f, 1.4f);
                yield return CamTo(home, focus, 1f, 1.5f, 1f);
                // 2. o chão racha no meio da arena
                float t = 0f;
                while (t < 1.1f)
                {
                    t += Time.deltaTime;
                    _craterA.Show(hole, Mathf.Lerp(0.2f, 0.9f, t / 1.1f), false);
                    if (Random.value < Time.deltaTime * 18f)
                        HitSparks.One(hole + new Vector2(Random.Range(-1.5f, 1.5f), 0.1f), new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 6f)), Dirt(), 0.6f, 25f);
                    yield return null;
                }
                _craterA.Show(hole, 1f, true);
                CameraShake.Shake(2.5f, 1.4f);
                HitSparks.Burst(hole + Vector2.up * 0.3f, 28, Dirt(), new Color(0.35f, 0.25f, 0.33f), 3f, 10f, 0.9f, 25f, 1.3f);
                // 3. ela sobe devagar, imensa, de olhos fechados
                var path = BurrowRules.Smooth(new[] { hole + Vector2.down * 12f, hole, hole + new Vector2(-0.8f, 4f), hole + new Vector2(0.6f, 8f), hole + new Vector2(-1.2f, 10.5f) });
                _rig.SetPath(path);
                _rig.ClearClip();
                _rig.MaxS = R.MaxHead(12f);   // o buraco a 12 tiles do começo do trilho
                _rig.Frame = IsyathyArt.FrameSleep;
                _rig.Glow = false;
                _rig.Visible = true;
                yield return Move(9f, BurrowRules.Length(path), 2.4f, BurrowRules.EaseOutBack, 0.15f);
                // 4. abre os olhos (o brilho roxo acende, piscando) e encara o gato
                yield return new WaitForSeconds(0.25f);
                _rig.Frame = IsyathyArt.FrameAwake;
                for (int i = 0; i < 3; i++) { _rig.Glow = i % 2 == 0; _rig.Render(); yield return new WaitForSeconds(0.12f); }
                _rig.Glow = true;
                var basePath = _rig.Path;
                t = 0f;
                while (t < 0.7f)
                {
                    t += Time.deltaTime;
                    _rig.SetPath(Looking(basePath, 1.1f * BurrowRules.EaseInOut(t / 0.7f) + 0.01f));
                    _rig.S = _rig.PathLength;
                    Animate(0.06f, 0f);
                    yield return null;
                }
                // 5. o rugido: a câmera treme, um soco de zoom, ondas roxas saindo da boca, terra caindo do teto
                _rig.Frame = IsyathyArt.FrameBite;
                CameraShake.Shake(4.5f, 1.8f);
                StartCoroutine(TitleCard(Title, 3.2f));
                float s0 = _rig.S, nextRing = 0f;
                t = 0f;
                while (t < 1.8f)
                {
                    t += Time.deltaTime;
                    _rig.S = s0 + 0.12f * Mathf.Sin(t * 40f);
                    if (cam) CinematicCamera.SetZoom(1.5f + 0.35f * Mathf.Sin(Mathf.Min(1f, t / 0.5f) * Mathf.PI));
                    if (t >= nextRing)
                    {
                        nextRing = t + 0.3f;
                        PixelRing.Spawn(_rig.Snout, 6, 70, 0.6f, new Color32(0xc4, 0x7b, 0xff, 0xff), 2);
                    }
                    if (Random.value < Time.deltaTime * 14f)
                        HitSparks.One(new Vector2(Random.Range(arena.xMin + 2f, arena.xMax - 2f), arena.yMax - 1.5f), new Vector2(0f, -2f), new Color32(0x6e, 0x60, 0x58, 0xff), 1.6f, 20f);
                    Animate(0.14f, 0f);
                    yield return null;
                }
                _rig.S = s0;
                _rig.Frame = IsyathyArt.FrameAwake;
                yield return Hold(1.3f, 0.1f, false);   // o letreiro termina de aparecer
                // 6. a câmera volta para o gato
                if (cam) yield return CamTo(CinematicCamera.Position, home, CinematicCamera.Zoom, 1f, 0.9f);
            }
            finally
            {
                if (cam) CinematicCamera.End();
                if (input != null) input.SetLock(this, false);
                if (_health != null) _health.Shielded = false;
            }
            BossBarSource.Active = this;   // a barra dela aparece
            if (FindPlayer()) SpeechBubble.Show(_player.transform, "Ela é... maior do que a floresta.", 2.2f);
            yield return Move(_rig.S, 2f, 1f, BurrowRules.EaseIn, 0.2f);
            HideBody();
            HideCraters();
            _u = R.ParamOf(Inside, hole);
            _state = State.Fighting;
            _fight = StartCoroutine(Fight());
        }

        /// <summary>Se a apresentação for cortada no meio (a cena trocou), devolve a câmera e o controle.</summary>
        void ReleaseIntro()
        {
            if (_state != State.Intro) return;
            if (CinematicCamera.Active && !_cinematic) CinematicCamera.End();
            if (_player != null) { var input = _player.GetComponent<PlayerInputReader>(); if (input != null) input.SetLock(this, false); }
            if (_health != null) _health.Shielded = false;
        }

        /// <summary>A câmera de cena indo de um ponto a outro, com o zoom junto.</summary>
        IEnumerator CamTo(Vector2 from, Vector2 to, float zoomFrom, float zoomTo, float seconds)
        {
            if (!CinematicCamera.Active) yield break;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = BurrowRules.EaseInOut(t / seconds);
                CinematicCamera.MoveTo(Vector2.Lerp(from, to, k));
                CinematicCamera.SetZoom(Mathf.Lerp(zoomFrom, zoomTo, k));
                yield return null;
            }
        }

        /// <summary>
        /// O letreiro da apresentação, na fonte do jogo: "ISYATHY" grande em ouro e, embaixo, o título em lilás, entre duas
        /// faixas de ouro com o degrau maia em jade; entra deslizando, fica e some.
        /// </summary>
        IEnumerator TitleCard(string title, float seconds)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            string name = title, sub = "";
            int comma = title.IndexOf(',');
            if (comma > 0) { name = title.Substring(0, comma); sub = title.Substring(comma + 1).Trim(); }
            var root = new GameObject("Letreiro do chefe").transform;
            root.SetParent(transform, false);
            var parts = new List<SpriteRenderer>
            {
                Text(root, name.ToUpperInvariant(), new Color32(0xff, 0xd8, 0x5a, 0xff), 2f, 0f),
                Text(root, sub, new Color32(0xe0, 0xb8, 0xff, 0xff), 1f, -1.6f),
                Band(root, 1.1f), Band(root, -2.5f),
            };
            float t = 0f;
            try
            {
                while (t < seconds)
                {
                    t += Time.deltaTime;
                    float inK = Mathf.Clamp01(t / 0.35f), outK = Mathf.Clamp01((seconds - t) / 0.4f), a = Mathf.Min(inK, outK);
                    // embaixo do meio da tela, seguindo a câmera (o tamanho acompanha o zoom)
                    float halfH = cam.orthographicSize;
                    Vector3 c = cam.transform.position;
                    root.position = PixelFont.Snap(new Vector3(c.x - (1f - BurrowRules.EaseOut(inK)) * 3f, c.y - halfH * 0.45f, 0f));
                    float scale = halfH / 8.4375f;
                    root.localScale = new Vector3(scale, scale, 1f);
                    foreach (var sr in parts) if (sr != null) sr.color = new Color(1f, 1f, 1f, a);
                    yield return null;
                }
            }
            finally { Destroy(root.gameObject); }
        }

        SpriteRenderer Text(Transform parent, string text, Color32 color, float scale, float y)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var sr = new GameObject("texto").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.sprite = PixelFont.Render(text, color, PixelFont.Outline);
            sr.sortingOrder = 40;
            return sr;
        }

        /// <summary>A faixa de ouro com o degrau maia (em jade), em cima e embaixo do nome; as pontas somem em pontilhado.</summary>
        SpriteRenderer Band(Transform parent, float y)
        {
            const int w = 160, h = 4;
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
                for (int yy = 0; yy < h; yy++)
                {
                    float fade = Mathf.Min(x, w - 1 - x) / 24f;
                    if (fade < 1f && ((x + yy) % 2 == 0 || fade < 0.4f)) continue;
                    bool step = (x / 3) % 4 == 0 ? yy == 1 : ((x / 3) % 4 == 2 && yy != 1);
                    px[yy * w + x] = yy == 0 || yy == h - 1 ? new Color32(0xb0, 0x7a, 0x1c, 0xff) : (step ? new Color32(0x36, 0xc8, 0xa0, 0xff) : new Color32(0xf0, 0xc0, 0x40, 0xff));
                }
            var sr = new GameObject("faixa").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.sprite = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0.5f), "faixa_letreiro");
            sr.sortingOrder = 40;
            return sr;
        }

        /// <summary>O ciclo: anda pelas bordas, avisa, sai, ataca, espera cansada e volta para a terra.</summary>
        IEnumerator Fight()
        {
            int chained = 0;
            yield return new WaitForSeconds(0.6f);
            while (_state == State.Fighting)
            {
                int phase = R.Phase(_hp, R.MaxHealth);
                if (phase != _phase) { _phase = phase; yield return Roar(); }
                if (R.VulpisBreaksShell(_shell, Time.time - _shellSince))
                {
                    yield return VulpisStrike();   // a flecha forte quebra a casca
                    chained = 0;
                    continue;
                }
                var move = R.Pick(_phase, _last, Random.value);
                _last = move;
                bool chain = R.Chains(_phase, chained);
                // anda dentro da terra, pelas bordas, direto até o lugar do golpe
                yield return Travel(move);
                if (_state != State.Fighting) yield break;
                bool finisher = _phase >= 3 && !chain;   // o último da sequência da fase III tira um coração
                yield return Do(move, finisher);
                if (chain) { chained++; continue; }
                chained = 0;
                yield return new WaitForSeconds(R.Rest(_phase));
            }
        }

        IEnumerator Do(IsyathyMove m, bool finisher)
        {
            switch (m)
            {
                case IsyathyMove.Lunge: return Strike(m, _spot, _normal, finisher);
                case IsyathyMove.WallStrike: return Strike(m, _spot, _normal, finisher);
                case IsyathyMove.CeilingDrop: return CeilingDrop(_spot.x, finisher);
                case IsyathyMove.Arch: return Arch(finisher);
                default: return Spit(_spot, _normal);
            }
        }

        // ------------------------------------------------------------ andando pelas bordas, dentro da terra

        Vector2 _spot, _normal;

        /// <summary>De onde sai o golpe <paramref name="m"/> agora (o gato pode ter mudado de lugar).</summary>
        Vector2 SpotFor(IsyathyMove m, out Vector2 normal)
        {
            var r = Inside;
            Vector2 cat = FindPlayer() ? Cat : r.center;
            switch (m)
            {
                case IsyathyMove.WallStrike:
                case IsyathyMove.Spit:
                {
                    bool left = R.SideOf(m, cat.x, r) == ArenaSide.Left;
                    float y = m == IsyathyMove.Spit ? Mathf.Clamp(cat.y + 3f, r.yMin + 4f, r.yMax - 5f) : R.WallHeight(cat.y - 0.2f, r);
                    normal = left ? Vector2.right : Vector2.left;
                    return new Vector2(left ? r.xMin : r.xMax, y);
                }
                case IsyathyMove.CeilingDrop:
                {
                    // o vão do meio tem os galhos da saída em cima (G1): do teto, ela desce pelos outros
                    normal = Vector2.down;
                    int center = CenterHole(), best = -1;
                    for (int i = 0; i < holesX.Length; i++)
                        if ((i != center || holesX.Length < 3) && (best < 0 || Mathf.Abs(holesX[i] - cat.x) < Mathf.Abs(holesX[best] - cat.x))) best = i;
                    return new Vector2(holesX[Mathf.Max(0, best)], r.yMax);
                }
                case IsyathyMove.Arch:
                    normal = Vector2.up;
                    return Hole(R.ArchHoles(holesX, cat.x).x);
                default:
                    normal = Vector2.up;
                    return Hole(R.NearHole(holesX, cat.x, 3.5f));
            }
        }

        /// <summary>
        /// Anda dentro da terra: o montinho corre pelas bordas (chão → parede → teto → parede) direto até o lugar do golpe
        /// (que segue o gato até faltar pouco), correndo quando está longe e chegando devagar (dá para ler onde vai sair).
        /// </summary>
        IEnumerator Travel(IsyathyMove move)
        {
            var r = Inside;
            _spot = SpotFor(move, out _normal);
            float target = R.ParamOf(r, _spot);
            int dir = R.ShortestDir(r, _u, target);
            bool locked = false;
            ShowMound(true);
            CameraShake.Shake(1f, 0.25f);
            while (_state == State.Fighting)
            {
                if (!locked)
                {
                    _spot = SpotFor(move, out _normal);
                    target = R.ParamOf(r, _spot);
                }
                float left = R.Distance(r, _u, target, dir), other = R.Distance(r, _u, target, -dir);
                if (!locked && other < left * 0.6f) { dir = -dir; left = other; }   // dá meia-volta pelo caminho curto
                if (left < 5f) locked = true;
                float step = R.TravelSpeed(_phase, left) * Time.deltaTime;
                if (left <= step) { _u = target; MoundAt(_u, dir, left); break; }
                _u += dir * step;
                MoundAt(_u, dir, step);
                yield return null;
            }
            ShowMound(false);
        }

        // ------------------------------------------------------------ os golpes

        /// <summary>A tangente da borda (para o lado do gato).</summary>
        Vector2 TowardCat(Vector2 at, Vector2 n)
        {
            var t = new Vector2(n.y, -n.x);
            if (FindPlayer() && Vector2.Dot(Cat - at, t) < 0f) t = -t;
            return t;
        }

        /// <summary>
        /// O Bote (do chão) e o Avanço (da parede): avisa, sai e se arma inclinada para trás, mira acompanhando o gato, dá o
        /// bote e cai cansada (com o queixo no chão, ou pendurada da parede). Espera e volta.
        /// </summary>
        IEnumerator Strike(IsyathyMove m, Vector2 at, Vector2 n, bool finisher)
        {
            if (!FindPlayer()) yield break;
            Vector2 tan = TowardCat(at, n);
            yield return Warn(at, n, R.Warning(m, _phase), _craterA);
            Vector2[] Rise(Vector2 off) => BurrowRules.Smooth(new[]
            {
                at - n * 10f, at - n * 1.5f, at, at + n * 1.7f + tan * 0.25f, at + n * R.RearOut - tan * 0.6f + off,
            });
            var path = Rise(Vector2.zero);
            Emerge(at, n, path, _craterA);
            yield return Move(8f, _rig.PathLength, R.RiseTime, BurrowRules.EaseOutBack, 0.12f);
            // mira: acompanha o gato e recua para armar (os olhos piscam: vem aí)
            float t = 0f, aim = R.AimTime(_phase);
            Vector2 off = Vector2.zero;
            while (t < aim)
            {
                t += Time.deltaTime;
                float k = t / aim;
                Vector2 to = FindPlayer() ? Cat - path[path.Length - 1] : Vector2.zero;
                Vector2 want = Vector2.ClampMagnitude(to * 0.05f, 0.35f) + (-tan * 0.7f + n * 0.25f) * BurrowRules.EaseInOut(k);
                off = Vector2.Lerp(off, want, 1f - Mathf.Exp(-Time.deltaTime * 10f));
                // encara o gato (a cabeça aponta para ele) e treme armando, como quem vai dar o bote
                path = Looking(Rise(off + n * 0.08f * Mathf.Sin(t * 13f)), 0.9f);
                _rig.SetPath(path);
                _rig.S = _rig.PathLength;
                _rig.Frame = k > 0.4f ? IsyathyArt.FrameBite : IsyathyArt.FrameAwake;
                _rig.Glow = k < 0.4f || Mathf.Repeat(t, 0.2f) < 0.1f;
                Animate(0.08f, 1.2f);
                yield return null;
            }
            _rig.Glow = true;
            // o bote: reto no gato (da parede, quase na horizontal), até o alcance
            Vector2 rear = path[path.Length - 1];
            Vector2 d = FindPlayer() ? Aim() - rear : tan * 4f;   // onde o gato vai estar
            if (m == IsyathyMove.WallStrike) d.y = Mathf.Clamp(d.y, -3f, 3f);   // da parede, quase reto, mas na altura do gato
            float reach = m == IsyathyMove.WallStrike ? R.WallReach : R.LungeReach;
            if (d.sqrMagnitude < 0.01f) d = tan;
            if (d.magnitude > reach) d = d.normalized * reach;
            Vector2 target = ClampInside(rear + d + d.normalized * 0.7f, 0.9f);
            int s = d.x >= 0f ? 1 : -1;
            Vector2 rest = m == IsyathyMove.WallStrike
                ? new Vector2(target.x, Mathf.Max(target.y - 1.6f, floorY + 0.9f))      // cai pendurada da parede
                : new Vector2(target.x + s * 0.6f, floorY + 0.9f);                       // o queixo no chão
            var arc = BurrowRules.LungeArc(rear, target);
            var full = new Vector2[path.Length + arc.Length + 1];
            path.CopyTo(full, 0); arc.CopyTo(full, path.Length); full[full.Length - 1] = rest;
            _rig.SetPath(full);
            float from = _rig.S, mid = BurrowRules.LengthTo(full, full.Length - 2), end = _rig.PathLength;
            bool hit = false;
            t = 0f;
            while (t < R.StrikeTime)
            {
                t += Time.deltaTime;
                _rig.S = Mathf.Lerp(from, mid, BurrowRules.EaseOut(t / R.StrikeTime));
                Animate(0.1f, 18f);
                if (!hit) hit = Bite(R.Damage(m, finisher));   // só a boca fere (o corpo é sólido)
                yield return null;
            }
            _rig.Frame = IsyathyArt.FrameAwake;
            CameraShake.Shake(1.5f, 0.2f);
            yield return Move(_rig.S, end, 0.35f, BurrowRules.EaseInOut, 0.1f);
            HitSparks.Burst(_rig.Snout, 8, Dirt(), new Color(0.35f, 0.25f, 0.33f), 1.5f, 4f, 0.4f, 20f, 0.6f);
            yield return Exposed(R.Exposed(m, _phase));
            yield return Retract(_craterA);
            _u = R.ParamOf(Inside, at);
        }

        /// <summary>
        /// A Descida do Teto: o teto racha em cima do gato e a sombra dela aparece no chão; ela desce até meia altura,
        /// balança mirando e cai mordendo até o chão (um coração). Fica pendurada, cansada, com a cabeça ao alcance.
        /// </summary>
        IEnumerator CeilingDrop(float x, bool finisher)
        {
            if (!FindPlayer()) yield break;
            var r = Inside;
            Vector2 at = new Vector2(x, r.yMax), n = Vector2.down;
            Vector2 floor = new Vector2(x, floorY);
            float warn = R.Warning(IsyathyMove.CeilingDrop, _phase);
            // o aviso: o teto racha, pedrinhas caem e a sombra cresce no chão
            CameraShake.Shake(1.2f, warn);
            float t = 0f;
            while (t < warn)
            {
                t += Time.deltaTime;
                float k = t / warn;
                _craterA.Show(at, Mathf.Lerp(0.2f, 0.75f, k), false, R.CraterAngle(n));
                _impact.Show(floor, Mathf.Lerp(0.25f, 0.7f, k), false);
                if (Random.value < Time.deltaTime * 12f)
                    HitSparks.One(at + new Vector2(Random.Range(-1.2f, 1.2f), -0.1f), new Vector2(Random.Range(-0.5f, 0.5f), -1f), Dirt(), 1.2f, 20f);
                yield return null;
            }
            float hover = floorY + 6.5f, hx = x;
            // pairando, a cabeça segue o gato (até 4,5 tiles para os lados do buraco no teto) e a sombra vai junto
            Vector2[] Build(float sway) => BurrowRules.Smooth(new[]
            {
                at + Vector2.up * 10f, at + Vector2.up * 1.5f, at, new Vector2((x + hx) * 0.5f + 0.4f + sway * 0.3f, at.y - 3f), new Vector2(hx + sway, hover),
            });
            var path = Build(0f);
            Emerge(at, n, path, _craterA);
            yield return Move(8f, _rig.PathLength, R.RiseTime + 0.15f, BurrowRules.EaseOutBack, 0.12f);
            // balança mirando e vai atrás do gato; a boca abre e ela trava pouco antes de cair: vem aí
            float aim = R.AimTime(_phase) + 0.35f;
            t = 0f;
            while (t < aim)
            {
                t += Time.deltaTime;
                float k = t / aim;
                if (k < 0.75f && FindPlayer())
                    hx = Mathf.MoveTowards(hx, Mathf.Clamp(Aim().x, Mathf.Max(x - 4.5f, Inside.xMin + 1.5f), Mathf.Min(x + 4.5f, Inside.xMax - 1.5f)), Time.deltaTime * 7f);
                floor = new Vector2(hx, floorY);
                path = Build(Mathf.Sin(t * 5f) * 0.3f * (1f - k));
                _rig.SetPath(path);
                _rig.S = _rig.PathLength;
                _rig.Frame = k > 0.5f ? IsyathyArt.FrameBite : IsyathyArt.FrameAwake;
                _rig.Glow = k < 0.5f || Mathf.Repeat(t, 0.2f) < 0.1f;
                _impact.Show(floor, Mathf.Lerp(0.7f, 1f, k), false);
                Animate(0.08f, 1f);
                yield return null;
            }
            _rig.Glow = true;
            // cai: o pescoço para no chão + 3 (o focinho entra no chão; o que passa do chão não aparece)
            var dive = new Vector2[path.Length + 1];
            path.CopyTo(dive, 0);
            dive[path.Length] = new Vector2(hx, floorY + 3f);
            _rig.SetPath(dive);
            float from = _rig.S;
            bool hit = false;
            t = 0f;
            while (t < R.StrikeTime)
            {
                t += Time.deltaTime;
                _rig.S = Mathf.Lerp(from, _rig.PathLength, BurrowRules.EaseIn(t / R.StrikeTime));
                Animate(0.06f, 20f);
                if (!hit) hit = Bite(R.Damage(IsyathyMove.CeilingDrop, finisher)) || Column(floor, R.Damage(IsyathyMove.CeilingDrop, finisher));
                yield return null;
            }
            _rig.Frame = IsyathyArt.FrameAwake;
            _impact.Show(floor, 1f, true);
            CameraShake.Shake(3f, 0.45f);
            HitSparks.Burst(floor + Vector2.up * 0.3f, 22, Dirt(), new Color(0.35f, 0.25f, 0.33f), 3f, 9f, 0.7f, 25f, 1.2f);
            if (_phase >= 3) StartCoroutine(Shockwave(floor));
            // pendurada, cansada: a cabeça sobe um pouco do chão e fica ao alcance
            yield return Move(_rig.S, _rig.PathLength - 0.8f, 0.4f, BurrowRules.EaseInOut, 0.05f);
            yield return Exposed(R.Exposed(IsyathyMove.CeilingDrop, _phase));
            _impact.Hide();
            yield return Retract(_craterA);
            _u = R.ParamOf(Inside, at);
        }

        /// <summary>A onda da descida do teto na fase III: corre pelo chão para os dois lados (meio coração; pular passa).</summary>
        IEnumerator Shockwave(Vector2 at)
        {
            float t = 0f;
            bool hurt = false;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                float d = t * 9f;
                foreach (int s in new[] { -1, 1 })
                {
                    float x = at.x + s * d;
                    if (x < arena.xMin || x > arena.xMax) continue;
                    if (Random.value < 0.6f) HitSparks.One(new Vector2(x, floorY + 0.1f), new Vector2(Random.Range(-1f, 1f), Random.Range(3f, 6f)), Dirt(), 0.4f, 30f);
                    if (!hurt && FindPlayer() && _player.IsGrounded && Mathf.Abs(_player.Body.position.x - x) < 0.7f && _player.Body.position.y < floorY + 1f)
                    {
                        hurt = true;
                        _health.TakeDamage(new DamageInfo(R.ShockDamage, new Vector2(s, 0.5f), DamageType.Normal, AttackAngle.Front, gameObject));
                    }
                }
                yield return null;
            }
        }

        /// <summary>
        /// O Arco (fase II): a cabeça sai de um vão e o rabo do vão vizinho; o corpo passa em arco embaixo do chão. Ela fica
        /// assim um tempo: o rabo balança (bater nele também dói nela) e a cabeça vigia; se o gato chega perto da cabeça,
        /// ela dá uma mordida curta, avisada (a boca abre antes).
        /// </summary>
        IEnumerator Arch(bool finisher)
        {
            if (!FindPlayer()) yield break;
            var holes = R.ArchHoles(holesX, Cat.x);
            if (holes.y < 0 || holes.y == holes.x) { yield return Strike(IsyathyMove.Lunge, Hole(holes.x), Vector2.up, finisher); yield break; }
            Vector2 a = Hole(holes.x), b = Hole(holes.y);
            float s = Mathf.Sign(b.x - a.x);                       // da cabeça para o rabo
            // o aviso nos dois vãos (o da cabeça mais forte)
            float warn = R.Warning(IsyathyMove.Arch, _phase), t = 0f;
            CameraShake.Shake(1.2f, warn);
            while (t < warn)
            {
                t += Time.deltaTime;
                float k = t / warn;
                _craterA.Show(a, Mathf.Lerp(0.2f, 0.7f, k), false);
                _craterB.Show(b, Mathf.Lerp(0.15f, 0.5f, k), false);
                if (Random.value < Time.deltaTime * 16f)
                    HitSparks.One((Random.value < 0.6f ? a : b) + new Vector2(Random.Range(-1.2f, 1.2f), 0.1f), new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 5f)), Dirt(), 0.5f, 25f);
                yield return null;
            }
            // o trilho: a ponta do rabo (em cima do vão do rabo), o mergulho embaixo do chão, a cabeça (em cima do outro)
            float depth = 3f, tailOut = 3f;
            Vector2 headOff = Vector2.zero;
            float sway = 0f, lift = 5f;
            Vector2[] Build() => BurrowRules.Smooth(new[]
            {
                b + new Vector2(sway, tailOut) + Vector2.down * lift, b + Vector2.up * 0.6f + Vector2.down * lift, b + new Vector2(-s * 0.6f, -2.2f) + Vector2.down * lift,
                (a + b) * 0.5f + Vector2.down * (depth + lift), a + new Vector2(s * 0.6f, -2.2f) + Vector2.down * lift, a + Vector2.down * lift,
                a + new Vector2(-s * 0.3f, 1.8f) + Vector2.down * lift, a + new Vector2(-s * 0.9f, R.RearOut) + headOff + Vector2.down * lift,
            });
            // acerta o rabo para o corpo inteiro caber no trilho (o resto vai mais fundo)
            for (int i = 0; i < 14; i++)   // (o corpo da luta tem 32 tiles: o mergulho vai mais fundo)
            {
                float len = BurrowRules.Length(Build());
                tailOut = Mathf.Clamp(tailOut + (R.BodyLength + 0.2f - len), 1.2f, 5f);
                if (BurrowRules.Length(Build()) < R.BodyLength) depth += 1f;
            }
            _rig.SetPath(Build());
            _rig.ClearClip();
            _rig.MaxS = float.PositiveInfinity;   // o trilho do arco já tem o corpo inteiro (o meio embaixo do chão)
            _rig.Frame = IsyathyArt.FrameAwake;
            _rig.Glow = true;
            _rig.S = _rig.PathLength;
            _rig.Visible = true;
            _craterA.Show(a, 1f, true); _craterB.Show(b, 0.85f, true);
            CameraShake.Shake(2.5f, 0.4f);
            HitSparks.Burst(a + Vector2.up * 0.3f, 14, Dirt(), new Color(0.35f, 0.25f, 0.33f), 3f, 8f, 0.7f, 25f, 1.2f);
            HitSparks.Burst(b + Vector2.up * 0.3f, 10, Dirt(), new Color(0.35f, 0.25f, 0.33f), 3f, 7f, 0.6f, 25f, 1f);
            // sobem juntos, a cabeça e o rabo
            t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                lift = 5f * (1f - BurrowRules.EaseOutBack(t / 0.8f));
                _rig.SetPath(Build());
                _rig.S = _rig.PathLength;
                Animate(0.1f, 6f);
                yield return null;
            }
            lift = 0f;
            _tailS = tailOut * 0.5f;
            _tailOut = true;
            _exposed = true;
            // fica: o rabo balança, a cabeça vigia; mordida curta se o gato chegar perto (a boca abre antes)
            float hold = R.Exposed(IsyathyMove.Arch, _phase), nextSnap = 0.8f;
            t = 0f;
            while (t < hold && _state == State.Fighting)
            {
                t += Time.deltaTime;
                sway = Mathf.Sin(t * 3.2f) * 0.9f;
                Vector2 neck0 = a + new Vector2(-s * 0.9f, R.RearOut);
                Vector2 look = FindPlayer() ? Vector2.ClampMagnitude((Cat - neck0) * 0.16f, 1.2f) : Vector2.zero;   // encara o gato
                headOff = Vector2.Lerp(headOff, look, 1f - Mathf.Exp(-Time.deltaTime * 6f));
                _rig.Frame = IsyathyArt.FrameAwake;
                if (t >= nextSnap && FindPlayer() && Vector2.Distance(Cat, neck0) < 6f)
                {
                    // o aviso da mordida: a boca abre e os olhos piscam
                    float w = 0f;
                    while (w < 0.45f) { w += Time.deltaTime; t += Time.deltaTime; _rig.Frame = IsyathyArt.FrameBite; _rig.Glow = Mathf.Repeat(w, 0.16f) < 0.08f; Animate(0.08f, 0f); yield return null; }
                    _rig.Glow = true;
                    Vector2 snap = FindPlayer() ? Vector2.ClampMagnitude(Aim() - neck0, 3.8f) : Vector2.zero;
                    bool hit = false;
                    w = 0f;
                    while (w < 0.5f)
                    {
                        w += Time.deltaTime; t += Time.deltaTime;
                        float k = w < 0.18f ? BurrowRules.EaseOut(w / 0.18f) : 1f - BurrowRules.EaseInOut((w - 0.18f) / 0.32f);
                        headOff = snap * k;
                        _rig.SetPath(Build());
                        _rig.S = _rig.PathLength;
                        Animate(0.06f, k > 0.5f ? 10f : 2f);
                        if (!hit && w < 0.25f) hit = Bite(R.Damage(IsyathyMove.Arch, finisher));
                        yield return null;
                    }
                    nextSnap = t + 1.4f;
                }
                _rig.SetPath(Build());
                _rig.S = _rig.PathLength;
                Animate(0.05f, 0f);
                yield return null;
            }
            _exposed = false;
            _tailOut = false;
            // descem juntos
            t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                lift = 6f * BurrowRules.EaseIn(t / 0.8f);
                _rig.SetPath(Build());
                _rig.S = _rig.PathLength;
                Animate(0.1f, -6f);
                yield return null;
            }
            HideBody();
            HideCraters();
            _u = R.ParamOf(Inside, a);
        }

        /// <summary>
        /// O Cuspe (fase II): sai da parede de longe, abre a boca (a luz da praga junta na boca) e cospe três bolas em arco,
        /// uma de cada vez, onde o gato está; cada uma vira uma poça de veneno no chão (<see cref="PoisonPuddle"/>).
        /// </summary>
        IEnumerator Spit(Vector2 at, Vector2 n)
        {
            if (!FindPlayer()) yield break;
            yield return Warn(at, n, R.Warning(IsyathyMove.Spit, _phase), _craterA);
            var path = BurrowRules.Smooth(new[] { at - n * 10f, at - n * 1.5f, at, at + n * 1.8f + Vector2.up * 0.4f, at + n * 3.2f + Vector2.up * 1.1f });
            Emerge(at, n, path, _craterA);
            yield return Move(8f, _rig.PathLength, R.RiseTime, BurrowRules.EaseOutBack, 0.12f);
            _rig.Frame = IsyathyArt.FrameBite;
            // a boca junta a praga, a cabeça encarando o gato: o aviso dos tiros
            var aimPath = _rig.Path;
            float t = 0f, charge = R.AimTime(_phase);
            while (t < charge)
            {
                t += Time.deltaTime;
                _rig.SetPath(Looking(aimPath, 0.7f));
                _rig.S = _rig.PathLength;
                if (Random.value < Time.deltaTime * 20f)
                {
                    Vector2 from = _rig.Snout + Random.insideUnitCircle.normalized * 1.2f;
                    HitSparks.One(from, (_rig.Snout - from) * 3f, Random.value < 0.5f ? (Color)ForestMagic.Shell : new Color32(0x9a, 0xe0, 0x5a, 0xff), 0.3f, 0f);
                }
                Animate(0.08f, 0f);
                yield return null;
            }
            for (int i = 0; i < R.SpitShots && _state == State.Fighting; i++)
            {
                if (FindPlayer())
                {
                    Vector2 lead = Aim() - Cat;   // prevê para onde o gato vai
                    Vector2 target = new Vector2(Mathf.Clamp(Feet.x + lead.x * 3f + (i - (R.SpitShots - 1) * 0.5f) * 1.8f, Inside.xMin + 1f, Inside.xMax - 1f), Feet.y + 0.2f);
                    _rig.SetPath(Looking(aimPath, 0.7f)); _rig.S = _rig.PathLength; _rig.Render();
                    PlagueSpit.Fire(_rig.Snout, target, R.SpitFlight, R.Damage(IsyathyMove.Spit), gameObject);
                }
                CameraShake.Shake(0.8f, 0.12f);
                yield return Hold(0.32f, 0.1f, false);
            }
            _rig.Frame = IsyathyArt.FrameAwake;
            yield return Exposed(R.Exposed(IsyathyMove.Spit, _phase));
            yield return Retract(_craterA);
            _u = R.ParamOf(Inside, at);
        }

        /// <summary>O Rugido da troca de fase (não fere): sai no meio, ruge e a arena treme.</summary>
        IEnumerator Roar()
        {
            Vector2 h = Hole(CenterHole());
            yield return Warn(h, Vector2.up, 0.7f, _craterA);
            var path = BurrowRules.Smooth(new[] { h + Vector2.down * 10f, h, h + new Vector2(0.8f, 4f), h + new Vector2(-0.6f, 8f), h + new Vector2(0.4f, 10f) });
            Emerge(h, Vector2.up, path, _craterA);
            yield return Move(8f, _rig.PathLength, 0.8f, BurrowRules.EaseOutBack, 0.14f);
            _rig.Frame = IsyathyArt.FrameBite;
            CameraShake.Shake(3.5f, 1.2f);
            yield return Hold(1.3f, 0.14f, true);
            _rig.Frame = IsyathyArt.FrameAwake;
            yield return Retract(_craterA);
            _u = R.ParamOf(Inside, h);
        }

        // ------------------------------------------------------------ a cabeça exposta e o golpe nela

        /// <summary>
        /// A espera cansada (a janela de bater): respira, solta bafo e, mesmo cansada, segue o gato com a cabeça (pedido do
        /// usuário, 17/11/2026: "parecer viva"; antes, ela olhava para o nada).
        /// </summary>
        IEnumerator Exposed(float seconds)
        {
            _exposed = true;
            float t = 0f;
            var basePath = Upto(_rig.Path, _rig.S);
            Vector2 end = basePath[basePath.Length - 1], look = Vector2.zero;
            while (t < seconds && _state == State.Fighting)
            {
                t += Time.deltaTime;
                Vector2 want = FindPlayer() ? Vector2.ClampMagnitude((Cat - end) * 0.15f, 0.9f) : Vector2.zero;
                look = Vector2.Lerp(look, want, 1f - Mathf.Exp(-Time.deltaTime * 4f));
                _rig.SetPath(look.sqrMagnitude > 0.01f ? Append(basePath, end + look) : basePath);
                _rig.S = _rig.PathLength - 0.12f * Mathf.Abs(Mathf.Sin(t * 2.5f));   // respira, cansada
                _rig.Glow = !_stunned && Mathf.Repeat(t, 0.8f) < 0.55f;
                if (Random.value < Time.deltaTime * 3f) HitSparks.One(_rig.Snout, new Vector2(Random.Range(-0.5f, 0.5f), 1.5f), new Color(0.75f, 0.7f, 0.8f), 0.6f, 0f);   // o bafo cansado
                Animate(0.05f, 0f);
                yield return null;
            }
            _exposed = false;
            _rig.Glow = true;
        }

        /// <summary>O golpe do gato na cabeça (<see cref="IsyathyHead"/>).</summary>
        public void HitHead(in DamageInfo info) => Hit(info, _rig != null ? (_rig.Neck + _rig.Snout) * 0.5f : (Vector2)transform.position, _exposed);

        /// <summary>O golpe do gato no rabo (o Arco): sempre pega enquanto o rabo está para fora.</summary>
        public void HitTail(in DamageInfo info)
        {
            if (!TailOut) return;
            Hit(info, BurrowRules.PointAt(_rig.Path, _tailS), true);
        }

        /// <summary>O golpe no corpo (fora da terra): na espera, meio dano; fora dela, resvala nas escamas.</summary>
        public void HitBody(in DamageInfo info, Vector2 at) => Hit(info, at, _exposed, true);

        /// <summary>
        /// As bolas de golpe do corpo seguem o desenho: do pescoço para trás, só nos pedaços fora da terra (dentro da
        /// arena), do tamanho do corpo ali (grosso perto da cabeça, fino no rabo).
        /// </summary>
        void UpdateBodyBoxes()
        {
            bool on = _rig.Visible && (_state == State.Fighting || _state == State.Intro) && !_cinematic && _rig.Path.Length > 1;
            for (int i = 0; i < _bodyCols.Count; i++)
            {
                var c = _bodyCols[i];
                float d = 1.6f + i * BodyStep;                       // o primeiro logo atrás da cabeça
                Vector2 p = on ? BurrowRules.PointAt(_rig.Path, _rig.S - d) : Vector2.zero;
                bool show = on && !_rig.OutsideRoom(p, -0.4f);
                c.enabled = show;
                float radius = Mathf.Max(0.35f, BurrowRules.Radius(d, R.BodyLength) / 16f);
                // o corpo sólido (não fere, mas o gato não atravessa): só na luta, um pouco menor que o desenho
                PlaceSolid(_solids[i], show && R.BodyIsSolid && _state == State.Fighting, p, radius * 0.8f);
                if (!show) continue;
                c.transform.position = p;
                c.radius = radius;
            }
        }

        // ------------------------------------------------------------ o corpo sólido (18/11/2026)

        readonly List<Rigidbody2D> _solids = new List<Rigidbody2D>();
        Rigidbody2D _headSolid;

        /// <summary>
        /// Uma bola sólida do corpo (camada do chão: o gato para nela, nem o dash atravessa; dá para pular por cima e
        /// até pisar). Cinemática: empurra o gato quando passa (pedido do usuário: "você não pode atravessar ela nem o corpo dela").
        /// </summary>
        Rigidbody2D Solid(string name)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(transform, false);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var c = go.AddComponent<CircleCollider2D>();
            c.enabled = false;
            go.AddComponent<IsyathySolid>();
            return rb;
        }

        static void PlaceSolid(Rigidbody2D rb, bool on, Vector2 at, float radius)
        {
            if (rb == null) return;
            var c = rb.GetComponent<CircleCollider2D>();
            if (!on) { if (c.enabled) c.enabled = false; return; }
            if (!c.enabled) { rb.position = at; rb.transform.position = at; c.enabled = true; }
            else rb.MovePosition(at);
            c.radius = radius;
        }

        void Hit(in DamageInfo info, Vector2 at, bool exposed, bool body = false)
        {
            if (_state != State.Fighting) return;
            int dmg = body ? R.BodyDamage(info.Amount, exposed, _stunned, _shell) : R.HeadDamage(info.Amount, exposed, _stunned, _shell);
            if (dmg <= 0 && _shell)
            {
                SeteVidas.Audio.Sfx.Play("adaga_tink", at, 0.8f, 0.6f);   // a casca da praga: nada passa
                HitSparks.Burst(at, 6, ForestMagic.Shell, ForestMagic.ShellDark, 1.5f, 4f, 0.2f, 10f, 0.3f);
                return;
            }
            if (dmg <= 0)
            {
                SeteVidas.Audio.Sfx.Play("adaga_tink", at, 0.7f, 0.8f);   // resvala nas escamas
                HitSparks.Burst(at, 4, Color.white, new Color32(0x8a, 0x9a, 0x72, 0xff), 1.5f, 3f, 0.15f, 10f, 0.2f);
                return;
            }
            int before = _hp;
            _hp = Mathf.Max(0, _hp - dmg);
            _flashUntil = Time.time + 0.08f;
            _rig.Tint = new Color(1f, 0.55f, 0.6f);
            SeteVidas.Audio.Sfx.Play("zumbi_dano", at, 0.9f, 0.6f);
            HitSparks.Burst(at, 8, new Color32(0xb4, 0x46, 0x5a, 0xff), new Color32(0xc4, 0x7b, 0xff, 0xff), 2f, 5f, 0.3f, 20f, 0.4f);
            if (_hp <= 0) { Die(); return; }
            if (R.CrossedVulpisMark(before, _hp, R.MaxHealth)) StartShell();
        }

        // ------------------------------------------------------------ a Vulpis

        void BuildVulpis()
        {
            var go = new GameObject("Vulpis (no alto)");
            go.transform.SetParent(transform, false);
            _vulpis = go.AddComponent<SpriteRenderer>();
            _vulpis.sortingOrder = _foxOrder;
            if (vulpisAnimations != null)
            {
                // a arte nova: o pivô nos pés, olhando para a arena (para a esquerda)
                go.transform.position = new Vector3(vulpisAt.x, vulpisAt.y, 0f);
                _fox = go.AddComponent<VulpisVisual>();
                _fox.Configure(vulpisAnimations, true);
                return;
            }
            _vulpis.sprite = vulpisSprite;
            float half = vulpisSprite != null ? vulpisSprite.bounds.extents.y : 0.8f;
            go.transform.position = new Vector3(vulpisAt.x, vulpisAt.y + half, 0f);
        }

        void ShowVulpis(bool on)
        {
            if (_vulpis == null) return;
            _vulpis.enabled = on && (_fox != null || _vulpis.sprite != null);
            foreach (var r in _vulpis.GetComponentsInChildren<SpriteRenderer>(true)) if (r != _vulpis) r.enabled = _vulpis.enabled;
        }

        /// <summary>Altura do balão de fala da Vulpis (o pivô da arte nova é nos pés).</summary>
        float FoxBubble => _fox != null ? 2.4f : 1.9f;

        /// <summary>
        /// A Casca da Praga (ao cruzar 65% e 30%): a praga cobre a Isyathy e ela fica imune por um tempo, lutando
        /// normalmente. A luta (<see cref="Fight"/>) chama <see cref="VulpisStrike"/> quando o tempo passa.
        /// </summary>
        void StartShell()
        {
            // nada passa desde já (nem durante o feitiço); o tempo da casca conta depois dele
            _shell = true;
            _shellSince = float.MaxValue;
            _exposed = _stunned = _tailOut = false;
            if (_fight != null) StopCoroutine(_fight);
            _fight = StartCoroutine(CastShell());
        }

        /// <summary>
        /// A Casca da Praga como magia (18/11/2026, pedido do usuário: "faça ela lançar uma magia que a deixe invulnerável e
        /// pareça um efeito"): ela para o que estava fazendo (ou sobe do vão do meio), ergue a cabeça e ruge; um círculo de
        /// runas roxas gira em volta dela, as runas sobem, e no estouro o escudo de placas da praga cobre o corpo inteiro
        /// (<see cref="UpdateBarrier"/>). Depois volta para a luta, imune, até a flecha da Vulpis quebrar o escudo.
        /// </summary>
        IEnumerator CastShell()
        {
            if (!_rig.Visible)
            {
                Vector2 h = Hole(CenterHole());
                yield return Warn(h, Vector2.up, 0.5f, _craterA);
                var path = BurrowRules.Smooth(new[] { h + Vector2.down * 10f, h, h + new Vector2(0.8f, 4f), h + new Vector2(-0.6f, 8f), h + new Vector2(0.4f, 10f) });
                Emerge(h, Vector2.up, path, _craterA);
                yield return Move(8f, _rig.PathLength, 0.6f, BurrowRules.EaseOutBack, 0.14f);
                _u = R.ParamOf(Inside, h);
            }
            _rig.Frame = IsyathyArt.FrameBite;
            if (FindPlayer()) SpeechBubble.Show(_player.transform, "O que ela está fazendo?!", 1.8f);
            // o círculo de runas gira e cresce em volta da cabeça; as runas sobem do corpo
            var circle = _circle = RuneCircle();
            float t = 0f, cast = 1.5f;
            CameraShake.Shake(1.5f, cast);
            while (t < cast)
            {
                t += Time.deltaTime;
                float k = t / cast;
                Vector2 c = (_rig.Neck + _rig.Snout) * 0.5f;
                circle.transform.position = c;
                circle.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Floor(t * 8f) * 22.5f);   // gira em degraus (pixel art)
                float s = Mathf.Lerp(0.3f, 1.6f, BurrowRules.EaseOut(k));
                circle.transform.localScale = new Vector3(s, s, 1f);
                circle.color = new Color(1f, 1f, 1f, k < 0.85f ? 1f : (1f - k) / 0.15f);
                _rig.Tint = Color.Lerp(PhaseTint(), new Color(0.74f, 0.46f, 1f), k);
                if (Random.value < Time.deltaTime * 30f)
                {
                    Vector2 p = BurrowRules.PointAt(_rig.Path, _rig.S - Random.Range(0f, R.BodyLength));
                    if (!_rig.OutsideRoom(p, 0f)) HitSparks.One(p, new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(2f, 4f)), Random.value < 0.4f ? (Color)ForestMagic.Shell : new Color32(0xf0, 0xc9, 0xff, 0xff), 0.8f, -1f);
                }
                Hold1(0.14f, true, t);
                yield return null;
            }
            Destroy(circle.gameObject);
            // o estouro: o escudo cobre o corpo
            Vector2 at = (_rig.Neck + _rig.Snout) * 0.5f;
            HitSparks.Burst(at, 26, ForestMagic.Shell, ForestMagic.ShellDark, 3f, 9f, 0.6f, 0f, 0.9f);
            CameraShake.Shake(3f, 0.5f);
            _barrierOn = true;
            _barrierBorn = Time.time;
            _shellSince = Time.time;
            if (FindPlayer()) SpeechBubble.Show(_player.transform, "Não entra nada! A praga cobriu ela inteira!", 2.4f);
            StartCoroutine(FoxComes(_hp <= R.PhaseThreeAt * R.MaxHealth, true));
            _rig.Frame = IsyathyArt.FrameAwake;
            yield return new WaitForSeconds(0.4f);
            yield return Retract(_craterA);
            HideCraters();
            _fight = StartCoroutine(Fight());
        }

        /// <summary>Um quadro do rugido (a cabeça treme) sem trocar de pose.</summary>
        void Hold1(float wave, bool shake, float t)
        {
            float s0 = _rig.S;
            if (shake) _rig.S = s0 + 0.08f * Mathf.Sin(t * 40f);
            Animate(wave, 0f);
            _rig.S = s0;
        }

        // ------------------------------------------------------------ o escudo da praga (a magia da casca)

        bool _barrierOn;
        SpriteRenderer _circle;
        float _barrierBorn;
        readonly List<SpriteRenderer> _plates = new List<SpriteRenderer>();
        const int BarrierPlates = 14;
        static Sprite s_plate, s_runes;

        /// <summary>
        /// O escudo: placas roxas translúcidas de borda acesa cobrindo o corpo fora da terra (do pescoço ao rabo), pulsando
        /// em onda, e um anel maior em volta da cabeça. Some quando ela entra na terra e volta quando ela sai.
        /// </summary>
        void UpdateBarrier()
        {
            bool on = _barrierOn && _rig != null && _rig.Visible && _rig.Path.Length > 1;
            if (on && _plates.Count == 0)
                for (int i = 0; i < BarrierPlates; i++)
                {
                    var sr = new GameObject("escudo da praga " + i).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = Plate();
                    sr.sortingOrder = SerpentRig.Order + 2;
                    _plates.Add(sr);
                }
            for (int i = 0; i < _plates.Count; i++)
            {
                var sr = _plates[i];
                if (!on) { sr.enabled = false; continue; }
                float d = i == 0 ? 1.2f : 0.2f + i * 1.55f;                    // a primeira (maior) na cabeça
                Vector2 p = i == 0 ? (_rig.Neck + _rig.Snout) * 0.5f : BurrowRules.PointAt(_rig.Path, _rig.S - d);
                bool show = !_rig.OutsideRoom(p, -0.3f);
                sr.enabled = show;
                if (!show) continue;
                float grow = Mathf.Min(1f, (Time.time - _barrierBorn) / 0.35f);   // aparecem crescendo, em onda
                float pulse = 0.55f + 0.3f * Mathf.Sin(Time.time * 6f - i * 0.7f);
                float r = (i == 0 ? 2.6f : BurrowRules.Radius(d, R.BodyLength) / 16f * 1.9f) * grow;
                sr.transform.position = p;
                sr.transform.localScale = new Vector3(r, r, 1f);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Floor(Time.time * 3f + i) * 30f);
                sr.color = new Color(1f, 1f, 1f, pulse);
            }
        }

        /// <summary>A flecha da Vulpis quebra o escudo: cada placa estoura em cacos roxos.</summary>
        void BreakBarrier()
        {
            foreach (var sr in _plates)
                if (sr.enabled) { ForestMagic.Shards(sr.transform.position, 6); sr.enabled = false; }
            _barrierOn = false;
        }

        /// <summary>A placa do escudo (16 × 16, 1 tile): um hexágono roxo translúcido de borda acesa, com runa no meio.</summary>
        static Sprite Plate()
        {
            if (s_plate != null) return s_plate;
            const int n = 16;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x - 7.5f), dy = Mathf.Abs(y - 7.5f);
                    float hex = Mathf.Max(dx * 0.866f + dy * 0.5f, dy);   // distância do hexágono
                    if (hex > 7.2f) continue;
                    Color32 c = hex > 6.2f ? new Color32(0xe6, 0xb8, 0xff, 0xff) : (hex > 5.2f ? new Color32(0xa0, 0x5c, 0xe0, 0xc0) : new Color32(0x7a, 0x3a, 0xb8, 0x50));
                    if ((x == 7 || x == 8) && y >= 5 && y <= 10) c = new Color32(0xd8, 0xa8, 0xff, 0xa0);   // a runa
                    if ((y == 7 || y == 8) && x >= 6 && x <= 9) c = new Color32(0xd8, 0xa8, 0xff, 0xa0);
                    px[y * n + x] = c;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "escudo_praga", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_plate = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
            s_plate.hideFlags = HideFlags.DontSave;
            return s_plate;
        }

        /// <summary>O círculo de runas do feitiço (48 × 48): dois anéis roxos com as runas entre eles.</summary>
        SpriteRenderer RuneCircle()
        {
            if (s_runes == null)
            {
                const int n = 48;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x - 23.5f, dy = y - 23.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;
                        bool outer = d > 21.5f && d < 23f, inner = d > 15.5f && d < 16.6f;
                        int seg = (int)(ang / 30f);
                        bool rune = d > 17.5f && d < 20.5f && (ang % 30f) > 8f && (ang % 30f) < 22f && ((seg % 2 == 0) ? (x + y) % 3 != 0 : Mathf.Abs(d - 19f) < 0.8f || (ang % 30f) < 11f);
                        if (outer || inner) px[y * n + x] = new Color32(0xc4, 0x7b, 0xff, 0xff);
                        else if (rune) px[y * n + x] = new Color32(0xf0, 0xc9, 0xff, 0xff);
                    }
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "runas_praga", hideFlags = HideFlags.DontSave };
                tex.SetPixels32(px);
                tex.Apply(false);
                s_runes = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
                s_runes.hideFlags = HideFlags.DontSave;
            }
            var sr = new GameObject("círculo de runas").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = s_runes;
            sr.sortingOrder = SerpentRig.Order + 3;
            return sr;
        }

        // ------------------------------------------------------------ a Vulpis chega, espera pronta e vai embora

        bool _foxReady;
        Vector2 _foxFeet;

        /// <summary>A plataforma onde a Vulpis chega: a dos lados mais perto do gato (para aparecer na tela).</summary>
        Vector2 FoxSpot()
        {
            if (vulpisSpots == null || vulpisSpots.Length == 0) return vulpisAt;
            Vector2 best = vulpisSpots[0];
            float catX = FindPlayer() ? Cat.x : arena.center.x;
            foreach (var s in vulpisSpots) if (Mathf.Abs(s.x - catX) < Mathf.Abs(best.x - catX)) best = s;
            return best;
        }

        /// <summary>
        /// A Vulpis chega (pedido do usuário, 17/11/2026: "apareça só nos momentos que ataca"): cai do alto na plataforma do
        /// lado do gato, pousa e fala. Com <paramref name="ready"/>, fica pronta com o arco baixo, juntando a magia da
        /// floresta na flecha (a pose "ready", viva), até a cena da flecha forte.
        /// </summary>
        IEnumerator FoxComes(bool second, bool ready)
        {
            if (_vulpis == null) yield break;
            _foxFeet = FoxSpot();
            Transform fox = _vulpis.transform;
            if (_fox != null) _fox.FaceLeft = _foxFeet.x > arena.center.x;   // olhando para o meio da arena
            Vector2 from = _foxFeet + Vector2.up * 7f;
            fox.position = from;
            _vulpis.color = new Color(1f, 1f, 1f, 0f);
            ShowVulpis(true);
            if (_fox != null) _fox.Play("fall");
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float k = t / 0.5f;
                fox.position = Vector2.Lerp(from, _foxFeet, BurrowRules.EaseIn(k));
                _vulpis.color = new Color(1f, 1f, 1f, Mathf.Min(1f, k * 3f));
                if (Random.value < Time.deltaTime * 20f) ForestMagic.One((Vector2)fox.position + Random.insideUnitCircle * 0.5f + Vector2.up, Vector2.up * 1.5f, ForestMagic.Mid, 0.4f);
                yield return null;
            }
            fox.position = _foxFeet;
            _vulpis.color = Color.white;
            HitSparks.Burst(_foxFeet + Vector2.up * 0.1f, 8, new Color(0.45f, 0.38f, 0.3f), new Color(0.3f, 0.25f, 0.2f), 1.5f, 4f, 0.4f, 15f, 0.5f);
            if (_fox != null)
            {
                _fox.Play("land", ready ? "ready" : "idle");
                yield return new WaitForSeconds(_fox.Duration("land"));
                if (!ready) _fox.Release();
            }
            if (!ready) yield break;
            _foxReady = true;
            // a primeira vez que ela aparece na luta: a conversa com retrato (D-ISYATHY-65, pedido do usuário, 18/11/2026)
            if (!second && talkFirstArrow != null)
            {
                bool opened = false;
                yield return Talk(talkFirstArrow, ok => opened = ok);
                if (opened)
                {
                    if (_fox != null && _foxReady) _fox.Play("ready");   // volta para a pose de juntar a magia
                    yield break;
                }
            }
            // a conversa enquanto ela junta a magia (a casca dura uns 4,5 s)
            string a = second ? "De novo! A praga está mais forte agora..." : "Segura ela, gato! A praga endureceu a pele dela!";
            string b = second ? "Mantém ela ocupada. Essa vai doer nela." : "Só a magia da floresta atravessa isso. Me dá um instante!";
            SpeechBubble.Show(fox, a, 2.2f, FoxBubble);
            yield return new WaitForSeconds(2.3f);
            if (_foxReady) SpeechBubble.Show(fox, b, 2.2f, FoxBubble);
        }

        /// <summary>A Vulpis vai embora: agacha, pula para o alto e some (volta só no próximo ataque).</summary>
        IEnumerator FoxLeaves()
        {
            if (_vulpis == null || !_vulpis.enabled) yield break;
            _foxReady = false;
            Transform fox = _vulpis.transform;
            if (_fox != null) _fox.Play("crouch");
            yield return new WaitForSeconds(0.16f);
            if (_fox != null) _fox.Play("leap");
            Vector2 from = fox.position, to = from + new Vector2(_fox != null && _fox.FaceLeft ? 1.5f : -1.5f, 7f);
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = t / 0.45f;
                fox.position = Vector2.Lerp(from, to, BurrowRules.EaseOut(k));
                _vulpis.color = new Color(1f, 1f, 1f, 1f - BurrowRules.EaseIn(k));
                if (Random.value < Time.deltaTime * 25f) ForestMagic.One((Vector2)fox.position + Random.insideUnitCircle * 0.5f + Vector2.up, Vector2.down * 1.2f, ForestMagic.Mid, 0.45f);
                yield return null;
            }
            ShowVulpis(false);
            _vulpis.color = Color.white;
            if (_fox != null) _fox.Release();
        }

        /// <summary>Pronta, esperando a hora: a magia verde junta na ponta da flecha.</summary>
        void FoxReadyGlow()
        {
            if (!_foxReady || _vulpis == null || !_vulpis.enabled || Time.timeScale <= 0f) return;
            if (Random.value > Time.deltaTime * 14f) return;
            float side = _fox != null && _fox.FaceLeft ? -1f : 1f;
            Vector2 tip = (Vector2)_vulpis.transform.position + new Vector2(side * 0.75f, 0.8f);
            Vector2 from = tip + Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.6f);
            ForestMagic.One(from, (tip - from) * 2.5f, Random.value < 0.4f ? (Color)ForestMagic.Core : (Color)ForestMagic.Mid, 0.35f, Random.value < 0.3f);
        }


        /// <summary>
        /// A flecha forte (pedido do usuário, 17/11/2026): a Isyathy sobe longe da Vulpis e ruge, coberta pela casca; o jogo
        /// pausa e a câmera vai até a Vulpis com zoom; ela agacha, pula, puxa o arco no ar juntando a magia da floresta e
        /// atira. A câmera segue a flecha até a cabeça; a casca estoura, o zoom volta e a luta continua com a cabeça
        /// atordoada, levando mais dano (D-ISYATHY-65 e D-ISYATHY-30).
        /// </summary>
        IEnumerator VulpisStrike()
        {
            bool second = _hp <= R.PhaseThreeAt * R.MaxHealth;
            Vector2 h = Hole(StrikeHole());
            // anda pelas bordas até o vão (como sempre), sobe e ruge
            _spot = h; _normal = Vector2.up;
            yield return TravelTo(R.ParamOf(Inside, h));
            yield return Warn(h, Vector2.up, 0.7f, _craterA);
            var path = BurrowRules.Smooth(new[] { h + Vector2.down * 10f, h, h + new Vector2(0.8f, 4f), h + new Vector2(-0.6f, 8f), h + new Vector2(0.4f, 10f) });
            Emerge(h, Vector2.up, path, _craterA);
            yield return Move(8f, _rig.PathLength, 0.8f, BurrowRules.EaseOutBack, 0.14f);
            _rig.Frame = IsyathyArt.FrameBite;
            CameraShake.Shake(3f, 0.8f);
            yield return Hold(0.7f, 0.14f, true);
            if (_state != State.Fighting) yield break;
            if (_vulpis != null && !_vulpis.enabled) yield return FoxComes(second, true);   // (se ela ainda não chegou)
            _foxReady = false;

            yield return Cinematic();
            if (_state != State.Fighting) yield break;

            if (_vulpis != null)
                SpeechBubble.Show(_vulpis.transform, second ? "A praga está tomando ela. Mais um pouco e não sobra nada da guardiã!" : "Agora, gato!", second ? 3f : 1.6f, FoxBubble);
            // atordoada: os olhos fecham com a dor, a cabeça cai devagar até perto do chão
            _stunned = true;
            _exposed = true;
            _rig.Frame = IsyathyArt.FrameSleep;
            _rig.Glow = false;
            float s0 = _rig.S, low = Mathf.Max(BurrowRules.Depth + 1f, _rig.S - 5f), t = 0f;
            while (t < R.StunTime && _state == State.Fighting)
            {
                t += Time.deltaTime;
                _rig.S = Mathf.Lerp(s0, low, BurrowRules.EaseOut(Mathf.Min(1f, t / 0.6f))) - 0.12f * Mathf.Abs(Mathf.Sin(t * 3f));
                Animate(0.04f, 0f);
                if (Random.value < Time.deltaTime * 6f) HitSparks.One(_rig.Neck + Random.insideUnitCircle * 0.6f, Vector2.up * 2f, ForestMagic.Mid, 0.5f, 0f);
                yield return null;
            }
            _stunned = false;
            _exposed = false;
            _rig.Glow = true;
            _rig.Frame = IsyathyArt.FrameAwake;
            if (_state != State.Fighting) yield break;
            yield return Retract(_craterA);
            _u = R.ParamOf(Inside, h);
        }

        /// <summary>O vão da cena da flecha: longe da Vulpis (a flecha atravessa a arena) e nunca perto do gato.</summary>
        int StrikeHole()
        {
            int best = CenterHole();
            float score = float.MinValue;
            for (int i = 0; i < holesX.Length; i++)
            {
                if (FindPlayer() && Mathf.Abs(holesX[i] - Cat.x) < 3.5f) continue;
                float s = Mathf.Abs(holesX[i] - _foxFeet.x) - 0.5f * Mathf.Abs(holesX[i] - arena.center.x);
                if (s > score) { score = s; best = i; }
            }
            return best;
        }

        /// <summary>Onde a flecha sai no desenho (px do pivô nos pés, olhando para a direita): a ponta no "air_draw".</summary>
        static readonly Vector2 ArrowTipPx = new Vector2(12.75f, 10.9f);

        PlayerInputReader _lockedInput;

        /// <summary>A cena em si, toda no tempo real (o jogo fica pausado até a flecha acertar).</summary>
        IEnumerator Cinematic()
        {
            _cinematic = true;
            if (FindPlayer())
            {
                _lockedInput = _player.GetComponent<PlayerInputReader>();
                if (_lockedInput != null) _lockedInput.SetLock(this, true);
                _health.Shielded = true;
            }
            Time.timeScale = 0f;
            CinematicCamera.Begin();
            Vector2 home = CinematicCamera.Home;
            BuildDim();
            _rigOrder = _rig.SortingOrder; _rig.SortingOrder = 21;
            if (_vulpis != null) { _foxOrder = _vulpis.sortingOrder; _vulpis.sortingOrder = 23; }
            Transform fox = _vulpis != null ? _vulpis.transform : transform;
            Vector2 feet = fox.position;
            float side = _fox != null && !_fox.FaceLeft ? 1f : -1f;

            // 1. foca nela: a câmera vai até a Vulpis, dá zoom e o resto escurece
            yield return Real(0.6f, k =>
            {
                float e = BurrowRules.EaseInOut(k);
                CinematicCamera.MoveTo(Vector2.Lerp(home, feet + Vector2.up * 1.4f, e));
                CinematicCamera.SetZoom(Mathf.Lerp(1f, 2f, e));
                Dim(0.62f * e);
            });
            // 2. agacha e pula
            if (_fox != null) _fox.Play("crouch");
            yield return Real(0.18f, null);
            if (_fox != null) _fox.Play("leap");
            Vector2 apex = feet + new Vector2(-side * 0.4f, 2.6f);
            for (int i = 0; i < 6; i++) ForestMagic.One(feet + new Vector2(Random.Range(-0.5f, 0.5f), 0.1f), new Vector2(Random.Range(-2f, 2f), Random.Range(0.5f, 2f)), ForestMagic.Mid, 0.5f, i % 2 == 0);
            yield return Real(0.38f, k =>
            {
                fox.position = Vector2.Lerp(feet, apex, BurrowRules.EaseOut(k));
                CinematicCamera.MoveTo(Vector2.Lerp(feet + Vector2.up * 1.4f, apex + Vector2.up * 0.9f, BurrowRules.EaseOut(k)));
            });
            // 3. no alto, puxa o arco: a magia da floresta se junta na ponta da flecha
            if (_fox != null) _fox.Play("air_draw");
            Vector2 Tip() => (Vector2)fox.position + new Vector2(side * ArrowTipPx.x, ArrowTipPx.y) / 16f;
            yield return Real(0.62f, k =>
            {
                fox.position = apex + Vector2.up * 0.15f * k;                     // paira, quase parada
                Vector2 tip = Tip();
                if (Random.value < 0.7f)
                {
                    Vector2 from = tip + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.4f);
                    ForestMagic.One(from, (tip - from) * 2.6f, Random.value < 0.4f ? (Color)ForestMagic.Core : (Color)ForestMagic.Mid, 0.35f, Random.value < 0.3f);
                }
                CinematicCamera.SetZoom(Mathf.Lerp(2f, 2.4f, k));                 // aperta um pouco na carga
            });
            CameraShake.Shake(1f, 0.15f);
            // 4. solta
            Vector2 start = Tip();
            Vector2 Target() => (_rig.Neck + _rig.Snout) * 0.5f;
            if (_fox != null) _fox.Play("air_release");
            ForestMagic.Burst(start, 16, 2f, 6f, 0.4f);
            var arrow = new GameObject("Flecha forte da Vulpis").AddComponent<SpriteRenderer>();
            arrow.sprite = ForestMagic.Arrow();
            arrow.sortingOrder = 24;
            arrow.transform.position = start;
            Vector2 p = start;
            // 5. a câmera segue a flecha até a cabeça (o zoom abre um pouco para a viagem)
            float zoom = CinematicCamera.Zoom;
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                Time.timeScale = 0f;
                Vector2 d = Target() - p;
                float step = 24f * dt;
                if (d.magnitude <= step) { p = Target(); break; }
                p += d.normalized * step;
                arrow.transform.position = p;
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                ForestMagic.One(p - d.normalized * 1.2f + Random.insideUnitCircle * 0.2f, -d.normalized * 1.5f + Random.insideUnitCircle, Random.value < 0.5f ? (Color)ForestMagic.Mid : (Color)ForestMagic.Deep, 0.45f, Random.value < 0.4f);
                zoom = Mathf.MoveTowards(zoom, 1.6f, dt * 2f);
                CinematicCamera.SetZoom(zoom);
                CinematicCamera.MoveTo(Vector2.Lerp(CinematicCamera.Position, p, 1f - Mathf.Exp(-dt * 16f)));
                yield return null;
            }
            Destroy(arrow.gameObject);
            // 6. o impacto: a casca da praga estoura (o escudo quebra em cacos)
            _shell = false;
            BreakBarrier();
            ManaPotion.Spawn(p);   // o potinho de mana cai dela (recarrega a Cura do Paladino)
            ForestMagic.Burst(p, 44, 3f, 12f, 0.9f);
            ForestMagic.Shards(p, 30);
            CameraShake.Shake(4.5f, 0.5f);
            SeteVidas.Audio.Sfx.Play("adaga_tink", p, 1f, 0.5f);
            _rig.Tint = Color.white;
            _rig.Frame = IsyathyArt.FrameSleep;
            _rig.Glow = false;
            _rig.Render();
            if (_dim != null) _dim.color = new Color(0.86f, 1f, 0.84f, 0.75f);   // clarão verde
            yield return Real(0.07f, null);
            yield return Real(0.45f, k =>
            {
                Dim(0.62f * (1f - k * 0.3f));
                CinematicCamera.MoveTo(Vector2.Lerp(CinematicCamera.Position, p, 0.2f));
                CinematicCamera.SetZoom(Mathf.Lerp(1.6f, 1.9f, Mathf.Sin(k * Mathf.PI)));   // um soco de zoom
            });
            // 7. zoom out e volta para a batalha: o tempo volta, a Vulpis desce para a plataforma
            Time.timeScale = 1f;
            if (_fox != null) StartCoroutine(FoxLands(fox, feet, _hp <= R.PhaseThreeAt * R.MaxHealth ? 3.2f : 1.9f));
            else fox.position = feet;
            Vector2 from2 = CinematicCamera.Position;
            float z0 = CinematicCamera.Zoom;
            float dimNow = _dim != null ? _dim.color.a : 0f;
            yield return Real(0.75f, k =>
            {
                float e = BurrowRules.EaseInOut(k);
                CinematicCamera.MoveTo(Vector2.Lerp(from2, home, e));
                CinematicCamera.SetZoom(Mathf.Lerp(z0, 1f, e));
                Dim(dimNow * (1f - e));
            }, false);
            EndCinematic();
        }

        /// <summary>A Vulpis volta para a plataforma (tempo normal) e pousa.</summary>
        /// <remarks>Depois de pousar, fica <paramref name="stay"/> s (a fala) e vai embora.</remarks>
        IEnumerator FoxLands(Transform fox, Vector2 feet, float stay)
        {
            yield return new WaitForSeconds(0.2f);
            _fox.Play("fall");
            Vector2 from = fox.position;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                fox.position = Vector2.Lerp(from, feet, BurrowRules.EaseIn(t / 0.35f));
                yield return null;
            }
            fox.position = feet;
            _fox.Play("land", "idle");
            yield return new WaitForSeconds(stay);
            yield return FoxLeaves();
        }

        /// <summary>Espera <paramref name="seconds"/> no tempo real; com <paramref name="paused"/>, segura o jogo pausado (um hitstop não o solta).</summary>
        IEnumerator Real(float seconds, System.Action<float> each, bool paused = true)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (paused) Time.timeScale = 0f;
                each?.Invoke(Mathf.Clamp01(t / seconds));
                yield return null;
            }
        }

        /// <summary>O escurecido em volta (só a Vulpis, a flecha e a Isyathy por cima): um retângulo preto maior que a arena.</summary>
        void BuildDim()
        {
            if (_dim != null) return;
            var tex = Texture2D.whiteTexture;
            var go = new GameObject("Cena da flecha (escurecido)");
            go.transform.SetParent(transform, false);
            _dim = go.AddComponent<SpriteRenderer>();
            _dim.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            _dim.sortingOrder = 20;
            go.transform.position = arena.center;
            go.transform.localScale = new Vector3(arena.width + 80f, arena.height + 60f, 1f);
            Dim(0f);
        }

        void Dim(float a) { if (_dim != null) _dim.color = new Color(0.02f, 0.03f, 0.05f, a); }

        /// <summary>Desfaz a cena (também se o gato morrer, a cena trocar ou o objeto sumir no meio dela).</summary>
        void EndCinematic()
        {
            if (!_cinematic) return;
            _cinematic = false;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            CinematicCamera.End();
            if (_dim != null) { Destroy(_dim.gameObject); _dim = null; }
            if (_rig != null) _rig.SortingOrder = _rigOrder;
            if (_vulpis != null) _vulpis.sortingOrder = _foxOrder;
            if (_lockedInput != null) { _lockedInput.SetLock(this, false); _lockedInput = null; }
            if (_health != null) _health.Shielded = false;
        }

        // ------------------------------------------------------------ a morte e o espírito

        void Die()
        {
            StopAllCoroutines();
            EndCinematic();
            _shell = false;
            _barrierOn = false;
            if (_circle != null) Destroy(_circle.gameObject);
            _foxReady = false;
            if (_fox != null) { _fox.transform.position = vulpisAt; _fox.Release(); }
            if (_vulpis != null) { _vulpis.color = Color.white; ShowVulpis(false); }
            _fight = null;
            _exposed = _stunned = false;
            _state = State.Dying;
            StartCoroutine(Death());
        }

        /// <summary>A terra voando enquanto ela afunda: na boca do buraco (segue a cabeça até ela sumir).</summary>
        IEnumerator DirtFountain(Vector2 at, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                Vector2 p = _rig != null && _rig.Visible && !_rig.OutsideRoom(_rig.Neck, 0f) ? _rig.Neck : at;
                if (Random.value < Time.deltaTime * 30f)
                    HitSparks.One(p + new Vector2(Random.Range(-1f, 1f), 0f), new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3f, 7f)), Dirt(), 0.6f, 25f);
                yield return null;
            }
        }

        IEnumerator Death()
        {
            // D-ISYATHY-DERROTA (refeita em 18/11/2026, pedido do usuário: "ela se enfiando na terra e depois a cabeça
            // saindo e deitando morta"):
            // 1. um último rugido, a cabeça tremendo
            _rig.Frame = IsyathyArt.FrameBite;
            _rig.Tint = Color.white;
            _tailOut = false;
            CameraShake.Shake(4f, 1.4f);
            yield return Hold(1.2f, 0.16f, true);
            // 2. ela se debate e afunda na terra pelo caminho por onde veio (a onda forte: a agonia)
            _rig.Frame = IsyathyArt.FrameAwake;
            CameraShake.Shake(2.5f, 1.6f);
            StartCoroutine(DirtFountain(_rig.Neck, 1.6f));
            yield return Move(_rig.S, 0f, 1.6f, BurrowRules.EaseIn, 0.3f);
            HideBody();
            HideCraters();
            // 3. silêncio... e o chão treme e racha perto do gato (nunca embaixo dele)
            yield return new WaitForSeconds(0.9f);
            float catX = FindPlayer() ? Cat.x : Inside.center.x;
            int hi = R.NearHole(holesX, catX, 5f);
            if (hi < 0) hi = CenterHole();
            Vector2 h = Hole(hi);
            int side = h.x >= catX ? 1 : -1;                                     // cai para longe do gato
            if (h.x + side * 5f > Inside.xMax - 0.5f || h.x + side * 5f < Inside.xMin + 0.5f) side = -side;
            float t = 0f;
            CameraShake.Shake(1.5f, 1f);
            while (t < 1f)
            {
                t += Time.deltaTime;
                _craterA.Show(h, Mathf.Lerp(0.2f, 0.8f, t), false);
                if (Random.value < Time.deltaTime * 14f)
                    HitSparks.One(h + new Vector2(Random.Range(-1.2f, 1.2f), 0.1f), new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 5f)), Dirt(), 0.5f, 25f);
                yield return null;
            }
            // 4. a cabeça sai devagar, fraca, os olhos piscando (o brilho roxo falhando)
            Vector2 Up(float k) => Vector2.Lerp(new Vector2(side * 0.3f, 2.4f), new Vector2(side * 2.2f, 1.5f), k);
            Vector2 Top(float k) => Vector2.Lerp(new Vector2(side * 0.9f, 4f), new Vector2(side * 4.6f, 0.9f), k);
            Vector2[] Pose(float k, float sway) => BurrowRules.Smooth(new[]
            {
                h + Vector2.down * 10f, h + Vector2.down * 1.5f, h, h + Up(k), h + Top(k) + new Vector2(sway, 0f),
            });
            Emerge(h, Vector2.up, Pose(0f, 0f), _craterA);
            _rig.Glow = false;
            yield return Move(8f, _rig.PathLength, 1.5f, BurrowRules.EaseOut, 0.05f);
            t = 0f;
            while (t < 1.4f)   // balança, tentando ficar de pé
            {
                t += Time.deltaTime;
                _rig.SetPath(Pose(0f, Mathf.Sin(t * 4f) * 0.35f * (1f - t / 1.4f)));
                _rig.S = _rig.PathLength;
                _rig.Glow = Mathf.Repeat(t, 0.45f) < 0.12f;
                Animate(0.03f, 0f);
                yield return null;
            }
            // 5. desaba de lado: o queixo bate no chão
            _rig.Glow = true;
            t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                _rig.SetPath(Pose(BurrowRules.EaseIn(t / 0.7f), 0f));
                _rig.S = _rig.PathLength;
                Animate(0.02f, 0f);
                yield return null;
            }
            _rig.SetPath(Pose(1f, 0f));
            _rig.S = _rig.PathLength;
            CameraShake.Shake(3f, 0.5f);
            HitSparks.Burst(_rig.Snout, 20, Dirt(), new Color(0.35f, 0.25f, 0.33f), 2f, 7f, 0.6f, 25f, 1f);
            // 6. o brilho dos olhos apaga devagar e os olhos fecham
            for (int i = 0; i < 3; i++) { _rig.Glow = i % 2 == 0; _rig.Render(); yield return new WaitForSeconds(0.35f); }
            _rig.Glow = false;
            _rig.Frame = IsyathyArt.FrameSleep;
            _rig.Amplitude = 0f;
            _rig.Render();
            _state = State.Dead;
            if (ReferenceEquals(BossBarSource.Active, this)) BossBarSource.Active = null;
            var data = SaveManager.Data;
            if (data != null) { data.SetFlag(DeadFlag); SaveManager.Save(); }   // abre a arena (o teto e a volta para a E6)
            yield return new WaitForSeconds(0.8f);

            // a Vulpis chega para a conversa do fim (e fica: "Alguém precisa ficar com a floresta")
            if (_vulpis != null && !_vulpis.enabled) yield return FoxComes(false, false);
            Transform cat = FindPlayer() ? _player.transform : transform;
            Transform fox = _vulpis != null ? _vulpis.transform : transform;
            yield return Speak(talkDefeat, DefeatBubbles(cat, fox));

            // D-ISYATHY-ESPIRITO-01: do corpo sobe uma luz azul-clara; a serpente se desenha no ar, translúcida
            var spirit = BuildSpirit(_rig.Neck);
            t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                spirit.color = new Color(1f, 1f, 1f, 0.9f * t / 1.6f);
                spirit.transform.position = (Vector3)(_rig.Neck + Vector2.up * (t * 1.2f));
                if (Random.value < Time.deltaTime * 12f) HitSparks.Rise((Vector2)spirit.transform.position + Vector2.up * 3.5f + Random.insideUnitCircle * 2.5f, new Color(0.7f, 0.95f, 1f), 1.2f, 1f);
                yield return null;
            }
            // o corpo de carne some na terra (fica a cratera)
            StartCoroutine(FadeBody());
            yield return Speak(talkSpirit, SpiritBubbles(cat, fox, spirit.transform));
            yield return Speak(talkSpiritMore, SpiritMoreBubbles(cat, spirit.transform));
            t = 0f;
            while (t < 1.5f)   // o espírito se dissolve entre as árvores
            {
                t += Time.deltaTime;
                spirit.color = new Color(1f, 1f, 1f, 0.9f * (1f - t / 1.5f));
                spirit.transform.position += Vector3.up * Time.deltaTime * 1.2f;
                yield return null;
            }
            Destroy(spirit.gameObject);
            yield return Speak(talkFarewell, FarewellBubbles(cat, fox));
        }

        /// <summary>A conversa com retrato; se não abrir (sem o diálogo na cena), os balões de antes.</summary>
        IEnumerator Speak(SeteVidas.Dialogue.DialogueDefinition d, IEnumerator bubbles)
        {
            bool opened = false;
            yield return Talk(d, ok => opened = ok);
            if (!opened) yield return bubbles;
        }

        IEnumerator DefeatBubbles(Transform cat, Transform fox)
        {
            yield return Say(fox, "...", 1.2f);
            yield return Say(cat, "Ela parou.", 1.6f);
            yield return Say(fox, "Eu passei semanas procurando um jeito de salvar ela. Não tinha jeito.", 3f);
            yield return Say(cat, "A praga não sai. Nem de uma coisa desse tamanho.", 2.6f);
        }

        IEnumerator SpiritBubbles(Transform cat, Transform fox, Transform spirit)
        {
            yield return Say(spirit, "Pequeno que cai... e que sempre se levanta.", 2.6f);
            yield return Say(cat, "Ela... fala?", 1.6f);
            yield return Say(spirit, "Eu falava antes. Mas a doença não deixava ninguém me ouvir. Nem eu mesma.", 3.2f);
            yield return Say(spirit, "Vocês me soltaram. A carne era a prisão da praga. Agora eu sou só floresta.", 3.2f);
            yield return Say(fox, "Guardiã... me perdoe.", 2f);
            yield return Say(spirit, "Raposa fiel. Você não errou nenhuma flecha que precisava errar.", 3f);
        }

        IEnumerator SpiritMoreBubbles(Transform cat, Transform spirit)
        {
            yield return Say(spirit, "A doença saiu do livro que você carregou, pequeno. Eu senti quando ele acordou.", 3.2f);
            yield return Say(spirit, "Mas o livro não nasceu entre os cães. A maldição e o livro vêm de longe, de uma torre que fura as nuvens.", 3.6f);
            yield return Say(spirit, "E a doença não vai parar nas minhas raízes.", 2.4f);
            yield return Say(cat, "A torre do horizonte. Aetherion.", 2.2f);
            yield return Say(spirit, "Vá, pequeno que cai. Eu continuo aqui. Quando passar pela floresta, procure os meus olhos.", 3.4f);
        }

        IEnumerator FarewellBubbles(Transform cat, Transform fox)
        {
            yield return Say(fox, "O caminho de saída é por ali, subindo. Eu fico. Alguém precisa ficar com a floresta.", 3.4f);
            yield return Say(cat, "Obrigado pelas flechas. Não conte para ninguém que um gato precisou de ajuda.", 3.2f);
            yield return Say(fox, "Ninguém ia acreditar mesmo.", 2.2f);
        }

        IEnumerator Say(Transform who, string line, float seconds)
        {
            bool fox = _fox != null && who == _fox.transform;
            if (who != null) SpeechBubble.Show(who, line, seconds, fox ? FoxBubble : 1.9f);
            // a Vulpis fala mexendo a boca; triste pela guardiã, e sorri no fim
            if (fox) _fox.Play(line.StartsWith("Ninguém") ? "smile" : (line.StartsWith("Guardiã") || line.StartsWith("Eu passei") || line == "..." ? "sad" : "talk"));
            yield return new WaitForSeconds(seconds + 0.15f);
            if (fox) _fox.Release();
        }

        IEnumerator FadeBody()
        {
            float t = 0f;
            while (t < 2.5f)
            {
                t += Time.deltaTime;
                _rig.Tint = new Color(0.8f, 0.8f, 0.85f, 1f - t / 2.5f);
                yield return null;
            }
            _rig.Visible = false;
            _rig.Tint = Color.white;
        }

        /// <summary>
        /// O espírito (18/11/2026, arte nova: <see cref="IsyathySpiritArt"/>): a serpente inteira de luz azul-clara, subindo
        /// em S, o rabo se desfazendo em fiapos. Antes era só a cabeça, translúcida ("parecia uma cabeça flutuante").
        /// </summary>
        SpriteRenderer BuildSpirit(Vector2 at)
        {
            var px = IsyathySpiritArt.Pixels();
            var tex = new Texture2D(IsyathySpiritArt.W, IsyathySpiritArt.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "isyathy_espirito", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            var go = new GameObject("Espírito da Isyathy");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, IsyathySpiritArt.W, IsyathySpiritArt.H), new Vector2(0.5f, 0.05f), 16f);
            sr.sortingOrder = 9;
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.flipX = FindPlayer() && Cat.x < at.x;   // a cabeça olha para o gato
            go.AddComponent<SpiritFloat>();
            return sr;
        }


        // ------------------------------------------------------------ apoio

        Vector2 Hole(int i) => new Vector2(holesX[Mathf.Clamp(i, 0, holesX.Length - 1)], floorY);

        int CenterHole()
        {
            int best = 0;
            float d = float.MaxValue;
            for (int i = 0; i < holesX.Length; i++)
                if (Mathf.Abs(holesX[i] - arena.center.x) < d) { d = Mathf.Abs(holesX[i] - arena.center.x); best = i; }
            return best;
        }

        // o rastro do gato (onde ele esteve), para a mira atrasada
        readonly List<Vector3> _trail = new List<Vector3>();   // x, y, o tempo

        void RecordTrail()
        {
            if (!FindPlayer()) return;
            Vector2 c = Cat;
            _trail.Add(new Vector3(c.x, c.y, Time.time));
            while (_trail.Count > 2 && _trail[1].z < Time.time - 2f) _trail.RemoveAt(0);
        }

        /// <summary>
        /// Onde o gato estava há <see cref="IsyathyBossRules.AimDelay"/> s (pedido do usuário, 18/11/2026): ela mira no rastro
        /// dele, não onde ele está; quem se mexe (o dash) sai de onde ela vai morder.
        /// </summary>
        Vector2 Aim()
        {
            if (!FindPlayer()) return Inside.center;
            float when = Time.time - R.AimDelay(_phase);
            for (int i = _trail.Count - 1; i > 0; i--)
            {
                var a = _trail[i - 1];
                if (a.z > when) continue;
                var b = _trail[i];
                float k = b.z > a.z ? Mathf.Clamp01((when - a.z) / (b.z - a.z)) : 1f;
                return Vector2.Lerp(a, b, k);
            }
            return _trail.Count > 0 ? (Vector2)_trail[0] : Cat;
        }

        /// <summary>O trilho com um pedacinho a mais na ponta, apontando para o gato: a cabeça o encara.</summary>
        Vector2[] Looking(Vector2[] path, float reach)
        {
            Vector2 end = path[path.Length - 1];
            Vector2 d = FindPlayer() ? Cat - end : Vector2.zero;
            if (d.sqrMagnitude < 0.04f) return path;
            return Append(path, end + d.normalized * reach);
        }

        static Vector2[] Append(Vector2[] path, Vector2 p)
        {
            var r = new Vector2[path.Length + 1];
            path.CopyTo(r, 0);
            r[path.Length] = p;
            return r;
        }

        /// <summary>O trilho só até <paramref name="s"/> (onde a cabeça está agora).</summary>
        static Vector2[] Upto(Vector2[] path, float s)
        {
            var list = new List<Vector2> { path[0] };
            float acc = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector2.Distance(path[i - 1], path[i]);
                if (acc + seg >= s)
                {
                    Vector2 p = Vector2.Lerp(path[i - 1], path[i], seg > 1e-5f ? (s - acc) / seg : 0f);
                    if (Vector2.Distance(p, list[list.Count - 1]) > 1e-3f) list.Add(p);
                    break;
                }
                acc += seg;
                list.Add(path[i]);
            }
            if (list.Count < 2) list.Add(path[path.Length - 1]);
            return list.ToArray();
        }

        Vector2 ClampInside(Vector2 p, float margin)
        {
            var r = Inside;
            return new Vector2(Mathf.Clamp(p.x, r.xMin + margin, r.xMax - margin), Mathf.Clamp(p.y, r.yMin + margin, r.yMax - margin));
        }

        void HideCraters() { _craterA?.Hide(); _craterB?.Hide(); _impact?.Hide(); }

        /// <summary>Anda pelas bordas até <paramref name="target"/> (posição no perímetro), pelo caminho mais curto.</summary>
        IEnumerator TravelTo(float target)
        {
            var r = Inside;
            int dir = R.ShortestDir(r, _u, target);
            float speed = R.TravelSpeed(_phase);
            ShowMound(true);
            while (_state == State.Fighting)
            {
                float left = R.Distance(r, _u, target, dir), step = speed * Time.deltaTime;
                if (left <= step) { _u = target; MoundAt(_u, dir, left); break; }
                _u += dir * step;
                MoundAt(_u, dir, step);
                yield return null;
            }
            ShowMound(false);
        }

        /// <summary>O aviso: a superfície treme, racha e brilha onde ela vai sair (no chão, na parede ou no teto).</summary>
        IEnumerator Warn(Vector2 at, Vector2 n, float seconds, IsyathyBurrow.Crater c)
        {
            CameraShake.Shake(1.2f, seconds);
            float angle = R.CraterAngle(n), t = 0f;
            Vector2 tan = new Vector2(n.y, -n.x);
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = t / seconds;
                c.Show(at, Mathf.Lerp(0.2f, 0.75f, k), false, angle);
                if (Random.value < Time.deltaTime * 14f)
                    HitSparks.One(at + tan * Random.Range(-1.2f, 1.2f) + n * 0.1f, n * Random.Range(2f, 5f) + tan * Random.Range(-1f, 1f), Dirt(), 0.5f, n.y < -0.5f ? 20f : 25f);
                // no fim do aviso, o brilho roxo da praga escapa pela rachadura: vai sair agora
                if (k > 0.6f && Random.value < Time.deltaTime * 10f)
                    HitSparks.One(at + tan * Random.Range(-0.8f, 0.8f), n * 2.5f, ForestMagic.Shell, 0.4f, 0f);
                yield return null;
            }
        }

        /// <summary>Sai da superfície: a cratera abre (girada para o lado), a terra voa, o corpo aparece no trilho.</summary>
        void Emerge(Vector2 at, Vector2 n, Vector2[] path, IsyathyBurrow.Crater c)
        {
            c.Show(at, 1f, true, R.CraterAngle(n));
            CameraShake.Shake(2f, 0.3f);
            HitSparks.Burst(at + n * 0.3f, 16, Dirt(), new Color(0.35f, 0.25f, 0.33f), 3f, 8f, 0.7f, 25f, 1.2f);
            _rig.SetPath(path);
            _rig.ClearClip();
            _rig.Frame = IsyathyArt.FrameAwake;
            _rig.Glow = true;
            // o trilho começa Depth tiles embaixo do buraco: a cabeça para antes de o rabo sair (fica sempre um pedaço na terra)
            _rig.MaxS = R.MaxHead(BurrowRules.Depth);
            _rig.S = BurrowRules.Depth - 2f;
            _rig.Visible = true;
        }

        /// <summary>Volta para a terra pelo mesmo caminho (devagar no começo, como quem cansou).</summary>
        IEnumerator Retract(IsyathyBurrow.Crater c)
        {
            float time = Mathf.Max(R.RetractTime, _rig.S / 18f);
            yield return Move(_rig.S, 0f, time, BurrowRules.EaseIn, 0.2f);
            HideBody();
            c.Hide();
            _impact.Hide();
        }

        void HideBody()
        {
            _rig.Visible = false;
            _rig.ClearClip();
            _tailOut = false;
        }

        /// <summary>
        /// A boca encostou: fere na hora, com ou sem dash (18/11/2026, pedido do usuário: "se ela relar em nós, ainda vamos
        /// tomar o dano"). Desviar é sair de onde ela mirou (a mira atrasada, <see cref="IsyathyBossRules.AimDelay"/>).
        /// </summary>
        bool Land(bool touching, int damage, Vector2 knock)
        {
            if (!touching || damage <= 0) return false;
            _health.TakeDamage(new DamageInfo(damage, knock, DamageType.Normal, AttackAngle.Front, gameObject));
            return true;
        }

        /// <summary>A boca encostou no gato: o dano do golpe (só a boca fere; o corpo é sólido, não machuca).</summary>
        bool Bite(int damage)
        {
            if (damage <= 0 || !FindPlayer()) return false;
            bool touching = BurrowRules.Bites(_rig.Snout, Cat) || Vector2.Distance((_rig.Neck + _rig.Snout) * 0.5f, Cat) <= R.BiteRadius;
            float dir = Cat.x >= _rig.Neck.x ? 1f : -1f;
            return Land(touching, damage, new Vector2(dir, 0.4f));
        }

        /// <summary>
        /// Embaixo da cabeça que desce (a coluna da mordida): o dano do golpe, só quando a cabeça já chegou no gato (antes,
        /// feria o gato embaixo desde o começo da descida, com a cabeça ainda no alto: "hitbox estranho").
        /// </summary>
        bool Column(Vector2 at, int damage)
        {
            if (!FindPlayer()) return false;
            Vector2 c = Cat;
            if (Mathf.Abs(c.x - at.x) > 1.3f || c.y > floorY + 4f) return false;
            float lowest = Mathf.Min(_rig.Snout.y, _rig.Neck.y);
            if (lowest > c.y + _player.CurrentHeight * 0.5f) return false;   // a cabeça ainda não desceu até ele
            return Land(true, damage, new Vector2(Mathf.Sign(c.x - at.x + 0.01f), 0.5f));
        }

        IEnumerator Move(float from, float to, float seconds, System.Func<float, float> ease, float wave)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float prev = _rig.S;
                _rig.S = Mathf.LerpUnclamped(from, to, ease(t / seconds));
                Animate(wave, (_rig.S - prev) / Mathf.Max(Time.deltaTime, 1e-4f));
                yield return null;
            }
            _rig.S = to;
            _rig.Render();
        }

        /// <summary>Segura a pose (a onda continua); <paramref name="shake"/>: a cabeça treme (o rugido).</summary>
        IEnumerator Hold(float seconds, float wave, bool shake)
        {
            float t = 0f, s0 = _rig.S;
            while (t < seconds)
            {
                t += Time.deltaTime;
                if (shake) _rig.S = s0 + 0.08f * Mathf.Sin(t * 40f);
                Animate(wave, 0f);
                yield return null;
            }
            _rig.S = s0;
        }

        void Animate(float wave, float headSpeed)
        {
            _rig.Amplitude = BurrowRules.WaveAmplitude(wave, headSpeed);
            _rig.Phase += Time.deltaTime * BurrowRules.WaveSpeed(headSpeed) * (headSpeed >= 0f ? 1f : -1f);
            _rig.Render();
        }

        static Color Dirt() => Random.value < 0.5f ? new Color(0.21f, 0.16f, 0.2f) : new Color(0.31f, 0.24f, 0.3f);

        // ------------------------------------------------------------ o montinho de terra (ela andando embaixo)

        void BuildMound()
        {
            var go = new GameObject("Isyathy (montinho de terra)");
            go.transform.SetParent(transform, false);
            _mound = go.AddComponent<SpriteRenderer>();
            _mound.sprite = MoundSprite();
            _mound.sortingOrder = SerpentRig.Order + 1;
            _mound.enabled = false;
        }

        void ShowMound(bool on) { if (_mound != null) _mound.enabled = on; }

        /// <summary>
        /// O montinho no ponto <paramref name="u"/> do perímetro: gira com a borda, pulsa, solta terra (do teto, ela cai) e
        /// deixa rachaduras para trás. Não fere (pedido do usuário, 18/11/2026): é o aviso de onde ela vai sair.
        /// </summary>
        void MoundAt(float u, int dir, float step)
        {
            Vector2 p = R.SurfaceAt(Inside, u, out var n, out var side);
            Vector2 tan = new Vector2(n.y, -n.x) * dir;          // para onde ela vai
            _mound.transform.position = p;
            _mound.transform.rotation = Quaternion.Euler(0f, 0f, R.CraterAngle(n));
            float bob = 1f + 0.18f * Mathf.Sin(Time.time * 16f);
            _mound.transform.localScale = new Vector3(dir >= 0 ? 1f : -1f, bob, 1f);
            if (Random.value < Time.deltaTime * 22f)
                HitSparks.One(p + n * 0.2f - tan * 0.6f, n * Random.Range(1.5f, 4f) - tan * Random.Range(0.5f, 2f), Dirt(), 0.5f, 22f);
            if (Random.value < Time.deltaTime * 3f) CameraShake.Shake(0.7f, 0.12f);
            _sinceCrack += step;
            if (_sinceCrack > 0.9f) { _sinceCrack = 0f; Crack(p, n); }
            // o montinho não fere (R.MoundHurts): é só o aviso de onde ela está
        }

        void Crack(Vector2 p, Vector2 n)
        {
            SpriteRenderer sr = null;
            int idx = -1;
            for (int i = 0; i < _cracks.Count; i++) if (!_cracks[i].enabled) { sr = _cracks[i]; idx = i; break; }
            if (sr == null)
            {
                if (_cracks.Count >= 28) { idx = 0; float oldest = float.MaxValue; for (int i = 0; i < _crackBorn.Count; i++) if (_crackBorn[i] < oldest) { oldest = _crackBorn[i]; idx = i; } sr = _cracks[idx]; }
                else
                {
                    sr = new GameObject("rachadura").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = CrackSprite();
                    sr.sortingOrder = SerpentRig.Order - 1;
                    _cracks.Add(sr); _crackBorn.Add(0f);
                    idx = _cracks.Count - 1;
                }
            }
            sr.enabled = true;
            sr.transform.position = p;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, R.CraterAngle(n));
            sr.transform.localScale = new Vector3(Random.value < 0.5f ? 1f : -1f, 1f, 1f);
            _crackBorn[idx] = Time.time;
        }

        void FadeCracks()
        {
            for (int i = 0; i < _cracks.Count; i++)
            {
                if (!_cracks[i].enabled) continue;
                float age = Time.time - _crackBorn[i];
                if (age > 1.8f) { _cracks[i].enabled = false; continue; }
                _cracks[i].color = new Color(1f, 1f, 1f, age < 1.2f ? 1f : 1f - (age - 1.2f) / 0.6f);
            }
        }

        /// <summary>O montinho (30 × 9 px, o pivô na superfície): terra estufada, pedrinhas e a rachadura roxa no alto.</summary>
        static Sprite MoundSprite()
        {
            if (s_mound != null) return s_mound;
            const int w = 30, h = 9;
            var px = new Color32[w * h];
            var dark = new Color32(0x2a, 0x1c, 0x24, 0xff);
            var dirt = new Color32(0x4a, 0x34, 0x3a, 0xff);
            var lit = new Color32(0x6a, 0x52, 0x50, 0xff);
            var stone = new Color32(0x7a, 0x70, 0x6a, 0xff);
            var plague = new Color32(0xb0, 0x5c, 0xe8, 0xff);
            for (int x = 0; x < w; x++)
            {
                float k = Mathf.Abs(x - (w - 1) * 0.5f) / ((w - 1) * 0.5f);
                int top = Mathf.RoundToInt((h - 1) * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k)) * (0.85f + 0.15f * Mathf.Sin(x * 1.7f)));
                for (int y = 0; y <= top; y++)
                {
                    Color32 c = y == top ? dark : (y >= top - 1 ? lit : dirt);
                    if ((x * 7 + y * 3) % 11 == 0 && y < top - 1) c = stone;
                    px[y * w + x] = c;
                }
            }
            // a rachadura roxa da praga no alto do montinho
            int[][] crack = { new[] { 12, 7 }, new[] { 13, 6 }, new[] { 14, 7 }, new[] { 15, 6 }, new[] { 16, 7 }, new[] { 17, 6 } };
            foreach (var c in crack) px[c[1] * w + c[0]] = plague;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "isyathy_montinho", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_mound = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
            s_mound.hideFlags = HideFlags.DontSave;
            return s_mound;
        }

        /// <summary>A rachadura que ela deixa no caminho (12 × 3 px).</summary>
        static Sprite CrackSprite()
        {
            if (s_crack != null) return s_crack;
            const int w = 12, h = 3;
            var px = new Color32[w * h];
            var c = new Color32(0x16, 0x0e, 0x14, 0xff);
            int[] ys = { 1, 1, 2, 1, 0, 1, 1, 2, 1, 1, 0, 1 };
            for (int x = 0; x < w; x++) px[ys[x] * w + x] = c;
            px[0 * w + 4] = new Color32(0x6a, 0x26, 0x8c, 0xff);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "isyathy_rachadura", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_crack = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 16f);
            s_crack.hideFlags = HideFlags.DontSave;
            return s_crack;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.6f, 0.6f);
            Gizmos.DrawWireCube(arena.center, arena.size);
            Gizmos.color = new Color(0.6f, 0.9f, 0.4f, 0.6f);
            var r = Inside;
            Gizmos.DrawWireCube(r.center, r.size);
            foreach (float x in holesX) Gizmos.DrawWireSphere(new Vector3(x, floorY, 0f), 0.6f);
            Gizmos.DrawLine(new Vector3(startX, arena.yMin, 0f), new Vector3(startX, arena.yMax, 0f));
        }
    }
}
