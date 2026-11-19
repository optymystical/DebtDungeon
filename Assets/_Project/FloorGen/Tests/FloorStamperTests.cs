using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen.Tests
{
    /// <summary>Edit Mode tests for stamping (Trello: "Chunk stamping and collision").</summary>
    public class FloorStamperTests
    {
        private const int SeedCount = 20;

        private GameObject _gridObject;
        private Tilemap _floor;
        private Tilemap _walls;
        private Tile _plugTile;
        private FloorGenConfig _config;
        private ChunkLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _gridObject = new GameObject("TestGrid");
            _gridObject.AddComponent<Grid>();
            _floor = CreateTilemap("Floor");
            _walls = CreateTilemap("Walls");
            _plugTile = ScriptableObject.CreateInstance<Tile>();
            _config = ScriptableObject.CreateInstance<FloorGenConfig>();

            string[] guids = AssetDatabase.FindAssets("t:ChunkLibrary");
            Assert.AreEqual(1, guids.Length, "Expected exactly one ChunkLibrary asset.");
            _library = AssetDatabase.LoadAssetAtPath<ChunkLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gridObject);
            Object.DestroyImmediate(_plugTile);
            Object.DestroyImmediate(_config);
        }

        // Acceptance test: from the start chunk, the player can walk into every chunk and never off the floor's edge
        [Test]
        public void StampedFloor_ReachesEveryChunk_AndIsSealed_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorCell[,] cells = Build(layout, seed);
                int floorSize = cells.GetLength(0) * ChunkRules.Size;

                HashSet<Vector2Int> reached = FloodFill(FirstWalkableTileIn(layout.StartCell));

                foreach (Vector2Int tile in reached)
                {
                    bool onOuterEdge = tile.x == 0 || tile.y == 0 || tile.x == floorSize - 1 || tile.y == floorSize - 1;
                    Assert.IsFalse(onOuterEdge, "Walkable tile on the floor's outer edge at " + tile + " on seed " + seed);
                }

                foreach (FloorCell cell in cells)
                {
                    Assert.IsTrue(ReachedAnyTileIn(reached, cell.Coord), "Chunk " + cell.Coord + " can't be reached on seed " + seed);
                }
            }
        }

        // Every edge a cell doesn't connect through must have walls in its door slot
        [Test]
        public void ClosedEdges_HaveWallsInTheirDoorSlots_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorCell[,] cells = Build(new FloorLayoutGenerator(_config, seed), seed);

                foreach (FloorCell cell in cells)
                {
                    Vector3Int origin = FloorStamper.ChunkOrigin(cell.Coord);
                    foreach (Direction edge in DirectionHelper.Cardinals)
                    {
                        if (cell.IsOpen(edge))
                        {
                            continue;
                        }

                        for (int i = 0; i < ChunkRules.DoorWidth; i++)
                        {
                            Vector2Int tile = ChunkRules.DoorTile(edge, i);
                            Assert.IsTrue(_walls.HasTile(origin + new Vector3Int(tile.x, tile.y, 0)),
                                          "Unplugged " + edge + " door at cell " + cell.Coord + " on seed " + seed);
                        }
                    }
                }
            }
        }

        private FloorCell[,] Build(FloorLayoutGenerator layout, int seed)
        {
            FloorCell[,] cells = layout.Generate();
            new FloorStamper(_floor, _walls, _plugTile).Stamp(cells, _library.PickAll(cells, seed));
            return cells;
        }

        private Tilemap CreateTilemap(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_gridObject.transform);
            return child.AddComponent<Tilemap>();
        }

        private bool IsWalkable(Vector2Int tile)
        {
            Vector3Int position = new Vector3Int(tile.x, tile.y, 0);
            return _floor.HasTile(position) && !_walls.HasTile(position);
        }

        private Vector2Int FirstWalkableTileIn(Vector2Int cell)
        {
            Vector3Int origin = FloorStamper.ChunkOrigin(cell);
            for (int x = 0; x < ChunkRules.Size; x++)
            {
                for (int y = 0; y < ChunkRules.Size; y++)
                {
                    Vector2Int tile = new Vector2Int(origin.x + x, origin.y + y);
                    if (IsWalkable(tile))
                    {
                        return tile;
                    }
                }
            }

            Assert.Fail("The start chunk has no walkable tile.");
            return Vector2Int.zero;
        }

        // Breadth-first search over walkable tiles; tiles off the floor have no floor tile, so the search stays inside
        private HashSet<Vector2Int> FloodFill(Vector2Int start)
        {
            HashSet<Vector2Int> reached = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            reached.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Direction direction in DirectionHelper.Cardinals)
                {
                    Vector2Int next = current + DirectionHelper.ToOffset(direction);
                    if (IsWalkable(next) && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return reached;
        }

        private static bool ReachedAnyTileIn(HashSet<Vector2Int> reached, Vector2Int cell)
        {
            Vector3Int origin = FloorStamper.ChunkOrigin(cell);
            for (int x = 0; x < ChunkRules.Size; x++)
            {
                for (int y = 0; y < ChunkRules.Size; y++)
                {
                    if (reached.Contains(new Vector2Int(origin.x + x, origin.y + y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
