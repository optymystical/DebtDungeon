using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Worn armor. Absorbs Physical damage up to its Limit per hit; once Durability hits 0 it's broken
    /// until repaired at the spawn-chunk station. Poison and Magic go straight through.
    /// </summary>
    public class ArmorAbsorber : EquipmentAbsorber
    {
        /// <summary>Starts at full Durability from the armor's data.</summary>
        public ArmorAbsorber(ArmorDefinition armor)
            : base(armor.Durability, armor.Limit)
        {
            Armor = armor;
        }

        /// <summary>The armor this pool belongs to.</summary>
        public ArmorDefinition Armor { get; }

        /// <summary>Back to full Durability (the station's repair).</summary>
        public void Repair()
        {
            RestoreDurability(MaxDurability);
        }

        /// <inheritdoc/>
        protected override bool Blocks(DamageType type)
        {
            return type == DamageType.Physical;
        }
    }
}
