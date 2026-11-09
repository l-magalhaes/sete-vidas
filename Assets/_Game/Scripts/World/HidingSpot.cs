using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Esconderijo (matinho, arbusto, pilha de caixas baixas): só funciona com o gato **agachado**.
    /// Agachado dentro da área, a visibilidade é multiplicada por <see cref="Concealment"/>; de pé, nada muda
    /// (a cabeça aparece por cima). O transform fica no chão, no centro da base; a área sobe a partir dele.
    /// </summary>
    [ExecuteAlways]
    public class HidingSpot : MonoBehaviour
    {
        static readonly List<HidingSpot> s_active = new List<HidingSpot>();

        [Tooltip("Multiplicador da visibilidade agachado aqui dentro: 0 = invisível, 1 = não esconde")]
        [SerializeField, Range(0f, 1f)] float concealment = 0.25f;
        [Tooltip("Largura × altura em tiles, a partir do chão (centro da base no transform)")]
        [SerializeField] Vector2 size = new Vector2(2f, 0.95f);

        public float Concealment => concealment;
        public Vector2 Size => size;
        public Rect Area => new Rect((Vector2)transform.position - new Vector2(size.x * 0.5f, 0f), size);

        void OnEnable() { if (!s_active.Contains(this)) s_active.Add(this); }
        void OnDisable() => s_active.Remove(this);

        /// <summary>Multiplicador para um gato agachado neste ponto: o menor entre os esconderijos; 1 fora deles.</summary>
        public static float ConcealmentAt(Vector2 point)
        {
            float result = 1f;
            foreach (var spot in s_active)
                if (spot != null && spot.isActiveAndEnabled && spot.Area.Contains(point))
                    result = Mathf.Min(result, spot.concealment);
            return result;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.7f);
            var r = Area;
            Gizmos.DrawWireCube(r.center, r.size);
        }
#endif
    }
}
