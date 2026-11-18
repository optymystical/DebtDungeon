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
            new FloorStamper(_floor, _walls, _plugTile).Stamp(cells, _library.PickAll(cells, seed));
            return new FloorQuery(cells, layout.StartCell, _floor, _walls, new List<SpawnPoint>());
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
    }
}
