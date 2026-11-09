namespace SeteVidas.Player
{
    /// <summary>
    /// Coyote time e jump buffer. C# puro para ser testado em EditMode.
    /// O pulo acontece quando os dois timers estão ativos ao mesmo tempo.
    /// </summary>
    public sealed class JumpTimers
    {
        float _coyote;
        float _buffer;

        /// <summary>Há um pulo apertado recentemente e o gato está (ou acabou de estar) no chão.</summary>
        public bool CanJump => _coyote > 0f && _buffer > 0f;

        /// <summary>Há um pulo apertado recentemente, ainda não usado (serve para subir da borda).</summary>
        public bool HasBufferedJump => _buffer > 0f;

        /// <summary>O gato está no chão ou saiu dele há menos que o coyote time.</summary>
        public bool InCoyoteWindow => _coyote > 0f;

        /// <summary>Chame a cada passo de física em que o gato está no chão.</summary>
        public void OnGrounded(float coyoteTime) => _coyote = coyoteTime;

        /// <summary>Chame no frame em que o botão de pulo foi apertado.</summary>
        public void OnJumpPressed(float bufferTime) => _buffer = bufferTime;

        /// <summary>Avança o tempo. Chame uma vez por passo de física.</summary>
        public void Tick(float deltaTime)
        {
            _coyote -= deltaTime;
            _buffer -= deltaTime;
        }

        /// <summary>Gasta o pulo: zera os dois timers para não pular duas vezes.</summary>
        public void Consume()
        {
            _coyote = 0f;
            _buffer = 0f;
        }
    }
}
