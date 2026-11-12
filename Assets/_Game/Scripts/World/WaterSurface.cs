using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Melhoria 2: a água reage ao gato. Fica na lagoa, com um gatilho do tamanho da água.
    /// Entrando caindo forte (mais rápido que <see cref="BigSplashSpeed"/>, ex.: o salto de fé): respingo grande e dois
    /// anéis. Queda curta ou pulo: respingo pequeno e um anel. Andando dentro: ondinhas nos pés a cada 0,4 s.
    /// Saindo: o gato pinga por 0,5 s. Efeitos de uma vez num pool de no máximo <see cref="MaxAlive"/>; nada fora da água.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterSurface : MonoBehaviour
    {
        public const int MaxAlive = 6;

        [Tooltip("Altura da linha da água no mundo (tiles)")]
        [SerializeField] float surfaceY;
        [Tooltip("Caindo mais rápido que isto (tiles/s) ao entrar: respingo grande")]
        [SerializeField, Min(0f)] float bigSplashSpeed = 12f;
        [Tooltip("Entrando mais devagar que isto (tiles/s, para baixo): só a ondinha (entrou andando pela beira)")]
        [SerializeField, Min(0f)] float smallSplashSpeed = 2f;
        [Tooltip("Intervalo entre ondinhas nos pés andando na água (s)")]
        [SerializeField, Min(0.05f)] float rippleInterval = 0.4f;
        [Tooltip("Tempo pingando depois de sair (s)")]
        [SerializeField, Min(0f)] float dripTime = 0.5f;
        [SerializeField] Sprite[] bigSplash;
        [SerializeField] Sprite[] smallSplash;
        [SerializeField] Sprite[] footRipple;
        [SerializeField] Sprite[] ring;
        [SerializeField] float splashFps = 14f, rippleFps = 10f, ringFps = 8f;
        [SerializeField] int sortingOrder = 9;

        struct Fx { public SpriteRenderer Renderer; public Sprite[] Frames; public float Fps, Age, Delay; }

        readonly List<Fx> _alive = new List<Fx>();
        readonly Stack<SpriteRenderer> _free = new Stack<SpriteRenderer>();
        PlayerController _inside;
        float _rippleTimer, _dripUntil, _nextDrip;

        public float SurfaceY => surfaceY;
        public float BigSplashSpeed => bigSplashSpeed;
        /// <summary>Efeitos tocando agora (testes).</summary>
        public int AliveCount => _alive.Count;
        /// <summary>Último respingo: 2 = grande, 1 = pequeno, 0 = nenhum ou só ondinha (testes).</summary>
        public int LastSplash { get; private set; }
        /// <summary>Quando foi o último respingo (o som da aterrissagem não toca junto com o da água).</summary>
        public static float LastSplashAt { get; private set; } = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => LastSplashAt = -99f; // Play sem recarregar o domínio

        public void Configure(float surface, Sprite[] big, Sprite[] small, Sprite[] ripple, Sprite[] rings)
        {
            surfaceY = surface;
            bigSplash = big; smallSplash = small; footRipple = ripple; ring = rings;
        }

        /// <summary>Regra pura (EditMode): qual respingo para esta velocidade vertical ao entrar (2 grande, 1 pequeno, 0 nenhum).</summary>
        public static int SplashFor(float velocityY, float bigSpeed, float smallSpeed)
        {
            if (velocityY <= -bigSpeed) return 2;
            if (velocityY <= -smallSpeed) return 1;
            return 0;
        }

        /// <summary>Regra pura (EditMode): 0 a 1 pela altura da queda (tiles). Pulo normal ~0,08; o salto de fé (40+) = 1; sem altura (-1) = 1.</summary>
        public static float SplashLoudness(float fallHeight) => fallHeight < 0f ? 1f : Mathf.InverseLerp(1f, 40f, fallHeight);

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            _inside = player;
            var audio = player.GetComponent<SeteVidas.Audio.PlayerAudio>();
            Splash(player.Body.position.x, player.Velocity.y, audio != null ? audio.FallHeight : -1f);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != _inside) return;
            _inside = null;
            _dripUntil = Time.time + dripTime;   // saiu molhado
        }

        /// <summary>O respingo de uma entrada na água em <paramref name="x"/> com esta velocidade vertical.</summary>
        public void Splash(float x, float velocityY, float fallHeight = -1f)
        {
            LastSplash = SplashFor(velocityY, bigSplashSpeed, smallSplashSpeed);
            if (LastSplash > 0)
            {
                LastSplashAt = Time.time;
                // o mesmo respingo, com o volume da altura: o salto de fé cheio, um pulo da beira bem baixinho
                float k = SplashLoudness(fallHeight);
                if (fallHeight >= 0f && fallHeight < 1.5f) SeteVidas.Audio.Sfx.Play("gato_splash", null, 0.6f);
                else SeteVidas.Audio.Sfx.Play("gato_splash_grande", null, Mathf.Lerp(0.12f, 1f, k), Mathf.Lerp(1.1f, 1f, k));
            }
            if (LastSplash == 2)
            {
                Play(bigSplash, splashFps, x, 0f);
                Play(ring, ringFps, x, 0f);
                Play(ring, ringFps, x, 0.18f);
            }
            else if (LastSplash == 1)
            {
                Play(smallSplash, splashFps, x, 0f);
                Play(ring, ringFps, x, 0f);
            }
            else Play(footRipple, rippleFps, x, 0f);
            _rippleTimer = rippleInterval;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // andando dentro da água: ondinhas nos pés
            if (_inside != null)
            {
                _rippleTimer -= dt;
                if (_rippleTimer <= 0f && Mathf.Abs(_inside.Velocity.x) > 0.5f && _inside.IsGrounded)
                {
                    _rippleTimer = rippleInterval;
                    Play(footRipple, rippleFps, _inside.Body.position.x, 0f, _inside.Velocity.x < 0f);
                }
            }
            else if (Time.time < _dripUntil)
            {
                // pingando: gotinhas caem do pelo
                var player = PlayerHealth.Active != null ? PlayerHealth.Active.GetComponent<PlayerController>() : null;
                _nextDrip -= dt;
                if (player != null && _nextDrip <= 0f)
                {
                    _nextDrip = 0.12f;
                    SeteVidas.Combat.HitSparks.WaterDrop(player.Body.position + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.3f, 1f)));
                }
            }

            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var fx = _alive[i];
                fx.Age += dt;
                float t = fx.Age - fx.Delay;
                int frame = t < 0f ? -1 : (int)(t * fx.Fps);
                if (frame >= fx.Frames.Length)
                {
                    fx.Renderer.enabled = false;
                    _free.Push(fx.Renderer);
                    _alive.RemoveAt(i);
                    continue;
                }
                fx.Renderer.enabled = frame >= 0;
                if (frame >= 0) fx.Renderer.sprite = fx.Frames[frame];
                _alive[i] = fx;
            }
        }

        void Play(Sprite[] frames, float fps, float x, float delay, bool flip = false)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            if (_alive.Count >= MaxAlive)
            {
                // cheio: o mais antigo dá lugar
                var old = _alive[0];
                old.Renderer.enabled = false;
                _free.Push(old.Renderer);
                _alive.RemoveAt(0);
            }
            SpriteRenderer r;
            if (_free.Count > 0) r = _free.Pop();
            else
            {
                var go = new GameObject("Respingo");
                go.transform.SetParent(transform, false);
                r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = sortingOrder;
            }
            r.sprite = frames[0];
            r.flipX = flip;
            r.enabled = delay <= 0f;
            // a base do efeito na linha da água (1 px dentro), no grid de pixel
            const float px = 1f / 16f;
            float y = surfaceY + frames[0].bounds.extents.y - px;
            r.transform.position = new Vector3(Mathf.Round(x * 16f) / 16f, Mathf.Round(y * 16f) / 16f, 0f);
            _alive.Add(new Fx { Renderer = r, Frames = frames, Fps = fps, Delay = delay });
        }
    }
}
