using SeteVidas.Audio;
using SeteVidas.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Prólogo do Novo jogo: o gato lê o contrato num pergaminho, página por página, e comenta.
    /// Páginas sem falante = texto do contrato; com falante = o gato pensando alto (retrato e nome).
    /// A página com a expressão "selo" mostra o lacre de cera do Contratante. Ao fim, abre a Fortaleza.
    /// E/Enter/Espaço/clique: completa a página ou avança. Esc (ou Start): pula o prólogo.
    /// </summary>
    public class PrologueController : MonoBehaviour
    {
        [SerializeField] DialogueDefinition contract;
        [Tooltip("Cena aberta depois do contrato")]
        [SerializeField] string nextScene = "Fortress_MVP";
        [SerializeField] float charsPerSecond = 55f;

        [Header("Música")]
        [Tooltip("Trilha de fundo da leitura (bem baixinha)")]
        [SerializeField] AudioClip music;
        [Tooltip("Volume da faixa (a mixagem geral AudioMix.MusicLevel ainda se aplica por cima)")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.14f;
        [SerializeField, Min(0f)] float musicFadeIn = 2.5f;
        [Tooltip("Ao sair, a música some aos poucos enquanto a Fortaleza começa")]
        [SerializeField, Min(0f)] float musicFadeOut = 1.8f;

        AudioSource _music;

        [Header("Interface")]
        [SerializeField] Text title;
        [SerializeField] Text body;
        [SerializeField] Text hint;
        [SerializeField] GameObject speakerBox;
        [SerializeField] Image portrait;
        [SerializeField] Text speakerName;
        [SerializeField] GameObject seal;

        readonly TypewriterText _typer = new TypewriterText();
        PromptRow _hintRow;
        int _index = -1;
        int _baseFontSize;
        bool _finished;

        public int Page => _index;
        public int PageCount => contract != null ? contract.Lines.Count : 0;
        public bool PageComplete => _typer.IsComplete;

        void Start()
        {
            Time.timeScale = 1f;
            if (body != null) _baseFontSize = body.fontSize;
            // "continuar · pular" com os botões do aparelho em uso (no lugar do texto "E / Enter: continuar · Esc: pular")
            if (hint != null)
            {
                _hintRow = PromptRow.Create(hint.rectTransform, "Botões", hint.font, hint.fontSize, hint.color,
                    new Vector2(0f, 0f), hint.rectTransform.rect.width);
                _hintRow.Set(new PromptRow.Item(GameAction.Confirm, "continuar"), new PromptRow.Item(GameAction.Skip, "pular"));
                hint.text = "";
            }
            StartMusic();
            if (contract == null || contract.Lines.Count == 0) { Finish(); return; }
            Show(0);
        }

        void Update()
        {
            if (_finished) return;
            if (_music != null)
                _music.volume = Mathf.MoveTowards(_music.volume, MusicTarget, Time.unscaledDeltaTime * MusicTarget / Mathf.Max(0.05f, musicFadeIn));
            if (SkipPressed()) { Finish(); return; }

            if (_typer.Tick(Time.unscaledDeltaTime, charsPerSecond) > 0 && body != null) body.text = _typer.VisibleText;
            if (_hintRow != null) _hintRow.gameObject.SetActive(_typer.IsComplete);
            if (AdvancePressed()) Advance();
        }

        /// <summary>Completa a página ou passa para a próxima; depois da última, abre a Fortaleza.</summary>
        public void Advance()
        {
            if (_finished) return;
            if (!_typer.IsComplete)
            {
                _typer.Complete();
                if (body != null) body.text = _typer.VisibleText;
                return;
            }
            if (_index + 1 >= contract.Lines.Count) Finish();
            else Show(_index + 1);
        }

        void Show(int i)
        {
            _index = i;
            var line = contract.Lines[i];
            bool spoken = line.Speaker != null;
            _typer.Start(line.Text);
            if (body != null)
            {
                body.text = "";
                body.fontStyle = spoken ? FontStyle.Normal : FontStyle.Italic;
                body.alignment = spoken ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                // Letra miúda: menor, de propósito (dá para ler, mas passa fácil)
                body.fontSize = line.Expression == "miuda" ? Mathf.Max(6, Mathf.RoundToInt(_baseFontSize * 0.75f)) : _baseFontSize;
            }
            if (title != null) title.enabled = !spoken;
            if (speakerBox != null) speakerBox.SetActive(spoken);
            if (spoken)
            {
                if (portrait != null) portrait.sprite = line.Speaker.PortraitFor(line.Expression);
                if (speakerName != null) speakerName.text = line.Speaker.DisplayName;
            }
            if (seal != null) seal.SetActive(line.Expression == "selo");
            // o gato pensando alto: o mesmo murmúrio sem palavras do diálogo (a expressão escolhe a emoção)
            if (spoken)
            {
                string voice = DialogueRunner.VoiceClipId(line.Speaker, line.Expression, line.Text, Sfx.Has);
                if (voice != null) Sfx.Play(voice);
            }
        }

        // ------------------------------------------------------------ música

        float MusicTarget => musicVolume * AudioMix.MusicLevel * PlayerPrefs.GetFloat(MusicDirector.VolumeKey, 1f);

        void StartMusic()
        {
            if (music == null) return;
            var go = new GameObject("Música do prólogo");
            _music = go.AddComponent<AudioSource>();
            _music.clip = music;
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.volume = 0f;
            _music.Play();
        }

        /// <summary>A música atravessa a troca de cena e some aos poucos (a transição para a Fortaleza).</summary>
        void ReleaseMusic()
        {
            if (_music == null) return;
            DontDestroyOnLoad(_music.gameObject);
            _music.gameObject.AddComponent<AudioFadeOut>().Begin(musicFadeOut);
            _music = null;
        }

        /// <summary>Sai do prólogo e começa a Fortaleza (também usado pelos testes e pelo Esc).</summary>
        public void Finish()
        {
            if (_finished) return;
            _finished = true;
            ReleaseMusic();
            SceneManager.LoadScene(nextScene);
        }

        static bool AdvancePressed()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) ||
                   (m != null && m.leftButton.wasPressedThisFrame) ||
                   (g != null && g.buttonSouth.wasPressedThisFrame);
        }

        static bool SkipPressed()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
        }
    }
}
