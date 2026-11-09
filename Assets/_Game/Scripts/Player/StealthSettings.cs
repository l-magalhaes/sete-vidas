using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>Visibilidade e ruídos do gato (spec: Docs/Design/furtividade-sentinela.md).</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Stealth Settings", fileName = "StealthSettings")]
    public class StealthSettings : ScriptableObject
    {
        [Header("Visibilidade")]
        [Tooltip("Multiplicador da visibilidade agachado")]
        [Range(0f, 1f)] public float CrouchVisibilityMultiplier = 0.6f;
        [Tooltip("Abaixo disto, o gato conta como escondido (só para depuração/feedback)")]
        [Range(0f, 1f)] public float HiddenThreshold = 0.35f;

        [Header("Passos (correndo em pé)")]
        [Tooltip("Faz barulho acima desta fração da velocidade máxima")]
        [Range(0f, 1f)] public float FootstepMinSpeedFraction = 0.6f;
        [Min(0.05f)] public float FootstepInterval = 0.35f;
        [Min(0f)] public float FootstepRadius = 3f;
        [Range(0f, 1f)] public float FootstepLoudness = 0.15f;

        [Header("Aterrissagem")]
        [Tooltip("Queda a partir desta altura faz barulho (tiles). Acima da altura do pulo, abaixo de plataforma − 1,25 (pendurar e soltar é silencioso)")]
        [Min(0f)] public float LandingNoiseHeight = 4.5f;
        [Min(0f)] public float LandingRadius = 5f;
        [Range(0f, 1f)] public float LandingLoudness = 0.4f;

        [Header("Golpe")]
        [Min(0f)] public float AttackNoiseRadius = 4f;
        [Range(0f, 1f)] public float AttackLoudness = 0.3f;
    }
}
