using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public abstract class BasicAttackDefinition : AbilityDefinition
    { // set Kind = BasicAttack on every asset.
        [SerializeField] private int _baseDamage = 10;
        [SerializeField] private float _windup = 0.15f;
        [SerializeField] private StatType _scalingStat;
        [SerializeField] private int _accuracy = 100;
        [SerializeField] private DamageType _damageType;

        public int BaseDamage => _baseDamage;
        public float Windup => _windup;
        public StatType ScalingStat => _scalingStat;
        public int Accuracy => _accuracy;
        public DamageType DamageType => _damageType;

        public abstract void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy);
    }
}
