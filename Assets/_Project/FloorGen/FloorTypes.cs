using UnityEngine;

namespace DebtDungeon.FloorGen
{
    public enum Faction { Neutral, Goblin, Spider}
    public enum Intensity { Neutral, Minor, Major}

    [System.Flags]
    public enum Direction
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    public enum ChunkRole { Normal, Start, Exit}

    public static class DirectionHelper
    {
        public static readonly Direction[] All = { Direction.North, Direction.East, Direction.South, Direction.West };

        public static Vector2Int Offset(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return Vector2Int.up;
                case Direction.East: return Vector2Int.right;
                case Direction.South: return Vector2Int.down;
                case Direction.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }

        public static Direction Opposite(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return Direction.South;
                case Direction.East: return Direction.West;
                case Direction.South: return Direction.North;
                case Direction.West: return Direction.East;
                default: return Direction.None;
            }
        }
    }

    // one cell of the floor grid (becomes on 16x16 chunk later)
    public class CellData
    {
        public Vector2Int Coord;
        public float GoblinHeat;
        public float SpiderHeat;
        public Faction Owner = Faction.Neutral;
        public Intensity Band = Intensity.Neutral;
        public bool IsStart;
        public bool IsExit;

        public CellData(Vector2Int coord)
        {
            Coord = coord;
        }

        public Direction Openings = Direction.None;
        public int DistanceFromStart = -1;

        public bool IsOpen(Direction dir)
        {
            return (Openings & dir) != 0;
        }
        
    }

    //tuning values, editable in the inspector (becomes a scriptableobject later)
    [System.Serializable]
    public class FloorSettings
    {
        [Header("Connections")]
        [Min(0)] public int extraConnections = 6;
        [Min(3)] public int gridSize = 5;

        [Header("Hotspots")]
        public int minHotspotDistance = 2;
        public float minStrength = 0.8f;
        public float maxStrength = 1.2f;
        public float heatRadius = 3f;

        [Header("Bands")]
        [Range(0f, 1f)] public float minorThreshold = 0.35f;
        [Range(0f, 1.5f)] public float majorThreshold = 0.75f;
        public int maxMajorPerFaction = 2;
    }
}
