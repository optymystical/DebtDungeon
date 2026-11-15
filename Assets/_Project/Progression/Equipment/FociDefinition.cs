using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>Foci type. Channeler scales with Reach, Igniter with Grasp. (Multi, the lower of the two, is Beta.)</summary>
    public enum FociType
    {
        Channeler = 0,
        Igniter = 1
    }

    /// <summary>
    /// A Magic Foci (Scroll, Wand). It gives the basic spell-bolt attack, and its stat drives spells and the Radiation tree.
    /// Free Offensive slots hold Affixes that cost no burden.
    /// </summary>
    [CreateAssetMenu(fileName = "Foci_New", menuName = "Debt Dungeon/Equipment/Foci")]
    public class FociDefinition : EquipmentDefinition
    {
        [Header("Foci")]
        [SerializeField] private FociType _fociType = FociType.Channeler;

        [Tooltip("Offensive Affixes that can sit on this Foci without costing burden.")]
        [SerializeField, Min(0)] private int _freeOffensiveSlots;

        // TODO(Chris): Scroll's cheaper abilities and Wand's cheaper every-other cast wait for Player's energy system.

        /// <summary>Channeler or Igniter.</summary>
        public FociType FociType => _fociType;

        /// <summary>Offensive Affixes that cost no burden on this Foci.</summary>
        public int FreeOffensiveSlots => _freeOffensiveSlots;

        /// <summary>Igniters are light (quick bolts), Channelers are heavy (slower, bigger bolts).</summary>
        public AttackWeight Weight => _fociType == FociType.Igniter ? AttackWeight.Light : AttackWeight.Heavy;

        /// <summary>The stat spells scale with: Reach for Channeler, Grasp for Igniter.</summary>
        public StatType ScalingStat => _fociType == FociType.Channeler ? StatType.Reach : StatType.Grasp;

        /// <inheritdoc/>
        public override EquipmentSlot Slot => EquipmentSlot.Foci;

        /// <inheritdoc/>
        public override Archetype Archetype => Archetype.Magic;
    }
}
