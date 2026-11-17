using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Copies each cell's chosen chunk onto the floor and wall tilemaps, then plugs the door slots the cell doesn't use.
    /// </summary>
    public class FloorStamper
    {
        private static readonly BoundsInt ChunkBounds = new BoundsInt(0, 0, 0, ChunkRules.Size, ChunkRules.Size, 1);

        private readonly Tilemap _floor;
        private readonly Tilemap _walls;
        private readonly TileBase _plugTile;

        public FloorStamper(Tilemap floor, Tilemap walls, TileBase plugTile)
        {
            _floor = floor;
            _walls = walls;
            _plugTile = plugTile;
        }

        /// <summary>Clears both tilemaps and stamps every cell. chunks[x, y] is the chunk chosen for cells[x, y].</summary>
        public void Stamp(FloorCell[,] cells, ChunkDefinition[,] chunks)
        {
            _floor.ClearAllTiles();
            _walls.ClearAllTiles();

            foreach (FloorCell cell in cells)
            {
                ChunkDefinition chunk = chunks[cell.Coord.x, cell.Coord.y];
                if (chunk == null)
                {
                    continue;
                }

                Vector3Int origin = ChunkOrigin(cell.Coord);
                CopyTiles(chunk.Layout.Floor, _floor, origin);
                CopyTiles(chunk.Layout.Walls, _walls, origin);
                PlugUnusedDoors(chunk.Openings & ~cell.Openings, origin);
            }
        }

        /// <summary>Tile position of a cell's bottom-left corner on the floor tilemaps.</summary>
        public static Vector3Int ChunkOrigin(Vector2Int cell)
        {
            return new Vector3Int(cell.x * ChunkRules.Size, cell.y * ChunkRules.Size, 0);
        }

        // Reads the whole 16x16 block in one call and writes it in one call, instead of 256 GetTile/SetTile pairs
        private static void CopyTiles(Tilemap source, Tilemap target, Vector3Int origin)
        {
            TileBase[] tiles = source.GetTilesBlock(ChunkBounds);
            target.SetTilesBlock(new BoundsInt(origin, ChunkBounds.size), tiles);
        }

        private void PlugUnusedDoors(Direction plugs, Vector3Int origin)
        {
            foreach (Direction edge in DirectionHelper.Cardinals)
            {
                if (!DirectionHelper.Contains(plugs, edge))
                {
                    continue;
                }

                for (int i = 0; i < ChunkRules.DoorWidth; i++)
                {
                    Vector2Int tile = ChunkRules.DoorTile(edge, i);
                    _walls.SetTile(origin + new Vector3Int(tile.x, tile.y, 0), _plugTile);
                }
            }
        }
    }
}
