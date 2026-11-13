using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.UI
{
    /// <summary>
    /// Aviso de interação no mundo ("falar", "ler", "descansar", "pegar o tomo") com o ícone do botão do aparelho em uso:
    /// a tecla E no teclado, o B no controle de Xbox (pedido do usuário, 12/11/2026). Toma o lugar do TextMesh
    /// "E: falar" que os setups criam: esconde o texto e desenha o ícone e o verbo na fonte em pixel. Os scripts
    /// continuam ligando e desligando o objeto do aviso como antes.
    /// </summary>
    public class WorldPrompt : MonoBehaviour
    {
        GameAction _action = GameAction.Interact;
        string _verb = "";
        Color _color = Color.white;
        SpriteRenderer _glyph, _label;

        /// <summary>O verbo mostrado (testes).</summary>
        public string Verb => _verb;
        public SpriteRenderer GlyphRenderer => _glyph;

        /// <summary>Troca o TextMesh "E: verbo" por este aviso (uma vez só). Retorna o aviso.</summary>
        public static WorldPrompt Upgrade(TextMesh text, GameAction action = GameAction.Interact)
        {
            if (text == null) return null;
            var existing = text.GetComponent<WorldPrompt>();
            if (existing != null) return existing;
            string raw = text.text ?? "";
            int colon = raw.IndexOf(':');
            string verb = colon >= 0 ? raw.Substring(colon + 1).Trim() : raw.Trim();
            var p = text.gameObject.AddComponent<WorldPrompt>();
            p._action = action;
            p._verb = verb;
            p._color = text.color;
            var mr = text.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            p.Build();
            return p;
        }

        void Build()
        {
            _glyph = Child("Botão");
            _label = Child("Verbo");
            Redraw();
        }

        SpriteRenderer Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            return sr;
        }

        void OnEnable()
        {
            InputDevices.Changed += Redraw;
            if (_glyph != null) Redraw();
        }

        void OnDisable() => InputDevices.Changed -= Redraw;

        void Redraw()
        {
            if (_glyph == null) return;
            var glyph = InputPrompts.Glyph(_action);
            var label = PixelFont.Render(_verb, _color, PixelFont.Outline, 0f);
            _glyph.sprite = glyph;
            _label.sprite = label;
            // o par fica centrado no ponto do aviso: [botão] 2 px [verbo], o verbo no meio da altura do botão
            float px = 1f / PixelFont.Ppu;
            float gw = glyph != null ? glyph.rect.width : 0f, lw = label.rect.width;
            float total = gw + 2f + lw;
            _glyph.transform.localPosition = new Vector3(Mathf.Round(-total * 0.5f + gw * 0.5f) * px, 0f, 0f);
            _label.transform.localPosition = new Vector3(Mathf.Round(-total * 0.5f + gw + 2f) * px, 2f * px, 0f);
            // o aviso não segue a escala do pai (ex.: o NPC virado) e fica no pixel inteiro
            var s = transform.lossyScale;
            transform.localScale = new Vector3(s.x < 0f ? -transform.localScale.x : transform.localScale.x, transform.localScale.y, 1f);
        }
    }
}
