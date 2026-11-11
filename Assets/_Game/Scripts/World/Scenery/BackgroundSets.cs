using System;
using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Conjuntos de fundo por trecho da fase (plano de assets, 7.8): cada conjunto é um grupo de camadas com paralaxe
    /// (céu, longe, médio, perto). As zonas dizem qual conjunto vale onde; quando o gato passa para outra zona, o fundo
    /// novo aparece por cima do antigo em <see cref="fadeTime"/> s (crossfade) e o antigo é desligado.
    /// Enquanto o gato continua dentro da zona atual, nada troca (as zonas se sobrepõem nas bordas).
    /// Montado pelo editor a partir de FortressBackgrounds.
    /// </summary>
    public class BackgroundSets : MonoBehaviour
    {
        [Serializable]
        public class SetEntry
        {
            public string key;
            public GameObject root;
        }

        [Serializable]
        public class ZoneEntry
        {
            public string set;
            public Rect rect;
        }

        [SerializeField] SetEntry[] sets = new SetEntry[0];
        [SerializeField] ZoneEntry[] zones = new ZoneEntry[0];
        [SerializeField] string startSet = "";
        [SerializeField, Min(0f)] float fadeTime = 0.6f;

        /// <summary>Quanto a ordem do conjunto que entra sobe durante a troca (fica na frente do que sai, atrás da parede -10).</summary>
        const int FadeBoost = 41;

        class Cached
        {
            public SpriteRenderer[] renderers;
            public int[] orders;
        }

        readonly Dictionary<string, Cached> _cache = new Dictionary<string, Cached>();
        string _current, _fadingOut;
        float _fade = -1f;

        public string CurrentSet => _current;
        public bool IsFading => _fade >= 0f;

        public void Configure(SetEntry[] newSets, ZoneEntry[] newZones, string start, float newFadeTime)
        {
            sets = newSets; zones = newZones; startSet = start; fadeTime = newFadeTime;
        }

        void Start()
        {
            _current = string.IsNullOrEmpty(startSet) && sets.Length > 0 ? sets[0].key : startSet;
            Vector2 p;
            if (TryTarget(out p)) _current = BackgroundZoneRules.Pick(ToRules(), _current, p.x, p.y) ?? _current;
            foreach (var s in sets) if (s.root != null) s.root.SetActive(s.key == _current);
        }

        void LateUpdate()
        {
            if (_fade >= 0f) StepFade(Time.deltaTime);
            Vector2 p;
            if (!TryTarget(out p)) return;
            string next = BackgroundZoneRules.Pick(ToRules(), _current, p.x, p.y);
            if (next != null && next != _current) SwitchTo(next, fadeTime);
        }

        /// <summary>Troca de conjunto (0 s = na hora). Uma troca no meio de outra termina a anterior antes.</summary>
        public void SwitchTo(string key, float duration)
        {
            if (key == _current || Root(key) == null) return;
            if (_fade >= 0f) FinishFade();
            _fadingOut = _current;
            _current = key;
            var root = Root(key);
            root.SetActive(true);
            if (duration <= 0f || _fadingOut == null) { _fade = 0f; FinishFade(); return; }
            var c = Cache(key);
            for (int i = 0; i < c.renderers.Length; i++)
            {
                c.renderers[i].sortingOrder = c.orders[i] + FadeBoost;
                SetAlpha(c.renderers[i], 0f);
            }
            _fade = 0f;
            fadeDuration = duration;
        }

        float fadeDuration = 0.6f;

        void StepFade(float dt)
        {
            _fade += dt / Mathf.Max(0.01f, fadeDuration);
            if (_fade >= 1f) { FinishFade(); return; }
            var c = Cache(_current);
            foreach (var r in c.renderers) SetAlpha(r, _fade);
        }

        void FinishFade()
        {
            var c = Cache(_current);
            for (int i = 0; i < c.renderers.Length; i++)
            {
                c.renderers[i].sortingOrder = c.orders[i];
                SetAlpha(c.renderers[i], 1f);
            }
            var old = Root(_fadingOut);
            if (old != null && _fadingOut != _current) old.SetActive(false);
            _fadingOut = null;
            _fade = -1f;
        }

        /// <summary>
        /// Transparência da troca para uma camada: o conjunto que está entrando vai de 0 a 1; os outros ficam em 1.
        /// (O <see cref="SkyHeightBlend"/> multiplica a mistura dele por isto.)
        /// </summary>
        public float AlphaOf(GameObject layer)
        {
            if (_fade < 0f || layer == null) return 1f;
            var root = Root(_current);
            return root != null && layer.transform.IsChildOf(root.transform) ? Mathf.Clamp01(_fade) : 1f;
        }

        static void SetAlpha(SpriteRenderer r, float a)
        {
            var col = r.color; col.a = a; r.color = col;
        }

        Cached Cache(string key)
        {
            Cached c;
            if (_cache.TryGetValue(key, out c)) return c;
            var root = Root(key);
            c = new Cached { renderers = root != null ? root.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0] };
            c.orders = new int[c.renderers.Length];
            for (int i = 0; i < c.renderers.Length; i++) c.orders[i] = c.renderers[i].sortingOrder;
            _cache[key] = c;
            return c;
        }

        GameObject Root(string key)
        {
            if (key == null) return null;
            foreach (var s in sets) if (s.key == key) return s.root;
            return null;
        }

        List<BackgroundZoneRules.Zone> _rules;
        List<BackgroundZoneRules.Zone> ToRules()
        {
            if (_rules != null && _rules.Count == zones.Length) return _rules;
            _rules = new List<BackgroundZoneRules.Zone>();
            foreach (var z in zones) _rules.Add(new BackgroundZoneRules.Zone(z.set, z.rect.xMin, z.rect.yMin, z.rect.xMax, z.rect.yMax));
            return _rules;
        }

        static bool TryTarget(out Vector2 p)
        {
            var player = PlayerHealth.Active;
            if (player != null) { p = player.transform.position; return true; }
            var cam = Camera.main;
            if (cam != null) { p = cam.transform.position; return true; }
            p = Vector2.zero;
            return false;
        }
    }

    /// <summary>Regra pura da escolha do conjunto (testável sem cena): o atual vale enquanto contém o ponto.</summary>
    public static class BackgroundZoneRules
    {
        public struct Zone
        {
            public readonly string Set;
            public readonly float X0, Y0, X1, Y1;
            public Zone(string set, float x0, float y0, float x1, float y1) { Set = set; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
            public bool Contains(float x, float y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
        }

        /// <summary>O conjunto do ponto: o atual, se ainda contém o ponto; senão a primeira zona que contém; senão o atual.</summary>
        public static string Pick(IList<Zone> zones, string current, float x, float y)
        {
            if (current != null)
                foreach (var z in zones) if (z.Set == current && z.Contains(x, y)) return current;
            foreach (var z in zones) if (z.Contains(x, y)) return z.Set;
            return current;
        }
    }
}
