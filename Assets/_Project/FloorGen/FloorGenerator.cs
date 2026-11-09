using UnityEngine;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    public class FloorGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private FloorSettings settings = new FloorSettings();
        [SerializeField] private int tilesPerChunk = 16;

        [Header("Tiles")]
        [SerializeField] private ChunkLibrary chunkLibrary;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;

        [Header("Debug View")]
        [SerializeField] private bool showCellOverlay = true;
        [SerializeField, Range(0f, 1f)] private float overlayAlpha = 0.3f;

        private FloorLayoutGenerator layout;
        private CellData[,] cells;

        void Start()
        {
            Generate();
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            layout = new FloorLayoutGenerator(settings, seed);
            cells = layout.Generate();
            Debug.Log("Floor generated. Seed: " + seed + 
                " | Exit distance: " + cells[layout.ExitCell.x, layout.ExitCell.y].DistanceFromStart + 
                " | Unreachable cells: " + layout.UnreachableCount);

            if (chunkLibrary != null && floorTilemap != null && wallTilemap != null)
            {
                System.Diagnostics.Stopwatch tileTimer = System.Diagnostics.Stopwatch.StartNew();
                FloorTileBuilder builder = new FloorTileBuilder(chunkLibrary, floorTilemap, wallTilemap, seed);
                builder.Build(cells);
                tileTimer.Stop();
                Debug.Log("Tiles built in " + tileTimer.ElapsedMilliseconds + " ms");
            }
            else
            {
                Debug.LogWarning("Tiles skipped: assigne the Chunk Library and both Tilempas on FloorGenerator.");
            }
        }

        [ContextMenu("Generate With New Seed")]
        public void GenerateWithNewSeed()
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
            Generate();
        }

        // removes generated tiles so they don't get saved into the scene file
        [ContextMenu("Clear Tiles")]
        public void ClearTiles()
        {
            if (floorTilemap != null) floorTilemap.ClearAllTiles();
            if (wallTilemap != null) wallTilemap.ClearAllTiles();
        }

        // runs 1000 layouts with the current settings and logs a summary. No play mode needed
        [ContextMenu("Run Evidence Test (1000 Floors)")]
        public void RunEvidenceTest()
        {
            FloorEvidenceTest.Run(settings, 1000, 1);
        }

        void OnDrawGizmos()
        {
            if (cells == null) return;

            if (showCellOverlay)
            {
                foreach (CellData cell in cells)
                {
                    Color color = ColorFor(cell);
                    color.a = overlayAlpha; //see-through to the tiles show underneath
                    Gizmos.color = color;
                    Gizmos.DrawCube(CellCenter(cell.Coord), new Vector3(tilesPerChunk - 1, tilesPerChunk - 1, 0.1f));
                }
            }

            // connections - drawing each link once (east and north to avoid doubles)
            Gizmos.color = Color.white;
            foreach (CellData cell in cells)
            {
                Vector3 from = CellCenter(cell.Coord);
                if (cell.IsOpen(Direction.East)) Gizmos.DrawLine(from, CellCenter(cell.Coord + Vector2Int.right)); 
                if (cell.IsOpen(Direction.North)) Gizmos.DrawLine(from, CellCenter(cell.Coord + Vector2Int.up));

                //additional research done to properly draw the labels in scene view for testing
#if UNITY_EDITOR
                //BFS distance label in the scene view
                UnityEditor.Handles.Label(from + new Vector3(-6f, 6f, 0f), cell.DistanceFromStart.ToString());
#endif
            }

            //start = cyan circle, hotspots = yellow outlines
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(CellCenter(layout.StartCell), tilesPerChunk * 0.3f);

            Gizmos.color = Color.yellow;
            Vector3 markerSize = new Vector3(tilesPerChunk * 0.6f, tilesPerChunk * 0.6f, 0.1f);
            Gizmos.DrawWireCube(CellCenter(layout.GoblinHotspot), markerSize);
            Gizmos.DrawWireCube(CellCenter(layout.SpiderHotspot), markerSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(CellCenter(layout.ExitCell), markerSize * 1.2f);
        }

        private Vector3 CellCenter(Vector2Int coord)
        {
            float half = tilesPerChunk * 0.5f;
            return transform.position + new Vector3(coord.x * tilesPerChunk + half, coord.y * tilesPerChunk + half, 0f);
        }

        //following the design doc for colors. Open to change: goblins = green, spiders = purple, neutral = gray
        private static Color ColorFor(CellData cell)
        {
            if (cell.Owner == Faction.Goblin)
            {
                return cell.Band == Intensity.Major ? new Color(0.1f, 0.5f, 0.1f) : new Color(0.5f, 0.85f, 0.4f);
            }
            if (cell.Owner == Faction.Spider)
            {
                return cell.Band == Intensity.Major ? new Color(0.4f, 0.1f, 0.5f) : new Color(0.75f, 0.55f, 0.9f);
            }
            return new Color(0.6f, 0.6f, 0.65f);
        }
    }
}
