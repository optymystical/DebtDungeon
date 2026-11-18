using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class ProjectileAttackDefinition : BasicAttackDefinition
    {
        private readonly Projectile _projectilePrefab;
        private readonly float _speed;
        private readonly float _lifetime;

        // Creates and launches projectile from user
        public override void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy)
        {
            Instantiate(_projectilePrefab, (Vector2)(user.transform.position) + direction * 0.6f, user.transform.rotation);
            _projectilePrefab.Launch(direction * _speed, _lifetime, user, userFaction, damage, accuracy, DamageType.Magic);
        }
    }
}
