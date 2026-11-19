using System;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The station object in the starting room. It only notices the player walking in and out of its trigger
    /// and announces it; StationScreen (on the GameManager's object) shows the prompt, handles E and draws the window.
    /// Announcing through a static event means a station the floor spawns later needs no scene references.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EquipmentStation : MonoBehaviour
    {
        /// <summary>True when the player enters a station's range, false when they leave it.</summary>
        public static event Action<bool> PlayerRangeChanged;

        private bool _playerInside;

        // Domain reload is off (Enter Play Mode Options), so clear the static event each time Play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerRangeChanged = null;
        }

        // Runs when the component is first added in the Editor: makes the collider a trigger for you.
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_playerInside && IsPlayer(other))
            {
                _playerInside = true;
                PlayerRangeChanged?.Invoke(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerInside && IsPlayer(other))
            {
                _playerInside = false;
                PlayerRangeChanged?.Invoke(false);
            }
        }

        private void OnDisable()
        {
            // A station destroyed or disabled with the player beside it must not leave the prompt stuck on.
            if (_playerInside)
            {
                _playerInside = false;
                PlayerRangeChanged?.Invoke(false);
            }
        }

        private static bool IsPlayer(Collider2D other)
        {
            Health health = other.GetComponentInParent<Health>();
            return health != null && health.Faction == Faction.Player;
        }
    }
}
