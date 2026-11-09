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
