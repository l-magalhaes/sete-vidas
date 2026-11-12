using System.Collections;
using SeteVidas.Abilities;
using SeteVidas.Combat;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.World;
using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>
    /// Os sons do gato (Docs/Audio/plano-sfx.md §3): passos, pulo, aterrissagem, borda, hera, golpes, acertos,
    /// dano, morte, cura e o batimento da última vida. Só observa o estado do gato e os eventos do combate e da
    /// vida; não muda nada no jogo. O StealthController o adiciona sozinho (não precisa recriar o prefab).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAudio : MonoBehaviour
    {
        [Tooltip("Queda (tiles) a partir da qual a aterrissagem soa; abaixo disso, silêncio")]
        [SerializeField] float softLandingHeight = 0.6f;

        PlayerController _player;
        CombatController _combat;
        PlayerHealth _health;
        HealingSpell _heal;
        StealthController _stealth;

        bool _wasGrounded = true, _wasHanging, _wasLedge, _wasClimbing, _wasCover, _wasWindup, _wasCasting, _healed;
        float _peakY, _stepTimer, _climbTimer;
        SfxLoop _heartbeat;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _combat = GetComponent<CombatController>();
            _health = GetComponent<PlayerHealth>();
            _heal = GetComponent<HealingSpell>();
            _stealth = GetComponent<StealthController>();
        }

        void OnEnable()
        {
            if (_combat != null)
            {
                _combat.SwingStarted += OnSwing;
                _combat.Hit += OnHit;
                _combat.Bounced += OnBounced;
                _combat.ShockwaveReleased += OnShockwave;
            }
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died += OnDied;
                _health.Respawned += OnRespawned;
            }
            if (_heal != null) _heal.ChargesChanged += OnChargesChanged;
        }

        void OnDisable()
        {
            if (_combat != null)
            {
                _combat.SwingStarted -= OnSwing;
                _combat.Hit -= OnHit;
                _combat.Bounced -= OnBounced;
                _combat.ShockwaveReleased -= OnShockwave;
            }
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
                _health.Respawned -= OnRespawned;
            }
            if (_heal != null) _heal.ChargesChanged -= OnChargesChanged;
            StopHeartbeat();
        }

        void Update()
        {
            if (_player == null || _player.Body == null) return;
            if (Time.timeScale <= 0f) return; // pausado (diálogo, inventário)

            Vector2 v = _player.Velocity;
            float y = _player.Body.position.y;
            bool grounded = _player.IsGrounded;
            bool climbing = _player.ActiveAbility is WallClimbAbility;
            bool hanging = _player.IsHanging, ledge = _player.IsClimbingLedge;
            bool supported = hanging || ledge || climbing;
            bool dead = _health != null && (_health.IsDead || _health.IsCollapsed);

            if (!dead)
            {
                // aterrissagem: a altura conta do ponto mais alto no ar (ou de onde soltou a borda/parede)
                if (grounded && !_wasGrounded && !supported && Time.time - WaterSurface.LastSplashAt > 0.6f)
                {
                    float fall = _peakY - y;
                    float loud = _stealth != null && _stealth.Settings != null ? _stealth.Settings.LandingNoiseHeight : 4.5f;
                    if (fall >= loud) Sfx.Play("gato_aterrissa_pesada");
                    else if (fall >= softLandingHeight) Sfx.Play("gato_aterrissa");
                    _stepTimer = 0.12f;
                }
                // pulo: saiu do chão subindo
                if (!grounded && _wasGrounded && !supported && v.y > 3f) Sfx.Play("gato_pulo");

                // parede escalável: agarrar, escalar, pular dela
                if (climbing && !_wasClimbing) { Sfx.Play("gato_hera"); _climbTimer = 0.2f; }
                if (!climbing && _wasClimbing && v.y > 3f) Sfx.Play("gato_pulo_parede");
                if (climbing && Mathf.Abs(v.y) > 0.5f)
                {
                    _climbTimer -= Time.deltaTime;
                    if (_climbTimer <= 0f) { Sfx.Play("gato_escalada"); _climbTimer = Mathf.Abs(v.y) > 5f ? 0.14f : 0.2f; }
                }

                if (hanging && !_wasHanging) Sfx.Play("gato_borda");
                if (ledge && !_wasLedge) Sfx.Play("gato_sobe_borda");

                // passos: o agachado quase não soa (agachar = ruído 0 na regra)
                float speed = Mathf.Abs(v.x);
                if (grounded && !supported && speed > 0.8f)
                {
                    _stepTimer -= Time.deltaTime;
                    if (_stepTimer <= 0f)
                    {
                        bool crouch = _player.IsCrouching;
                        float k = _player.Settings != null ? Mathf.Clamp01(speed / _player.Settings.MoveSpeed) : 1f;
                        // correndo (≥ 60% da velocidade, o mesmo limite do ruído de passo) ou andando devagar
                        Sfx.Play(crouch ? "gato_passo_agachado" : k >= 0.6f ? "gato_passo" : "gato_passo_lento");
                        _stepTimer = crouch ? 0.42f : Mathf.Lerp(0.4f, 0.27f, k);
                    }
                }
                else _stepTimer = 0.06f; // o 1º passo sai logo depois de começar a andar

                bool cover = _stealth != null && _stealth.InCover;
                if (cover && !_wasCover) Sfx.Play("gato_esconder");
                _wasCover = cover;

                // marreta erguida (preparação do golpe pesado)
                bool windup = _combat != null && _combat.Weapon != null && _combat.Weapon.Type == DamageType.Heavy && _combat.IsWindingUp;
                if (windup && !_wasWindup) Sfx.Play("marreta_ergue");
                _wasWindup = windup;
            }

            if (grounded || supported) _peakY = y;
            else _peakY = Mathf.Max(_peakY, y);

            // cura: concentração, efeito ou interrupção
            bool casting = _heal != null && _heal.IsCasting;
            if (casting && !_wasCasting) { _healed = false; Sfx.Play("cura_concentra"); }
            if (!casting && _wasCasting && !dead) Sfx.Play(_healed ? "cura_efeito" : "cura_interrompida");
            _wasCasting = casting;

            // a última vida: o batimento
            bool lastLife = _health != null && !_health.IsDead && _health.CurrentHits == 1;
            if (lastLife && _heartbeat == null) _heartbeat = Sfx.Loop("gato_batimento_loop", transform);
            else if (!lastLife) StopHeartbeat();

            _wasGrounded = grounded;
            _wasHanging = hanging;
            _wasLedge = ledge;
            _wasClimbing = climbing;
        }

        void StopHeartbeat()
        {
            if (_heartbeat != null) _heartbeat.Stop(0.6f);
            _heartbeat = null;
        }

        // ------------------------------------------------------------ combate

        void OnSwing()
        {
            if (_combat == null || _combat.Weapon == null) return;
            if (_combat.Weapon.Type == DamageType.Heavy) Sfx.Play("marreta_golpe");
            else if (_combat.IsDownAttacking) Sfx.Play("estocada");
            else Sfx.Play(_combat.ComboStep == 0 ? "adaga_golpe" : "adaga_golpe2");
        }

        void OnHit(IDamageable target, DamageInfo info)
        {
            if (target is BreakableWall) return;                              // a parede toca o próprio som
            if (target is GoldenBoss boss && boss.IsShielded) return;         // o escudo toca o "dong"
            if (target is TrainingDummy) Sfx.Play("boneco", ((Component)target).transform.position);
            if (info.Type == DamageType.Heavy) { Sfx.Play("marreta_acerto"); return; }
            switch (info.Angle)
            {
                case AttackAngle.StealthBack: Sfx.Play("acerto_furtivo"); break;
                case AttackAngle.Back: Sfx.Play("acerto_costas"); break;
                default: Sfx.Play("acerto"); break;
            }
        }

        void OnBounced() => Sfx.Play("quique");
        /// <summary>Quanto o gato caiu desde o ponto mais alto do pulo (tiles). A água usa para o volume do respingo.</summary>
        public float FallHeight => _player != null && _player.Body != null ? Mathf.Max(0f, _peakY - _player.Body.position.y) : 0f;

        void OnShockwave(float charge) => Sfx.Play("marreta_chao", null, 0.7f + 0.3f * charge);

        // ------------------------------------------------------------ vida

        void OnDamaged(DamageInfo info)
        {
            Sfx.Play("gato_dano");
            if (info.Source != null && info.Source.GetComponent<SpikeStrip>() != null)
            {
                Sfx.Play("espinhos");
                if (_health != null && !_health.IsDead) StartCoroutine(SafeGroundLater());
            }
        }

        IEnumerator SafeGroundLater()
        {
            yield return new WaitForSeconds(0.15f); // o mesmo tempo do escurecer em PlayerHealth.ReturnToSafeGround
            Sfx.Play("chao_seguro");
        }

        void OnDied()
        {
            StopHeartbeat();
            Sfx.Play("gato_morte");
        }

        void OnRespawned() => Sfx.Play("gato_renasce");

        void OnChargesChanged()
        {
            if (_heal != null && _heal.IsCasting) _healed = true;
        }
    }
}
