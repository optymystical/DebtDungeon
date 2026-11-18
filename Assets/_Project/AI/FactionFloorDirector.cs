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

    [Serializable]
    public class FactionRaidSettings
    {
        public Faction Faction;
        [Min(5f)] public float RaidIntervalSeconds = 45f;
        [Min(1)] public int MinResidentsToRally = 4;
        [Min(0.5f)] public float RallyOffsetDistance = 2.2f;
        public AudioClip RaidHornCue;

        [HideInInspector] public float Timer;
        [HideInInspector] public bool IsRallying;
        [HideInInspector] public Vector2 ActiveRallyPoint;
        [HideInInspector] public List<FactionAI2D> RaidSquad = new List<FactionAI2D>();
    }

    /// <summary>
    /// Orchestrates floor-wide enemy placement upon GameEvents.FloorGenerated,
    /// enforces map-wide population caps per faction, and runs per-faction raid timers
    /// (gathering Residents at a rally point, playing a raid sound cue, and targeting nearest enemy Minor/Major chunks).
    /// </summary>
    public class FactionFloorDirector : MonoBehaviour
    {
        [Header("Diplomacy & Masks")]
        [SerializeField] private FactionData _factionData;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Faction Prefab Sets")]
        [SerializeField] private List<FactionSpawnSet> _factionSets = new List<FactionSpawnSet>();

        [Header("Map-Wide Faction Population Caps (8 Total Per Faction)")]
        [SerializeField, Min(1)] private int _maxResidentsPerFaction = 6;
        [SerializeField, Min(1)] private int _maxGuardsPerFaction = 2;

        [Header("Per-Faction Raid Scheduler")]
        [SerializeField]
        private List<FactionRaidSettings> _raidSettings = new List<FactionRaidSettings>
        {
            new FactionRaidSettings { Faction = Faction.Goblin, RaidIntervalSeconds = 45f, MinResidentsToRally = 4 },
            new FactionRaidSettings { Faction = Faction.Spider, RaidIntervalSeconds = 50f, MinResidentsToRally = 4 }
        };

        private readonly Dictionary<Faction, List<FactionAI2D>> _unitsByFaction = new Dictionary<Faction, List<FactionAI2D>>();
        private readonly List<FactionSpawner2D> _activeSpawners = new List<FactionSpawner2D>();

        private IFloorQuery _currentFloor;

        private void Awake()
        {
            ResetAllRaidTimers();
            EnsureFactionBuckets();
        }

        private void Start()
        {
            // Support standalone sandbox scenes where spawners are placed manually in the Hierarchy
            if (_currentFloor == null)
            {
                //Reset Exit Floor state

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

            for (int i = 0; i < _raidSettings.Count; i++)
            {
                UpdateFactionRaid(_raidSettings[i]);
            }
        }

        private void ResetAllRaidTimers()
        {
            for (int i = 0; i < _raidSettings.Count; i++)
            {
                _raidSettings[i].Timer = _raidSettings[i].RaidIntervalSeconds;
                _raidSettings[i].IsRallying = false;
                _raidSettings[i].RaidSquad.Clear();
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
            //Reset Exit Floor state
            ResetAllRaidTimers();

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

        private void UpdateFactionRaid(FactionRaidSettings cfg)
        {
            cfg.RaidSquad.RemoveAll(u => u == null || !u.IsAlive);

            if (!cfg.IsRallying)
            {
                cfg.Timer -= Time.deltaTime;
                if (cfg.Timer > 0f)
                {
                    return;
                }

                List<FactionAI2D> availableResidents = GetAvailableResidents(cfg.Faction);
                if (availableResidents.Count < cfg.MinResidentsToRally)
                {
                    cfg.Timer = 3f; // Retry shortly until at least MinResidentsToRally are alive
                    return;
                }

                FactionSpawner2D homeSpawner = GetActiveSpawner(cfg.Faction);
                Vector2 origin = homeSpawner != null
                    ? (Vector2)homeSpawner.transform.position
                    : (Vector2)availableResidents[0].transform.position;

                Vector3 targetPos = FindRivalTargetPosition(cfg.Faction, origin);
                if (targetPos == Vector3.zero)
                {
                    cfg.Timer = cfg.RaidIntervalSeconds;
                    return;
                }

                Vector2 toTarget = ((Vector2)targetPos - origin).normalized;
                if (toTarget == Vector2.zero)
                {
                    toTarget = Vector2.right;
                }

                Vector2 rawRally = origin + toTarget * cfg.RallyOffsetDistance;
                if (_currentFloor != null)
                {
                    Vector2Int rallyCell = _currentFloor.WorldToCell(rawRally);
                    cfg.ActiveRallyPoint = _currentFloor.IsWalkable(rallyCell)
                        ? (Vector2)_currentFloor.CellToWorld(rallyCell)
                        : origin;
                }
                else
                {
                    cfg.ActiveRallyPoint = rawRally;
                }

                cfg.RaidSquad.Clear();

                for (int i = 0; i < cfg.MinResidentsToRally; i++)
                {
                    FactionAI2D unit = availableResidents[i];
                    unit.BeginRally(cfg.ActiveRallyPoint);
                    cfg.RaidSquad.Add(unit);
                }

                cfg.IsRallying = true;
                return;
            }

            // Abort rally if squad suffers heavy casualties before assembling
            if (cfg.RaidSquad.Count < 2)
            {
                cfg.IsRallying = false;
                cfg.Timer = cfg.RaidIntervalSeconds;
                return;
            }

            bool allAssembled = true;
            for (int i = 0; i < cfg.RaidSquad.Count; i++)
            {
                if (!cfg.RaidSquad[i].IsAtRallyPoint(1.5f))
                {
                    allAssembled = false;
                    break;
                }
            }

            if (allAssembled)
            {
                FactionSpawner2D homeSpawner = GetActiveSpawner(cfg.Faction);
                Vector2 origin = homeSpawner != null ? (Vector2)homeSpawner.transform.position : cfg.ActiveRallyPoint;
                Vector3 finalTarget = FindRivalTargetPosition(cfg.Faction, origin);

                if (finalTarget != Vector3.zero)
                {
                    PlayRaidCueSound(cfg, origin);

                    for (int i = 0; i < cfg.RaidSquad.Count; i++)
                    {
                        cfg.RaidSquad[i].TriggerRaidState(finalTarget);
                    }
                }

                cfg.IsRallying = false;
                cfg.Timer = cfg.RaidIntervalSeconds;
            }
        }

        private void PlayRaidCueSound(FactionRaidSettings cfg, Vector2 origin)
        {
            if (cfg.RaidHornCue != null)
            {
                AudioSource.PlayClipAtPoint(cfg.RaidHornCue, origin);
            }
            else
            {
                Debug.Log($"[RaidCue] {cfg.Faction} war horn sounded at {origin}!");
            }
        }

        private List<FactionAI2D> GetAvailableResidents(Faction faction)
        {
            List<FactionAI2D> result = new List<FactionAI2D>();
            if (!_unitsByFaction.TryGetValue(faction, out List<FactionAI2D> roster))
            {
                return result;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                FactionAI2D unit = roster[i];
                if (unit != null && unit.IsAlive && unit.Role == UnitRole.Resident &&
                    unit.CurrentState != AIState.Raid && unit.CurrentState != AIState.Rally)
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        private FactionSpawner2D GetActiveSpawner(Faction faction)
        {
            for (int i = 0; i < _activeSpawners.Count; i++)
            {
                if (_activeSpawners[i] != null && !_activeSpawners[i].IsDestroyed && _activeSpawners[i].Faction == faction)
                {
                    return _activeSpawners[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Targets the nearest hostile Minor chunk first (if defenders exist),
        /// otherwise falls back to the nearest hostile Major chunk / spawner.
        /// </summary>
        private Vector3 FindRivalTargetPosition(Faction sourceFaction, Vector3 originPos)
        {
            // 1. Check nearest hostile Minor chunk first
            if (_currentFloor != null)
            {
                Vector3 bestMinorTarget = Vector3.zero;
                float bestMinorDist = float.MaxValue;

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

                    IReadOnlyList<Vector2Int> minorChunks = _currentFloor.GetChunkCells(candidateFaction, ChunkIntensity.Minor);
                    if (minorChunks == null)
                    {
                        continue;
                    }

                    for (int c = 0; c < minorChunks.Count; c++)
                    {
                        BoundsInt chunkBounds = _currentFloor.GetChunkTileBounds(minorChunks[c]);
                        Vector3 worldPos = GetWalkableChunkCenter(chunkBounds);

                        // Only target Minor chunks that still have living hostile defenders nearby
                        if (!HasLivingHostilesNear(worldPos, candidateFaction, 6f))
                        {
                            continue;
                        }

                        float dist = Vector3.Distance(originPos, worldPos);
                        if (dist < bestMinorDist)
                        {
                            bestMinorDist = dist;
                            bestMinorTarget = worldPos;
                        }
                    }
                }

                if (bestMinorTarget != Vector3.zero)
                {
                    return bestMinorTarget;
                }
            }

            // 2. Fallback: nearest hostile Major chunk spawner
            Vector3 bestMajorTarget = Vector3.zero;
            float bestMajorDist = float.MaxValue;

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
                if (dist < bestMajorDist)
                {
                    bestMajorDist = dist;
                    bestMajorTarget = candidate.transform.position;
                }
            }

            if (bestMajorTarget == Vector3.zero && _currentFloor != null)
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
                        return GetWalkableChunkCenter(chunkBounds);
                    }
                }
            }

            return bestMajorTarget;
        }

        private Vector3 GetWalkableChunkCenter(BoundsInt chunkBounds)
        {
            if (_currentFloor == null)
            {
                return Vector3.zero;
            }

            Vector2Int centerTile = new Vector2Int(
                Mathf.RoundToInt(chunkBounds.center.x),
                Mathf.RoundToInt(chunkBounds.center.y));

            if (_currentFloor.IsWalkable(centerTile))
            {
                return _currentFloor.CellToWorld(centerTile);
            }

            // Spiral outward up to 6 tiles to find the nearest walkable floor tile inside the chunk
            for (int radius = 1; radius <= 6; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(centerTile.x + dx, centerTile.y + dy);
                        if (chunkBounds.Contains(new Vector3Int(candidate.x, candidate.y, 0)) &&
                            _currentFloor.IsWalkable(candidate))
                        {
                            return _currentFloor.CellToWorld(candidate);
                        }
                    }
                }
            }

            return _currentFloor.CellToWorld(centerTile);
        }

        private bool HasLivingHostilesNear(Vector3 center, Faction targetFaction, float radius)
        {
            if (!_unitsByFaction.TryGetValue(targetFaction, out List<FactionAI2D> roster))
            {
                return false;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] != null && roster[i].IsAlive &&
                    Vector3.Distance(center, roster[i].transform.position) <= radius)
                {
                    return true;
                }
            }

            return false;
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
