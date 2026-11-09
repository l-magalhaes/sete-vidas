using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>
    /// Habilidade que só funciona com uma classe ativa (adagas, marreta; depois fogo, música, flores).
    /// O <see cref="ClassManager"/> liga a da classe ativa e desliga as outras. Habilidades de locomoção
    /// (escalada, agachar, pulo duplo, dash) NÃO são ClassAbility: valem em qualquer classe.
    /// Nova classe = um ClassDefinition + um componente destes. Sem editar o PlayerController.
    /// </summary>
    public abstract class ClassAbility : MonoBehaviour
    {
        [SerializeField] CharacterClass forClass;

        public CharacterClass ForClass => forClass;

        /// <summary>Chamado pelo ClassManager logo depois de ligar esta habilidade (troca para a classe dela).</summary>
        public virtual void OnClassActivated() { }
    }
}
