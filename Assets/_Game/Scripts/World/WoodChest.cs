using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Baú da Floresta (Docs/Design/floresta-mecanicas.md §3): abre com Interagir (E) e guarda uma Madeira de Reconstrução
    /// (ou moedas). Aberto fica aberto para sempre: grava "bau_&lt;id&gt;" (e "madeira_&lt;id&gt;") e salva na hora.
    /// Ranger da tampa = barulho baixo (conta no ninho da Isyathy).
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    [RequireComponent(typeof(BoxCollider2D))]
    public class WoodChest : MonoBehaviour
    {
        [Tooltip("Identificador único no jogo inteiro (W01..W12). Vai para o save: não mude depois de lançado")]
        [SerializeField] string chestId = "W01";
        [Tooltip("Guarda uma Madeira de Reconstrução. Desligado = só moedas")]
        [SerializeField] bool containsWood = true;
        [SerializeField, Min(0)] int coins;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [Tooltip("\"E: abrir\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;

        [Header("Primeira madeira do jogo (D-MADEIRA-01)")]
        [SerializeField] DialogueDefinition firstWoodDialogue;

        [Header("Barulho da tampa")]
        [SerializeField] float noiseRadius = 3f;
        [SerializeField, Range(0f, 1f)] float noiseLoudness = 0.2f;

        PlayerHealth _near;

        public string Id => chestId;
        public bool ContainsWood => containsWood;
        public bool IsOpen => SaveManager.Data != null && SaveManager.Data.HasFlag(ReconstructionWood.ChestFlag(chestId));

        void Start()
        {
            SeteVidas.UI.WorldPrompt.Upgrade(prompt);
            Refresh();
        }

        void Refresh()
        {
            if (spriteRenderer != null) spriteRenderer.sprite = IsOpen && openSprite != null ? openSprite : closedSprite;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null) _near = h;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null && h == _near) _near = null;
        }

        void Update()
        {
            var runner = DialogueRunner.Active;
            bool canOpen = _near != null && !_near.IsDead && !IsOpen && (runner == null || !runner.IsOpen);
            if (prompt != null) prompt.gameObject.SetActive(canOpen);
            if (!canOpen) return;
            var input = _near.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Open(input);
        }

        /// <summary>Abre o baú (também usado pelos testes de cena).</summary>
        public void Open(PlayerInputReader input = null)
        {
            var data = SaveManager.Data;
            if (data == null || IsOpen) return;
            bool firstWood = containsWood && ReconstructionWood.Count(data) == 0;

            data.SetFlag(ReconstructionWood.ChestFlag(chestId));
            if (containsWood) data.SetFlag(ReconstructionWood.WoodFlag(chestId));
            if (coins > 0) CoinPurse.Add(coins);
            SaveManager.Save(); // como a parede quebrada: o que foi pego é para sempre

            Refresh();
            SeteVidas.Audio.Sfx.Play("moeda", transform.position); // provisório até o "bau_abre" (Docs/Audio/floresta-audio.md)
            NoiseSystem.Emit((Vector2)transform.position + Vector2.up * 0.5f, noiseRadius, noiseLoudness, gameObject, "bau");

            Vector2 top = (Vector2)transform.position + Vector2.up * 1.2f;
            var wood = new Color32(0xc4, 0x98, 0x60, 0xff);
            for (int i = 0; i < 6; i++)
                HitSparks.Rise(top + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.4f, 0.1f)), wood, Random.Range(1f, 2f), Random.Range(0.4f, 0.7f));
            if (containsWood)
                PixelPopup.Spawn(transform.position + Vector3.up * 2f, ReconstructionWood.PickupText(ReconstructionWood.Count(data)), wood);

            if (firstWood && firstWoodDialogue != null && DialogueRunner.Active != null)
                DialogueRunner.Active.Play(firstWoodDialogue, input);
        }
    }
}
