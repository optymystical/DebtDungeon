using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>Armor weight. Light costs 1 burden step, Medium costs 2.</summary>
    public enum ArmorWeight
    {
        Light = 0,
        Medium = 1
    }

    /// <summary>
    /// Martial armor (Leather Coat). Each hit it absorbs Physical damage up to its Limit and loses that much Durability;
    /// at 0 it's broken until repaired at the station. Poison and Magic go through.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor_New", menuName = "Debt Dungeon/Equipment/Armor")]
    public class ArmorDefinition : EquipmentDefinition
    {
        private const int LightArmorBurden = 1;
        private const int MediumArmorBurden = 2;

        [Header("Armor")]
        [SerializeField] private ArmorWeight _weight = ArmorWeight.Medium;

        [Tooltip("Total damage the armor can absorb before it breaks.")]
        [SerializeField, Min(1)] private int _durability = 40;

        [Tooltip("Most damage it can absorb from a single hit.")]
        [SerializeField, Min(1)] private int _limit = 8;

        /// <summary>Light or Medium.</summary>
        public ArmorWeight Weight => _weight;

        /// <summary>Total damage absorbed before breaking.</summary>
        public int Durability => _durability;

        /// <summary>Most damage absorbed per hit.</summary>
        public int Limit => _limit;

        /// <inheritdoc/>
        public override EquipmentSlot Slot => EquipmentSlot.Armor;

        /// <inheritdoc/>
        public override Archetype Archetype => Archetype.Martial;

        /// <summary>Light armor costs 1 burden step, Medium costs 2.</summary>
        public override int BurdenSteps => _weight == ArmorWeight.Light ? LightArmorBurden : MediumArmorBurden;
    }
}
