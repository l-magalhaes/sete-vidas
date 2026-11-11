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
        [Tooltip("Atraso do primeiro tiro automático (s): sincroniza com uma serra de mesmo período (o poço)")]
        [SerializeField, Min(0f)] float autoOffset;

        int _toFire;
        float _timer;
        float _autoTimer;

        public void Configure(int dir, float auto) => Configure(dir, auto, 0f);

        /// <summary>Direção, intervalo automático e quanto do intervalo já "passou" no começo (defasa o primeiro tiro).</summary>
        public void Configure(int dir, float auto, float offset)
        {
            direction = dir >= 0 ? 1 : -1;
            autoInterval = auto;
            autoOffset = offset;
            ApplyFacing();   // já no editor: no jogo e na cena o desenho fica virado para o lado do tiro
        }

        void Awake() { _autoTimer = autoOffset; }

        /// <summary>
        /// O desenho (boca para a direita) vira para o lado do tiro: a boca fica sempre do lado de onde saem as flechas e o
        /// bloco de pedra do lado da parede.
        /// </summary>
        void Start() => ApplyFacing();

        public void ApplyFacing()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.flipX = direction < 0;
                var p = sr.transform.localPosition;
                if (sr.transform != transform) sr.transform.localPosition = new Vector3(Mathf.Abs(p.x) * direction, p.y, p.z);
            }
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
                if (_autoTimer >= autoInterval) { _autoTimer -= autoInterval; Fire(); }   // sem acumular atraso: fica no compasso da serra
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
