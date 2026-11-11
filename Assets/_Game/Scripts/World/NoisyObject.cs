using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Objeto do cenário que faz barulho quando o gato passa (spec: Docs/Design/objetos-barulhentos.md).
    /// O ruído vai para o <see cref="NoiseSystem"/>, que os inimigos já ouvem. Presets em <see cref="NoisyObjectKind"/>:
    /// cacos de vidro (sempre, até agachado: pule por cima), folhas secas e palha (só correndo),
    /// sininhos pendurados (de pé; agachado passa por baixo) e baldes/panelas (uma vez, correndo contra).
    /// Precisa de um Collider2D em modo trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NoisyObject : MonoBehaviour
    {
        [SerializeField] NoisyObjectKind kind = NoisyObjectKind.Glass;
        [Tooltip("Usar os valores do preset do tipo. Desligue para ajustar à mão")]
        [SerializeField] bool usePreset = true;
        [SerializeField] NoisyObjectRules.Config custom = NoisyObjectRules.Preset(NoisyObjectKind.Glass);
        [Tooltip("Quadros da reação (cacos tilintando, sininho balançando). Opcional")]
        [SerializeField] Sprite[] reactFrames;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] float reactFps = 12f;

        float _lastEmit = -999f;
        bool _spent;
        float _reactTime = -1f;
        Sprite _rest;

        public NoisyObjectRules.Config Config => usePreset ? NoisyObjectRules.Preset(kind) : custom;
        public bool Spent => _spent;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _rest = spriteRenderer.sprite;
        }

        void OnEnable() => GameEvents.WorldReset += ResetState;
        void OnDisable() => GameEvents.WorldReset -= ResetState;

        /// <summary>Volta ao estado inicial (o balde derrubado fica em pé de novo ao descansar no santuário).</summary>
        public void ResetState()
        {
            _spent = false;
            _reactTime = -1f;
            _lastEmit = -999f;
            if (spriteRenderer != null && _rest != null) spriteRenderer.sprite = _rest;
        }

        /// <summary>Montagem pelo editor (FortressSetup): tipo, desenho em repouso e quadros da reação.</summary>
        public void Setup(NoisyObjectKind newKind, SpriteRenderer renderer, Sprite[] frames, float fps)
        {
            kind = newKind;
            usePreset = true;
            spriteRenderer = renderer;
            reactFrames = frames;
            reactFps = fps;
            if (renderer != null) _rest = renderer.sprite;
        }

        /// <summary>Alcance diferente do preset (ex.: os sininhos de alarme da galeria chamam as sentinelas a 16 tiles).</summary>
        public void SetRadius(float radius)
        {
            custom = NoisyObjectRules.Preset(kind);
            custom.Radius = radius;
            usePreset = false;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.Settings == null) return;

            float speedFraction = player.Settings.MoveSpeed > 0f ? Mathf.Abs(player.Velocity.x) / player.Settings.MoveSpeed : 0f;
            var cfg = Config;
            if (!NoisyObjectRules.ShouldEmit(cfg, player.IsCrouching, player.IsGrounded, speedFraction,
                    Time.time - _lastEmit, _spent))
                return;

            _lastEmit = Time.time;
            if (cfg.OneShot) _spent = true;
            NoiseSystem.Emit(transform.position, cfg.Radius, cfg.Loudness, gameObject, NoisyObjectRules.Label(kind));
            _reactTime = 0f;
        }

        void Update()
        {
            if (_reactTime < 0f || spriteRenderer == null || reactFrames == null || reactFrames.Length == 0) return;
            _reactTime += Time.deltaTime;
            int frame = (int)(_reactTime * reactFps);
            if (frame >= reactFrames.Length)
            {
                _reactTime = -1f;
                if (!_spent) spriteRenderer.sprite = _rest; // balde derrubado fica no último quadro
                return;
            }
            spriteRenderer.sprite = reactFrames[frame];
        }
    }

    public enum NoisyObjectKind { Glass, DryLeaves, Chimes, Bucket }

    /// <summary>Regras dos objetos barulhentos (C# puro, testadas em EditMode).</summary>
    public static class NoisyObjectRules
    {
        [System.Serializable]
        public struct Config
        {
            [Tooltip("Até onde se ouve (tiles)")] public float Radius;
            [Tooltip("Quanto soma à suspeita (0 a 1)")] [Range(0f, 1f)] public float Loudness;
            [Tooltip("Agachado não faz barulho")] public bool QuietWhenCrouched;
            [Tooltip("Só faz barulho com o gato no chão (pisando)")] public bool NeedsGround;
            [Tooltip("Velocidade mínima, em fração da máxima (0 = até parado)")] [Range(0f, 1f)] public float MinSpeedFraction;
            [Tooltip("Tempo mínimo entre dois barulhos (s)")] public float Cooldown;
            [Tooltip("Faz barulho uma vez só (balde caído)")] public bool OneShot;
        }

        /// <summary>Valores iniciais [PROPOSTA] de cada tipo.</summary>
        public static Config Preset(NoisyObjectKind kind)
        {
            switch (kind)
            {
                // Vidro: até agachado e devagar. Só parado em cima não faz barulho. A saída é pular por cima.
                // Alcance 10 (era 5, pedido do usuário): os guardas da sala inteira ouvem.
                case NoisyObjectKind.Glass:
                    return new Config { Radius = 10f, Loudness = 0.7f, QuietWhenCrouched = false, NeedsGround = true, MinSpeedFraction = 0.05f, Cooldown = 0.5f };
                // Folhas secas e palha: só correndo. Andar agachado passa em silêncio.
                case NoisyObjectKind.DryLeaves:
                    return new Config { Radius = 3f, Loudness = 0.2f, QuietWhenCrouched = true, NeedsGround = true, MinSpeedFraction = 0.6f, Cooldown = 0.4f };
                // Sininhos pendurados na altura da cabeça: de pé tocam, agachado passa por baixo. Também no ar.
                // Alcance 11 (era 6, pedido do usuário); os de alarme da galeria continuam em 16.
                case NoisyObjectKind.Chimes:
                    return new Config { Radius = 11f, Loudness = 0.75f, QuietWhenCrouched = true, NeedsGround = false, MinSpeedFraction = 0f, Cooldown = 1.5f };
                // Balde ou panela: correndo contra, derruba uma vez.
                case NoisyObjectKind.Bucket:
                    return new Config { Radius = 4f, Loudness = 0.4f, QuietWhenCrouched = true, NeedsGround = true, MinSpeedFraction = 0.6f, Cooldown = 0f, OneShot = true };
                default:
                    return new Config { Radius = 3f, Loudness = 0.2f, NeedsGround = true, MinSpeedFraction = 0.6f, Cooldown = 0.5f };
            }
        }

        public static bool ShouldEmit(Config cfg, bool crouching, bool grounded, float speedFraction, float sinceLast, bool spent)
        {
            if (spent) return false;
            if (cfg.QuietWhenCrouched && crouching) return false;
            if (cfg.NeedsGround && !grounded) return false;
            if (speedFraction < cfg.MinSpeedFraction) return false;
            return sinceLast >= cfg.Cooldown;
        }

        public static string Label(NoisyObjectKind kind)
        {
            switch (kind)
            {
                case NoisyObjectKind.Glass: return "vidro";
                case NoisyObjectKind.DryLeaves: return "folhas";
                case NoisyObjectKind.Chimes: return "sininhos";
                case NoisyObjectKind.Bucket: return "balde";
                default: return "barulho";
            }
        }
    }
}
