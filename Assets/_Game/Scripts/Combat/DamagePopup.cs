using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Texto de depuração que sobe e some (dano no boneco de treino). Não é a UI final.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        const float Lifetime = 0.7f;
        const float Rise = 0.9f;

        TextMesh _text;
        Color _color;
        float _age;
        Vector3 _start;

        public static DamagePopup Spawn(Vector3 position, string text, Color color)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = position;
            var popup = go.AddComponent<DamagePopup>();
            popup._text = CreateText(go, text, color, 60);
            popup._color = color;
            popup._start = position;
            return popup;
        }

        /// <summary>TextMesh de depuração com a fonte embutida do Unity. Altura ≈ fontSize × 0,01 tile.</summary>
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

        void Update()
        {
            _age += Time.unscaledDeltaTime; // continua subindo durante o hitstop
            float t = _age / Lifetime;
            transform.position = _start + Vector3.up * (Rise * (1f - (1f - t) * (1f - t)));
            var c = _color;
            c.a = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
            _text.color = c;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
