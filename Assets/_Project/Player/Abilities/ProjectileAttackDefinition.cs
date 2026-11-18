using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    [CreateAssetMenu(fileName = "Attack_", menuName = "Debt Dungeon/Abilities/Projectile Attack")]
    public class ProjectileAttackDefinition : BasicAttackDefinition
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _speed;
        [SerializeField] private float _lifetime;

        public Projectile ProjectilePrefab => _projectilePrefab;
        public float Speed => _speed;
        public float Lifetime => _lifetime;

        // Creates and launches projectile from user
        public override void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy)
        {
            Projectile projectile = Instantiate(_projectilePrefab, (Vector2)(user.transform.position) + direction * 0.6f,
                user.transform.rotation);
            projectile.Launch(direction * _speed, _lifetime, user, userFaction, damage, accuracy, DamageType);
        }
    }
}
