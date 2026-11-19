using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.FloorGen.Tests
{
    /// <summary>Edit Mode tests for chunk selection (Trello: "Chunk library and selection").</summary>
    public class ChunkLibraryTests
    {
        private const int SeedCount = 50;

        private ChunkLibrary _library;
        private FloorGenConfig _config;

        [SetUp]
        public void SetUp()
        {
            string[] guids = AssetDatabase.FindAssets("t:ChunkLibrary");
            Assert.AreEqual(1, guids.Length, "Expected exactly one ChunkLibrary asset.");
            _library = AssetDatabase.LoadAssetAtPath<ChunkLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            _config = ScriptableObject.CreateInstance<FloorGenConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            // Only destroy what the test created. The library is a real asset on disk.
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Library_HasAFourWayFallbackForEveryType()
        {
            Assert.IsEmpty(_library.Validate(), string.Join(" | ", _library.Validate()));
        }

        // Acceptance test: every cell gets a chunk of its own type that has every door it needs
        [Test]
        public void EveryCellGetsAFittingChunkOfItsType_Over50Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorCell[,] cells = new FloorLayoutGenerator(_config, seed).Generate();
                System.Random random = new System.Random(seed);

                foreach (FloorCell cell in cells)
                {
                    ChunkDefinition chunk = _library.Pick(cell, random);
                    string where = cell.Coord + " on seed " + seed;

                    Assert.IsNotNull(chunk, "No chunk for " + where);
                    Assert.IsTrue(DirectionHelper.Covers(chunk.Openings, cell.Openings), chunk.name + " is missing doors at " + where);
                    Assert.AreEqual(cell.IsStart, chunk.IsSpawnChunk, "Spawn chunk mismatch at " + where);
                    if (!cell.IsStart)
                    {
                        Assert.AreEqual(cell.Owner, chunk.Owner, "Wrong owner at " + where);
                        Assert.AreEqual(cell.Intensity, chunk.Intensity, "Wrong intensity at " + where);
                    }
                }
            }
        }

        // A chunk that isn't in the library can never appear on a floor
        [Test]
        public void EveryChunkDefinition_IsInTheLibrary()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ChunkDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ChunkDefinition chunk = AssetDatabase.LoadAssetAtPath<ChunkDefinition>(path);
                Assert.IsTrue(_library.Contains(chunk), path + " isn't in " + _library.name + ".");
            }
        }
    }
}
