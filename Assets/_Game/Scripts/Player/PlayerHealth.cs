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
            Changed?.Invoke();
        }

        // ------------------------------------------------------------- dano

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || IsInvulnerable || info.Amount <= 0) return;
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
