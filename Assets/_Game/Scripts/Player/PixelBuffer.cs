using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Textura pequena (32×32, filtro Point, 16 PPU) que recebe um buffer de índices de paleta desenhado pelo
    /// <see cref="SoftRaster"/>. Usada pela cauda e pela capa, redesenhadas a cada quadro.
    /// </summary>
    internal sealed class PixelBuffer
    {
        public const int Size = 32;
        public readonly byte[] Indices = new byte[Size * Size];
        public readonly Sprite Sprite;
        readonly Color32[] _pixels = new Color32[Size * Size];
        readonly Texture2D _texture;

        public PixelBuffer(string name)
        {
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            Sprite = Sprite.Create(_texture, new Rect(0, 0, Size, Size), Vector2.zero, 16f, 0, SpriteMeshType.FullRect);
            Sprite.name = name;
        }

        public void Upload(Color32[] palette)
        {
            for (int i = 0; i < Indices.Length; i++) _pixels[i] = palette[Indices[i]];
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        public void Dispose()
        {
            Object.Destroy(Sprite);
            Object.Destroy(_texture);
        }

        /// <summary>Paleta do Lion (igual à de Tools/LionSprites/LionSpriteGen.cs).</summary>
        public static Color32[] LionPalette()
        {
            var p = new Color32[SoftPal.Count];
            p[SoftPal.Empty] = new Color32(0, 0, 0, 0);
            p[SoftPal.Outline] = Hex(0x1a1c26);
            p[SoftPal.FurLight] = Hex(0xf4d6a3);
            p[SoftPal.Fur] = Hex(0xe3b77d);
            p[SoftPal.FurDark] = Hex(0xb98552);
            p[SoftPal.ClothLight] = Hex(0x6a7080);
            p[SoftPal.Cloth] = Hex(0x4a4f5c);
            p[SoftPal.ClothDark] = Hex(0x33363f);
            p[SoftPal.Trim] = Hex(0x2b3a67);
            p[SoftPal.TrimDark] = Hex(0x1c2645);
            return p;
        }

        static Color32 Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
