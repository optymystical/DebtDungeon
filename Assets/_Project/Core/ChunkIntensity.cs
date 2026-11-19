namespace DebtDungeon.Core
{
    /// <summary>
    /// How strongly a faction holds a chunk, from the floor heat map.
    /// Major chunks hold spawners and Residents; Minor chunks are patrolled by Guards.
    /// </summary>
    public enum ChunkIntensity
    {
        Neutral = 0,
        Minor = 1,
        Major = 2
    }
}
