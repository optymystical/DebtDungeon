using DebtDungeon.Core;
using UnityEngine;
using System.Collections;

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

        /// <summary>
        /// Wrap-up addition (Chris/Claude): raised when the projectile reaches a living enemy, with the roll result
        /// (Miss included). Crackle listens to it to start its chain. Basic attacks don't use it.
        /// </summary>
        public event System.Action<Health, HitResult> Hit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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

            // Timer to destroy projectile
            StartCoroutine(ProjectileTimer(lifetime));
        }

        private IEnumerator ProjectileTimer(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }

        // When the projectile collides
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Walls remove them
            if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
            {
                Destroy(gameObject);
                return;
            }

            // They try to hit and damage living enemies
            Health targetsHealth = collision.GetComponentInParent<Health>();
            if (targetsHealth != null && targetsHealth.gameObject != _user && _faction != targetsHealth.Faction
                && targetsHealth.IsAlive)
            {
                HitResult result = CombatRules.RollHit(_accuracy);
                if (result != HitResult.Miss)
                {
                    targetsHealth.TakeDamage(new DamageInfo(CombatRules.GetDamage(_damage, result), _damageType, _user,
                        _faction, result == HitResult.Critical));
                }
                Hit?.Invoke(targetsHealth, result);
                Destroy(gameObject);
            }
        }
    }
}
