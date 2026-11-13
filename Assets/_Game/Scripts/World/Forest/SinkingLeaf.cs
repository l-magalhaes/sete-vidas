using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Folha larga: afunda devagar com o gato em cima (1 tile/s, até 3) e sobe quando ele sai.</summary>
    public class SinkingLeaf : WeightedPlatform
    {
        [SerializeField, Min(0.1f)] float sinkSpeed = 1f;
        [SerializeField, Min(0f)] float maxDepth = 3f;

        protected override float NextOffset(float offset, bool occupied, float dt) =>
            PlatformRules.SinkStep(offset, occupied, dt, sinkSpeed, maxDepth);
    }

}
