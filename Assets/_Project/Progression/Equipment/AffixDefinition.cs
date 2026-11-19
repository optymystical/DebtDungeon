using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A Magic Affix gem (Agate). It's attuned in one of two forms when equipped: Offensive (changes spells) or
    /// Defensive (changes the Shroud). The form is chosen in the loadout, not stored here.
    /// </summary>
    [CreateAssetMenu(fileName = "Affix_New", menuName = "Debt Dungeon/Equipment/Affix")]
    public class AffixDefinition : EquipmentDefinition
    {
        [Header("Affix Forms")]
        [Tooltip("What the gem does in its Offensive form (on spells).")]
        [SerializeField, TextArea] private string _offensiveEffect;

        [Tooltip("What the gem does in its Defensive form (on the Shroud).")]
        [SerializeField, TextArea] private string _defensiveEffect;

        // TODO(Chris): the effects are text for now; they become real once spells and Shroud hooks exist.

        /// <summary>Description of the Offensive form.</summary>
        public string OffensiveEffect => _offensiveEffect;

        /// <summary>Description of the Defensive form.</summary>
        public string DefensiveEffect => _defensiveEffect;

        /// <inheritdoc/>
        public override EquipmentSlot Slot => EquipmentSlot.Affix;

        /// <inheritdoc/>
        public override Archetype Archetype => Archetype.Magic;
    }
}
