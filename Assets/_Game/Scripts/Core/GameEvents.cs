using System;
using UnityEngine;

namespace SeteVidas.Core
{
    /// <summary>
    /// Avisos globais simples. <see cref="WorldReset"/> dispara quando o gato descansa num santuário ou
    /// volta depois de morrer: inimigos derrotados reaparecem e voltam às patrulhas (decisão de 09/11/2026).
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Inimigos voltam ao ponto inicial com vida cheia.</summary>
        public static event Action WorldReset;

        public static void RaiseWorldReset() => WorldReset?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => WorldReset = null;
    }
}
