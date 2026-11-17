using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Every chunk a floor can use, and the rule for choosing one per cell:
    /// same type, and its openings cover every opening the cell needs.
    /// </summary>
    [CreateAssetMenu(fileName = "ChunkLibrary", menuName = "Debt Dungeon/Floor Gen/Chunk Library")]
    public class ChunkLibrary : ScriptableObject
    {
        [SerializeField] private List<ChunkDefinition> _chunks = new List<ChunkDefinition>();

        /// <summary>Picks a chunk for a cell. Falls back to a neutral 4-way; returns null only if there is none.</summary>
        public ChunkDefinition Pick(FloorCell cell, System.Random random)
        {
            List<ChunkDefinition> options = new List<ChunkDefinition>();
            foreach (ChunkDefinition chunk in _chunks)
            {
                if (chunk != null && IsSameType(chunk, cell) && DirectionHelper.Covers(chunk.Openings, cell.Openings))
                {
                    options.Add(chunk);
                }
            }

            if (options.Count == 0)
            {
                Debug.LogWarning("No chunk fits " + DescribeType(cell) + " with openings " + cell.Openings
                                 + ". Using a neutral 4-way instead.", this);
                foreach (ChunkDefinition chunk in _chunks)
                {
                    if (chunk != null && !chunk.IsSpawnChunk && chunk.Owner == Faction.Neutral && IsFourWay(chunk))
                    {
                        options.Add(chunk);
                    }
                }
            }

            if (options.Count == 0)
            {
                Debug.LogError("ChunkLibrary has no neutral 4-way chunk to fall back on.", this);
                return null;
            }

            return options[random.Next(options.Count)];
        }

        /// <summary>Picks a chunk for every cell. Uses its own random from the seed, so library changes never change the layout.</summary>
        public ChunkDefinition[,] PickAll(FloorCell[,] cells, int seed)
        {
            System.Random random = new System.Random(seed);
            ChunkDefinition[,] chunks = new ChunkDefinition[cells.GetLength(0), cells.GetLength(1)];

            foreach (FloorCell cell in cells)
            {
                chunks[cell.Coord.x, cell.Coord.y] = Pick(cell, random);
            }

            return chunks;
        }

        /// <summary>Checks that every chunk type has a 4-way fallback. Returns every problem found; empty means valid.</summary>
        public List<string> Validate()
        {
            List<string> problems = new List<string>();
            foreach (ChunkDefinition chunk in _chunks)
            {
                if (chunk == null)
                {
                    problems.Add("The chunk list has an empty slot.");
                }
            }

            CheckFourWay(problems, Faction.Neutral, ChunkIntensity.Neutral, true, "Neutral Spawn");
            CheckFourWay(problems, Faction.Neutral, ChunkIntensity.Neutral, false, "Neutral");
            CheckFourWay(problems, Faction.Goblin, ChunkIntensity.Minor, false, "Goblin Minor");
            CheckFourWay(problems, Faction.Goblin, ChunkIntensity.Major, false, "Goblin Major");
            CheckFourWay(problems, Faction.Spider, ChunkIntensity.Minor, false, "Spider Minor");
            CheckFourWay(problems, Faction.Spider, ChunkIntensity.Major, false, "Spider Major");
            return problems;
        }

        [ContextMenu("Validate Library")]
        private void LogValidation()
        {
            List<string> problems = Validate();
            if (problems.Count == 0)
            {
                Debug.Log(name + " has a 4-way fallback for every chunk type.", this);
                return;
            }

            foreach (string problem in problems)
            {
                Debug.LogError(name + ": " + problem, this);
            }
        }

        private void CheckFourWay(List<string> problems, Faction owner, ChunkIntensity intensity, bool isSpawn, string typeName)
        {
            foreach (ChunkDefinition chunk in _chunks)
            {
                if (chunk != null && chunk.Owner == owner && chunk.Intensity == intensity
                    && chunk.IsSpawnChunk == isSpawn && IsFourWay(chunk))
                {
                    return;
                }
            }

            problems.Add("No 4-way " + typeName + " chunk. Every type needs one as its fallback.");
        }

        // The start cell only takes spawn chunks; every other cell takes a matching faction and intensity
        private static bool IsSameType(ChunkDefinition chunk, FloorCell cell)
        {
            if (cell.IsStart)
            {
                return chunk.IsSpawnChunk;
            }

            return !chunk.IsSpawnChunk && chunk.Owner == cell.Owner && chunk.Intensity == cell.Intensity;
        }

        private static bool IsFourWay(ChunkDefinition chunk)
        {
            return chunk.Openings == Direction.All;
        }

        private static string DescribeType(FloorCell cell)
        {
            return cell.IsStart ? "the start cell" : cell.Owner + " " + cell.Intensity;
        }
    }
}
