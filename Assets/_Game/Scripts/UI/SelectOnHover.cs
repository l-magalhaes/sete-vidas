using UnityEngine;
using UnityEngine.EventSystems;

namespace SeteVidas.UI
{
    /// <summary>Passar o mouse por cima seleciona o botão (a seta da tela inicial acompanha o mouse também).</summary>
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            var s = GetComponent<UnityEngine.UI.Selectable>();
            if (s != null && s.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
