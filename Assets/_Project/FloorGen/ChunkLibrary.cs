using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        [Header("Marker tiles")]
        [Tooltip("Painted on a chunk's Markers tilemap. Each becomes a spawn point (or water) when the floor is built.")]
        [SerializeField] private TileBase _spawnerMarker;
        [SerializeField] private TileBase _stationMarker;
        [SerializeField] private TileBase _chestMarker;
        [SerializeField] private TileBase _enemyMarker;
        [SerializeField] private TileBase _waterMarker;

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

        /// <summary>True if this chunk is in the library and can appear on a floor.</summary>
        public bool Contains(ChunkDefinition chunk)
        {
            return _chunks.Contains(chunk);
        }

        /// <summary>Which marker a tile is. False for any tile that isn't one of the five marker tiles.</summary>
        public bool TryGetMarkerKind(TileBase tile, out MarkerKind kind)
        {
            kind = MarkerKind.Spawner;
            if (tile == null)
            {
                return false;
            }

            if (tile == _spawnerMarker)
            {
                kind = MarkerKind.Spawner;
                return true;
            }

            if (tile == _stationMarker)
            {
                kind = MarkerKind.Station;
                return true;
            }

            if (tile == _chestMarker)
            {
                kind = MarkerKind.Chest;
                return true;
            }

            if (tile == _enemyMarker)
            {
                kind = MarkerKind.Enemy;
                return true;
            }

            if (tile == _waterMarker)
            {
                kind = MarkerKind.Water;
                return true;
            }

            return false;
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

            if (_spawnerMarker == null || _stationMarker == null || _chestMarker == null || _enemyMarker == null || _waterMarker == null)
            {
                problems.Add("All five marker tiles need to be assigned.");
            }

            foreach (ChunkDefinition chunk in _chunks)
            {
                if (chunk != null)
                {
                    CheckMarkers(chunk, problems);
                }
            }
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

        // One Station, in spawn chunks only; at least one Spawner in Major chunks, and only there;
        // Enemies in faction chunks only; every marker on walkable floor
        private void CheckMarkers(ChunkDefinition chunk, List<string> problems)
        {
            if (chunk.Layout == null)
            {
                return; // ChunkDefinition.Validate already reports a missing layout
            }

            int stations = 0;
            int spawners = 0;
            Tilemap markers = chunk.Layout.Markers;
            if (markers != null)
            {
                TileBase[] tiles = markers.GetTilesBlock(ChunkRules.LocalBounds);
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i] == null)
                    {
                        continue;
                    }

                    // GetTilesBlock fills a row at a time: x first, then y
                    Vector2Int tile = new Vector2Int(i % ChunkRules.Size, i / ChunkRules.Size);
                    string where = chunk.name + " " + tile;

                    MarkerKind kind;
                    if (!TryGetMarkerKind(tiles[i], out kind))
                    {
                        problems.Add(where + ": the Markers tilemap has a tile that isn't a marker.");
                        continue;
                    }

                    if (!IsWalkable(chunk, tile))
                    {
                        problems.Add(where + ": " + kind + " marker isn't on walkable floor.");
                    }

                    if (kind == MarkerKind.Station)
                    {
                        stations++;
                    }
                    else if (kind == MarkerKind.Spawner)
                    {
                        spawners++;
                        if (chunk.Intensity != ChunkIntensity.Major)
                        {
                            problems.Add(where + ": Spawners only go in Major chunks.");
                        }
                    }
                    else if (kind == MarkerKind.Enemy && chunk.Owner == Faction.Neutral)
                    {
                        problems.Add(where + ": Enemies only go in faction chunks.");
                    }
                }
            }

            if (chunk.IsSpawnChunk && stations != 1)
            {
                problems.Add(chunk.name + ": spawn chunks need exactly one Station marker (found " + stations + ").");
            }

            if (!chunk.IsSpawnChunk && stations > 0)
            {
                problems.Add(chunk.name + ": only spawn chunks can have a Station marker.");
            }

            if (chunk.Intensity == ChunkIntensity.Major && spawners == 0)
            {
                problems.Add(chunk.name + ": Major chunks need at least one Spawner marker.");
            }
        }

        private static bool IsWalkable(ChunkDefinition chunk, Vector2Int tile)
        {
            Vector3Int position = new Vector3Int(tile.x, tile.y, 0);
            return chunk.Layout.Floor.HasTile(position) && !chunk.Layout.Walls.HasTile(position);
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
