using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Camada de fundo com paralaxe: acompanha a câmera numa fração do movimento. <c>factor</c> 0 = presa à câmera
    /// (o céu); 1 = parada no mundo. Com <c>wrapWidth</c> &gt; 0, repete na horizontal (cópias à esquerda e à direita).
    /// A posição é arredondada para o pixel (16 PPU) para não tremer.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Quanto a camada fica parada no mundo (0 = presa à câmera, 1 = parada)")]
        [SerializeField] Vector2 factor = new Vector2(0.05f, 0.05f);
        [Tooltip("Posição da camada quando a câmera está em cima dela (mundo)")]
        [SerializeField] Vector2 anchor;
        [Tooltip("Largura do sprite em unidades para repetir (0 = não repete)")]
        [SerializeField, Min(0f)] float wrapWidth;

        Transform _camera;

        public void Configure(Vector2 newFactor, Vector2 newAnchor, float newWrapWidth)
        {
            factor = newFactor; anchor = newAnchor; wrapWidth = newWrapWidth;
        }

        void LateUpdate()
        {
            if (_camera == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                _camera = cam.transform;
            }
            Vector2 c = _camera.position;
            Vector2 p = anchor + (c - anchor) * (Vector2.one - factor);
            if (wrapWidth > 0f) p.x = c.x + Mathf.Repeat(p.x - c.x + wrapWidth * 0.5f, wrapWidth) - wrapWidth * 0.5f;
            const float px = 1f / 16f;
            transform.position = new Vector3(Mathf.Round(p.x / px) * px, Mathf.Round(p.y / px) * px, transform.position.z);
        }
    }
}
