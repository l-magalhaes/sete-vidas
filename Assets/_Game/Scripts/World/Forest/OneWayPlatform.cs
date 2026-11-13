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
            // galho fino demais é atravessado em queda rápida: engrossa para baixo, sem mudar o topo
            if (col.size.y < PlatformRules.MinOneWayThickness)
            {
                float top = col.offset.y + col.size.y * 0.5f;
                col.size = new Vector2(col.size.x, PlatformRules.MinOneWayThickness);
                col.offset = new Vector2(col.offset.x, top - PlatformRules.MinOneWayThickness * 0.5f);
            }
            var eff = GetComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 160f;
            eff.useSideFriction = false;
            eff.useSideBounce = false;
        }
    }

}
