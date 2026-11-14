using System;
using System.Collections.Generic;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>Golpes do Golden.</summary>
    public enum BossMove { Walk, Swing, Leap, Heal, Lightning }

    /// <summary>Situação no momento de escolher o próximo golpe.</summary>
    public struct BossSituation
    {
        public float DistanceToPlayer;   // horizontal, em tiles
        public float HealthFraction;     // 0 a 1
        public bool HealReady;           // passou o tempo de recarga da cura
        public bool LightningReady;      // passou o tempo de recarga dos relâmpagos
        public float SwingRange;
        public float HealBelowFraction;  // só cura abaixo desta fração de vida
    }

    /// <summary>
    /// Escolha de golpes e posição dos relâmpagos. C# puro (testado em EditMode): o mesmo sorteio dá sempre
    /// o mesmo resultado, e os relâmpagos sempre deixam um espaço seguro.
    /// </summary>
    public static class BossRules
    {
        /// <param name="roll">Número de 0 a 1 (ex.: Random.value).</param>
        public static BossMove Pick(in BossSituation s, float roll)
        {
            bool canHeal = s.HealReady && s.HealthFraction <= s.HealBelowFraction;
            if (canHeal && roll < 0.35f) return BossMove.Heal;
            if (s.LightningReady && (s.DistanceToPlayer > 4f || roll < 0.3f)) return BossMove.Lightning;
            if (s.DistanceToPlayer <= s.SwingRange) return roll < 0.7f ? BossMove.Swing : BossMove.Leap;
            if (s.DistanceToPlayer > 6f) return BossMove.Leap;
            return roll < 0.5f ? BossMove.Walk : BossMove.Leap;
        }

        /// <summary>
        /// Posições X dos relâmpagos de uma onda. Garante uma faixa livre de pelo menos <paramref name="safeGap"/>
        /// tiles longe do gato (ele precisa se mexer) e sempre um raio em cima de onde o gato está.
        /// </summary>
        /// <param name="rand01">Sorteio de 0 a 1.</param>
        public static List<float> LightningColumns(float minX, float maxX, int count, float strikeWidth, float safeGap,
            float playerX, Func<float> rand01)
        {
            var result = new List<float>();
            // Faixas que cobrem a arena inteira (cada uma um pouco mais larga que o raio, nunca mais estreita)
            int slots = Math.Max(1, (int)Math.Floor((maxX - minX) / strikeWidth));
            float slotWidth = (maxX - minX) / slots;
            int safeSlots = Math.Max(1, (int)Math.Ceiling(safeGap / slotWidth));
            if (safeSlots >= slots) return result; // arena pequena demais: nenhum raio

            int playerSlot = Clamp((int)Math.Floor((playerX - minX) / slotWidth), 0, slots - 1);

            // Faixa segura: não pode conter o gato (senão ele não precisaria se mexer)
            var starts = new List<int>();
            for (int s = 0; s + safeSlots <= slots; s++)
                if (playerSlot < s || playerSlot >= s + safeSlots) starts.Add(s);
            int safeStart = starts.Count > 0 ? starts[Clamp((int)(rand01() * starts.Count), 0, starts.Count - 1)] : 0;

            var candidates = new List<int>();
            for (int i = 0; i < slots; i++)
                if (i < safeStart || i >= safeStart + safeSlots) candidates.Add(i);

            // Primeiro, onde o gato está; depois, sorteio sem repetir
            if (candidates.Remove(playerSlot)) result.Add(Center(playerSlot, minX, slotWidth));
            while (result.Count < count && candidates.Count > 0)
            {
                int pick = Clamp((int)(rand01() * candidates.Count), 0, candidates.Count - 1);
                result.Add(Center(candidates[pick], minX, slotWidth));
                candidates.RemoveAt(pick);
            }
            return result;
        }

        /// <summary>A faixa [a, b] fica livre de raios de largura <paramref name="strikeWidth"/>?</summary>
        public static bool IsClear(IEnumerable<float> strikes, float strikeWidth, float a, float b)
        {
            foreach (float x in strikes)
                if (x + strikeWidth * 0.5f > a && x - strikeWidth * 0.5f < b) return false;
            return true;
        }

        /// <summary>
        /// O gato está em cima da cabeça (pulando nela ou parado nela)? Pés acima do alto do corpo, até
        /// <paramref name="maxAbove"/> tiles, e na largura do corpo mais <paramref name="sideMargin"/> de cada lado.
        /// </summary>
        public static bool PlayerAbove(float bossX, float bossFeetY, float bodyWidth, float bodyHeight,
            float playerX, float playerFeetY, float sideMargin = 0.9f, float maxAbove = 8f)
        {
            float top = bossFeetY + bodyHeight;
            if (playerFeetY < top - 0.25f || playerFeetY > top + maxAbove) return false;
            return Math.Abs(playerX - bossX) <= bodyWidth * 0.5f + sideMargin;
        }

        /// <summary>
        /// Repulsa sagrada (anti "quicar na cabeça"): reage quando o gato fica tempo demais em cima, ou depois de
        /// vários golpes de cima seguidos, respeitando o tempo de recarga.
        /// </summary>
        public static bool ShouldRepel(float secondsAbove, float afterAbove, int hitsFromAbove, int afterHits,
            float sinceLastRepel, float cooldown) =>
            sinceLastRepel >= cooldown && (secondsAbove >= afterAbove || hitsFromAbove >= afterHits);

        static float Center(int slot, float minX, float width) => minX + (slot + 0.5f) * width;
        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
