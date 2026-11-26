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
        public GameObject SpecialistPrefab;
        public UnitConfig ResidentConfig;
        public UnitConfig GuardConfig;
        public UnitConfig SpecialistConfig; 
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
        [HideInInspector] public float RallyElapsed;
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

        [Header("Generation Specialists & Neutral Encounters")]
        [SerializeField, Range(0f, 1f)] private float _generationSpecialistChance = 0.3f;
        [SerializeField, Min(0)] private int _extraNeutralEncounters = 6;
        [SerializeField, Min(1f)] private float _minNeutralSpacing = 4.5f;

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
                _raidSettings[i].RallyElapsed = 0f;
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
            if (spawnPoints != null)
            {
                // 1. Place faction spawners on verified walkable tiles
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

                    Vector3 safeSpawnerPos = FindNearestWalkablePosition(sp.Position, 5);
                    GameObject spawnerObj = Instantiate(set.SpawnerPrefab, safeSpawnerPos, Quaternion.identity);
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

                // 2. Place standalone enemies on verified walkable tiles (including specialists)
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

                    Vector3 safeSpawnPos = FindNearestWalkablePosition(sp.Position, 5);
                    Vector2Int tileCell = floor.WorldToCell(safeSpawnPos);
                    if (!floor.IsWalkable(tileCell))
                    {
                        continue;
                    }

                    ChunkIntensity intensity = floor.GetIntensityAt(tileCell);
                    UnitRole role = intensity == ChunkIntensity.Minor ? UnitRole.Guard : UnitRole.Resident;

                    if (!CanSpawnForFaction(sp.Faction, role))
                    {
                        continue;
                    }

                    GameObject prefab = role == UnitRole.Guard ? set.GuardPrefab : set.ResidentPrefab;
                    UnitConfig config = role == UnitRole.Guard ? set.GuardConfig : set.ResidentConfig;

                    if (role == UnitRole.Resident && set.SpecialistPrefab != null && UnityEngine.Random.value < _generationSpecialistChance)
                    {
                        prefab = set.SpecialistPrefab;
                        if (set.SpecialistConfig != null)
                        {
                            config = set.SpecialistConfig;
                        }
                    }

                    if (prefab == null)
                    {
                        continue;
                    }

                    GameObject unitObj = Instantiate(prefab, safeSpawnPos, Quaternion.identity);

                    CombatAgent2D combat = unitObj.GetComponent<CombatAgent2D>();
                    if (combat != null && config != null)
                    {
                        combat.Initialize(config);
                    }

                    FactionAI2D ai = unitObj.GetComponent<FactionAI2D>();
                    if (ai != null)
                    {
                        ai.SetFloorQuery(floor);
                        Vector2 rivalPos = FindRivalTargetPosition(sp.Faction, safeSpawnPos);
                        ai.ConfigureRole(role, safeSpawnPos, _obstacleMask, _factionData, rivalPos);
                        RegisterSpawnedUnit(sp.Faction, ai);
                    }
                }
            }

            // 3. Populate extra neutral encounters on walkable neutral chunks
            SpawnNeutralEncounters(floor);
        }

        private void SpawnNeutralEncounters(IFloorQuery floor)
        {
            if (_extraNeutralEncounters <= 0)
            {
                return;
            }

            // Gather all active playable/enemy faction sets (excluding Neutral) so units count toward faction caps
            List<FactionSpawnSet> activeSets = new List<FactionSpawnSet>();
            for (int i = 0; i < _factionSets.Count; i++)
            {
                if (_factionSets[i].Faction != Faction.Neutral && _factionSets[i].ResidentPrefab != null)
                {
                    activeSets.Add(_factionSets[i]);
                }
            }

            if (activeSets.Count == 0)
            {
                return;
            }

            List<Vector2Int> candidateChunks = new List<Vector2Int>();
            IReadOnlyList<Vector2Int> minorNeutral = floor.GetChunkCells(Faction.Neutral, ChunkIntensity.Minor);
            IReadOnlyList<Vector2Int> majorNeutral = floor.GetChunkCells(Faction.Neutral, ChunkIntensity.Major);

            if (minorNeutral != null)
            {
                candidateChunks.AddRange(minorNeutral);
            }

            if (majorNeutral != null)
            {
                candidateChunks.AddRange(majorNeutral);
            }

            if (candidateChunks.Count == 0)
            {
                return;
            }

            List<Vector3> placedPositions = new List<Vector3>();
            int spawned = 0;

            for (int attempt = 0; attempt < _extraNeutralEncounters * 8 && spawned < _extraNeutralEncounters; attempt++)
            {
                // Pick a random faction that still has open Resident capacity under _maxResidentsPerFaction
                FactionSpawnSet chosenSet = activeSets[UnityEngine.Random.Range(0, activeSets.Count)];
                if (!CanSpawnForFaction(chosenSet.Faction, UnitRole.Resident))
                {
                    continue;
                }

                Vector2Int chunkCell = candidateChunks[UnityEngine.Random.Range(0, candidateChunks.Count)];
                BoundsInt bounds = floor.GetChunkTileBounds(chunkCell);
                Vector3 rawPos = GetWalkableChunkCenter(bounds);
                Vector3 safePos = FindNearestWalkablePosition(rawPos, 5);

                if (!floor.IsWalkable(floor.WorldToCell(safePos)))
                {
                    continue;
                }

                bool tooClose = false;
                for (int p = 0; p < placedPositions.Count; p++)
                {
                    if (Vector3.Distance(safePos, placedPositions[p]) < _minNeutralSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                GameObject prefab = (chosenSet.SpecialistPrefab != null && UnityEngine.Random.value < _generationSpecialistChance)
                    ? chosenSet.SpecialistPrefab
                    : chosenSet.ResidentPrefab;
                UnitConfig config = (prefab == chosenSet.SpecialistPrefab && chosenSet.SpecialistConfig != null)
                    ? chosenSet.SpecialistConfig
                    : chosenSet.ResidentConfig;

                GameObject unitObj = Instantiate(prefab, safePos, Quaternion.identity);
                if (unitObj.TryGetComponent(out CombatAgent2D combat) && config != null)
                {
                    combat.Initialize(config);
                }

                if (unitObj.TryGetComponent(out FactionAI2D ai))
                {
                    ai.SetFloorQuery(floor);
                    ai.ConfigureRole(UnitRole.Resident, safePos, _obstacleMask, _factionData, Vector2.zero);
                    RegisterSpawnedUnit(Faction.Neutral, ai);
                }

                placedPositions.Add(safePos);
                spawned++;
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
                    cfg.Timer = 3f;
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
                cfg.ActiveRallyPoint = FindNearestWalkablePosition(rawRally, 4);

                cfg.RaidSquad.Clear();

                for (int i = 0; i < cfg.MinResidentsToRally; i++)
                {
                    FactionAI2D unit = availableResidents[i];
                    unit.BeginRally(cfg.ActiveRallyPoint);
                    cfg.RaidSquad.Add(unit);
                }

                cfg.RallyElapsed = 0f;
                cfg.IsRallying = true;
                return;
            }

            if (cfg.RaidSquad.Count < 2)
            {
                AbortAndReleaseSquad(cfg);
                return;
            }

            cfg.RallyElapsed += Time.deltaTime;
            bool timedOut = cfg.RallyElapsed >= 8f;

            List<FactionAI2D> assembledUnits = new List<FactionAI2D>();
            for (int i = 0; i < cfg.RaidSquad.Count; i++)
            {
                if (cfg.RaidSquad[i].IsAtRallyPoint(2.25f))
                {
                    assembledUnits.Add(cfg.RaidSquad[i]);
                }
            }

            bool allAssembled = assembledUnits.Count == cfg.RaidSquad.Count;

            if (allAssembled || timedOut)
            {
                if (assembledUnits.Count >= 2)
                {
                    for (int i = 0; i < cfg.RaidSquad.Count; i++)
                    {
                        if (!assembledUnits.Contains(cfg.RaidSquad[i]))
                        {
                            cfg.RaidSquad[i].CancelRally();
                        }
                    }

                    FactionSpawner2D homeSpawner = GetActiveSpawner(cfg.Faction);
                    Vector2 origin = homeSpawner != null ? (Vector2)homeSpawner.transform.position : cfg.ActiveRallyPoint;
                    Vector3 finalTarget = FindRivalTargetPosition(cfg.Faction, origin);

                    if (finalTarget != Vector3.zero)
                    {
                        PlayRaidCueSound(cfg, origin);

                        for (int i = 0; i < assembledUnits.Count; i++)
                        {
                            assembledUnits[i].TriggerRaidState(finalTarget);
                        }

                        cfg.RaidSquad.Clear();
                        cfg.IsRallying = false;
                        cfg.Timer = cfg.RaidIntervalSeconds;
                    }
                    else
                    {
                        AbortAndReleaseSquad(cfg);
                    }
                }
                else
                {
                    AbortAndReleaseSquad(cfg);
                }
            }
        }

        private void AbortAndReleaseSquad(FactionRaidSettings cfg)
        {
            for (int i = 0; i < cfg.RaidSquad.Count; i++)
            {
                if (cfg.RaidSquad[i] != null && cfg.RaidSquad[i].IsAlive)
                {
                    cfg.RaidSquad[i].CancelRally();
                }
            }

            cfg.RaidSquad.Clear();
            cfg.RallyElapsed = 0f;
            cfg.IsRallying = false;
            cfg.Timer = cfg.RaidIntervalSeconds;
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

        private Vector3 FindNearestWalkablePosition(Vector3 rawPos, int maxRadiusTiles = 5)
        {
            if (_currentFloor != null)
            {
                Vector2Int originCell = _currentFloor.WorldToCell(rawPos);
                if (_currentFloor.IsWalkable(originCell))
                {
                    return _currentFloor.CellToWorld(originCell);
                }

                for (int radius = 1; radius <= maxRadiusTiles; radius++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            {
                                continue;
                            }

                            Vector2Int candidate = new Vector2Int(originCell.x + dx, originCell.y + dy);
                            if (_currentFloor.IsWalkable(candidate))
                            {
                                return _currentFloor.CellToWorld(candidate);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Physics2D.OverlapCircle(rawPos, 0.35f, _obstacleMask) == null)
                {
                    return rawPos;
                }

                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI * 2f / 12f;
                    Vector3 candidate = rawPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.1f;
                    if (Physics2D.OverlapCircle(candidate, 0.35f, _obstacleMask) == null)
                    {
                        return candidate;
                    }
                }
            }

            return rawPos;
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
