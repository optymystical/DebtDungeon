using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Everything about one death, sent with GameEvents.ActorKilled and PlayerDied.
    /// Health fills it in at the moment of death, so listeners don't have to look anything up
    /// on an object that may already be destroyed.
    /// </summary>
    public readonly struct KillInfo
    {
        /// <summary>The GameObject that died.</summary>
        public GameObject Victim { get; }

        /// <summary>The victim's faction.</summary>
        public Faction VictimFaction { get; }

        /// <summary>The GameObject that dealt the killing blow. Can be null.</summary>
        public GameObject Killer { get; }

        /// <summary>The killer's faction. Player kills give XP; Neutral means a trap or the environment.</summary>
        public Faction KillerFaction { get; }

        /// <summary>The killer's display name, e.g. "Goblin Brute", for the death screen.</summary>
        public string KillerName { get; }

        /// <summary>Where the victim died. Gold drops and the exit spawn here.</summary>
        public Vector3 Position { get; }

        /// <summary>XP from the victim's IKillReward (0 if it has none).</summary>
        public int XpReward { get; }

        /// <summary>Gold from the victim's IKillReward (0 if it has none).</summary>
        public int GoldReward { get; }

        /// <summary>Creates the record of a death. Normally only Health calls this.</summary>
        public KillInfo(
            GameObject victim,
            Faction victimFaction,
            GameObject killer,
            Faction killerFaction,
            string killerName,
            Vector3 position,
            int xpReward,
            int goldReward)
        {
            Victim = victim;
            VictimFaction = victimFaction;
            Killer = killer;
            KillerFaction = killerFaction;
            KillerName = killerName;
            Position = position;
            XpReward = xpReward;
            GoldReward = goldReward;
        }
    }
}
