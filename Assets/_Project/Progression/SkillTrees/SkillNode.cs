using System;
using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// One step of a skill tree. It grants one or more abilities; an upgrade node also replaces an earlier
    /// ability from the same tree (Crackle II replaces Crackle). Lives inside a SkillTreeDefinition asset.
    /// </summary>
    [Serializable]
    public class SkillNode
    {
        [Tooltip("Abilities this node gives. Usually one; passives are abilities too.")]
        [SerializeField] private List<AbilityDefinition> _grantedAbilities = new List<AbilityDefinition>();

        [Tooltip("Upgrade nodes only: the earlier ability in this tree that this node replaces. Leave empty otherwise.")]
        [SerializeField] private AbilityDefinition _replacedAbility;

        /// <summary>Empty node, used by Unity when it loads the asset.</summary>
        public SkillNode()
        {
        }

        /// <summary>Builds a node in code (tests and editor tools). Pass null for replacedAbility on a normal node.</summary>
        public SkillNode(List<AbilityDefinition> grantedAbilities, AbilityDefinition replacedAbility)
        {
            _grantedAbilities = grantedAbilities;
            _replacedAbility = replacedAbility;
        }

        /// <summary>Abilities this node gives.</summary>
        public IReadOnlyList<AbilityDefinition> GrantedAbilities => _grantedAbilities;

        /// <summary>The ability this node replaces, or null if it isn't an upgrade.</summary>
        public AbilityDefinition ReplacedAbility => _replacedAbility;

        /// <summary>True if this node replaces an earlier ability.</summary>
        public bool IsUpgrade => _replacedAbility != null;
    }
}
