using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>
    /// Confirmação da troca de classe: nome da classe na cor dela e um "plim" curto gerado por código
    /// (fallback: os SFX de Resources/Audio/SFX, Docs/Audio/plano-sfx.md). O brilho no sprite fica no PlayerVisual.
    /// </summary>
    [RequireComponent(typeof(ClassManager))]
    public class ClassSwitchFeedback : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float volume = 0.25f;

        ClassManager _classes;
        AudioSource _audio;
        AudioClip _switchClip, _unlockClip;

        void Awake()
        {
            _classes = GetComponent<ClassManager>();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _switchClip = Chirp("troca_classe", 520f, 880f, 0.12f);
            _unlockClip = Chirp("classe_desbloqueada", 440f, 1320f, 0.35f);
        }

        void OnEnable()
        {
            _classes.ClassChanged += OnChanged;
            _classes.ClassUnlocked += OnUnlocked;
        }

        void OnDisable()
        {
            _classes.ClassChanged -= OnChanged;
            _classes.ClassUnlocked -= OnUnlocked;
        }

        void OnChanged(ClassDefinition previous, ClassDefinition next)
        {
            if (next == null) return;
            PixelPopup.Spawn(transform.position + Vector3.up * 2f, next.DisplayName, next.ThemeColor);
            string id = next.Class == CharacterClass.Paladin ? "classe_paladino" : "classe_ladino"; // as outras classes: VS
            if (SeteVidas.Audio.Sfx.Has(id)) SeteVidas.Audio.Sfx.Play(id);
            else _audio.PlayOneShot(_switchClip, volume);
        }

        void OnUnlocked(ClassDefinition def)
        {
            if (SeteVidas.Audio.Sfx.Has("classe_desbloqueada")) SeteVidas.Audio.Sfx.Play("classe_desbloqueada");
            else _audio.PlayOneShot(_unlockClip, volume);
        }

        /// <summary>Varredura de frequência curta com fade: um "plim" simples.</summary>
        static AudioClip Chirp(string name, float fromHz, float toHz, float seconds)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * seconds);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += 2f * Mathf.PI * Mathf.Lerp(fromHz, toHz, t) / rate;
                float envelope = Mathf.Min(1f, t * 20f) * (1f - t);
                data[i] = Mathf.Sin(phase) * envelope * 0.6f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
