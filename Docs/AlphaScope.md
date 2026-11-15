# Alpha scope — "First Playable" sprint

*As decided at the Nov 9–10 meetings. Trello's Product Backlog is the master list; this is the quick reference.*

**Target:** a playable Windows build under 512 MB at the end of the ~2-week sprint. Beta adds a WebGL build.

## Who owns what

| Module | Owner | Folder | Branch |
| --- | --- | --- | --- |
| Floor & Levels (1) | Gregory Rooney | `FloorGen/` | `feature/greg-floor-generation` |
| Faction AI (2) | Larry Miller | `AI/` | `feature/larry-faction-ai` |
| Player & Combat (3) | Matthew Phillips | `Player/` | `feature/matthew-player-combat` |
| Progression (4), Core, Game glue | Christopher | `Progression/`, `Core/`, `Game/` | `feature/chris-progression`, `feature/chris-core` |

**Shared Pool** cards on Trello are week-2 work for whoever is ahead once their own module runs in the main project.

## In Alpha

- **Archetypes:** Martial and Magic only.
- **Skill trees:** 2 per archetype, nodes 1–3: Bladed Weapons and Medium Armor (Martial); Radiation and Protection Arcana (Magic).
- **Equipment:** Martial: Sword (Light), Greatsword (Heavy), Leather Coat (Medium armor). Magic: Scroll (Channeler, Reach), Wand (Igniter, Grasp), Bastion Shroud, Agate + 2 placeholder Affixes.
- **Loadout** picked at character creation; the spawn-chunk station is the only place to change it.
- **No inventory, no consumables, no item loot.**
- **Art:** placeholder colored shapes (player teal, goblins green, spiders purple, neutral grey stone). Claude-made
  placeholders have `_placeholder` in the file name.

## Design rules

**Win and loss**
- Each faction has destructible spawner structures in its Major chunks. No spawners left = that faction stops spawning.
- The first spawner destroyed, by anyone, spawns the exit at its position. Walking into the exit wins.
- Death at any point → death screen.

**Gold and score**
- Every death drops gold that lasts 60 seconds. Chests also give gold. Gold is the score (HUD and results screen).

**Factions**
- Population cap per faction, counting all its units on the map.
- Residents patrol Major chunks; Guards patrol Minor chunks; both have a leash distance.
- Raid timer starts once population reaches a threshold. A raid takes about ½ of the Residents and ¼ of the Guards.
  The timer pauses during a raid; spawning continues.

**In combat**
- Being chased, attacking or being attacked, or within 5 s of taking damage.
- The station (spawn chunk) works only out of combat and pauses the game. It repairs armor and changes the loadout.
  The spawn chunk is **not** a safe zone.
- Leveling up and buying upgrades need you out of combat. Changing hotkeys is allowed anytime.

**Combat**
- Enemy attacks are telegraphed with 0.5–1 s wind-ups. Skitterlings are faster than the player.
- Accuracy over 100% becomes crit chance; a crit deals 2× damage.
- Burden: −10% movement per step, capped at 2.
- Basic attacks: right-click fires toward the mouse; movement isn't locked. Melee hitboxes stay attached to the character.
  Light vs Heavy (Light weapon / Igniter vs Heavy weapon / Channeler): heavy has a longer windup, longer cooldown, bigger hitbox or projectile.
- Shrouds: taking damage pauses recovery for 2.5 s, then they recover quickly (their Limit per second). Armor doesn't recover; the station repairs it.

**Progression**
- All 9 Flow State stats, 3 per archetype. A character uses only its own archetype's stats.
- Power = stat ÷ 10. Max HP comes from the defensive stat. Basic attacks come from equipment.
- 3 stat points and 1 skill point per level. Level cap 10 (temporary).

**Floor**
- 5×5 grid of 16×16-tile chunks. Start cell first (random, inner 3×3, Neutral); faction heat maps decide the rest.
- Chunk types: Neutral, Neutral Spawn, Goblin Minor, Goblin Major, Spider Minor, Spider Major.
- Chunks are chosen by their openings; extra openings get plugged; each type keeps a 4-way fallback.
- Tiles: water (slows), traps (heavy or lethal, for anyone), single-enemy spawn chances, chest spots.

## Beta / later

Mental archetype · inventory, consumables, loot · ranged weapon and three armor types · 4–5 trees plus 1–2
supplementary trees per archetype · faction elimination and territory takeover · units picking up gold · dodge ·
tutorial signs · the town and the debt loop · farthest-cell exit placement.

## Project settings (already set in the repo)

- **Unity 6000.3.27f2 exactly.** The Hub's "Recommended" badge points at 6.6. Don't use it.
- 1 tile = 1 world unit, 16 Pixels Per Unit, Point filter, no compression.
- **Sorting layers:** `Floor`, `Walls`, `Props`, `Actors`, `FX`, `UI`.
- **Physics layers:** `Player`, `Goblin`, `Spider`, `Walls`, `Projectile`.
