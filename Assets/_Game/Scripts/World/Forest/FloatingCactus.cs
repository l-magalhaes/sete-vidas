using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Cacto Flutuante (Docs/Design/floresta-bestiario.md): bola de espinhos que é ponto de pogo. Não morre.
    /// Encostar fere 1 hit e empurra; a estocada para baixo quica nela (e dá a graça do pogo, que protege do toque).
    /// Pode flutuar num vaivém (<see cref="travel"/> a partir do ponto inicial, ida e volta em <see cref="period"/> s).
    /// A variante infectada (trajetória tremida, disparos) vem na F4.
    /// </summary>
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
    public class FloatingCactus : TrapBase
    {
        [Tooltip("Raio da bola (tiles)")]
        [SerializeField, Min(0.2f)] float radius = 0.6f;
        [Tooltip("Deslocamento máximo a partir do ponto inicial (tiles). Zero = parado")]
        [SerializeField] Vector2 travel = Vector2.zero;
        [Tooltip("Tempo de uma ida e volta (s)")]
        [SerializeField, Min(0.5f)] float period = 3f;
        [SerializeField] Transform visual;

        Vector3 _home;
        float _phase;

        void Reset() => returnToSafeGround = false;

        void Awake()
        {
            _home = transform.position;
            _phase = Random.value * period; // vários cactos não andam em sincronia
        }

        /// <summary>Para o setup do editor.</summary>
        public void Configure(float r, Vector2 move, float seconds)
        {
            radius = r;
            travel = move;
            period = seconds;
        }

        protected override System.Func<Rect> PogoArea => DangerArea;

        protected override Rect DangerArea()
        {
            Vector2 c = transform.position;
            return new Rect(c - Vector2.one * radius, Vector2.one * radius * 2f);
        }

        /// <summary>Posição no vaivém (seno: freia nas pontas). Regra pura para testes.</summary>
        public static Vector2 PathOffset(Vector2 travel, float period, float t) =>
            travel * Mathf.Sin(t / Mathf.Max(0.01f, period) * Mathf.PI * 2f);

        void FixedUpdate()
        {
            if (travel != Vector2.zero)
            {
                _phase += Time.fixedDeltaTime;
                transform.position = _home + (Vector3)PathOffset(travel, period, _phase);
            }
            // a caixa que fere é um pouco menor que a de pogo: raspar a ponta dos espinhos não pune
            HitPlayerIn(transform.position, Vector2.one * radius * 1.5f);
        }

        void Update()
        {
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f + _phase) * 8f); // balança de leve
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            if (travel == Vector2.zero) return;
            Vector3 home = Application.isPlaying ? _home : transform.position;
            Gizmos.color = new Color(0.4f, 1f, 0.9f, 0.5f);
            Gizmos.DrawLine(home - (Vector3)travel, home + (Vector3)travel);
        }
#endif
    }
}
