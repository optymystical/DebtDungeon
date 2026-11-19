using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Alpha win rule: the first spawner destroyed, by anyone (the player or a rival raid), opens the exit
    /// where it stood. Listens to Core's GameEvents.SpawnerDestroyed, which Larry's FactionSpawner2D raises,
    /// so Game needs no reference to the spawners themselves. Put it on the GameManager's object in Main.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExitSpawner : MonoBehaviour
    {
        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        [Tooltip("Leave empty to use the PlayerBootstrap on this object (only used by the debug menu item).")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Tooltip("The exit prefab: a sprite, a trigger collider and a LevelExit component.")]
        [SerializeField] private LevelExit _exitPrefab;

        private LevelExit _exit;

        /// <summary>True once the exit is on the floor.</summary>
        public bool IsExitOpen => _exit != null;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }

            if (_exitPrefab == null)
            {
                Debug.LogError("ExitSpawner has no exit prefab, so the floor can't be won.", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.SpawnerDestroyed += OnSpawnerDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.SpawnerDestroyed -= OnSpawnerDestroyed;
        }

        private void OnSpawnerDestroyed(Faction faction, Vector3 position)
        {
            // Only the first spawner counts; later ones change nothing.
            if (IsExitOpen || _exitPrefab == null)
            {
                return;
            }

            _exit = Instantiate(_exitPrefab, position, Quaternion.identity);
            _exit.Initialize(_gameManager);
            Debug.Log($"A {faction} spawner fell. The exit is open at {position}.", _exit);
        }

        // Play-mode test without a real spawner: right-click this component's header > this item.
        // It raises the same Core event a dying spawner does, two units to the player's right.
        [ContextMenu("Debug: Pretend A Spawner Died Next To The Player")]
        private void DebugSpawnerDestroyed()
        {
            if (!Application.isPlaying || _bootstrap == null || _bootstrap.PlayerObject == null)
            {
                Debug.LogWarning("Enter Play mode in Main first.", this);
                return;
            }

            Vector3 position = _bootstrap.PlayerObject.transform.position + Vector3.right * 2f;
            GameEvents.RaiseSpawnerDestroyed(Faction.Goblin, position);
        }
    }
}
