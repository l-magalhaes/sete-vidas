using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Decide para que lado o gato quer olhar a partir do direcional. C# puro (EditMode).
    ///
    /// No teclado (e no D-pad) a direção vale na hora. No analógico há dois problemas que viravam o gato sem querer:
    /// 1. ao soltar o analógico, ele passa do centro para o outro lado por 1–3 quadros (retorno da mola);
    /// 2. empurrando para baixo (agachar) ou para cima (escalar), sobra um pouco de x para um dos lados.
    /// Por isso, no analógico, a virada só vale quando o x domina o y e se mantém por um instante:
    /// empurrão firme (≥ <see cref="StrongX"/>) por <see cref="StrongHold"/>, empurrão leve por <see cref="WeakHold"/>.
    /// </summary>
    public sealed class FacingFilter
    {
        /// <summary>A partir daqui o empurrão é firme.</summary>
        public const float StrongX = 0.5f;
        /// <summary>Abaixo disto não conta (mesma zona morta do <see cref="PlayerInputReader"/>).</summary>
        public const float WeakX = 0.2f;
        /// <summary>Tempo segurando um empurrão firme para virar. O retorno da mola dura menos que isto.</summary>
        public const float StrongHold = 0.04f;
        /// <summary>Tempo segurando um empurrão leve para virar.</summary>
        public const float WeakHold = 0.15f;

        int _candidate;
        float _held;

        /// <summary>Lado pedido neste quadro: −1, 1 ou 0 (nenhum pedido; o gato continua como está).</summary>
        public int Step(Vector2 move, bool analog, float dt)
        {
            float x = move.x, ax = Mathf.Abs(x);
            if (!analog)
            {
                _candidate = 0;
                _held = 0f;
                return x > 0.01f ? 1 : x < -0.01f ? -1 : 0;
            }

            // o x precisa dominar: empurrar para baixo com um pouco de diagonal não vira o gato
            int raw = ax >= WeakX && ax >= Mathf.Abs(move.y) ? (x > 0f ? 1 : -1) : 0;
            if (raw == 0) { _candidate = 0; _held = 0f; return 0; }
            if (raw != _candidate) { _candidate = raw; _held = 0f; }
            _held += dt;
            float need = ax >= StrongX ? StrongHold : WeakHold;
            return _held >= need ? raw : 0;
        }

        public void Reset() { _candidate = 0; _held = 0f; }
    }
}
