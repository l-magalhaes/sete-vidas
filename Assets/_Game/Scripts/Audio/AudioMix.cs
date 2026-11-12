using SeteVidas.Dialogue;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>
    /// A mixagem geral (Docs/Audio/plano-sfx.md §13): a trilha fica de fundo, bem baixa, como mais uma camada do
    /// ambiente; os SFX contam a ação. Quando algo importante acontece (impacto forte, dano no gato, alerta,
    /// relâmpago), a música abaixa um instante (ducking) e volta devagar. No diálogo ela também fica mais baixa.
    /// </summary>
    public static class AudioMix
    {
        /// <summary>Ganho geral da trilha sobre o volume de cada faixa (a trilha é fundo, não protagonista).</summary>
        public const float MusicLevel = 0.5f;
        /// <summary>Quanto a música fica durante uma conversa.</summary>
        public const float DialogueMusic = 0.55f;
        /// <summary>Tempo para a música voltar depois de um ducking (s).</summary>
        public const float DuckRelease = 0.9f;

        static float s_depth = 1f, s_until = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_depth = 1f; s_until = -99f; } // Play sem recarregar o domínio

        /// <summary>Abaixa a música para <paramref name="depth"/> (0 a 1) por <paramref name="hold"/> s. Vale o mais fundo.</summary>
        public static void Duck(float depth, float hold)
        {
            float now = Time.unscaledTime;
            float current = DuckAt(now, s_depth, s_until, DuckRelease);
            if (depth > current) return; // já está mais baixa por algo mais importante
            s_depth = depth;
            s_until = Mathf.Max(now + hold, depth == current ? s_until : 0f);
        }

        /// <summary>O ganho do ducking num instante: <paramref name="depth"/> até <paramref name="until"/>, depois volta a 1. Regra pura.</summary>
        public static float DuckAt(float now, float depth, float until, float release)
        {
            if (now < until) return depth;
            if (release <= 0f) return 1f;
            return Mathf.Lerp(depth, 1f, Mathf.Clamp01((now - until) / release));
        }

        /// <summary>O ganho que a trilha deve ter agora (nível de fundo × ducking × diálogo).</summary>
        public static float MusicGain
        {
            get
            {
                float g = MusicLevel * DuckAt(Time.unscaledTime, s_depth, s_until, DuckRelease);
                var talk = DialogueRunner.Active;
                if (talk != null && talk.IsOpen) g *= DialogueMusic;
                return g;
            }
        }
    }
}
