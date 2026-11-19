# Player & Combat (Module 3) — `DebtDungeon.Player`

**Owner:** Matthew Phillips (Chris pairs on the ability framework) · **Branch:** `feature/matthew-player-combat`
**Sandbox scene:** `Scenes/Sandbox/Matthew_PlayerCombat.unity`
**Data:** `Data/Abilities/` (`Ability_<Name>`) · **Prefabs:** `Prefabs/Player/`
**References:** Core, Input System, uGUI, TextMeshPro.

## Alpha scope
- Rigidbody2D movement (in `FixedUpdate`) with 360° mouse aim, through the Input Actions asset (new Input System only).
- Controls: right-click = basic attack, Q = defensive ability slot, 1–5 = abilities.
- The player uses **Core's `Health`** (Faction = Player); damage resolves through `DamageInfo`. Roll hits with `CombatRules.RollHit`.
- Accuracy: hit chance is min(Accuracy, 100%); anything over 100% is crit chance; a crit does 2× damage.
- Armor/Shroud absorption is built into `Health` through `IDamageAbsorber` (Progression supplies the absorbers).
- Ability execution: an ability runner implementing `IAbilityUser` (`GrantAbility`, `RevokeAbility`, `HasAbility`).
  Slot by `AbilityDefinition.Kind`. Basic attacks come from equipment. Progression only grants usable abilities.
- **In combat:** being chased, attacking or being attacked, or within 5 s of taking damage. Exposed through
  `ICombatState` in Core (v1.2) so the station and UI can read it.
- Burden: −8% movement per step, capped at 2.
- Camera follow (Cinemachine) and the HUD: HP, energy, cooldowns, gold.
- Beta, not now: dodge, consumables.

## Talks to other modules through Core
Reads stats through `IStatProvider` (Progression implements it). Use a stub provider in your sandbox until the real one lands.
Owns the combat types in Core: changes go through Chris.

## Project settings to use
Physics layers `Player` and `Projectile`; sorting layers `Actors` and `FX`; the Input Actions asset in `Settings/`.
