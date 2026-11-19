namespace DebtDungeon.Core
{
    /// <summary>
    /// The status effects anything in the game can apply (projectiles, traps, abilities) and receive
    /// (the player, units). Applied through IStatusReceiver.
    /// </summary>
    public enum StatusEffectType
    {
        /// <summary>Moves slower. Doesn't stack: reapplying refreshes the timer to whichever ends later.</summary>
        Slowed = 0,

        /// <summary>Takes Poison damage every second. Armor and Shrouds never absorb it. Doesn't stack: refreshes.</summary>
        Poisoned = 1,

        /// <summary>Buff from Flow State. Stacks up to 3 and fades over time. (Not built yet.)</summary>
        Strengthened = 2,

        /// <summary>Debuff from Flow State. Stacks up to 3 and fades over time. (Not built yet.)</summary>
        Weakened = 3,
    }
}
