using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A Magic Shroud (Bastion). Like armor, but it blocks Physical and Magic and has no burden. Taking damage pauses its
    /// recovery for RegenDelay seconds; then it recovers quickly, which rewards hit-and-run play (decided Nov 12).
    /// Poison goes through. Free Defensive slots hold Affixes that cost no burden.
    /// </summary>
    [CreateAssetMenu(fileName = "Shroud_New", menuName = "Debt Dungeon/Equipment/Shroud")]
    public class ShroudDefinition : EquipmentDefinition
    {
        [Header("Shroud")]
        [Tooltip("Total damage the Shroud can absorb before it's down.")]
        [SerializeField, Min(1)] private int _durability = 100;

        [Tooltip("Most damage it can absorb from a single hit.")]
        [SerializeField, Min(1)] private int _limit = 20;

        [Tooltip("Durability restored per second while regenerating. Flow State: a Shroud recovers its Limit each turn; here 1 turn is about 1 second.")]
        [SerializeField, Min(0f)] private float _regenPerSecond = 20f;

        [Tooltip("Seconds after the last damage before regeneration starts. Long enough to reward stepping out of a fight (hit-and-run).")]
        [SerializeField, Min(0f)] private float _regenDelay = 2.5f;

        [Tooltip("Defensive Affixes that can sit on this Shroud without costing burden.")]
        [SerializeField, Min(0)] private int _freeDefensiveSlots = 1;

        /// <summary>Total damage absorbed before it's down.</summary>
        public int Durability => _durability;

        /// <summary>Most damage absorbed per hit.</summary>
        public int Limit => _limit;

        /// <summary>Durability restored per second while regenerating.</summary>
        public float RegenPerSecond => _regenPerSecond;

        /// <summary>Seconds after the last hit before regeneration starts.</summary>
        public float RegenDelay => _regenDelay;

        /// <summary>Defensive Affixes that cost no burden on this Shroud.</summary>
        public int FreeDefensiveSlots => _freeDefensiveSlots;

        /// <inheritdoc/>
        public override EquipmentSlot Slot => EquipmentSlot.Shroud;

        /// <inheritdoc/>
        public override Archetype Archetype => Archetype.Magic;
    }
}
