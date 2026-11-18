using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace DebtDungeon.FloorGen
{
    /// <summary>
    /// Builds the floor in the scene. Game calls Generate(seed) with the run seed; sandbox scenes use the debug seed.
    /// </summary>
    public class FloorGenerator : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private FloorGenConfig _config;
        [SerializeField] private ChunkLibrary _library;

        [Header("Tilemaps")]
        [SerializeField] private Tilemap _floorTilemap;
        [Tooltip("Needs a Tilemap Collider 2D merged into a Composite Collider 2D, on the Walls layer.")]
        [SerializeField] private Tilemap _wallTilemap;
        [Tooltip("Wall tile used to close door slots a cell doesn't connect through.")]
        [SerializeField] private TileBase _plugTile;

        [Header("Debug")]
        [Tooltip("Seed used when this component builds the floor itself (sandbox testing). Game passes the run seed instead.")]
        [SerializeField] private int _debugSeed = 12345;
        [Tooltip("Build a floor on Start with the debug seed. Turn off in Main, where Game decides when to build.")]
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _showCellOverlay = true;
        [SerializeField, Range(0f, 1f)] private float _overlayAlpha = 0.35f;

        private FloorLayoutGenerator _layout;
        private int _currentSeed;
        private ChunkDefinition[,] _placedChunks;
        private FloorQuery _floorQuery;

        public int CurrentSeed => _currentSeed;

        /// <summary>
        /// The last floor built, or null before the first Generate. Most systems get it from GameEvents.FloorGenerated instead.
        /// </summary>
        public IFloorQuery Floor => _floorQuery;

        private void Start()
        {
            if (_generateOnStart)
            {
                Generate(_debugSeed);
            }
        }

        /// <summary>Builds a floor from a seed. The same seed always builds the same floor.</summary>
        public void Generate(int seed)
        {
            if (!IsSetUp())
            {
                return;
            }

            _currentSeed = seed;
            _layout = new FloorLayoutGenerator(_config, seed);
            FloorCell[,] cells = _layout.Generate();
            _placedChunks = _library.PickAll(cells, seed);
            new FloorStamper(_floorTilemap, _wallTilemap, _plugTile).Stamp(cells, _placedChunks);

            // Spawn points come from the chunk markers (next card); empty until then
            _floorQuery = new FloorQuery(cells, _layout.StartCell, _floorTilemap, _wallTilemap, new List<SpawnPoint>());

            // Log before raising, so the seed is in the Console even if a listener throws
            Debug.Log("Floor generated. Seed: " + seed, this);
            GameEvents.RaiseFloorGenerated(_floorQuery);
        }

        // Logs every missing reference at once, so a broken setup is obvious in the Console
        private bool IsSetUp()
        {
            bool isSetUp = true;
            if (_config == null)
            {
                Debug.LogError("FloorGenerator needs a FloorGenConfig.", this);
                isSetUp = false;
            }

            if (_library == null)
            {
                Debug.LogError("FloorGenerator needs a ChunkLibrary.", this);
                isSetUp = false;
            }

            if (_floorTilemap == null || _wallTilemap == null)
            {
                Debug.LogError("FloorGenerator needs both the floor and wall tilemaps.", this);
                isSetUp = false;
            }

            if (_plugTile == null)
            {
                Debug.LogError("FloorGenerator needs a plug tile for unused doors.", this);
                isSetUp = false;
            }

            return isSetUp;
        }

        [ContextMenu("Generate With Debug Seed")]
        private void GenerateWithDebugSeed()
        {
            Generate(_debugSeed);
        }

        [ContextMenu("Generate With New Random Seed")]
        private void GenerateWithNewRandomSeed()
        {
            _debugSeed = UnityEngine.Random.Range(0, int.MaxValue);
            Generate(_debugSeed);
        }

        // Run this before saving the scene, so thousands of stamped tiles don't get committed
        [ContextMenu("Clear Floor")]
        private void ClearFloor()
        {
            if (_floorTilemap != null)
            {
                _floorTilemap.ClearAllTiles();
            }

            if (_wallTilemap != null)
            {
                _wallTilemap.ClearAllTiles();
            }

            _layout = null;
            _placedChunks = null;
            _floorQuery = null;
        }

        private void OnDrawGizmos()
        {
            if (_layout == null || _layout.Cells == null || !_showCellOverlay)
            {
                return;
            }

            Vector3 cellSize = new Vector3(FloorLayoutGenerator.ChunkSize - 1, FloorLayoutGenerator.ChunkSize - 1, 0.1f);
            foreach (FloorCell cell in _layout.Cells)
            {
                Color color = ColorFor(cell);
                color.a = _overlayAlpha;
                Gizmos.color = color;
                Gizmos.DrawCube(CellCenter(cell.Coord), cellSize);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(CellCenter(_layout.StartCell), FloorLayoutGenerator.ChunkSize * 0.3f);

            Gizmos.color = Color.yellow;
            Vector3 markerSize = cellSize * 0.6f;
            Gizmos.DrawWireCube(CellCenter(_layout.GoblinHotspot), markerSize);
            Gizmos.DrawWireCube(CellCenter(_layout.SpiderHotspot), markerSize);
        }

        private Vector3 CellCenter(Vector2Int coord)
        {
            float half = FloorLayoutGenerator.ChunkSize * 0.5f;
            Vector3 offset = new Vector3(coord.x * FloorLayoutGenerator.ChunkSize + half,
                                         coord.y * FloorLayoutGenerator.ChunkSize + half,
                                         0f);
            return transform.position + offset;
        }

        // Design doc palette: goblins green, spiders purple, neutral grey
        private static Color ColorFor(FloorCell cell)
        {
            if (cell.Owner == Faction.Goblin)
            {
                return cell.Intensity == ChunkIntensity.Major ? new Color(0.1f, 0.5f, 0.1f) : new Color(0.5f, 0.85f, 0.4f);
            }

            if (cell.Owner == Faction.Spider)
            {
                return cell.Intensity == ChunkIntensity.Major ? new Color(0.4f, 0.1f, 0.5f) : new Color(0.75f, 0.55f, 0.9f);
            }

            return new Color(0.6f, 0.6f, 0.65f);
        }
    }
}
