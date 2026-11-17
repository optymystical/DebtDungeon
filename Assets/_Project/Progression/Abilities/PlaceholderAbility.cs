using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Stand-in ability for skill trees until Player's real ability types (melee arc, projectile, self-buff) exist.
    /// It carries everything the skill screen shows (name, description, slot, cooldown, energy) but does nothing
    /// when used. To swap one for a real ability later, point the tree node at the new asset; no code changes.
    /// Assets live in Data/Abilities/Placeholder so they're easy to find and remove.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_New", menuName = "Debt Dungeon/Placeholder Ability")]
    public class PlaceholderAbility : AbilityDefinition
    {
    }
}
