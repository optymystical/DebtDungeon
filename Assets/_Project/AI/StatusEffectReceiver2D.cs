using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    public class StatusEffectReceiver2D : MonoBehaviour
    {
        private Health _health;
        private Rigidbody2D _rb;
        private SpriteRenderer _renderer;

        private float _slowTimer;
        private float _slowMultiplier = 1f;

        private float _poisonTimer;
        private float _poisonTickTimer;
        private int _poisonTickDamage;
        private float _poisonTickInterval = 1f;
        private GameObject _poisonSource;
        private Faction _poisonFaction;

        public float SpeedMultiplier => _slowTimer > 0f ? _slowMultiplier : 1f;
        public bool IsSlowed => _slowTimer > 0f;
        public bool IsPoisoned => _poisonTimer > 0f;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }
            _slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        public void ApplyPoison(int tickDamage, float tickInterval, float duration, GameObject source, Faction sourceFaction)
        {
            if (duration <= 0f || tickDamage <= 0)
            {
                return;
            }
            _poisonTickDamage = tickDamage;
            _poisonTickInterval = Mathf.Max(0.2f, tickInterval);
            _poisonTimer = Mathf.Max(_poisonTimer, duration);
            _poisonTickTimer = _poisonTickInterval;
            _poisonSource = source;
            _poisonFaction = sourceFaction;
        }

        private void Update()
        {
            if (_slowTimer > 0f)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0f)
                {
                    _slowMultiplier = 1f;
                }
            }

            if (_poisonTimer > 0f && _health != null && _health.IsAlive)
            {
                _poisonTimer -= Time.deltaTime;
                _poisonTickTimer -= Time.deltaTime;

                if (_poisonTickTimer <= 0f)
                {
                    _poisonTickTimer = _poisonTickInterval;
                    DamageInfo dot = new DamageInfo(_poisonTickDamage, DamageType.Poison, _poisonSource, _poisonFaction, false);
                    _health.TakeDamage(dot);
                }
            }
        }

        private void FixedUpdate()
        {
            // Automatically scales velocity for both PlayerController and FactionAI2D
            if (_slowTimer > 0f && _rb != null)
            {
                _rb.linearVelocity *= _slowMultiplier;
            }
        }
    }
}
