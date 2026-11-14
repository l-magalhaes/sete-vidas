using System;
using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>
    /// Classes desbloqueadas e classe ativa (spec: Docs/Design/classes-paladino.md).
    /// R/Q (ou ombros do controle) giram entre as desbloqueadas; 1–7 escolhem direto. Classe bloqueada: nada
    /// acontece. Não troca no meio de um ataque; no ar, escalando ou pendurado pode.
    /// Ao trocar, liga as <see cref="ClassAbility"/> da nova classe e desliga as outras.
    /// </summary>
    public class ClassManager : MonoBehaviour
    {
        [Tooltip("Uma definição por classe já criada (a ordem não importa)")]
        [SerializeField] ClassDefinition[] classes = new ClassDefinition[0];
        [SerializeField] PlayerInputReader input;
        [SerializeField] CombatController combat;

        PlayerHealth _health;

        public static ClassManager Active { get; private set; }

        public ClassDefinition Current { get; private set; }
        public CharacterClass CurrentClass => Current != null ? Current.Class : CharacterClass.Rogue;

        /// <summary>(anterior, nova). Disparado em cada troca, não na carga inicial.</summary>
        public event Action<ClassDefinition, ClassDefinition> ClassChanged;
        /// <summary>Uma classe foi desbloqueada (vínculo).</summary>
        public event Action<ClassDefinition> ClassUnlocked;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (combat == null) combat = GetComponent<CombatController>();
            _health = GetComponent<PlayerHealth>();
        }

        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        void Start()
        {
            // A classe ativa vem do save (o SaveStore já garante que ela está desbloqueada)
            var saved = Enum.TryParse(SaveManager.Data.activeClass, out CharacterClass c) ? c : CharacterClass.Rogue;
            Apply(Definition(saved) ?? Definition(CharacterClass.Rogue));
        }

        void Update()
        {
            if (input == null || !input.isActiveAndEnabled) return;
            if (SeteVidas.UI.ClassWheel.IsOpen) { _holdDir = 0; return; }   // a roda cuida até soltar

            // R/Q (RB/LB): um toque alterna; segurar abre a roda de classes (14/11/2026)
            if (input.NextClassPressedThisFrame) { _holdDir = 1; _holdTime = 0f; }
            else if (input.PrevClassPressedThisFrame) { _holdDir = -1; _holdTime = 0f; }
            else
            {
                int slot = input.ClassSlotPressedThisFrame;
                if (ClassRules.FromSlot(slot, out var chosen)) TrySwitch(chosen);
            }

            if (_holdDir == 0) return;
            bool held = _holdDir > 0 ? input.NextClassHeldRaw : input.PrevClassHeldRaw;
            _holdTime += Time.unscaledDeltaTime;
            if (!held)
            {
                if (_holdTime < ClassRules.WheelHoldTime) TryCycle(_holdDir);
                _holdDir = 0;
            }
            else if (_holdTime >= ClassRules.WheelHoldTime && UnlockedCount() >= 2 && !input.GameplayLocked)
            {
                SeteVidas.UI.ClassWheel.Open(this, input, _holdDir);
                _holdDir = 0;
            }
        }

        int _holdDir;
        float _holdTime;

        int UnlockedCount()
        {
            int n = 0;
            for (int i = 0; i < ClassRules.ClassCount; i++) if (IsAvailable((CharacterClass)i)) n++;
            return n;
        }

        public ClassDefinition Definition(CharacterClass c)
        {
            foreach (var def in classes)
                if (def != null && def.Class == c) return def;
            return null;
        }

        /// <summary>Desbloqueada no save e com definição criada.</summary>
        public bool IsAvailable(CharacterClass c) => Definition(c) != null && SaveManager.Data.IsUnlocked(c);

        public bool CanSwitchNow => ClassRules.CanSwitch(combat != null && combat.IsAttacking, _health != null && _health.IsDead);

        /// <summary>Troca para <paramref name="c"/>. Retorna false se já é a ativa, está bloqueada ou o gato está atacando.</summary>
        public bool TrySwitch(CharacterClass c)
        {
            if (Current != null && c == Current.Class) return false;
            if (!IsAvailable(c) || !CanSwitchNow) return false;

            var previous = Current;
            Apply(Definition(c));
            SaveManager.Data.activeClass = c.ToString(); // vai para o disco no próximo salvamento
            ClassChanged?.Invoke(previous, Current);
            return true;
        }

        public bool TryCycle(int dir)
        {
            return ClassRules.TryCycle(CurrentClass, dir, IsAvailable, out var next) && TrySwitch(next);
        }

        /// <summary>Desbloqueia para sempre (vínculo) e grava o save na hora.</summary>
        public void Unlock(CharacterClass c)
        {
            var data = SaveManager.Data;
            if (data.IsUnlocked(c)) return;
            data.unlockedClasses.Add(c.ToString());
            SaveManager.Save();
            ClassUnlocked?.Invoke(Definition(c));
        }

        void Apply(ClassDefinition def)
        {
            if (def == null) return;
            Current = def;
            var abilities = GetComponents<ClassAbility>();
            foreach (var a in abilities) if (a.ForClass != def.Class) a.enabled = false;
            foreach (var a in abilities)
            {
                if (a.ForClass != def.Class) continue;
                a.enabled = true;
                a.OnClassActivated();
            }
        }
    }
}
