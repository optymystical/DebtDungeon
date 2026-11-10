namespace DebtDungeon.Core
{
    /// <summary>
    /// Who something belongs to. Used for units, chunks, spawners and damage sources.
    /// Neutral covers unowned things: neutral chunks, chests, traps.
    /// </summary>
    /// <remarks>
    /// Unity saves enums as numbers. Only add new values at the end; never reorder or remove one,
    /// or every asset that stored a faction silently changes meaning.
    /// </remarks>
    public enum Faction
    {
        Neutral = 0,
        Player = 1,
        Goblin = 2,
        Spider = 3
    }
}
