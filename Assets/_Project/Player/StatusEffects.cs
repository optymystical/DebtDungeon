using DebtDungeon.Core;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DebtDungeon.Player
{
    // The player's status effects. Implements Core's IStatusReceiver, so AI projectiles, traps and abilities can
    // apply effects without referencing Player. Slowed and Poisoned don't stack: reapplying keeps whichever end
    // time is later. Strengthened and Weakened do apply stacks.
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
        [SerializeField] private int _strengthenStacks = 0;
        [SerializeField] private float _strengthenDuration = 3f;
        [SerializeField] private int _weakenedStacks = 0;
        [SerializeField] private float _weakenedDuration = 3f;
        public int StrengthenedStacks => _strengthenStacks; // Wrap-up fix: returned _weakenedStacks
        public int WeakenedStacks => _weakenedStacks;
        private const int _maxStacks = 3;

        private List<StatusEffectType> _statusEffectTypes = new List<StatusEffectType>();

        private Coroutine _slowCoroutine;
        private float _slowEndTime;

        private Coroutine _poisonCoroutine;
        private float _poisonEndTime;
        private int _poisonDamagePerTick;
        private GameObject _poisonSource;
        private Faction _poisonFaction;

        private Coroutine _strengthenCoroutine;
        private float _strengthenEndTime;

        private Coroutine _weakenedCoroutine;
        private float _weakenedEndTime;

        public bool IsSlowed => _slowCoroutine != null;
        public bool IsPoisoned => _poisonCoroutine != null;
        public bool IsStrengthened => _strengthenStacks > 0;
        public bool IsWeakened => _weakenedStacks > 0;

        public event System.Action<List<StatusEffectType>> EffectsChanged;

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
            RemoveStatus(StatusEffectType.Strengthened);
            RemoveStatus(StatusEffectType.Weakened);
        }

        // Core's IStatusReceiver: how AI, traps and abilities apply effects to the player
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
                case StatusEffectType.Strengthened:
                    ApplyStrengthened(duration, (int)magnitude);
                    break;
                case StatusEffectType.Weakened:
                    ApplyWeakened(duration, (int)magnitude);
                    break;
            }

            _statusEffectTypes.Add(type);
            EffectsChanged?.Invoke(_statusEffectTypes);
        }

        // Shortcut for test scripts: applies an effect with the Inspector defaults, poisons as if from a spider
        public void ApplyStatus(StatusEffectType type)
        {
            float duration = 0f;
            float magnitude = 0f;

            switch (type)
            {
                case StatusEffectType.Slowed:
                    duration = _slowDuration;
                    magnitude = _slowMultiplier;
                    break;
                case StatusEffectType.Poisoned:
                    duration = _poisonDuration;
                    magnitude = _poisonDamage;
                    break;
                case StatusEffectType.Strengthened:
                    duration = _strengthenDuration;
                    magnitude = _strengthenStacks;
                    break;
                case StatusEffectType.Weakened:
                    duration = _weakenedDuration;
                    magnitude = _weakenedStacks;
                    break;
            }

            ApplyStatus(type, duration, magnitude, null, Faction.Spider, 1);

            _statusEffectTypes.Add(type);
            EffectsChanged?.Invoke(_statusEffectTypes);
        }

        // Ends an effect early. Safe to call when it isn't active
        public void RemoveStatus(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Slowed:
                    if (_slowCoroutine != null)
                    {
                        StopCoroutine(_slowCoroutine);
                    }

                    _slowCoroutine = null;
                    _slowEndTime = 0f;
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
                case StatusEffectType.Strengthened:
                    if (_strengthenCoroutine != null)
                    {
                        StopCoroutine(_strengthenCoroutine);
                    }

                    _strengthenCoroutine = null;
                    _strengthenEndTime = 0f;
                    _strengthenStacks = 0;
                    break;
                case StatusEffectType.Weakened:
                    if (_weakenedCoroutine != null)
                    {
                        StopCoroutine(_weakenedCoroutine);
                    }

                    _weakenedCoroutine = null;
                    _weakenedEndTime = 0f;
                    _weakenedStacks = 0;
                    break;
            }

            _statusEffectTypes.Remove(type);
            EffectsChanged?.Invoke(_statusEffectTypes);
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

            RemoveStatus(StatusEffectType.Slowed);
        }

        private void EndSlow()
        {
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

            // Wrap-up fix (Chris/Claude): clear the poison when it runs out, like SlowTimer does for Slowed.
            // Without this _poisonCoroutine stayed set, so IsPoisoned (the HUD tag) never turned off and a new
            // poison could never start ticking again.
            RemoveStatus(StatusEffectType.Poisoned);
        }

        // Applies stacks of strengthened buff
        private void ApplyStrengthened(float duration, int stacks)
        {
            _strengthenEndTime = Mathf.Max(_strengthenEndTime, Time.time + duration);

            _strengthenStacks += stacks;
            if (_strengthenStacks > _maxStacks)
            {
                _strengthenStacks = _maxStacks;
            }

            _weakenedStacks = 0;

            if (_strengthenCoroutine == null)
            {
                _strengthenCoroutine = StartCoroutine(StrengthenTimer());
            }
        }

        // Waits until the (possibly refreshed) end time, then restores
        private IEnumerator StrengthenTimer()
        {
            while (Time.time < _strengthenEndTime)
            {
                yield return null;
            }

            RemoveStatus(StatusEffectType.Strengthened);
        }

        // Applies stacks of strengthened buff
        private void ApplyWeakened(float duration, int stacks)
        {
            _weakenedEndTime = Mathf.Max(_weakenedEndTime, Time.time + duration);

            _weakenedStacks += stacks;
            if (_weakenedStacks > _maxStacks)
            {
                _weakenedStacks = _maxStacks;
            }

            _strengthenStacks = 0;

            if (_weakenedCoroutine == null)
            {
                _weakenedCoroutine = StartCoroutine(WeakenedTimer());
            }
        }

        // Waits until the (possibly refreshed) end time, then restores
        private IEnumerator WeakenedTimer()
        {
            while (Time.time < _weakenedEndTime)
            {
                yield return null;
            }

            RemoveStatus(StatusEffectType.Weakened);
        }
    }
}
