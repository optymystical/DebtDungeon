using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A worn Shroud. Like armor, but it also blocks Magic and regenerates: after RegenDelay seconds without a hit,
    /// it restores RegenPerSecond Durability each second. Poison goes through. Tick must be called every frame.
    /// </summary>
    public class ShroudAbsorber : EquipmentAbsorber
    {
        private float _secondsSinceLastHit;

        /// <summary>Starts at full Durability from the Shroud's data.</summary>
        public ShroudAbsorber(ShroudDefinition shroud)
            : base(shroud.Durability, shroud.Limit)
        {
            Shroud = shroud;
            _secondsSinceLastHit = shroud.RegenDelay;
        }

        /// <summary>The Shroud this pool belongs to.</summary>
        public ShroudDefinition Shroud { get; }

        /// <summary>Advances the regen timer. Call once per frame with Time.deltaTime (0 while paused).</summary>
        public void Tick(float deltaTime)
        {
            _secondsSinceLastHit += deltaTime;

            // Only the part of this frame after the delay counts, so regen starts exactly when the delay ends.
            float secondsPastDelay = _secondsSinceLastHit - Shroud.RegenDelay;
            if (secondsPastDelay > 0f)
            {
                RestoreDurability(Shroud.RegenPerSecond * Mathf.Min(deltaTime, secondsPastDelay));
            }
        }

        /// <inheritdoc/>
        protected override bool Blocks(DamageType type)
        {
            return type == DamageType.Physical || type == DamageType.Magic;
        }

        /// <summary>Any hit, even poison, restarts the regen delay.</summary>
        protected override void OnHit()
        {
            _secondsSinceLastHit = 0f;
        }
    }
}
