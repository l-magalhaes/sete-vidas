using SeteVidas.Dialogue;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Vulpis desenhada (Tools/CharacterSprites/VulpisSpriteGen.cs, VulpisAnimationSet): toca as animações e, numa
    /// conversa em que ela fala, mexe a boca (e fica triste ou sorri pela expressão da fala).
    /// Anda no tempo real: continua viva com o jogo pausado (o diálogo e a cena da flecha forte pausam o jogo).
    /// Clipes: idle, talk, sad, smile, run, shoot, crouch, leap, air_draw, air_release, fall, land.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class VulpisVisual : MonoBehaviour
    {
        public const string SpeakerName = "Vulpis";

        [SerializeField] CharacterAnimationSet animations;
        [Tooltip("Olhando para a esquerda (o desenho olha para a direita)")]
        [SerializeField] bool faceLeft = true;
        [Tooltip("Numa conversa em que ela fala, mexe a boca")]
        [SerializeField] bool followDialogue = true;
        [Tooltip("O nome de quem fala nas conversas (o mesmo componente anima a Vulpis e a Tobi)")]
        [SerializeField] string speaker = SpeakerName;

        SpriteRenderer _renderer;
        SpriteClip _clip;
        string _next;
        float _time;
        bool _scripted;


        public bool HasAnimations => animations != null;
        public string CurrentClipName => _clip != null ? _clip.name : "";
        public SpriteRenderer Renderer { get { if (_renderer == null) _renderer = GetComponent<SpriteRenderer>(); return _renderer; } }

        public bool FaceLeft
        {
            get => faceLeft;
            set { faceLeft = value; Renderer.flipX = value; }
        }

        public void Configure(CharacterAnimationSet set, bool left, string speakerName = SpeakerName)
        {
            animations = set;
            speaker = speakerName;
            faceLeft = left;
            Renderer.flipX = left;
            _clip = null;
            Play("idle");
        }

        void Awake() { _renderer = GetComponent<SpriteRenderer>(); _renderer.flipX = faceLeft; }

        void OnEnable() { if (_clip == null) Play("idle"); }

        /// <summary>
        /// Toca <paramref name="clipName"/>; sem loop, passa para <paramref name="then"/> ao terminar (sem
        /// <paramref name="then"/>, segura o último quadro). Uma cena que chama isto manda nela até <see cref="Release"/>.
        /// </summary>
        public void Play(string clipName, string then = null)
        {
            _scripted = true;
            PlayInternal(clipName, then);
        }

        /// <summary>Devolve a Vulpis para o idle (e para as conversas).</summary>
        public void Release() { _scripted = false; PlayInternal("idle", null); }

        void PlayInternal(string clipName, string then)
        {
            if (animations == null) return;
            var clip = animations.Get(clipName);
            if (clip == null) return;
            if (_clip == clip && clip.loop) { _next = then; return; }
            _clip = clip;
            _next = then;
            _time = 0f;
            Apply();
        }

        public float Duration(string clipName)
        {
            var clip = animations != null ? animations.Get(clipName) : null;
            return clip != null ? clip.Duration : 0f;
        }

        void Update()
        {
            if (!_scripted && followDialogue) FollowDialogue();
            if (_clip == null) return;
            _time += Time.unscaledDeltaTime;
            if (!_clip.loop && _time >= _clip.Duration && !string.IsNullOrEmpty(_next))
            {
                PlayInternal(_next, null);
                return;
            }
            Apply();
        }

        /// <summary>Na conversa: fala quando a linha é dela (triste e sorrindo pela expressão); senão, fica parada.</summary>
        void FollowDialogue()
        {
            var runner = DialogueRunner.Active;
            string want = "idle";
            if (runner != null && runner.IsOpen)
            {
                var line = runner.CurrentLine;
                if (line.Speaker != null && line.Speaker.DisplayName == speaker)
                {
                    want = ClipFor(line.Expression);
                    if (animations == null || animations.Get(want) == null) want = "talk";
                }
            }
            if (CurrentClipName != want) PlayInternal(want, null);
        }

        /// <summary>A animação pela expressão da fala (a da Vulpis e a da Tobi); sem uma própria, fala ("talk").</summary>
        public static string ClipFor(string expression)
        {
            switch (expression)
            {
                case "triste": return "sad";
                case "sorriso": return "smile";
                case "ofendida": return "offended";
                case "gananciosa": return "greedy";
                case "orgulhosa": return "proud";
                default: return "talk";
            }
        }

        void Apply()
        {
            var frame = _clip.At(_time);
            if (frame != null && frame.sprite != null) Renderer.sprite = frame.sprite;
            Renderer.flipX = faceLeft;
        }
    }
}
