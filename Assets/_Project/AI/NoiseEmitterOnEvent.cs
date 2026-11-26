using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Emits a NoiseEventBus alert when a destructible prop takes damage/breaks or a trap triggers.
    /// </summary>
    public class NoiseEmitterOnEvent : MonoBehaviour
    {
        [SerializeField] private float _noiseRadius = 8.5f;
        [SerializeField] private bool _emitOnHealthDeath = true;
        [SerializeField] private bool _emitOnTriggerEnter = false;

        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (_health != null && _emitOnHealthDeath)
            {
                _health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null && _emitOnHealthDeath)
            {
                _health.Died -= HandleDied;
            }
        }

        private void HandleDied(KillInfo kill)
        {
            EmitNow();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_emitOnTriggerEnter)
            {
                return;
            }

            if (other.GetComponentInParent<Health>() != null)
            {
                EmitNow();
            }
        }

        public void EmitNow()
        {
            NoiseEventBus.EmitNoise(transform.position, _noiseRadius);
        }
    }
}
