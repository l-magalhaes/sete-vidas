using SeteVidas.Enemies;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>
    /// Os sons dos templários (Docs/Audio/plano-sfx.md §4): passos de armadura, os vocais que contam o estado da IA
    /// ("?" suspeito, farejando, "!" viu, caçando, desistiu), o telégrafo da lança e da besta, dano e morte.
    /// Só observa a <see cref="EnemyAI"/>; ela mesma o adiciona no Awake.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyAudio : MonoBehaviour
    {
        [Tooltip("Intervalo dos resmungos na patrulha (s)")]
        [SerializeField] Vector2 idleInterval = new Vector2(8f, 18f);

        EnemyAI _ai;
        Rigidbody2D _body;
        AlertState _state;
        bool _hunting, _dead, _locked;
        EnemyAI.AttackPhase _phase;
        int _health;
        float _stepTimer, _idleTimer;

        bool Ranged => _ai.Settings != null && _ai.Settings.Ranged;
        /// <summary>O atirador tem a voz um pouco mais aguda (mais magro).</summary>
        float VoicePitch => Ranged ? 1.07f : 1f;

        void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _body = GetComponent<Rigidbody2D>();
        }

        void Start()
        {
            if (_ai == null) { enabled = false; return; }
            Sync();
            _idleTimer = Random.Range(idleInterval.x, idleInterval.y);
        }

        void Sync()
        {
            _state = _ai.State;
            _hunting = _ai.Brain.IsHunting;
            _phase = _ai.CurrentAttackPhase;
            _health = _ai.Health;
            _dead = _ai.IsDead;
            _locked = false;
        }

        void Play(string id, float pitch = 1f) => Sfx.Play(id, (Vector2)transform.position + Vector2.up, 1f, pitch);

        void Update()
        {
            if (_ai == null || Time.timeScale <= 0f) return;

            if (_ai.IsDead)
            {
                // abate furtivo: um gemido abafado, baixo (o golpe silencioso não grita para a fortaleza toda)
                if (!_dead) Sfx.Play("guarda_morte", (Vector2)transform.position + Vector2.up, _ai.WasStealthKilled ? 0.35f : 1f, VoicePitch * (_ai.WasStealthKilled ? 0.85f : 1f));
                _dead = true;
                return;
            }
            if (_dead) { Sync(); return; } // reapareceu: começa calado

            // dano (a vida só volta a subir ao reaparecer)
            if (_ai.Health < _health) Play("guarda_dano", VoicePitch);
            _health = _ai.Health;

            // os vocais do estado
            var state = _ai.State;
            bool hunting = _ai.Brain.IsHunting;
            if (state != _state)
            {
                switch (state)
                {
                    case AlertState.Suspicious: if (_state == AlertState.Patrolling) Play("guarda_suspeito", VoicePitch); break;
                    case AlertState.Investigating: if (!hunting) Play("guarda_investiga", VoicePitch); break;
                    case AlertState.Combat: Play("guarda_alerta", VoicePitch); break;
                    case AlertState.Patrolling:
                        if (_state == AlertState.Suspicious || _state == AlertState.Investigating) Play("guarda_desiste", VoicePitch);
                        break;
                }
            }
            if (hunting && !_hunting) Play("guarda_caca", VoicePitch);
            _state = state;
            _hunting = hunting;

            // resmungos de quem patrulha sossegado
            if (state == AlertState.Patrolling)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f)
                {
                    Play("guarda_ocioso", VoicePitch);
                    _idleTimer = Random.Range(idleInterval.x, idleInterval.y);
                }
            }

            // ataque: o som da preparação dura o mesmo que o aviso (lança 0,45 s, mira 0,9 s)
            var phase = _ai.CurrentAttackPhase;
            if (phase != _phase)
            {
                if (Ranged)
                {
                    if (phase == EnemyAI.AttackPhase.Windup) { Play("besta_mira"); _locked = false; }
                    else if (phase == EnemyAI.AttackPhase.Active) Play("besta_disparo");
                    else if (phase == EnemyAI.AttackPhase.Recovery) Play("besta_recarga");
                }
                else
                {
                    if (phase == EnemyAI.AttackPhase.Windup) Play("lanca_prepara");
                    else if (phase == EnemyAI.AttackPhase.Active) Play("lanca_golpe", VoicePitch);
                }
                _phase = phase;
            }
            bool locked = Ranged && _ai.AimLocked;
            if (locked && !_locked) Play("besta_trava");
            _locked = locked;

            // passos de armadura
            float speed = _body != null ? Mathf.Abs(_body.linearVelocity.x) : 0f;
            if (speed > 0.3f)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    Play("guarda_passo");
                    _stepTimer = speed > 3.5f ? 0.27f : 0.42f;
                }
            }
            else _stepTimer = 0.1f;
        }
    }
}
