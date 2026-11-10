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
        [Tooltip("Atraso do ciclo (s): enfeites iguais lado a lado não animam em sincronia (ex.: gotas das gárgulas)")]
        [SerializeField, Min(0f)] float timeOffset;

        SpriteRenderer[] _renderers;
        int _shown = -1;

        public void Configure(Sprite[] newFrames, float newFrameTime) => Configure(newFrames, newFrameTime, 0f);

        public void Configure(Sprite[] newFrames, float newFrameTime, float newTimeOffset)
        {
            frames = newFrames; frameTime = newFrameTime; timeOffset = newTimeOffset;
        }

        void Awake() => _renderers = GetComponentsInChildren<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0 || _renderers == null) return;
            int i = (int)((Time.time + timeOffset) / frameTime) % frames.Length;
            if (i == _shown) return;
            _shown = i;
            foreach (var r in _renderers) if (r != null) r.sprite = frames[i];
        }
    }
}
