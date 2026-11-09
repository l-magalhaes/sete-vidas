using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>Arma corpo a corpo da classe: ao ser ligada, equipa a arma do ClassDefinition no CombatController.</summary>
    public class MeleeWeaponAbility : ClassAbility
    {
        [SerializeField] CombatController combat;
        [SerializeField] ClassManager classes;

        void Awake()
        {
            if (combat == null) combat = GetComponent<CombatController>();
            if (classes == null) classes = GetComponent<ClassManager>();
        }

        public override void OnClassActivated()
        {
            if (combat == null || classes == null) return;
            var def = classes.Definition(ForClass);
            if (def != null && def.Weapon != null) combat.Equip(def.Weapon);
        }
    }
}
