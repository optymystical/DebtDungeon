# Game (glue) — `DebtDungeon.Game`

**Owner:** Christopher · **References:** Core and every module. **The only assembly allowed to.**

Game wires real systems together: it creates the floor, spawns the player with the chosen character, hands
`IFloorQuery` to the AI, and runs the game flow.

## Scenes (in `Scenes/`)

| Scene | In build | Purpose |
| --- | --- | --- |
| `MainMenu` | index 0 | Title, character creation (archetype → preset → loadout → name) |
| `Main` | index 1 | The dungeon run: floor, factions, player, HUD, pause, station pop-up, death and results screens |
| `Sandbox/<Name>_<Topic>` | no | Personal test scenes. One owner each; nobody else edits them |

`Main` and `MainMenu` are shared. Post in Discord before editing one.

## Alpha responsibilities
- Game flow: Main Menu → Character Creation → Playing ↔ Paused → Dead / Won → Main Menu or Retry.
  Time is frozen while paused, on the end screens and while the station is open.
- One seed per run, drives floor generation and spawns.
- Exit: listens for the first `SpawnerDestroyed` and spawns the exit there. Walking into it wins.
- Station in the spawn chunk: out of combat only; repairs armor, changes loadout. The spawn chunk is **not** a safe zone.
- Gold is the score: HUD and results screen.
