using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Buraco na parede que atira flechas na horizontal. Disparado por fio/placa (rajada) ou sozinho em
    /// intervalo fixo (<see cref="autoInterval"/> &gt; 0). A flecha voa na altura do peito: agachado, passa por cima.
    /// </summary>
    public class ArrowLauncher : TrapEffect
    {
        [SerializeField] TrapArrow arrowPrefab;
        [Tooltip("1 = atira para a direita, -1 = para a esquerda")]
        [SerializeField] int direction = 1;
        [SerializeField, Min(1)] int burst = 3;
        [SerializeField, Min(0.05f)] float burstInterval = 0.25f;
        [SerializeField, Min(1f)] float speed = 12f;
        [Tooltip("Atira sozinho a cada N segundos (0 = só quando disparado)")]
        [SerializeField, Min(0f)] float autoInterval;
        [Tooltip("Aviso antes da rajada (s)")]
        [SerializeField, Min(0f)] float warnTime = 0.3f;
        [SerializeField] SpriteRenderer warning;

        int _toFire;
        float _timer;
        float _autoTimer;

        public void Configure(int dir, float auto)
        {
            direction = dir >= 0 ? 1 : -1;
            autoInterval = auto;
        }

        protected override Rect DangerArea() => new Rect(transform.position, Vector2.zero); // as flechas é que machucam

        public override void Fire()
        {
            if (_toFire > 0) return;
            _toFire = burst;
            _timer = warnTime;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (autoInterval > 0f)
            {
                _autoTimer += dt;
                if (_autoTimer >= autoInterval) { _autoTimer = 0f; Fire(); }
            }
            if (_toFire <= 0) return;

            _timer -= dt;
            if (_timer > 0f) return;
            if (arrowPrefab != null)
            {
                var arrow = Instantiate(arrowPrefab, transform.position + new Vector3(direction * 0.6f, 0f, 0f), Quaternion.identity);
                arrow.Launch(new Vector2(direction * speed, 0f), damage);
            }
            _toFire--;
            _timer = burstInterval;
        }

        void LateUpdate()
        {
            if (warning != null) warning.enabled = _toFire > 0 && _toFire == burst && _timer > 0f && ((int)(Time.time * 20f) & 1) == 0;
        }
    }
}
