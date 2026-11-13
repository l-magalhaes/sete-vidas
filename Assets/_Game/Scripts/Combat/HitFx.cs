using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Clarão branco e achatar do corpo atingido (melhoria 3). Fica no objeto do sprite: cria uma cópia do sprite
    /// pintada de branco por cima (shader <c>SeteVidas/SpriteSolidColor</c>) e escala o sprite por um instante.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        const string ShaderPath = "SeteVidas/SpriteSolidColor";
        static Material s_material;
        static bool s_loaded;

        SpriteRenderer _source;
        SpriteRenderer _overlay;
        float _flashUntil;
        float _squashUntil;
        Vector2 _squash = Vector2.one;
        Vector3 _baseScale = Vector3.one;
        bool _scaled;

        /// <summary>Material de cor sólida (branco ou qualquer cor do SpriteRenderer). Null se o shader não existir.</summary>
        public static Material SolidMaterial
        {
            get
            {
                if (!s_loaded)
                {
                    s_loaded = true;
                    var shader = Resources.Load<Shader>(ShaderPath);
                    if (shader != null) s_material = new Material(shader) { name = "Sprite cor sólida" };
                }
                return s_material;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_material = null; s_loaded = false; } // Play sem recarregar o domínio

        public bool IsFlashing => Time.time < _flashUntil;

        /// <summary>O HitFlash do sprite (cria se faltar).</summary>
        public static HitFlash For(SpriteRenderer sprite)
        {
            if (sprite == null) return null;
            var flash = sprite.GetComponent<HitFlash>();
            if (flash == null) flash = sprite.gameObject.AddComponent<HitFlash>();
            return flash;
        }

        void Awake()
        {
            _source = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        public void Flash(float seconds)
        {
            if (seconds <= 0f) return;
            _flashUntil = Mathf.Max(_flashUntil, Time.time + seconds);
            EnsureOverlay();
        }

        public void Squash(int px, float seconds)
        {
            if (px <= 0 || seconds <= 0f || _source == null || _source.sprite == null) return;
            var r = _source.sprite.rect;
            // o sprite do inimigo tem margem no canvas: usa a parte ocupada (~metade da largura, ~70% da altura)
            _squash = HitReactionRules.SquashScale(px, r.width * 0.5f, r.height * 0.7f);
            _squashUntil = Time.time + seconds;
        }

        void EnsureOverlay()
        {
            if (_overlay != null || _source == null || SolidMaterial == null) return;
            var go = new GameObject("Clarão") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            _overlay = go.AddComponent<SpriteRenderer>();
            _overlay.sharedMaterial = SolidMaterial;
            _overlay.color = Color.white;
            _overlay.enabled = false;
        }

        void LateUpdate()
        {
            if (_source == null) return;

            bool squashing = Time.time < _squashUntil;
            if (squashing) { transform.localScale = new Vector3(_baseScale.x * _squash.x, _baseScale.y * _squash.y, _baseScale.z); _scaled = true; }
            else if (_scaled) { transform.localScale = _baseScale; _scaled = false; }

            if (_overlay == null) return;
            bool on = IsFlashing && _source.sprite != null;
            _overlay.enabled = on;
            if (!on) return;
            _overlay.sprite = _source.sprite;
            _overlay.flipX = _source.flipX;
            _overlay.flipY = _source.flipY;
            _overlay.sortingLayerID = _source.sortingLayerID;
            _overlay.sortingOrder = _source.sortingOrder + 1;
        }
    }

    /// <summary>Faíscas na armadura e poeira de impacto: pontinhos de 1 px que voam e caem (pool de 128).</summary>
    public class HitSparks : MonoBehaviour
    {
        struct Bit { public Vector2 Velocity; public float Life, Age, Gravity; }

        const int Capacity = 128; // o terremoto e os golpes do Koda soltam muitos de uma vez
        static HitSparks s_instance;
        static Sprite s_pixel;

        static readonly Color SparkLight = new Color32(0xff, 0xf2, 0xd2, 0xff);
        static readonly Color SparkDim = new Color32(0xd8, 0xc8, 0xa0, 0xff);
        static readonly Color DustColor = new Color32(0x8c, 0x82, 0x74, 0xff);

        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        readonly List<Bit> _bits = new List<Bit>();
        int _next;

        /// <summary>Quantos pontinhos estão no ar (testes).</summary>
        public static int Alive
        {
            get
            {
                if (s_instance == null) return 0;
                int n = 0;
                foreach (var r in s_instance._renderers) if (r.enabled) n++;
                return n;
            }
        }

        /// <summary>Solta <paramref name="sparks"/> faíscas em <paramref name="at"/>, voando para o lado <paramref name="side"/>.</summary>
        public static void Spawn(Vector2 at, int sparks, int side, bool dust, Vector2 dustAt)
        {
            if (!Application.isPlaying || (sparks <= 0 && !dust)) return;
            if (s_instance == null)
            {
                var go = new GameObject("[Faíscas]");
                s_instance = go.AddComponent<HitSparks>();
            }
            for (int i = 0; i < sparks; i++)
            {
                float angle = Random.Range(-50f, 50f) * Mathf.Deg2Rad;
                float speed = Random.Range(4f, 8f);
                var v = new Vector2(side * Mathf.Cos(angle), Mathf.Sin(angle) + 0.4f) * speed;
                s_instance.Emit(at, v, Random.Range(0.12f, 0.25f), i % 2 == 0 ? SparkLight : SparkDim);
            }
            if (!dust) return;
            for (int i = 0; i < 6; i++)
            {
                float dir = i % 2 == 0 ? 1f : -1f;
                var v = new Vector2(dir * Random.Range(1.5f, 4f), Random.Range(0.5f, 2f));
                s_instance.Emit(dustAt + new Vector2(dir * 0.2f, 0.05f), v, Random.Range(0.25f, 0.4f), DustColor);
            }
        }

        static readonly Color GlintColor = new Color32(0xf2, 0xc9, 0x6b, 0xff);   // dourado do Paladino, mais claro

        static HitSparks Instance()
        {
            if (s_instance == null)
            {
                var go = new GameObject("[Faíscas]");
                s_instance = go.AddComponent<HitSparks>();
            }
            return s_instance;
        }

        static readonly Color WaterColor = new Color32(0x7f, 0xd0, 0xf0, 0xff);   // a mesma da gota da gárgula

        /// <summary>Uma gotinha d'água que pinga do gato (saiu da lagoa, melhoria 2).</summary>
        public static void WaterDrop(Vector2 at)
        {
            if (!Application.isPlaying) return;
            Instance().Emit(at, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.5f)), 0.35f, WaterColor);
        }

        /// <summary>Uma faísca dourada que sobe devagar (marreta carregando).</summary>
        public static void Glint(Vector2 at)
        {
            if (!Application.isPlaying) return;
            Instance().Emit(at, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(2.5f, 4f)), 0.3f, GlintColor);
        }

        /// <summary>Explosão de <paramref name="count"/> pontinhos para todos os lados, alternando as duas cores.</summary>
        public static void Burst(Vector2 at, int count, Color a, Color b, float speedMin = 3f, float speedMax = 7f,
                                 float life = 0.3f, float gravity = 20f, float upBias = 0.3f)
        {
            if (!Application.isPlaying || count <= 0) return;
            var inst = Instance();
            for (int i = 0; i < count; i++)
            {
                float ang = (i + Random.value * 0.6f) / count * Mathf.PI * 2f;
                var v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) + upBias) * Random.Range(speedMin, speedMax);
                inst.Emit(at, v, life * Random.Range(0.8f, 1.2f), i % 2 == 0 ? a : b, gravity);
            }
        }

        /// <summary>Um pontinho que sobe devagar (luz da cura, brasas): quase sem gravidade, para cima.</summary>
        public static void Rise(Vector2 at, Color color, float speed = 1.5f, float life = 0.6f)
        {
            if (!Application.isPlaying) return;
            Instance().Emit(at, new Vector2(Random.Range(-0.3f, 0.3f), speed), life, color, -0.5f);
        }

        /// <summary>Um pontinho com velocidade e gravidade escolhidas (lascas do terremoto, pedrinhas).</summary>
        public static void One(Vector2 at, Vector2 velocity, Color color, float life, float gravity = 30f)
        {
            if (!Application.isPlaying) return;
            Instance().Emit(at, velocity, life, color, gravity);
        }

        struct WaveFx { public Vector2 Origin; public float Range, Duration, Age, NextPuff; }
        readonly List<WaveFx> _waves = new List<WaveFx>();

        /// <summary>Terremoto: poeira correndo pelo chão para os dois lados até <paramref name="range"/> tiles.</summary>
        public static void Wave(Vector2 feet, float range, float duration)
        {
            if (!Application.isPlaying || range <= 0f) return;
            var inst = Instance();
            inst._waves.Add(new WaveFx { Origin = feet, Range = range, Duration = Mathf.Max(0.05f, duration) });
            Spawn(feet + Vector2.up * 0.1f, 0, 1, true, feet);
        }

        void StepWaves(float dt)
        {
            for (int i = _waves.Count - 1; i >= 0; i--)
            {
                var w = _waves[i];
                w.Age += dt;
                w.NextPuff -= dt;
                if (w.NextPuff <= 0f)
                {
                    w.NextPuff = 0.035f;
                    float d = w.Range * Mathf.Clamp01(w.Age / w.Duration);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var at = w.Origin + new Vector2(s * d, 0.06f);
                        Emit(at, new Vector2(s * Random.Range(0.5f, 1.5f), Random.Range(1.5f, 3f)), Random.Range(0.2f, 0.3f), DustColor);
                    }
                }
                if (w.Age >= w.Duration) _waves.RemoveAt(i);
                else _waves[i] = w;
            }
        }

        void Emit(Vector2 at, Vector2 velocity, float life, Color color, float gravity = 30f)
        {
            if (s_pixel == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply(false);
                s_pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
            }
            SpriteRenderer r;
            if (_renderers.Count < Capacity)
            {
                var go = new GameObject("faísca");
                go.transform.SetParent(transform, false);
                r = go.AddComponent<SpriteRenderer>();
                r.sprite = s_pixel;
                r.sortingOrder = 12;
                _renderers.Add(r);
                _bits.Add(default);
                _next = _renderers.Count - 1;
            }
            else
            {
                _next = (_next + 1) % Capacity; // cheio: reaproveita o mais antigo
            }
            r = _renderers[_next];
            r.color = color;
            r.enabled = true;
            r.transform.position = at;
            _bits[_next] = new Bit { Velocity = velocity, Life = life, Gravity = gravity };
        }

        void Update()
        {
            float dt = Time.deltaTime;
            StepWaves(dt);
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (!r.enabled) continue;
                var b = _bits[i];
                b.Age += dt;
                if (b.Age >= b.Life) { r.enabled = false; continue; }
                b.Velocity += Vector2.down * b.Gravity * dt;
                Vector2 p = (Vector2)r.transform.position + b.Velocity * dt;
                // no grid de pixel: nada de pontinho tremido entre dois pixels
                r.transform.position = new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);
                _bits[i] = b;
            }
        }

        void OnDestroy() { if (s_instance == this) s_instance = null; }
    }

    /// <summary>
    /// Tremor de câmera em pixels inteiros (golpes pesados). Desloca a câmera só durante a renderização, depois
    /// que o Cinemachine já a posicionou, então o tremor nunca se acumula nem briga com o seguimento.
    /// </summary>
    public static class CameraShake
    {
        static float s_until, s_px, s_duration;
        static bool s_hooked;
        static Vector3 s_offset;
        static Camera s_shaken;

        /// <summary>
        /// Zera o tremor a cada Play: o projeto entra no Play sem recarregar o domínio, e o relógio (unscaledTime)
        /// volta a zero; sem isto, um tremor da sessão anterior durava minutos (bug relatado: a tela toda tremendo).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_until = s_px = s_duration = 0f;
            s_offset = Vector3.zero;
            s_shaken = null;
            if (s_hooked)
            {
                RenderPipelineManager.beginCameraRendering -= Begin;
                RenderPipelineManager.endCameraRendering -= End;
                s_hooked = false;
            }
        }

        /// <summary>Nunca treme mais que o último pedido (protege de qualquer relógio fora do lugar).</summary>
        public static bool IsShaking
        {
            get
            {
                if (!Application.isPlaying) return false;
                float left = s_until - Time.unscaledTime;
                if (left > s_duration + 0.05f) { s_until = 0f; return false; }
                return left > 0f;
            }
        }

        public static void Shake(float px, float seconds)
        {
            if (px <= 0f || seconds <= 0f || !Application.isPlaying) return;
            if (s_until - Time.unscaledTime > s_duration + 0.05f) s_until = 0f; // relógio de outra sessão
            if (!s_hooked)
            {
                s_hooked = true;
                RenderPipelineManager.beginCameraRendering += Begin;
                RenderPipelineManager.endCameraRendering += End;
            }
            if (!IsShaking || px >= s_px) { s_px = px; s_duration = seconds; }
            s_until = Mathf.Max(s_until, Time.unscaledTime + seconds);
            s_duration = Mathf.Max(s_duration, s_until - Time.unscaledTime); // dois tremores juntos: vale o mais longo
        }

        static void Begin(ScriptableRenderContext context, Camera cam)
        {
            if (!IsShaking || cam != Camera.main) return;
            float left = (s_until - Time.unscaledTime) / Mathf.Max(0.01f, s_duration);
            int px = Mathf.Max(1, Mathf.RoundToInt(s_px * Mathf.Clamp01(left)));
            // alterna de lado a cada quadro, em pixels inteiros (1/16 de tile)
            int sx = (Time.frameCount & 1) == 0 ? px : -px;
            int sy = (Time.frameCount & 2) == 0 ? px / 2 : -(px / 2);
            s_offset = new Vector3(sx / 16f, sy / 16f, 0f);
            s_shaken = cam;
            cam.transform.position += s_offset;
        }

        static void End(ScriptableRenderContext context, Camera cam)
        {
            if (cam != s_shaken) return;
            cam.transform.position -= s_offset;
            s_shaken = null;
            s_offset = Vector3.zero;
        }
    }
}
