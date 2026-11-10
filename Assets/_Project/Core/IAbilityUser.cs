namespace DebtDungeon.Core
{
    /// <summary>
    /// Something that can hold and use abilities. Implemented by Player's ability runner; Progression grants and revokes.
    /// Progression only grants an ability while its required equipment is worn, so the runner never checks gear.
    /// </summary>
    public interface IAbilityUser
    {
        /// <summary>Adds an ability and slots it by its Kind. Granting one it already has does nothing.</summary>
        void GrantAbility(AbilityDefinition ability);

        /// <summary>
        /// Removes an ability and frees its slot. Used when an upgrade node replaces it (Crackle becomes Crackle II)
        /// or when the loadout changes. Revoking one it doesn't have does nothing.
        /// </summary>
        void RevokeAbility(AbilityDefinition ability);

        /// <summary>True if this ability is currently granted.</summary>
        bool HasAbility(AbilityDefinition ability);
    }
}
