using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Animações do Contratante (Capivara Necromante) na cena do tomo: surge na névoa, debocha, pega o tomo com a
    /// mão de osso, joga a moeda e some. A cena (<see cref="CursedTome"/>) pede cada uma; animações sem loop voltam
    /// para a seguinte quando terminam. Arte: Tools/CharacterSprites (ContractorAnimationSet).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ContractorVisual : MonoBehaviour
    {
        [SerializeField] CharacterAnimationSet animations;

        SpriteRenderer _renderer;
        SpriteClip _clip;
        string _next;
        float _time;

        public string CurrentClipName => _clip != null ? _clip.name : "";
        public bool HasAnimations => animations != null;

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        void OnEnable()
        {
            if (_clip == null) Play("idle");
        }

        /// <summary>Toca <paramref name="clipName"/>; se não tiver loop, passa para <paramref name="then"/> ao terminar.</summary>
        public void Play(string clipName, string then = null)
        {
            if (animations == null) return;
            var clip = animations.Get(clipName);
            if (clip == null) return;
            _clip = clip;
            _next = then;
            _time = 0f;
            Apply();
        }

        /// <summary>Duração da animação (s), 0 se não existir.</summary>
        public float Duration(string clipName)
        {
            var clip = animations != null ? animations.Get(clipName) : null;
            return clip != null ? clip.Duration : 0f;
        }

        void Update()
        {
            if (_clip == null) return;
            _time += Time.deltaTime;
            if (!_clip.loop && _time >= _clip.Duration && !string.IsNullOrEmpty(_next))
            {
                string next = _next;
                Play(next);
                return;
            }
            Apply();
        }

        void Apply()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            var frame = _clip.At(_time);
            if (frame != null && frame.sprite != null) _renderer.sprite = frame.sprite;
        }
    }
}
