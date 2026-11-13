using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Lâmina pendular (Floresta, desenho do usuário de 13/11/2026): pendurada no teto por uma haste, balança de um lado
    /// para o outro. A lâmina fere (1 hit, empurra) e, como os espinhos, aceita o quique da estocada para baixo.
    /// O transform é o pivô (no teto); a haste e a lâmina são o filho <see cref="arm"/>, que gira.
    /// </summary>
    [SelectionBase]
    public class SwingingBlade : TrapBase
    {
        [Tooltip("Comprimento da haste até o meio da lâmina (tiles)")]
        [SerializeField, Min(1f)] float length = 4f;
        [Tooltip("Ângulo máximo para cada lado (graus)")]
        [SerializeField, Range(10f, 80f)] float maxAngle = 45f;
        [Tooltip("Tempo de uma ida e volta (s)")]
        [SerializeField, Min(0.5f)] float period = 2.4f;
        [Tooltip("Começo do balanço (0..1 do período): lâminas vizinhas fora de compasso")]
        [SerializeField, Range(0f, 1f)] float phase;
        [SerializeField] Transform arm;
        [SerializeField] Vector2 bladeSize = new Vector2(1.6f, 0.6f);

        float _t;

        void Reset() => returnToSafeGround = false;

        public void Configure(float armLength, float angle, float seconds, float startPhase)
        {
            length = armLength;
            maxAngle = angle;
            period = seconds;
            phase = startPhase;
        }

        /// <summary>Ângulo da haste no tempo <paramref name="t"/> (graus; 0 = pendurada reta). Regra pura.</summary>
        public static float AngleAt(float maxAngle, float period, float t) =>
            maxAngle * Mathf.Sin((t / Mathf.Max(0.01f, period)) * Mathf.PI * 2f);

        Vector2 BladeCenter()
        {
            float a = AngleAt(maxAngle, period, _t + phase * period) * Mathf.Deg2Rad;
            return (Vector2)transform.position + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * length;
        }

        protected override System.Func<Rect> PogoArea => DangerArea;

        protected override Rect DangerArea()
        {
            Vector2 c = BladeCenter();
            return new Rect(c - bladeSize * 0.5f, bladeSize);
        }

        void FixedUpdate()
        {
            _t += Time.fixedDeltaTime;
            if (arm != null) arm.localRotation = Quaternion.Euler(0f, 0f, AngleAt(maxAngle, period, _t + phase * period));
            var r = DangerArea();
            HitPlayerIn(r.center, r.size * 0.85f);
        }
    }
}
