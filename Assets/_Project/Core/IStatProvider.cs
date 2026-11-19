namespace DebtDungeon.Core
{
    /// <summary>
    /// Anything with stats (the player character for now). Implemented by Progression; read by Player and combat code.
    /// </summary>
    public interface IStatProvider
    {
        /// <summary>The character's archetype. Only this archetype's 3 stats count.</summary>
        Archetype Archetype { get; }

        /// <summary>
        /// The stat's final value: base value plus all modifiers.
        /// Returns 0 for any stat outside the character's archetype.
        /// </summary>
        int GetStat(StatType stat);

        /// <summary>Adds a modifier. Modifiers for stats outside the character's archetype are ignored. Raises StatsChanged.</summary>
        void AddModifier(StatModifier modifier);

        /// <summary>Removes every modifier added by this source (e.g. a piece of equipment being unequipped). Raises StatsChanged.</summary>
        void RemoveModifiersFromSource(object source);

        /// <summary>Raised whenever any final stat value may have changed, so UI and dependent values can refresh.</summary>
        event System.Action StatsChanged;
    }
}
