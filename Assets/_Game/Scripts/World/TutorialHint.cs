using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Que tecla a dica mostra (o texto vem do controle atual: teclado ou gamepad).</summary>
    public enum HintKey { None, Jump, Crouch, Attack, Up, Move, Interact, NextClass, Heal }

    /// <summary>Quando a dica some e fica marcada como vista.</summary>
    public enum HintDismiss
    {
        /// <summary>Ao fazer a ação da tecla (pular, agachar, atacar, escalar). Só aí fica vista; até lá, volta.</summary>
        Action,
        /// <summary>Ao se esconder agachado atrás de um esconderijo.</summary>
        Cover,
        /// <summary>Sem ação: some ao sair da área ou pelo tempo, e fica vista.</summary>
        Read,
    }

    /// <summary>
    /// Melhoria 9: área de uma dica do tutorial. Quando o gato entra, a dica aparece no pergaminho do canto da tela
    /// (<see cref="SeteVidas.UI.TutorialScroll"/>). Substitui os textos que flutuavam no mundo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TutorialHint : MonoBehaviour
    {
        [Tooltip("Id da dica: cada uma aparece uma vez por save (flag dica_<id>). Duas áreas com o mesmo id são a mesma dica")]
        [SerializeField] string hintId = "";
        [SerializeField] HintKey key;
        [Tooltip("Frase curta (até 6 palavras)")]
        [SerializeField] string phrase = "";
        [SerializeField] HintDismiss dismiss;
        [Tooltip("Só aparece com esta flag no save (ex.: vinculo_golden, as dicas do Paladino). Vazio = sempre")]
        [SerializeField] string requiresFlag = "";

        public string Id => hintId;
        public HintKey Key => key;
        public string Phrase => phrase;
        public HintDismiss Dismiss => dismiss;
        public string RequiresFlag => requiresFlag;

        public void Configure(string id, HintKey k, string text, HintDismiss d, string requires = "")
        {
            hintId = id; key = k; phrase = text; dismiss = d; requiresFlag = requires ?? "";
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            SeteVidas.UI.TutorialScroll.Get().Enter(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (SeteVidas.UI.TutorialScroll.Exists) SeteVidas.UI.TutorialScroll.Get().Exit(this);
        }
    }

    /// <summary>Regras puras das dicas (EditMode): fila de uma por vez e "uma vez por save".</summary>
    public sealed class TutorialQueue
    {
        public const float ShowTime = 6f;
        readonly List<string> _queue = new List<string>();

        public string Current { get; private set; }
        public float Shown { get; private set; }
        public int Pending => _queue.Count;

        /// <summary>Entrou na área de <paramref name="id"/>. Ignora a dica já vista neste save ou já na fila.</summary>
        public void Enter(string id, bool alreadySeen)
        {
            if (string.IsNullOrEmpty(id) || alreadySeen || id == Current || _queue.Contains(id)) return;
            _queue.Add(id);
            if (Current == null) Next();
        }

        /// <summary>Saiu da área: a dica sai da tela (e da fila, se ainda não tinha aparecido).</summary>
        public void Exit(string id)
        {
            _queue.Remove(id);
            if (id == Current) Next();
        }

        /// <summary>Fez a ação (ou leu): a dica some. Retorna o id que deve ficar marcado como visto.</summary>
        public string Done()
        {
            string done = Current;
            Next();
            return done;
        }

        /// <summary>Avança o tempo na tela. Retorna true quando passou de <see cref="ShowTime"/> (a dica deve sair).</summary>
        public bool Tick(float dt)
        {
            if (Current == null) return false;
            Shown += dt;
            return Shown >= ShowTime;
        }

        void Next()
        {
            Shown = 0f;
            if (_queue.Count == 0) { Current = null; return; }
            Current = _queue[0];
            _queue.RemoveAt(0);
        }
    }
}
