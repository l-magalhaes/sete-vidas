using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>Valores da escalada (spec: Docs/Design/escalada.md). Ajustáveis com o jogo rodando.</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Wall Climb Settings", fileName = "WallClimbSettings")]
    public class WallClimbSettings : ScriptableObject
    {
        [Header("Escalar")]
        [Tooltip("Velocidade subindo (tiles/s)")]
        [Min(0.5f)] public float ClimbUpSpeed = 4f;
        [Tooltip("Velocidade descendo (tiles/s)")]
        [Min(0.5f)] public float ClimbDownSpeed = 6f;

        [Header("Pular da parede")]
        [Tooltip("Altura do pulo para longe da parede (tiles)")]
        [Min(0f)] public float WallJumpHeight = 2.5f;
        [Tooltip("Velocidade horizontal do impulso (tiles/s)")]
        [Min(0f)] public float WallJumpSpeedX = 7f;
        [Tooltip("Tempo em que a direção do jogador não freia o impulso (s)")]
        [Range(0f, 0.5f)] public float WallJumpInputLock = 0.15f;
        [Tooltip("Espaço segurando a direção da parede (ou cima): pulo para cima que agarra de novo (tiles)")]
        [Min(0f)] public float WallHopHeight = 2.5f;
        [Tooltip("Depois de soltar a parede, tempo em que o Espaço ainda vale como pulo da parede (s)")]
        [Range(0f, 0.3f)] public float WallJumpCoyote = 0.12f;

        [Header("Soltar")]
        [Tooltip("Depois de soltar ou pular, tempo sem poder agarrar de novo (s)")]
        [Range(0f, 1f)] public float ReattachCooldown = 0.2f;
        [Tooltip("Segurar a direção oposta por este tempo solta a parede (antes disso, o Espaço ainda pula para aquele lado)")]
        [Range(0f, 0.4f)] public float ReleaseHoldTime = 0.12f;
    }
}
