using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for XP and leveling (Trello: "XP and leveling").</summary>
    public class LevelingTests
    {
        // Acceptance test (Trello): do kills between factions give no XP?
        [Test]
        public void KillsBetweenFactions_GiveNoXp()
        {
            Assert.IsFalse(PlayerProgression.IsPlayerKill(Kill(Faction.Spider, Faction.Goblin)));
            Assert.IsFalse(PlayerProgression.IsPlayerKill(Kill(Faction.Goblin, Faction.Neutral)));
            Assert.IsTrue(PlayerProgression.IsPlayerKill(Kill(Faction.Goblin, Faction.Player)));
        }

        // Acceptance test (Trello): does each level grant exactly 3 stat points and 1 skill point?
        [Test]
        public void EachLevel_GrantsThreeStatPointsAndOneSkillPoint()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());
            int startingSkillPoints = progress.UnspentSkillPoints;

            progress.AddXp(50);

            Assert.AreEqual(2, progress.Level);
            Assert.AreEqual(3, progress.UnspentStatPoints);
            Assert.AreEqual(startingSkillPoints + 1, progress.UnspentSkillPoints);
        }

        [Test]
        public void NewCharacter_StartsAtLevelOneWithOneSkillPoint()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());

            Assert.AreEqual(1, progress.Level);
            Assert.AreEqual(0, progress.CurrentXp);
            Assert.AreEqual(1, progress.UnspentSkillPoints);
            Assert.AreEqual(50, progress.XpToNextLevel);
        }

        [Test]
        public void LeftoverXp_CarriesOver_AndBigRewardsGiveSeveralLevels()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());

            int gained = progress.AddXp(160); // 50 for level 2, 100 for level 3, 10 left over

            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, progress.Level);
            Assert.AreEqual(10, progress.CurrentXp);
        }

        [Test]
        public void XpPastTheCap_IsDiscarded()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());

            progress.AddXp(100000);

            Assert.AreEqual(10, progress.Level);
            Assert.IsTrue(progress.IsMaxLevel);
            Assert.AreEqual(0, progress.CurrentXp);
            Assert.AreEqual(27, progress.UnspentStatPoints);
            Assert.AreEqual(10, progress.UnspentSkillPoints);
            Assert.AreEqual(0, progress.AddXp(50));
        }

        [Test]
        public void LeveledUp_FiresOncePerLevel()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());
            int timesRaised = 0;
            progress.LeveledUp += newLevel => timesRaised++;

            progress.AddXp(160);

            Assert.AreEqual(2, timesRaised);
        }

        [Test]
        public void SpendingPoints_UsesThemUp()
        {
            CharacterProgress progress = new CharacterProgress(DefaultCurve());

            Assert.IsTrue(progress.TryUseSkillPoint());
            Assert.IsFalse(progress.TryUseSkillPoint());
            Assert.IsFalse(progress.TryUseStatPoint());
        }

        private static LevelCurveDefinition DefaultCurve()
        {
            return ScriptableObject.CreateInstance<LevelCurveDefinition>();
        }

        private static KillInfo Kill(Faction victim, Faction killer)
        {
            return new KillInfo(null, victim, null, killer, "Test", Vector3.zero, 10, 5);
        }
    }
}
