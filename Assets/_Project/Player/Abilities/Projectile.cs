using Codice.Client.Common;
using DebtDungeon.Core;
using UnityEngine;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

namespace DebtDungeon.Player
{
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private Vector2 _direction;
        private float _lifetime;
        private GameObject _user;
        private Faction _faction;
        private int _damage;
        private int _accuracy;
        private DamageType _damageType;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            Destroy(gameObject, _lifetime);
        }

        public void Launch(Vector2 direction, float lifetime, GameObject user, Faction faction, int damage,
            int accuracy, DamageType damageType)
        {
            // Store data
            _direction = direction;
            _lifetime = lifetime;
            _user = user;
            _faction = faction;
            _damage = damage;
            _accuracy = accuracy;
            _damageType = damageType;

            // Move projectile
            _rb.linearVelocity = direction;
        }

        // When the projectile collides
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Walls remove them
            if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
            {
                Destroy(gameObject);
            }

            // They try to hit and damage living enemies
            Health targetsHealth = collision.GetComponentInParent<Health>();
            if (GetComponentInParent<Health>().Faction != targetsHealth.Faction && targetsHealth.IsAlive)
            {
                HitResult result = CombatRules.RollHit(_accuracy);
                if (result != HitResult.Miss)
                {
                    targetsHealth.TakeDamage(new DamageInfo(CombatRules.GetDamage(_damage, result), _damageType, _user,
                        _faction, result == HitResult.Critical));
                }
                Destroy(gameObject);
            }
        }
    }
}
