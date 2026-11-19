namespace DebtDungeon.Core
{
    /// <summary>
    /// What a floor spawn point is for. FloorGen places them; AI and Game each pick out the types they care about.
    /// </summary>
    public enum SpawnPointType
    {
        /// <summary>A destructible faction spawner structure (Major chunks). AI builds it.</summary>
        Spawner = 0,

        /// <summary>A single enemy, rolled at generation. AI spawns the unit.</summary>
        SingleEnemy = 1,

        /// <summary>A chest that gives gold. Game places it.</summary>
        Chest = 2,

        /// <summary>The out-of-combat station in the spawn chunk. Game places it.</summary>
        Station = 3
    }
}
