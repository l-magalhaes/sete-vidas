using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Galho que balança: afunda 0,25 tile com o peso e volta.</summary>
    public class SwayingBranch : WeightedPlatform
    {
        [SerializeField, Min(0f)] float dip = 0.25f;
        [SerializeField, Min(0.1f)] float speed = 2f;

        protected override float NextOffset(float offset, bool occupied, float dt) =>
            PlatformRules.SwayStep(offset, occupied, dt, dip, speed);
    }

}
