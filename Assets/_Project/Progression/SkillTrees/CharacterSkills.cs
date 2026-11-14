using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Which skill tree nodes a character has bought. Stores one number per tree: how many nodes are unlocked.
    /// Only the next node in a tree can be bought, so nodes always unlock in order. Bought nodes are never refunded,
    /// even if the tree's equipment is taken off.
    /// </summary>
    public class CharacterSkills
    {
        private readonly Archetype _archetype;
        private readonly CharacterProgress _progress;

        // Tree asset -> nodes unlocked in it. Trees with nothing bought aren't in here.
        private readonly Dictionary<SkillTreeDefinition, int> _unlockedCounts = new Dictionary<SkillTreeDefinition, int>();

        public CharacterSkills(Archetype archetype, CharacterProgress progress)
        {
            _archetype = archetype;
            _progress = progress;
        }

        /// <summary>Raised after a node is bought. The ability granter listens to this.</summary>
        public event Action SkillsChanged;

        /// <summary>Trees with at least one node bought.</summary>
        public IEnumerable<SkillTreeDefinition> OwnedTrees => _unlockedCounts.Keys;

        /// <summary>How many nodes of this tree are bought (0 if none).</summary>
        public int GetUnlockedCount(SkillTreeDefinition tree)
        {
            if (tree != null && _unlockedCounts.TryGetValue(tree, out int count))
            {
                return count;
            }

            return 0;
        }

        /// <summary>The abilities this character owns from one tree, with upgrades already applied.</summary>
        public List<AbilityDefinition> GetOwnedAbilities(SkillTreeDefinition tree)
        {
            return tree.GetAbilities(GetUnlockedCount(tree));
        }

        /// <summary>
        /// How one node should look: Owned if bought, Next if it's the next one to buy, Locked otherwise.
        /// nodeIndex counts from 0.
        /// </summary>
        public SkillNodeState GetNodeState(SkillTreeDefinition tree, int nodeIndex)
        {
            int unlocked = GetUnlockedCount(tree);

            if (nodeIndex < unlocked)
            {
                return SkillNodeState.Owned;
            }

            return nodeIndex == unlocked ? SkillNodeState.Next : SkillNodeState.Locked;
        }

        /// <summary>
        /// Every ability bought from any tree, each marked active or inactive for this loadout. Feeds the
        /// Owned Abilities panel. (The AbilityGranter decides what is actually granted; this is for display.)
        /// </summary>
        public List<OwnedSkillAbility> GetOwnedAbilities(Loadout loadout)
        {
            List<OwnedSkillAbility> rows = new List<OwnedSkillAbility>();

            foreach (SkillTreeDefinition tree in OwnedTrees)
            {
                bool isActive = tree.IsUsableWith(loadout);
                string reason = isActive ? null : tree.GetEquipmentNote();

                foreach (AbilityDefinition ability in GetOwnedAbilities(tree))
                {
                    rows.Add(new OwnedSkillAbility(ability, tree, isActive, reason));
                }
            }

            return rows;
        }

        /// <summary>
        /// True if the next node of this tree could be bought right now. Doesn't spend anything, so the UI can
        /// use it to grey out the Unlock button and show the reason. Pass null for combatState where there is no combat.
        /// </summary>
        public bool CanUnlockNextNode(SkillTreeDefinition tree, ICombatState combatState, out string refusalReason)
        {
            if (tree == null)
            {
                refusalReason = "No tree given.";
                return false;
            }

            if (combatState != null && combatState.InCombat)
            {
                refusalReason = "Can't spend points while in combat.";
                return false;
            }

            if (tree.Archetype != _archetype)
            {
                refusalReason = $"{tree.DisplayName} is a {tree.Archetype} tree.";
                return false;
            }

            if (GetUnlockedCount(tree) >= tree.NodeCount)
            {
                refusalReason = $"{tree.DisplayName} is already complete.";
                return false;
            }

            if (_progress.UnspentSkillPoints <= 0)
            {
                refusalReason = "No skill points to spend.";
                return false;
            }

            refusalReason = null;
            return true;
        }

        /// <summary>
        /// Buys the next node of this tree for 1 skill point. Refused (with a reason) in combat, for another
        /// archetype's tree, for a complete tree, or with no skill points.
        /// </summary>
        public bool TryUnlockNextNode(SkillTreeDefinition tree, ICombatState combatState, out string refusalReason)
        {
            if (!CanUnlockNextNode(tree, combatState, out refusalReason))
            {
                return false;
            }

            _progress.TryUseSkillPoint();
            _unlockedCounts[tree] = GetUnlockedCount(tree) + 1;
            SkillsChanged?.Invoke();
            return true;
        }
    }
}
