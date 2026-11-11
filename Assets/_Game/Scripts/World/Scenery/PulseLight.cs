using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Pulso lento de brilho (plano de assets, 6.2): runas douradas e veias roxas pulsam em 2–3 s (alfa 60↔100%), fora
    /// de tempo umas das outras, "como se brigassem". Com <see cref="failing"/>, de vez em quando apaga por um instante
    /// (as runas falhando perto do tomo).
    /// </summary>
    public class PulseLight : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] float period = 2.5f;
        [Tooltip("Defasagem do ciclo (0..1)")]
        [SerializeField] float phase;
        [SerializeField, Range(0f, 1f)] float minAlpha = 0.6f;
        [SerializeField, Range(0f, 1f)] float maxAlpha = 1f;
        [Tooltip("Falhando: apaga por ~0,15 s de vez em quando")]
        [SerializeField] bool failing;

        SpriteRenderer[] _renderers;

        public void Configure(float newPeriod, float newPhase, float min, float max, bool isFailing)
        {
            period = newPeriod; phase = newPhase; minAlpha = min; maxAlpha = max; failing = isFailing;
        }

        /// <summary>Alfa no tempo t (s): seno suave entre o mínimo e o máximo.</summary>
        public static float Value(float t, float period, float phase, float min, float max)
        {
            float s = 0.5f + 0.5f * Mathf.Sin((t / Mathf.Max(0.01f, period) + phase) * Mathf.PI * 2f);
            return Mathf.Lerp(min, max, s);
        }

        /// <summary>Falha (apagão breve) num ciclo de 3,7 s deslocado pela fase: ~4% do tempo.</summary>
        public static bool FailsAt(float t, float phase) => Mathf.Repeat(t + phase * 3.7f, 3.7f) < 0.15f;

        void LateUpdate()
        {
            if (_renderers == null) _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            float t = Time.time;
            float a = failing && FailsAt(t, phase) ? minAlpha * 0.35f : Value(t, period, phase, minAlpha, maxAlpha);
            foreach (var r in _renderers) { var c = r.color; c.a = a; r.color = c; }
        }
    }
}
