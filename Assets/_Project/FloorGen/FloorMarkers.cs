using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>What the chunk markers produced for one floor: spawn points for AI and Game, and water tiles.</summary>
    public class FloorMarkers
    {
        public List<SpawnPoint> SpawnPoints { get; } = new List<SpawnPoint>();
        public HashSet<Vector2Int> WaterTiles { get; } = new HashSet<Vector2Int>();
    }
}
