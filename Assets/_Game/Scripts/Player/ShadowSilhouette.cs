using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Melhoria 1: dentro de uma sombra jogável (exposição abaixo de <see cref="ShadowExposure"/>), o gato vira uma
    /// silhueta quase preta (#141018) com só os olhos acesos (#f2a53a) e um contorno de 1 px (#3a3450).
    /// Vale para o corpo, a arma, a capa e a cauda. Entra e sai em <see cref="FadeTime"/>; os olhos piscam de vez em quando.
    /// É a resposta visual a "estou escondido?" e não muda a regra da furtividade.
    /// Fica no objeto do jogador e roda depois do visual (troca o material só enquanto a silhueta aparece).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ShadowSilhouette : MonoBehaviour
    {
        const string ShaderPath = "SeteVidas/SpriteSilhouette";
        static Material s_material;
        static bool s_loaded;
        static readonly int AmountId = Shader.PropertyToID("_Amount");
        static readonly int EyesOnId = Shader.PropertyToID("_EyesOn");
        static readonly int OutlineId = Shader.PropertyToID("_OutlineColor");

        [Tooltip("Abaixo desta exposição de luz o gato está na sombra (a mesma do limite de escondido)")]
        [SerializeField, Range(0f, 1f)] float shadowExposure = 0.35f;
        [Tooltip("Tempo para entrar e sair da silhueta (s)")]
        [SerializeField, Min(0.01f)] float fadeTime = 0.2f;
        [Tooltip("Intervalo entre piscadas dos olhos (s)")]
        [SerializeField] Vector2 blinkInterval = new Vector2(2.5f, 5f);
        [Tooltip("Duração da piscada (s)")]
        [SerializeField, Min(0f)] float blinkTime = 0.08f;

        // Cada parte do gato (corpo, arma, capa, cauda) ganha a própria cópia do material de silhueta, criada uma vez
        // e reaproveitada. (Um MaterialPropertyBlock não pode ser criado no inicializador de campo: o Unity recusa.)
        struct Entry { public SpriteRenderer Renderer; public Material Original; public Material Instance; }

        readonly List<Entry> _renderers = new List<Entry>();
        readonly Dictionary<SpriteRenderer, Material> _instances = new Dictionary<SpriteRenderer, Material>();
        StealthController _stealth;
        PlayerHealth _health;
        bool _applied;
        float _nextBlink, _blinkUntil;
        int _childCount = -1;

        public float ShadowExposure => shadowExposure;
        public float FadeTime => fadeTime;
        /// <summary>0 = cores normais, 1 = silhueta.</summary>
        public float Amount { get; private set; }
        /// <summary>A silhueta está aparecendo agora.</summary>
        public bool IsSilhouette => Amount > 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_material = null; s_loaded = false; } // Play sem recarregar o domínio

        static Material SilhouetteMaterial
        {
            get
            {
                if (!s_loaded)
                {
                    s_loaded = true;
                    var shader = Resources.Load<Shader>(ShaderPath);
                    if (shader != null) s_material = new Material(shader) { name = "Silhueta na sombra" };
                }
                return s_material;
            }
        }

        void Awake()
        {
            _stealth = GetComponent<StealthController>();
            _health = GetComponent<PlayerHealth>();
            _nextBlink = Random.Range(blinkInterval.x, blinkInterval.y);
        }

        /// <summary>Regra pura (EditMode): para onde a silhueta vai neste quadro.</summary>
        public static float Step(float amount, bool inShadow, float dt, float fadeTime) =>
            Mathf.MoveTowards(amount, inShadow ? 1f : 0f, fadeTime <= 0f ? 1f : dt / fadeTime);

        void LateUpdate()
        {
            bool inShadow = _stealth != null && _stealth.LightExposure < shadowExposure;
            // machucado (piscando) ou morto: as cores voltam, para o clarão de dano e a morte lerem
            if (_health != null && (_health.IsInvulnerable || _health.IsDead)) inShadow = false;
            Amount = Step(Amount, inShadow, Time.deltaTime, fadeTime);

            if (Amount <= 0f) { if (_applied) Restore(); return; }
            var mat = SilhouetteMaterial;
            if (mat == null) return;
            if (!_applied || _childCount != transform.childCount) Apply(mat);

            // piscar: os olhos somem por um instante
            _nextBlink -= Time.deltaTime;
            if (_nextBlink <= 0f)
            {
                _blinkUntil = Time.time + blinkTime;
                _nextBlink = Random.Range(blinkInterval.x, blinkInterval.y);
            }
            float eyes = Time.time < _blinkUntil ? 0f : 1f;

            foreach (var e in _renderers)
            {
                if (e.Renderer == null || e.Instance == null) continue;
                e.Instance.SetFloat(AmountId, Amount);
                e.Instance.SetFloat(EyesOnId, eyes);
            }
        }

        void Apply(Material mat)
        {
            if (_applied) Restore();
            _childCount = transform.childCount;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            {
                // o rastro do golpe e o clarão branco ficam como estão
                if (r.name == "Slash" || r.name == "Clarão") continue;
                if (!_instances.TryGetValue(r, out var instance) || instance == null)
                {
                    instance = new Material(mat) { name = mat.name + " (" + r.name + ")" };
                    // o corpo tem o contorno cheio; capa, cauda e arma, mais leve
                    float outline = r.GetComponent<PlayerVisual>() != null ? 1f : 0.6f;
                    instance.SetColor(OutlineId, new Color(0.227f, 0.204f, 0.314f, outline));
                    _instances[r] = instance;
                }
                _renderers.Add(new Entry { Renderer = r, Original = r.sharedMaterial, Instance = instance });
                r.sharedMaterial = instance;
            }
            _applied = true;
        }

        void Restore()
        {
            foreach (var e in _renderers)
                if (e.Renderer != null) e.Renderer.sharedMaterial = e.Original;
            _renderers.Clear();
            _applied = false;
        }

        void OnDisable() { if (_applied) Restore(); Amount = 0f; }

        void OnDestroy()
        {
            foreach (var m in _instances.Values) if (m != null) Destroy(m);
            _instances.Clear();
        }
    }
}
