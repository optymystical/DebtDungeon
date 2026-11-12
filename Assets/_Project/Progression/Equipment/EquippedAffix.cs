namespace DebtDungeon.Progression
{
    /// <summary>An Affix in the loadout, together with the form it's attuned in.</summary>
    public readonly struct EquippedAffix
    {
        /// <summary>The gem.</summary>
        public AffixDefinition Affix { get; }

        /// <summary>Offensive (spells) or Defensive (Shroud).</summary>
        public AffixForm Form { get; }

        /// <summary>Pairs a gem with its form.</summary>
        public EquippedAffix(AffixDefinition affix, AffixForm form)
        {
            Affix = affix;
            Form = form;
        }
    }
}
