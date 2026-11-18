using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.World
{
    /// <summary>
    /// A Isyathy adormecida no Ninho (F7, Docs/Design/floresta-mecanicas.md §4 e plano-implementacao.md §1.2). Um por cena
    /// (<c>Forest_E_Nest</c>). Mede o alerta (0–100) pelo movimento do gato e pelos barulhos do <see cref="NoiseSystem"/>
    /// dentro das salas do ninho; na clareira do musgo (E4) ele cai rápido.
    /// <para>
    /// Retorno pela imagem, sem depender do som (o som é da F10): o olho no alto do HUD (fechado, meio, aberto, vermelho
    /// em busca) com a barrinha do alerta, que pulsa no ritmo da respiração; os olhos dela no fundo, entre as árvores,
    /// mais vezes quanto mais acordada (e abertos, parados, de 50 para cima); folhas caindo (25+); galhos frágeis
    /// tremendo (50+, <see cref="FragileBranch"/>); a terra tremendo de leve em busca.
    /// </para>
    /// Em 80+, o <b>bote de busca</b>: mira o último barulho (não o gato), sombra no chão e escamas caindo por 1,2 s, a
    /// cabeça desce numa área de 4 × 3 (meio coração, quebra os galhos frágeis) e volta; o alerta vai a 60 e há 4 s sem bote.
    /// Morrer ou descansar zera o alerta. Depois da morte da Isyathy (flag <see cref="IsyathySighting.DeadFlag"/>), não faz nada.
    /// </summary>
    public class IsyathyAlert : MonoBehaviour
    {
        public static IsyathyAlert Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active = null; // Play sem recarregar o domínio

        [Header("Salas (mundo)")]
        [Tooltip("Onde o medidor vale (as salas do ninho)")]
        [SerializeField] Rect[] areas = new Rect[0];
        [Tooltip("Clareira do musgo: o alerta cai rápido e o movimento não soma")]
        [SerializeField] Rect[] calm = new Rect[0];
        [Tooltip("D-NINHO-01: o gato percebe que a raiz respira ao entrar aqui (vazio = sem fala)")]
        [SerializeField] Rect barkRoom;

        [Header("Desenhos")]
        [Tooltip("isy_olhos_0..2: aberto, meio, fechado")]
        [SerializeField] Sprite[] eyeFrames = new Sprite[0];
        [Tooltip("isy_bote_0..1: a cabeça descendo (boca fechada) e mordendo (aberta)")]
        [SerializeField] Sprite[] headFrames = new Sprite[0];
        [Tooltip("isy_aviso: a sombra da cabeça no chão")]
        [SerializeField] Sprite warningSprite;
        [Tooltip("isy_escama: escama caindo no aviso")]
        [SerializeField] Sprite scaleSprite;
        [Tooltip("folha_caindo: folhas que caem com ela inquieta")]
        [SerializeField] Sprite leafSprite;

        [Header("Fundo")]
        [SerializeField] int backgroundOrder = -85;
        [SerializeField, Range(0f, 1f)] float parallax = 0.08f;

        /// <summary>Linhas do D-NINHO-01 (roteiro: Docs/Narrativa/floresta-roteiro.md).</summary>
        public static readonly string[] RootLines = { "...Isso não é raiz.", "Devagar, Lion. Bem devagar." };
        public const string RootBarkKey = "ninho_raiz";

        float _alert, _stillFor, _cooldown, _swingTime = -10f;
        bool _swingCounted;
        Vector2 _lastNoise;
        bool _hasNoise, _busy, _dead;
        NestState _state;
        StealthController _stealth;

        // visual
        SpriteRenderer _head, _warning, _bgEyes;
        readonly List<SpriteRenderer> _bits = new List<SpriteRenderer>();
        readonly List<Vector3> _bitVel = new List<Vector3>();
        Image _hudEye, _hudBar, _hudBarBack;
        float _leafTimer, _bgEyesAlpha, _bgNext = 2f, _bgUntil;
        Vector2 _bgBase;
        bool _bgVisible;
        Coroutine _strike;

        public float Alert => _alert;
        public NestState State => _state;
        public bool IsStriking => _busy;

        public void Configure(Rect[] nestAreas, Rect[] calmAreas, Rect rootRoom, Sprite[] eyes, Sprite[] head,
                              Sprite warning, Sprite scale, Sprite leaf)
        {
            areas = nestAreas; calm = calmAreas; barkRoom = rootRoom;
            eyeFrames = eyes; headFrames = head; warningSprite = warning; scaleSprite = scale; leafSprite = leaf;
        }

        void OnEnable()
        {
            Active = this;
            NoiseSystem.Emitted += OnNoise;
            GameEvents.WorldReset += ResetAlert;
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
            NoiseSystem.Emitted -= OnNoise;
            GameEvents.WorldReset -= ResetAlert;
        }

        void Start()
        {
            _dead = SaveManager.Data != null && SaveManager.Data.HasFlag(IsyathySighting.DeadFlag);
            if (_dead) { enabled = false; return; }
            BuildVisuals();
            StartCoroutine(RootBark());
        }

        // ------------------------------------------------------------ medidor

        bool InNest(Vector2 p) { foreach (var r in areas) if (r.Contains(p)) return true; return false; }
        bool InCalm(Vector2 p) { foreach (var r in calm) if (r.Contains(p)) return true; return false; }

        void OnNoise(NoiseEvent e)
        {
            if (_dead || !enabled) return;
            var h = PlayerHealth.Active;
            if (h != null && h.IsDead) return;
            if (!InNest(e.Position) || InCalm(e.Position)) return;
            _lastNoise = e.Position;
            _hasNoise = true;
            if (_busy) return;
            float gain = IsyathyRules.NoiseGain(e.Label, e.Loudness);
            float refund = IsyathyRules.SwingRefund(e.Label, Time.time - _swingTime, _swingCounted);
            if (refund > 0f) { gain -= refund; _swingCounted = false; }
            if (e.Label == "golpe") { _swingTime = Time.time; _swingCounted = true; }
            if (gain != 0f) _alert = IsyathyRules.Add(_alert, gain);
        }

        void ResetAlert()
        {
            _alert = 0f; _stillFor = 0f; _cooldown = 0f; _hasNoise = false;
            if (_strike != null) { StopCoroutine(_strike); _strike = null; }
            _busy = false;
            if (_head != null) _head.enabled = false;
            if (_warning != null) _warning.enabled = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_stealth == null) _stealth = StealthController.Active;
            var health = PlayerHealth.Active;
            if (_stealth != null && _stealth.Player != null && (health == null || !health.IsDead))
            {
                var pc = _stealth.Player;
                Vector2 feet = pc.Body.position;
                if (!InNest(feet)) _alert = IsyathyRules.Step(_alert, 0f, IsyathyRules.HiddenDecay, dt);
                else if (!_busy)
                {
                    bool climbing = pc.IsHanging || pc.IsClimbingLedge || pc.ActiveAbility != null;
                    var motion = IsyathyRules.MotionOf(pc.Velocity.x, pc.Velocity.y, pc.IsGrounded, pc.IsCrouching, climbing,
                        pc.Settings.MoveSpeed, _stealth.Settings != null ? _stealth.Settings.FootstepMinSpeedFraction : 0.6f);
                    bool moving = IsyathyRules.IsMoving(motion);
                    _stillFor = moving ? 0f : _stillFor + dt;
                    bool isCalm = InCalm(feet);
                    bool hidden = _stealth.IsHidden || _stealth.InCover;
                    float gain = isCalm ? 0f : IsyathyRules.MotionGain(motion);
                    _alert = IsyathyRules.Step(_alert, gain, IsyathyRules.DecayRate(moving, hidden, _stillFor, isCalm), dt);
                }
            }
            _state = IsyathyRules.StateOf(_alert);

            if (_cooldown > 0f) _cooldown -= dt;
            if (_hasNoise && IsyathyRules.CanStrike(_alert, _cooldown, _busy)) _strike = StartCoroutine(Strike(_lastNoise));

            UpdateHud();
            UpdateLeaves(dt);
            UpdateBits(dt);
        }

        void LateUpdate() => UpdateBackgroundEyes(Time.deltaTime);

        // ------------------------------------------------------------ bote de busca

        Vector2 GroundUnder(Vector2 p)
        {
            int mask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Platform);
            var hit = Physics2D.Raycast(p + Vector2.up * 0.5f, Vector2.down, IsyathyRules.GroundSearch, mask);
            return hit.collider != null ? hit.point : p;
        }

        IEnumerator Strike(Vector2 target)
        {
            _busy = true;
            Vector2 ground = GroundUnder(target);
            CameraShake.Shake(1f, 0.6f);   // o rugido grave: a terra treme

            // aviso (1,2 s): a sombra da cabeça cresce no chão e escamas caem do alto
            float t = 0f, nextScale = 0f;
            if (_warning != null)
            {
                _warning.transform.position = new Vector3(ground.x, ground.y + 0.1f, 0f);
                _warning.enabled = true;
            }
            if (_head != null)   // a cabeça surge lá no fundo, em cima
            {
                _head.sortingOrder = backgroundOrder;
                _head.sprite = headFrames.Length > 0 ? headFrames[0] : null;
                _head.enabled = true;
            }
            while (t < IsyathyRules.WarningTime)
            {
                t += Time.deltaTime;
                float k = t / IsyathyRules.WarningTime;
                if (_warning != null)
                {
                    _warning.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1f, k), Mathf.Lerp(0.4f, 1f, k), 1f);
                    _warning.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.2f, 0.85f, k));
                }
                if (_head != null)
                {
                    float s = Mathf.Lerp(0.35f, 0.6f, k);
                    _head.transform.localScale = new Vector3(s, s, 1f);
                    _head.transform.position = new Vector3(ground.x, ground.y + 9f, 0f);
                    _head.color = new Color(0.55f, 0.6f, 0.55f, Mathf.Clamp01(k * 1.5f) * 0.8f);
                }
                if (t >= nextScale) { nextScale = t + 0.12f; Spawn(scaleSprite, new Vector2(ground.x + Random.Range(-2f, 2f), ground.y + Random.Range(5f, 7f)), 4); }
                yield return null;
            }

            // o bote: a cabeça vem para a frente e desce de boca aberta
            if (_head != null)
            {
                _head.sortingOrder = 6;
                _head.color = Color.white;
                _head.transform.localScale = Vector3.one;
                if (headFrames.Length > 1) _head.sprite = headFrames[1];
            }
            t = 0f;
            bool hitDone = false;
            while (t < IsyathyRules.DiveTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / IsyathyRules.DiveTime);
                if (_head != null) _head.transform.position = new Vector3(ground.x, Mathf.Lerp(ground.y + 10f, ground.y + 3f, k * k), 0f);
                yield return null;
            }
            if (_head != null && headFrames.Length > 0) _head.sprite = headFrames[0];   // a boca fecha: a mordida
            if (_warning != null) _warning.enabled = false;
            CameraShake.Shake(2f, 0.25f);
            FragileBranch.BreakInside(IsyathyRules.StrikeArea(ground));
            for (int i = 0; i < 6; i++) Spawn(scaleSprite, ground + new Vector2(Random.Range(-1.5f, 1.5f), 0.3f), 2);

            t = 0f;
            while (t < IsyathyRules.BiteHold)
            {
                t += Time.deltaTime;
                if (!hitDone) hitDone = TryHit(ground);
                yield return null;
            }

            // volta para o fundo
            t = 0f;
            while (t < IsyathyRules.RetreatTime)
            {
                t += Time.deltaTime;
                float k = t / IsyathyRules.RetreatTime;
                if (_head != null)
                {
                    _head.transform.position = new Vector3(ground.x, Mathf.Lerp(ground.y + 3f, ground.y + 11f, k), 0f);
                    _head.color = new Color(1f, 1f, 1f, 1f - k);
                    if (k > 0.3f) _head.sortingOrder = backgroundOrder;
                }
                yield return null;
            }
            if (_head != null) _head.enabled = false;
            _alert = IsyathyRules.AfterStrike;
            _cooldown = IsyathyRules.StrikeCooldown;
            _busy = false;
            _strike = null;
        }

        bool TryHit(Vector2 ground)
        {
            var h = PlayerHealth.Active;
            if (h == null || h.IsDead || _stealth == null || _stealth.Player == null) return false;
            Vector2 feet = _stealth.Player.Body.position;
            if (!IsyathyRules.InStrike(feet, ground)) return false;
            float dir = feet.x >= ground.x ? 1f : -1f;
            h.TakeDamage(new DamageInfo(IsyathyRules.StrikeDamage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front,
                _head != null ? _head.gameObject : gameObject));
            return true;
        }

        // ------------------------------------------------------------ visual

        SpriteRenderer NewRenderer(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        void BuildVisuals()
        {
            _head = NewRenderer("Cabeça (bote)", backgroundOrder);
            _warning = NewRenderer("Sombra da cabeça", 3);
            _warning.sprite = warningSprite;
            _bgEyes = NewRenderer("Olhos no fundo", backgroundOrder);
            if (eyeFrames.Length > 0) _bgEyes.sprite = eyeFrames[eyeFrames.Length - 1];

            // o olho no alto do HUD (na tela do HUD dos corações)
            var hud = FindFirstObjectByType<HeartsHud>();
            if (hud == null) return;
            var root = new GameObject("Alerta da Isyathy", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.SetParent(hud.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(40f, 16f);
            _hudEye = NewImage("Olho", rt, new Vector2(0f, 0f), new Vector2(40f, 12f), Color.white);
            _hudEye.sprite = eyeFrames.Length > 0 ? eyeFrames[eyeFrames.Length - 1] : null;
            _hudBarBack = NewImage("Barra (fundo)", rt, new Vector2(0f, -13f), new Vector2(40f, 2f), new Color(0f, 0f, 0f, 0.55f));
            _hudBar = NewImage("Barra", rt, new Vector2(-20f, -13f), new Vector2(0f, 2f), Color.white);
            _hudBar.rectTransform.pivot = new Vector2(0f, 1f);
        }

        static Image NewImage(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Color StateColor(NestState s) =>
            s == NestState.Asleep ? new Color32(0x8a, 0xa0, 0x80, 0xff)
            : s == NestState.Restless ? new Color32(0xd8, 0xd0, 0x6a, 0xff)
            : s == NestState.Agitated ? new Color32(0xf0, 0x9a, 0x3c, 0xff)
            : new Color32(0xe8, 0x4a, 0x3a, 0xff);

        void UpdateHud()
        {
            if (_hudEye == null) return;
            var h = PlayerHealth.Active;
            bool show = _stealth != null && _stealth.Player != null && (InNest(_stealth.Player.Body.position) || _alert > 0.5f)
                        && (h == null || !h.IsDead);
            _hudEye.enabled = _hudBar.enabled = _hudBarBack.enabled = show;
            if (!show) return;
            int stage = IsyathyRules.EyeStage(_state);
            if (eyeFrames.Length >= 3) _hudEye.sprite = eyeFrames[2 - stage];
            // respira: o olho pulsa no ritmo dela (pixel inteiro: 1 px de altura)
            float breath = Mathf.Sin(Time.time * Mathf.PI * 2f / IsyathyRules.BreathPeriod(_state));
            _hudEye.rectTransform.anchoredPosition = new Vector2(0f, breath > 0f ? 1f : 0f);
            var c = StateColor(_state);
            _hudEye.color = _state == NestState.Searching ? Color.Lerp(Color.white, c, 0.5f + 0.5f * breath) : Color.white;
            _hudBar.color = c;
            _hudBar.rectTransform.sizeDelta = new Vector2(Mathf.Round(40f * _alert / IsyathyRules.Max), 2f);
        }

        /// <summary>Folhas caindo perto da câmera com ela inquieta (25+): mais quanto mais acordada.</summary>
        void UpdateLeaves(float dt)
        {
            if (leafSprite == null || _state == NestState.Asleep || _stealth == null || _stealth.Player == null) return;
            if (!InNest(_stealth.Player.Body.position)) return;
            var cam = Camera.main;
            if (cam == null) return;
            _leafTimer -= dt;
            if (_leafTimer > 0f) return;
            _leafTimer = _state == NestState.Restless ? 0.7f : _state == NestState.Agitated ? 0.35f : 0.2f;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector2 c = cam.transform.position;
            Spawn(leafSprite, new Vector2(c.x + Random.Range(-halfW, halfW), c.y + halfH + 0.5f), 1);
        }

        void Spawn(Sprite sprite, Vector2 at, int order)
        {
            if (sprite == null) return;
            SpriteRenderer sr = null;
            int idx = -1;
            for (int i = 0; i < _bits.Count; i++) if (!_bits[i].enabled) { sr = _bits[i]; idx = i; break; }
            if (sr == null)
            {
                if (_bits.Count >= 40) return;
                sr = NewRenderer("Pedacinho", order);
                _bits.Add(sr); _bitVel.Add(Vector3.zero);
                idx = _bits.Count - 1;
            }
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Color.white;
            sr.flipX = Random.value < 0.5f;
            sr.transform.position = at;
            sr.enabled = true;
            _bitVel[idx] = new Vector3(Random.Range(-0.6f, 0.6f), -Random.Range(1.2f, 2.4f), Random.Range(2f, 4f)); // z = vida (s)
        }

        void UpdateBits(float dt)
        {
            for (int i = 0; i < _bits.Count; i++)
            {
                var sr = _bits[i];
                if (!sr.enabled) continue;
                var v = _bitVel[i];
                v.z -= dt;
                if (v.z <= 0f) { sr.enabled = false; continue; }
                var p = sr.transform.position;
                float sway = sr.sprite == leafSprite ? Mathf.Sin(Time.time * 3f + i) * 0.8f : 0f;
                sr.transform.position = new Vector3(p.x + (v.x + sway) * dt, p.y + v.y * dt, p.z);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(v.z * 2f));
                _bitVel[i] = v;
            }
        }

        /// <summary>
        /// Os olhos dela no fundo, entre as árvores (como as aparições de longe: lá atrás, pequenos, com paralaxe). Dormindo,
        /// de vez em quando e fechados/meio abertos; inquieta, mais vezes; agitada ou em busca, abertos e ficando.
        /// </summary>
        void UpdateBackgroundEyes(float dt)
        {
            if (_bgEyes == null || eyeFrames.Length < 3) return;
            var cam = Camera.main;
            bool inside = _stealth != null && _stealth.Player != null && InNest(_stealth.Player.Body.position);
            bool awake = _state >= NestState.Agitated;
            if (cam != null && inside && !_bgVisible)
            {
                _bgNext -= dt;
                if (_bgNext <= 0f || (awake && _bgEyesAlpha <= 0.01f))
                {
                    float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                    int side = Random.value < 0.5f ? -1 : 1;
                    _bgBase = IsyathySighting.SpotFromCamera(cam.transform.position, halfW, halfH, side, Random.value);
                    _bgVisible = true;
                    _bgUntil = Time.time + Random.Range(2.5f, 4.5f);
                    var s = 0.7f * Random.Range(0.9f, 1.1f);
                    _bgEyes.transform.localScale = new Vector3(s, s, 1f);
                }
            }
            if (_bgVisible && (!inside || (!awake && Time.time > _bgUntil)))
            {
                _bgVisible = false;
                var gap = IsyathyRules.BodyGap(_state);
                _bgNext = Random.Range(gap.x, gap.y);
            }
            float target = _bgVisible ? (awake ? 0.95f : 0.75f) : 0f;
            _bgEyesAlpha = Mathf.MoveTowards(_bgEyesAlpha, target, dt * 1.6f);
            _bgEyes.enabled = _bgEyesAlpha > 0.01f;
            if (!_bgEyes.enabled || cam == null) return;
            int frame = _state == NestState.Asleep ? 1 : 0;                       // dormindo: meio aberto (espia)
            if (Mathf.Repeat(Time.time, awake ? 2.2f : 3.1f) < 0.12f) frame = 2;  // pisca
            _bgEyes.sprite = eyeFrames[frame];
            _bgEyes.color = _state == NestState.Searching ? new Color(1f, 0.75f, 0.7f, _bgEyesAlpha) : new Color(0.9f, 0.9f, 0.85f, _bgEyesAlpha);
            var p = IsyathySighting.Parallax(_bgBase, cam.transform.position, parallax);
            _bgEyes.transform.position = new Vector3(p.x, p.y, 0f);
        }

        /// <summary>D-NINHO-01: ao entrar na E2, uma vez por jogo.</summary>
        IEnumerator RootBark()
        {
            if (barkRoom.width <= 0f) yield break;
            var data = SaveManager.Data;
            string flag = "fala_" + RootBarkKey;
            while (true)
            {
                if (data == null || data.HasFlag(flag)) yield break;
                var h = PlayerHealth.Active;
                if (h != null && !h.IsDead && barkRoom.Contains((Vector2)h.transform.position + Vector2.up * 0.5f)) break;
                yield return new WaitForSeconds(0.3f);
            }
            data.SetFlag(flag);
            yield return new WaitForSeconds(0.8f);
            foreach (var line in RootLines)
            {
                var h = PlayerHealth.Active;
                if (h == null || h.IsDead) yield break;
                SpeechBubble.Show(h.transform, line, 2.6f);
                yield return new WaitForSeconds(2.6f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.3f, 0.6f);
            foreach (var r in areas) Gizmos.DrawWireCube(r.center, r.size);
            Gizmos.color = new Color(0.4f, 0.9f, 0.5f, 0.6f);
            foreach (var r in calm) Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
