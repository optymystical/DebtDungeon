using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// One row of the Owned Abilities panel: an ability bought from a tree, and whether it works right now.
    /// Inactive means the tree's equipment isn't worn; InactiveReason says what's missing.
    /// </summary>
    public readonly struct OwnedSkillAbility
    {
        public OwnedSkillAbility(AbilityDefinition ability, SkillTreeDefinition tree, bool isActive, string inactiveReason)
        {
            Ability = ability;
            Tree = tree;
            IsActive = isActive;
            InactiveReason = inactiveReason;
        }

        /// <summary>The ability.</summary>
        public AbilityDefinition Ability { get; }

        /// <summary>The tree it came from.</summary>
        public SkillTreeDefinition Tree { get; }

        /// <summary>True if its tree's equipment is worn.</summary>
        public bool IsActive { get; }

        /// <summary>e.g. "Needs a Bladed weapon". Null when active.</summary>
        public string InactiveReason { get; }
    }
}
