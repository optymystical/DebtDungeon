using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The Game side of "the floor is ready". When FloorGen raises GameEvents.FloorGenerated, this moves the
    /// player to the floor's PlayerStart and puts an equipment station on every Station spawn point.
    /// (Larry's FactionFloorDirector hears the same event and places the spawners; the exit still comes from
    /// ExitSpawner.) Everything goes through Core's IFloorQuery, so Game never references FloorGen's classes.
    /// Put it on the GameManager's object in Main, next to PlayerBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloorIntegration : MonoBehaviour
    {
        [Tooltip("Leave empty to use the PlayerBootstrap on this object.")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Tooltip("The station prefab (sprite, trigger collider, EquipmentStation).")]
        [SerializeField] private GameObject _stationPrefab;

        [Tooltip("The hand-placed station used before the floor existed. Turned off once the floor provides a Station spawn point.")]
        [SerializeField] private GameObject _placeholderStation;

        // Stations this component spawned, so a second FloorGenerated (e.g. the debug floor) doesn't stack them.
        private readonly List<GameObject> _spawnedStations = new List<GameObject>();

        /// <summary>The floor from the last FloorGenerated, or null before the floor exists.</summary>
        public IFloorQuery CurrentFloor { get; private set; }

        private void Awake()
        {
            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }
        }

        private void OnEnable()
        {
            GameEvents.FloorGenerated += OnFloorGenerated;
        }

        private void OnDisable()
        {
            GameEvents.FloorGenerated -= OnFloorGenerated;
        }

        private void OnFloorGenerated(IFloorQuery floor)
        {
            CurrentFloor = floor;
            if (floor == null)
            {
                return;
            }

            MovePlayer(floor.PlayerStart);
            PlaceStations(floor);
        }

        private void MovePlayer(Vector3 position)
        {
            if (_bootstrap == null || _bootstrap.PlayerObject == null)
            {
                Debug.LogWarning("FloorIntegration: the floor is ready but there's no player to move.", this);
                return;
            }

            GameObject player = _bootstrap.PlayerObject;

            // Move the physics body too, or the Rigidbody2D would pull the player back on the next physics step.
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }

            player.transform.position = position;
        }

        private void PlaceStations(IFloorQuery floor)
        {
            foreach (GameObject station in _spawnedStations)
            {
                if (station != null)
                {
                    Destroy(station);
                }
            }

            _spawnedStations.Clear();

            IReadOnlyList<SpawnPoint> spawnPoints = floor.GetSpawnPoints();
            if (spawnPoints == null || _stationPrefab == null)
            {
                return;
            }

            foreach (SpawnPoint point in spawnPoints)
            {
                if (point.Type == SpawnPointType.Station)
                {
                    _spawnedStations.Add(Instantiate(_stationPrefab, point.Position, Quaternion.identity));
                }
            }

            // The real floor has stations now, so the hand-placed one would be a second, misplaced copy.
            if (_spawnedStations.Count > 0 && _placeholderStation != null)
            {
                _placeholderStation.SetActive(false);
            }

            if (_spawnedStations.Count == 0)
            {
                Debug.LogWarning("FloorIntegration: the floor has no Station spawn point, so no station was placed.", this);
            }
        }

        // Play-mode test without Gregory's floor: right-click this component's header > this item.
        // Sends a small fake floor: the player start 4 units to the player's right, a station 2 units past that.
        // Note: Larry's FactionFloorDirector hears it too and resets its unit lists (harmless in a test).
        [ContextMenu("Debug: Send A Test Floor")]
        private void DebugSendTestFloor()
        {
            if (!Application.isPlaying || _bootstrap == null || _bootstrap.PlayerObject == null)
            {
                Debug.LogWarning("Enter Play mode in Main first.", this);
                return;
            }

            Vector3 start = _bootstrap.PlayerObject.transform.position + new Vector3(4f, 0f, 0f);
            GameEvents.RaiseFloorGenerated(new DebugFloorQuery(start, start + new Vector3(2f, 1f, 0f)));
        }
    }
}
