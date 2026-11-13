using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// Um relâmpago do Koda. Aviso: runas douradas acendem no chão e a coluna se enche de pontilhado cada vez mais
    /// denso, com as bordas tracejadas subindo (onde ele VAI cair). Depois o raio cai em zigue-zague animado,
    /// com um clarão de 1 quadro, faíscas e uma marca queimada no chão; machuca quem estiver na coluna. Some sozinho.
    /// Tudo em pixel inteiro, sem sprite esticado nem fade contínuo (pedido do usuário, 12/11/2026: coeso com o jogo).
    /// </summary>
    public class LightningStrike : MonoBehaviour
    {
        [SerializeField] SpriteRenderer telegraph;
        [SerializeField] SpriteRenderer bolt;
        [SerializeField] LayerMask playerLayers;

        const int BoltFrames = 4;
        const float BoltFrameTime = 0.04f;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 White = new Color32(0xff, 0xff, 0xf4, 0xff);
        static readonly Color32 Glow = new Color32(0xff, 0xf4, 0xc2, 0xff);
        static readonly Color32 Gold = new Color32(0xf2, 0xc9, 0x6b, 0xff);
        static readonly Color32 Deep = new Color32(0xa8, 0x70, 0x2a, 0xff);
        static readonly Color32 Scorch = new Color32(0x1e, 0x18, 0x26, 0xff);

        static readonly Dictionary<long, Sprite> s_cache = new Dictionary<long, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_cache.Clear(); // Play sem recarregar o domínio

        float _width, _height, _telegraphTime, _strikeTime, _age, _nextMote;
        int _damage, _wPx, _hPx, _seed;
        bool _struck, _hit;
        GameObject _source;

        public bool HasStruck => _struck;
        public float Width => _width;

        /// <param name="floor">Pés da coluna (chão da arena).</param>
        public void Init(Vector2 floor, float width, float height, float telegraphTime, float strikeTime, int damage, GameObject source)
        {
            transform.position = PixelFont.Snap(floor);
            _width = width;
            _height = height;
            _telegraphTime = telegraphTime;
            _strikeTime = strikeTime;
            _damage = damage;
            _source = source;
            _wPx = Mathf.Max(4, Mathf.RoundToInt(width * PixelFont.Ppu));
            _hPx = Mathf.Max(8, Mathf.RoundToInt(height * PixelFont.Ppu));
            _seed = Random.Range(0, 8); // 8 variações de raio (o cache de texturas não cresce sem limite)
            if (playerLayers == 0) playerLayers = LayerMask.GetMask(Core.GameConstants.Layers.Player);
            Audio.Sfx.Play("raio_aviso", floor + Vector2.up);

            // os dois renderers do prefab passam a mostrar sprites no tamanho exato (pivô no chão, sem escala)
            if (telegraph != null)
            {
                telegraph.transform.localPosition = Vector3.zero;
                telegraph.transform.localScale = Vector3.one;
                telegraph.color = Color.white;
                telegraph.sprite = Column(0, 0);
            }
            if (bolt != null)
            {
                bolt.transform.localPosition = Vector3.zero;
                bolt.transform.localScale = Vector3.one;
                bolt.color = Color.white;
                bolt.enabled = false;
            }
        }

        void Update()
        {
            _age += Time.deltaTime;

            if (!_struck)
            {
                // Aviso: o pontilhado fica mais denso e as bordas sobem mais rápido perto do raio
                float k = Mathf.Clamp01(_age / Mathf.Max(0.01f, _telegraphTime));
                int density = k < 0.4f ? 0 : k < 0.75f ? 1 : 2;
                int crawl = (int)(_age * Mathf.Lerp(10f, 24f, k)) & 3;
                if (telegraph != null) telegraph.sprite = Column(density, crawl);
                // centelhas subindo das runas
                _nextMote -= Time.deltaTime;
                if (_nextMote <= 0f)
                {
                    _nextMote = Mathf.Lerp(0.12f, 0.04f, k);
                    Vector2 p = (Vector2)transform.position + new Vector2(Random.Range(-_width * 0.45f, _width * 0.45f), 0.15f);
                    HitSparks.Rise(p, Random.value < 0.5f ? (Color)Glow : Gold, Random.Range(1.5f, 3f), 0.5f);
                }
                if (_age >= _telegraphTime) Strike();
                return;
            }

            if (bolt != null)
            {
                float t = _age - _telegraphTime;
                // 1º quadro: a coluna inteira acende (clarão); depois o zigue-zague troca a cada 0,04 s
                bolt.sprite = t < BoltFrameTime ? Flash() : Bolt((int)(t / BoltFrameTime) % BoltFrames);
            }
            if (!_hit) TryHit();
            if (_age >= _telegraphTime + _strikeTime) Destroy(gameObject);
        }

        void Strike()
        {
            _struck = true;
            Audio.Sfx.Play("raio", (Vector2)transform.position + Vector2.up);
            if (telegraph != null) telegraph.enabled = false;
            if (bolt != null) { bolt.enabled = true; bolt.sprite = Flash(); }
            Vector2 floor = transform.position;
            HitSparks.Burst(floor + Vector2.up * 0.1f, 10, Glow, Gold, 3f, 7f, 0.3f, 25f, 0.8f);
            PixelRing.Spawn(floor + Vector2.up * 0.1f, 3, _wPx / 2 + 10, 0.2f, Glow, 1, 0f, 13, 0.3f);
            CameraShake.Shake(1f, 0.1f);
            // marca queimada: escura, depois pontilhada, depois some (SpriteFx não depende deste objeto)
            SpriteFx.Play(new[] { ScorchMark(0), ScorchMark(0), ScorchMark(1), ScorchMark(2) }, 0.35f,
                          transform.position, false, Color.white, 0, 3);
            TryHit();
        }

        void TryHit()
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * (_height * 0.5f);
            var col = Physics2D.OverlapBox(center, new Vector2(_width * 0.9f, _height), 0f, playerLayers);
            if (col == null) return;
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null) return;
            float dir = col.transform.position.x >= transform.position.x ? 1f : -1f;
            target.TakeDamage(new DamageInfo(_damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, _source));
            _hit = true;
        }

        // ------------------------------------------------------------ desenho

        Sprite Cached(long kind, System.Func<Sprite> make, bool seeded = false)
        {
            long key = ((kind * 4096 + _wPx) * 4096 + _hPx) * 16 + (seeded ? _seed + 1 : 0);
            if (s_cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            s_cache[key] = s;
            return s;
        }

        /// <summary>Coluna do aviso: pontilhado (1/8, 1/4, 1/2), bordas tracejadas que sobem e runas no chão.</summary>
        Sprite Column(int density, int crawl) => Cached(1 + density * 4 + crawl, () =>
        {
            int w = _wPx, h = _hPx;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = Clear;
                    bool edge = x == 0 || x == w - 1;
                    if (edge) { if ((((y + 64 - crawl) / 2) & 1) == 0) c = density == 2 ? Glow : Gold; }
                    else
                    {
                        bool dot = density == 0 ? (x % 4 == 0 && y % 4 == ((x / 4) & 1) * 2)
                                 : density == 1 ? (x % 2 == 0 && y % 4 == ((x / 2) & 1) * 2)
                                 : ((x + y) & 1) == 0 && y % 2 == 0;
                        // o pontilhado vai sumindo para o alto (a coluna "nasce" do chão)
                        if (dot && y < h * (0.45f + 0.25f * density)) c = Gold;
                    }
                    px[y * w + x] = c;
                }
            // runas no chão: losangos de 3×3 alternando claro e dourado
            for (int x = 2; x + 2 < w; x += 5)
            {
                Color32 rc = ((x / 5 + density) & 1) == 0 ? Glow : Gold;
                Set(px, w, h, x + 1, 0, rc); Set(px, w, h, x, 1, rc); Set(px, w, h, x + 2, 1, rc); Set(px, w, h, x + 1, 2, rc);
            }
            return PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0f), "aviso do raio");
        });

        /// <summary>Clarão de 1 quadro: a coluna inteira clara com o miolo branco.</summary>
        Sprite Flash() => Cached(100, () =>
        {
            int w = _wPx, h = _hPx;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int edgeDist = Mathf.Min(x, w - 1 - x);
                    px[y * w + x] = edgeDist == 0 ? Gold : edgeDist < w / 4 ? Glow : White;
                }
            return PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0f), "clarão do raio");
        });

        /// <summary>
        /// Raio em zigue-zague: miolo branco de 2 px, borda clara e dourada, um galho curto. Cada quadro tem um
        /// caminho diferente (semente do relâmpago + quadro), então o raio "treme" sem esticar nada.
        /// </summary>
        Sprite Bolt(int frame) => Cached(200 + frame, () =>
        {
            int w = _wPx, h = _hPx;
            var px = new Color32[w * h];
            var rng = new System.Random(_seed * 31 + frame * 7 + 1);
            int cx = w / 2, x = cx;
            int branchAt = h / 3 + rng.Next(h / 3);
            int bx = 0, bdir = rng.Next(2) == 0 ? -1 : 1, blen = 0;
            for (int y = h - 1; y >= 0; y--)
            {
                if (y % 3 == 0) x = Mathf.Clamp(x + rng.Next(-2, 3), 3, w - 4);
                if (y == 0) x = cx; // termina no meio da coluna (onde está a marca)
                Stroke(px, w, h, x, y);
                if (y == branchAt) { bx = x; blen = 6 + rng.Next(8); }
                if (blen > 0)
                {
                    bx += bdir * (rng.Next(3) == 0 ? 0 : 1);
                    if (bx > 1 && bx < w - 2) { Set(px, w, h, bx, y, Glow); Set(px, w, h, bx + bdir, y, Gold); }
                    blen--;
                }
            }
            return PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0f), "raio " + frame);
        }, true);

        static void Stroke(Color32[] px, int w, int h, int x, int y)
        {
            Set(px, w, h, x - 2, y, Deep);
            Set(px, w, h, x - 1, y, Gold);
            Set(px, w, h, x, y, White);
            Set(px, w, h, x + 1, y, White);
            Set(px, w, h, x + 2, y, Glow);
            Set(px, w, h, x + 3, y, Gold);
        }

        /// <summary>Marca queimada no chão: risco escuro com brasas, depois pontilhada.</summary>
        Sprite ScorchMark(int dither) => Cached(300 + dither, () =>
        {
            int w = _wPx + 6, h = 2;
            var px = new Color32[w * h];
            var rng = new System.Random(_seed + 5);
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(x, w - 1 - x);
                if (d < 2 && rng.Next(2) == 0) continue;
                bool keep = dither == 0 || (dither == 1 ? (x & 1) == 0 : x % 3 == 0);
                if (!keep) continue;
                px[x] = Scorch;
                if (d > 3 && rng.Next(5) == 0) px[w + x] = dither == 0 ? Gold : Scorch; // brasa
                else if (d > 1) px[w + x] = Scorch;
            }
            // pivô no topo: a marca fica dentro do chão, logo abaixo da superfície
            return PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 1f), "marca do raio");
        }, true);

        static void Set(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }
    }
}
