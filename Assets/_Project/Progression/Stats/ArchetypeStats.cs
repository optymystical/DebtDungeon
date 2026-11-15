using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>Lists an archetype's own 3 stats, in order (two attack stats, then the defensive stat). For stat panels.</summary>
    public static class ArchetypeStats
    {
        /// <summary>e.g. Martial -> Strength, Dexterity, Constitution.</summary>
        public static List<StatType> GetStats(Archetype archetype)
        {
            List<StatType> stats = new List<StatType>();
            int firstStat = (int)archetype * StatRules.StatsPerArchetype;

            for (int i = 0; i < StatRules.StatsPerArchetype; i++)
            {
                stats.Add((StatType)(firstStat + i));
            }

            return stats;
        }
    }
}
