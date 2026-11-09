using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Bocal no chão que solta uma coluna de fogo em ciclo: desligado → aviso (fagulhas) → chama.
    /// Vários bocais com deslocamentos diferentes formam uma onda; plataformas por cima são o lugar de esperar.
    /// </summary>
    public class FlameJet : TrapBase
    {
        [SerializeField, Min(0.1f)] float offTime = 1.6f;
        [Tooltip("Fagulhas antes da chama (s): o aviso")]
        [SerializeField, Min(0f)] float warnTime = 0.6f;
        [SerializeField, Min(0.1f)] float onTime = 1.2f;
        [Tooltip("Desloca o ciclo (s), para formar ondas")]
        [SerializeField] float offset;
        [Tooltip("Altura da chama (tiles)")]
        [SerializeField] float flameHeight = 2.5f;
        [SerializeField] float flameWidth = 0.9f;

        [Header("Visual")]
        [SerializeField] SpriteRenderer flame;
        [SerializeField] SpriteRenderer sparks;

        public TrapPhase Phase { get; private set; }

        public void Configure(float newOffset, float height)
        {
            offset = newOffset;
            flameHeight = height;
        }

        protected override Rect DangerArea() =>
            new Rect((Vector2)transform.position - new Vector2(flameWidth * 0.5f, 0f), new Vector2(flameWidth, flameHeight));

        void FixedUpdate()
        {
            Phase = TrapRules.PhaseAt(Time.time, offset, offTime, warnTime, onTime);
            if (Phase == TrapPhase.On)
            {
                var r = DangerArea();
                HitPlayerIn(r.center, r.size);
            }
        }

        void LateUpdate()
        {
            if (flame != null)
            {
                flame.enabled = Phase == TrapPhase.On;
                if (flame.enabled)
                {
                    float flicker = 1f + 0.08f * Mathf.Sin(Time.time * 40f);
                    flame.transform.localScale = new Vector3(flameWidth * flicker, flameHeight / 2f, 1f); // sprite de 1 × 2 tiles
                    flame.transform.localPosition = new Vector3(0f, flameHeight * 0.5f, 0f);
                }
            }
            if (sparks != null) sparks.enabled = Phase == TrapPhase.Warning && ((int)(Time.time * 16f) & 1) == 0;
        }
    }
}
