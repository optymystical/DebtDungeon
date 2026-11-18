using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen.Tests
{
    /// <summary>Edit Mode tests for the floor query AI and Game read (Trello: "IFloorQuery and FloorGenerated").</summary>
    public class FloorQueryTests
    {
        private const int SeedCount = 20;
        private const int TestSeed = 12345;

        private GameObject _gridObject;
        private Tilemap _floor;
        private Tilemap _walls;
        private Tile _plugTile;
        private FloorGenConfig _config;
        private ChunkLibrary _library;
        private FloorMarkers _markers;

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

        // Acceptance test (Chris): the player starts in the center of the Neutral start chunk, on a walkable tile
        [Test]
        public void PlayerStart_IsWalkable_AndInTheStartChunk_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorQuery floor = Build(layout, seed);
                Vector2Int startTile = floor.WorldToCell(floor.PlayerStart);

                Assert.AreEqual(layout.StartCell, floor.TileToChunkCell(startTile), "PlayerStart outside the start chunk on seed " + seed);
                Assert.IsTrue(floor.IsWalkable(startTile), "PlayerStart on a wall on seed " + seed);
            }
        }

        [Test]
        public void FactionAndIntensity_MatchTheLayout_ForEveryChunk()
        {
            FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, TestSeed);
            FloorQuery floor = Build(layout, TestSeed);

            foreach (FloorCell cell in layout.Cells)
            {
                BoundsInt bounds = floor.GetChunkTileBounds(cell.Coord);
                Vector2Int middle = new Vector2Int(bounds.xMin + ChunkRules.Middle, bounds.yMin + ChunkRules.Middle);

                Assert.AreEqual(cell.Coord, floor.TileToChunkCell(middle));
                Assert.AreEqual(cell.Owner, floor.GetFactionAt(middle), "Wrong faction at " + cell.Coord);
                Assert.AreEqual(cell.Intensity, floor.GetIntensityAt(middle), "Wrong intensity at " + cell.Coord);
            }
        }

        [Test]
        public void GetChunkCells_ListsEveryChunkOnce_UnderItsOwnType()
        {
            FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, TestSeed);
            FloorQuery floor = Build(layout, TestSeed);

            int listed = CountMatching(floor, layout.Cells, Faction.Neutral, ChunkIntensity.Neutral)
                       + CountMatching(floor, layout.Cells, Faction.Goblin, ChunkIntensity.Minor)
                       + CountMatching(floor, layout.Cells, Faction.Goblin, ChunkIntensity.Major)
                       + CountMatching(floor, layout.Cells, Faction.Spider, ChunkIntensity.Minor)
                       + CountMatching(floor, layout.Cells, Faction.Spider, ChunkIntensity.Major);

            Assert.AreEqual(layout.Cells.Length, listed);
        }

        // AI units at the edge ask about neighboring tiles, so off-floor questions must answer safely
        [Test]
        public void TilesOffTheFloor_AreNotWalkable_AndCountAsNeutral()
        {
            FloorQuery floor = Build(new FloorLayoutGenerator(_config, TestSeed), TestSeed);
            Vector2Int left = new Vector2Int(-1, 5);
            Vector2Int right = new Vector2Int(floor.Bounds.xMax, 5);

            Assert.AreEqual(new Vector2Int(-1, 0), floor.TileToChunkCell(left));
            Assert.IsFalse(floor.IsWalkable(left));
            Assert.IsFalse(floor.IsWalkable(right));
            Assert.AreEqual(Faction.Neutral, floor.GetFactionAt(left));
            Assert.AreEqual(ChunkIntensity.Neutral, floor.GetIntensityAt(right));
        }

        [Test]
        public void CellToWorld_AndWorldToCell_RoundTrip()
        {
            FloorQuery floor = Build(new FloorLayoutGenerator(_config, TestSeed), TestSeed);
            Vector2Int[] tiles = { new Vector2Int(0, 0), new Vector2Int(17, 40), new Vector2Int(79, 79) };

            foreach (Vector2Int tile in tiles)
            {
                Assert.AreEqual(tile, floor.WorldToCell(floor.CellToWorld(tile)));
            }
        }

        private FloorQuery Build(FloorLayoutGenerator layout, int seed)
        {
            FloorCell[,] cells = layout.Generate();
            ChunkDefinition[,] chunks = _library.PickAll(cells, seed);
            FloorStamper stamper = new FloorStamper(_floor, _walls, _plugTile);
            stamper.Stamp(cells, chunks);

            _markers = new MarkerReader(_library, _config).Read(cells, chunks, _floor, seed);
            stamper.PaintWater(_markers.WaterTiles, _plugTile); // Any tile works for water in a test
            return new FloorQuery(cells, layout.StartCell, _floor, _walls, _markers, _config.WaterSpeedMultiplier);
        }

        private Tilemap CreateTilemap(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_gridObject.transform);
            return child.AddComponent<Tilemap>();
        }

        // Checks every chunk listed for a type really is that type; returns how many were listed
        private static int CountMatching(FloorQuery floor, FloorCell[,] cells, Faction faction, ChunkIntensity intensity)
        {
            IReadOnlyList<Vector2Int> chunkCells = floor.GetChunkCells(faction, intensity);
            foreach (Vector2Int chunkCell in chunkCells)
            {
                Assert.AreEqual(faction, cells[chunkCell.x, chunkCell.y].Owner);
                Assert.AreEqual(intensity, cells[chunkCell.x, chunkCell.y].Intensity);
            }

            return chunkCells.Count;
        }

        // Acceptance test (Chris): exactly one Station per floor, in the start chunk, for Game to place
        [Test]
        public void EachFloor_HasOneStation_InTheStartChunk_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorQuery floor = Build(layout, seed);
                int stations = 0;

                foreach (SpawnPoint point in floor.GetSpawnPoints())
                {
                    if (point.Type != SpawnPointType.Station)
                    {
                        continue;
                    }

                    stations++;
                    Assert.AreEqual(layout.StartCell, floor.TileToChunkCell(floor.WorldToCell(point.Position)),
                                    "Station outside the start chunk on seed " + seed);
                }

                Assert.AreEqual(1, stations, "Wrong station count on seed " + seed);
            }
        }

        // Acceptance test (Larry's spawners): every Major chunk has a spawner of its own faction, and no other chunk has one
        [Test]
        public void Spawners_AreInEveryMajorChunk_AndOnlyThere_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorQuery floor = Build(layout, seed);
                HashSet<Vector2Int> chunksWithSpawner = new HashSet<Vector2Int>();

                foreach (SpawnPoint point in floor.GetSpawnPoints())
                {
                    if (point.Type != SpawnPointType.Spawner)
                    {
                        continue;
                    }

                    Vector2Int chunkCell = floor.TileToChunkCell(floor.WorldToCell(point.Position));
                    FloorCell cell = layout.Cells[chunkCell.x, chunkCell.y];
                    Assert.AreEqual(ChunkIntensity.Major, cell.Intensity, "Spawner outside a Major chunk on seed " + seed);
                    Assert.AreEqual(cell.Owner, point.Faction, "Spawner has the wrong faction on seed " + seed);
                    chunksWithSpawner.Add(chunkCell);
                }

                foreach (FloorCell cell in layout.Cells)
                {
                    if (cell.Intensity == ChunkIntensity.Major)
                    {
                        Assert.IsTrue(chunksWithSpawner.Contains(cell.Coord), "Major chunk " + cell.Coord + " has no spawner on seed " + seed);
                    }
                }
            }
        }

        [Test]
        public void EverySpawnPoint_IsOnAWalkableTile_Over20Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorQuery floor = Build(new FloorLayoutGenerator(_config, seed), seed);
                foreach (SpawnPoint point in floor.GetSpawnPoints())
                {
                    Assert.IsTrue(floor.IsWalkable(floor.WorldToCell(point.Position)), point.Type + " on a wall on seed " + seed);
                }
            }
        }

        [Test]
        public void WaterTiles_SlowMovement_AndStayWalkable_Over20Seeds()
        {
            int waterSeen = 0;
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorQuery floor = Build(new FloorLayoutGenerator(_config, seed), seed);
                foreach (Vector2Int tile in _markers.WaterTiles)
                {
                    waterSeen++;
                    Assert.AreEqual(_config.WaterSpeedMultiplier, floor.GetMoveSpeedMultiplier(tile));
                    Assert.IsTrue(floor.IsWalkable(tile), "Water tile " + tile + " isn't walkable on seed " + seed);
                }
            }

            Assert.Greater(waterSeen, 0, "No water on any floor. Is a Water marker painted in a chunk?");
        }

        // Run seed card: the same seed places the same spawn points, enemy rolls included
        [Test]
        public void SameSeed_PlacesTheSameSpawnPoints()
        {
            IReadOnlyList<SpawnPoint> first = Build(new FloorLayoutGenerator(_config, TestSeed), TestSeed).GetSpawnPoints();
            IReadOnlyList<SpawnPoint> second = Build(new FloorLayoutGenerator(_config, TestSeed), TestSeed).GetSpawnPoints();

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Position, second[i].Position);
                Assert.AreEqual(first[i].Type, second[i].Type);
                Assert.AreEqual(first[i].Faction, second[i].Faction);
            }
        }
    }
}
