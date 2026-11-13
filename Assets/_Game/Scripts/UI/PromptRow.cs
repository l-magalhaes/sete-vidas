using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Uma fileira de comandos na interface: [ícone] texto   [ícone] texto ..., centrada, com os ícones em pixel art do
    /// aparelho em uso (pedido do usuário, 12/11/2026). Ex.: rodapé do inventário, "continuar · pular" do prólogo.
    /// Redesenha sozinha quando o jogador troca de aparelho.
    /// </summary>
    public class PromptRow : MonoBehaviour
    {
        public struct Item
        {
            public GameAction Action;
            public string Label;
            /// <summary>Segunda ação no mesmo ícone (ex.: LB/RB das abas).</summary>
            public GameAction? Also;
            public Item(GameAction action, string label, GameAction? also = null) { Action = action; Label = label; Also = also; }
        }

        const float Spacing = 9f, LabelGap = 3f;

        RectTransform _rt;
        Font _font;
        int _size;
        Color _color;
        Item[] _items = new Item[0];
        readonly List<Image> _glyphs = new List<Image>();
        readonly List<Text> _labels = new List<Text>();

        /// <summary>Cria a fileira dentro de <paramref name="parent"/>, ocupando o retângulo dado (canto de cima à esquerda).</summary>
        public static PromptRow Create(RectTransform parent, string name, Font font, int size, Color color, Vector2 topLeft, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = new Vector2(width, InputGlyphs.Height);
            var row = go.AddComponent<PromptRow>();
            row._rt = rt;
            row._font = font;
            row._size = size;
            row._color = color;
            return row;
        }

        void OnEnable() => InputDevices.Changed += Redraw;
        void OnDisable() => InputDevices.Changed -= Redraw;

        public void Set(params Item[] items)
        {
            _items = items ?? new Item[0];
            Redraw();
        }

        /// <summary>O texto da fileira, sem os ícones (testes).</summary>
        public string Caption
        {
            get
            {
                var parts = new List<string>();
                foreach (var i in _items) parts.Add(i.Label);
                return string.Join(" · ", parts);
            }
        }

        void Redraw()
        {
            if (_rt == null) return;
            while (_glyphs.Count < _items.Length)
            {
                var g = new GameObject("Botão", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                g.transform.SetParent(_rt, false);
                g.raycastTarget = false;
                Corner(g.rectTransform);
                _glyphs.Add(g);
                var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                t.transform.SetParent(_rt, false);
                t.font = _font;
                t.fontSize = _size;
                t.color = _color;
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                Corner(t.rectTransform);
                _labels.Add(t);
            }
            var kind = InputDevices.Current;
            float total = 0f;
            var widths = new float[_items.Length * 2];
            for (int i = 0; i < _items.Length; i++)
            {
                var it = _items[i];
                string paths = InputPrompts.PathFor(it.Action, kind);
                if (it.Also.HasValue) paths += "|" + InputPrompts.PathFor(it.Also.Value, kind);
                var sprite = InputGlyphs.ForPaths(paths);
                _glyphs[i].sprite = sprite;
                float gw = sprite != null ? sprite.rect.width : 0f;
                _glyphs[i].rectTransform.sizeDelta = new Vector2(gw, sprite != null ? sprite.rect.height : 0f);
                _glyphs[i].enabled = sprite != null;
                _labels[i].text = it.Label;
                float lw = Mathf.Ceil(_labels[i].preferredWidth);
                _labels[i].rectTransform.sizeDelta = new Vector2(lw, InputGlyphs.Height);
                _labels[i].enabled = true;
                widths[i * 2] = gw;
                widths[i * 2 + 1] = lw;
                total += gw + LabelGap + lw + (i > 0 ? Spacing : 0f);
            }
            for (int i = _items.Length; i < _glyphs.Count; i++) { _glyphs[i].enabled = false; _labels[i].enabled = false; }
            float x = Mathf.Round((_rt.sizeDelta.x - total) * 0.5f);
            for (int i = 0; i < _items.Length; i++)
            {
                if (i > 0) x += Spacing;
                _glyphs[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
                x += widths[i * 2] + LabelGap;
                _labels[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
                x += widths[i * 2 + 1];
            }
        }

        static void Corner(RectTransform rt) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f); }
    }
}
