using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Escada que se pega por cima (18/11/2026, pedido do usuário: "mas como que eu desço?"): em pé em cima dela, ou
    /// do lado dela no estrado, segurar baixo agarra a escada (do lado <see cref="Side"/>) e o gato desce como num cipó
    /// (<see cref="SeteVidas.Abilities.WallClimbAbility"/>). Fica no objeto da escada (camada Climbable).
    /// </summary>
    public class LadderTop : MonoBehaviour
    {
        [Tooltip("De que lado da escada o gato desce: -1 = à esquerda dela (o poço), 1 = à direita")]
        [SerializeField] int side = -1;

        public int Side => side >= 0 ? 1 : -1;

        public void Configure(int s) => side = s;

        /// <summary>Onde o gato fica agarrado (x dos pés) para uma escada com estes limites e meia largura do gato.</summary>
        public static float GrabX(Bounds b, int side, float halfWidth) => side < 0 ? b.min.x - halfWidth - 0.01f : b.max.x + halfWidth + 0.01f;
    }
}
