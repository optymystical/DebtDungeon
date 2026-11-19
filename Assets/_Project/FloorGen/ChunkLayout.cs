using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Sits on the root of a chunk prefab and points at its tilemaps. Painted tiles live here;
    /// placement data lives in the ChunkDefinition that references this prefab.
    /// </summary>
    public class ChunkLayout : MonoBehaviour
    {
        [SerializeField] private Tilemap _floor;
        [SerializeField] private Tilemap _walls;
        [Tooltip("Marker tiles: spawns, chests, water, traps. Optional until marker resolution is built.")]
        [SerializeField] private Tilemap _markers;

        public Tilemap Floor => _floor;
        public Tilemap Walls => _walls;
        public Tilemap Markers => _markers;
    }
}
