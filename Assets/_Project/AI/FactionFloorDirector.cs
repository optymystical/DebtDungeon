using UnityEngine;
using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    [Serializable]
    public class FactionSpawnSet
    {
        public Faction Faction;
        public GameObject SpawnerPrefab;
        public GameObject ResidentPrefab;
        public GameObject GuardPrefab;
        public UnitConfig ResidentConfig;
        public UnitConfig GuardConfig;
    }

    /// <summary>
    /// Orchestrates floor-wide enemy placement upon GameEvents.FloorGenerated,
    /// enforces map-wide population caps per faction, and runs the cross-map raid timer
    /// (drafting 1/2 of Residents + 1/4 of Guards per faction).
    /// </summary>
    public class FactionFloorDirector : MonoBehaviour
    {
        [Header("Diplomacy & Masks")]
        [SerializeField] private FactionData _factionData;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Faction Prefab Sets")]
        [SerializeField] private List<FactionSpawnSet> _factionSets = new List<FactionSpawnSet>();

        [Header("Map-Wide Faction Population Caps")]
        [SerializeField, Min(1)] private int _maxResidentsPerFaction = 10;
        [SerializeField, Min(1)] private int _maxGuardsPerFaction = 4;

        [Header("Cross-Map Raid Scheduler")]
        [SerializeField, Min(5f)] private float _raidIntervalSeconds = 45f;

        private readonly Dictionary<Faction, List<FactionAI2D>> _unitsByFaction = new Dictionary<Faction, List<FactionAI2D>>();
        private readonly List<FactionSpawner2D> _activeSpawners = new List<FactionSpawner2D>();

        private IFloorQuery _currentFloor;
        private float _raidTimer;

        private void Awake()
        {
            _raidTimer = _raidIntervalSeconds;
            EnsureFactionBuckets();
        }

        private void Start()
        {
            // Support standalone sandbox scenes where spawners are placed manually in the Hierarchy
            if (_currentFloor == null)
            {
                FactionSpawner2D[] sceneSpawners = FindObjectsByType<FactionSpawner2D>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneSpawners.Length; i++)
                {
                    if (sceneSpawners[i] != null && !_activeSpawners.Contains(sceneSpawners[i]))
                    {
                        _activeSpawners.Add(sceneSpawners[i]);
                    }
                }

                for (int i = 0; i < _activeSpawners.Count; i++)
                {
                    FactionSpawner2D spawner = _activeSpawners[i];
                    Vector3 rivalPos = FindRivalTargetPosition(spawner.Faction, spawner.transform.position);
                    spawner.ConfigureFromFloor(
                        null,
                        _factionData,
                        _obstacleMask,
                        rivalPos,
                        CanSpawnForFaction,
                        RegisterSpawnedUnit);
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.FloorGenerated += HandleFloorGenerated;
        }

        private void OnDisable()
        {
            GameEvents.FloorGenerated -= HandleFloorGenerated;
        }

        private void Update()
        {
            RemoveDeadUnits();

            _raidTimer -= Time.deltaTime;
            if (_raidTimer <= 0f)
            {
                _raidTimer = _raidIntervalSeconds;
                ExecuteCrossMapRaids();
            }
        }

        private void EnsureFactionBuckets()
        {
            for (int i = 0; i < _factionSets.Count; i++)
            {
                Faction f = _factionSets[i].Faction;
                if (!_unitsByFaction.ContainsKey(f))
                {
                    _unitsByFaction[f] = new List<FactionAI2D>();
                }
            }
        }

        private void HandleFloorGenerated(IFloorQuery floor)
        {
            _currentFloor = floor;
            _raidTimer = _raidIntervalSeconds;

            _unitsByFaction.Clear();
            _activeSpawners.Clear();
            EnsureFactionBuckets();

            if (_currentFloor == null)
            {
                return;
            }

            ProcessFloorSpawnPoints(_currentFloor);
        }

        private void ProcessFloorSpawnPoints(IFloorQuery floor)
        {
            IReadOnlyList<SpawnPoint> spawnPoints = floor.GetSpawnPoints();
            if (spawnPoints == null)
            {
                return;
            }

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint sp = spawnPoints[i];
                if (sp.Type != SpawnPointType.Spawner)
                {
                    continue;
                }

                FactionSpawnSet set = GetSpawnSet(sp.Faction);
                if (set == null || set.SpawnerPrefab == null)
                {
                    continue;
                }

                GameObject spawnerObj = Instantiate(set.SpawnerPrefab, sp.Position, Quaternion.identity);
                FactionSpawner2D spawner = spawnerObj.GetComponent<FactionSpawner2D>();
                if (spawner != null)
                {
                    _activeSpawners.Add(spawner);
                }
            }

            for (int i = 0; i < _activeSpawners.Count; i++)
            {
                FactionSpawner2D spawner = _activeSpawners[i];
                Vector3 rivalPos = FindRivalTargetPosition(spawner.Faction, spawner.transform.position);
                spawner.ConfigureFromFloor(
                    floor,
                    _factionData,
                    _obstacleMask,
                    rivalPos,
                    CanSpawnForFaction,
                    RegisterSpawnedUnit);
            }

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint sp = spawnPoints[i];
                if (sp.Type != SpawnPointType.SingleEnemy)
                {
                    continue;
                }

                FactionSpawnSet set = GetSpawnSet(sp.Faction);
                if (set == null)
                {
                    continue;
                }

                Vector2Int tileCell = floor.WorldToCell(sp.Position);
                ChunkIntensity intensity = floor.GetIntensityAt(tileCell);
                UnitRole role = intensity == ChunkIntensity.Minor ? UnitRole.Guard : UnitRole.Resident;

                if (!CanSpawnForFaction(sp.Faction, role))
                {
                    continue;
                }

                GameObject prefab = role == UnitRole.Guard ? set.GuardPrefab : set.ResidentPrefab;
                UnitConfig config = role == UnitRole.Guard ? set.GuardConfig : set.ResidentConfig;

                if (prefab == null)
                {
                    continue;
                }

                GameObject unitObj = Instantiate(prefab, sp.Position, Quaternion.identity);

                CombatAgent2D combat = unitObj.GetComponent<CombatAgent2D>();
                if (combat != null && config != null)
                {
                    combat.Initialize(config);
                }

                FactionAI2D ai = unitObj.GetComponent<FactionAI2D>();
                if (ai != null)
                {
                    ai.SetFloorQuery(floor);
                    Vector2 rivalPos = FindRivalTargetPosition(sp.Faction, sp.Position);
                    ai.ConfigureRole(role, sp.Position, _obstacleMask, _factionData, rivalPos);
                    RegisterSpawnedUnit(sp.Faction, ai);
                }
            }
        }

        private bool CanSpawnForFaction(Faction faction, UnitRole role)
        {
            if (!_unitsByFaction.TryGetValue(faction, out List<FactionAI2D> roster))
            {
                return true;
            }

            int livingCount = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] != null && roster[i].IsAlive && roster[i].Role == role)
                {
                    livingCount++;
                }
            }

            int cap = role == UnitRole.Guard ? _maxGuardsPerFaction : _maxResidentsPerFaction;
            return livingCount < cap;
        }

        private void RegisterSpawnedUnit(Faction faction, FactionAI2D unit)
        {
            if (unit == null)
            {
                return;
            }

            if (!_unitsByFaction.TryGetValue(faction, out List<FactionAI2D> roster))
            {
                roster = new List<FactionAI2D>();
                _unitsByFaction[faction] = roster;
            }

            if (!roster.Contains(unit))
            {
                roster.Add(unit);
            }
        }

        private void RemoveDeadUnits()
        {
            _activeSpawners.RemoveAll(s => s == null || s.IsDestroyed);

            foreach (KeyValuePair<Faction, List<FactionAI2D>> pair in _unitsByFaction)
            {
                pair.Value.RemoveAll(u => u == null || !u.IsAlive);
            }
        }

        private void ExecuteCrossMapRaids()
        {
            foreach (KeyValuePair<Faction, List<FactionAI2D>> pair in _unitsByFaction)
            {
                Faction raiderFaction = pair.Key;
                List<FactionAI2D> roster = pair.Value;

                List<FactionAI2D> residents = new List<FactionAI2D>();
                List<FactionAI2D> guards = new List<FactionAI2D>();

                for (int i = 0; i < roster.Count; i++)
                {
                    FactionAI2D unit = roster[i];
                    if (unit == null || !unit.IsAlive || unit.CurrentState == AIState.Raid)
                    {
                        continue;
                    }

                    if (unit.Role == UnitRole.Resident)
                    {
                        residents.Add(unit);
                    }
                    else if (unit.Role == UnitRole.Guard)
                    {
                        guards.Add(unit);
                    }
                }

                // 1/2 of Residents + 1/4 of Guards (round up if at least 2 units exist)
                int residentDraftCount = residents.Count / 2;
                int guardDraftCount = guards.Count / 4;

                if (residentDraftCount == 0 && guardDraftCount == 0)
                {
                    continue;
                }

                Vector3 raidDestination = FindRivalTargetPosition(raiderFaction, Vector3.zero);
                if (raidDestination == Vector3.zero)
                {
                    continue;
                }

                for (int i = 0; i < residentDraftCount; i++)
                {
                    residents[i].TriggerRaidState(raidDestination);
                }

                for (int i = 0; i < guardDraftCount; i++)
                {
                    guards[i].TriggerRaidState(raidDestination);
                }
            }
        }

        private Vector3 FindRivalTargetPosition(Faction sourceFaction, Vector3 originPos)
        {
            Vector3 bestTarget = Vector3.zero;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _activeSpawners.Count; i++)
            {
                FactionSpawner2D candidate = _activeSpawners[i];
                if (candidate == null || candidate.IsDestroyed || candidate.Faction == sourceFaction)
                {
                    continue;
                }

                if (_factionData != null && !_factionData.IsHostile(sourceFaction, candidate.Faction))
                {
                    continue;
                }

                float dist = Vector3.Distance(originPos, candidate.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = candidate.transform.position;
                }
            }

            if (bestTarget == Vector3.zero && _currentFloor != null)
            {
                for (int i = 0; i < _factionSets.Count; i++)
                {
                    Faction candidateFaction = _factionSets[i].Faction;
                    if (candidateFaction == sourceFaction)
                    {
                        continue;
                    }

                    if (_factionData != null && !_factionData.IsHostile(sourceFaction, candidateFaction))
                    {
                        continue;
                    }

                    IReadOnlyList<Vector2Int> majorChunks = _currentFloor.GetChunkCells(candidateFaction, ChunkIntensity.Major);
                    if (majorChunks != null && majorChunks.Count > 0)
                    {
                        BoundsInt chunkBounds = _currentFloor.GetChunkTileBounds(majorChunks[0]);
                        Vector2Int centerTile = new Vector2Int(
                            Mathf.RoundToInt(chunkBounds.center.x),
                            Mathf.RoundToInt(chunkBounds.center.y));
                        return _currentFloor.CellToWorld(centerTile);
                    }
                }
            }

            return bestTarget;
        }

        private FactionSpawnSet GetSpawnSet(Faction faction)
        {
            for (int i = 0; i < _factionSets.Count; i++)
            {
                if (_factionSets[i].Faction == faction)
                {
                    return _factionSets[i];
                }
            }

            return null;
        }
    }
}
