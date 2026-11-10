using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>Valores da Sentinela (spec: Docs/Design/furtividade-sentinela.md). Ajustáveis com o jogo rodando.</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Sentinel Settings", fileName = "SentinelSettings")]
    public class SentinelSettings : ScriptableObject
    {
        [Header("Vida")]
        [Min(1)] public int MaxHealth = 5;

        [Header("Movimento (tiles/s)")]
        [Min(0f)] public float PatrolSpeed = 2f;
        [Min(0f)] public float InvestigateSpeed = 3f;
        [Tooltip("Menor que a velocidade do gato (7): dá para fugir")]
        [Min(0f)] public float ChaseSpeed = 4.5f;
        [Tooltip("Pausa em cada ponta da patrulha (s)")]
        [Min(0f)] public float PatrolPause = 1.5f;
        [Tooltip("Procurando: tempo olhando para cada lado (s)")]
        [Min(0.1f)] public float SearchLookInterval = 1f;
        [Min(0f)] public float GravityScale = 4f;

        [Header("Visão")]
        [Min(0f)] public float VisionRange = 8f;
        [Range(5f, 90f)] public float VisionHalfAngle = 35f;
        [Tooltip("Altura dos olhos a partir dos pés (tiles)")]
        [Min(0f)] public float EyeHeight = 1.3f;
        [Tooltip("Suspeita por segundo vendo o gato (×0,5 na borda do alcance, ×2 colado)")]
        [Min(0f)] public float SightFillRate = 0.8f;
        [Tooltip("Em combate, a visibilidade do gato nunca fica abaixo disto")]
        [Range(0f, 1f)] public float CombatVisibilityFloor = 0.6f;
        [Tooltip("Multiplica o raio dos ruídos que ela ouve")]
        [Min(0f)] public float HearingMultiplier = 1f;

        [Header("Suspeita")]
        [Range(0f, 1f)] public float SuspiciousThreshold = 0.25f;
        [Range(0f, 1f)] public float InvestigateThreshold = 0.5f;
        [Range(0f, 1f)] public float CalmThreshold = 0.1f;
        [Min(0f)] public float DrainDelay = 1f;
        [Min(0f)] public float DrainRate = 0.15f;
        [Min(0f)] public float SuspiciousTime = 1.2f;
        [Min(0f)] public float LoseSightTime = 2f;
        [Min(0f)] public float SearchTime = 3f;

        [Header("Ataque")]
        [Tooltip("Distância horizontal para começar o ataque (tiles)")]
        [Min(0f)] public float AttackRange = 1.3f;
        [Tooltip("Preparação visível antes do golpe (s): o jogador precisa poder reagir")]
        [Min(0f)] public float AttackWindup = 0.45f;
        [Min(0.02f)] public float AttackActive = 0.12f;
        [Tooltip("Recuperação depois do golpe (s): janela de contra-ataque")]
        [Min(0f)] public float AttackRecovery = 0.6f;
        [Tooltip("Hits tirados do gato por golpe")]
        [Min(0)] public int AttackDamage = 1;
        public Vector2 AttackHitboxSize = new Vector2(1.2f, 0.9f);
        [Tooltip("Distância do centro da hitbox à frente da Sentinela")]
        public float AttackHitboxForward = 0.6f;

        [Header("Atirador (besta)")]
        [Tooltip("Cão Sentinela Atirador: mira e atira virotes em vez de atacar com a lança")]
        public bool Ranged;
        [Tooltip("Não sai do posto (no alto): investiga só olhando, nunca desce para perseguir")]
        public bool HoldPosition;
        [Tooltip("Distância máxima do tiro (tiles)")]
        [Min(0f)] public float ShootRange = 11f;
        [Tooltip("Mirando antes do disparo (s): a linha de mira avisa o jogador")]
        [Min(0.1f)] public float AimTime = 0.9f;
        [Tooltip("Nos últimos segundos da mira ela trava (dá para desviar do lugar marcado)")]
        [Min(0f)] public float AimLockTime = 0.25f;
        [Tooltip("Recarregando a besta depois do tiro (s): janela para se aproximar")]
        [Min(0f)] public float ReloadTime = 1.4f;
        [Min(1f)] public float BoltSpeed = 13f;
        [Min(0)] public int BoltDamage = 1;
        [Tooltip("Quanto ela consegue mirar para baixo (graus abaixo da horizontal)")]
        [Range(0f, 89f)] public float MaxAimDown = 70f;
        [Tooltip("Quanto ela consegue mirar para cima (graus)")]
        [Range(0f, 89f)] public float MaxAimUp = 20f;
        [Tooltip("O cone de visão aponta este tanto para baixo (graus): quem fica no alto olha para o chão")]
        [Range(0f, 80f)] public float VisionTiltDown = 0f;

        [Header("Morte")]
        [Min(0f)] public float DeathDuration = 0.6f;
        [Tooltip("Chance de soltar meio coração ao morrer (0,15 = 15%)")]
        [Range(0f, 1f)] public float HeartDropChance = 0.15f;

        [Header("Corpo (tiles)")]
        public Vector2 BodySize = new Vector2(0.75f, 1.6f);

        public AlertTuning Alert => new AlertTuning
        {
            SuspiciousThreshold = SuspiciousThreshold, InvestigateThreshold = InvestigateThreshold,
            CalmThreshold = CalmThreshold, DrainDelay = DrainDelay, DrainRate = DrainRate,
            SuspiciousTime = SuspiciousTime, LoseSightTime = LoseSightTime, SearchTime = SearchTime
        };
    }
}
