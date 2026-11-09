using SeteVidas.Dialogue;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Placa, inscrição ou estátua que se lê com E: abre um texto curto de lore na interface de diálogo.
    /// Opcional: o jogador lê se quiser (a história principal nunca depende dela).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LoreSign : MonoBehaviour
    {
        [SerializeField] DialogueDefinition text;
        [Tooltip("\"E: ler\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;

        PlayerHealth _near;

        public DialogueDefinition Text => text;

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
            bool canRead = _near != null && !_near.IsDead && text != null && runner != null && !runner.IsOpen;
            if (prompt != null) prompt.gameObject.SetActive(canRead);
            if (!canRead) return;
            var input = _near.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) runner.Play(text, input);
        }
    }
}
