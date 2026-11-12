namespace DebtDungeon.Core
{
    /// <summary>
    /// The 9 Flow State stats, 3 per archetype, grouped in archetype order (Martial, Magic, Mental).
    /// In each group the first two are attack stats and the third is the defensive stat (armor, Shroud or Ward, plus max HP).
    /// </summary>
    /// <remarks>
    /// The grouping is load-bearing: StatRules.BelongsTo finds a stat's archetype by dividing its number by 3.
    /// Never reorder these values.
    /// </remarks>
    public enum StatType
    {
        // Martial
        Strength = 0,      // Heavy weapons
        Dexterity = 1,     // Light weapons
        Constitution = 2,  // Armor, max HP

        // Magic
        Reach = 3,         // Channeler Foci
        Grasp = 4,         // Igniter Foci
        Build = 5,         // Shrouds, max HP

        // Mental (Beta)
        Ponderance = 6,    // Dream Wonders
        Snappence = 7,     // Nightmare Wonders
        Willpower = 8      // Icon Wards, max HP
    }
}
