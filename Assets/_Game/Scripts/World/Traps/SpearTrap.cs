using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Lanças que saem do chão quando disparadas (placa de pressão): um tremor curto de aviso, sobem,
    /// ficam um instante e voltam. Machucam quem estiver em cima.
    /// </summary>
    public class SpearTrap : TrapEffect
    {
        [SerializeField, Min(1)] int width = 2;
        [Tooltip("Tempo entre disparar e as lanças subirem (s)")]
        [SerializeField, Min(0f)] float delay = 0.25f;
        [SerializeField, Min(0.05f)] float upTime = 0.6f;
        [SerializeField] float height = 1.6f;
        [SerializeField] SpriteRenderer spears;

        float _timer = -1f;

        public bool IsUp => _timer >= delay && _timer < delay + upTime;

        protected override Rect DangerArea() =>
            new Rect((Vector2)transform.position - new Vector2(width * 0.5f, 0f), new Vector2(width, height));

        public override void Fire()
        {
            if (_timer < 0f) _timer = 0f;
        }

        void FixedUpdate()
        {
            if (_timer < 0f) return;
            _timer += Time.fixedDeltaTime;
            if (IsUp)
            {
                var r = DangerArea();
                HitPlayerIn(r.center, r.size);
            }
            if (_timer >= delay + upTime) _timer = -1f;
        }

        void LateUpdate()
        {
            if (spears == null) return;
            float k = _timer < 0f ? 0f
                    : _timer < delay ? 0.05f * (((int)(_timer * 30f) & 1) == 0 ? 1f : 0f) // tremor de aviso
                    : IsUp ? Mathf.Clamp01((_timer - delay) / 0.06f)
                    : 0f;
            spears.enabled = k > 0f;
            spears.size = new Vector2(width, height);
            spears.transform.localPosition = new Vector3(0f, height * 0.5f - height * (1f - k), 0f); // sprite com pivô central; sobe de dentro do chão
        }
    }
}
