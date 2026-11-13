using System;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Vínculo com um companheiro (Fase 5: Golden). Perto dele, E abre a conversa de vínculo na interface de
    /// diálogo; ao terminar, recupera um coração, desbloqueia a classe, grava o save e mostra a dica da troca.
    /// Depois, cada E abre uma fala curta (em rodízio). Textos: Docs/Narrativa/dialogos-mvp.md.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CompanionBond : MonoBehaviour
    {
        [SerializeField] CharacterClass grantsClass = CharacterClass.Paladin;
        [Tooltip("Flag do save que marca o vínculo feito")]
        [SerializeField] string bondFlag = "vinculo_golden";

        [Header("Diálogos")]
        [Tooltip("Conversa de vínculo (D-GOLDEN-01)")]
        [SerializeField] DialogueDefinition bondDialogue;
        [Tooltip("Continuação da conversa de vínculo (o juramento do gato, D-GOLDEN-01b). Opcional")]
        [SerializeField] DialogueDefinition bondFollowUp;
        [Tooltip("Linha de sistema logo depois do vínculo (dica de troca de classe)")]
        [SerializeField] DialogueDefinition unlockDialogue;
        [Tooltip("Falas seguintes, uma por conversa, em rodízio (D-GOLDEN-02)")]
        [SerializeField] DialogueDefinition[] idleDialogues = new DialogueDefinition[0];

        [Header("Visual")]
        [Tooltip("\"E: falar\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;

        PlayerHealth _playerInRange;
        int _idleIndex;

        public bool IsBonded => SaveManager.Data.HasFlag(bondFlag);

        /// <summary>O vínculo terminou (classe e coração concedidos).</summary>
        public event Action Bonded;

        void Start() => SeteVidas.UI.WorldPrompt.Upgrade(prompt); // o botão do aparelho em uso no lugar de "E: falar"

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) _playerInRange = health;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null && health == _playerInRange) _playerInRange = null;
        }

        void Update()
        {
            var runner = DialogueRunner.Active;
            bool talking = runner != null && runner.IsOpen;
            bool near = _playerInRange != null && !_playerInRange.IsDead;
            if (prompt != null) prompt.gameObject.SetActive(near && !talking);
            if (!near || talking) return;

            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Interact(_playerInRange);
        }

        /// <summary>E perto do companheiro: conversa de vínculo (primeira vez) ou uma fala curta.</summary>
        public void Interact(PlayerHealth player)
        {
            if (player == null || player.IsDead) return;
            var runner = DialogueRunner.Active;
            var input = player.GetComponent<PlayerInputReader>();

            if (runner == null)
            {
                Debug.LogWarning("[Sete Vidas] Sem DialogueRunner na cena (HUD): o vínculo acontece sem conversa.", this);
                CompleteBond(player);
                return;
            }
            if (runner.IsOpen) return;

            if (!IsBonded)
            {
                StartBond(player, null);
                return;
            }

            if (idleDialogues.Length == 0) return;
            runner.Play(idleDialogues[_idleIndex % idleDialogues.Length], input);
            _idleIndex++;
        }

        /// <summary>
        /// Conversa de vínculo: ao terminar, concede coração e classe e mostra a dica de troca.
        /// <paramref name="onDone"/> roda depois da última linha (a cena do tomo usa para levantar o gato).
        /// </summary>
        public void StartBond(PlayerHealth player, Action onDone)
        {
            var runner = DialogueRunner.Active;
            var input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (runner == null || bondDialogue == null)
            {
                CompleteBond(player);
                onDone?.Invoke();
                return;
            }
            void Finish()
            {
                CompleteBond(player);
                if (unlockDialogue != null) runner.Play(unlockDialogue, input, onDone);
                else onDone?.Invoke();
            }

            runner.Play(bondDialogue, input, () =>
            {
                if (bondFollowUp != null) runner.Play(bondFollowUp, input, Finish);
                else Finish();
            });
        }

        /// <summary>Concede coração e classe e grava o save (também usado pelos testes).</summary>
        public void CompleteBond(PlayerHealth player)
        {
            if (IsBonded || player == null) return;
            var data = SaveManager.Data;
            data.SetFlag(bondFlag);
            var classes = player.GetComponent<ClassManager>();
            if (classes != null) classes.Unlock(grantsClass);
            else if (!data.IsUnlocked(grantsClass)) data.unlockedClasses.Add(grantsClass.ToString());
            player.AddHeart(); // +1 coração e grava o save

            Vector2 chest = player.transform.position + Vector3.up * 1.2f;
            HitSparks.Burst(chest, 10, new Color32(0xff, 0xc8, 0xd0, 0xff), new Color32(0xe0, 0x3c, 0x4a, 0xff), 2f, 4f, 0.4f, 4f, 0.5f);
            PixelRing.Spawn(chest, 3, 14, 0.35f, new Color32(0xff, 0x8a, 0x9a, 0xff));
            PixelPopup.Spawn(player.transform.position + Vector3.up * 2.2f, "+1", new Color32(0xff, 0x8a, 0x9a, 0xff), PixelIcons.Heart());
            Bonded?.Invoke();
        }
    }
}
