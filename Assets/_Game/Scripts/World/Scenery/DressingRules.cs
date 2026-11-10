namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Escolhas "aleatórias" dos enfeites, sempre iguais para a mesma posição (plano de assets, 4.5):
    /// variação, espelho e deslocamento saem de uma semente fixa. C# puro, testado em EditMode.
    /// Escrito em C# 5 (sem membros "=>") para compilar também na prévia da cena (Tools/LevelPreview).
    /// </summary>
    public static class DressingRules
    {
        /// <summary>Semente estável a partir da posição (em pixels) e do id do asset.</summary>
        public static uint Seed(string id, int xPx, int yPx)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (id != null)
                    foreach (char c in id) { h ^= c; h *= 16777619u; }
                h ^= (uint)xPx * 374761393u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= (uint)yPx * 668265263u;
                h = (h ^ (h >> 16)) * 2246822519u;
                return h ^ (h >> 15);
            }
        }

        /// <summary>Número de 0 a 1 a partir da semente e de um canal (para tirar vários sorteios da mesma semente).</summary>
        public static float Value(uint seed, int channel)
        {
            unchecked
            {
                uint h = seed + (uint)channel * 0x9E3779B9u;
                h = (h ^ (h >> 16)) * 0x85EBCA6Bu;
                h = (h ^ (h >> 13)) * 0xC2B2AE35u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>
        /// Variação de 0 a count-1. Regra: nunca a mesma 3 vezes seguidas. <paramref name="previous1"/> e
        /// <paramref name="previous2"/> são as duas últimas escolhidas para este id na mesma fileira (-1 = nenhuma).
        /// </summary>
        public static int PickVariant(uint seed, int count, int previous1, int previous2)
        {
            if (count <= 1) return 0;
            int v = (int)(Value(seed, 0) * count);
            if (v >= count) v = count - 1;
            if (v == previous1 && v == previous2)
            {
                int skip = (int)(Value(seed, 1) * (count - 1));
                if (skip >= count - 1) skip = count - 2; // 0..count-2: nunca volta para a mesma
                v = (v + 1 + skip) % count;
            }
            return v;
        }

        /// <summary>Espelhar? Só se o asset permitir.</summary>
        public static bool PickFlip(uint seed, bool allowed) { return allowed && Value(seed, 2) >= 0.5f; }

        /// <summary>Deslocamento em pixels inteiros, entre -max e +max.</summary>
        public static int PickOffsetPx(uint seed, int maxPx)
        {
            if (maxPx <= 0) return 0;
            int span = 2 * maxPx + 1;
            int v = (int)(Value(seed, 3) * span);
            if (v >= span) v = span - 1;
            return v - maxPx;
        }
    }
}
