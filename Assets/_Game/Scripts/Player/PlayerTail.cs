using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Cauda procedural do Lion: uma corrente com mola (a ponta atrasa e chicoteia) redesenhada pixel a pixel a cada
    /// quadro como uma forma só, afinando na base e felpuda no meio, com luz em cima, sombra embaixo e ponta clara.
    /// A pose muda com o estado (parado: gancho balançando; correndo: reta para trás; agachado: baixa com a ponta
    /// em gancho). Física e desenho em <see cref="SoftChain"/>/<see cref="SoftRaster"/>.
    /// Spec: SeteVidasDocs/Lion_Arte_e_Animacoes.md §5.2.
    /// </summary>
    public class PlayerTail : MonoBehaviour
    {
        const float Px = 1f / 16f;

        SoftChain _chain;
        PixelBuffer _buffer;
        SpriteRenderer _renderer;
        Color32[] _palette;
        readonly float[] _angles = new float[SoftPoses.TailSegments.Length];
        float _bristleUntil;

        public void Build(int sortingLayerId, int sortingOrder)
        {
            _chain = new SoftChain(SoftPoses.TailSegments);
            _buffer = new PixelBuffer("chr_lion_tail_runtime");
            _palette = PixelBuffer.LionPalette();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _buffer.Sprite;
            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;
        }

        /// <summary>Pelo eriçado (cauda mais grossa) por alguns segundos: dano e susto.</summary>
        public void Bristle(float seconds) => _bristleUntil = Time.time + seconds;

        public void SetVisible(bool visible, Color color)
        {
            if (_renderer == null) return;
            _renderer.enabled = visible;
            _renderer.color = color;
            if (!visible) _chain.Reset(); // reaparece já na pose, sem chicotear do lugar antigo
        }

        /// <summary>Move e redesenha. <paramref name="anchor"/>: base da cauda no mundo.</summary>
        public void Step(Vector3 anchor, int facing, BodyMood mood, float speed01, float dt)
        {
            if (_chain == null) return;
            float ax = anchor.x / Px, ay = anchor.y / Px;
            SoftPoses.Tail(mood, facing, Time.time, speed01, _angles);
            _chain.Step(ax, ay, _angles, dt);

            int ox = Mathf.FloorToInt(ax) - PixelBuffer.Size / 2, oy = Mathf.FloorToInt(ay) - PixelBuffer.Size / 2;
            float bristle = Time.time < _bristleUntil ? 1.3f : 1f;
            SoftRaster.Clear(_buffer.Indices);
            SoftRaster.Tail(_buffer.Indices, PixelBuffer.Size, ox, oy, _chain.X, _chain.Y, SoftPoses.TailRadius, bristle);
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
