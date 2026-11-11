using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Partículas de ambiente sem física (plano de assets, 6.1 e 6.2): poeira subindo nos feixes de luz, folhas caindo
    /// com o vento, vaga-lumes piscando, poeira roxa da praga. Pixel perfeito (posição arredondada para 1/16 de tile),
    /// poucas de cada vez e um teto global de <see cref="GlobalCap"/> na cena. Longe da câmera, o emissor some e para.
    /// Só cenário: não colide, não faz barulho, nunca usa o vermelho das armadilhas.
    /// </summary>
    public class AmbientEmitter : MonoBehaviour
    {
        public enum Kind { Dust, Leaves, Fireflies, PlagueDust }

        /// <summary>Máximo de partículas vivas ao mesmo tempo na cena (orçamento por tela, 6.1).</summary>
        public const int GlobalCap = 40;
        static int s_alive;
        public static int AliveInScene => s_alive;

        [SerializeField] Kind kind = Kind.Dust;
        [Tooltip("Onde as partículas nascem (mundo, tiles)")]
        [SerializeField] Rect area;
        [Tooltip("Partículas por segundo")]
        [SerializeField, Min(0f)] float rate = 0.5f;
        [SerializeField, Min(1)] int maxAlive = 6;
        [SerializeField] Sprite[] sprites = new Sprite[0];
        [SerializeField] int sortingOrder = 8;

        class Particle
        {
            public Transform t; public SpriteRenderer sr;
            public Vector2 pos, vel; public float age, life, phase; public bool alive;
        }

        readonly List<Particle> _pool = new List<Particle>();
        float _accum;
        Transform _camera;
        int _seed;

        public Kind EmitterKind => kind;
        public int Alive { get { int n = 0; foreach (var p in _pool) if (p.alive) n++; return n; } }

        public void Configure(Kind newKind, Rect newArea, float newRate, int newMax, Sprite[] newSprites, int order)
        {
            kind = newKind; area = newArea; rate = newRate; maxAlive = newMax; sprites = newSprites; sortingOrder = order;
        }

        void Awake() { _seed = Mathf.Abs((int)(area.x * 31f + area.y * 17f + area.width * 7f)) % 9973 + 1; }   // semente fixa pela área

        void OnDisable()
        {
            foreach (var p in _pool) if (p.alive) Kill(p);
        }

        void Update()
        {
            if (_camera == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                _camera = cam.transform;
            }
            // longe da câmera: nada nasce e as vivas somem (economia; a tela tem 30×17 tiles)
            Vector2 c = _camera.position;
            var view = new Rect(c.x - 17f, c.y - 10f, 34f, 20f);
            if (!view.Overlaps(area))
            {
                foreach (var p in _pool) if (p.alive) Kill(p);
                _accum = 0f;
                return;
            }
            float dt = Time.deltaTime;
            _accum += rate * dt;
            while (_accum >= 1f)
            {
                _accum -= 1f;
                if (Alive < maxAlive && s_alive < GlobalCap) Spawn();
            }
            foreach (var p in _pool) if (p.alive) Step(p, dt);
        }

        float Rand() { _seed = (_seed * 1103515245 + 12345) & 0x7fffffff; return (_seed % 10000) / 10000f; }

        void Spawn()
        {
            Particle p = null;
            foreach (var q in _pool) if (!q.alive) { p = q; break; }
            if (p == null)
            {
                var go = new GameObject("Partícula");
                go.transform.SetParent(transform, false);
                p = new Particle { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
                p.sr.sortingOrder = sortingOrder;
                _pool.Add(p);
            }
            p.alive = true; p.age = 0f; p.phase = Rand() * 6.28f;
            p.sr.sprite = sprites.Length > 0 ? sprites[(int)(Rand() * sprites.Length) % sprites.Length] : null;
            p.sr.enabled = true;
            float x = area.xMin + Rand() * area.width;
            switch (kind)
            {
                case Kind.Leaves:
                    p.pos = new Vector2(x, area.yMax); p.vel = new Vector2(0f, -0.55f - Rand() * 0.25f); p.life = 9f; break;
                case Kind.Fireflies:
                    p.pos = new Vector2(x, area.yMin + Rand() * area.height); p.vel = Vector2.zero; p.life = 6f + Rand() * 6f; break;
                case Kind.PlagueDust:
                    p.pos = new Vector2(x, area.yMin + Rand() * area.height); p.vel = new Vector2(0f, 0.05f); p.life = 7f + Rand() * 5f; break;
                default:
                    p.pos = new Vector2(x, area.yMin + Rand() * area.height * 0.4f); p.vel = new Vector2(0f, 0.1f); p.life = 6f + Rand() * 4f; break;
            }
            s_alive++;
            Place(p);
        }

        void Step(Particle p, float dt)
        {
            p.age += dt;
            bool gone = kind == Kind.Leaves
                ? p.pos.y < area.yMin - 0.5f                                    // a folha chegou ao chão
                : p.pos.y > area.yMax + 1f || p.pos.y < area.yMin - 1f;          // saiu do feixe de luz
            if (p.age >= p.life || gone) { Kill(p); return; }
            float a;
            switch (kind)
            {
                case Kind.Leaves:
                {
                    // caem balançando e seguem o vento da área; trocam de desenho para "virar" no ar
                    Vector2 wind = WindZone2D.At(p.pos);
                    p.pos += new Vector2(wind.x * 0.8f + Mathf.Sin(p.age * 3f + p.phase) * 0.35f, p.vel.y) * dt;
                    if (sprites.Length > 1) p.sr.sprite = sprites[((int)(p.age * 4f) + (int)p.phase) % sprites.Length];
                    a = Fade(p.age, p.life, 0.3f, 1.2f);
                    break;
                }
                case Kind.Fireflies:
                    p.pos += new Vector2(Mathf.Sin(p.age * 0.7f + p.phase) * 0.25f, Mathf.Cos(p.age * 0.9f + p.phase) * 0.15f) * dt;
                    a = Fade(p.age, p.life, 0.8f, 0.8f) * (0.5f + 0.5f * Mathf.Sin(p.age * 2.4f + p.phase));   // pisca
                    break;
                case Kind.PlagueDust:
                    p.pos += new Vector2(Mathf.Sin(p.age * 0.5f + p.phase) * 0.06f, p.vel.y) * dt;
                    a = Fade(p.age, p.life, 1.5f, 1.5f) * 0.6f;
                    break;
                default:
                    p.pos += new Vector2(Mathf.Sin(p.age * 0.8f + p.phase) * 0.08f, p.vel.y) * dt;
                    a = Fade(p.age, p.life, 1.5f, 1.5f) * (0.3f + 0.3f * Mathf.Sin(p.phase));   // alfa 30–60%
                    break;
            }
            var col = p.sr.color; col.a = Mathf.Clamp01(a); p.sr.color = col;
            Place(p);
        }

        /// <summary>Entra em <paramref name="fadeIn"/> s e sai nos últimos <paramref name="fadeOut"/> s.</summary>
        public static float Fade(float age, float life, float fadeIn, float fadeOut)
        {
            float a = fadeIn > 0f ? Mathf.Clamp01(age / fadeIn) : 1f;
            if (fadeOut > 0f) a = Mathf.Min(a, Mathf.Clamp01((life - age) / fadeOut));
            return a;
        }

        void Place(Particle p)
        {
            const float px = 1f / 16f;
            p.t.position = new Vector3(Mathf.Round(p.pos.x / px) * px, Mathf.Round(p.pos.y / px) * px, 0f);
        }

        void Kill(Particle p)
        {
            if (!p.alive) return;
            p.alive = false;
            p.sr.enabled = false;
            s_alive = Mathf.Max(0, s_alive - 1);
        }
    }
}
