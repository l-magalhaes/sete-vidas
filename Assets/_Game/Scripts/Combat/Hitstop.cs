using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Congela o jogo (Time.timeScale = 0) por um instante no impacto. Golpes seguidos estendem o
    /// congelamento em vez de somar. Restaura o timeScale anterior ao terminar.
    /// </summary>
    public class Hitstop : MonoBehaviour
    {
        static Hitstop s_instance;

        float _until;
        float _savedScale = 1f;
        bool _active;

        public static bool IsActive => s_instance != null && s_instance._active;

        public static void Trigger(float seconds)
        {
            if (seconds <= 0f || !Application.isPlaying) return;
            if (s_instance == null)
            {
                var go = new GameObject("[Hitstop]") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                s_instance = go.AddComponent<Hitstop>();
            }
            s_instance.Begin(seconds);
        }

        void Begin(float seconds)
        {
            if (!_active)
            {
                _savedScale = Time.timeScale;
                _active = true;
            }
            _until = Mathf.Max(_until, Time.unscaledTime + seconds);
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (_active && Time.unscaledTime >= _until) End();
        }

        void End()
        {
            _active = false;
            Time.timeScale = _savedScale;
        }

        void OnDestroy()
        {
            if (_active) End();
            if (s_instance == this) s_instance = null;
        }
    }
}
