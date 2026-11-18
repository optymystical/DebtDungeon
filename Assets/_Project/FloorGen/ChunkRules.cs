using UnityEngine;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// The fixed rules every chunk follows: its size where the door slots sit.
    /// </summary>
    public static class ChunkRules
    {
        public const int Size = FloorLayoutGenerator.ChunkSize;
        public const int LastIndex = Size - 1;
        public const int DoorStart = 7;
        public const int DoorWidth = 2;

        ///<summary>Half the chunk size. The player starts on the corner shared by the four middle tiles.</summary>
        public const int Middle = Size / 2;

        /// <summary>Chunk-local tile number <paramref name="index"/> (0 or 1) of the door slot on an edge.</summary>
        public static Vector2Int DoorTile(Direction edge, int index)
        {
            int along = DoorStart + index;
            switch (edge)
            {
                case Direction.North:
                    return new Vector2Int(along, LastIndex);
                case Direction.South:
                    return new Vector2Int(along, 0);
                case Direction.East:
                    return new Vector2Int(LastIndex, along);
                case Direction.West:
                    return new Vector2Int(0, along);
                default:
                    return Vector2Int.zero;
            }
        }

        /// <summary>True for tiles on the outer ring of the chunk.</summary>
        public static bool IsBorder(Vector2Int tile)
        {
            return tile.x == 0 || tile.y == 0 || tile.x == LastIndex || tile.y == LastIndex;
        }

        ///<summary>The edge whose door slot contains this tile, or none</summary>
        public static Direction DoorEdgeAt(Vector2Int tile)
        {
            foreach (Direction edge in DirectionHelper.Cardinals)
            {
                for (int i = 0; i < DoorWidth; i++)
                {
                    if (DoorTile(edge, i) == tile)
                    {
                        return edge;
                    }
                }
            }

            return Direction.None;
        }
    }
}
