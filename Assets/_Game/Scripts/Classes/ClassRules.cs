using System;

namespace SeteVidas.Classes
{
    /// <summary>Regras da troca de classe. C# puro, testado em EditMode.</summary>
    public static class ClassRules
    {
        public const int ClassCount = 7;

        /// <summary>Não troca no meio de um ataque. No ar, escalando ou pendurado é permitido.</summary>
        public static bool CanSwitch(bool isAttacking, bool isDead) => !isAttacking && !isDead;

        /// <summary>
        /// Próxima classe disponível na direção <paramref name="dir"/> (+1 = R, -1 = Q), dando a volta.
        /// Retorna false se nenhuma outra classe estiver disponível.
        /// </summary>
        public static bool TryCycle(CharacterClass current, int dir, Func<CharacterClass, bool> isAvailable, out CharacterClass next)
        {
            next = current;
            int step = dir >= 0 ? 1 : -1;
            int index = (int)current;
            for (int i = 1; i < ClassCount; i++)
            {
                var candidate = (CharacterClass)(((index + step * i) % ClassCount + ClassCount) % ClassCount);
                if (isAvailable(candidate))
                {
                    next = candidate;
                    return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- roda de classes (14/11/2026)

        /// <summary>Segurar a troca de classe por este tempo (s) abre a roda; menos que isso é um toque (alterna).</summary>
        public const float WheelHoldTime = 0.22f;
        /// <summary>Tempo do jogo com a roda aberta (câmera lenta, não pausa).</summary>
        public const float WheelTimeScale = 0.15f;

        /// <summary>
        /// Fatia da roda para onde aponta <paramref name="dir"/> (0 = em cima, sentido horário, <paramref name="count"/>
        /// fatias). Abaixo de <paramref name="deadZone"/>: -1 (nenhuma).
        /// </summary>
        public static int WheelSlotAt(UnityEngine.Vector2 dir, int count, float deadZone)
        {
            if (count <= 0 || dir.magnitude < deadZone) return -1;
            float a = UnityEngine.Mathf.Atan2(dir.x, dir.y) * UnityEngine.Mathf.Rad2Deg; // 0 = cima, 90 = direita
            if (a < 0f) a += 360f;
            float step = 360f / count;
            return (int)UnityEngine.Mathf.Floor((a + step * 0.5f) / step) % count;
        }

        /// <summary>Ângulo (graus, horário a partir de cima) do meio da fatia <paramref name="slot"/>.</summary>
        public static float WheelSlotAngle(int slot, int count) => count <= 0 ? 0f : slot * 360f / count;

        /// <summary>Tecla 1–7 para a classe (1 = Ladino ... 7 = Druida). Fora disso: false.</summary>
        public static bool FromSlot(int slot1To7, out CharacterClass c)
        {
            c = CharacterClass.Rogue;
            if (slot1To7 < 1 || slot1To7 > ClassCount) return false;
            c = (CharacterClass)(slot1To7 - 1);
            return true;
        }
    }
}
