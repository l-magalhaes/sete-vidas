using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>Cone de visão e velocidade de detecção. Sem MonoBehaviour: testado em EditMode.</summary>
    public static class PerceptionMath
    {
        /// <summary>O alvo está dentro do cone (distância e ângulo), olhando para <paramref name="facing"/>?</summary>
        public static bool InVisionCone(Vector2 eye, int facing, Vector2 target, float range, float halfAngleDegrees)
        {
            Vector2 d = target - eye;
            float sqr = d.sqrMagnitude;
            if (sqr > range * range) return false;
            if (sqr < 1e-4f) return true;
            float angle = Vector2.Angle(new Vector2(facing >= 0 ? 1f : -1f, 0f), d);
            return angle <= halfAngleDegrees;
        }

        /// <summary>
        /// Cone inclinado <paramref name="tiltDownDegrees"/> para baixo (atirador no alto olhando para o chão).
        /// Com inclinação 0, é o mesmo cone de <see cref="InVisionCone(Vector2,int,Vector2,float,float)"/>.
        /// </summary>
        public static bool InVisionCone(Vector2 eye, int facing, Vector2 target, float range, float halfAngleDegrees, float tiltDownDegrees)
        {
            Vector2 d = target - eye;
            float sqr = d.sqrMagnitude;
            if (sqr > range * range) return false;
            if (sqr < 1e-4f) return true;
            float t = tiltDownDegrees * Mathf.Deg2Rad;
            var axis = new Vector2((facing >= 0 ? 1f : -1f) * Mathf.Cos(t), -Mathf.Sin(t));
            return Vector2.Angle(axis, d) <= halfAngleDegrees;
        }

        /// <summary>
        /// Graus abaixo da horizontal de <paramref name="from"/> até <paramref name="to"/> (negativo = acima).
        /// </summary>
        public static float AngleBelowHorizontal(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            return Mathf.Atan2(-d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
        }

        /// <summary>Qual pose de mira usar: 0 (reto), 30 ou 60 graus para baixo.</summary>
        public static int AimBucket(float degreesBelow)
        {
            if (degreesBelow < 15f) return 0;
            return degreesBelow < 45f ? 30 : 60;
        }

        /// <summary>Alcance real contra este alvo: alcance base × visibilidade.</summary>
        public static float EffectiveRange(float range, float visibility) => range * Mathf.Clamp01(visibility);

        /// <summary>
        /// Quanto a suspeita sobe por segundo vendo o alvo a esta distância.
        /// Na borda do alcance efetivo: 0,5 × base. Colado: 2 × base. Fora do alcance: 0.
        /// </summary>
        public static float SightFillRate(float distance, float effectiveRange, float baseRate)
        {
            if (effectiveRange <= 0f || distance > effectiveRange) return 0f;
            float t = Mathf.Clamp01(distance / effectiveRange);
            return baseRate * (0.5f + 1.5f * (1f - t));
        }
    }
}
