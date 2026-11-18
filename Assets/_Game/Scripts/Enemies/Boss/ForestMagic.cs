using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// A magia da floresta (verde clara) da Vulpis: as cores, a flecha forte e os pontinhos de luz. Os pontinhos andam
    /// no tempo real, para continuarem vivos com o jogo pausado (a cena da flecha forte pausa a luta).
    /// O coração do Caçador no HUD brilha nas mesmas cores (<see cref="SeteVidas.UI.HeartsHud"/>).
    /// </summary>
    public class ForestMagic : MonoBehaviour
    {
        public static readonly Color32 Core = new Color32(0xe2, 0xff, 0xd8, 0xff);
        public static readonly Color32 Mid = new Color32(0x8f, 0xf2, 0x8a, 0xff);
        public static readonly Color32 Deep = new Color32(0x3c, 0xbf, 0x5a, 0xff);
        /// <summary>A casca da praga (roxa) que a flecha quebra.</summary>
        public static readonly Color32 Shell = new Color32(0xb0, 0x5c, 0xe8, 0xff);
        public static readonly Color32 ShellDark = new Color32(0x4a, 0x1d, 0x5c, 0xff);

        struct Mote { public SpriteRenderer Sr; public Vector2 Vel; public float Age, Life, Gravity; public Color Color; }

        const int Capacity = 160;
        static ForestMagic s_instance;
        static Sprite s_dot, s_big, s_arrow, s_shard;
        readonly List<Mote> _motes = new List<Mote>();
        readonly Stack<SpriteRenderer> _free = new Stack<SpriteRenderer>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_instance = null; s_dot = s_big = s_arrow = s_shard = null; }   // Play sem recarregar o domínio

        static ForestMagic Instance
        {
            get
            {
                if (s_instance == null) s_instance = new GameObject("Magia da floresta").AddComponent<ForestMagic>();
                return s_instance;
            }
        }

        /// <summary>Um pontinho de luz (tempo real). <paramref name="big"/>: 2 × 2 px.</summary>
        public static void One(Vector2 at, Vector2 vel, Color color, float life, bool big = false, float gravity = 0f, int order = 24)
        {
            var m = Instance;
            if (m._motes.Count >= Capacity) return;
            SpriteRenderer sr;
            if (m._free.Count > 0) sr = m._free.Pop();
            else
            {
                sr = new GameObject("luz").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(m.transform, false);
            }
            sr.gameObject.SetActive(true);
            sr.sprite = big ? Big() : Dot();
            sr.sortingOrder = order;
            sr.color = color;
            sr.transform.position = at;
            m._motes.Add(new Mote { Sr = sr, Vel = vel, Life = Mathf.Max(0.05f, life), Gravity = gravity, Color = color });
        }

        /// <summary>Uma explosão de luz: <paramref name="count"/> pontinhos para todos os lados.</summary>
        public static void Burst(Vector2 at, int count, float speedMin, float speedMax, float life, int order = 24)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f, s = Random.Range(speedMin, speedMax);
                Color c = i % 3 == 0 ? (Color)Core : i % 3 == 1 ? (Color)Mid : (Color)Deep;
                One(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, c, life * Random.Range(0.6f, 1.2f), i % 2 == 0, 0f, order);
            }
        }

        /// <summary>Os cacos roxos da casca da praga voando (caem com gravidade).</summary>
        public static void Shards(Vector2 at, int count, int order = 24)
        {
            var m = Instance;
            for (int i = 0; i < count && m._motes.Count < Capacity; i++)
            {
                float a = Random.Range(0.1f, Mathf.PI - 0.1f) + (Random.value < 0.3f ? Mathf.PI : 0f), s = Random.Range(4f, 11f);
                One(at + Random.insideUnitCircle * 0.8f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, i % 2 == 0 ? (Color)Shell : (Color)ShellDark, Random.Range(0.5f, 1f), false, 22f, order);
                m._motes[m._motes.Count - 1].Sr.sprite = Shard();
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                var m = _motes[i];
                m.Age += dt;
                if (m.Age >= m.Life || m.Sr == null)
                {
                    if (m.Sr != null) { m.Sr.gameObject.SetActive(false); _free.Push(m.Sr); }
                    _motes.RemoveAt(i);
                    continue;
                }
                m.Vel.y -= m.Gravity * dt;
                m.Vel *= 1f - Mathf.Min(1f, dt * 1.5f);
                m.Sr.transform.position += (Vector3)(m.Vel * dt);
                var c = m.Color;
                float k = m.Age / m.Life;
                c.a = k < 0.6f ? 1f : (((int)(m.Age * 24f) & 1) == 0 ? 1f - k : 0f);   // pisca ao apagar
                m.Sr.color = c;
                _motes[i] = m;
            }
        }

        // ------------------------------------------------------------ sprites

        static Sprite Make(string name, int w, int h, Color32[] px, Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = name, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16f);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        static Sprite Dot()
        {
            if (s_dot == null) s_dot = Make("magia_ponto", 1, 1, new[] { new Color32(255, 255, 255, 255) }, new Vector2(0.5f, 0.5f));
            return s_dot;
        }

        static Sprite Big()
        {
            if (s_big == null) { var px = new Color32[4]; for (int i = 0; i < 4; i++) px[i] = new Color32(255, 255, 255, 255); s_big = Make("magia_ponto2", 2, 2, px, new Vector2(0.5f, 0.5f)); }
            return s_big;
        }

        static Sprite Shard()
        {
            if (s_shard == null)
            {
                var px = new Color32[9];
                var w = new Color32(255, 255, 255, 255);
                px[0] = w; px[1] = w; px[3] = w; px[4] = w; px[5] = w; px[7] = w;   // caco torto de 3 × 3
                s_shard = Make("casca_caco", 3, 3, px, new Vector2(0.5f, 0.5f));
            }
            return s_shard;
        }

        /// <summary>
        /// A flecha forte da Vulpis (30 × 9, apontando para a direita, o pivô na ponta): a haste de luz verde, a ponta
        /// em folha brilhando e as penas de folha; um halo verde escuro em volta.
        /// </summary>
        public static Sprite Arrow()
        {
            if (s_arrow != null) return s_arrow;
            const int w = 30, h = 9;
            var px = new Color32[w * h];
            Color32 core = Core, mid = Mid, deep = Deep;
            Color32 halo = new Color32(Deep.r, Deep.g, Deep.b, 110);
            void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c; }
            const int cy = 4;
            // halo em volta da haste
            for (int x = 4; x < 24; x++) { Set(x, cy - 2, halo); Set(x, cy + 2, halo); }
            // a haste: miolo claro, bordas verdes
            for (int x = 3; x < 24; x++) { Set(x, cy, core); Set(x, cy - 1, x % 3 == 0 ? deep : mid); Set(x, cy + 1, x % 3 == 1 ? deep : mid); }
            // a ponta em folha (mais larga no meio, fina na ponta)
            int[] half = { 0, 1, 2, 3, 3, 2, 1 };
            for (int i = 0; i < half.Length; i++)
                for (int j = -half[i]; j <= half[i]; j++)
                    Set(23 + i, cy + j, Mathf.Abs(j) == half[i] && half[i] > 0 ? mid : core);
            Set(w - 1, cy, core);
            // as penas: duas folhas abertas para trás
            for (int i = 0; i < 5; i++) { Set(i, cy - 1 - (4 - i) / 2, i % 2 == 0 ? mid : deep); Set(i, cy + 1 + (4 - i) / 2, i % 2 == 0 ? mid : deep); }
            s_arrow = Make("flecha_forte_vulpis", w, h, px, new Vector2(1f, 0.5f));
            return s_arrow;
        }
    }
}
