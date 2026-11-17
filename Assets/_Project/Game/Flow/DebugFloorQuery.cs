using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// A stand-in floor for testing FloorIntegration before FloorGen raises the real one: an 80x80-tile open
    /// Neutral floor with one player start and one Station spawn point. Debug only; FloorGen implements the real
    /// IFloorQuery. Implementing the Core interface is all it takes for every listener to accept it.
    /// </summary>
    public class DebugFloorQuery : IFloorQuery
    {
        private const int ChunkSize = 16;
        private const int FloorSize = 80;

        private readonly List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();

        public DebugFloorQuery(Vector3 playerStart, Vector3 stationPosition)
        {
            PlayerStart = playerStart;
            _spawnPoints.Add(new SpawnPoint(stationPosition, Faction.Neutral, SpawnPointType.Station));
        }

        public BoundsInt Bounds => new BoundsInt(0, 0, 0, FloorSize, FloorSize, 1);

        public Vector3 PlayerStart { get; }

        public bool IsWalkable(Vector2Int tileCell)
        {
            return true;
        }

        public float GetMoveSpeedMultiplier(Vector2Int tileCell)
        {
            return 1f;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        public Vector3 CellToWorld(Vector2Int tileCell)
        {
            return new Vector3(tileCell.x + 0.5f, tileCell.y + 0.5f, 0f);
        }

        public Vector2Int TileToChunkCell(Vector2Int tileCell)
        {
            return new Vector2Int(Mathf.FloorToInt(tileCell.x / (float)ChunkSize), Mathf.FloorToInt(tileCell.y / (float)ChunkSize));
        }

        public Faction GetFactionAt(Vector2Int tileCell)
        {
            return Faction.Neutral;
        }

        public ChunkIntensity GetIntensityAt(Vector2Int tileCell)
        {
            return ChunkIntensity.Neutral;
        }

        public IReadOnlyList<SpawnPoint> GetSpawnPoints()
        {
            return _spawnPoints;
        }

        public IReadOnlyList<Vector2Int> GetChunkCells(Faction faction, ChunkIntensity intensity)
        {
            return new List<Vector2Int>();
        }

        public BoundsInt GetChunkTileBounds(Vector2Int chunkCell)
        {
            return new BoundsInt(chunkCell.x * ChunkSize, chunkCell.y * ChunkSize, 0, ChunkSize, ChunkSize, 1);
        }
    }
}
