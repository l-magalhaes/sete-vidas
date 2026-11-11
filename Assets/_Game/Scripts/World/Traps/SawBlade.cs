using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Serra que corre num trilho entre dois pontos (X), girando, com uma pausa em cada ponta.
    /// Pule por cima dela ou espere numa plataforma.
    /// </summary>
    public class SawBlade : TrapBase
    {
        [Tooltip("Pontas do trilho (X no mundo)")]
        [SerializeField] float fromX;
        [SerializeField] float toX;
        [SerializeField, Min(0.1f)] float speed = 4f;
        [SerializeField, Min(0f)] float pause = 0.4f;
        [SerializeField] float radius = 0.55f;
        [SerializeField] Transform blade;

        int _dir = 1;
        float _pauseTimer;

        public void Configure(float a, float b)
        {
            fromX = Mathf.Min(a, b);
            toX = Mathf.Max(a, b);
        }

        protected override Rect DangerArea()
        {
            float y = transform.position.y;
            return new Rect(fromX - radius, y - radius, toX - fromX + 2f * radius, 2f * radius);
        }

        // a lâmina de agora (não o trilho todo): é onde a estocada quica
        protected override System.Func<Rect> PogoArea => () =>
            new Rect((Vector2)transform.position - Vector2.one * radius * 0.8f, Vector2.one * radius * 1.6f);

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var p = transform.position;
            if (_pauseTimer > 0f) _pauseTimer -= dt;
            else
            {
                float target = _dir > 0 ? toX : fromX;
                p.x = Mathf.MoveTowards(p.x, target, speed * dt);
                if (Mathf.Approximately(p.x, target)) { _dir = -_dir; _pauseTimer = pause; }
                transform.position = p;
            }
            HitPlayerIn(p, new Vector2(radius * 1.6f, radius * 1.6f));
        }

        void LateUpdate()
        {
            if (blade != null && _pauseTimer <= 0f) blade.Rotate(0f, 0f, -_dir * 720f * Time.deltaTime);
        }
    }
}
