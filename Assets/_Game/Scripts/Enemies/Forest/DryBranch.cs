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
    /// <para>Variação <b>Broto</b> (<see cref="spawnsBats"/>): uma colmeia pendurada no galho do alto (pedido do usuário,
    /// 19/11/2026); dela saem as vespas (no máximo 2 vivas, uma a cada 4 s). A colmeia treme antes de cada uma sair e
    /// tomba junto com o tronco.</para>
    /// </summary>
    public class DryBranch : ForestEnemy, ISolidBody
    {
        protected override string SoundName => "galho";   // F10
        [Tooltip("Altura do corpo (tiles). Mais de 5 = não dá para passar por cima")]
        [SerializeField, Min(1f)] float height = BranchHeight;
        [SerializeField] bool startsDisguised = true;
        [SerializeField] Sprite standing, armUp, armDown, disguised;
        [Tooltip("F4c: disfarçado (a copa balança; de vez em quando um olho se abre na casca), acordado (respirando) e braço erguido (tremendo)")]
        [SerializeField] Sprite[] disguisedFrames = new Sprite[0], standingFrames = new Sprite[0], armUpFrames = new Sprite[0];
        [Header("Broto: solta morcegos")]
        [SerializeField] bool spawnsBats;
        [Tooltip("Morcego modelo (desligado) que o Broto copia")]
        [SerializeField] NightBat batTemplate;

        BranchState _state;
        float _timer;
        SpriteRenderer _hive;
        Vector3 _hiveRest;
        float _hiveShake;
        /// <summary>A colmeia do Broto fica menor que a das vespas do teto (pendurada num galho do tronco).</summary>
        const float HiveScale = 0.7f;
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
        /// <summary>
        /// O gato encostou no tronco (pedido do usuário, 15/11/2026: "os Galhos Secos te detectam se você encostar neles"):
        /// vê na hora ("!"), larga o disfarce mesmo com o gato na sombra e vira para ele (logo em seguida, a pancada).
        /// </summary>
        public void OnBumped(Vector2 playerPosition)
        {
            if (IsDead || Senses == null) return;
            bool wasDisguised = _state == BranchState.Disguised;
            if (wasDisguised || !Aware)
                Sound("estalo", transform.position);   // o estalo de madeira (F10)
            Senses.ForceAware(playerPosition);
            _state = BranchBumped(_state);
            if (_state == BranchState.Awake) _facing = playerPosition.x >= transform.position.x ? 1 : -1;
        }

        protected override void OnEnable() { base.OnEnable(); SolidBodies.Register(this); }
        protected override void OnDisable() { base.OnDisable(); SolidBodies.Unregister(this); }

        protected override void Awake()
        {
            if (maxHealth < BranchHealth) maxHealth = BranchHealth;   // cenas geradas antes de 19/11/2026 (6 de vida) já valem com a nova
            base.Awake();
            if (spriteRenderer != null) _visualPos = spriteRenderer.transform.localPosition;
            _state = startsDisguised ? BranchState.Disguised : BranchState.Awake;
            if (spawnsBats) BuildHive();
        }

        static readonly Color HiveInfected = new Color(0.9f, 0.8f, 1f);   // a praga chegou na cera também

        /// <summary>
        /// A colmeia do Broto: filha do desenho (tomba junto com ele), pendurada no galho do alto, do lado para onde ele
        /// olha. A vespa sai pela boca dela.
        /// </summary>
        void BuildHive()
        {
            if (spriteRenderer == null || _hive != null) return;
            var go = new GameObject("Colmeia do Broto");
            go.transform.SetParent(spriteRenderer.transform, false);
            float side = startFacing >= 0 ? 1f : -1f;
            _hiveRest = new Vector3(side * 0.85f, height - 0.9f - _visualPos.y, 0f);
            go.transform.localPosition = _hiveRest;
            go.transform.localScale = new Vector3(HiveScale, HiveScale, 1f);
            _hive = go.AddComponent<SpriteRenderer>();
            _hive.sprite = SeteVidas.World.BeeHive.HiveSprite();
            _hive.sortingOrder = spriteRenderer.sortingOrder + 1;
            if (IsInfected) _hive.color = HiveInfected;
        }

        /// <summary>A boca da colmeia (mundo): de onde a vespa sai.</summary>
        Vector3 HiveMouth => _hive != null
            ? _hive.transform.position + Vector3.down * (SeteVidas.World.BeeArt.HiveH / 16f * 0.68f * HiveScale)
            : transform.position + Vector3.up * (height - 0.3f);

        /// <summary>A colmeia balança de leve; logo antes de soltar uma vespa, treme forte (o aviso).</summary>
        void ShakeHive(float dt)
        {
            if (_hive == null) return;
            bool soon = _state != BranchState.Disguised && SproutInterval - _sinceSpawn < 0.6f && _bats.Count < 2;
            _hiveShake += dt;
            float dx = soon ? Mathf.Sin(_hiveShake * 55f) * 0.06f : Mathf.Sin(_hiveShake * 2.2f) * 0.03f;
            _hive.transform.localPosition = _hiveRest + new Vector3(dx, 0f, 0f);
        }

        protected override void Revive()
        {
            base.Revive();
            StopAllCoroutines();
            _arrowCarry = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localPosition = _visualPos;
                spriteRenderer.transform.localRotation = Quaternion.identity;
                spriteRenderer.color = Color.white;
            }
            if (_hive != null)
            {
                _hive.enabled = true;
                _hive.color = IsInfected ? HiveInfected : Color.white;
                _hive.transform.localPosition = _hiveRest;
            }
            _state = startsDisguised ? BranchState.Disguised : BranchState.Awake;
            _timer = 0f;
            _sinceSpawn = SproutInterval;
        }

        float _arrowCarry;
        Vector3 _visualPos;

        /// <summary>A flecha crava na madeira e tira pouco (<see cref="BranchArrowHit"/>); de perto, o dano inteiro.</summary>
        protected override int AdjustDamage(in DamageInfo info, int dmg)
        {
            if (!info.Ranged) return dmg;
            int d = BranchArrowHit(dmg, ref _arrowCarry);
            if (d <= 0) SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position, 0.4f, 0.7f);   // tóc: cravou na casca
            return d;
        }

        protected override float CorpseTime => BranchCorpseTime;

        protected override void OnDied()
        {
            StartCoroutine(Fall());
            // derrubado: os morcegos que soltou continuam (já estão no ar)
            // infectado: vira uma nuvem tóxica no pé do tronco (bestiário)
            if (IsInfected)
                ToxicCloud.Spawn((Vector2)transform.position + Vector2.up * (BranchCloudSize * 0.5f),
                    Vector2.one * BranchCloudSize, BranchCloudTime);
        }

        bool _eyesWereLit;

        /// <summary>Infectado e disfarçado: os olhos verdes piscam de vez em quando (dá para notar o tronco falso).</summary>
        void BlinkEyes()
        {
            bool lit = BranchEyesLit(Time.time + _home.x * 0.37f); // cada tronco pisca na sua hora
            if (lit && !_eyesWereLit)
            {
                float y = transform.position.y + height - 1.6f;
                for (int i = -1; i <= 1; i += 2)
                    HitSparks.One(new Vector2(transform.position.x + i * 0.2f, y), Vector2.zero, Infected.Acid, BranchBlinkOn, 0f);
            }
            _eyesWereLit = lit;
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

        protected override Vector2 EyePosition => (Vector2)transform.position + Vector2.up * height * 0.75f;
        protected override Vector3 IconOffset => new Vector3(0f, height + 0.4f, 0f);

        protected override void Tick(float dt)
        {
            float dist = PlayerDistance(out int side);
            if (!Aware) dist = -1f; // alerta (14/11/2026): só acorda e ataca depois do "!"
            var sil = Player != null ? Player.GetComponent<SeteVidas.Player.ShadowSilhouette>() : null;
            bool hidden = sil != null && sil.IsSilhouette;
            var before = _state;
            // parado: vira para o gato quando ele está perto (não anda)
            if (_state == BranchState.Awake && dist >= 0f && dist < 6f) _facing = side;
            else if (_state == BranchState.Awake && Senses.Suspicious) _facing = Senses.LastKnown.x >= transform.position.x ? 1 : -1; // "?": olha para lá
            _state = StepBranch(_state, ref _timer, dt, dist, hidden, IsInfected);
            if (IsInfected && _state == BranchState.Disguised) BlinkEyes();
            Senses.IconHidden = _state == BranchState.Disguised;

            if (_state == BranchState.Windup && before != BranchState.Windup)
                Sound("estalo", transform.position);   // o estalo de madeira (F10)
            if (_state == BranchState.Stuck && before == BranchState.Windup)
            {
                // a pancada no chão à frente
                var box = new Rect(transform.position.x + (_facing > 0 ? 0.3f : -0.3f - BranchAttackRange), transform.position.y, BranchAttackRange, 1.8f);
                HurtPlayerIn(box);
                HitSparks.Burst((Vector2)transform.position + Vector2.right * _facing * 1.5f, 8, new Color32(0x82, 0x6a, 0x52, 0xff), new Color32(0x52, 0x46, 0x40, 0xff), 1.5f, 4f, 0.35f, 30f, 0.4f);
                Sound("pancada", transform.position);
            }

            if (spawnsBats && _state != BranchState.Disguised) StepSprout(dt);
            if (spawnsBats) ShakeHive(dt);

            if (spriteRenderer != null)
            {
                Sprite want = _state == BranchState.Disguised ? Anim(disguisedFrames, disguised, 2.5f)
                           : _state == BranchState.Windup ? Anim(armUpFrames, armUp, 14f)
                           : _state == BranchState.Stuck ? armDown : Anim(standingFrames, standing, 3f);
                if (want != null) spriteRenderer.sprite = want;
            }
        }


        /// <summary>
        /// A morte (pedido do usuário, 17/11/2026: "eles não têm animação de morte"): o tronco estala e treme, tomba para o
        /// lado de longe do gato (a base fica no chão, cada vez mais rápido), bate no chão levantando lascas e poeira,
        /// quica um pouco e some.
        /// </summary>
        System.Collections.IEnumerator Fall()
        {
            if (spriteRenderer == null) yield break;
            var visual = spriteRenderer.transform;
            var h = Player;
            int side = h != null && h.transform.position.x > transform.position.x ? -1 : 1;   // cai para longe do gato
            var wood = new Color32(0x82, 0x6a, 0x52, 0xff);
            var dark = new Color32(0x4a, 0x3a, 0x2c, 0xff);
            Vector2 mid = (Vector2)transform.position + Vector2.up * height * 0.45f;
            HitSparks.Burst(mid, 12, wood, dark, 2f, 6f, 0.5f, 30f, 0.5f);
            Sound("estalo", transform.position, 1f, 0.8f);
            float t = 0f;
            while (t < BranchCrackTime)   // estala: treme no lugar
            {
                t += Time.deltaTime;
                visual.localPosition = _visualPos + new Vector3(Mathf.Sin(t * 90f) * 0.06f, 0f, 0f);
                yield return null;
            }
            t = 0f;
            bool landed = false;
            float total = BranchFallTime + BranchSettleTime;
            while (t < total)
            {
                t += Time.deltaTime;
                float ang = BranchFallAngleAt(t) * -side;   // z negativo = tomba para a direita
                visual.localRotation = Quaternion.Euler(0f, 0f, ang);
                // gira em volta da base (os pés ficam no chão)
                visual.localPosition = Quaternion.Euler(0f, 0f, ang) * _visualPos;
                if (!landed && t >= BranchFallTime)
                {
                    landed = true;
                    Vector2 ground = (Vector2)transform.position + Vector2.right * side * height * 0.5f;
                    HitSparks.Burst(ground + Vector2.up * 0.2f, 16, wood, new Color32(0x6e, 0x60, 0x58, 0xff), 2f, 7f, 0.5f, 35f, 0.8f);
                    for (int i = 0; i < 6; i++)
                        HitSparks.One((Vector2)transform.position + Vector2.right * side * (0.8f + i * 0.8f), new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 4f)), dark, 0.5f, 25f);
                    CameraShake.Shake(1.5f, 0.25f);
                    Sound("tomba", transform.position);
                }
                yield return null;
            }
            t = 0f;
            while (t < BranchFadeTime)   // some (o ForestEnemy esconde o desenho no fim)
            {
                t += Time.deltaTime;
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - t / BranchFadeTime);
                if (_hive != null) { var c = _hive.color; c.a = 1f - t / BranchFadeTime; _hive.color = c; }
                yield return null;
            }
            if (_hive != null) _hive.enabled = false;
        }
        void StepSprout(float dt)
        {
            _sinceSpawn += dt;
            _bats.RemoveAll(b => b == null || b.IsDead);
            var h = Player;
            bool near = h != null && !h.IsDead && Vector2.Distance(h.transform.position, transform.position) <= SproutRange;
            if (batTemplate == null || !SproutSpawns(_sinceSpawn, _bats.Count, near)) return;
            _sinceSpawn = 0f;
            var top = HiveMouth;
            var bat = Instantiate(batTemplate, top, Quaternion.identity, transform.parent);
            bat.gameObject.SetActive(true);
            bat.LaunchFromLeaves();
            _bats.Add(bat);
            // lascas de cera saindo da boca da colmeia
            HitSparks.Burst(top, 8, new Color32(0xc9, 0x92, 0x38, 0xff), new Color32(0x6e, 0x45, 0x19, 0xff), 1.5f, 4f, 0.4f, 60f, 0.4f);
            SeteVidas.Audio.Sfx.Play("colmeia_solta", top);
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
