using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Mostra os desenhos deste objeto (e dos filhos) só com uma flag no save, ou só sem ela (<see cref="hideWithFlag"/>).
    /// Ex.: a corda que desce ao vale só aparece depois da ponte reconstruída, que é quando ela funciona.
    /// </summary>
    public class FlagVisible : MonoBehaviour
    {
        [SerializeField] string flag = "";
        [Tooltip("Ao contrário: some quando a flag existe")]
        [SerializeField] bool hideWithFlag;

        SpriteRenderer[] _renderers;
        float _next;

        public void Configure(string requiredFlag, bool invert = false) { flag = requiredFlag; hideWithFlag = invert; }

        void Awake() => _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        void Start() => Apply();

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 0.5f;   // a flag pode mudar no meio da cena (a ponte reconstruída)
            Apply();
        }

        void Apply()
        {
            bool has = !string.IsNullOrEmpty(flag) && SaveManager.Data != null && SaveManager.Data.HasFlag(flag);
            bool show = hideWithFlag ? !has : has;
            foreach (var r in _renderers) if (r != null) r.enabled = show;
        }
    }
}
