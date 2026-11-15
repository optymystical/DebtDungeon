namespace DebtDungeon.Core
{
    /// <summary>
    /// Where the game flow is. Game's state machine owns it and announces changes through
    /// GameEvents.GameStateChanged; other modules only listen.
    /// </summary>
    /// <remarks>
    /// Unity saves enums as numbers. Only add new values at the end; never reorder or remove one.
    /// </remarks>
    public enum GameState
    {
        /// <summary>Title screen.</summary>
        MainMenu = 0,

        /// <summary>Picking archetype, preset, loadout and name.</summary>
        CharacterCreation = 1,

        /// <summary>In the dungeon, time running.</summary>
        Playing = 2,

        /// <summary>Pause menu open. Time frozen.</summary>
        Paused = 3,

        /// <summary>Death screen. Time frozen.</summary>
        Dead = 4,

        /// <summary>Results screen after reaching the exit. Time frozen.</summary>
        Won = 5
    }
}
