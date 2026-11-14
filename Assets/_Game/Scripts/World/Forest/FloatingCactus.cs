using SeteVidas.Enemies;
using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Cacto Flutuante (Docs/Design/floresta-bestiario.md): bola de espinhos que é ponto de pogo. Não morre.
    /// Encostar fere 1 hit e empurra; a estocada para baixo quica nela (e dá a graça do pogo, que protege do toque).
    /// Pode flutuar num vaivém (<see cref="travel"/> a partir do ponto inicial, ida e volta em <see cref="period"/> s).
    /// <para>Variante <b>infectada</b> (F4, componente <see cref="Infected"/>): a trajetória treme (até 0,5 tile) e, a
    /// cada 3 s, incha por 0,5 s (aviso) e solta 4 espinhos em cruz, com alcance de 5 tiles.</para>
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    public class FloatingCactus : TrapBase
    {
        [Tooltip("Raio da bola (tiles)")]
        [SerializeField, Min(0.2f)] float radius = 0.6f;
        [Tooltip("Deslocamento máximo a partir do ponto inicial (tiles). Zero = parado")]
        [SerializeField] Vector2 travel = Vector2.zero;
        [Tooltip("Tempo de uma ida e volta (s)")]
        [SerializeField, Min(0.5f)] float period = 3f;
        [SerializeField] Transform visual;

        Vector3 _home;
        float _phase;
        bool _infected;
        float _sinceShot;
        float _seed;

        static Sprite s_spike;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_spike = null; // Play sem recarregar o domínio

        public bool IsInfected => _infected;

        void Reset() => returnToSafeGround = false;

        void Awake()
        {
            _home = transform.position;
            _phase = Random.value * period; // vários cactos não andam em sincronia
            _infected = Infected.On(this);
            _seed = Random.value * 100f;
            _sinceShot = Random.value * ForestEnemyRules.CactusShotEvery * 0.5f; // nem todos disparam juntos
        }

        protected override void OnEnable() { base.OnEnable(); SeteVidas.Core.GameEvents.WorldReset += OnWorldReset; }
        protected override void OnDisable() { base.OnDisable(); SeteVidas.Core.GameEvents.WorldReset -= OnWorldReset; }
        void OnWorldReset() => _sinceShot = 0f;

        /// <summary>Para o setup do editor.</summary>
        public void Configure(float r, Vector2 move, float seconds)
        {
            radius = r;
            travel = move;
            period = seconds;
        }

        protected override System.Func<Rect> PogoArea => DangerArea;

        protected override Rect DangerArea()
        {
            Vector2 c = transform.position;
            return new Rect(c - Vector2.one * radius, Vector2.one * radius * 2f);
        }

        /// <summary>Posição no vaivém (seno: freia nas pontas). Regra pura para testes.</summary>
        public static Vector2 PathOffset(Vector2 travel, float period, float t) =>
            travel * Mathf.Sin(t / Mathf.Max(0.01f, period) * Mathf.PI * 2f);

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _phase += dt;
            Vector2 offset = travel != Vector2.zero ? PathOffset(travel, period, _phase) : Vector2.zero;
            if (_infected) offset += ForestEnemyRules.CactusJitterOffset(_phase, _seed);
            if (travel != Vector2.zero || _infected) transform.position = _home + (Vector3)offset;

            // a caixa que fere é um pouco menor que a de pogo: raspar a ponta dos espinhos não pune
            HitPlayerIn(transform.position, Vector2.one * radius * 1.5f);

            if (_infected) StepShots(dt);
        }

        void StepShots(float dt)
        {
            _sinceShot += dt;
            if (!ForestEnemyRules.CactusFires(_sinceShot)) return;
            _sinceShot = 0f;
            // só dispara com o gato por perto (não enche a sala de espinhos à toa)
            var h = SeteVidas.Player.PlayerHealth.Active;
            if (h == null || h.IsDead || Vector2.Distance(h.transform.position, transform.position) > 14f) return;
            Vector2 c = transform.position;
            for (int i = 0; i < 4; i++)
            {
                Vector2 dir = ForestEnemyRules.CactusSpikeDir(i);
                SeteVidas.Enemies.EnemyBolt.Fire(SpikeSprite(), c + dir * (radius + 0.1f), dir * ForestEnemyRules.CactusSpikeSpeed,
                    damage, gameObject, 6, ForestEnemyRules.CactusSpikeRange - radius);
            }
            SeteVidas.Audio.Sfx.Play("virote_crava", c, 0.6f, 1.5f); // provisório: o disparo
        }

        void Update()
        {
            if (visual == null) return;
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f + _phase) * 8f); // balança de leve
            // infectado: incha antes de disparar (aviso de 0,5 s)
            float s = 1f;
            if (_infected && ForestEnemyRules.CactusSwelling(_sinceShot))
                s = 1.15f + 0.08f * Mathf.Sin(Time.time * 40f);
            visual.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Espinho (8 × 3 px, verde-ácido com a ponta clara), feito na hora: não depende de arte nova.</summary>
        static Sprite SpikeSprite()
        {
            if (s_spike != null) return s_spike;
            const int w = 8, h = 3;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "cacto_espinho", hideFlags = HideFlags.DontSave
            };
            Color32 clear = new Color32(0, 0, 0, 0), tip = new Color32(0xe8, 0xf8, 0xc8, 0xff);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool body = y == 1 || x < 5;
                    px[y * w + x] = !body ? clear : x >= 6 ? tip : (y == 1 ? Infected.Acid : Infected.Purple);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            s_spike = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            s_spike.hideFlags = HideFlags.DontSave;
            return s_spike;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            if (travel == Vector2.zero) return;
            Vector3 home = Application.isPlaying ? _home : transform.position;
            Gizmos.color = new Color(0.4f, 1f, 0.9f, 0.5f);
            Gizmos.DrawLine(home - (Vector3)travel, home + (Vector3)travel);
        }
#endif
    }
}
