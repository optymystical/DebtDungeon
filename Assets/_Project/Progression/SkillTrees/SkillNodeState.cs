namespace DebtDungeon.Progression
{
    /// <summary>How a skill tree node looks on the skill screen.</summary>
    public enum SkillNodeState
    {
        /// <summary>Bought.</summary>
        Owned = 0,

        /// <summary>The next one to buy (it may still be refused, e.g. no points or in combat).</summary>
        Next = 1,

        /// <summary>Further down the tree; the nodes before it come first.</summary>
        Locked = 2
    }
}
