using UnityEngine;

namespace DebtDungeon.FloorGen
{
    public class FloorGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private FloorSettings settings = new FloorSettings();
        [SerializeField] private int tilesPerChunk = 16;

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
        }

        [ContextMenu("Generate With New Seed")]
        public void GenerateWithNewSeed()
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
            Generate();
        }

        void OnDrawGizmos()
        {
            if (cells == null) return;

            foreach (CellData cell in cells)
            {
                Gizmos.color = ColorFor(cell);
                Gizmos.DrawCube(CellCenter(cell.Coord), new Vector3(tilesPerChunk - 1, tilesPerChunk - 1, 0.1f));
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
