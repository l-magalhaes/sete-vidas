using System.Collections.Generic;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
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

        /// <summary>Marca do save: a placa foi lida (o Diário do inventário lista as lidas, melhoria 10).</summary>
        public const string ReadPrefix = "lido_";
        static readonly List<LoreSign> s_all = new List<LoreSign>();
        /// <summary>As placas da cena (o Diário acha o texto de cada uma pelo id).</summary>
        public static IReadOnlyList<LoreSign> All => s_all;
        /// <summary>Título para o Diário: o nome do objeto (ex.: "Placa do pátio").</summary>
        public string Title => name;
        public string Key => text != null ? text.ScriptId : "";
        public bool IsRead => SaveManager.Data != null && SaveManager.Data.HasFlag(ReadPrefix + Key);

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

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
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame)
            {
                runner.Play(text, input);
                SeteVidas.Audio.Sfx.Play("placa");
                if (SaveManager.Data != null) SaveManager.Data.SetFlag(ReadPrefix + Key); // para o Diário
            }
        }
    }
}
