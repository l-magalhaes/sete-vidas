using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>O que a barra de vida do chefe precisa saber (o Golden, a Isyathy, os próximos).</summary>
    public interface IBossBarSource
    {
        bool IsFighting { get; }
        int Health { get; }
        int MaxHealth { get; }
        /// <summary>Onde fica a marca da fase na barra (fração da vida; 0 = sem marca).</summary>
        float SecondPhaseFraction { get; }
        bool InSecondPhase { get; }
        /// <summary>O nome na barra (null = o nome que a barra já tem).</summary>
        string BarTitle { get; }
    }

    /// <summary>O chefe lutando agora (o que não é o Golden se registra aqui ao começar a luta).</summary>
    public static class BossBarSource
    {
        public static IBossBarSource Active { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; }   // Play sem recarregar o domínio
    }
}
