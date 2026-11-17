using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Stateless helpers for Direction.
    /// </summary>
    public static class DirectionHelper
    {
        /// <summary>The four single directions in a fixed order. A fixed order keeps seeded results the same.</summary>
        public static readonly Direction[] Cardinals = { Direction.North, Direction.East, Direction.South, Direction.West };

        /// <summary>One grid step in a direction.</summary>
         public static Vector2Int ToOffset(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector2Int.up;
                case Direction.East:
                    return Vector2Int.right;
                case Direction.South:
                    return Vector2Int.down;
                case Direction.West:
                    return Vector2Int.left;
                default:
                    return Vector2Int.zero;
            }
        }

        ///<summary>True if the set includes this direction.</summary>
        public static bool Contains(Direction set, Direction direction)
        {
            return (set & direction) == direction;
        }

        ///<summary>True if every opening is needed is also available. This is the chunk-fits-cell rule.</summary>
        public static bool Covers(Direction available, Direction needed)
        {
            return (available & needed) == needed;
        }
    }
}
