using SeteVidas.Enemies.Boss;
using UnityEngine;
using State = SeteVidas.Enemies.Boss.GoldenBoss.State;

namespace SeteVidas.Audio
{
    /// <summary>
    /// Os sons de Koda, o Dourado (Docs/Audio/plano-sfx.md §5): cada golpe tem o som da preparação com o mesmo
    /// tempo do aviso visual (marreta erguida, agachar do salto, marreta ao céu), o impacto, a cura, o escudo, a
    /// fúria da 2ª fase, o dano e a derrota. Só observa o <see cref="GoldenBoss"/>; ele mesmo o adiciona.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossAudio : MonoBehaviour
    {
        GoldenBoss _boss;
        int _health;
        bool _secondPhase;
        float _stepTimer;
        SfxLoop _healLoop;

        Vector2 Pos => (Vector2)transform.position + Vector2.up * 1.5f;

        void Awake() => _boss = GetComponent<GoldenBoss>();

        void OnEnable()
        {
            if (_boss != null) _boss.StateChanged += OnState;
        }

        void OnDisable()
        {
            if (_boss != null) _boss.StateChanged -= OnState;
            StopHeal();
        }

        void Start()
        {
            if (_boss == null) { enabled = false; return; }
            _health = _boss.Health;
        }

        void OnState(State from, State to)
        {
            if (from == State.Heal) StopHeal();
            if (from == State.Dormant && to != State.Defeated && to != State.Companion) { Sfx.Play("ui_barra_chefe"); _secondPhase = false; }

            switch (to)
            {
                case State.SwingWindup:
                    // emendando outra marretada (combo): a preparação é mais curta e mais aguda
                    Sfx.Play("koda_ergue", Pos, 1f, from == State.SwingRecover ? 1.12f : 1f);
                    break;
                case State.SwingActive: Sfx.Play("koda_impacto", Pos); break;
                case State.LeapWindup: Sfx.Play("koda_salto_agacha", Pos); break;
                case State.LeapAir: Sfx.Play("koda_salto_ar", Pos); break;
                case State.LeapRecover: Sfx.Play("koda_salto_queda", Pos); break;
                case State.Heal: _healLoop = Sfx.Loop("koda_cura_loop", transform); break;
                case State.Stagger:
                    Sfx.Play("koda_cura_quebra", Pos);
                    Sfx.Play("koda_atordoado", Pos);
                    break;
                case State.LightningCast:
                    Sfx.Play("koda_raio_ergue", Pos);
                    Sfx.Play("escudo_liga", Pos);
                    break;
                case State.Defeated:
                    if (from != State.Dormant) Sfx.Play("koda_derrota", Pos);
                    break;
                // repulsa sagrada (gato em cima da cabeça): a marreta sobe aguda, a luz estala
                case State.RepelWindup: Sfx.Play("koda_ergue", Pos, 1f, 1.25f); break;
                case State.RepelActive:
                    Sfx.Play("escudo_liga", Pos);
                    Sfx.Play("koda_impacto", Pos, 0.8f, 1.2f);
                    break;
            }
            if (from == State.Lightning && to != State.Lightning) Sfx.Play("escudo_desliga", Pos);
        }

        void StopHeal()
        {
            if (_healLoop != null) _healLoop.Stop(0.3f);
            _healLoop = null;
        }

        void Update()
        {
            if (_boss == null || Time.timeScale <= 0f) return;

            if (_boss.IsFighting)
            {
                if (_boss.Health < _health) Sfx.Play("koda_dano", Pos);
                bool second = _boss.InSecondPhase;
                if (second && !_secondPhase) Sfx.Play("koda_furioso", Pos);
                _secondPhase = second;
            }
            _health = _boss.Health;

            if (_boss.Current == State.Walk)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f) { Sfx.Play("koda_passo", Pos); _stepTimer = 0.38f; }
            }
            else _stepTimer = 0.1f;
        }
    }
}
