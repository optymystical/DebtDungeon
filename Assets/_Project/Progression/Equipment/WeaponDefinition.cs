using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A Martial weapon (Sword, Greatsword). Its weight decides the stat it scales with:
    /// Light uses Dexterity, Heavy uses Strength (Flow State weapon scaling).
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_New", menuName = "Debt Dungeon/Equipment/Weapon")]
    public class WeaponDefinition : EquipmentDefinition
    {
        [Header("Weapon")]
        [Tooltip("Light: fast attacks (Dexterity). Heavy: slower, stronger, bigger hitbox (Strength).")]
        [SerializeField] private AttackWeight _weight = AttackWeight.Light;

        /// <summary>Light or Heavy.</summary>
        public AttackWeight Weight => _weight;

        /// <summary>The stat this weapon and its skills scale with: Dexterity for Light, Strength for Heavy.</summary>
        public StatType ScalingStat => _weight == AttackWeight.Light ? StatType.Dexterity : StatType.Strength;

        /// <inheritdoc/>
        public override EquipmentSlot Slot => EquipmentSlot.Weapon;

        /// <inheritdoc/>
        public override Archetype Archetype => Archetype.Martial;
    }
}
