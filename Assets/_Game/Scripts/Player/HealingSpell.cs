using System;
using System.Collections;
using SeteVidas.Classes;
using SeteVidas.Combat;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Magia de cura, aprendida com o Paladino (Koda): F (ou LT) cura 1 coração. Tem 3 usos, guardados no save, e
    /// só recarrega ao descansar num santuário. Vale em qualquer classe depois de desbloqueada.
    /// Uma concentração curta no chão (~0,45 s): tomar dano no meio interrompe sem gastar o uso; com a vida cheia
    /// não gasta. Spec: Docs/Design/magia-cura.md.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class HealingSpell : MonoBehaviour
    {
        public const int DefaultCharges = SaveData.MaxHealCharges;

        [Tooltip("Usos até descansar no santuário")]
        [SerializeField, Min(1)] int maxCharges = DefaultCharges;
        [Tooltip("Hits curados por uso (1 coração = 2 hits)")]
        [SerializeField, Min(1)] int hitsPerCast = 2;
        [Tooltip("Tempo de concentração antes de curar (s)")]
        [SerializeField, Min(0f)] float castTime = 0.45f;
        [Tooltip("Classe que ensina a magia")]
        [SerializeField] CharacterClass unlockedBy = CharacterClass.Paladin;

        PlayerHealth _health;
        PlayerController _controller;
        PlayerInputReader _input;
        CombatController _combat;
        PlayerVisual _visual;
        bool _interrupted;

        public int MaxCharges => maxCharges;
        /// <summary>Usos que restam (vêm do save).</summary>
        public int Charges => Mathf.Clamp(SaveManager.Data.healCharges, 0, maxCharges);
        public bool IsUnlocked => SaveManager.Data.IsUnlocked(unlockedBy);
        public bool IsCasting { get; private set; }

        /// <summary>Usos mudaram (HUD).</summary>
        public event Action ChargesChanged;

        void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputReader>();
            _combat = GetComponent<CombatController>();
            _visual = GetComponentInChildren<PlayerVisual>();
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            var classes = GetComponent<ClassManager>();
            if (classes != null) classes.ClassUnlocked += OnClassUnlocked;
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            var classes = GetComponent<ClassManager>();
            if (classes != null) classes.ClassUnlocked -= OnClassUnlocked;
        }

        void Update()
        {
            if (_input != null && _input.isActiveAndEnabled && _input.HealPressedThisFrame) TryCast();
        }

        /// <summary>Por que não dá para curar agora (null = dá). Útil para testes e para o HUD.</summary>
        public string BlockReason()
        {
            if (!IsUnlocked) return "bloqueada";
            if (IsCasting) return "já curando";
            if (Charges <= 0) return "sem usos";
            if (_health.IsDead || _health.IsCollapsed) return "sem condições";
            if (_health.CurrentHits >= _health.MaxHits) return "vida cheia";
            if (_input != null && _input.GameplayLocked) return "controle travado";
            if (_combat != null && _combat.IsAttacking) return "atacando";
            if (_controller != null && (!_controller.IsGrounded || _controller.IsHanging || _controller.IsClimbingLedge)) return "fora do chão";
            return null;
        }

        /// <summary>Começa a cura. Retorna false se não pode agora (ver <see cref="BlockReason"/>).</summary>
        public bool TryCast()
        {
            if (BlockReason() != null) return false;
            StartCoroutine(Cast());
            return true;
        }

        IEnumerator Cast()
        {
            IsCasting = true;
            _interrupted = false;
            if (_input != null) _input.SetLock(this, true);
            if (_controller != null) _controller.Body.linearVelocity = new Vector2(0f, _controller.Body.linearVelocity.y);
            if (_visual != null) { _visual.PlayAction("heal", hold: true); _visual.HealFx(false); }

            float t = 0f;
            while (t < castTime && !_interrupted && !_health.IsDead)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (!_interrupted && !_health.IsDead && _health.Heal(hitsPerCast))
            {
                SaveManager.Data.healCharges = Charges - 1;   // vai para o disco no próximo salvamento
                if (_visual != null) _visual.HealFx(true);
                ChargesChanged?.Invoke();
            }

            if (_visual != null) _visual.StopAction();
            if (_input != null) _input.SetLock(this, false);
            IsCasting = false;
        }

        /// <summary>Recarrega todos os usos (descanso no santuário).</summary>
        public void Refill()
        {
            if (SaveManager.Data.healCharges == maxCharges) return;
            SaveManager.Data.healCharges = maxCharges;
            ChargesChanged?.Invoke();
        }

        void OnDamaged(DamageInfo info)
        {
            if (IsCasting) _interrupted = true;
        }

        void OnClassUnlocked(ClassDefinition def)
        {
            if (def != null && def.Class == unlockedBy) Refill(); // aprende a magia já com os 3 usos
        }
    }
}
