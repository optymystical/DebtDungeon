namespace DebtDungeon.Core
{
    /// <summary>
    /// What a unit is worth when it dies, and the name shown for it (e.g. on the death screen).
    /// Put a component implementing this on each enemy and spawner; Health reads it at death.
    /// </summary>
    public interface IKillReward
    {
        /// <summary>XP the player gets for the kill (only if the player made it).</summary>
        int XpReward { get; }

        /// <summary>Gold dropped where the unit died. The drop lasts 60 seconds.</summary>
        int GoldReward { get; }

        /// <summary>Readable name, e.g. "Goblin Brute". Used for "Slain by a Goblin Brute".</summary>
        string DisplayName { get; }
    }
}
