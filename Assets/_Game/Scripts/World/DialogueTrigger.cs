using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.World
{
    /// <summary>
    /// Área que abre uma conversa quando o gato entra (narração de chegada, fim da demonstração).
    /// Pode tocar uma vez só (flag no save), exigir uma flag antes e trocar de cena ao terminar.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] DialogueDefinition dialogue;
        [Tooltip("Toca só uma vez: esta flag vai para o save (vazio = toda vez que entrar)")]
        [SerializeField] string onceFlag = "";
        [Tooltip("Só toca se esta flag existir no save (vazio = sempre)")]
        [SerializeField] string requiresFlag = "";
        [Tooltip("Cena aberta ao terminar a conversa (vazio = continua jogando)")]
        [SerializeField] string loadSceneAfter = "";

        bool _playing;

        /// <summary>Tem uma conversa ligada (o setup ligou certo).</summary>
        public bool HasDialogue => dialogue != null;

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) Play(player);
        }

        /// <summary>Toca a conversa se as condições permitirem (também usado pelos testes).</summary>
        public bool Play(PlayerHealth player)
        {
            if (_playing || player == null || player.IsDead || dialogue == null) return false;
            var data = SaveManager.Data;
            if (!string.IsNullOrEmpty(onceFlag) && data.HasFlag(onceFlag)) return false;
            if (!string.IsNullOrEmpty(requiresFlag) && !data.HasFlag(requiresFlag)) return false;
            var runner = DialogueRunner.Active;
            if (runner == null || runner.IsOpen) return false;

            _playing = true;
            if (!string.IsNullOrEmpty(onceFlag))
            {
                data.SetFlag(onceFlag);
                SaveManager.Save();
            }
            return runner.Play(dialogue, player.GetComponent<PlayerInputReader>(), () =>
            {
                _playing = false;
                if (!string.IsNullOrEmpty(loadSceneAfter)) SceneManager.LoadScene(loadSceneAfter);
            });
        }
    }
}
