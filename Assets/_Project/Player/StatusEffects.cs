using System.Collections;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// The player's status effects. Implements Core's IStatusReceiver, so AI projectiles, traps and abilities can
    /// apply effects without referencing Player. Slowed and Poisoned don't stack: reapplying keeps whichever end
    /// time is later. Strengthened and Weakened come later (Status effects card).
    /// </summary>
    public class StatusEffects : MonoBehaviour, IStatusReceiver
    {
        private const float PoisonTickSeconds = 1f;

        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Health _health;

        [Header("Test defaults (used by TestStatus)")]
        [SerializeField] private float _slowMultiplier = 0.5f;
        [SerializeField] private float _slowDuration = 2f;
        [SerializeField] private int _poisonDamage = 1;
        [SerializeField] private float _poisonDuration = 3f;

        private Coroutine _slowCoroutine;
        private float _slowEndTime;

        private Coroutine _poisonCoroutine;
        private float _poisonEndTime;
        private int _poisonDamagePerTick;
        private GameObject _poisonSource;
        private Faction _poisonFaction;

        /// <summary>True while Slowed is active (for the HUD later).</summary>
        public bool IsSlowed => _slowCoroutine != null;

        /// <summary>True while Poisoned is active (for the HUD later).</summary>
        public bool IsPoisoned => _poisonCoroutine != null;

        private void Awake()
        {
            // The player prefab may not have these wired in the Inspector, so fall back to this object's components.
            if (_playerController == null)
            {
                _playerController = GetComponent<PlayerController>();
            }

            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        private void OnDisable()
        {
            // Disabling stops coroutines, so clear the state too, or the effects would look "active" forever.
            RemoveStatus(StatusEffectType.Slowed);
            RemoveStatus(StatusEffectType.Poisoned);
        }

        /// <summary>Core's IStatusReceiver: how AI, traps and abilities apply effects to the player.</summary>
        public void ApplyStatus(StatusEffectType type, float duration, float magnitude, GameObject source, Faction sourceFaction, int stacks)
        {
            if (duration <= 0f || !isActiveAndEnabled || (_health != null && !_health.IsAlive))
            {
                return;
            }

            switch (type)
            {
                case StatusEffectType.Slowed:
                    ApplySlow(duration, magnitude);
                    break;
                case StatusEffectType.Poisoned:
                    ApplyPoison(duration, Mathf.Max(1, Mathf.RoundToInt(magnitude)), source, sourceFaction);
                    break;
                // Strengthened and Weakened aren't built yet (Status effects card).
            }
        }

        /// <summary>Shortcut for test scripts: applies an effect with the Inspector defaults, poison as if from a spider.</summary>
        public void ApplyStatus(StatusEffectType type)
        {
            bool isSlow = type == StatusEffectType.Slowed;
            float duration = isSlow ? _slowDuration : _poisonDuration;
            float magnitude = isSlow ? _slowMultiplier : _poisonDamage;
            ApplyStatus(type, duration, magnitude, null, Faction.Spider, 1);
        }

        /// <summary>Ends an effect early. Safe to call when it isn't active.</summary>
        public void RemoveStatus(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Slowed:
                    if (_slowCoroutine != null)
                    {
                        StopCoroutine(_slowCoroutine);
                    }

                    EndSlow();
                    break;
                case StatusEffectType.Poisoned:
                    if (_poisonCoroutine != null)
                    {
                        StopCoroutine(_poisonCoroutine);
                    }

                    _poisonCoroutine = null;
                    _poisonEndTime = 0f;
                    break;
            }
        }

        private void ApplySlow(float duration, float multiplier)
        {
            _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);

            if (_playerController != null)
            {
                _playerController.SetStatusSpeedMultiplier(Mathf.Clamp(multiplier, 0.1f, 1f));
            }

            if (_slowCoroutine == null)
            {
                _slowCoroutine = StartCoroutine(SlowTimer());
            }
        }

        // Waits until the (possibly refreshed) end time, then restores normal speed.
        private IEnumerator SlowTimer()
        {
            while (Time.time < _slowEndTime)
            {
                yield return null;
            }

            EndSlow();
        }

        private void EndSlow()
        {
            _slowCoroutine = null;
            _slowEndTime = 0f;

            if (_playerController != null)
            {
                _playerController.SetStatusSpeedMultiplier(1f);
            }
        }

        private void ApplyPoison(float duration, int damagePerTick, GameObject source, Faction sourceFaction)
        {
            _poisonEndTime = Mathf.Max(_poisonEndTime, Time.time + duration);
            _poisonDamagePerTick = damagePerTick;
            _poisonSource = source;
            _poisonFaction = sourceFaction;

            if (_poisonCoroutine == null)
            {
                _poisonCoroutine = StartCoroutine(PoisonDoT());
            }
        }

        // Deals poison damage once per second until the (possibly refreshed) end time.
        private IEnumerator PoisonDoT()
        {
            while (Time.time < _poisonEndTime)
            {
                yield return new WaitForSeconds(PoisonTickSeconds);

                if (_health == null || !_health.IsAlive)
                {
                    break;
                }

                // Source can be null (the spider may be dead by now); Health and the death screen handle that.
                _health.TakeDamage(new DamageInfo(_poisonDamagePerTick, DamageType.Poison, _poisonSource, _poisonFaction));
            }

            _poisonCoroutine = null;
        }
    }
}
