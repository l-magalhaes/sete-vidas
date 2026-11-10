using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Aba da capa do Lion: pano preso no ombro que balança junto com a cauda. Parado, cai atrás e respira; correndo,
    /// voa para trás como bandeira e a barra tremula; caindo, sobe com o vento; agachado, deita nas costas.
    /// A barra tem a cor da classe atual. Física e desenho em <see cref="SoftChain"/>/<see cref="SoftRaster"/>.
    /// Spec: SeteVidasDocs/Lion_Arte_e_Animacoes.md §5.3.
    /// </summary>
    public class PlayerCape : MonoBehaviour
    {
        const float Px = 1f / 16f;

        SoftChain _chain;
        PixelBuffer _buffer;
        SpriteRenderer _renderer;
        Color32[] _palette;
        readonly float[] _angles = new float[SoftPoses.CapeSegments.Length];

        public void Build(int sortingLayerId, int sortingOrder)
        {
            _chain = new SoftChain(SoftPoses.CapeSegments) { Stiffness = 300f, Damping = 11f }; // pano: mais solto que a cauda
            _buffer = new PixelBuffer("chr_lion_cape_runtime");
            _palette = PixelBuffer.LionPalette();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _buffer.Sprite;
            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;
        }

        public void SetVisible(bool visible, Color color)
        {
            if (_renderer == null) return;
            _renderer.enabled = visible;
            _renderer.color = color;
            if (!visible) _chain.Reset();
        }

        /// <summary>Cor da barra (a cor da classe).</summary>
        public void SetTrim(Color color)
        {
            if (_palette == null) return;
            color.a = 1f;
            _palette[SoftPal.Trim] = color;
            _palette[SoftPal.TrimDark] = Color.Lerp(color, Color.black, 0.35f);
        }

        /// <summary>Move e redesenha. <paramref name="anchor"/>: ombro (topo da capa) no mundo.</summary>
        public void Step(Vector3 anchor, int facing, BodyMood mood, float speed01, float dt)
        {
            if (_chain == null) return;
            float ax = anchor.x / Px, ay = anchor.y / Px;
            SoftPoses.Cape(mood, facing, Time.time, speed01, _angles);
            _chain.Step(ax, ay, _angles, dt);

            int ox = Mathf.FloorToInt(ax) - PixelBuffer.Size / 2, oy = Mathf.FloorToInt(ay) - PixelBuffer.Size / 2;
            SoftRaster.Clear(_buffer.Indices);
            SoftRaster.Cape(_buffer.Indices, PixelBuffer.Size, ox, oy, _chain.X, _chain.Y, SoftPoses.CapeHalfWidth, facing);
            SoftRaster.Outline(_buffer.Indices, PixelBuffer.Size);
            _buffer.Upload(_palette);
            transform.position = new Vector3(ox * Px, oy * Px, anchor.z);
        }

        void OnDestroy()
        {
            if (_buffer != null) _buffer.Dispose();
        }
    }
}
