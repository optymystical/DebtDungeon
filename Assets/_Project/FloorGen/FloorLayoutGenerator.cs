using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Builds a floor's chunk-cell layout from a seed: the start cell, the faction heat map and intensity bands.
    /// The aim is to run it thousands of times without a scene
    /// </summary>
    public class FloorLayoutGenerator
    {
        /// <summary>Tiles per side of one chunk.</summary>
        public const int ChunkSize = 16;

        private const int OuterRingWidth = 1;
        private const int MaxDemotionPasses = 100;
        private const int MaxExtraConnectionAttempts = 100;

        private readonly FloorGenConfig _config;
        private readonly System.Random _random;
        private FloorCell[,] _cells;

        public FloorLayoutGenerator(FloorGenConfig config, int seed)
        {
            _config = config;
            _random = new System.Random(seed);
        }

        public FloorCell[,] Cells => _cells;
        public Vector2Int StartCell { get; private set; }
        public Vector2Int GoblinHotspot { get; private set; }
        public Vector2Int SpiderHotspot { get; private set; }

        /// <summary>Builds the layout. The same config and seed always give the same layout.</summary>
        public FloorCell[,] Generate()
        {
            CreateCells();
            PickStartCell();
            PlaceHotspots();
            BuildHeatMap();
            AssignIntensity();
            BuildSpanningTree();
            AddExtraConnections();
            return _cells;
        }

        /// <summary>Grid distance where diagonal neighbors count as one step.</summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void CreateCells()
        {
            int size = _config.GridSize;
            _cells = new FloorCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    _cells[x, y] = new FloorCell(new Vector2Int(x, y));
                }
            }
        }

        private void PickStartCell()
        {
            // Inner area only: never on the outer ring (1..3 on a 5x5 grid)
            int x = _random.Next(OuterRingWidth, _config.GridSize - OuterRingWidth);
            int y = _random.Next(OuterRingWidth, _config.GridSize - OuterRingWidth);

            StartCell = new Vector2Int(x, y);
            _cells[x, y].IsStart = true;
        }

        private void PlaceHotspots()
        {
            List<Vector2Int> goblinOptions = GetCellsFarFrom(StartCell, _config.MinHotspotDistance);
            if (goblinOptions.Count == 0)
            {
                Debug.LogWarning("No valid goblin hotspot; using a corner. Check MinHotspotDistance.");
                goblinOptions.Add(new Vector2Int(0, 0));
            }
            GoblinHotspot = goblinOptions[_random.Next(goblinOptions.Count)];

            // The spider hotspot must be far from the start AND from the goblins
            List<Vector2Int> spiderOptions = new List<Vector2Int>();
            foreach (Vector2Int option in goblinOptions)
            {
                if (ChebyshevDistance(option, GoblinHotspot) >= _config.MinHotspotDistance)
                {
                    spiderOptions.Add(option);
                }
            }

            if (spiderOptions.Count == 0)
            {
                Debug.LogWarning("No valid spider hotspot; using the opposite corner.");
                spiderOptions.Add(new Vector2Int(_config.GridSize - 1, _config.GridSize - 1));
            }
            SpiderHotspot = spiderOptions[_random.Next(spiderOptions.Count)];
        }

        private void BuildHeatMap()
        {
            // Strength varies per seed, so some floors lean goblin and others spider
            float goblinStrength = RandomRange(_config.MinHotspotStrength, _config.MaxHotspotStrength);
            float spiderStrength = RandomRange(_config.MinHotspotStrength, _config.MaxHotspotStrength);

            foreach (FloorCell cell in _cells)
            {
                cell.GoblinHeat = Influence(cell.Coord, GoblinHotspot, goblinStrength);
                cell.SpiderHeat = Influence(cell.Coord, SpiderHotspot, spiderStrength);
            }
        }

        private void AssignIntensity()
        {
            foreach (FloorCell cell in _cells)
            {
                if (cell.IsStart)
                {
                    continue;
                }

                float topHeat = Mathf.Max(cell.GoblinHeat, cell.SpiderHeat);
                if (topHeat < _config.MinorThreshold)
                {
                    continue;
                }

                cell.Owner = cell.GoblinHeat >= cell.SpiderHeat ? Faction.Goblin : Faction.Spider;
                cell.Intensity = topHeat >= _config.MajorThreshold ? ChunkIntensity.Major : ChunkIntensity.Minor;
            }

            LimitMajorCells(Faction.Goblin);
            LimitMajorCells(Faction.Spider);
        }

        // Demotes the weakest Major cells until the faction is at the cap
        private void LimitMajorCells(Faction faction)
        {
            int passes = 0;
            while (CountMajorCells(faction) > _config.MaxMajorPerFaction)
            {
                passes++;
                if (passes > MaxDemotionPasses)
                {
                    Debug.LogError("LimitMajorCells stopped: the loop is not making progress.");
                    return;
                }

                FloorCell weakest = null;
                foreach (FloorCell cell in _cells)
                {
                    if (cell.Owner != faction || cell.Intensity != ChunkIntensity.Major)
                    {
                        continue;
                    }

                    if (weakest == null || HeatFor(cell, faction) < HeatFor(weakest, faction))
                    {
                        weakest = cell;
                    }
                }

                weakest.Intensity = ChunkIntensity.Minor;
            }
        }

        // Randomized depth-first search from the start: carves one path to every cell, with no loops
        private void BuildSpanningTree()
        {
            int size = _config.GridSize;
            bool[,] visited = new bool[size, size];
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            List<Direction> options = new List<Direction>();

            visited[StartCell.x, StartCell.y] = true;
            stack.Push(StartCell);

            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();

                options.Clear();
                foreach (Direction direction in DirectionHelper.Cardinals)
                {
                    Vector2Int next = current + DirectionHelper.ToOffset(direction);
                    if (IsInGrid(next) && !visited[next.x, next.y])
                    {
                        options.Add(direction);
                    }
                }

                if (options.Count == 0)
                {
                    stack.Pop(); // Dead end: back up to the previous cell
                    continue;
                }

                Direction chosen = options[_random.Next(options.Count)];
                Vector2Int chosenCell = current + DirectionHelper.ToOffset(chosen);
                Connect(current, chosen);
                visited[chosenCell.x, chosenCell.y] = true;
                stack.Push(chosenCell);
            }
        }

        // Adds a few extra links so the floor has loops and more than one route
        private void AddExtraConnections()
        {
            int added = 0;
            int attempts = 0;

            while (added < _config.ExtraConnections && attempts < MaxExtraConnectionAttempts)
            {
                attempts++;

                Vector2Int cell = new Vector2Int(_random.Next(_config.GridSize), _random.Next(_config.GridSize));
                Direction direction = DirectionHelper.Cardinals[_random.Next(DirectionHelper.Cardinals.Length)];
                Vector2Int neighbor = cell + DirectionHelper.ToOffset(direction);

                if (!IsInGrid(neighbor) || _cells[cell.x, cell.y].IsOpen(direction))
                {
                    continue;
                }

                Connect(cell, direction);
                added++;
            }
        }

        // Opens the shared edge on both sides, so the two chunks' doors line up
        private void Connect(Vector2Int cell, Direction direction)
        {
            Vector2Int neighbor = cell + DirectionHelper.ToOffset(direction);
            _cells[cell.x, cell.y].Openings |= direction;
            _cells[neighbor.x, neighbor.y].Openings |= DirectionHelper.Opposite(direction);
        }

        private bool IsInGrid(Vector2Int coord)
        {
            return coord.x >= 0 && coord.y >= 0 && coord.x < _config.GridSize && coord.y < _config.GridSize;
        }

        private List<Vector2Int> GetCellsFarFrom(Vector2Int origin, int minDistance)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (FloorCell cell in _cells)
            {
                if (ChebyshevDistance(cell.Coord, origin) >= minDistance)
                {
                    result.Add(cell.Coord);
                }
            }
            return result;
        }

        private float Influence(Vector2Int cell, Vector2Int hotspot, float strength)
        {
            float distance = Vector2Int.Distance(cell, hotspot);
            float falloff = 1f - (distance / _config.HeatRadius);
            return strength * Mathf.Max(0f, falloff);
        }

        private int CountMajorCells(Faction faction)
        {
            int count = 0;
            foreach (FloorCell cell in _cells)
            {
                if (cell.Owner == faction && cell.Intensity == ChunkIntensity.Major)
                {
                    count++;
                }
            }
            return count;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private static float HeatFor(FloorCell cell, Faction faction)
        {
            return faction == Faction.Goblin ? cell.GoblinHeat : cell.SpiderHeat;
        }
    }
}
