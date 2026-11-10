using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Contas das rampas (spec: Docs/Design/controlador-gato.md, "Rampas"). C# puro para os testes EditMode.
    /// Normal = vetor unitário perpendicular à superfície, apontando para fora (para cima no chão).
    /// </summary>
    public static class SlopeMath
    {
        /// <summary>Abaixo disto (graus) a superfície conta como plana.</summary>
        public const float FlatTolerance = 0.5f;

        /// <summary>Inclinação da superfície em graus (0 = chão plano, 90 = parede).</summary>
        public static float AngleOf(Vector2 normal) => Vector2.Angle(normal, Vector2.up);

        /// <summary>O gato consegue ficar de pé e andar nesta superfície?</summary>
        public static bool IsWalkable(Vector2 normal, float maxAngle) =>
            normal.y > 0f && AngleOf(normal) <= maxAngle + 0.01f;

        /// <summary>A superfície é inclinada (não é chão plano)?</summary>
        public static bool IsSlope(Vector2 normal) => AngleOf(normal) > FlatTolerance;

        /// <summary>
        /// Velocidade vertical para andar <paramref name="vx"/> tiles/s na horizontal colado na rampa.
        /// A velocidade horizontal é a mesma do chão plano: o gato não fica mais lento subindo.
        /// </summary>
        public static float VerticalSpeedFor(Vector2 normal, float vx) =>
            normal.y <= 0.0001f ? 0f : -vx * normal.x / normal.y;

        /// <summary>
        /// Distância vertical entre o meio do fundo da caixa e a rampa quando a quina de cima encosta nela.
        /// O sprite desce isso para os pés tocarem a rampa (a caixa tem fundo reto).
        /// </summary>
        public static float CenterGap(float halfWidth, Vector2 normal) =>
            normal.y <= 0.0001f ? 0f : halfWidth * Mathf.Abs(normal.x) / normal.y;

        /// <summary>Normal de uma rampa que sobe <paramref name="rise"/> tiles a cada <paramref name="run"/> para a direita (negativo = para a esquerda).</summary>
        public static Vector2 NormalFor(float rise, float run) =>
            new Vector2(-rise * Mathf.Sign(run), Mathf.Abs(run)).normalized;
    }
}
