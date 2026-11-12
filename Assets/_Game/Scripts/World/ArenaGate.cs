using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Grade de ferro da arena. Fechada: sólida (layer Ground). Abrindo: as barras sobem para dentro da
    /// parede e a passagem libera. O visual fica atrás dos tiles, então some ao subir.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ArenaGate : MonoBehaviour
    {
        [SerializeField] bool startsClosed;
        [SerializeField] Transform bars;
        [Tooltip("Quanto as barras sobem ao abrir (tiles)")]
        [SerializeField] float openHeight = 4.5f;
        [Tooltip("Velocidade das barras (tiles/s)")]
        [SerializeField] float speed = 10f;

        BoxCollider2D _solid;
        Vector3 _closedPos;

        public bool IsClosed { get; private set; }

        void Awake()
        {
            _solid = GetComponent<BoxCollider2D>();
            if (bars != null) _closedPos = bars.localPosition;
            SetImmediate(startsClosed);
        }

        public void Close() { if (!IsClosed) SeteVidas.Audio.Sfx.Play("grade_fecha", transform.position); Set(true); }
        public void Open() { if (IsClosed) SeteVidas.Audio.Sfx.Play("grade_abre", transform.position); Set(false); }

        void Set(bool closed)
        {
            IsClosed = closed;
            _solid.enabled = closed; // fecha na hora (ninguém escapa); o visual desce em seguida
        }

        public void SetImmediate(bool closed)
        {
            Set(closed);
            if (bars != null) bars.localPosition = closed ? _closedPos : _closedPos + Vector3.up * openHeight;
        }

        void Update()
        {
            if (bars == null) return;
            var target = IsClosed ? _closedPos : _closedPos + Vector3.up * openHeight;
            bars.localPosition = Vector3.MoveTowards(bars.localPosition, target, speed * Time.unscaledDeltaTime);
        }
    }
}
