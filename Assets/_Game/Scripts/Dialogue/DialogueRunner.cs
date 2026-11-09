using System;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.Dialogue
{
    /// <summary>
    /// Interface de diálogo (spec: Docs/Design/dialogo.md). Caixa embaixo, placa com o nome e o retrato de quem
    /// fala: o gato à esquerda, NPCs à direita, alternando a cada fala. O texto aparece letra por letra com um
    /// bipe na "voz" de cada um. E/Espaço/clique: completa a fala; de novo: próxima. O jogo pausa enquanto a
    /// conversa está aberta e o controle do gato fica bloqueado.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [Header("Partes da interface")]
        [SerializeField] GameObject root;
        [SerializeField] RectTransform box;
        [SerializeField] Text body;
        [SerializeField] RectTransform nameplate;
        [SerializeField] Text nameText;
        [SerializeField] RectTransform portraitFrame;
        [SerializeField] Image portrait;
        [SerializeField] Image continueArrow;

        [Header("Ritmo")]
        [Tooltip("Letras por segundo")]
        [SerializeField] float charsPerSecond = 45f;
        [Tooltip("Um bipe a cada N letras")]
        [SerializeField, Min(1)] int blipEvery = 2;
        [SerializeField, Range(0f, 1f)] float blipVolume = 0.12f;

        [Header("Layout (unidades da tela 480×270)")]
        [SerializeField] float textPadding = 10f;
        [SerializeField] float portraitGap = 8f;

        readonly TypewriterText _typer = new TypewriterText();
        DialogueDefinition _dialogue;
        PlayerInputReader _input;
        Action _onComplete;
        int _index;
        int _openedFrame;
        int _closedFrame = -10;
        bool _unlockPending;
        float _savedTimeScale = 1f;
        AudioSource _audio;
        AudioClip _blip;
        Color _bodyColor;

        public static DialogueRunner Active { get; private set; }
        public bool IsOpen { get; private set; }
        public DialogueLine CurrentLine => _dialogue != null && _index < _dialogue.Lines.Count ? _dialogue.Lines[_index] : default;
        public int LineIndex => _index;
        public bool LineComplete => _typer.IsComplete;

        public event Action<DialogueDefinition> Opened;
        public event Action<DialogueDefinition> Closed;

        void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;
            _blip = MakeBlip();
            if (body != null) _bodyColor = body.color;
            if (root != null) root.SetActive(false);
        }

        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        /// <summary>
        /// Abre a conversa. <paramref name="input"/> é o controle do gato (fica bloqueado até o fim).
        /// <paramref name="onComplete"/> roda ao fechar; pode abrir outra conversa em seguida.
        /// </summary>
        public bool Play(DialogueDefinition dialogue, PlayerInputReader input, Action onComplete = null)
        {
            if (dialogue == null || dialogue.Lines.Count == 0 || IsOpen) return false;

            _dialogue = dialogue;
            _input = input;
            _onComplete = onComplete;
            _index = 0;
            _openedFrame = Time.frameCount;
            _unlockPending = false;
            if (_input != null) _input.SetLock(this, true);
            if (!IsOpen) _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            IsOpen = true;
            if (root != null) root.SetActive(true);
            ShowLine();
            Opened?.Invoke(dialogue);
            return true;
        }

        /// <summary>Um aperto do jogador: completa a fala ou passa para a próxima.</summary>
        public void Advance()
        {
            if (!IsOpen) return;
            if (!_typer.IsComplete)
            {
                _typer.Complete();
                Refresh();
                return;
            }
            _index++;
            if (_index >= _dialogue.Lines.Count) Close();
            else ShowLine();
        }

        void Update()
        {
            if (!IsOpen)
            {
                // Libera o controle só no frame seguinte ao fechamento: o E que fechou não reabre a conversa
                if (_unlockPending && Time.frameCount > _closedFrame)
                {
                    if (_input != null) _input.SetLock(this, false);
                    _unlockPending = false;
                }
                return;
            }

            Time.timeScale = 0f; // o hitstop pode ter devolvido o tempo: a conversa mantém o jogo parado

            int newChars = _typer.Tick(Time.unscaledDeltaTime, charsPerSecond);
            if (newChars > 0)
            {
                Refresh();
                char last = _typer.Text[_typer.Visible - 1];
                if (!char.IsWhiteSpace(last) && _typer.Visible % blipEvery == 0) PlayBlip();
            }
            if (continueArrow != null)
                continueArrow.enabled = _typer.IsComplete && ((int)(Time.unscaledTime * 2.5f) & 1) == 0;

            // O aperto que abriu a conversa não conta como "avançar"
            if (Time.frameCount != _openedFrame && _input != null && _input.AdvanceDialoguePressedThisFrame)
                Advance();
        }

        void Close()
        {
            IsOpen = false;
            if (root != null) root.SetActive(false);
            Time.timeScale = _savedTimeScale;
            _closedFrame = Time.frameCount;
            _unlockPending = true;

            var finished = _dialogue;
            var callback = _onComplete;
            _onComplete = null;
            Closed?.Invoke(finished);
            callback?.Invoke(); // pode abrir outra conversa (Play cancela o desbloqueio pendente)
        }

        // ------------------------------------------------------------ layout

        void ShowLine()
        {
            var line = _dialogue.Lines[_index];
            _typer.Start(line.Text);
            bool hasSpeaker = line.Speaker != null;
            bool left = hasSpeaker && line.Speaker.Side == SpeakerProfile.ScreenSide.Left;
            var face = hasSpeaker ? line.Speaker.PortraitFor(line.Expression) : null;

            // Retrato do lado de quem fala
            if (portraitFrame != null)
            {
                portraitFrame.gameObject.SetActive(face != null);
                var anchor = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
                portraitFrame.anchorMin = portraitFrame.anchorMax = portraitFrame.pivot = anchor;
                portraitFrame.anchoredPosition = new Vector2(left ? 6f : -6f, 6f);
            }
            if (portrait != null) portrait.sprite = face;

            // Placa do nome: em cima da caixa, do lado de quem fala (depois do retrato, se ele estiver lá)
            if (nameplate != null)
            {
                nameplate.gameObject.SetActive(hasSpeaker);
                float portraitWidth = face != null && portraitFrame != null ? portraitFrame.sizeDelta.x + portraitGap : 0f;
                var anchor = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                nameplate.anchorMin = nameplate.anchorMax = anchor;
                nameplate.pivot = new Vector2(left ? 0f : 1f, 0f);
                nameplate.anchoredPosition = new Vector2(left ? 6f + portraitWidth : -(6f + portraitWidth), -2f);
                if (nameText != null && hasSpeaker)
                {
                    nameText.text = line.Speaker.DisplayName;
                    nameText.color = line.Speaker.NameColor;
                }
            }

            // Texto: abre espaço para o retrato do lado certo; linha de sistema usa a largura toda, em itálico
            if (body != null)
            {
                float reserve = face != null && portraitFrame != null ? portraitFrame.sizeDelta.x + portraitGap + 6f : 0f;
                var rt = body.rectTransform;
                rt.offsetMin = new Vector2(textPadding + (left ? reserve : 0f), 8f);
                rt.offsetMax = new Vector2(-(textPadding + (left ? 0f : reserve)), -10f);
                body.fontStyle = hasSpeaker ? FontStyle.Normal : FontStyle.Italic;
                body.alignment = hasSpeaker ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
                body.color = hasSpeaker ? _bodyColor : new Color(_bodyColor.r, _bodyColor.g, _bodyColor.b, 0.8f);
            }

            if (_audio != null) _audio.pitch = hasSpeaker ? line.Speaker.VoicePitch : 1f;
            Refresh();
        }

        void Refresh()
        {
            if (body != null) body.text = _typer.VisibleText;
            if (continueArrow != null && !_typer.IsComplete) continueArrow.enabled = false;
        }

        void PlayBlip()
        {
            if (_audio != null && _blip != null && CurrentLine.Speaker != null) _audio.PlayOneShot(_blip, blipVolume);
        }

        /// <summary>Bipe curto de onda quadrada (placeholder até existirem vozes de verdade).</summary>
        static AudioClip MakeBlip()
        {
            const int rate = 44100;
            const float seconds = 0.035f, hz = 660f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = 1f - i / (float)n;
                data[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) >= 0f ? 0.5f : -0.5f) * env;
            }
            var clip = AudioClip.Create("dialogo_bipe", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
