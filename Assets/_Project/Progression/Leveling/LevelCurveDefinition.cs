using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// How leveling works, as data: XP needed per level, the level cap and the points each level grants.
    /// Default curve (Module 4 prototype): 50 XP to reach level 2, then 50 more per level (100, 150 ... 450).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCurve_New", menuName = "Debt Dungeon/Level Curve")]
    public class LevelCurveDefinition : ScriptableObject
    {
        [Header("XP")]
        [Tooltip("XP needed to go from level 1 to level 2. Tune so level 2 comes in the first couple of minutes.")]
        [SerializeField, Min(1)] private int _xpToReachLevel2 = 50;

        [Tooltip("How much more XP each later level needs than the one before.")]
        [SerializeField, Min(0)] private int _xpIncreasePerLevel = 50;

        [Tooltip("Highest level. XP past it is discarded. (Temporary cap for Alpha.)")]
        [SerializeField, Min(1)] private int _maxLevel = 10;

        [Header("Points")]
        [Tooltip("Skill points a new character starts with.")]
        [SerializeField, Min(0)] private int _startingSkillPoints = 1;

        [Tooltip("Stat points granted per level, spent freely among the archetype's 3 stats.")]
        [SerializeField, Min(0)] private int _statPointsPerLevel = 3;

        [Tooltip("Skill points granted per level.")]
        [SerializeField, Min(0)] private int _skillPointsPerLevel = 1;

        /// <summary>Highest level.</summary>
        public int MaxLevel => _maxLevel;

        /// <summary>Skill points a new character starts with.</summary>
        public int StartingSkillPoints => _startingSkillPoints;

        /// <summary>Stat points per level.</summary>
        public int StatPointsPerLevel => _statPointsPerLevel;

        /// <summary>Skill points per level.</summary>
        public int SkillPointsPerLevel => _skillPointsPerLevel;

        /// <summary>
        /// XP needed to go from this level to the next (level 1 -> 50, level 2 -> 100, ...). 0 at the cap.
        /// </summary>
        public int GetXpToNextLevel(int currentLevel)
        {
            if (currentLevel >= _maxLevel)
            {
                return 0;
            }

            return _xpToReachLevel2 + (currentLevel - 1) * _xpIncreasePerLevel;
        }
    }
}
