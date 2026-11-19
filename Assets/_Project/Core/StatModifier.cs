namespace DebtDungeon.Core
{
    /// <summary>
    /// A temporary change to one stat, such as +2 Strength from a weapon or -3 Dexterity from a status effect.
    /// Source records what added it, so everything from that source can be removed together (e.g. when gear is swapped).
    /// </summary>
    public readonly struct StatModifier
    {
        /// <summary>The stat this modifier changes.</summary>
        public StatType Stat { get; }

        /// <summary>The amount added to the stat. Negative values lower it.</summary>
        public int Value { get; }

        /// <summary>What added this modifier: an equipment asset, a skill, a status effect, etc.</summary>
        public object Source { get; }

        /// <summary>Creates a modifier. Pass the object that owns it as the source.</summary>
        public StatModifier(StatType stat, int value, object source)
        {
            Stat = stat;
            Value = value;
            Source = source;
        }
    }
}
