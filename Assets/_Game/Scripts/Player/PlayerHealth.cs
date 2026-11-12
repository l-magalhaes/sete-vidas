using System;
using System.Collections;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Vida do gato (GDD §14: cada coração = 2 hits). Ao tomar dano: recuo e invencibilidade piscando.
    /// Em 0 hits: animação de morte, tela escurece, volta ao último santuário com vida cheia e o mundo
    /// reinicia (inimigos voltam). Corações e classes nunca se perdem. Spec: Docs/Design/vida-morte-save.md.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        PlayerController _player;
        PlayerInputReader _input;
        float _invulnerableUntil = -1f;
        Vector2 _sceneStart;

        /// <summary>O gato atual (o HUD e os santuários leem daqui).</summary>
        public static PlayerHealth Active { get; private set; }

        public int HeartsRecovered { get; private set; } = 1;
        public int CurrentHits { get; private set; }
        public int MaxHits => HealthRules.MaxHits(HeartsRecovered);
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        /// <summary>Caído depois da maldição do tomo (sem controle, deitado), até o Golden o salvar.</summary>
        public bool IsCollapsed { get; private set; }

        /// <summary>Vida ou corações mudaram (HUD).</summary>
        public event Action Changed;
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action Respawned;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputReader>();
        }

        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        void Start()
        {
            _sceneStart = _player.Body.position;
            HeartsRecovered = SaveManager.Data.heartsRecovered;
            CurrentHits = MaxHits;

            // Continuar de onde salvou: se o último santuário é desta cena, começa nele
            if (TryGetSavedSanctuary(out var sanctuary)) _player.Teleport(sanctuary.SpawnPoint);
            var safeStart = GetComponent<SafeGroundTracker>();
            if (safeStart != null) safeStart.ResetTo(_player.Body.position);
            Changed?.Invoke();
        }

        /// <summary>Sem dano e sem piscar (cenas em que o gato fica de pé parado, ex.: a tela escurecendo no santuário).</summary>
        public bool Shielded { get; set; }

        float _pogoGraceUntil = -1f;

        /// <summary>Acabou de quicar num espinho ou numa serra com a estocada para baixo: a armadilha não machuca (melhoria 5).</summary>
        public bool InPogoGrace => Time.time < _pogoGraceUntil;

        public void GrantPogoGrace(float seconds) => _pogoGraceUntil = Mathf.Max(_pogoGraceUntil, Time.time + seconds);

        // ------------------------------------------------------------- dano

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || IsCollapsed || IsInvulnerable || Shielded || info.Amount <= 0) return;
            var s = _player.Settings;

            CurrentHits = HealthRules.ApplyDamage(CurrentHits, info.Amount);
            _invulnerableUntil = Time.time + s.InvulnerabilityTime;

            float dir = info.Direction.x >= 0f ? 1f : -1f; // para longe do atacante
            float vy = s.LaunchVelocityFor(s.KnockbackHeight, Time.fixedDeltaTime);
            _player.ApplyKnockback(new Vector2(dir * s.KnockbackSpeed, vy), s.KnockbackControlLock);

            DamagePopup.Spawn(transform.position + Vector3.up * 1.8f, $"-{info.Amount} hit", new Color32(0xff, 0x6b, 0x6b, 0xff));
            Damaged?.Invoke(info);
            Changed?.Invoke();

            if (HealthRules.IsDead(CurrentHits)) StartCoroutine(DeathRoutine());
        }

        /// <summary>
        /// Cura <paramref name="hits"/> (meio coração = 1), sem passar do máximo. Retorna false se não curou nada
        /// (morto ou já com vida cheia): quem chamou decide se o item continua no chão.
        /// </summary>
        public bool Heal(int hits)
        {
            if (IsDead || hits <= 0 || CurrentHits >= MaxHits) return false;
            int before = CurrentHits;
            CurrentHits = HealthRules.Heal(CurrentHits, hits, HeartsRecovered);
            DamagePopup.Spawn(transform.position + Vector3.up * 1.8f, $"+{CurrentHits - before} hit", new Color32(0x7d, 0xe0, 0x7a, 0xff));
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Dano de espinho: tira o hit sem recuo, escurece a tela rapidinho e devolve o gato ao último chão seguro
        /// (<see cref="SafeGroundTracker"/>). Se for o último hit, é morte normal.
        /// </summary>
        public void TakeHazardDamage(in DamageInfo info)
        {
            if (IsDead || IsCollapsed || IsInvulnerable || info.Amount <= 0) return;
            CurrentHits = HealthRules.ApplyDamage(CurrentHits, info.Amount);
            _invulnerableUntil = Time.time + _player.Settings.InvulnerabilityTime;
            DamagePopup.Spawn(transform.position + Vector3.up * 1.8f, $"-{info.Amount} hit", new Color32(0xff, 0x6b, 0x6b, 0xff));
            Damaged?.Invoke(info);
            Changed?.Invoke();

            if (HealthRules.IsDead(CurrentHits)) StartCoroutine(DeathRoutine());
            else StartCoroutine(ReturnToSafeGround());
        }

        IEnumerator ReturnToSafeGround()
        {
            if (_input != null) _input.SetLock(this, true);
            _player.Body.linearVelocity = Vector2.zero;
            var fader = ScreenFader.Active;
            if (fader != null) yield return fader.FadeTo(1f, 0.15f);
            else yield return new WaitForSeconds(0.15f);

            var tracker = GetComponent<SafeGroundTracker>();
            _player.Teleport(tracker != null ? tracker.LastSafe : _sceneStart);
            yield return null;

            if (fader != null) yield return fader.FadeTo(0f, 0.15f);
            if (_input != null) _input.SetLock(this, false);
        }

        /// <summary>Vida cheia (descansar no santuário).</summary>
        public void RestoreFull()
        {
            if (IsDead) return;
            CurrentHits = MaxHits;
            Changed?.Invoke();
        }

        // ------------------------------------------------------------ morte

        IEnumerator DeathRoutine()
        {
            IsDead = true;
            if (_input != null) _input.enabled = false; // sem controle enquanto morre
            Died?.Invoke();

            var s = _player.Settings;
            yield return new WaitForSeconds(s.DeathAnimationTime);

            var fader = ScreenFader.Active;
            if (fader != null) yield return fader.FadeTo(1f, s.DeathFadeTime);

            Respawn();
            yield return null;

            if (fader != null) yield return fader.FadeTo(0f, s.DeathFadeTime);
            if (_input != null) _input.enabled = true;
        }

        void Respawn()
        {
            _player.Teleport(TryGetSavedSanctuary(out var sanctuary) ? sanctuary.SpawnPoint : _sceneStart);
            var safe = GetComponent<SafeGroundTracker>();
            if (safe != null) safe.ResetTo(_player.Body.position);
            CurrentHits = MaxHits;
            _invulnerableUntil = -1f;
            IsDead = false;
            GameEvents.RaiseWorldReset(); // inimigos voltam (decisão de 09/11/2026)
            Respawned?.Invoke();
            Changed?.Invoke();
        }

        bool TryGetSavedSanctuary(out Sanctuary sanctuary)
        {
            sanctuary = null;
            var data = SaveManager.Data;
            if (string.IsNullOrEmpty(data.lastSanctuaryId)) return false;
            if (data.scene != gameObject.scene.name) return false;
            sanctuary = Sanctuary.Find(data.lastSanctuaryId);
            return sanctuary != null;
        }

        // ---------------------------------------------------------- maldição

        /// <summary>
        /// Muda os corações recuperados (maldição do tomo: 7 → 1). <paramref name="save"/> grava na hora.
        /// A vida atual nunca passa do novo máximo.
        /// </summary>
        public void SetHeartsRecovered(int hearts, bool save)
        {
            var data = SaveManager.Data;
            data.heartsRecovered = HealthRules.ClampHearts(hearts);
            if (save) SaveManager.Save();
            HeartsRecovered = data.heartsRecovered;
            CurrentHits = Mathf.Min(CurrentHits, MaxHits);
            Changed?.Invoke();
        }

        /// <summary>Maldição completa (atalho para testes e para a cena do tomo): sobra <paramref name="remainingHearts"/> e meio coração de vida.</summary>
        /// <summary>Marca no save: a maldição terminou (o HUD passa a mostrar o coração de cada classe; antes, os 7 são do Ladino).</summary>
        public const string CurseDoneFlag = "maldicao_completa";

        public void ApplyCurse(int remainingHearts = 1)
        {
            SaveManager.Data.SetFlag(CurseDoneFlag);
            SetHeartsRecovered(remainingHearts, save: true);
            CurrentHits = 1; // quase morto
            Changed?.Invoke();
        }

        /// <summary>
        /// Caído (deitado, sem dano): cena da maldição e o cochilo na caixinha de areia do santuário. Ao levantar, o
        /// visual toca "get_up" e o gato fica <paramref name="graceAfter"/> s invulnerável (0 = sem piscar).
        /// </summary>
        public void SetCollapsed(bool collapsed, float graceAfter = 1f)
        {
            IsCollapsed = collapsed;
            if (collapsed) _invulnerableUntil = float.MaxValue;
            else _invulnerableUntil = Time.time + graceAfter;
        }

        // ------------------------------------------------------- depuração

        /// <summary>Recupera um coração (o vínculo da Fase 5 fará isso de verdade). Também grava o save.</summary>
        [ContextMenu("Depuração: +1 coração")]
        public void AddHeart()
        {
            var data = SaveManager.Data;
            data.heartsRecovered = HealthRules.ClampHearts(data.heartsRecovered + 1);
            SaveManager.Save();
            HeartsRecovered = data.heartsRecovered;
            CurrentHits = MaxHits;
            Changed?.Invoke();
        }

        [ContextMenu("Depuração: tomar 1 hit")]
        void DebugHit() => TakeDamage(new DamageInfo(1, Vector2.right, DamageType.Normal, AttackAngle.Front, null));
    }
}
