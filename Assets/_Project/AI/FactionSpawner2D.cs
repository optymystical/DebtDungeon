using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DebtDungeon.Core;
using System;



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
        private const float HitFlashDuration = 0.1f;

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
        [SerializeField, Min(1)] private int _fallbackMaxResidents = 6;
        [SerializeField, Min(1)] private int _fallbackMaxGuards = 2;
        [SerializeField, Min(0.5f)] private float _spawnInterval = 3.5f;

        [Header("Specialist Roster (Optional Resident Mix-In)")]
        [SerializeField] private GameObject _specialistPrefab;
        [SerializeField] private UnitConfig _specialistConfig;
        [SerializeField, Range(0f, 1f)] private float _specialistSpawnChance = 0.3f;

        private Health _health;
        private IFloorQuery _floor;
        private Func<Faction, UnitRole, bool> _canSpawnMapWideQuery;
        private Action<Faction, FactionAI2D> _onUnitSpawnedCallback;
        private readonly List<FactionAI2D> _localResidents = new List<FactionAI2D>();
        private readonly List<FactionAI2D> _localGuards = new List<FactionAI2D>();
        private float _spawnTimer;

        // Wrap-up addition (Chris/Claude): red hit flash, same look and length as CombatAgent2D's on units.
        private SpriteRenderer _spriteRenderer;
        private Color _baseColor = Color.white;
        private Coroutine _flashCoroutine;

        // IKillReward Implementation
        public string DisplayName => !string.IsNullOrWhiteSpace(_displayName) ? _displayName : gameObject.name;
        public int XpReward => _xpReward;
        public int GoldReward => _goldReward;

        public Faction Faction => _health != null ? _health.Faction : Faction.Neutral;
        public bool IsDestroyed => _health == null || !_health.IsAlive;

        
        private void Awake()
        {
            _health = GetComponent<Health>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
            }
            // Layer comes from the prefab (Goblin or Spider), so faction attacks and masks can hit the spawner.
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += HandleSpawnerDied;
                _health.Damaged += HandleSpawnerDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= HandleSpawnerDied;
                _health.Damaged -= HandleSpawnerDamaged;
            }
        }

        private void HandleSpawnerDamaged(DamageInfo info, int damageTaken)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(HitFlashDuration);

            // The base color is stored once in Awake, so quick repeat hits can't leave it stuck on red.
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }

            _flashCoroutine = null;
        }

        public void ConfigureFromFloor(
            IFloorQuery floor,
            FactionData factionData,
            LayerMask obstacleMask,
            Vector3 rivalTerritory,
            Func<Faction, UnitRole, bool> canSpawnMapWideQuery = null,
            Action<Faction, FactionAI2D> onUnitSpawnedCallback = null)
        {
            _floor = floor;
            _canSpawnMapWideQuery = canSpawnMapWideQuery;
            _onUnitSpawnedCallback = onUnitSpawnedCallback;

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

            _localResidents.RemoveAll(u => u == null || !u.IsAlive);
            _localGuards.RemoveAll(u => u == null || !u.IsAlive);

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _spawnInterval;
                TrySpawnNextUnit();
            }
        }

        private void TrySpawnNextUnit()
        {
            if (CanSpawnRole(UnitRole.Guard))
            {
                SpawnUnit(UnitRole.Guard, _guardPrefab, _guardConfig, _localGuards);
            }
            else if (CanSpawnRole(UnitRole.Resident))
            {
                SpawnUnit(UnitRole.Resident, _residentPrefab, _residentConfig, _localResidents);
            }
        }
        private bool CanSpawnRole(UnitRole role)
        {
            if (_canSpawnMapWideQuery != null)
            {
                return _canSpawnMapWideQuery(Faction, role);
            }

            return role == UnitRole.Guard
                ? _localGuards.Count < _fallbackMaxGuards
                : _localResidents.Count < _fallbackMaxResidents;
        }

        private void SpawnUnit(UnitRole role, GameObject prefab, UnitConfig config, List<FactionAI2D> localTracker)
        {
            GameObject selectedPrefab = prefab;
            UnitConfig selectedConfig = config;

            if (role == UnitRole.Resident && _specialistPrefab != null && UnityEngine.Random.value < _specialistSpawnChance)
            {
                selectedPrefab = _specialistPrefab;
                if (_specialistConfig != null)
                {
                    selectedConfig = _specialistConfig;
                }
            }

            if (selectedPrefab == null)
            {
                Debug.LogWarning($"[FactionSpawner2D] Missing prefab for {role} on {gameObject.name}.", this);
                return;
            }

            
            Vector2 spawnPosition = FindWalkableSpawnPoint();

            GameObject unitObject = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            if (TryGetComponent(out SpawnerAnimator2D spawnerAnim))
            {
                spawnerAnim.TriggerSpawnAnimation();
            }

            CombatAgent2D combatAgent = unitObject.GetComponent<CombatAgent2D>();
            if (combatAgent != null && selectedConfig != null)
            {
                combatAgent.Initialize(selectedConfig);
               
            }

            FactionAI2D ai = unitObject.GetComponent<FactionAI2D>();
            if (ai != null)
            {
                if (_floor != null)
                {
                    ai.SetFloorQuery(_floor);
                }

                Vector2 targetTerritory = _rivalTerritoryTarget != null ? (Vector2)_rivalTerritoryTarget.position : Vector2.zero;
                ai.ConfigureRole(role, spawnPosition, _obstacleMask, _factionData, targetTerritory);
                localTracker.Add(ai);
                _onUnitSpawnedCallback?.Invoke(Faction,ai);
            }
        }

        private Vector2 FindWalkableSpawnPoint()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 candidate = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * DefaultSpawnRadius;
                if (IsPositionWalkable(candidate))
                {
                    return candidate;
                }
            }

            // Widen search radius to 1.6 units if immediate perimeter is blocked
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector2 candidate = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.6f;
                if (IsPositionWalkable(candidate))
                {
                    return candidate;
                }
            }

            return transform.position;
        }
        private bool IsPositionWalkable(Vector2 pos)
        {
            if (_floor != null)
            {
                return _floor.IsWalkable(_floor.WorldToCell(pos));
            }

            return Physics2D.OverlapCircle(pos, 0.35f, _obstacleMask) == null;
        }

        private void HandleSpawnerDied(KillInfo kill)
        {
            // No exit key (decided Nov 10): Game's ExitSpawner opens the exit where the first spawner falls.
            GameEvents.RaiseSpawnerDestroyed(Faction, transform.position);

            if (TryGetComponent(out Collider2D col))
            {
                col.enabled = false;
            }

            if (TryGetComponent(out SpawnerAnimator2D spawnerAnim))
            {
                spawnerAnim.TriggerDeathAnimation();
                Destroy(gameObject, 0.75f);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
