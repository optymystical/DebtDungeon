using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// FloorGen's answer to Core's IFloorQuery: read-only questions about one generated floor.
    /// Built by FloorGenerator after stamping and handed out through GameEvents.FloorGenerated.
    /// </summary>
    public class FloorQuery : IFloorQuery
    {
        private const float NormalMoveSpeed = 1f;

        private readonly FloorCell[,] _cells;
        private readonly Tilemap _floor;
        private readonly bool[,] _walkable;
        private readonly List<SpawnPoint> _spawnPoints;
        private readonly int _floorSize;

        public FloorQuery(FloorCell[,] cells, Vector2Int startCell, Tilemap floor, Tilemap walls, List<SpawnPoint> spawnPoints)
        {
            _cells = cells;
            _floor = floor;
            _spawnPoints = spawnPoints;
            _floorSize = cells.GetLength(0) * ChunkRules.Size;
            Bounds = new BoundsInt(0, 0, 0, _floorSize, _floorSize, 1);

            // AI pathfinding asks IsWalkable thousands of times, so read the tilemaps once into a plain array
            _walkable = new bool[_floorSize, _floorSize];
            for (int x = 0; x < _floorSize; x++)
            {
                for (int y = 0; y < _floorSize; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    _walkable[x, y] = floor.HasTile(position) && !walls.HasTile(position);
                }
            }

            // CellToWorld gives a tile's bottom-left corner, so the middle tile's corner is the chunk's exact center
            Vector3Int startCenter = FloorStamper.ChunkOrigin(startCell) + new Vector3Int(ChunkRules.Middle, ChunkRules.Middle, 0);
            PlayerStart = floor.CellToWorld(startCenter);
        }

        public BoundsInt Bounds { get; }
        public Vector3 PlayerStart { get; }

        public bool IsWalkable(Vector2Int tileCell)
        {
            return IsOnFloor(tileCell) && _walkable[tileCell.x, tileCell.y];
        }

        public float GetMoveSpeedMultiplier(Vector2Int tileCell)
        {
            // Water comes from the chunk marker pass; until then every tile is normal speed
            return NormalMoveSpeed;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            Vector3Int cell = _floor.WorldToCell(worldPosition);
            return new Vector2Int(cell.x, cell.y);
        }

        public Vector3 CellToWorld(Vector2Int tileCell)
        {
            return _floor.GetCellCenterWorld(new Vector3Int(tileCell.x, tileCell.y, 0));
        }

        public Vector2Int TileToChunkCell(Vector2Int tileCell)
        {
            // FloorToInt, not integer division: -1 / 16 is 0 in C#, which would put tile -1 inside chunk 0
            return new Vector2Int(Mathf.FloorToInt((float)tileCell.x / ChunkRules.Size),
                                  Mathf.FloorToInt((float)tileCell.y / ChunkRules.Size));
        }

        public Faction GetFactionAt(Vector2Int tileCell)
        {
            FloorCell cell = CellAt(tileCell);
            return cell != null ? cell.Owner : Faction.Neutral;
        }

        public ChunkIntensity GetIntensityAt(Vector2Int tileCell)
        {
            FloorCell cell = CellAt(tileCell);
            return cell != null ? cell.Intensity : ChunkIntensity.Neutral;
        }

        public IReadOnlyList<SpawnPoint> GetSpawnPoints()
        {
            return _spawnPoints;
        }

        public IReadOnlyList<Vector2Int> GetChunkCells(Faction faction, ChunkIntensity intensity)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (FloorCell cell in _cells)
            {
                if (cell.Owner == faction && cell.Intensity == intensity)
                {
                    result.Add(cell.Coord);
                }
            }

            return result;
        }

        public BoundsInt GetChunkTileBounds(Vector2Int chunkCell)
        {
            return new BoundsInt(FloorStamper.ChunkOrigin(chunkCell), new Vector3Int(ChunkRules.Size, ChunkRules.Size, 1));
        }

        private bool IsOnFloor(Vector2Int tileCell)
        {
            return tileCell.x >= 0 && tileCell.y >= 0 && tileCell.x < _floorSize && tileCell.y < _floorSize;
        }

        // The layout cell a tile belongs to, or null for tiles off the floor
        private FloorCell CellAt(Vector2Int tileCell)
        {
            if (!IsOnFloor(tileCell))
            {
                return null;
            }

            Vector2Int chunkCell = TileToChunkCell(tileCell);
            return _cells[chunkCell.x, chunkCell.y];
        }
    }
}
