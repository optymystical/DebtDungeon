using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    // Sits on the root of every chunk prefab. Type, where it lies, etc.
    public class Chunk : MonoBehaviour
    {
        public const int Size = 16; // tiles per side
        public const int DoorStart = 7; // door slot = should be true for tiles 7 and 8 on every edge
        public const int DoorWidth = 2;

        [SerializeField] private Faction owner = Faction.Neutral;
        [SerializeField] private Intensity band = Intensity.Neutral;
        [SerializeField] private ChunkRole role = ChunkRole.Normal;

        [SerializeField] private Tilemap floor;
        [SerializeField] private Tilemap walls;

        public Faction Owner { get { return owner; } }
        public Intensity Band { get { return band;  } }
        public ChunkRole Role { get { return role; } }
        public Tilemap Floor { get { return floor; } }
        public Tilemap Walls { get { return walls; } }
    }
}
