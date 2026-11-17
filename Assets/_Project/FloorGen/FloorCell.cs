using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// one chunk cell of the floor grid: who owns it and how strongly. Becomes one 16x16 chunk
    /// </summary>
    public class FloorCell
    {
    public FloorCell(Vector2Int coord)
        {
            Coord = coord;
            Owner = Faction.Neutral;
            Intensity = ChunkIntensity.Neutral;
        }

        public Vector2Int Coord { get; }
        public float GoblinHeat { get; internal set; }
        public float SpiderHeat { get; internal set; }
        public Faction Owner { get; internal set; }
        public ChunkIntensity Intensity { get; internal set; }
        public bool IsStart { get; internal set; }
    }
}
