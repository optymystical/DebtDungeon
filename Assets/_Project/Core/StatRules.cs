namespace DebtDungeon.Core
{
    /// <summary>
    /// Shared stat math every module uses, so the rules live in one place.
    /// Stateless: it only does calculations and stores nothing.
    /// </summary>
    public static class StatRules
    {
        /// <summary>How many stats each archetype owns (2 attack stats + 1 defensive stat).</summary>
        public const int StatsPerArchetype = 3;

        /// <summary>Power = stat / 10 (GDD: Stats). Ability base values are multiplied by Power.</summary>
        public const float PowerDivisor = 10f;

        /// <summary>
        /// Returns the archetype that owns a stat. Works because StatType is grouped 3 per archetype,
        /// in the same order as Archetype (e.g. Build = 5, and 5 / 3 = 1 = Magic).
        /// </summary>
        public static Archetype GetArchetype(StatType stat)
        {
            return (Archetype)((int)stat / StatsPerArchetype);
        }

        /// <summary>
        /// True if the stat is one of this archetype's 3 stats. Characters ignore any stat or modifier where this is false.
        /// </summary>
        public static bool BelongsTo(StatType stat, Archetype archetype)
        {
            return GetArchetype(stat) == archetype;
        }

        /// <summary>
        /// Returns the archetype's defensive stat (Constitution, Build or Willpower), which drives max HP and armor/Shroud/Ward.
        /// It's the third stat in each group of 3.
        /// </summary>
        public static StatType GetDefensiveStat(Archetype archetype)
        {
            int firstStatOfArchetype = (int)archetype * StatsPerArchetype;
            return (StatType)(firstStatOfArchetype + 2);
        }

        /// <summary>
        /// Converts a stat value into Power, the multiplier for an ability's base values (GDD: Power = stat / 10).
        /// </summary>
        public static float GetPower(int statValue)
        {
            return statValue / PowerDivisor;
        }
    }
}
