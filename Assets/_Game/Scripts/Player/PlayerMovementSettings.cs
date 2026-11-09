using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Todos os valores de movimento do gato (spec: Docs/Design/controlador-gato.md).
    /// O controlador lê estes campos a cada frame, então dá para ajustar com o jogo rodando.
    /// Unidades: 1 tile = 1 unidade Unity = 16 px.
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Player Movement Settings", fileName = "PlayerMovementSettings")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("Andar")]
        [Tooltip("Velocidade máxima andando (tiles/s)")]
        [Min(0f)] public float MoveSpeed = 7f;
        [Tooltip("Aceleração até a velocidade máxima (tiles/s²)")]
        [Min(0f)] public float Acceleration = 70f;
        [Tooltip("Desaceleração ao soltar a direção (tiles/s²)")]
        [Min(0f)] public float Deceleration = 90f;
        [Tooltip("Multiplicador de velocidade agachado")]
        [Range(0f, 1f)] public float CrouchSpeedMultiplier = 0.5f;

        [Header("Pulo")]
        [Tooltip("Altura máxima do pulo com o botão segurado (tiles)")]
        [Min(0.5f)] public float JumpHeight = 4f;
        [Tooltip("Tempo do chão até o topo do pulo (s)")]
        [Min(0.1f)] public float TimeToApex = 0.38f;
        [Tooltip("Multiplicador da gravidade enquanto cai")]
        [Min(1f)] public float FallGravityMultiplier = 1.8f;
        [Tooltip("Ao soltar o pulo durante a subida, a velocidade vertical é multiplicada por este valor")]
        [Range(0f, 1f)] public float JumpCutMultiplier = 0.5f;
        [Tooltip("Velocidade máxima de queda (tiles/s)")]
        [Min(1f)] public float MaxFallSpeed = 20f;
        [Tooltip("Tempo para ainda pular depois de sair de uma borda (s)")]
        [Range(0f, 0.3f)] public float CoyoteTime = 0.1f;
        [Tooltip("Tempo em que um pulo apertado antes de aterrissar é guardado (s)")]
        [Range(0f, 0.3f)] public float JumpBufferTime = 0.1f;

        [Header("Agarrar borda [PROPOSTA]")]
        [Tooltip("Liga/desliga agarrar bordas ao cair segurando a direção da parede")]
        public bool LedgeGrabEnabled = true;
        [Tooltip("Altura das patas acima dos pés ao se pendurar (tiles). 1,25 = 20 px")]
        [Min(0.1f)] public float LedgeHandHeight = 1.25f;
        [Tooltip("Tolerância vertical para pegar a quina (tiles). Maior = mais fácil de agarrar")]
        [Range(0.1f, 0.8f)] public float LedgeGrabWindow = 0.4f;
        [Tooltip("Duração da subida da borda (s)")]
        [Range(0.05f, 0.6f)] public float LedgeClimbDuration = 0.2f;
        [Tooltip("Depois de soltar a borda, tempo sem poder agarrar de novo (s)")]
        [Range(0f, 1f)] public float LedgeRegrabCooldown = 0.3f;

        [Header("Colisor (tamanhos da arte aprovada)")]
        [Tooltip("Largura do colisor (tiles). 0,75 = 12 px")]
        [Min(0.1f)] public float ColliderWidth = 0.75f;
        [Tooltip("Altura em pé (tiles). 1,375 = 22 px")]
        [Min(0.1f)] public float StandingHeight = 1.375f;
        [Tooltip("Altura agachado (tiles). Precisa ser menor que 1 para passar em passagens de 1 tile")]
        [Min(0.1f)] public float CrouchHeight = 0.875f;

        /// <summary>Gravidade da subida (tiles/s²), calculada a partir da altura e do tempo até o topo.</summary>
        public float Gravity => JumpMath.Gravity(JumpHeight, TimeToApex);

        /// <summary>Velocidade de saída do pulo para o passo de física informado.</summary>
        public float LaunchVelocity(float fixedDeltaTime) =>
            JumpMath.LaunchVelocity(JumpHeight, TimeToApex, fixedDeltaTime);
    }
}
