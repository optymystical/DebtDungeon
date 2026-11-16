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
