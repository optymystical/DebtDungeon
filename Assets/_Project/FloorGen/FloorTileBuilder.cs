using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    // turn the cell layout into real tiles. Copies one chunk per cell, then plugs closed doors
    public class FloorTileBuilder
    {
            private readonly ChunkLibrary library;
            private readonly Tilemap floorTarget;
            private readonly Tilemap wallTarget;
            private readonly System.Random rng;

            // tiles are collected first, then applied in one batch per tielmap
            private readonly List<Vector3Int> floorPositions = new List<Vector3Int>();
            private readonly List<TileBase> floorTiles = new List<TileBase>();
            private readonly List<Vector3Int> wallPositions = new List<Vector3Int>();
            private readonly List<TileBase> wallTiles = new List<TileBase>();

            public FloorTileBuilder(ChunkLibrary library, Tilemap floorTarget, Tilemap wallTarget, int seed)
            {
                this.library = library;
                this.floorTarget = floorTarget;
                this.wallTarget = wallTarget;
                rng = new System.Random(seed);
            }

            public void Build(CellData[,] cells)
            {
                floorTarget.ClearAllTiles();
                wallTarget.ClearAllTiles();

                foreach (CellData cell in cells)
                {
                    Chunk chunk = library.PickChunk(cell, rng);
                    if (chunk == null)
                    {
                        Debug.LogError("No chunk fits cell " + cell.Coord + ". Add at least on Neutral chunk to the library.");
                        continue;
                    }

                    Vector3Int offset = new Vector3Int(cell.Coord.x * Chunk.Size, cell.Coord.y * Chunk.Size, 0);

                    int copied = CollectTiles(chunk.Floor, offset, floorPositions, floorTiles);
                    if (copied == 0)
                    {
                        Debug.LogWarning("Chunk '" + chunk.name + "' gave 0 floor tiles. Check its Floor slot and that it was painted at (0,0).");
                    }

                    CollectTiles(chunk.Walls, offset, wallPositions, wallTiles);
                    CollectWallPlugs(cell, offset);
                }

                floorTarget.SetTiles(floorPositions.ToArray(), floorTiles.ToArray());
                wallTarget.SetTiles(wallPositions.ToArray(), wallTiles.ToArray());
            }

            // reading a chunk's 16x16 area and queues each tile at its spot in the bigger floor (via design doc specs)
            private static int CollectTiles(Tilemap source, Vector3Int offset, List<Vector3Int> positions, List<TileBase> tiles)
            {
                int count = 0;
                for (int x = 0; x < Chunk.Size; x++)
                {
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        Vector3Int local = new Vector3Int(x, y, 0);
                        TileBase tile = source.GetTile(local);
                        if (tile == null) continue; // empty spot in the chunk

                        positions.Add(local + offset);
                        tiles.Add(tile);
                        count++;
                    }
                }

                return count;
            }

            // fills the door slot of every closed edge with wall tiles
            private void CollectWallPlugs(CellData cell, Vector3Int offset)
            {
                foreach (Direction dir in DirectionHelper.All)
                {
                    if (cell.IsOpen(dir)) continue; // open edge keeps its doorway

                    for (int i = Chunk.DoorStart; i < Chunk.DoorStart + Chunk.DoorWidth; i++)
                    {
                        wallPositions.Add(DoorTile(dir, i) + offset);
                        wallTiles.Add(library.WallTile);
                    }
                }
            }

            // positions of door tile i on a given edge, in chunk-local coords
            private static Vector3Int DoorTile(Direction dir, int i)
            {
                int last = Chunk.Size - 1;
                switch (dir)
                {
                    case Direction.North:  return new Vector3Int(i, last, 0);
                    case Direction.South:  return new Vector3Int(i, 0, 0);
                    case Direction.East:   return new Vector3Int(last, i, 0);
                    case Direction.West:   return new Vector3Int(0, i, 0);
                    default:               return Vector3Int.zero;
                }
            }
         
    }
}
