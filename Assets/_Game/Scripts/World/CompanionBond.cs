using System;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Vínculo placeholder com um companheiro (Fase 5: Golden). Perto dele, E conversa. Na primeira vez, a
    /// conversa de vínculo vai caixa a caixa (E avança); ao terminar, recupera um coração, desbloqueia a
    /// classe e grava o save. Depois, cada E mostra uma fala curta. Afastar-se encerra a conversa.
    /// Textos: Docs/Narrativa/dialogos-mvp.md.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CompanionBond : MonoBehaviour
    {
        [Serializable]
        public struct Line
        {
            public string Speaker;
            [TextArea] public string Text;
        }

        [SerializeField] string companionName = "Golden";
        [SerializeField] CharacterClass grantsClass = CharacterClass.Paladin;
        [Tooltip("Flag do save que marca o vínculo feito")]
        [SerializeField] string bondFlag = "vinculo_golden";
        [Tooltip("Conversa de vínculo (D-GOLDEN-01)")]
        [SerializeField] Line[] bondLines = new Line[0];
        [Tooltip("Mostrada logo depois do vínculo")]
        [SerializeField] string unlockHint = "Paladino desbloqueado! Aperte 2 (ou Q/R) para trocar.";
        [Tooltip("Falas seguintes, em rodízio (D-GOLDEN-02)")]
        [SerializeField] string[] idleLines = new string[0];

        [Header("Visual")]
        [Tooltip("\"E: falar\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;
        [Tooltip("Balão de fala")]
        [SerializeField] TextMesh speech;
        [Tooltip("Segundos que uma fala solta fica na tela")]
        [SerializeField] float idleLineSeconds = 3.5f;

        PlayerHealth _playerInRange;
        int _lineIndex = -1;      // -1 = sem conversa de vínculo em andamento
        int _idleIndex;
        float _hideSpeechAt;

        public bool IsBonded => SaveManager.Data.HasFlag(bondFlag);
        public bool IsTalking => _lineIndex >= 0;

        /// <summary>O vínculo terminou (classe e coração concedidos).</summary>
        public event Action Bonded;

        void Start() => ShowSpeech(null);

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) _playerInRange = health;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health != _playerInRange) return;
            _playerInRange = null;
            _lineIndex = -1; // afastou: encerra a conversa (o vínculo só conta se chegar ao fim)
            ShowSpeech(null);
        }

        void Update()
        {
            bool near = _playerInRange != null && !_playerInRange.IsDead;
            if (prompt != null) prompt.gameObject.SetActive(near && !IsTalking);
            if (!IsTalking && speech != null && speech.gameObject.activeSelf && Time.time >= _hideSpeechAt) ShowSpeech(null);
            if (!near) return;

            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Interact(_playerInRange);
        }

        /// <summary>Um "E": começa, avança ou termina a conversa.</summary>
        public void Interact(PlayerHealth player)
        {
            if (player == null || player.IsDead) return;

            if (IsBonded && !IsTalking)
            {
                if (idleLines.Length == 0) return;
                ShowSpeech($"{companionName.ToUpperInvariant()}: {idleLines[_idleIndex % idleLines.Length]}");
                _idleIndex++;
                _hideSpeechAt = Time.time + idleLineSeconds;
                return;
            }

            _lineIndex++;
            if (_lineIndex < bondLines.Length)
            {
                var line = bondLines[_lineIndex];
                ShowSpeech($"{line.Speaker}: {line.Text}");
                return;
            }

            // Fim da conversa: vínculo
            _lineIndex = -1;
            CompleteBond(player);
        }

        /// <summary>Concede coração e classe, grava o save (também usado pelos testes).</summary>
        public void CompleteBond(PlayerHealth player)
        {
            if (IsBonded) return;
            var data = SaveManager.Data;
            data.SetFlag(bondFlag);
            var classes = player.GetComponent<ClassManager>();
            if (classes != null) classes.Unlock(grantsClass);
            else if (!data.IsUnlocked(grantsClass)) data.unlockedClasses.Add(grantsClass.ToString());
            player.AddHeart(); // +1 coração e grava o save

            ShowSpeech(unlockHint);
            _hideSpeechAt = Time.time + idleLineSeconds + 1.5f;
            DamagePopup.Spawn(player.transform.position + Vector3.up * 2.2f, "+1 coração", new Color32(0xff, 0x8a, 0x9a, 0xff));
            Bonded?.Invoke();
        }

        void ShowSpeech(string text)
        {
            if (speech == null) return;
            bool show = !string.IsNullOrEmpty(text);
            speech.gameObject.SetActive(show);
            if (show) speech.text = Wrap(text, 34);
        }

        /// <summary>Quebra o texto em linhas de até <paramref name="width"/> caracteres (balão estreito).</summary>
        static string Wrap(string text, int width)
        {
            var words = text.Split(' ');
            var sb = new System.Text.StringBuilder();
            int lineLength = 0;
            foreach (var w in words)
            {
                if (lineLength > 0 && lineLength + 1 + w.Length > width)
                {
                    sb.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    sb.Append(' ');
                    lineLength++;
                }
                sb.Append(w);
                lineLength += w.Length;
            }
            return sb.ToString();
        }
    }
}
