using DebtDungeon.Core;
using UnityEditor.VersionControl;
using UnityEngine;

namespace DebtDungeon.Player
{
    public abstract class BasicAttackDefinition : AbilityDefinition
    { // set Kind = BasicAttack on every asset.
        public readonly int _baseDamage;
        public readonly float _windUp;
        public readonly StatType _scalingStat;
        public readonly int _accuracy = 100;
        public readonly DamageType _damageType; 

        public abstract void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy);
    }
}
