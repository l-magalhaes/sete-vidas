using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>Valores da luta contra o Golden (spec: Docs/Design/chefe-golden.md). Ajustáveis com o jogo rodando.</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Golden Boss Settings", fileName = "GoldenBossSettings")]
    public class GoldenBossSettings : ScriptableObject
    {
        [Header("Geral")]
        [Min(1)] public int MaxHealth = 30;
        [Tooltip("Abaixo desta fração da vida, fica mais rápido e agressivo")]
        [Range(0f, 1f)] public float SecondPhaseAt = 0.5f;
        [Tooltip("Pausa entre golpes (s); na 2ª fase, ×0,6")]
        [Min(0f)] public float IdleBetween = 0.7f;
        [Min(0f)] public float WalkSpeed = 2.5f;
        [Tooltip("Andando, re-escolhe o golpe depois deste tempo (s)")]
        [Min(0.2f)] public float WalkTime = 1.2f;
        public Vector2 BodySize = new Vector2(1.4f, 2f);

        [Header("Golpe de marreta (perto)")]
        [Min(0f)] public float SwingRange = 1.8f;
        [Min(0f)] public float SwingWindup = 0.6f;
        [Min(0.02f)] public float SwingActive = 0.15f;
        [Min(0f)] public float SwingRecovery = 0.7f;
        public Vector2 SwingHitbox = new Vector2(1.8f, 1.4f);

        [Header("Salto com marretada (passar por baixo)")]
        [Tooltip("Agachado antes de saltar (s): o aviso")]
        [Min(0f)] public float LeapWindup = 0.5f;
        [Tooltip("Tempo no ar (s)")]
        [Min(0.2f)] public float LeapAirTime = 0.9f;
        [Tooltip("Altura do salto (tiles): alto o bastante para o gato passar por baixo")]
        [Min(1f)] public float LeapHeight = 5f;
        [Tooltip("Metade da largura da área do impacto (tiles)")]
        [Min(0.2f)] public float LeapImpactHalfWidth = 1.6f;
        [Tooltip("Parado depois de cair (s): janela para bater nele")]
        [Min(0f)] public float LeapRecovery = 0.8f;

        [Header("Cura concentrada (interromper batendo)")]
        [Tooltip("Só cura com a vida abaixo desta fração")]
        [Range(0f, 1f)] public float HealBelow = 0.7f;
        [Min(0.5f)] public float HealDuration = 4f;
        [Min(0f)] public float HealPerSecond = 2f;
        [Tooltip("Dano levado durante a cura que a interrompe")]
        [Min(1)] public int HealInterruptDamage = 5;
        [Tooltip("Atordoado depois de interrompido (s)")]
        [Min(0f)] public float StaggerTime = 1.5f;
        [Min(0f)] public float HealCooldown = 15f;

        [Header("Chuva de relâmpagos (procurar o lugar seguro)")]
        [Tooltip("Marreta erguida antes da 1ª onda (s)")]
        [Min(0f)] public float LightningCastTime = 0.8f;
        [Min(1)] public int LightningWaves = 3;
        [Min(1)] public int StrikesPerWave = 4;
        [Tooltip("Tempo entre a marca no chão e o raio (s)")]
        [Min(0.3f)] public float LightningTelegraph = 1.2f;
        [Min(0.05f)] public float LightningStrikeTime = 0.2f;
        [Min(0f)] public float LightningWaveGap = 0.4f;
        [Tooltip("Largura de cada raio (tiles)")]
        [Min(0.5f)] public float LightningWidth = 1.4f;
        [Tooltip("Faixa sem raios garantida em cada onda (tiles)")]
        [Min(0.5f)] public float LightningSafeGap = 2.5f;
        [Min(0f)] public float LightningCooldown = 9f;

        [Header("Dano no gato (hits)")]
        [Min(0)] public int Damage = 1;
    }
}
