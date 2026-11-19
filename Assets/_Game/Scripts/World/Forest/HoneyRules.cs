using SeteVidas.Save;

namespace SeteVidas.World
{
    /// <summary>
    /// O mel das vespas (pedido do usuário, 17/11/2026: "faça o mel ser um item coletável, e o ícone no inventário um
    /// potinho de mel"). Pegar guarda no bolso (sem limite: 19/11/2026, pedido do usuário; era até 9); usar no inventário (aba Itens) cura
    /// <see cref="Heal"/> hit (meio coração). Com a vida cheia, não gasta.
    /// </summary>
    public static class HoneyRules
    {
        public const int Heal = 1;
        public const string Name = "Mel da raiz";

        public static int Count(SaveData data) => data != null ? data.honey : 0;

        /// <summary>Guarda um potinho (sem limite).</summary>
        public static bool Add(SaveData data)
        {
            if (data == null || data.honey == int.MaxValue) return false;
            data.honey++;
            return true;
        }

        /// <summary>Gasta um potinho se der para curar (<paramref name="canHeal"/>: a vida não está cheia).</summary>
        public static bool Use(SaveData data, bool canHeal)
        {
            if (data == null || data.honey <= 0 || !canHeal) return false;
            data.honey--;
            return true;
        }
    }
}
