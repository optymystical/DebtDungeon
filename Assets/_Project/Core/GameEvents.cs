using System;
using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// The shared event hub. Systems announce what happened here instead of calling each other.
    /// Subscribe in OnEnable and unsubscribe in OnDisable, every time.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>The floor finished generating. Raised by FloorGen. AI and Game start from this.</summary>
        public static event Action<IFloorQuery> FloorGenerated;

        /// <summary>Anything with Health died. Raised automatically by Health. Progression awards XP; Game drops gold.</summary>
        public static event Action<KillInfo> ActorKilled;

        /// <summary>The player died. Raised automatically by Health, right after ActorKilled. Game shows the death screen.</summary>
        public static event Action<KillInfo> PlayerDied;

        /// <summary>The player reached a new level (the parameter). Raised by Progression. UI shows the level-up popup.</summary>
        public static event Action<int> PlayerLeveledUp;

        /// <summary>A faction spawner was destroyed at a position. Raised by AI. Game spawns the exit on the first one.</summary>
        public static event Action<Faction, Vector3> SpawnerDestroyed;

        /// <summary>The player picked up gold (the amount). Raised by Game's gold pickups and chests. The HUD adds it to the score.</summary>
        public static event Action<int> GoldCollected;

        /// <summary>
        /// The game flow changed state (previous, current). Raised by Game. Player ignores attack and ability input
        /// unless the state is Playing; menus show and hide their screens.
        /// </summary>
        public static event Action<GameState, GameState> GameStateChanged;

        /// <summary>
        /// The last state announced through GameStateChanged. Lets something created later (e.g. the player,
        /// spawned after Playing began) check the state without having heard the event.
        /// </summary>
        public static GameState CurrentGameState { get; private set; } = GameState.MainMenu;

        /// <summary>Announces that the floor is ready.</summary>
        public static void RaiseFloorGenerated(IFloorQuery floor)
        {
            FloorGenerated?.Invoke(floor);
        }

        /// <summary>Announces a death. Health calls this; other code shouldn't need to.</summary>
        public static void RaiseActorKilled(KillInfo kill)
        {
            ActorKilled?.Invoke(kill);
        }

        /// <summary>Announces the player's death. Health calls this; other code shouldn't need to.</summary>
        public static void RaisePlayerDied(KillInfo kill)
        {
            PlayerDied?.Invoke(kill);
        }

        /// <summary>Announces a level-up.</summary>
        public static void RaisePlayerLeveledUp(int newLevel)
        {
            PlayerLeveledUp?.Invoke(newLevel);
        }

        /// <summary>Announces a destroyed spawner.</summary>
        public static void RaiseSpawnerDestroyed(Faction faction, Vector3 position)
        {
            SpawnerDestroyed?.Invoke(faction, position);
        }

        /// <summary>Announces collected gold.</summary>
        public static void RaiseGoldCollected(int amount)
        {
            GoldCollected?.Invoke(amount);
        }

        /// <summary>Announces a game state change. Only Game's state machine should call this.</summary>
        public static void RaiseGameStateChanged(GameState previous, GameState current)
        {
            CurrentGameState = current;
            GameStateChanged?.Invoke(previous, current);
        }

        /// <summary>
        /// Clears every listener when Play mode starts. Static events outlive scenes, so without this a listener
        /// left over from the last play session (if domain reload is off) could fire on a destroyed object.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAllListeners()
        {
            FloorGenerated = null;
            ActorKilled = null;
            PlayerDied = null;
            PlayerLeveledUp = null;
            SpawnerDestroyed = null;
            GoldCollected = null;
            GameStateChanged = null;
            CurrentGameState = GameState.MainMenu;
        }
    }
}
