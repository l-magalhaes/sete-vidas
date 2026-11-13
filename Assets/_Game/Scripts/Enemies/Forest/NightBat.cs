using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;
using static SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Morcego Noturno (Docs/Design/floresta-bestiario.md; ajuste do usuário: persegue). Dorme pendurado; acorda com o
    /// gato perto (agachado, só bem perto) ou com barulho; voa atrás dele e encosta para ferir (1 hit), recua e volta.
    /// Longe demais, desiste e volta ao poleiro. Os que o Broto solta não têm poleiro: somem ao reiniciar o mundo.
    /// </summary>
    public class NightBat : ForestEnemy
    {
        [SerializeField] Sprite sleeping, flyA, flyB;
        [Tooltip("Barulho mínimo (0..1) que acorda o morcego")]
        [SerializeField, Range(0f, 1f)] float noiseToWake = 0.3f;
        [Tooltip("Distância em que vê o gato de pé (agachado, a metade). Padrão: 7")]
        [SerializeField, Min(1f)] float wakeRange = BatWakeRange;

        BatState _state = BatState.Sleep;
        float _timer;
        Vector2 _vel;
        bool _spawned;
        bool _heard;

        public BatState State => _state;
        public override bool AcceptsPogo => false; // bestiário: sem pogo (é rápido e pequeno)

        public void Configure(float range = BatWakeRange) { maxHealth = BatHealth; wakeRange = range; }

        protected override void OnEnable() { base.OnEnable(); NoiseSystem.Emitted += OnNoise; }
        protected override void OnDisable() { base.OnDisable(); NoiseSystem.Emitted -= OnNoise; }

        void OnNoise(NoiseEvent n)
        {
            if (_state != BatState.Sleep || n.Loudness < noiseToWake) return;
            if (Vector2.Distance(n.Position, transform.position) <= n.Radius) _heard = true;
        }

        /// <summary>Saiu das folhas de um Broto: já acordado e perseguindo, sem poleiro.</summary>
        public void LaunchFromLeaves()
        {
            _spawned = true;
            _state = BatState.Chase;
            _vel = Vector2.up * 3f;
        }

        protected override void Revive()
        {
            if (_spawned) { Destroy(gameObject); return; } // os do Broto não voltam
            base.Revive();
            _state = BatState.Sleep;
            _vel = Vector2.zero;
            _heard = false;
        }

        const float Radius = 0.35f;
        static int s_wallMask = -1;
        static readonly RaycastHit2D[] s_hits = new RaycastHit2D[4];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetBatStatics() => s_wallMask = -1; // Play sem recarregar o domínio

        /// <summary>
        /// Anda <paramref name="move"/> sem atravessar chão, parede, teto ou plataforma (desliza pela superfície).
        /// Galhos de mão única contam como sólidos para o morcego.
        /// </summary>
        Vector2 Collide(Vector2 from, Vector2 move)
        {
            if (s_wallMask < 0)
                s_wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable,
                    GameConstants.Layers.Breakable, GameConstants.Layers.Platform);
            Vector2 done = Vector2.zero;
            for (int i = 0; i < 2 && move.sqrMagnitude > 1e-8f; i++)
            {
                float dist = move.magnitude;
                Vector2 dir = move / dist;
                int n = Physics2D.CircleCastNonAlloc(from + done, Radius, dir, s_hits, dist + 0.02f, s_wallMask);
                float best = float.MaxValue; Vector2 normal = Vector2.zero;
                for (int k = 0; k < n; k++)
                {
                    var hit = s_hits[k];
                    if (hit.collider == null || hit.collider.isTrigger) continue;
                    // encostado (distância 0): só bloqueia se estiver indo contra a superfície; senão deixa sair
                    if (hit.distance <= 0f && Vector2.Dot(dir, hit.normal) >= 0f) continue;
                    if (hit.distance < best) { best = hit.distance; normal = hit.normal; }
                }
                if (best == float.MaxValue) { done += move; break; }
                if (normal == Vector2.zero) normal = -dir;                 // começou dentro: trata como parede de frente
                float go = Mathf.Max(0f, best - 0.05f);
                done += dir * go;
                Vector2 rest = move - dir * go;
                move = rest - Vector2.Dot(rest, normal) * normal;          // desliza pela parede
                _vel -= Vector2.Dot(_vel, normal) * normal;                 // e perde a velocidade contra ela
            }
            return done;
        }

        protected override void Tick(float dt)
        {
            var h = Player;
            Vector2 me = transform.position;
            Vector2 target = h != null && !h.IsDead ? (Vector2)h.transform.position + Vector2.up * 0.7f : (Vector2)_home;
            float dist = h != null && !h.IsDead ? Vector2.Distance(me, target) : 999f;
            bool crouched = h != null && h.GetComponent<PlayerController>() is var pc && pc != null && pc.IsCrouching;
            bool atPerch = Vector2.Distance(me, _home) < 0.2f;

            bool hit = false;
            if (_state == BatState.Chase)
            {
                var body = new Rect(me - new Vector2(0.4f, 0.35f), new Vector2(0.8f, 0.7f));
                hit = HurtPlayerIn(body);
            }
            var before = _state;
            _state = StepBat(_state, ref _timer, dt, _spawned ? Mathf.Min(dist, BatGiveUpRange) : dist, crouched, _heard, hit, atPerch && !_spawned, wakeRange);
            _heard = false;
            if (_state == BatState.Wake && before == BatState.Sleep) SeteVidas.Audio.Sfx.Play("zumbi_sente", me, 0.6f, 1.8f); // guincho (provisório)

            Vector2 want;
            switch (_state)
            {
                case BatState.Chase:
                    // persegue com um voo ondulado
                    want = (target - me).normalized * BatSpeed + new Vector2(0f, Mathf.Sin(Time.time * 6f) * 1.2f);
                    break;
                case BatState.Recoil:
                    want = (me - target).normalized * BatSpeed * 0.8f + Vector2.up * 1.5f;
                    break;
                case BatState.Return:
                    want = ((Vector2)_home - me).normalized * BatSpeed * 0.7f;
                    if (atPerch) want = Vector2.zero;
                    break;
                default:
                    want = Vector2.zero;
                    break;
            }
            _vel = Vector2.MoveTowards(_vel, want, 14f * dt);
            Vector2 next = me + Collide(me, _vel * dt);
            // garantia: nunca entra numa parede (se já não estava dentro dela)
            if (Physics2D.OverlapCircle(next, Radius * 0.9f, s_wallMask) != null && Physics2D.OverlapCircle(me, Radius * 0.9f, s_wallMask) == null)
            { next = me; _vel = Vector2.zero; }
            transform.position = next;
            if (_state == BatState.Sleep && atPerch) transform.position = _home;
            if (Mathf.Abs(_vel.x) > 0.1f) _facing = _vel.x > 0f ? 1 : -1;

            if (spriteRenderer != null)
            {
                Sprite s = _state == BatState.Sleep ? sleeping : (((int)(Time.time * 10f)) & 1) == 0 ? flyA : flyB;
                if (s != null) spriteRenderer.sprite = s;
            }
        }
    }
}
