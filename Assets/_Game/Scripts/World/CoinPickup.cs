using SeteVidas.Combat;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Moeda no mundo: gira, flutua e vai para a bolsa ao encostar. Com <c>pickupId</c>, fica marcada no save e
    /// não volta mais (nem ao descansar, nem ao morrer). Sem id, é uma moeda solta (de inimigo, por exemplo).
    /// A moeda do Contratante usa o mesmo componente: vale 100 e só pode ser pega depois da cena do tomo.
    /// Spec: Docs/Design/moedas.md.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [Tooltip("Quantas moedas vale")]
        [SerializeField, Min(1)] int value = 1;
        [Tooltip("Id único na cena (salvo como \"moeda:<id>\"). Vazio = não fica marcada no save")]
        [SerializeField] string pickupId = "";
        [Tooltip("Pode ser pega desde o início (a do Contratante começa desligada até o fim da cena)")]
        [SerializeField] bool collectable = true;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Frames do giro")]
        [SerializeField] Sprite[] spin = new Sprite[0];
        [SerializeField, Min(0.02f)] float frameTime = 0.12f;
        [Tooltip("Flutua para chamar atenção (tiles)")]
        [SerializeField] float bob = 0.06f;

        [Header("Ao pegar")]
        [Tooltip("Conversa curta ao pegar (opcional, ex.: a moeda do Contratante)")]
        [SerializeField] DialogueDefinition pickupDialogue;

        Vector3 _rest;
        float _time;
        bool _collected;

        public int Value => value;
        public string PickupId => pickupId;
        public bool Collectable => collectable;
        public bool IsPersistent => !string.IsNullOrEmpty(pickupId);
        public bool AlreadyCollected => IsPersistent && SaveManager.Data.HasFlag(CoinPurse.FlagFor(pickupId));

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rest = transform.position;
            _time = Random.value * 2f; // moedas lado a lado não giram juntas
        }

        void Start()
        {
            if (AlreadyCollected) gameObject.SetActive(false);
        }

        /// <summary>Liga ou desliga a coleta (cena do tomo). Ao ligar, a moeda passa a flutuar de onde está.</summary>
        public void SetCollectable(bool on)
        {
            collectable = on;
            _rest = transform.position;
        }

        void Update()
        {
            _time += Time.deltaTime;
            if (spriteRenderer != null && spin != null && spin.Length > 0)
                spriteRenderer.sprite = spin[(int)(_time / frameTime) % spin.Length];
            if (collectable && bob > 0f)
                transform.position = _rest + Vector3.up * (bob * Mathf.Sin(_time * 3f));
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (_collected || !collectable) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || health.IsCollapsed) return;
            Collect(health);
        }

        /// <summary>Vai para a bolsa, marca no save e some. Também usado pelos testes.</summary>
        public void Collect(PlayerHealth by)
        {
            if (_collected) return;
            _collected = true;
            CoinPurse.Add(value);
            SeteVidas.Audio.Sfx.Play(value >= 100 ? "moeda100" : value >= 5 ? "moeda5" : "moeda");
            if (IsPersistent) SaveManager.Data.SetFlag(CoinPurse.FlagFor(pickupId));
            SaveManager.Save();
            DamagePopup.Spawn(transform.position + Vector3.up * 0.6f, value == 1 ? "+1 moeda" : $"+{value} moedas", new Color32(0xff, 0xd3, 0x4d, 0xff));

            var runner = DialogueRunner.Active;
            if (pickupDialogue != null && runner != null && !runner.IsOpen && by != null)
                runner.Play(pickupDialogue, by.GetComponent<PlayerInputReader>());

            gameObject.SetActive(false);
        }
    }
}
