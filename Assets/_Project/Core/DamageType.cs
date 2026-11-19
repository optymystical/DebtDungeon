namespace DebtDungeon.Core
{
    /// <summary>
    /// The kind of damage a hit deals. It decides what can absorb it:
    /// armor blocks Physical; Shrouds block Physical and Magic; nothing blocks Poison.
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Poison = 1,
        Magic = 2
    }
}
