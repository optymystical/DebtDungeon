using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace DebtDungeon.FloorGen
{
    // a designer-editable list of chunk prefabs, plus the rules for choosing one per cell
    [CreateAssetMenu(fileName = "ChunkLibrary", menuName = "Debt Dungeon/Floor Gen/ Chunk Library")]
    public class ChunkLibrary : ScriptableObject
    {
        [SerializeField] private List<Chunk> chunks = new List<Chunk>();
        [SerializeField] private TileBase wallTile; //using to plug closed door slots

        public TileBase WallTile { get { return wallTile; } }

        // tries the closest match first, then falls back step by step
        public Chunk PickChunk(CellData cell, System.Random rng)
        {
            List<Chunk> options = new List<Chunk>();

            // 1 - Start/Exit cells use a chunk built for that role, if one exists
            ChunkRole role = RoleFor(cell);
            if (role != ChunkRole.Normal)
            {
                foreach (Chunk chunk in chunks)
                {
                    if (chunk != null && chunk.Role == role) options.Add(chunk);
                }
            }

            // 2 - Exact match: same faction and same identity
            if (options.Count == 0) CollectNormal(options, cell.Owner, cell.Band, true);

            // 3 - Same faction, any intensity
            if (options.Count == 0) CollectNormal(options, cell.Owner, cell.Band, false);

            // 4 - Last resort, any neutral chunk
            if (options.Count == 0) CollectNormal(options, Faction.Neutral, Intensity.Neutral, false);

            if (options.Count == 0) return null;
            return options[rng.Next(options.Count)];
        }

        private void CollectNormal(List<Chunk> options, Faction owner, Intensity band, bool matchBand)
        {
            foreach (Chunk chunk in chunks)
            {
                if (chunk == null || chunk.Role != ChunkRole.Normal) continue;
                if (chunk.Owner != owner) continue;
                if (matchBand && chunk.Band != band) continue;
                options.Add(chunk);
            }
        }

        private static ChunkRole RoleFor(CellData cell)
        {
            if (cell.IsStart) return ChunkRole.Start;
            if (cell.IsExit) return ChunkRole.Exit;
            return ChunkRole.Normal;
        }
    }
}
