using UnityEngine;
using System.Collections.Generic;
using System;



namespace DebtDungeon.AI
{
    /// <summary>
    /// Physical, destructible spawner structure placed in Major Chunks.
    /// Manages squad role caps (Residents/Guards) and halts spawning upon destruction.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FactionSpawner2D : MonoBehaviour
    {
        private const float DefaultSpawnRadius = 0.8f;

        [Header("Faction Configuration")]
        [Tooltip("Assigned faction data asset.")]
        [SerializeField] private FactionDefinition _faction;

        [Tooltip("Prefab to instantiate (e.g., GoblinGrunt.prefab). Leave empty to use procedural fallback.")]
        [SerializeField] private GameObject _residentPrefab;

        [Tooltip("Prefab to instantiate for guards.")]
        [SerializeField] private GameObject _guardPrefab;

        [Tooltip("Unit configuration asset for procedural fallback.")]
        [SerializeField] private UnitConfig _residentConfig;

        [Tooltip("Unit configuration asset for guards.")]
        [SerializeField] private UnitConfig _guardConfig;

        [Tooltip("Destination transform representing the opposing faction's camp.")]
        [SerializeField] private Transform _rivalTerritoryTarget;

        [Tooltip("Layer mask designating static obstacle geometry.")]
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Population Caps")]
        [Tooltip("Maximum active resident units allowed simultaneously.")]
        [SerializeField, Min(1)] private int _maxResidents = 6;

        [Tooltip("Maximum active guard units allowed simultaneously.")]
        [SerializeField, Min(1)] private int _maxGuards = 2;

        [Tooltip("Interval in seconds between spawn attempts.")]
        [SerializeField, Min(0.5f)] private float _spawnInterval = 3.5f;

        [Header("Structure Health")]
        [Tooltip("Structure durability before being destroyed.")]
        [SerializeField, Min(10)] private int _maxHealth = 150;
        private int _currentHealth;

        private readonly List<FactionAI2D> _activeResidents = new List<FactionAI2D>();
        private readonly List<FactionAI2D> _activeGuards = new List<FactionAI2D>();
        private float _spawnTimer;

        public FactionDefinition Faction => _faction;
        public IReadOnlyList<FactionAI2D> ActiveResidents => _activeResidents;
        public IReadOnlyList<FactionAI2D> ActiveGuards => _activeGuards;
        public int TotalLivingCount => _activeResidents.Count + _activeGuards.Count;
        public bool IsDestroyed => _currentHealth <= 0;

        /// <summary>
        /// Hook for Core v1.2: raised when structure collapses.
        /// First destroyed spawner spawns the floor exit.
        /// </summary>
        public static event Action<FactionSpawner2D> SpawnerDestroyed;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            // Spawner structure counts as wall obstacle geometry
            gameObject.layer = LayerMask.NameToLayer("Walls");
        }

        private void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            // Strip null or dead unit references
            _activeResidents.RemoveAll(u => u == null || !u.IsAlive);
            _activeGuards.RemoveAll(u => u == null || !u.IsAlive);

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _spawnInterval;
                TrySpawnNextUnit();
            }
        }

        private void TrySpawnNextUnit()
        {
            if (_activeGuards.Count < _maxGuards)
            {
                SpawnUnit(UnitRole.Guard, _guardPrefab, _guardConfig, _activeGuards);
            }
            else if (_activeResidents.Count < _maxResidents)
            {
                SpawnUnit(UnitRole.Resident, _residentPrefab, _residentConfig, _activeResidents);
            }
        }

        private void SpawnUnit(UnitRole role, GameObject prefab, UnitConfig config, List<FactionAI2D> trackerList)
        {
            if (_faction == null)
            {
                Debug.LogWarning("FactionSpawner2D missing FactionDefinition assignment.", this);
                return;
            }

            Vector2 spawnOffset = UnityEngine.Random.insideUnitCircle * DefaultSpawnRadius;
            Vector2 spawnPosition = (Vector2)transform.position + spawnOffset;

            GameObject unitObject;

            if (prefab != null)
            {
                unitObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                if (config == null)
                {
                    return;
                }

                unitObject = new GameObject($"{_faction.FactionName}_{role}_{config.name}");
                unitObject.transform.position = spawnPosition;

                SpriteRenderer renderer = unitObject.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = "Actors";

                CircleCollider2D collider = unitObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.45f;

                CombatAgent2D combatAgent = unitObject.AddComponent<CombatAgent2D>();
                combatAgent.Initialize(_faction, config);

                unitObject.AddComponent<FactionAI2D>();
            }

            // Assign dedicated physics layer (7: Goblin, 8: Spider)
            //unitObject.layer = _faction.FactionName == "Goblin" ? 7 : 8;

            FactionAI2D ai = unitObject.GetComponent<FactionAI2D>();
            if (ai != null)
            {
                Vector2 targetTerritory = _rivalTerritoryTarget != null ? (Vector2)_rivalTerritoryTarget.position : Vector2.zero;
                ai.ConfigureRole(role, transform.position, _obstacleMask, targetTerritory);
                trackerList.Add(ai);
            }
        }

        public void TakeDamage(int damage)
        {
            if (IsDestroyed)
            {
                return;
            }

            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                SpawnerDestroyed?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
