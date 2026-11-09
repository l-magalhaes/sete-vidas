using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>O que acontece quando um fio de alarme ou uma placa de pressão dispara (flechas, lanças...).</summary>
    public abstract class TrapEffect : TrapBase
    {
        /// <summary>Dispara a armadilha uma vez.</summary>
        public abstract void Fire();
    }
}
