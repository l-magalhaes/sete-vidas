using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Anima um enfeite em loop (cachoeira, lagoa). Todos os SpriteRenderer filhos trocam de frame juntos, então uma
    /// coluna de pedaços empilhados anda como uma peça só.
    /// </summary>
    public class SpriteLoop : MonoBehaviour
    {
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.02f)] float frameTime = 0.1f;

        SpriteRenderer[] _renderers;
        int _shown = -1;

        public void Configure(Sprite[] newFrames, float newFrameTime)
        {
            frames = newFrames; frameTime = newFrameTime;
        }

        void Awake() => _renderers = GetComponentsInChildren<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0 || _renderers == null) return;
            int i = (int)(Time.time / frameTime) % frames.Length;
            if (i == _shown) return;
            _shown = i;
            foreach (var r in _renderers) if (r != null) r.sprite = frames[i];
        }
    }
}
