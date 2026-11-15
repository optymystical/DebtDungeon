using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Connects the player's loadout to Core's Health: registers worn armor or Shroud as a damage absorber,
    /// swaps it when the loadout changes, ticks Shroud regeneration, and repairs armor for the station.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public class PlayerDefenses : MonoBehaviour
    {
        private Health _health;
        private Loadout _loadout;
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

        /// <summary>Gives the player a loadout and registers its armor or Shroud. Call once when the player spawns.</summary>
        public void Initialize(Loadout loadout)
        {
            if (loadout == null)
            {
                Debug.LogError("PlayerDefenses.Initialize was given no loadout.", this);
                return;
            }

            Unsubscribe();
            _loadout = loadout;
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
        }

        private void Subscribe()
        {
            if (_loadout == null || _isSubscribed || !isActiveAndEnabled)
            {
                return;
            }

            _loadout.LoadoutChanged += RefreshAbsorbers;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_loadout == null || !_isSubscribed)
            {
                return;
            }

            _loadout.LoadoutChanged -= RefreshAbsorbers;
            _isSubscribed = false;
        }
    }
}
