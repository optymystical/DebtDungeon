using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Awards XP to the player's character for kills the player made, and announces level-ups to the rest of the
    /// game through GameEvents.PlayerLeveledUp (the HUD's level-up popup listens for it).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerProgression : MonoBehaviour
    {
        private CharacterData _character;

        /// <summary>The player's character, or null until Initialize is called.</summary>
        public CharacterData Character => _character;

        private void OnEnable()
        {
            GameEvents.ActorKilled += OnActorKilled;
            SubscribeToProgress();
        }

        private void OnDisable()
        {
            GameEvents.ActorKilled -= OnActorKilled;
            UnsubscribeFromProgress();
        }

        /// <summary>Gives this component the character to award XP to. Game calls this when the player spawns.</summary>
        public void Initialize(CharacterData character)
        {
            if (character == null)
            {
                Debug.LogError("PlayerProgression.Initialize was given no character.", this);
                return;
            }

            UnsubscribeFromProgress();
            _character = character;
            SubscribeToProgress();
        }

        /// <summary>
        /// True if this death should give the player XP: the player made the kill, and the victim wasn't the player.
        /// Kills between factions (a goblin killing a spider) give nothing.
        /// </summary>
        public static bool IsPlayerKill(KillInfo kill)
        {
            return kill.KillerFaction == Faction.Player && kill.VictimFaction != Faction.Player;
        }

        private void OnActorKilled(KillInfo kill)
        {
            if (_character == null || !IsPlayerKill(kill))
            {
                return;
            }

            _character.Progress.AddXp(kill.XpReward);
        }

        private void OnLeveledUp(int newLevel)
        {
            GameEvents.RaisePlayerLeveledUp(newLevel);
        }

        private void SubscribeToProgress()
        {
            if (_character != null && isActiveAndEnabled)
            {
                // Remove first so a second call can never subscribe twice.
                _character.Progress.LeveledUp -= OnLeveledUp;
                _character.Progress.LeveledUp += OnLeveledUp;
            }
        }

        private void UnsubscribeFromProgress()
        {
            if (_character != null)
            {
                _character.Progress.LeveledUp -= OnLeveledUp;
            }
        }
    }
}
