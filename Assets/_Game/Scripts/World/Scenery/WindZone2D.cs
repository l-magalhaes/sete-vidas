using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Vento de uma área (plano de assets, 6.2): direção e força em tiles/s. Todo efeito que se move com o vento (folhas
    /// caindo, vapor, fumaça) pergunta <see cref="At"/> e segue a mesma direção. Mais forte no alto das torres.
    /// Fora de qualquer zona: sem vento.
    /// </summary>
    public class WindZone2D : MonoBehaviour
    {
        static readonly List<WindZone2D> s_all = new List<WindZone2D>();

        [SerializeField] Rect area;
        [Tooltip("Direção e força (tiles/s). +x = para a direita (o vento da montanha)")]
        [SerializeField] Vector2 wind = new Vector2(0.4f, 0f);

        public void Configure(Rect newArea, Vector2 newWind) { area = newArea; wind = newWind; }

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        /// <summary>O vento num ponto do mundo (a primeira zona que contém o ponto).</summary>
        public static Vector2 At(Vector2 p)
        {
            foreach (var z in s_all) if (z != null && z.area.Contains(p)) return z.wind;
            return Vector2.zero;
        }
    }
}
