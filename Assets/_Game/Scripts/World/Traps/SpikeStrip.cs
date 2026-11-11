using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Fileira de espinhos no chão (largura em tiles). Tocar tira 1 hit e o gato volta ao último chão seguro
    /// onde pisou (sem recuo). Se o hit for o último, é morte normal (volta ao santuário).
    /// </summary>
    public class SpikeStrip : TrapBase
    {
        [Tooltip("Largura em tiles (o transform fica no centro da base)")]
        [SerializeField, Min(1)] int width = 3;
        [Tooltip("Altura da área que machuca (tiles)")]
        [SerializeField] float height = 0.5f;
        [SerializeField] SpriteRenderer spriteRenderer;

        public int Width => width;

        void Reset() => returnToSafeGround = true;

        /// <summary>Muda a largura e o desenho (o setup usa ao montar a fase).</summary>
        public void SetWidth(int tiles)
        {
            width = Mathf.Max(1, tiles);
            if (spriteRenderer != null)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
                spriteRenderer.size = new Vector2(width, spriteRenderer.size.y);
            }
        }

        protected override System.Func<Rect> PogoArea => DangerArea;

        protected override Rect DangerArea() =>
            new Rect((Vector2)transform.position - new Vector2(width * 0.5f, 0f), new Vector2(width, height));

        void FixedUpdate()
        {
            var r = DangerArea();
            HitPlayerIn(r.center, new Vector2(r.width - 0.1f, r.height));
        }
    }
}
