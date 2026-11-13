using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Galho de mão única (Docs/Level/floresta-mapa.md §3): o gato atravessa pulando por baixo e desce com baixo + pulo.
    /// Fica na layer Platform; a física de passar por baixo é do <see cref="PlatformEffector2D"/>, e o "chão" é do
    /// <see cref="GroundCheck"/> (só conta com os pés em cima).
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    [RequireComponent(typeof(BoxCollider2D), typeof(PlatformEffector2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        void Reset() => Configure();
        void Awake() => Configure();

        /// <summary>Layer, efetor de mão única e colisor usado pelo efetor (também chamado pelo setup do editor).</summary>
        public void Configure()
        {
            int layer = LayerMask.NameToLayer(GameConstants.Layers.Platform);
            if (layer >= 0) gameObject.layer = layer;
            var col = GetComponent<BoxCollider2D>();
            col.usedByEffector = true;
            var eff = GetComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 160f;
            eff.useSideFriction = false;
            eff.useSideBounce = false;
        }
    }

}
