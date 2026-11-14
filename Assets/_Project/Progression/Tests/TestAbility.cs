using DebtDungeon.Core;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>
    /// The smallest possible ability, for tests only. AbilityDefinition is abstract (Player makes the real kinds),
    /// so tests need a concrete class to create instances of.
    /// </summary>
    public class TestAbility : AbilityDefinition
    {
    }
}
