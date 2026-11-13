using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Do que é feito o obstáculo quebrável: muda a flag do save, as lascas e o som.</summary>
    public enum BreakableMaterial { Stone, Wood }

    /// <summary>Regras puras do obstáculo quebrável (EditMode).</summary>
    public static class BreakableRules
    {
        public const string StonePrefix = "parede_quebrada_";
        public const string WoodPrefix = "tronco_quebrado_";

        /// <summary>Flag do save. A pedra mantém o prefixo antigo (saves da Fortaleza continuam valendo).</summary>
        public static string Flag(BreakableMaterial material, string id) =>
            (material == BreakableMaterial.Wood ? WoodPrefix : StonePrefix) + id;

        /// <summary>Barulho da quebra: o tronco da Floresta é mais alto (Docs/Design/floresta-mecanicas.md §2).</summary>
        public static void BreakNoise(BreakableMaterial material, out float radius, out float loudness)
        {
            if (material == BreakableMaterial.Wood) { radius = 14f; loudness = 1f; }
            else { radius = 8f; loudness = 0.6f; }
        }
    }

    /// <summary>
    /// Parede rachada (gate do Paladino, layer Breakable): só aceita dano <see cref="DamageType.Heavy"/> (marreta).
    /// Adagas fazem "tink" e não tiram vida. Ao quebrar, faz um barulho alto, grava uma flag no save na hora
    /// e continua quebrada para sempre (inclusive depois de fechar o jogo).
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreakableWall : MonoBehaviour, IDamageable
    {
        [Tooltip("Identificador único. Vira a flag \"parede_quebrada_<id>\" no save")]
        [SerializeField] string wallId = "parede";
        [SerializeField, Min(1)] int maxHealth = 8;
        [SerializeField] DamageType requiredType = DamageType.Heavy;
        [Tooltip("Pedra (parede da Fortaleza) ou madeira (tronco rachado da Floresta): muda a flag, as lascas e o som")]
        [SerializeField] BreakableMaterial material = BreakableMaterial.Stone;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite intactSprite;
        [Tooltip("Mostrado com metade da vida ou menos")]
        [SerializeField] Sprite crackedSprite;
        [Tooltip("Entulho que aparece no chão depois de quebrada")]
        [SerializeField] GameObject rubble;

        [Header("Barulho ao quebrar")]
        [SerializeField] float breakNoiseRadius = 8f;
        [SerializeField, Range(0f, 1f)] float breakNoiseLoudness = 0.6f;

        BoxCollider2D _solid;
        float _shakeUntil;
        Vector3 _spriteRest;

        public int Health { get; private set; }
        public bool IsBroken { get; private set; }
        public string Flag => BreakableRules.Flag(material, wallId);
        public BreakableMaterial Material => material;
        bool IsWood => material == BreakableMaterial.Wood;

        void Awake()
        {
            _solid = GetComponent<BoxCollider2D>();
            Health = maxHealth;
            if (spriteRenderer != null) _spriteRest = spriteRenderer.transform.localPosition;
        }

        void Start()
        {
            if (SaveManager.Data.HasFlag(Flag)) SetBroken();
            else if (rubble != null) rubble.SetActive(false);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsBroken) return;
            _shakeUntil = Time.time + 0.12f;

            if (info.Type != requiredType)
            {
                // a lâmina resvala: faíscas de aço cinza no ponto do golpe (sem texto)
                Vector2 tinkAt = (Vector2)transform.position + Vector2.up * (_solid.size.y * 0.5f) - info.Direction.normalized * (_solid.size.x * 0.5f);
                HitSparks.Burst(tinkAt, 5, new Color32(0xe8, 0xec, 0xf2, 0xff), new Color32(0xb8, 0xbf, 0xcc, 0xff), 2f, 5f, 0.18f, 25f, 0.2f);
                SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position);
                return; // adagas não arranham a parede
            }

            Health = Mathf.Max(0, Health - info.Amount);
            // lascas de pedra saltando da parede (sem número de dano)
            Vector2 chipAt = (Vector2)transform.position + Vector2.up * (_solid.size.y * 0.5f) - info.Direction.normalized * (_solid.size.x * 0.5f);
            if (IsWood) // lascas de madeira clara e casca escura
                HitSparks.Burst(chipAt, 10, new Color32(0xc4, 0x98, 0x60, 0xff), new Color32(0x52, 0x26, 0x1e, 0xff), 2f, 6f, 0.45f, 30f, 0.5f);
            else
                HitSparks.Burst(chipAt, 8, new Color32(0x9a, 0x8c, 0x7c, 0xff), new Color32(0x5e, 0x52, 0x4a, 0xff), 2f, 6f, 0.4f, 30f, 0.5f);
            if (spriteRenderer != null && crackedSprite != null && Health <= maxHealth / 2) spriteRenderer.sprite = crackedSprite;
            // madeira: por enquanto os sons da parede, mais graves (sons próprios em Docs/Audio/floresta-audio.md)
            if (Health > 0) SeteVidas.Audio.Sfx.Play("parede_racha", transform.position, 1f, IsWood ? 0.75f : 1f);
            if (Health > 0 && IsWood) NoiseSystem.Emit((Vector2)transform.position + Vector2.up, 10f, 0.6f, gameObject, "tronco");
            if (Health <= 0) Break();
        }

        void Break()
        {
            SetBroken();
            SeteVidas.Audio.Sfx.Play("parede_quebra", transform.position, 1f, IsWood ? 0.7f : 1f);
            NoiseSystem.Emit((Vector2)transform.position + Vector2.up, breakNoiseRadius, breakNoiseLoudness, gameObject, IsWood ? "tronco" : "parede");
            var data = SaveManager.Data;
            data.SetFlag(Flag);
            SaveManager.Save(); // atalho aberto é para sempre: grava agora, sem esperar o santuário
        }

        void SetBroken()
        {
            IsBroken = true;
            Health = 0;
            _solid.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (rubble != null) rubble.SetActive(true);
        }

        void Update()
        {
            if (spriteRenderer == null || IsBroken) return;
            bool shaking = Time.time < _shakeUntil;
            spriteRenderer.transform.localPosition = _spriteRest + (shaking ? new Vector3(((int)(Time.time * 60f) & 1) == 0 ? 0.06f : -0.06f, 0f) : Vector3.zero);
        }
    }
}
