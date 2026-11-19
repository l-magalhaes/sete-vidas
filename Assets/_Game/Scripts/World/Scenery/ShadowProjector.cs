using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A projeção da sombra (pedido do usuário, 19/11/2026: "um sistema de sombra dinâmico, onde a sombra reflete o asset
    /// que ela está projetando"). Regra pura, testada no EditMode: a silhueta de quem faz a sombra (a máscara, em pixels,
    /// linha 0 embaixo) é arrastada na direção da luz (a lua no alto à esquerda: para baixo e para a direita) até o chão.
    /// O próprio objeto não é coberto pela sombra dele. A saída tem 4 níveis: 0 fora, 1 borda, 2 meio, 3 miolo (a borda
    /// mais clara, sem pontilhado), e manchas de luar entre as folhas na metade de cima.
    /// </summary>
    public static class ShadowCast
    {
        /// <summary>Quantos pixels para a direita a sombra anda a cada pixel que desce (a lua alta, à esquerda).</summary>
        public const float DefaultSlope = 0.3f;

        /// <summary>
        /// A sombra de <paramref name="mask"/> (<paramref name="mw"/> × <paramref name="mh"/>) caindo até
        /// <paramref name="drop"/> pixels abaixo do pé da máscara. A tela da sombra começa <paramref name="drop"/> pixels
        /// abaixo da máscara (y 0 = o chão) e tem a largura da máscara mais o quanto a luz empurra para o lado.
        /// </summary>
        public static byte[] Sweep(bool[] mask, int mw, int mh, int drop, float slope, bool dapples, int seed, out int w, out int h)
        {
            drop = Math.Max(0, drop);
            int push = (int)Math.Ceiling(Math.Abs(slope) * (mh + drop));
            w = mw + push;
            h = mh + drop;
            var shade = new bool[w * h];
            // cada pixel olha para a lua (para cima e para a esquerda): se a silhueta estiver no caminho, está na sombra.
            // Exato e sem furos (o caminho contrário, raios saindo da silhueta, deixava listras entre um raio e outro).
            // O próprio objeto não fica embaixo da sombra dele.
            for (int sy = 0; sy < h; sy++)
                for (int sx = 0; sx < w; sx++)
                {
                    int my = sy - drop;
                    if (IsMask(mask, mw, mh, sx, my)) continue;
                    for (int k = 1; my + k < mh; k++)
                    {
                        int mx = sx - (int)Math.Round(slope * k);
                        if (mx < 0) break;
                        if (my + k >= 0 && IsMask(mask, mw, mh, mx, my + k)) { shade[sy * w + sx] = true; break; }
                    }
                }
            // os níveis pela distância até a borda (o chão, embaixo, não é borda: a sombra encosta nele)
            var levels = new byte[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!shade[y * w + x]) continue;
                    int dist = 5;
                    for (int oy = -4; oy <= 4 && dist > 0; oy++)
                        for (int ox = -4; ox <= 4; ox++)
                        {
                            int nx = x + ox, ny = y + oy;
                            if (ny < 0) continue;
                            bool outside = nx < 0 || nx >= w || ny >= h || !shade[ny * w + nx];
                            if (outside) dist = Math.Min(dist, Math.Max(Math.Abs(ox), Math.Abs(oy)));
                        }
                    levels[y * w + x] = (byte)(dist <= 2 ? 1 : dist <= 4 ? 2 : 3);
                }
            // manchas de luar passando entre as folhas (só na metade de cima, onde a copa está perto)
            if (dapples)
            {
                var r = new System.Random(seed);
                int n = Math.Max(1, w / 24);
                for (int i = 0; i < n; i++)
                {
                    double mx = 8 + r.NextDouble() * Math.Max(1, w - 16), my = h * (0.55 + r.NextDouble() * 0.35);
                    double rx = 3 + r.NextDouble() * 3, ry = 2 + r.NextDouble() * 1.5;
                    for (int y = (int)(my - ry); y <= my + ry; y++)
                        for (int x = (int)(mx - rx); x <= mx + rx; x++)
                            if (x >= 0 && y >= 0 && x < w && y < h && levels[y * w + x] == 3 && Sq((x - mx) / rx) + Sq((y - my) / ry) <= 1)
                                levels[y * w + x] = 2;
                }
            }
            return levels;
        }

        static bool IsMask(bool[] mask, int mw, int mh, int x, int y) { return x >= 0 && y >= 0 && x < mw && y < mh && mask[y * mw + x]; }
        static double Sq(double v) { return v * v; }

        /// <summary>Opacidade de cada nível (0 fora, 1 borda, 2 meio, 3 miolo), a mesma escada da sombra antiga.</summary>
        public static readonly byte[] LevelAlpha = { 0, 95, 175, 255 };
    }

    /// <summary>
    /// A sombra projetada de um objeto do cenário (a árvore de uma zona de furtividade, um teto, uma saliência): no Start lê
    /// a silhueta de quem faz a sombra (os pixels do sprite dele, ou as células de um teto) e desenha a sombra com a mesma
    /// forma, arrastada para baixo e para a direita até o chão (<see cref="ShadowCast"/>). Mover a árvore na cena move a
    /// sombra junto. A zona de jogo (<see cref="LightZone"/>) continua sendo o retângulo; isto é só o desenho.
    /// </summary>
    [ExecuteAlways]
    public class ShadowProjector : MonoBehaviour
    {
        const string ShadeName = "Sombra (projetada)";

        [Tooltip("Quem faz a sombra (a árvore). Vazio = as células de teto abaixo")]
        [SerializeField] SpriteRenderer caster;
        [Tooltip("Teto (sem árvore): o retângulo do teto em tiles do mundo (x, y = canto de baixo à esquerda)")]
        [SerializeField] RectInt roofCells;
        [Tooltip("Onde a sombra termina embaixo (o chão, y do mundo)")]
        [SerializeField] float floorY;
        [SerializeField] Color tint = new Color(0.04f, 0.03f, 0.1f, 0.42f);
        [SerializeField] int sortingOrder = 8;
        [SerializeField] float slope = ShadowCast.DefaultSlope;

        SpriteRenderer _shade;
        static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_cache.Clear(); }   // Play sem recarregar o domínio

        public SpriteRenderer Shade => _shade;

        public void ConfigureTree(SpriteRenderer tree, float floor, Color color, int order)
        {
            caster = tree; floorY = floor; tint = color; sortingOrder = order;
        }

        public void ConfigureRoof(RectInt cells, float floor, Color color, int order)
        {
            caster = null; roofCells = cells; floorY = floor; tint = color; sortingOrder = order;
        }

        Vector3 _lastCaster;
        bool _lastFlip;

        // também no editor: mover a árvore na cena (ajuste à mão) leva a sombra junto
        void OnEnable() => Build();

        void Update()
        {
            if (Application.isPlaying || caster == null) return;
            if (caster.transform.position != _lastCaster || caster.flipX != _lastFlip) Build();
        }

        /// <summary>Monta (ou remonta) o desenho da sombra a partir de quem a faz.</summary>
        public void Build()
        {
            const int ppu = 16;
            bool[] mask; int mw, mh; Vector2 origin; string key;
            if (caster != null && caster.sprite != null)
            {
                if (!ReadMask(caster, out mask, out mw, out mh)) return;
                origin = caster.bounds.min;
                key = caster.sprite.name + (caster.flipX ? "_f" : "");
            }
            else
            {
                if (roofCells.width <= 0 || roofCells.height <= 0) return;
                mw = roofCells.width * ppu; mh = roofCells.height * ppu;
                mask = new bool[mw * mh];
                for (int i = 0; i < mask.Length; i++) mask[i] = true;
                origin = new Vector2(roofCells.x, roofCells.y);
                key = "teto_" + roofCells.width + "x" + roofCells.height;
            }
            // o pé da silhueta e o chão, alinhados ao pixel
            origin = new Vector2(Mathf.Round(origin.x * ppu) / ppu, Mathf.Round(origin.y * ppu) / ppu);
            int drop = Mathf.Max(0, Mathf.RoundToInt((origin.y - floorY) * ppu));
            key += "_d" + drop + "_s" + Mathf.RoundToInt(slope * 100);
            if (!s_cache.TryGetValue(key, out var sprite) || sprite == null)
            {
                var levels = ShadowCast.Sweep(mask, mw, mh, drop, slope, caster != null, key.GetHashCode(), out int w, out int h);
                var px = new Color32[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, ShadowCast.LevelAlpha[levels[i]]);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "sombra_" + key, hideFlags = HideFlags.DontSave };
                tex.SetPixels32(px);
                tex.Apply(false);
                sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, ppu);   // pivô no canto de baixo à esquerda
                sprite.hideFlags = HideFlags.DontSave;
                s_cache[key] = sprite;
            }
            if (_shade == null)
            {
                var found = transform.Find(ShadeName);
                if (found != null) _shade = found.GetComponent<SpriteRenderer>();
            }
            if (_shade == null)
            {
                // feita na hora, nunca salva na cena (a cena guarda só quem faz a sombra)
                var go = new GameObject(ShadeName) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _shade = go.AddComponent<SpriteRenderer>();
            }
            if (caster != null) { _lastCaster = caster.transform.position; _lastFlip = caster.flipX; }
            _shade.sprite = sprite;
            _shade.color = tint;
            _shade.sortingOrder = sortingOrder;
            _shade.transform.position = new Vector3(origin.x, origin.y - drop / (float)ppu, 0f);
        }

        /// <summary>A silhueta do sprite (alfa ≥ 50%), virada como ele está desenhado. Falso se a textura não for legível.</summary>
        static bool ReadMask(SpriteRenderer sr, out bool[] mask, out int w, out int h)
        {
            var s = sr.sprite;
            var r = s.textureRect;
            w = (int)r.width; h = (int)r.height;
            mask = null;
            if (!s.texture.isReadable)
            {
                Debug.LogWarning($"[Sete Vidas] Sombra: a textura {s.texture.name} não é legível (Read/Write). Gere as cenas de novo.");
                return false;
            }
            var px = s.texture.GetPixels32();
            int tw = s.texture.width;
            mask = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = sr.flipX ? w - 1 - x : x;
                    mask[y * w + x] = px[((int)r.y + y) * tw + (int)r.x + sx].a >= 128;
                }
            return true;
        }
    }
}
