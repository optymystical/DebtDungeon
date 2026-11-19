namespace DebtDungeon.Core
{
    /// <summary>
    /// A character's archetype. It decides which 3 stats and which equipment the character can use.
    /// </summary>
    /// <remarks>
    /// The order must match <see cref="StatType"/>: archetype N owns stats 3N, 3N+1 and 3N+2.
    /// StatRules.BelongsTo depends on this. Mental is Beta but stays in the enum so the stat math holds.
    /// </remarks>
    public enum Archetype
    {
        Martial = 0,
        Magic = 1,
        Mental = 2
    }
}
