using UnityEngine;
using System.Collections.Generic;
using DebtDungeon.Core;



namespace DebtDungeon.AI
{
    /// <summary>
    /// Physical, destructible spawner structure placed in Major Chunks.
    /// Uses Core's Health component, implements IKillReward, and raises GameEvents.RaiseSpawnerDestroyed on death.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Health))]
    public class FactionSpawner2D : MonoBehaviour, IKillReward
    {
        private const float DefaultSpawnRadius = 0.8f;

        [Header("Spawner Identity & Rewards")]
        [SerializeField] private string _displayName = "Goblin Camp";
        [SerializeField, Min(0)] private int _xpReward = 50;
        [SerializeField, Min(0)] private int _goldReward = 25;

        [Header("Spawner Configuration")]
        [SerializeField] private FactionData _factionData;
        [SerializeField] private GameObject _residentPrefab;
        [SerializeField] private GameObject _guardPrefab;
        [SerializeField] private UnitConfig _residentConfig;
        [SerializeField] private UnitConfig _guardConfig;
        [SerializeField] private Transform _rivalTerritoryTarget;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Population Caps")]
        [SerializeField, Min(1)] private int _maxResidents = 6;
        [SerializeField, Min(1)] private int _maxGuards = 2;
        [SerializeField, Min(0.5f)] private float _spawnInterval = 3.5f;

        private Health _health;
        private IFloorQuery _floor;
        private readonly List<FactionAI2D> _activeResidents = new List<FactionAI2D>();
        private readonly List<FactionAI2D> _activeGuards = new List<FactionAI2D>();
        private float _spawnTimer;

        // IKillReward Implementation
        public string DisplayName => !string.IsNullOrWhiteSpace(_displayName) ? _displayName : gameObject.name;
        public int XpReward => _xpReward;
        public int GoldReward => _goldReward;

        public Faction Faction => _health != null ? _health.Faction : Faction.Neutral;
        public IReadOnlyList<FactionAI2D> ActiveResidents => _activeResidents;
        public IReadOnlyList<FactionAI2D> ActiveGuards => _activeGuards;
        public int TotalLivingCount => _activeResidents.Count + _activeGuards.Count;
        public bool IsDestroyed => _health == null || !_health.IsAlive;

        private void Awake()
        {
            _health = GetComponent<Health>();
            gameObject.layer = LayerMask.NameToLayer("Walls");
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += HandleSpawnerDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= HandleSpawnerDied;
            }
        }

        public void ConfigureFromFloor(
            IFloorQuery floor,
            FactionData factionData,
            LayerMask obstacleMask,
            Vector3 rivalTerritory)
        {
            _floor = floor;

            if (factionData != null)
            {
                _factionData = factionData;
            }

            _obstacleMask = obstacleMask;

            if (_rivalTerritoryTarget == null && rivalTerritory != Vector3.zero)
            {
                GameObject marker = new GameObject($"{Faction}_RivalTarget");
                marker.transform.position = rivalTerritory;
                _rivalTerritoryTarget = marker.transform;
            }
        }

        private void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

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
            if (prefab == null)
            {
                Debug.LogWarning($"[FactionSpawner2D] Missing prefab for {role} on {gameObject.name}.", this);
                return;
            }

            Vector2 spawnOffset = UnityEngine.Random.insideUnitCircle * DefaultSpawnRadius;
            Vector2 spawnPosition = (Vector2)transform.position + spawnOffset;

            GameObject unitObject = Instantiate(prefab, spawnPosition, Quaternion.identity);

            CombatAgent2D combatAgent = unitObject.GetComponent<CombatAgent2D>();
            if (combatAgent != null && config != null)
            {
                combatAgent.Initialize(config);
            }

            FactionAI2D ai = unitObject.GetComponent<FactionAI2D>();
            if (ai != null)
            {
                if (_floor != null)
                {
                    ai.SetFloorQuery(_floor);
                }

                Vector2 targetTerritory = _rivalTerritoryTarget != null ? (Vector2)_rivalTerritoryTarget.position : Vector2.zero;
                ai.ConfigureRole(role, transform.position, _obstacleMask, _factionData, targetTerritory);
                trackerList.Add(ai);
            }
        }

        private void HandleSpawnerDied(KillInfo kill)
        {
            GameEvents.RaiseSpawnerDestroyed(Faction, transform.position);
            Destroy(gameObject);
        }
    }
}
