using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The exit the first destroyed spawner leaves behind. When the living player touches its trigger,
    /// it tells the GameManager, which switches to Won (and the EndScreen shows the results).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        private GameManager _gameManager;

        /// <summary>Called by ExitSpawner right after it creates the exit.</summary>
        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // Runs when the component is first added in the Editor: makes the collider a trigger for you.
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_gameManager == null)
            {
                return;
            }

            // The player's collider may sit on a child object, so look upward for its Health.
            Health health = other.GetComponentInParent<Health>();
            if (health != null && health.Faction == Faction.Player && health.IsAlive)
            {
                _gameManager.ReportExitReached();
            }
        }
    }
}
