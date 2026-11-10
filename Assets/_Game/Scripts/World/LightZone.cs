using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Retângulo de sombra (ou de luz parcial). Dentro dele, a exposição do gato cai para <see cref="Exposure"/>.
    /// O transform é o centro; <see cref="Size"/> é o tamanho em tiles. O visual escuro é um filho opcional.
    /// </summary>
    [ExecuteAlways]
    public class LightZone : MonoBehaviour
    {
        static readonly List<LightZone> s_active = new List<LightZone>();

        [Tooltip("Exposição dentro da zona: 1 = luz plena, 0 = escuridão total")]
        [SerializeField, Range(0f, 1f)] float exposure = 0.3f;
        [Tooltip("Tamanho em tiles (centro no transform)")]
        [SerializeField] Vector2 size = new Vector2(6f, 5f);
        [Tooltip("Sprite escuro que mostra a sombra (escalado para o tamanho da zona)")]
        [SerializeField] SpriteRenderer shade;

        public float Exposure => exposure;
        public Vector2 Size => size;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        void OnEnable() { if (!s_active.Contains(this)) s_active.Add(this); ApplyVisual(); }
        void OnDisable() => s_active.Remove(this);
        void OnValidate() => ApplyVisual();

        public void Configure(float newExposure, Vector2 newSize)
        {
            exposure = newExposure;
            size = newSize;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (shade == null) return;
            shade.transform.localPosition = Vector3.zero;
            if (shade.drawMode == SpriteDrawMode.Simple)
                shade.transform.localScale = new Vector3(size.x, size.y, 1f); // sprite de 1×1 tile
            else
            {
                // sombra de borda suave (9-slice): a borda pontilhada fica no tamanho original, o meio estica
                shade.transform.localScale = Vector3.one;
                shade.size = size;
            }
            var c = shade.color;
            c.a = Mathf.Lerp(0.6f, 0f, exposure); // mais escuro = mais opaco
            shade.color = c;
        }

        /// <summary>Exposição neste ponto: a menor entre as zonas que o contêm; 1 fora de todas.</summary>
        public static float ExposureAt(Vector2 point)
        {
            float result = 1f;
            foreach (var zone in s_active)
                if (zone != null && zone.isActiveAndEnabled && zone.Area.Contains(point))
                    result = Mathf.Min(result, zone.exposure);
            return result;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.3f, 0.8f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
#endif
    }
}
