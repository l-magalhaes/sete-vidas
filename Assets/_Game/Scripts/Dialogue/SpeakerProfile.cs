using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Dialogue
{
    /// <summary>Quem fala: nome, retratos (um por expressão), lado da tela e "voz" (bipe das letras).</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Dialogue/Speaker Profile", fileName = "Speaker")]
    public class SpeakerProfile : ScriptableObject
    {
        public enum ScreenSide { Left, Right }

        [Serializable]
        public struct Expression
        {
            [Tooltip("Nome usado nas falas, ex.: \"ironico\"")]
            public string Key;
            public Sprite Portrait;
        }

        public string DisplayName = "Gato";
        [Tooltip("Retrato padrão 48×48")]
        public Sprite Portrait;
        [Tooltip("Outras expressões (opcional)")]
        public List<Expression> Expressions = new List<Expression>();
        [Tooltip("O gato fica à esquerda; os NPCs, à direita")]
        public ScreenSide Side = ScreenSide.Right;
        public Color NameColor = new Color32(0xf2, 0xe6, 0xc8, 0xff);
        [Tooltip("Altura do bipe das letras (1 = normal; maior = mais agudo)")]
        [Range(0.3f, 2.5f)] public float VoicePitch = 1f;
        [Tooltip("Banco de murmúrios (vozes sem palavras): lion, koda, capivara. Vazio = descobre pelo nome")]
        public string VoiceId = "";

        /// <summary>Retrato da expressão pedida; sem ela (ou vazia), o padrão.</summary>
        public Sprite PortraitFor(string expression)
        {
            if (!string.IsNullOrEmpty(expression))
                foreach (var e in Expressions)
                    if (e.Key == expression && e.Portrait != null) return e.Portrait;
            return Portrait;
        }
    }
}
