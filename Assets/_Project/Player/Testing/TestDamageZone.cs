using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class TestDamageZone : MonoBehaviour
    {
        [SerializeField] private int _damage = 10;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(new DamageInfo(_damage, DamageType.Physical, gameObject, Faction.Goblin));
            }
        }
    }
}
