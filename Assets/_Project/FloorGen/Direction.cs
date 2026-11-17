namespace DebtDungeon.FloorGen
{
        /// <summary>Edges of a chunk cell. Flags, so one value can hold any combination of openings.</summary>
        [System.Flags]
        public enum Direction
        {
            None = 0,
            North = 1,
            East = 2,
            South = 4,
            West = 8,
            All = North | East | South | West
        }
    
}
