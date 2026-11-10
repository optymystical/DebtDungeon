# FloorGen (Module 1) — `DebtDungeon.FloorGen`

**Owner:** Gregory Rooney · **Branch:** `feature/greg-floor-generation`
**Sandbox scene:** `Scenes/Sandbox/Greg_FloorGen.unity`
**Data:** `Data/Chunks/` (`Chunk_<Type>_<NN>`, e.g. `Chunk_GoblinMajor_01`) · **Prefabs:** `Prefabs/FloorGen/Chunks/`
**References:** Core only.

The Module Week prototype is on `prototype/greg-floor-generation`. Use it as a reference and rewrite it here. Never merge it.

## Alpha scope
- 5×5 grid of handmade 16×16-tile chunks. The start cell is picked first: random, inner 3×3, forced Neutral.
  Faction heat maps decide the rest.
- Chunk types: Neutral, Neutral Spawn, Goblin Minor, Goblin Major, Spider Minor, Spider Major.
  Aim for 5–6 of each, fewer for Major and Spawn.
- Chunks are chosen by their openings: a chunk fits when its openings cover every opening the cell needs.
  Extra openings get plugged. Each chunk type keeps a 4-way fallback chunk.
- Tile types: water (slows anything moving through it), traps (rolled at generation; heavy or lethal damage to anyone),
  single-enemy spawn chances (rolled at generation), chest spots.
- Destructible faction spawners sit in Major chunks (the spawner itself is AI's). The exit spawns where the first spawner
  is destroyed. Farthest-cell exit placement moved to Beta.
- Same seed → same floor.

## Talks to other modules through Core
- Implements `IFloorQuery` (v1.2 adds `GetMoveSpeedMultiplier(cell)` for water) and raises `GameEvents.FloorGenerated`.
- Uses Core's `Faction` and `ChunkIntensity`. Don't define your own.

## Fix from the prototype while rewriting
- The `Faction` enum lives in FloorGen. It must come from Core.
- Private fields need the `_` prefix.
- `FloorSettings` has lowercase public fields. Switch them to `[SerializeField] private` with read-only properties.

## Project settings to use
Sorting layers `Floor` and `Walls`; physics layer `Walls` on wall colliders. 1 tile = 1 unit, 16 PPU.
