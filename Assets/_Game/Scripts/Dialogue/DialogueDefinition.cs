using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Dialogue
{
    /// <summary>Uma fala: quem diz, com que expressão e o texto. Sem falante = linha de sistema/narração.</summary>
    [Serializable]
    public struct DialogueLine
    {
        [Tooltip("Vazio = linha de sistema (sem retrato nem nome)")]
        public SpeakerProfile Speaker;
        [Tooltip("Chave da expressão do retrato (vazio = padrão)")]
        public string Expression;
        [TextArea(2, 3)] public string Text;
    }

    /// <summary>
    /// Uma conversa, guardada como dado (não no código) para revisão e tradução futura.
    /// Regras da skill de narrativa: até ~90 caracteres por caixa e até 6 caixas por conversa obrigatória.
    /// O texto de referência fica em Docs/Narrativa/dialogos-mvp.md.
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Dialogue/Dialogue", fileName = "Dialogue")]
    public class DialogueDefinition : ScriptableObject
    {
        public const int MaxCharsPerLine = 90;
        public const int MaxLinesRequired = 6;

        [Tooltip("ID do roteiro, ex.: D-GOLDEN-01")]
        public string ScriptId = "D-";
        [Tooltip("Conversa obrigatória (limite de 6 caixas)")]
        public bool Required = true;
        [Tooltip("Versão do texto. O setup só reescreve um asset existente se a revisão do código for maior")]
        public int Revision = 1;
        public List<DialogueLine> Lines = new List<DialogueLine>();

        /// <summary>Problemas de tamanho (vazio = tudo certo). Usado pelos testes.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (Lines.Count == 0) problems.Add($"{ScriptId}: sem falas");
            if (Required && Lines.Count > MaxLinesRequired)
                problems.Add($"{ScriptId}: {Lines.Count} caixas (máximo {MaxLinesRequired} numa conversa obrigatória)");
            for (int i = 0; i < Lines.Count; i++)
            {
                int len = Lines[i].Text?.Length ?? 0;
                if (len == 0) problems.Add($"{ScriptId} caixa {i + 1}: texto vazio");
                else if (len > MaxCharsPerLine) problems.Add($"{ScriptId} caixa {i + 1}: {len} caracteres (máximo {MaxCharsPerLine})");
            }
            return problems;
        }
    }
}
