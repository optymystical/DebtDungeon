using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Counts what the end screens show: time played, the player's kills per faction, gold, level and what killed
    /// the player. Everything comes from Core's GameEvents, so it doesn't need to know any other module.
    /// Put one in the Main scene (on the same object as the GameManager).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunStatsTracker : MonoBehaviour
    {
        private readonly Dictionary<Faction, int> _killsByFaction = new Dictionary<Faction, int>();

        /// <summary>Seconds spent in the Playing state (pause and end screens don't count).</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>Gold collected this run.</summary>
        public int Gold { get; private set; }

        /// <summary>The player's level (starts at 1, updated on every level-up).</summary>
        public int Level { get; private set; } = 1;

        /// <summary>True once the player has died this run.</summary>
        public bool HasPlayerDied { get; private set; }

        /// <summary>How the player died. Only meaningful when HasPlayerDied is true.</summary>
        public KillInfo PlayerDeath { get; private set; }

        private void OnEnable()
        {
            GameEvents.ActorKilled += OnActorKilled;
            GameEvents.GoldCollected += OnGoldCollected;
            GameEvents.PlayerLeveledUp += OnPlayerLeveledUp;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.ActorKilled -= OnActorKilled;
            GameEvents.GoldCollected -= OnGoldCollected;
            GameEvents.PlayerLeveledUp -= OnPlayerLeveledUp;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        private void Update()
        {
            if (GameEvents.CurrentGameState == GameState.Playing)
            {
                ElapsedSeconds += Time.deltaTime;
            }
        }

        /// <summary>Kills the player made against one faction (spawner camps count too).</summary>
        public int GetKills(Faction faction)
        {
            return _killsByFaction.TryGetValue(faction, out int kills) ? kills : 0;
        }

        /// <summary>Formats seconds as m:ss, e.g. 754 -> "12:34".</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        private void OnActorKilled(KillInfo kill)
        {
            if (kill.KillerFaction != Faction.Player || kill.VictimFaction == Faction.Player)
            {
                return;
            }

            _killsByFaction[kill.VictimFaction] = GetKills(kill.VictimFaction) + 1;
        }

        private void OnGoldCollected(int amount)
        {
            Gold += amount;
        }

        private void OnPlayerLeveledUp(int newLevel)
        {
            Level = newLevel;
        }

        private void OnPlayerDied(KillInfo kill)
        {
            HasPlayerDied = true;
            PlayerDeath = kill;
        }
    }
}
