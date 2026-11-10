# Faction AI (Module 2) — `DebtDungeon.AI`

**Owner:** Larry Miller · **Branch:** `feature/larry-faction-ai`
**Sandbox scene:** `Scenes/Sandbox/Larry_FactionAI.unity`
**Data:** `Data/Units/` (`Unit_<Name>`, e.g. `Unit_Skitterling`), `Data/Factions/` · **Prefabs:** `Prefabs/AI/`
**References:** Core only.

## Alpha scope
- Goblins and Spiders are hostile to each other and to the player (relations table as a ScriptableObject).
- **Population cap** per faction, counting all that faction's units on the map.
- **Roles:** Residents patrol Major chunks, Guards patrol Minor chunks; both have a leash distance.
- **Raids:** the raid timer starts once population reaches a threshold. A raid takes about ½ of the Residents and
  ¼ of the Guards. The timer pauses during a raid; spawning continues in the background.
- **Destructible spawners** in Major chunks. A faction with no spawners stops spawning.
  Destroying one raises `SpawnerDestroyed` (Core v1.2); the first one spawns the exit.
- Telegraphed enemy attacks with 0.5–1 s wind-ups. Skitterlings are fast ambush chasers, faster than the player.
- Every death drops gold that lasts 60 seconds (gold-drop hook on `ActorKilled`, Core v1.2).
- Beta, not now: faction elimination, territory takeover, units picking up gold.

## Talks to other modules through Core
- Reads the floor only through `IFloorQuery` (from `GameEvents.FloorGenerated`). Keep pathfinding stateless: pass the
  floor data in as a parameter.
- Units implement `IDamageable` and raise `GameEvents.ActorKilled`. v1.2 adds the XP-reward interface and Accuracy.
- **Open decision:** the prototype's CombatAgent health should merge into the shared `Health`/`IDamageable`, but AI can't
  reference the Player assembly. Chris will settle where `Health` lives as part of Core v1.2. Ask before you build it.

## Project settings to use
Physics layers `Goblin`, `Spider` (and `Walls` for line of sight); sorting layer `Actors`.
