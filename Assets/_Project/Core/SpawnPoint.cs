using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// A spot on the floor where something gets placed: a spawner, a single enemy, a chest or the station.
    /// FloorGen creates them; AI and Game read the ones they need from IFloorQuery.GetSpawnPoints().
    /// </summary>
    public readonly struct SpawnPoint
    {
        /// <summary>World position (the center of the tile).</summary>
        public Vector3 Position { get; }

        /// <summary>Which faction it's for. Neutral for chests and the station.</summary>
        public Faction Faction { get; }

        /// <summary>What goes here.</summary>
        public SpawnPointType Type { get; }

        /// <summary>Creates a spawn point.</summary>
        public SpawnPoint(Vector3 position, Faction faction, SpawnPointType type)
        {
            Position = position;
            Faction = faction;
            Type = type;
        }
    }
}
