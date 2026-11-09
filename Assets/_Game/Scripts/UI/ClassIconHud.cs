using SeteVidas.Classes;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>Ícone da classe ativa ao lado dos corações. Lê o <see cref="ClassManager.Active"/>.</summary>
    public class ClassIconHud : MonoBehaviour
    {
        [SerializeField] Image icon;

        void LateUpdate()
        {
            var classes = ClassManager.Active;
            var def = classes != null ? classes.Current : null;
            if (icon == null) return;
            icon.enabled = def != null && def.Icon != null;
            if (icon.enabled && icon.sprite != def.Icon) icon.sprite = def.Icon;
        }
    }
}
