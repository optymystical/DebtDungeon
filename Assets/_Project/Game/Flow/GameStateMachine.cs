using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The game flow as a small state machine: Main Menu -> Character Creation -> Playing <-> Paused -> Dead / Won
    /// -> Main Menu or Retry. Only the transitions in the table below are allowed; anything else is refused.
    /// Plain C# so it can be tested without a scene. The GameManager owns one and applies its effects
    /// (time scale, scene loads, GameEvents).
    /// </summary>
    public class GameStateMachine
    {
        // For each state, the states it may move to. Reads like the flow diagram.
        private static readonly Dictionary<GameState, GameState[]> AllowedTransitions = new Dictionary<GameState, GameState[]>
        {
            { GameState.MainMenu, new[] { GameState.CharacterCreation } },
            { GameState.CharacterCreation, new[] { GameState.MainMenu, GameState.Playing } },
            { GameState.Playing, new[] { GameState.Paused, GameState.Dead, GameState.Won } },
            { GameState.Paused, new[] { GameState.Playing, GameState.MainMenu } },
            { GameState.Dead, new[] { GameState.Playing, GameState.MainMenu } }, // Playing = Retry
            { GameState.Won, new[] { GameState.Playing, GameState.MainMenu } }
        };

        /// <summary>Starts in the given state: MainMenu normally, Playing when the Main scene is opened directly to test.</summary>
        public GameStateMachine(GameState startState = GameState.MainMenu)
        {
            Current = startState;
        }

        /// <summary>Raised after every successful change, with (previous, current).</summary>
        public event Action<GameState, GameState> StateChanged;

        /// <summary>The state the game is in now.</summary>
        public GameState Current { get; private set; }

        /// <summary>True while time should be stopped: the pause menu and both end screens.</summary>
        public static bool IsTimeFrozen(GameState state)
        {
            return state == GameState.Paused || state == GameState.Dead || state == GameState.Won;
        }

        /// <summary>True if the table allows moving from the current state to next. Staying in the same state is not a change.</summary>
        public bool CanChangeTo(GameState next)
        {
            if (next == Current)
            {
                return false;
            }

            return Array.IndexOf(AllowedTransitions[Current], next) >= 0;
        }

        /// <summary>Moves to next if allowed and raises StateChanged. Returns false (and changes nothing) otherwise.</summary>
        public bool TryChangeState(GameState next)
        {
            if (!CanChangeTo(next))
            {
                return false;
            }

            GameState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
