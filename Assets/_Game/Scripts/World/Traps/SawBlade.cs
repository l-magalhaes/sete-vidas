using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Serra que corre num trilho entre dois pontos, girando, com uma pausa em cada ponta: na horizontal (X) ou, com
    /// <c>vertical</c>, subindo e descendo (Y). Pule por cima dela, passe por baixo ou espere numa plataforma.
    /// </summary>
    public class SawBlade : TrapBase
    {
        [Tooltip("Pontas do trilho no mundo (X; ou Y se vertical)")]
        [SerializeField] float fromX;
        [SerializeField] float toX;
        [Tooltip("Sobe e desce em vez de ir de um lado ao outro")]
        [SerializeField] bool vertical;
        [SerializeField, Min(0.1f)] float speed = 4f;
        [SerializeField, Min(0f)] float pause = 0.4f;
        [SerializeField] float radius = 0.55f;
        [SerializeField] Transform blade;

        int _dir = 1;
        float _pauseTimer;
        SeteVidas.Audio.SfxLoop _whine;

        // o zumbido da lâmina: ouve-se antes de ver (Docs/Audio/plano-sfx.md §8)
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying) _whine = SeteVidas.Audio.Sfx.Loop("serra_loop", transform);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_whine != null) _whine.Stop(0.1f);
            _whine = null;
        }

        public void Configure(float a, float b, bool upDown = false)
        {
            vertical = upDown;
            fromX = Mathf.Min(a, b);
            toX = Mathf.Max(a, b);
        }

        protected override Rect DangerArea()
        {
            if (vertical)
            {
                float x = transform.position.x;
                return new Rect(x - radius, fromX - radius, 2f * radius, toX - fromX + 2f * radius);
            }
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
            if (_whine != null) _whine.Pitch = Mathf.MoveTowards(_whine.Pitch, _pauseTimer > 0f ? 0.8f : 1f, dt * 2f);
            if (_pauseTimer > 0f) _pauseTimer -= dt;
            else
            {
                float target = _dir > 0 ? toX : fromX;
                float now = Mathf.MoveTowards(vertical ? p.y : p.x, target, speed * dt);
                if (vertical) p.y = now; else p.x = now;
                if (Mathf.Approximately(now, target)) { _dir = -_dir; _pauseTimer = pause; }
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
