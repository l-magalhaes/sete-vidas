using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>Fase de uma armadilha periódica (jato de chama, lança automática).</summary>
    public enum TrapPhase { Off, Warning, On }

    /// <summary>
    /// Regras das armadilhas. C# puro (testado em EditMode) + registro das áreas perigosas, usado para
    /// escolher o "último chão seguro" para onde o gato volta ao tocar espinhos.
    /// </summary>
    public static class TrapRules
    {
        /// <summary>
        /// Fase no instante <paramref name="time"/> de um ciclo desligado → aviso → ligado.
        /// <paramref name="offset"/> desloca o ciclo (para fazer ondas com várias armadilhas).
        /// </summary>
        public static TrapPhase PhaseAt(float time, float offset, float offTime, float warnTime, float onTime)
        {
            float cycle = Math.Max(0.01f, offTime + warnTime + onTime);
            float t = ((time + offset) % cycle + cycle) % cycle;
            if (t < offTime) return TrapPhase.Off;
            if (t < offTime + warnTime) return TrapPhase.Warning;
            return TrapPhase.On;
        }

        /// <summary>Placa de pressão: o gato agachado anda leve e não dispara (passos de ladrão).</summary>
        public static bool PlateTriggers(bool grounded, bool crouching) => grounded && !crouching;

        /// <summary>
        /// Fio de alarme: dispara se o corpo do gato (do chão até <paramref name="bodyTop"/>) cruza o fio.
        /// Agachado, o topo fica abaixo do fio e ele passa por baixo.
        /// </summary>
        public static bool WireTouches(float feetY, float bodyTop, float wireBottom, float wireTop) =>
            bodyTop > wireBottom && feetY < wireTop;

        // ----------------------------------------------- áreas perigosas

        static readonly List<Func<Rect>> s_dangers = new List<Func<Rect>>();

        /// <summary>Registra uma área perigosa (retângulo no mundo, pode mudar com o tempo).</summary>
        public static void RegisterDanger(Func<Rect> area) { if (!s_dangers.Contains(area)) s_dangers.Add(area); }
        public static void UnregisterDanger(Func<Rect> area) => s_dangers.Remove(area);

        /// <summary>O ponto está a menos de <paramref name="margin"/> tiles de alguma área perigosa?</summary>
        public static bool IsNearDanger(Vector2 point, float margin)
        {
            foreach (var area in s_dangers)
            {
                var r = area();
                r.xMin -= margin; r.xMax += margin; r.yMin -= margin; r.yMax += margin;
                if (r.Contains(point)) return true;
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_dangers.Clear();
    }
}
