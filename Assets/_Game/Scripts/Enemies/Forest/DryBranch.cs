using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;
using static SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Galho Seco (ajuste do usuário, 13/11/2026): fica parado e é alto (5,5 tiles), então bloqueia a passagem: não dá
    /// para pular por cima, é preciso derrubá-lo. Finge ser tronco até o gato chegar perto; levanta o braço (aviso de
    /// 0,6 s) e bate no chão à frente. Com o braço preso no chão (1 s) aceita pogo e leva o dobro pelas costas.
    /// <para>Variação <b>Broto</b> (<see cref="spawnsBats"/>): das folhas do topo saem morcegos (no máximo 2 vivos, um a cada 4 s).</para>
    /// </summary>
    public class DryBranch : ForestEnemy, ISolidBody
    {
        [Tooltip("Altura do corpo (tiles). Mais de 5 = não dá para passar por cima")]
        [SerializeField, Min(1f)] float height = BranchHeight;
        [SerializeField] bool startsDisguised = true;
        [SerializeField] Sprite standing, armUp, armDown, disguised;
        [Header("Broto: solta morcegos")]
        [SerializeField] bool spawnsBats;
        [Tooltip("Morcego modelo (desligado) que o Broto copia")]
        [SerializeField] NightBat batTemplate;

        BranchState _state;
        float _timer;
        float _sinceSpawn = SproutInterval;
        readonly List<NightBat> _bats = new List<NightBat>();

        public BranchState State => _state;
        public override bool AcceptsPogo => BranchVulnerable(_state);
        public bool IsSprout => spawnsBats;

        public void Configure(float bodyHeight, int facing, bool disguise, bool sprout)
        {
            height = bodyHeight;
            startFacing = facing;
            startsDisguised = disguise;
            spawnsBats = sprout;
            maxHealth = BranchHealth;
        }

        // ------------------------------------------------------------ corpo sólido (o gato esbarra)

        public Rect Bounds => new Rect(transform.position.x - 0.6f, transform.position.y, 1.2f, height);
        public bool IsSolid => !IsDead && isActiveAndEnabled;
        public void OnBumped(Vector2 playerPosition) { }

        protected override void OnEnable() { base.OnEnable(); SolidBodies.Register(this); }
        protected override void OnDisable() { base.OnDisable(); SolidBodies.Unregister(this); }

        protected override void Awake()
        {
            base.Awake();
            _state = startsDisguised ? BranchState.Disguised : BranchState.Awake;
        }

        protected override void Revive()
        {
            base.Revive();
            _state = startsDisguised ? BranchState.Disguised : BranchState.Awake;
            _timer = 0f;
            _sinceSpawn = SproutInterval;
        }

        protected override void OnDied()
        {
            // derrubado: os morcegos que soltou continuam (já estão no ar)
        }

        protected override float DamageMultiplier(in DamageInfo info)
        {
            bool fromBehind = info.Direction.x * _facing > 0f;
            return BranchVulnerable(_state) && fromBehind ? 2f : 1f;
        }

        /// <summary>Distância horizontal até o gato, se ele estiver na altura do corpo (senão -1).</summary>
        float PlayerDistance(out int side)
        {
            side = _facing;
            var h = Player;
            if (h == null || h.IsDead) return -1f;
            Vector2 d = h.transform.position - transform.position;
            if (d.y < -1f || d.y > height) return -1f;
            side = d.x >= 0f ? 1 : -1;
            return Mathf.Abs(d.x);
        }

        protected override void Tick(float dt)
        {
            float dist = PlayerDistance(out int side);
            var sil = Player != null ? Player.GetComponent<SeteVidas.Player.ShadowSilhouette>() : null;
            bool hidden = sil != null && sil.IsSilhouette;
            var before = _state;
            // parado: vira para o gato quando ele está perto (não anda)
            if (_state == BranchState.Awake && dist >= 0f && dist < 6f) _facing = side;
            _state = StepBranch(_state, ref _timer, dt, dist, hidden);

            if (_state == BranchState.Windup && before != BranchState.Windup)
                SeteVidas.Audio.Sfx.Play("zumbi_sente", transform.position, 0.8f, 0.7f); // estalo de madeira (provisório)
            if (_state == BranchState.Stuck && before == BranchState.Windup)
            {
                // a pancada no chão à frente
                var box = new Rect(transform.position.x + (_facing > 0 ? 0.3f : -0.3f - BranchAttackRange), transform.position.y, BranchAttackRange, 1.8f);
                HurtPlayerIn(box);
                HitSparks.Burst((Vector2)transform.position + Vector2.right * _facing * 1.5f, 8, new Color32(0x82, 0x6a, 0x52, 0xff), new Color32(0x52, 0x46, 0x40, 0xff), 1.5f, 4f, 0.35f, 30f, 0.4f);
                SeteVidas.Audio.Sfx.Play("parede_racha", transform.position, 0.7f, 0.8f);
            }

            if (spawnsBats && _state != BranchState.Disguised) StepSprout(dt);

            if (spriteRenderer != null)
            {
                Sprite want = _state == BranchState.Disguised ? disguised : _state == BranchState.Windup ? armUp : _state == BranchState.Stuck ? armDown : standing;
                if (want != null) spriteRenderer.sprite = want;
            }
        }

        void StepSprout(float dt)
        {
            _sinceSpawn += dt;
            _bats.RemoveAll(b => b == null || b.IsDead);
            var h = Player;
            bool near = h != null && !h.IsDead && Vector2.Distance(h.transform.position, transform.position) <= SproutRange;
            if (batTemplate == null || !SproutSpawns(_sinceSpawn, _bats.Count, near)) return;
            _sinceSpawn = 0f;
            var top = transform.position + Vector3.up * (height - 0.3f);
            var bat = Instantiate(batTemplate, top, Quaternion.identity, transform.parent);
            bat.gameObject.SetActive(true);
            bat.LaunchFromLeaves();
            _bats.Add(bat);
            var leaf = new Color32(0x4a, 0x68, 0x46, 0xff);
            HitSparks.Burst(top, 8, leaf, new Color32(0x2e, 0x48, 0x34, 0xff), 1.5f, 4f, 0.4f, 60f, 0.4f);
            SeteVidas.Audio.Sfx.Play("zumbi_ocioso", top, 0.6f, 1.6f); // folhas sacudindo (provisório)
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            var b = Bounds;
            Gizmos.color = new Color(0.8f, 0.6f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
#endif
    }
}
