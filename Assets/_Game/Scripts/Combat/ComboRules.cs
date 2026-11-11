using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>Quem pode ser derrubado (o terremoto da marreta).</summary>
    public interface IKnockdown
    {
        void Knockdown(float seconds);
    }

    /// <summary>Regras puras do golpe final com avanço (melhoria 4, EditMode).</summary>
    public static class ComboRules
    {
        /// <summary>É o golpe final de um combo de mais de um golpe (a marreta, de 1 golpe, não tem).</summary>
        public static bool IsFinisher(int step, int steps) => steps > 1 && step == steps - 1;

        /// <summary>Dano do golpe final: arredonda para cima (2 × 1,5 = 3; 3 × 1,5 = 5).</summary>
        public static int FinisherDamage(int amount, float multiplier) =>
            amount <= 0 ? amount : Mathf.CeilToInt(amount * Mathf.Max(1f, multiplier) - 1e-4f);

        /// <summary>
        /// Quanto o avanço anda neste passo: o que falta, limitado à velocidade e ao vão até a parede.
        /// Sem chão à frente, para (o avanço nunca joga o gato de uma borda).
        /// </summary>
        public static float LungeStep(float remaining, float speed, float dt, float wallGap, bool groundAhead)
        {
            if (remaining <= 0f || !groundAhead) return 0f;
            return Mathf.Max(0f, Mathf.Min(remaining, speed * dt, wallGap));
        }
    }

    /// <summary>Regras puras do golpe carregado da marreta (melhoria 4, EditMode).</summary>
    public static class ChargeRules
    {
        /// <summary>Segurou o bastante para virar carga.</summary>
        public static bool IsCharge(float heldSeconds, float chargeStart) => heldSeconds >= chargeStart;

        /// <summary>De 0 (carga mínima, ao começar a carregar) a 1 (carga máxima).</summary>
        public static float Charge01(float heldSeconds, float chargeStart, float chargeFull)
        {
            if (chargeFull <= chargeStart) return heldSeconds >= chargeStart ? 1f : 0f;
            return Mathf.Clamp01((heldSeconds - chargeStart) / (chargeFull - chargeStart));
        }

        public static float Range(float charge01, Vector2 minMax) => Mathf.Lerp(minMax.x, minMax.y, Mathf.Clamp01(charge01));

        public static int Damage(float charge01, Vector2Int minMax) =>
            Mathf.RoundToInt(Mathf.Lerp(minMax.x, minMax.y, Mathf.Clamp01(charge01)));

        /// <summary>
        /// O alvo está na faixa da onda: dentro do alcance para um dos lados e de pé no mesmo chão
        /// (o pé dele perto da altura do pé do gato; quem está no ar ou numa plataforma não leva).
        /// </summary>
        public static bool InShockwave(Vector2 origin, float range, Rect target)
        {
            if (target.xMax < origin.x - range || target.xMin > origin.x + range) return false;
            return target.yMin >= origin.y - 0.35f && target.yMin <= origin.y + 0.5f;
        }
    }
}

namespace SeteVidas.Combat
{
    /// <summary>
    /// Superfícies em que a estocada para baixo quica (melhoria 5): espinhos e serras, além dos alvos que tomam dano.
    /// Cada uma registra a área que machuca agora; acertá-la por cima com a estocada não machuca o gato.
    /// </summary>
    public static class PogoSurfaces
    {
        static readonly System.Collections.Generic.List<System.Func<Rect>> s_all = new System.Collections.Generic.List<System.Func<Rect>>();

        public static void Register(System.Func<Rect> area) { if (area != null && !s_all.Contains(area)) s_all.Add(area); }
        public static void Unregister(System.Func<Rect> area) => s_all.Remove(area);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        /// <summary>A caixa da estocada encosta em alguma superfície de quique.</summary>
        public static bool Touches(Rect stab)
        {
            for (int i = s_all.Count - 1; i >= 0; i--)
                if (s_all[i] != null && s_all[i]().Overlaps(stab)) return true;
            return false;
        }
    }

    /// <summary>Regras puras do ataque para baixo (EditMode).</summary>
    public static class DownAttackRules
    {
        /// <summary>No ar com "baixo" segurado: o ataque vira o golpe para baixo. No chão, continua o golpe agachado.</summary>
        public static bool WantsDownAttack(bool grounded, float moveY, bool crouchHeld) =>
            !grounded && (crouchHeld || moveY < -0.5f);

        /// <summary>Quica em quem toma dano e nas superfícies de quique (espinhos, serras); a marreta não quica.</summary>
        public static bool Bounces(bool isDagger, bool hitTarget, bool hitPogoSurface) =>
            isDagger && (hitTarget || hitPogoSurface);

        /// <summary>Caixa da estocada embaixo dos pés.</summary>
        public static Rect StabBox(Vector2 feet, Vector2 size, float below) =>
            new Rect(feet.x - size.x * 0.5f, feet.y - below - size.y * 0.5f, size.x, size.y);
    }
}
