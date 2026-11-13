using System.Collections.Generic;
using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Terremoto visível (pedido do usuário, 12/11/2026): rachadura no ponto do impacto, placas do chão saltando
    /// em sequência atrás da frente da onda, pedrinhas pulando, clarão achatado no chão e a poeira de antes.
    /// Tudo em pixel inteiro e sem transparência (some em pontilhado). A versão dourada é a do Koda.
    /// Usado pela marreta do gato (CombatController) e pelo pouso do salto e pela marretada do Koda (GoldenBoss).
    /// </summary>
    public class QuakeFx : MonoBehaviour
    {
        const float SlabStep = 0.5f;     // uma placa a cada 8 px
        const float SlabTime = 0.18f;    // sobe e desce
        const float CrackHold = 0.55f, CrackFade = 0.25f;

        static readonly Color32 StoneLight = new Color32(0x9a, 0x8c, 0x7c, 0xff);
        static readonly Color32 StoneMid = new Color32(0x6b, 0x5e, 0x54, 0xff);
        static readonly Color32 StoneDark = new Color32(0x3a, 0x30, 0x34, 0xff);
        static readonly Color32 CrackColor = new Color32(0x1e, 0x18, 0x26, 0xff);
        static readonly Color32 GoldLight = new Color32(0xff, 0xf4, 0xc2, 0xff);
        static readonly Color32 Gold = new Color32(0xf2, 0xc9, 0x6b, 0xff);

        static readonly Dictionary<int, Sprite> s_cracks = new Dictionary<int, Sprite>();
        static Sprite s_slab, s_slabGold;
        static int s_groundMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_cracks.Clear(); s_slab = s_slabGold = null; s_groundMask = -1; } // Play sem recarregar o domínio

        struct Slab { public SpriteRenderer R; public Vector3 Rest; public float Age; }

        Vector2 _origin;
        float _range, _duration, _strength, _age;
        bool _golden;
        float _nextSide; // até onde já saíram placas (tiles a partir do centro)
        readonly List<Slab> _slabs = new List<Slab>();
        SpriteRenderer _crack;
        int _crackHalf;

        /// <summary>Quantos terremotos estão tocando (testes).</summary>
        public static int Alive { get; private set; }

        /// <param name="feet">Ponto do impacto, na superfície do chão.</param>
        /// <param name="range">Alcance para cada lado (tiles).</param>
        /// <param name="duration">Tempo da frente da onda ir do centro até o alcance.</param>
        /// <param name="strength01">0 = pancada leve (queda com a marreta), 1 = golpe carregado no máximo.</param>
        public static QuakeFx Spawn(Vector2 feet, float range, float duration, float strength01, bool golden = false)
        {
            if (!Application.isPlaying || range <= 0f) return null;
            if (s_groundMask < 0) s_groundMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Breakable);
            var go = new GameObject(golden ? "Terremoto dourado" : "Terremoto");
            var fx = go.AddComponent<QuakeFx>();
            fx._origin = feet;
            fx._range = range;
            fx._duration = Mathf.Max(0.05f, duration);
            fx._strength = Mathf.Clamp01(strength01);
            fx._golden = golden;
            fx._nextSide = SlabStep;
            fx.Begin();
            Alive++;
            return fx;
        }

        void Begin()
        {
            // poeira correndo pelo chão (o efeito antigo continua por baixo)
            HitSparks.Wave(_origin, _range, _duration);

            // clarão achatado no chão
            Color flash = _golden ? (Color)GoldLight : new Color32(0xf4, 0xee, 0xe0, 0xff);
            int ringTo = Mathf.RoundToInt(10f + 14f * _strength);
            PixelRing.Spawn(_origin + Vector2.up * 0.1f, 3, ringTo, 0.18f, flash, 2, 0f, 13, 0.3f);
            if (_golden) PixelRing.Spawn(_origin + Vector2.up * 0.1f, 2, ringTo - 4, 0.2f, Gold, 1, 0.06f, 13, 0.3f);

            // rachadura no ponto do impacto
            _crackHalf = Mathf.RoundToInt(8f + 14f * _strength);
            _crack = new GameObject("Rachadura").AddComponent<SpriteRenderer>();
            _crack.transform.SetParent(transform, false);
            _crack.transform.position = PixelFont.Snap(_origin);
            _crack.sprite = Crack(_crackHalf, 0);
            _crack.sortingOrder = 3;

            // pedrinhas no centro
            int pebbles = 4 + Mathf.RoundToInt(6f * _strength);
            for (int i = 0; i < pebbles; i++)
                HitSparks.One(_origin + new Vector2(Random.Range(-0.3f, 0.3f), 0.08f),
                              new Vector2(Random.Range(-3f, 3f), Random.Range(4f, 7f + 3f * _strength)),
                              i % 3 == 0 ? (Color)(_golden ? Gold : StoneLight) : StoneMid, Random.Range(0.35f, 0.55f), 30f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;

            // frente da onda: solta as placas conforme passa
            float front = _range * Mathf.Clamp01(_age / _duration);
            while (_nextSide <= front && _nextSide <= _range)
            {
                for (int s = -1; s <= 1; s += 2) PopSlab(_origin.x + s * _nextSide);
                _nextSide += SlabStep;
            }

            // placas: sobem 1 a 3 px e voltam em passos
            int peak = 1 + Mathf.RoundToInt(2f * _strength);
            for (int i = _slabs.Count - 1; i >= 0; i--)
            {
                var sl = _slabs[i];
                sl.Age += dt;
                float k = sl.Age / SlabTime;
                if (k >= 1f) { Destroy(sl.R.gameObject); _slabs.RemoveAt(i); continue; }
                int px = Mathf.RoundToInt(peak * Mathf.Sin(k * Mathf.PI));
                sl.R.transform.position = sl.Rest + new Vector3(0f, px / (float)PixelFont.Ppu, 0f);
                _slabs[i] = sl;
            }

            // rachadura: segura e some em pontilhado
            if (_crack != null)
            {
                if (_age > CrackHold) _crack.sprite = Crack(_crackHalf, _age > CrackHold + CrackFade * 0.5f ? 2 : 1);
                if (_age > CrackHold + CrackFade) { Destroy(_crack.gameObject); _crack = null; }
            }

            if (_crack == null && _slabs.Count == 0 && _nextSide > _range) Destroy(gameObject);
        }

        void OnDestroy() => Alive = Mathf.Max(0, Alive - 1);

        void PopSlab(float x)
        {
            // só onde há chão de verdade perto da altura do impacto (não flutua sobre o vão)
            var hit = Physics2D.Raycast(new Vector2(x, _origin.y + 0.6f), Vector2.down, 1.4f, s_groundMask);
            if (hit.collider == null) return;
            float top = hit.point.y;
            var r = new GameObject("Placa").AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false);
            var rest = PixelFont.Snap(new Vector3(x, top, 0f));
            r.transform.position = rest;
            r.sprite = SlabSprite(_golden);
            r.sortingOrder = 3;
            _slabs.Add(new Slab { R = r, Rest = rest });
            // lascas e pedrinhas saltando na frente da onda
            int bits = 1 + Mathf.RoundToInt(2f * _strength);
            for (int i = 0; i < bits; i++)
                HitSparks.One(new Vector2(x + Random.Range(-0.2f, 0.2f), top + 0.06f),
                              new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(3f, 5.5f) * (0.7f + 0.5f * _strength)),
                              i == 0 && _golden ? (Color)Gold : i % 2 == 0 ? StoneLight : StoneMid, Random.Range(0.25f, 0.4f), 30f);
        }

        /// <summary>Placa do chão 8×3: borda clara em cima, contorno escuro embaixo (dourada: borda dourada).</summary>
        static Sprite SlabSprite(bool golden)
        {
            if (golden ? s_slabGold != null : s_slab != null) return golden ? s_slabGold : s_slab;
            const int w = 8, h = 4;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || x == w - 1;
                    Color32 c = y == h - 1 ? (golden ? Gold : StoneLight) : y == 0 ? StoneDark : StoneMid;
                    if (edge && y > 0) c = StoneDark;
                    if (edge && y == h - 1) c = new Color32(0, 0, 0, 0); // cantos de cima arredondados
                    px[y * w + x] = c;
                }
            // pivô na linha de cima: a placa fica com o topo na superfície do chão
            var s = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 1f), golden ? "placa dourada" : "placa");
            if (golden) s_slabGold = s; else s_slab = s;
            return s;
        }

        /// <summary>
        /// Rachadura em zigue-zague de <paramref name="half"/> px para cada lado, 3 px de fundo, com galhos curtos.
        /// <paramref name="dither"/> 1 e 2 tiram pixels (some sem transparência). Determinística pelo tamanho.
        /// </summary>
        static Sprite Crack(int half, int dither)
        {
            int key = half * 4 + dither;
            if (s_cracks.TryGetValue(key, out var cached) && cached != null) return cached;
            int w = half * 2 + 1, h = 5;
            var px = new Color32[w * h];
            var rng = new System.Random(half * 7919);
            int cx = half;
            for (int side = -1; side <= 1; side += 2)
            {
                int depth = 3; // y do traço (4 = superfície)
                for (int i = 0; i <= half; i++)
                {
                    int x = cx + side * i;
                    if (x < 0 || x >= w) continue;
                    if (i > 0 && rng.Next(3) == 0) depth = Mathf.Clamp(depth + (rng.Next(2) == 0 ? -1 : 1), 1, 3);
                    if (i > half - 3 && rng.Next(2) == 0) continue; // a ponta se desfaz
                    Put(px, w, h, x, depth, CrackColor, dither);
                    if (i < half / 2) Put(px, w, h, x, depth - 1, CrackColor, dither);      // mais grossa perto do centro
                    if (i % 5 == 3) Put(px, w, h, x + side, depth - 1, CrackColor, dither); // galhinho
                }
            }
            for (int y = 1; y < h; y++) Put(px, w, h, cx, y, CrackColor, dither);        // o centro afunda
            var s = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 1f), "rachadura");
            s_cracks[key] = s;
            return s;
        }

        static void Put(Color32[] px, int w, int h, int x, int y, Color32 c, int dither)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            if (dither == 1 && ((x + y) & 1) == 1) return;
            if (dither == 2 && (x % 3 != 0)) return;
            px[y * w + x] = c;
        }
    }
}
