using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>
    /// Base das habilidades de locomoção (escalada agora; pulo duplo e dash depois).
    /// O PlayerController encontra todas no mesmo objeto. A cada passo de física livre ele pergunta
    /// <see cref="TryBegin"/>; a primeira que aceitar assume o controle até <see cref="Step"/> retornar false.
    /// Assim, uma habilidade nova é só um componente a mais, sem editar o PlayerController.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public abstract class PlayerAbility : MonoBehaviour
    {
        protected PlayerController Player { get; private set; }

        /// <summary>Enquanto ativa, o gato não vira com a entrada horizontal.</summary>
        public virtual bool LocksFacing => true;

        protected virtual void Awake() => Player = GetComponent<PlayerController>();

        /// <summary>Quer assumir o controle neste passo? Chamado só quando nenhuma outra está ativa.</summary>
        public abstract bool TryBegin(bool grounded);

        /// <summary>Um passo de física com o controle. Retorne false para devolvê-lo ao controlador.</summary>
        public abstract bool Step(float dt);

        /// <summary>O controle foi tirado à força (dano, morte, troca de cena).</summary>
        public virtual void OnInterrupted() { }
    }
}
