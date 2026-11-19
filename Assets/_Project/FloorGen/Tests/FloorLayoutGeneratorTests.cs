using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DebtDungeon.FloorGen.Tests
{
    /// <summary>Edit Mode tests for the floor layout (Trello: "Start cell and faction heat map", "Run seed").</summary>
    public class FloorLayoutGeneratorTests
    {
        private const int SeedCount = 50;

        private FloorGenConfig _config;

        [SetUp]
        public void SetUp()
        {
            // A fresh config with the default values from FloorGenConfig's field initializers
            _config = ScriptableObject.CreateInstance<FloorGenConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // Acceptance test: over 50 seeds, is the start always Neutral with hotspots 2+ cells away?
        [Test]
        public void StartIsNeutral_AndHotspotsAreFarEnough_Over50Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorCell[,] cells = layout.Generate();
                FloorCell start = cells[layout.StartCell.x, layout.StartCell.y];

                Assert.AreEqual(Faction.Neutral, start.Owner, "Start not neutral on seed " + seed);
                Assert.GreaterOrEqual(FloorLayoutGenerator.ChebyshevDistance(layout.StartCell, layout.GoblinHotspot),
                                      _config.MinHotspotDistance, "Goblin hotspot too close on seed " + seed);
                Assert.GreaterOrEqual(FloorLayoutGenerator.ChebyshevDistance(layout.StartCell, layout.SpiderHotspot),
                                      _config.MinHotspotDistance, "Spider hotspot too close on seed " + seed);
                Assert.GreaterOrEqual(FloorLayoutGenerator.ChebyshevDistance(layout.GoblinHotspot, layout.SpiderHotspot),
                                      _config.MinHotspotDistance, "Hotspots too close on seed " + seed);
            }
        }

        // Acceptance test: do some floors lean goblin and some lean spider?
        [Test]
        public void SomeFloorsLeanGoblin_AndSomeLeanSpider()
        {
            int goblinLeaning = 0;
            int spiderLeaning = 0;

            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorCell[,] cells = new FloorLayoutGenerator(_config, seed).Generate();
                int goblinCells = 0;
                int spiderCells = 0;

                foreach (FloorCell cell in cells)
                {
                    if (cell.Owner == Faction.Goblin)
                    {
                        goblinCells++;
                    }
                    else if (cell.Owner == Faction.Spider)
                    {
                        spiderCells++;
                    }
                }

                if (goblinCells > spiderCells)
                {
                    goblinLeaning++;
                }
                else if (spiderCells > goblinCells)
                {
                    spiderLeaning++;
                }
            }

            Assert.Greater(goblinLeaning, 0, "No floor leaned goblin.");
            Assert.Greater(spiderLeaning, 0, "No floor leaned spider.");
        }

        [Test]
        public void NoFactionExceedsTheMajorCap_Over50Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorCell[,] cells = new FloorLayoutGenerator(_config, seed).Generate();
                int goblinMajors = 0;
                int spiderMajors = 0;

                foreach (FloorCell cell in cells)
                {
                    if (cell.Intensity != ChunkIntensity.Major)
                    {
                        continue;
                    }

                    if (cell.Owner == Faction.Goblin)
                    {
                        goblinMajors++;
                    }
                    else if (cell.Owner == Faction.Spider)
                    {
                        spiderMajors++;
                    }
                }

                Assert.LessOrEqual(goblinMajors, _config.MaxMajorPerFaction, "Too many goblin Majors on seed " + seed);
                Assert.LessOrEqual(spiderMajors, _config.MaxMajorPerFaction, "Too many spider Majors on seed " + seed);
            }
        }

        // Run seed card: does the same seed give the same floor twice in a row? (Layout part; doors and loot come later.)
        [Test]
        public void SameSeed_BuildsTheSameLayout()
        {
            FloorCell[,] first = new FloorLayoutGenerator(_config, 12345).Generate();
            FloorCell[,] second = new FloorLayoutGenerator(_config, 12345).Generate();

            for (int x = 0; x < first.GetLength(0); x++)
            {
                for (int y = 0; y < first.GetLength(1); y++)
                {
                    Assert.AreEqual(first[x, y].Owner, second[x, y].Owner);
                    Assert.AreEqual(first[x, y].Intensity, second[x, y].Intensity);
                    Assert.AreEqual(first[x, y].IsStart, second[x, y].IsStart);
                    Assert.AreEqual(first[x, y].Openings, second[x, y].Openings);
                }
            }
        }

        // Acceptance test: can the player walk from the start to every cell, over 50 seeds?
        [Test]
        public void EveryCellIsReachableFromStart_Over50Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(_config, seed);
                FloorCell[,] cells = layout.Generate();

                Assert.AreEqual(cells.Length, CountReachable(cells, layout.StartCell), "Unreachable cells on seed " + seed);
            }
        }

        // Doors must line up with the neighbor's door, and no door may lead off the grid
        [Test]
        public void OpeningsMatchNeighbors_AndStayInsideTheGrid_Over50Seeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                FloorCell[,] cells = new FloorLayoutGenerator(_config, seed).Generate();
                int size = cells.GetLength(0);

                foreach (FloorCell cell in cells)
                {
                    foreach (Direction direction in DirectionHelper.Cardinals)
                    {
                        if (!cell.IsOpen(direction))
                        {
                            continue;
                        }

                        Vector2Int next = cell.Coord + DirectionHelper.ToOffset(direction);
                        bool inGrid = next.x >= 0 && next.y >= 0 && next.x < size && next.y < size;
                        Assert.IsTrue(inGrid, cell.Coord + " opens " + direction + " off the grid on seed " + seed);
                        Assert.IsTrue(cells[next.x, next.y].IsOpen(DirectionHelper.Opposite(direction)),
                                      cell.Coord + " opens " + direction + " but its neighbor doesn't open back on seed " + seed);
                    }
                }
            }
        }

        // Breadth-first search through open doors; returns how many cells were reached
        private static int CountReachable(FloorCell[,] cells, Vector2Int start)
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
                    if (!cells[current.x, current.y].IsOpen(direction))
                    {
                        continue;
                    }

                    Vector2Int next = current + DirectionHelper.ToOffset(direction);
                    if (reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return reached.Count;
        }
    }
}
