using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Data for one handmade chunk: who owns it, how intense it is, which edges have doors, and its tile prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "Chunk_", menuName = "Debt Dungeon/Floor Gen/Chunk Definition")]
    public class ChunkDefinition : ScriptableObject
    {
        [Header("Placement")]
        [SerializeField] private Faction _owner = Faction.Neutral;
        [SerializeField] private ChunkIntensity _intensity = ChunkIntensity.Neutral;
        [Tooltip("A Neutral Spawn chunk: only ever used for the player's start cell.")]
        [SerializeField] private bool _isSpawnChunk;
        [Tooltip("Edges that have a door slot. The generator plugs any opening the floor doesn't need.")]
        [SerializeField] private Direction _openings = Direction.All;

        [Header("Tiles")]
        [SerializeField] private ChunkLayout _layout;

        public Faction Owner => _owner;
        public ChunkIntensity Intensity => _intensity;
        public bool IsSpawnChunk => _isSpawnChunk;
        // "Everything" in the Inspector saves -1 (all bits); keep only the four real edges
        public Direction Openings => _openings & Direction.All;
        public ChunkLayout Layout => _layout;

        /// <summary>Checks this chunk against the authoring rules. Returns every problem found; empty means valid.</summary>
        public List<string> Validate()
        {
            List<string> problems = new List<string>();

            if (_layout == null || _layout.Floor == null || _layout.Walls == null)
            {
                problems.Add("Needs a chunk prefab with a ChunkLayout and both Floor and Walls tilemaps assigned.");
                return problems;
            }

            if (_openings == Direction.None)
            {
                problems.Add("Has no openings, so it can never connect to the rest of the floor.");
            }

            if (_isSpawnChunk && _owner != Faction.Neutral)
            {
                problems.Add("Spawn chunks must be Neutral.");
            }

            bool isNeutral = _owner == Faction.Neutral;
            if (isNeutral != (_intensity == ChunkIntensity.Neutral))
            {
                problems.Add("Neutral chunks need Neutral intensity; faction chunks need Minor or Major.");
            }

            CheckBorder(problems);
            CheckDoorsConnect(problems);
            return problems;
        }

        [ContextMenu("Validate Chunk")]
        private void LogValidation()
        {
            List<string> problems = Validate();
            if (problems.Count == 0)
            {
                Debug.Log(name + " passes every chunk rule.", this);
                return;
            }

            foreach (string problem in problems)
            {
                Debug.LogError(name + ": " + problem, this);
            }
        }

        // Every border tile is a wall, except the tiles of open door slots
        private void CheckBorder(List<string> problems)
        {
            for (int x = 0; x < ChunkRules.Size; x++)
            {
                for (int y = 0; y < ChunkRules.Size; y++)
                {
                    Vector2Int tile = new Vector2Int(x, y);
                    if (!ChunkRules.IsBorder(tile))
                    {
                        continue;
                    }

                    Direction doorEdge = ChunkRules.DoorEdgeAt(tile);
                    bool isOpenDoor = doorEdge != Direction.None && DirectionHelper.Contains(_openings, doorEdge);
                    bool hasWall = HasWall(tile);

                    if (isOpenDoor && hasWall)
                    {
                        problems.Add("The open " + doorEdge + " door slot has a wall at " + tile + ".");
                    }
                    else if (!isOpenDoor && !hasWall)
                    {
                        problems.Add("Gap in the border wall at " + tile + ".");
                    }
                }
            }
        }

        // Breadth-first search from one open door: every other open door tile must be reachable
        private void CheckDoorsConnect(List<string> problems)
        {
            List<Vector2Int> doorTiles = new List<Vector2Int>();
            foreach (Direction edge in DirectionHelper.Cardinals)
            {
                if (!DirectionHelper.Contains(_openings, edge))
                {
                    continue;
                }

                for (int i = 0; i < ChunkRules.DoorWidth; i++)
                {
                    doorTiles.Add(ChunkRules.DoorTile(edge, i));
                }
            }

            if (doorTiles.Count == 0)
            {
                return;
            }

            HashSet<Vector2Int> reached = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            if (IsWalkable(doorTiles[0]))
            {
                reached.Add(doorTiles[0]);
                queue.Enqueue(doorTiles[0]);
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Direction direction in DirectionHelper.Cardinals)
                {
                    Vector2Int next = current + DirectionHelper.ToOffset(direction);
                    if (!IsInside(next) || reached.Contains(next) || !IsWalkable(next))
                    {
                        continue;
                    }

                    reached.Add(next);
                    queue.Enqueue(next);
                }
            }

            foreach (Vector2Int door in doorTiles)
            {
                if (!reached.Contains(door))
                {
                    problems.Add("Door tile " + door + " can't be reached from the other doors.");
                }
            }
        }

        private bool HasWall(Vector2Int tile)
        {
            return _layout.Walls.HasTile(new Vector3Int(tile.x, tile.y, 0));
        }

        private bool IsWalkable(Vector2Int tile)
        {
            return _layout.Floor.HasTile(new Vector3Int(tile.x, tile.y, 0)) && !HasWall(tile);
        }

        private static bool IsInside(Vector2Int tile)
        {
            return tile.x >= 0 && tile.y >= 0 && tile.x < ChunkRules.Size && tile.y < ChunkRules.Size;
        }
    }
}
