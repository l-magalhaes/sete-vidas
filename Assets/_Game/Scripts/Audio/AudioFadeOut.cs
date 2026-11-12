using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>Abaixa o AudioSource até o silêncio e destrói o objeto (música que atravessa a troca de cena).</summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioFadeOut : MonoBehaviour
    {
        AudioSource _source;
        float _speed = 1f;

        public void Begin(float seconds)
        {
            _source = GetComponent<AudioSource>();
            _speed = _source.volume / Mathf.Max(0.05f, seconds);
        }

        void Update()
        {
            if (_source == null) { Destroy(gameObject); return; }
            _source.volume = Mathf.MoveTowards(_source.volume, 0f, _speed * Time.unscaledDeltaTime);
            if (_source.volume <= 0f) Destroy(gameObject);
        }
    }
}
