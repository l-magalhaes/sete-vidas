using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Céu que aparece aos poucos conforme a câmera sobe (plano de assets, 7.8: dentro do mesmo exterior, a luz muda
    /// com a altura sem troca visível). Fica por cima do céu de baixo do mesmo conjunto: transparente com a câmera em
    /// <see cref="fromY"/>, inteiro em <see cref="toY"/>. Roda depois do <see cref="BackgroundSets"/> e multiplica pela
    /// transparência da troca de conjunto, para os dois efeitos não brigarem.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SkyHeightBlend : MonoBehaviour
    {
        [SerializeField] float fromY = 10f;
        [SerializeField] float toY = 40f;

        SpriteRenderer[] _renderers;
        BackgroundSets _sets;
        Transform _camera;

        public void Configure(float newFromY, float newToY) { fromY = newFromY; toY = newToY; }

        /// <summary>Quanto do céu de cima aparece com a câmera na altura <paramref name="cameraY"/> (0..1, suave).</summary>
        public static float Amount(float cameraY, float from, float to)
        {
            if (to <= from) return cameraY >= from ? 1f : 0f;
            float t = Mathf.Clamp01((cameraY - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        void LateUpdate()
        {
            if (_camera == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                _camera = cam.transform;
            }
            if (_renderers == null) _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_sets == null) _sets = GetComponentInParent<BackgroundSets>();
            float a = Amount(_camera.position.y, fromY, toY);
            float fade = _sets != null ? _sets.AlphaOf(gameObject) : 1f;
            foreach (var r in _renderers)
            {
                var c = r.color; c.a = a * fade; r.color = c;
            }
        }
    }
}
