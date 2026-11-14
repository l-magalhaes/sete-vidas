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

        /// <summary>Lado para onde a rampa desce (1 = direita, -1 = esquerda, 0 = plano).</summary>
        /// <summary>
        /// Fração da velocidade de andar subindo a rampa: 1 no plano ou descendo; <paramref name="at45"/> numa rampa de 45°,
        /// proporcional à inclinação no meio (a de 1:2 fica no meio do caminho).
        /// </summary>
        public static float UphillFactor(Vector2 normal, float moveX, float at45)
        {
            int down = DownhillDir(normal);
            if (down == 0 || Mathf.Abs(moveX) < 0.01f || Mathf.Sign(moveX) == down) return 1f;
            float steep = Mathf.Clamp01(Mathf.Abs(normal.x) / 0.70710678f);   // seno da inclinação ÷ seno de 45°
            return Mathf.Lerp(1f, at45, steep);
        }

        public static int DownhillDir(Vector2 normal) =>
            !IsSlope(normal) ? 0 : normal.x > 0f ? 1 : -1;

        /// <summary>
        /// Escorrega nesta rampa? Sim, a menos que o jogador empurre rampa acima (aí ele sobe andando).
        /// Empurrar rampa abaixo, ficar parado ou agachado: escorrega.
        /// </summary>
        public static bool WantsSlide(Vector2 normal, float inputX)
        {
            int down = DownhillDir(normal);
            if (down == 0) return false;
            return !(inputX * down < -0.1f);
        }

        /// <summary>
        /// Velocidade horizontal escorregando: acelera rampa abaixo pela parte da gravidade (× seno da inclinação),
        /// até <paramref name="max"/>; empurrar rampa abaixo acelera 30% mais.
        /// </summary>
        public static float SlideStep(float vx, Vector2 normal, float inputX, float dt, float accel, float max)
        {
            int down = DownhillDir(normal);
            if (down == 0) return vx;
            float a = accel * Mathf.Sin(AngleOf(normal) * Mathf.Deg2Rad) * (inputX * down > 0.1f ? 1.3f : 1f);
            return Mathf.MoveTowards(vx, down * max, a * dt);
        }

        /// <summary>
        /// Velocidade horizontal fora da rampa com o embalo do escorregão: acima da velocidade de andar, no mesmo
        /// sentido, cai só <paramref name="decel"/>; abaixo disso (ou virou), vale o andar normal (<paramref name="walk"/>).
        /// </summary>
        public static float Momentum(float vx, float target, float decel, float dt, float walk)
        {
            bool sameSide = target == 0f || Mathf.Sign(target) == Mathf.Sign(vx);
            if (sameSide && Mathf.Abs(vx) > Mathf.Max(Mathf.Abs(target), 0.01f))
                return Mathf.MoveTowards(vx, target == 0f ? 0f : target, decel * dt);
            return walk;
        }

        /// <summary>Normal de uma rampa que sobe <paramref name="rise"/> tiles a cada <paramref name="run"/> para a direita (negativo = para a esquerda).</summary>
        public static Vector2 NormalFor(float rise, float run) =>
            new Vector2(-rise * Mathf.Sign(run), Mathf.Abs(run)).normalized;
    }
}
