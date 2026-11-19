using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    [RequireComponent(typeof(Animator))]
    public class SpawnerAnimator2D : MonoBehaviour
    {
        private static readonly int SpawnHash = Animator.StringToHash("Spawn");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private Animator _animator;
        private Health _health;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
            }
        }

        public void TriggerSpawnAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(SpawnHash);
            }
        }

        public void TriggerDeathAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(DieHash);
            }
        }

        private void HandleDamaged(DamageInfo info, int damageDealt)
        {
            if (_animator != null && _health != null && _health.IsAlive)
            {
                _animator.SetTrigger(HurtHash);
            }
        }
    }
}
