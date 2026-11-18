using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Reads the markers painted in each placed chunk and turns them into spawn points and water tiles.
    /// Markers are never stamped, so they stay invisible in game.
    /// </summary>
    public class MarkerReader
    {
        private readonly ChunkLibrary _library;
        private readonly FloorGenConfig _config;

        public MarkerReader(ChunkLibrary library, FloorGenConfig config)
        {
            _library = library;
            _config = config;
        }

        /// <summary>Reads every placed chunk's markers. The same seed always gives the same spawn points.</summary>
        public FloorMarkers Read(FloorCell[,] cells, ChunkDefinition[,] chunks, Tilemap floor, int seed)
        {
            System.Random random = new System.Random(seed);
            FloorMarkers result = new FloorMarkers();

            foreach (FloorCell cell in cells)
            {
                ChunkDefinition chunk = chunks[cell.Coord.x, cell.Coord.y];
                if (chunk == null || chunk.Layout == null || chunk.Layout.Markers == null)
                {
                    continue;
                }

                Vector3Int origin = FloorStamper.ChunkOrigin(cell.Coord);
                TileBase[] tiles = chunk.Layout.Markers.GetTilesBlock(ChunkRules.LocalBounds);
                for (int i = 0; i < tiles.Length; i++)
                {
                    MarkerKind kind;
                    if (!_library.TryGetMarkerKind(tiles[i], out kind))
                    {
                        continue;
                    }

                    // GetTilesBlock fills a row at a time: x first, then y
                    Vector3Int tile = origin + new Vector3Int(i % ChunkRules.Size, i / ChunkRules.Size, 0);
                    AddMarker(result, kind, tile, cell, floor.GetCellCenterWorld(tile), random);
                }
            }

            return result;
        }

        private void AddMarker(FloorMarkers result, MarkerKind kind, Vector3Int tile, FloorCell cell, Vector3 position, System.Random random)
        {
            switch (kind)
            {
                case MarkerKind.Spawner:
                    result.SpawnPoints.Add(new SpawnPoint(position, cell.Owner, SpawnPointType.Spawner));
                    break;
                case MarkerKind.Enemy:
                    // README: single enemies are rolled at generation, so the same marker isn't always occupied
                    if (random.NextDouble() < _config.SingleEnemyChance)
                    {
                        result.SpawnPoints.Add(new SpawnPoint(position, cell.Owner, SpawnPointType.SingleEnemy));
                    }
                    break;
                case MarkerKind.Chest:
                    result.SpawnPoints.Add(new SpawnPoint(position, Faction.Neutral, SpawnPointType.Chest));
                    break;
                case MarkerKind.Station:
                    result.SpawnPoints.Add(new SpawnPoint(position, Faction.Neutral, SpawnPointType.Station));
                    break;
                case MarkerKind.Water:
                    result.WaterTiles.Add(new Vector2Int(tile.x, tile.y));
                    break;
            }
        }
    }
}
