using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Texto dos prompts de interação ("E: falar", "E: ler"), montado pelos setups do editor.
    /// Os números de dano e os avisos flutuantes saíram (pedido do usuário, 12/11/2026): o retorno do golpe é
    /// o flash, as faíscas e o som; os avisos que ficaram usam a fonte em pixel (<see cref="PixelPopup"/>).
    /// </summary>
    public static class DamagePopup
    {
        /// <summary>TextMesh com a fonte embutida do Unity. Altura ≈ fontSize × 0,01 tile.</summary>
        public static TextMesh CreateText(GameObject go, string text, Color color, int fontSize)
        {
            var mesh = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            mesh.text = text;
            mesh.color = color;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.1f;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 50;
            return mesh;
        }
    }
}
