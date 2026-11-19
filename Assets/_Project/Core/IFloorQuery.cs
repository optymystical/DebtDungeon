using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Read-only questions about the generated floor. Implemented by FloorGen and handed out through
    /// GameEvents.FloorGenerated; used by AI (pathfinding, patrols, raids), Player (water) and Game (placement).
    /// </summary>
    /// <remarks>
    /// Two kinds of grid cell: a tile cell is one 1x1 tile (80x80 on a 5x5 floor);
    /// a chunk cell is one 16x16 chunk in the 5x5 grid. Parameter names say which one a method takes.
    /// </remarks>
    public interface IFloorQuery
    {
        /// <summary>The whole floor in tile cells.</summary>
        BoundsInt Bounds { get; }

        /// <summary>World position where the player spawns (in the Neutral start chunk).</summary>
        Vector3 PlayerStart { get; }

        /// <summary>True if a character can stand on this tile (not a wall, not off the floor).</summary>
        bool IsWalkable(Vector2Int tileCell);

        /// <summary>Movement multiplier for this tile: 1 = normal, below 1 = slowed (water).</summary>
        float GetMoveSpeedMultiplier(Vector2Int tileCell);

        /// <summary>The tile cell containing a world position.</summary>
        Vector2Int WorldToCell(Vector3 worldPosition);

        /// <summary>The world position of a tile cell's center.</summary>
        Vector3 CellToWorld(Vector2Int tileCell);

        /// <summary>The chunk cell (in the 5x5 grid) that contains a tile cell.</summary>
        Vector2Int TileToChunkCell(Vector2Int tileCell);

        /// <summary>The faction that owns the chunk this tile is in.</summary>
        Faction GetFactionAt(Vector2Int tileCell);

        /// <summary>The heat-map intensity of the chunk this tile is in.</summary>
        ChunkIntensity GetIntensityAt(Vector2Int tileCell);

        /// <summary>Every spawn point on the floor. Filter by Type and Faction.</summary>
        IReadOnlyList<SpawnPoint> GetSpawnPoints();

        /// <summary>Chunk cells matching a faction and intensity, e.g. all Goblin Minor chunks for guard posts and raid targets.</summary>
        IReadOnlyList<Vector2Int> GetChunkCells(Faction faction, ChunkIntensity intensity);

        /// <summary>The tile-cell bounds of one chunk, for patrol areas.</summary>
        BoundsInt GetChunkTileBounds(Vector2Int chunkCell);
    }
}
