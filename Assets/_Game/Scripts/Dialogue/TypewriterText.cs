using System;

namespace SeteVidas.Dialogue
{
    /// <summary>Texto que aparece letra por letra. C# puro, testado em EditMode.</summary>
    public sealed class TypewriterText
    {
        string _text = "";
        float _shown;

        public string Text => _text;
        /// <summary>Quantas letras estão visíveis.</summary>
        public int Visible => Math.Min(_text.Length, (int)_shown);
        public bool IsComplete => Visible >= _text.Length;
        public string VisibleText => _text.Substring(0, Visible);

        public void Start(string text)
        {
            _text = text ?? "";
            _shown = 0f;
        }

        /// <summary>Avança o tempo. Retorna quantas letras novas apareceram.</summary>
        public int Tick(float deltaTime, float charsPerSecond)
        {
            int before = Visible;
            if (!IsComplete) _shown += Math.Max(0f, deltaTime) * Math.Max(1f, charsPerSecond);
            return Visible - before;
        }

        /// <summary>Mostra a fala inteira de uma vez (jogador apertou para pular).</summary>
        public void Complete() => _shown = _text.Length;
    }
}
