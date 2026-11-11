using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Cenário que reage ao gato chegar perto (plano de assets, 6.2). Só visual: nunca faz barulho para a IA.
    /// <list type="bullet">
    /// <item><b>Swap</b> (cristal Primata): perto, toca os quadros "acesos" em loop; longe, volta ao desenho apagado.</item>
    /// <item><b>Flee</b> (pássaro, rato): perto, foge (quadros de fuga andando na direção de fuga) e some; volta quando o
    /// gato está longe e fora da tela.</item>
    /// <item><b>Lean</b> (velas): o gato passando inclina a chama para o lado do movimento por um instante.</item>
    /// </list>
    /// </summary>
    public class ReactOnProximity : MonoBehaviour
    {
        public enum Mode { Swap, Flee, Lean }

        [SerializeField] Mode mode = Mode.Swap;
        [SerializeField, Min(0.1f)] float radius = 3f;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Swap: quadros acesos. Flee: quadros da fuga. Lean: [0] = inclinada para a direita")]
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.02f)] float frameTime = 0.12f;
        [Tooltip("Flee: velocidade da fuga (tiles/s)")]
        [SerializeField] Vector2 fleeVelocity = new Vector2(3f, 2f);
        [Tooltip("Flee: some depois deste tempo fugindo (s) — usado quando ele tem onde se esconder (o rato no buraco)")]
        [SerializeField, Min(0.1f)] float fleeTime = 1.2f;
        [Tooltip("Flee: continua fugindo até sair da tela e só então some (pássaro; rato sem buraco)")]
        [SerializeField] bool untilOffscreen;

        Sprite _rest;
        Vector3 _home;
        bool _away;
        float _t = -1f;
        float _lastX;
        float _fleeDir = 1f;
        Transform _player;
        SpriteLoop _loop;

        public bool IsAway => _away;
        public bool IsActive => _t >= 0f;

        public void Configure(Mode newMode, float newRadius, SpriteRenderer renderer, Sprite[] newFrames, float newFrameTime,
                              Vector2 newFleeVelocity, float newFleeTime) => Configure(newMode, newRadius, renderer, newFrames, newFrameTime, newFleeVelocity, newFleeTime, false);

        public void Configure(Mode newMode, float newRadius, SpriteRenderer renderer, Sprite[] newFrames, float newFrameTime,
                              Vector2 newFleeVelocity, float newFleeTime, bool leaveScreen)
        {
            mode = newMode; radius = newRadius; spriteRenderer = renderer; frames = newFrames; frameTime = newFrameTime;
            fleeVelocity = newFleeVelocity; fleeTime = newFleeTime; untilOffscreen = leaveScreen;
        }

        /// <summary>O ponto está fora da tela da câmera (30×17 tiles), com uma margem.</summary>
        public static bool Offscreen(Vector2 p, Vector2 camera, float margin) => Mathf.Abs(p.x - camera.x) > 15f + margin || Mathf.Abs(p.y - camera.y) > 8.5f + margin;

        /// <summary>O gato está perto o bastante para a reação (distância ≤ raio).</summary>
        public static bool Near(Vector2 a, Vector2 b, float radius) => (a - b).sqrMagnitude <= radius * radius;

        void Start()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _rest = spriteRenderer.sprite;
            _home = transform.position;
            _loop = GetComponentInChildren<SpriteLoop>();
        }

        /// <summary>A animação própria (chama da vela) pausa enquanto a reação troca o desenho.</summary>
        void Hold(bool reacting) { if (_loop != null) _loop.enabled = !reacting; }

        void Update()
        {
            if (_player == null) { var h = PlayerHealth.Active; if (h == null) return; _player = h.transform; _lastX = _player.position.x; }
            if (spriteRenderer == null || frames.Length == 0) return;
            Vector2 cat = _player.position;
            bool near = Near(cat, _home, radius);
            float dt = Time.deltaTime;
            switch (mode)
            {
                case Mode.Swap:
                    if (near) { if (_t < 0f) Hold(true); _t = Mathf.Max(0f, _t) + dt; spriteRenderer.sprite = frames[(int)(_t / frameTime) % frames.Length]; }
                    else if (_t >= 0f) { _t = -1f; spriteRenderer.sprite = _rest; Hold(false); }
                    break;

                case Mode.Flee:
                    if (_away)
                    {
                        // volta quando o gato está longe e o lugar está fora da tela
                        var cam = Camera.main;
                        bool offscreen = cam == null || Mathf.Abs(cam.transform.position.x - _home.x) > 17f || Mathf.Abs(cam.transform.position.y - _home.y) > 10f;
                        if (!Near(cat, _home, radius * 3f) && offscreen) { _away = false; transform.position = _home; spriteRenderer.enabled = true; spriteRenderer.sprite = _rest; }
                        break;
                    }
                    if (_t < 0f && near) { _t = 0f; _fleeDir = cat.x > _home.x ? -1f : 1f; }   // foge para o lado contrário do gato (decidido no susto)
                    if (_t >= 0f)
                    {
                        _t += dt;
                        float dir = _fleeDir;
                        spriteRenderer.flipX = dir < 0f;
                        spriteRenderer.sprite = frames[(int)(_t / frameTime) % frames.Length];
                        transform.position += new Vector3(fleeVelocity.x * dir, fleeVelocity.y, 0f) * dt;
                        // some só quando sai da tela (pássaro) ou quando chega ao esconderijo (rato no buraco); 8 s no máximo
                        var cam2 = Camera.main;
                        bool gone = untilOffscreen
                            ? cam2 == null || Offscreen(transform.position, cam2.transform.position, 1f) || _t > 8f
                            : _t >= fleeTime;
                        if (gone) { _t = -1f; _away = true; spriteRenderer.enabled = false; }
                    }
                    break;

                case Mode.Lean:
                {
                    float vx = (cat.x - _lastX) / Mathf.Max(0.0001f, dt);
                    if (near && Mathf.Abs(vx) > 1.5f) { _t = 0f; Hold(true); spriteRenderer.sprite = frames[0]; spriteRenderer.flipX = vx < 0f; }
                    if (_t >= 0f) { _t += dt; if (_t > 0.45f) { _t = -1f; spriteRenderer.sprite = _rest; spriteRenderer.flipX = false; Hold(false); } }
                    break;
                }
            }
            _lastX = cat.x;
        }
    }
}
