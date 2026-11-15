using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Keeps the player's IAbilityUser in step with what the character should be able to use right now:
    /// abilities from worn equipment and Affixes, plus skill tree abilities from trees whose equipment is worn.
    /// Whenever the loadout or the bought skills change, it grants what's new and revokes what's gone.
    /// Created by the Game glue once the player exists; call Dispose when the player is destroyed.
    /// </summary>
    public class AbilityGranter : IDisposable
    {
        private readonly Loadout _loadout;
        private readonly CharacterSkills _skills;
        private readonly IAbilityUser _abilityUser;

        // What this granter has handed to the ability user. Only these are ever revoked.
        private readonly List<AbilityDefinition> _granted = new List<AbilityDefinition>();

        public AbilityGranter(Loadout loadout, CharacterSkills skills, IAbilityUser abilityUser)
        {
            _loadout = loadout;
            _skills = skills;
            _abilityUser = abilityUser;

            _loadout.LoadoutChanged += Refresh;
            _skills.SkillsChanged += Refresh;

            Refresh();
        }

        /// <summary>
        /// Everything the character can use right now: equipment abilities, then skill abilities from each
        /// owned tree whose equipment is worn. A tree without its gear contributes nothing, but its bought
        /// nodes stay bought.
        /// </summary>
        public List<AbilityDefinition> GetActiveAbilities()
        {
            List<AbilityDefinition> active = new List<AbilityDefinition>();
            AddMissing(active, _loadout.GetGrantedAbilities());

            foreach (SkillTreeDefinition tree in _skills.OwnedTrees)
            {
                if (tree.IsUsableWith(_loadout))
                {
                    AddMissing(active, _skills.GetOwnedAbilities(tree));
                }
            }

            return active;
        }

        /// <summary>
        /// Compares what should be active with what was granted last time, then revokes the abilities that are
        /// gone and grants the new ones. Abilities that stay active are left alone, so their cooldowns aren't reset.
        /// </summary>
        public void Refresh()
        {
            List<AbilityDefinition> active = GetActiveAbilities();

            // Revoke first, so an upgrade frees the old ability's slot before the new one is slotted.
            // Loop backwards because items are removed from the list while looping.
            for (int i = _granted.Count - 1; i >= 0; i--)
            {
                if (!active.Contains(_granted[i]))
                {
                    _abilityUser.RevokeAbility(_granted[i]);
                    _granted.RemoveAt(i);
                }
            }

            foreach (AbilityDefinition ability in active)
            {
                if (!_granted.Contains(ability))
                {
                    _abilityUser.GrantAbility(ability);
                    _granted.Add(ability);
                }
            }
        }

        /// <summary>Stops listening for changes. Granted abilities are left as they are.</summary>
        public void Dispose()
        {
            _loadout.LoadoutChanged -= Refresh;
            _skills.SkillsChanged -= Refresh;
        }

        private static void AddMissing(List<AbilityDefinition> target, IEnumerable<AbilityDefinition> abilities)
        {
            foreach (AbilityDefinition ability in abilities)
            {
                if (ability != null && !target.Contains(ability))
                {
                    target.Add(ability);
                }
            }
        }
    }
}
