using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Connects the player's loadout to Core's Health: registers worn armor or Shroud as a damage absorber,
    /// swaps it when the loadout changes, scales it with the defensive stat, ticks Shroud regeneration,
    /// and repairs armor for the station.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public class PlayerDefenses : MonoBehaviour
    {
        private Health _health;
        private Loadout _loadout;
        private CharacterStats _stats;
        private ArmorAbsorber _armor;
        private ShroudAbsorber _shroud;
        private bool _isSubscribed;

        /// <summary>The worn armor's pool, or null. For the HUD and station UI.</summary>
        public ArmorAbsorber Armor => _armor;

        /// <summary>The worn Shroud's pool, or null. For the HUD.</summary>
        public ShroudAbsorber Shroud => _shroud;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Update()
        {
            if (_shroud != null)
            {
                _shroud.Tick(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Gives the player a loadout (and the stats that scale its armor or Shroud) and registers the absorbers.
        /// Call once when the player spawns. stats can be null (tests, sandboxes): the items then use their listed values.
        /// </summary>
        public void Initialize(Loadout loadout, CharacterStats stats = null)
        {
            if (loadout == null)
            {
                Debug.LogError("PlayerDefenses.Initialize was given no loadout.", this);
                return;
            }

            Unsubscribe();
            _loadout = loadout;
            _stats = stats;
            Subscribe();
            RefreshAbsorbers();
        }

        /// <summary>Restores worn armor to full Durability. The station calls this (out of combat only).</summary>
        public void RepairArmor()
        {
            if (_armor != null)
            {
                _armor.Repair();
            }
        }

        private void RefreshAbsorbers()
        {
            ArmorDefinition wornArmor = _loadout.GetEquipped(EquipmentSlot.Armor) as ArmorDefinition;
            ShroudDefinition wornShroud = _loadout.GetEquipped(EquipmentSlot.Shroud) as ShroudDefinition;

            // Only rebuild a pool when the item itself changed, so swapping a weapon keeps the armor's damage.
            if (_armor == null || _armor.Armor != wornArmor)
            {
                _health.RemoveAbsorber(_armor);
                _armor = wornArmor != null ? new ArmorAbsorber(wornArmor) : null;
                _health.AddAbsorber(_armor);
            }

            if (_shroud == null || _shroud.Shroud != wornShroud)
            {
                _health.RemoveAbsorber(_shroud);
                _shroud = wornShroud != null ? new ShroudAbsorber(wornShroud) : null;
                _health.AddAbsorber(_shroud);
            }

            // A newly worn item starts full, then scales up keeping 100%.
            ApplyStatScaling();
        }

        // Stats changed (a stat point, an affix, a level) or an item was swapped: rescale both pools.
        private void ApplyStatScaling()
        {
            ApplyStatPower(_armor);
            ApplyStatPower(_shroud);
        }

        private void ApplyStatPower(EquipmentAbsorber absorber)
        {
            if (absorber != null)
            {
                absorber.ApplyStatPower(_stats != null ? _stats.GetPower(absorber.ScalingStat) : 1f);
            }
        }

        private void Subscribe()
        {
            if (_loadout == null || _isSubscribed || !isActiveAndEnabled)
            {
                return;
            }

            _loadout.LoadoutChanged += RefreshAbsorbers;
            if (_stats != null)
            {
                _stats.StatsChanged += ApplyStatScaling;
            }

            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_loadout == null || !_isSubscribed)
            {
                return;
            }

            _loadout.LoadoutChanged -= RefreshAbsorbers;
            if (_stats != null)
            {
                _stats.StatsChanged -= ApplyStatScaling;
            }

            _isSubscribed = false;
        }
    }
}
