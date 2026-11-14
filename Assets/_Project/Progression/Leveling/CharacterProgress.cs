using System;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A character's level, XP and unspent points. Plain C#: it only counts. Spending stat points
    /// (and the out-of-combat rule) is handled by CharacterData; skill points by the skill tree system.
    /// </summary>
    public class CharacterProgress
    {
        private readonly LevelCurveDefinition _curve;

        /// <summary>Starts at level 1 with 0 XP and the curve's starting skill points.</summary>
        public CharacterProgress(LevelCurveDefinition curve)
        {
            _curve = curve;
            Level = 1;
            UnspentSkillPoints = curve.StartingSkillPoints;
        }

        /// <summary>Raised once per level gained, with the new level.</summary>
        public event Action<int> LeveledUp;

        /// <summary>Raised whenever XP, level or unspent points change (for the HUD and skill screen).</summary>
        public event Action ProgressChanged;

        /// <summary>Current level, starting at 1.</summary>
        public int Level { get; private set; }

        /// <summary>XP earned toward the next level.</summary>
        public int CurrentXp { get; private set; }

        /// <summary>XP needed for the next level, or 0 at the cap.</summary>
        public int XpToNextLevel => _curve.GetXpToNextLevel(Level);

        /// <summary>True once the level cap is reached.</summary>
        public bool IsMaxLevel => Level >= _curve.MaxLevel;

        /// <summary>Stat points waiting to be spent.</summary>
        public int UnspentStatPoints { get; private set; }

        /// <summary>Skill points waiting to be spent.</summary>
        public int UnspentSkillPoints { get; private set; }

        /// <summary>
        /// Adds XP and levels up as many times as it covers; leftover XP carries over. XP past the cap is discarded.
        /// Returns how many levels were gained.
        /// </summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
            {
                return 0;
            }

            int levelsGained = 0;
            CurrentXp += amount;

            while (!IsMaxLevel && CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
                UnspentStatPoints += _curve.StatPointsPerLevel;
                UnspentSkillPoints += _curve.SkillPointsPerLevel;
                levelsGained++;
                LeveledUp?.Invoke(Level);
            }

            if (IsMaxLevel)
            {
                CurrentXp = 0;
            }

            ProgressChanged?.Invoke();
            return levelsGained;
        }

        /// <summary>Uses one stat point if there is one. CharacterData calls this when the player spends a point.</summary>
        public bool TryUseStatPoint()
        {
            if (UnspentStatPoints <= 0)
            {
                return false;
            }

            UnspentStatPoints--;
            ProgressChanged?.Invoke();
            return true;
        }

        /// <summary>Uses one skill point if there is one. The skill tree system calls this when a node is unlocked.</summary>
        public bool TryUseSkillPoint()
        {
            if (UnspentSkillPoints <= 0)
            {
                return false;
            }

            UnspentSkillPoints--;
            ProgressChanged?.Invoke();
            return true;
        }
    }
}
