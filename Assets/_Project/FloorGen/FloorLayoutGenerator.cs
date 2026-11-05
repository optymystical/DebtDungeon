using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;


namespace DebtDungeon.FloorGen
{
    //building the cell layout from a seed. No GameObject needed for now
    public class  FloorLayoutGenerator
    {
        private readonly FloorSettings settings;
        private readonly System.Random rng;
        private CellData[,] cells;

        public Vector2Int StartCell {  get; private set; }
        public Vector2Int GoblinHotspot { get; private set; }
        public Vector2Int SpiderHotspot { get; private set; }
        public Vector2Int ExitCell { get; private set; }
        public int UnreachableCount { get; private set; }

        public FloorLayoutGenerator(FloorSettings settings, int seed)
        {
            this.settings = settings;
            rng = new System.Random(seed);
        }

        public CellData[,] Generate()
        {
            CreateCells();
            PickStartCell();
            PlaceHotspots();
            BuildHeatMap();
            AssignBands();
            BuildSpanningTree();
            AddExtraConnections();
            PlaceExit();
            return cells;
        }

        private void CreateCells()
        {
            int size = settings.gridSize;
            cells = new CellData[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    cells[x, y] = new CellData(new Vector2Int(x, y));
                }
            }
        }

        private void PickStartCell()
        {
            // inner area only, never the outer ring 
            int x = rng.Next(1, settings.gridSize - 1);
            int y = rng.Next(1, settings.gridSize - 1);

            StartCell = new Vector2Int(x, y);
            cells[x, y].IsStart = true;
        }

        private void PlaceHotspots()
        {
            List<Vector2Int> goblinOptions = GetCellsFarFrom(StartCell, settings.minHotspotDistance);
            if (goblinOptions.Count ==0)
            {
                Debug.LogWarning("No valid goblin hotspot; using a corner. Check minHotspotDistance.");
                goblinOptions.Add(new Vector2Int(0, 0));
            }
            GoblinHotspot = goblinOptions[rng.Next(goblinOptions.Count)];

            //SpiderHotspot must be far from the start && from the golbins
            List<Vector2Int> spiderOptions = new List<Vector2Int>();
            foreach (Vector2Int option in goblinOptions)
            {
                if (ChebyshevDistance(option, GoblinHotspot) >= settings.minHotspotDistance)
                {
                    spiderOptions.Add(option);
                }
            }
            if (spiderOptions.Count == 0)
            {
                Debug.LogWarning("No valid spider hotspot; using the opposite corner.");
                spiderOptions.Add(new Vector2Int(settings.gridSize - 1, settings.gridSize - 1));
            }
            SpiderHotspot = spiderOptions[rng.Next(spiderOptions.Count)];
        }

        private void BuildHeatMap()
        {
            // stength varies per seed, so some floors lean goblin and others spider
            float goblinStrength = RandomRange(settings.minStrength, settings.maxStrength);
            float spiderStrength = RandomRange(settings.minStrength, settings.maxStrength);

            foreach (CellData cell in cells)
            {
                cell.GoblinHeat = Influence(cell.Coord, GoblinHotspot, goblinStrength);
                cell.SpiderHeat = Influence(cell.Coord, SpiderHotspot, spiderStrength);
            }
        }

        private void AssignBands()
        {
            foreach (CellData cell in cells)
            {
                if (cell.IsStart) continue; // always staying neutral on start

                float topHeat = Mathf.Max(cell.GoblinHeat, cell.SpiderHeat);
                if (topHeat < settings.minorThreshold) continue; // too weak so it stays neutral

                cell.Owner = cell.GoblinHeat >= cell.SpiderHeat ? Faction.Goblin : Faction.Spider;
                cell.Band = topHeat >= settings.majorThreshold ? Intensity.Major : Intensity.Minor;
            }

            LimitMajorCells(Faction.Goblin);
            LimitMajorCells(Faction.Spider);
        }

        //demoting the weakest major cells until the faction is at the cap
        private void LimitMajorCells(Faction faction)
        {

            int safety = 100; // 25 should not need more than this

            while (CountMajorCells(faction) > settings.maxMajorPerFaction)
            {
                if (--safety <= 0)
                {
                    Debug.LogError("LimitMajorCells stopped: loop is not making progress.");
                    return;
                }

                CellData weakest = null;
                foreach (CellData cell in cells)
                {
                    if (cell.Owner != faction || cell.Band != Intensity.Major) continue;

                    if (weakest == null || HeatFor(cell, faction) < HeatFor(weakest, faction))
                    {
                        weakest = cell;
                    }
                }

                weakest.Band = Intensity.Minor;
            }
        }


        //randomized depth-first search. Should carve a path to every cell with no loops
        private void BuildSpanningTree()
        {
            int size = settings.gridSize;
            bool[,] visited = new bool[size, size];
            Stack<Vector2Int> stack = new Stack<Vector2Int>();

            visited[StartCell.x, StartCell.y] = true;
            stack.Push(StartCell);

            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();

                //collecting neighbors we haven't visisted yet
                List<Direction> options = new List<Direction>();
                foreach (Direction dir in DirectionHelper.All)
                {
                    Vector2Int next = current + DirectionHelper.Offset(dir);
                    if (InBounds(next) && !visited[next.x, next.y])
                    {
                        options.Add(dir);
                    }
                }

                if (options.Count == 0)
                {
                    stack.Pop(); // dead end - back up and try from the previous cell
                    continue;
                }

                Direction chosen = options[rng.Next(options.Count)];
                Vector2Int nextCell = current + DirectionHelper.Offset(chosen);

                Connect(current, chosen);
                visited[nextCell.x, nextCell.y] = true;
                stack.Push(nextCell);
            }
        }

        // adding a few extra links to create loops so there is more than one route
        private void AddExtraConnections()
        {
            int added = 0;
            int attempts = 0;

            while (added < settings.extraConnections && attempts < 100)
            {
                attempts++;

                Vector2Int cell = new Vector2Int(rng.Next(settings.gridSize), rng.Next(settings.gridSize));
                Direction dir = DirectionHelper.All[rng.Next(DirectionHelper.All.Length)];
                Vector2Int neighbor = cell + DirectionHelper.Offset(dir);

                // =====

                if (!InBounds(neighbor)) continue;
                if (cells[cell.x, cell.y].IsOpen(dir)) continue;  // already connected

                // ====
                Connect(cell, dir);
                added++;
            }
        }

        // adding the breadth-first search from the start: walking should be true distance to every cell
        private void PlaceExit()
        {
            foreach (CellData cell in cells)
            {
                cell.DistanceFromStart = -1;
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            cells[StartCell.x, StartCell.y].DistanceFromStart = 0;
            queue.Enqueue(StartCell);

            while (queue.Count >0)
            {
                Vector2Int current = queue.Dequeue();
                CellData currentCell = cells[current.x, current.y];

                foreach (Direction dir in DirectionHelper.All)
                {
                    if (!currentCell.IsOpen(dir)) continue; // should be a wall, so can't walk this way

                    Vector2Int next = current + DirectionHelper.Offset(dir);
                    CellData nextCell = cells[next.x, next.y];
                    if (nextCell.DistanceFromStart != -1) continue; // already reached

                    nextCell.DistanceFromStart = currentCell.DistanceFromStart + 1;
                    queue.Enqueue(next);
                }
            }

            // exit = farthest cell that isn't a faction stronghold
            int farthest = -1;
            List<Vector2Int> candidates = new List<Vector2Int>();
            UnreachableCount = 0;

            foreach (CellData cell in cells)
            {
                if (cell.DistanceFromStart == -1) UnreachableCount++;
                if (cell.Band == Intensity.Major) continue;

                if (cell.DistanceFromStart > farthest)
                {
                    farthest = cell.DistanceFromStart;
                    candidates.Clear();
                }
                if (cell.DistanceFromStart == farthest)
                {
                    candidates.Add(cell.Coord);
                }
            }

            ExitCell = candidates[rng.Next(candidates.Count)];
            cells[ExitCell.x, ExitCell.y].IsExit = true;
        }

        // <----- Listing Helpers for Code ----->

        private List<Vector2Int> GetCellsFarFrom(Vector2Int origin, int minDistance)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (CellData cell in cells)
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
            float falloff = 1f - (distance / settings.heatRadius);
            return strength * Mathf.Max(0f, falloff);
        }

        private int CountMajorCells(Faction faction)
        {
            int count = 0;
            foreach (CellData cell in cells)
            {
                if (cell.Owner == faction && cell.Band == Intensity.Major) count++;
            }

            return count;
        }

        private static float HeatFor(CellData cell, Faction faction)
        {
            return faction == Faction.Goblin ? cell.GoblinHeat : cell.SpiderHeat;
        }

        private static int ChebyshevDistance(Vector2Int a,  Vector2Int b)
        {
            // grid distance where diagonal neighbors count as 1 step
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private void Connect(Vector2Int from, Direction dir)
        {
            Vector2Int to = from + DirectionHelper.Offset(dir);
            cells[from.x, from.y].Openings |= dir;
            cells[to.x, to.y].Openings |= DirectionHelper.Opposite(dir);
        }

        private bool InBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.y >= 0 && coord.x < settings.gridSize && coord.y < settings.gridSize;
        }
    }
}
