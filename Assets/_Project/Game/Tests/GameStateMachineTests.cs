using DebtDungeon.Core;
using NUnit.Framework;

namespace DebtDungeon.Game.Tests
{
    /// <summary>Edit Mode tests for the game flow (Trello: "Game Manager and game states").</summary>
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInMainMenu()
        {
            Assert.AreEqual(GameState.MainMenu, new GameStateMachine().Current);
        }

        // Acceptance test (Trello): does every state transition described in the GDD work?
        [Test]
        public void FullRun_FollowsTheGddFlow()
        {
            GameStateMachine machine = new GameStateMachine();

            Assert.IsTrue(machine.TryChangeState(GameState.CharacterCreation));
            Assert.IsTrue(machine.TryChangeState(GameState.Playing));
            Assert.IsTrue(machine.TryChangeState(GameState.Paused));
            Assert.IsTrue(machine.TryChangeState(GameState.Playing));
            Assert.IsTrue(machine.TryChangeState(GameState.Dead));
            Assert.IsTrue(machine.TryChangeState(GameState.Playing)); // Retry
            Assert.IsTrue(machine.TryChangeState(GameState.Won));
            Assert.IsTrue(machine.TryChangeState(GameState.MainMenu));
        }

        [Test]
        public void CreationCanGoBackToTheMenu_AndPauseCanQuitToIt()
        {
            GameStateMachine machine = new GameStateMachine();
            machine.TryChangeState(GameState.CharacterCreation);
            Assert.IsTrue(machine.TryChangeState(GameState.MainMenu));

            GameStateMachine paused = new GameStateMachine(GameState.Paused);
            Assert.IsTrue(paused.TryChangeState(GameState.MainMenu));
        }

        [TestCase(GameState.MainMenu, GameState.Playing)]
        [TestCase(GameState.MainMenu, GameState.Paused)]
        [TestCase(GameState.CharacterCreation, GameState.Won)]
        [TestCase(GameState.Paused, GameState.Dead)]
        [TestCase(GameState.Dead, GameState.Won)]
        [TestCase(GameState.Playing, GameState.CharacterCreation)]
        [TestCase(GameState.Playing, GameState.Playing)]
        public void IllegalChange_IsRefused_AndStateIsKept(GameState from, GameState to)
        {
            GameStateMachine machine = new GameStateMachine(from);

            Assert.IsFalse(machine.TryChangeState(to));
            Assert.AreEqual(from, machine.Current);
        }

        [Test]
        public void StateChanged_SendsPreviousAndCurrent_OnlyOnSuccess()
        {
            GameStateMachine machine = new GameStateMachine(GameState.Playing);
            int raised = 0;
            GameState seenPrevious = GameState.MainMenu;
            GameState seenCurrent = GameState.MainMenu;
            machine.StateChanged += (previous, current) =>
            {
                raised++;
                seenPrevious = previous;
                seenCurrent = current;
            };

            machine.TryChangeState(GameState.Paused);
            machine.TryChangeState(GameState.Dead); // refused: can't die while paused

            Assert.AreEqual(1, raised);
            Assert.AreEqual(GameState.Playing, seenPrevious);
            Assert.AreEqual(GameState.Paused, seenCurrent);
        }

        [TestCase(GameState.MainMenu, false)]
        [TestCase(GameState.CharacterCreation, false)]
        [TestCase(GameState.Playing, false)]
        [TestCase(GameState.Paused, true)]
        [TestCase(GameState.Dead, true)]
        [TestCase(GameState.Won, true)]
        public void TimeIsFrozen_OnlyWhilePausedOrOnEndScreens(GameState state, bool frozen)
        {
            Assert.AreEqual(frozen, GameStateMachine.IsTimeFrozen(state));
        }
    }
}
