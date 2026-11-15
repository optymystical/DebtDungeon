namespace DebtDungeon.Progression
{
    /// <summary>
    /// What a skill tree can require. Bladed Weapons needs a Bladed weapon; Medium Armor needs Medium armor.
    /// A tree can be bought without its gear, but its abilities are only granted while the gear is worn.
    /// </summary>
    public enum EquipmentTag
    {
        Bladed = 0,
        LightArmor = 1,
        MediumArmor = 2
    }
}
