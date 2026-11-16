using UnityEngine;
using System.Collections.Generic;
using System;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    public enum AIState
    {
        Wander,
        Chase,
        Attack,
        Flee,
        Return,
        Raid,
        Investigate
    }

    public enum UnitRole
    {
        Resident,
        Guard
    }


    /// <summary>
    /// Modular finite-state machine controlling faction AI perception, stateless A* pathing,
    /// role leashing, raid execution, and Core CombatRules telegraphed attacks.
    /// </summary>
    [RequireComponent(typeof(CombatAgent2D))]
    public class FactionAI2D : MonoBehaviour
    {
        private const float ScanIntervalSeconds = 0.2f;
        private const int MaxScanColliders = 24;
        private const float RaidArrivalDistance = 2f;

        private Color _baseSpriteColor = Color.white;
        private float _backstepTimer;
        private Vector2 _backstepVelocity;

        [Header("Diplomacy & Masks")]
        [SerializeField] private FactionData _factionData;
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Role & Territory Leash")]
        [SerializeField] private UnitRole _role = UnitRole.Resident;
        [SerializeField] private float _residentLeashDistance = 10f;
        [SerializeField] private float _guardLeashDistance = 4.5f;

        [Header("Pathing & Corner Avoidance")]
        [SerializeField] private float _waypointTolerance = 0.35f;
        [SerializeField] private float _repathThresholdDistance = 2.5f;
        [SerializeField] private float _repathCooldown = 0.4f;
        [SerializeField] private float _cornerAvoidanceRadius = 0.5f;

        [Header("Telegraphed Combat")]
        [SerializeField, Range(0.3f, 1.5f)] private float _attackTelegraphDuration = 0.65f;
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField, Range(0f, 0.5f)] private float _fleeHealthThreshold = 0.2f;

        [Header("Ranged Telegraph Visual")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private GameObject _windupWarningCirclePrefab;

        private Vector2 _lockedAimPosition;
        private GameObject _activeWindupCircle;
        private Vector2 _investigatePosition;
        private float _investigateTimer;

        private CombatAgent2D _agent;
        private AIState _currentState = AIState.Wander;
        private Health _currentTarget;

        private Vector2 _spawnAnchor;
        private Vector2 _rivalTerritoryOrigin;
        private Vector2 _wanderDestination;
        private bool _hasSpawnAnchor;
        private bool _isRaiding;

        private List<Vector2> _waypoints = new List<Vector2>();
        private int _waypointIndex;
        private Vector2 _lastPathTargetPos = new Vector2(float.MaxValue, float.MaxValue);
        private float _repathTimer;
        private float _scanTimer;
        private float _cohesionTimer;
        private Vector2 _cachedCohesion;

        private readonly Collider2D[] _scanBuffer = new Collider2D[MaxScanColliders];
        private ContactFilter2D _targetFilter;

        private float _nextAttackTime;
        private float _telegraphTimer;
        private bool _isTelegraphing;
        private Vector2 _desiredVelocity;

        private IFloorQuery _floor;
        private Func<Vector2Int, bool> _floorWalkableQuery;

        public AIState CurrentState => _currentState;
        public UnitRole Role => _role;
        public bool IsAlive => _agent != null && _agent.IsAlive;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent2D>();
            _targetFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _targetMask,
                useTriggers = true
            };
        }

        private void Start()
        {
            _cohesionTimer = UnityEngine.Random.Range(0f, 0.25f);

            if (!_hasSpawnAnchor)
            {
                _spawnAnchor = transform.position;
                _hasSpawnAnchor = true;
            }

            if (_agent.VisualRenderer != null)
            {
                _baseSpriteColor = _agent.VisualRenderer.color;
            }

            // Stagger initial scan timers
            _scanTimer = UnityEngine.Random.Range(0f, ScanIntervalSeconds);
            PickNewWanderDestination();
        }

        private void OnEnable()
        {
            if (_agent != null)
            {
                _agent.OnAttackedBy += HandleAttackedBy;
            }

            NoiseEventBus.OnNoiseEmitted += HandleNoiseHeard;
        }

        private void OnDisable()
        {
            if (_agent != null)
            {
                _agent.OnAttackedBy -= HandleAttackedBy;
            }

            NoiseEventBus.OnNoiseEmitted -= HandleNoiseHeard;
            ClearRangedTelegraphs();
        }

        private void HandleNoiseHeard(Vector2 noisePos, float soundRadius)
        {
            if (!IsAlive || _agent.Stats == null || _agent.Stats.HearingRadius <= 0f)
            {
                return;
            }
            if (_currentState == AIState.Attack || _currentState == AIState.Chase || _currentState == AIState.Flee)
            {
                return;
            }

            float maxListenDist = Mathf.Min(_agent.Stats.HearingRadius, soundRadius);
            if (Vector2.Distance(transform.position, noisePos) <= maxListenDist)
            {
                _investigatePosition = noisePos;
                _investigateTimer = 4.0f;
                TransitionToState(AIState.Investigate);
                ForceRepath(_investigatePosition);
            }
        }

        public void ConfigureRole(
            UnitRole role,
            Vector2 spawnAnchor,
            LayerMask obstacleMask,
            FactionData factionData = null,
            Vector2 rivalTerritory = default)
        {
            _role = role;
            _spawnAnchor = spawnAnchor;
            _hasSpawnAnchor = true;
            _obstacleMask = obstacleMask;

            if (factionData != null)
            {
                _factionData = factionData;
            }

            _rivalTerritoryOrigin = rivalTerritory;
            PickNewWanderDestination();
        }

        public void SetFloorQuery(IFloorQuery floor)
        {
            _floor = floor;
            if (_floor != null)
            {
                _floorWalkableQuery = _floor.IsWalkable;
            }
        }

        public void SetFloorWalkableQuery(Func<Vector2Int, bool> walkableQuery)
        {
            _floorWalkableQuery = walkableQuery;
        }

        public void TriggerRaidState(Vector2 raidTargetPosition)
        {
            if (!IsAlive)
            {
                return;
            }

            _rivalTerritoryOrigin = raidTargetPosition;
            _isRaiding = true;
            _isTelegraphing = false;
            TransitionToState(AIState.Raid);
            ForceRepath(raidTargetPosition);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                _desiredVelocity = Vector2.zero;
                return;
            }

            _repathTimer -= Time.deltaTime;
            _scanTimer -= Time.deltaTime;

            EvaluateStateTransitions();
            ExecuteStateLogic();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || _agent.Rigidbody == null)
            {
                return;
            }

            if (_backstepTimer > 0f)
            {
                _backstepTimer -= Time.fixedDeltaTime;
                _agent.Rigidbody.linearVelocity = _backstepVelocity;
                return;
            }

            if (_isTelegraphing)
            {
                _agent.Rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            float tileSpeedMultiplier = 1f;
            if (_floor != null)
            {
                Vector2Int currentCell = _floor.WorldToCell(transform.position);
                tileSpeedMultiplier = _floor.GetMoveSpeedMultiplier(currentCell);
            }

            _agent.Rigidbody.linearVelocity = _desiredVelocity * tileSpeedMultiplier;
        }

        private void EvaluateStateTransitions()
        {
            float healthRatio = _agent.MaxHealth > 0 ? (float)_agent.CurrentHealth / _agent.MaxHealth : 1f;

            // 1. Low-health flee check (only when there is something to flee from)
            bool hasLiveTarget = _currentTarget != null && _currentTarget.IsAlive;
            if (_currentState != AIState.Flee && _currentState != AIState.Raid && healthRatio <= _fleeHealthThreshold && hasLiveTarget)
            {
                _isTelegraphing = false;
                ClearRangedTelegraphs();
                TransitionToState(AIState.Flee);
                return;
            }

            // 1b. While fleeing, skip the other rules until the threat is gone, then head home.
            // (Without this, step 5 switched a fleeing unit back to Chase every frame.)
            if (_currentState == AIState.Flee)
            {
                float fleeSightRadius = _agent.Stats != null ? _agent.Stats.SightRadius : 6f;
                bool threatGone = !hasLiveTarget ||
                    Vector2.Distance(transform.position, _currentTarget.transform.position) > fleeSightRadius * 1.3f;

                if (threatGone)
                {
                    _currentTarget = null;
                    TransitionToState(AIState.Return);
                    ForceRepath(_spawnAnchor);
                }

                return;
            }

            // 2. Territory leash check (raiders are not leashed until their raid ends)
            if (!_isRaiding && (_currentState == AIState.Wander || _currentState == AIState.Chase))
            {
                float maxLeash = _role == UnitRole.Guard ? _guardLeashDistance : _residentLeashDistance;
                if (Vector2.Distance(transform.position, _spawnAnchor) > maxLeash)
                {
                    _currentTarget = null;
                    _isTelegraphing = false;
                    ClearRangedTelegraphs();
                    TransitionToState(AIState.Return);
                    ForceRepath(_spawnAnchor);
                    return;
                }
            }

            // 3. Return completion with 60% hysteresis buffer to prevent leash edge freezing
            if (_currentState == AIState.Return)
            {
                float maxLeash = _role == UnitRole.Guard ? _guardLeashDistance : _residentLeashDistance;
                if (Vector2.Distance(transform.position, _spawnAnchor) <= maxLeash * 0.6f)
                {
                    TransitionToState(AIState.Wander);
                    PickNewWanderDestination();
                }

                return;
            }

            // 4. Target acquisition & line-of-sight check
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                if (_scanTimer <= 0f)
                {
                    _scanTimer = ScanIntervalSeconds;
                    _currentTarget = ScanForHostileTarget();
                }
            }
            else if (!HasLineOfSight(_currentTarget.transform.position))
            {
                float sightRadius = _agent.Stats != null ? _agent.Stats.SightRadius : 6f;
                if (Vector2.Distance(transform.position, _currentTarget.transform.position) > sightRadius * 1.3f)
                {
                    _currentTarget = null;
                }
            }

            // 5. Combat vs Patrol/Raid state routing
            if (_currentTarget != null && _currentTarget.IsAlive)
            {
                float attackRange = _agent.Stats != null ? _agent.Stats.AttackRange : 1.2f;
                float distToTarget = Vector2.Distance(transform.position, _currentTarget.transform.position);

                if (distToTarget <= attackRange)
                {
                    if (_currentState != AIState.Attack)
                    {
                        TransitionToState(AIState.Attack);
                    }
                }
                else if (_currentState != AIState.Chase && !_isTelegraphing)
                {
                    TransitionToState(AIState.Chase);
                }
            }
            else
            {
                _isTelegraphing = false;
                ClearRangedTelegraphs();
                if (_currentState == AIState.Chase || _currentState == AIState.Attack)
                {
                    // Only units drafted by the raid manager go back to raiding; everyone else patrols.
                    TransitionToState(_isRaiding ? AIState.Raid : AIState.Wander);
                }
            }
        }

        private void ExecuteStateLogic()
        {
            float moveSpeed = _agent.Stats != null ? _agent.Stats.MoveSpeed : 3.5f;

            switch (_currentState)
            {
                case AIState.Wander:
                    if (Vector2.Distance(transform.position, _wanderDestination) <= 0.6f)
                    {
                        PickNewWanderDestination();
                    }

                    UpdatePathfinding(_wanderDestination);
                    _desiredVelocity = ComputeSmoothSteering() * (moveSpeed * 0.65f);
                    break;

                case AIState.Investigate:
                    _investigateTimer -= Time.deltaTime;
                    UpdatePathfinding(_investigatePosition);
                    _desiredVelocity = ComputeSmoothSteering() * (moveSpeed * 0.85f);

                    if (_investigateTimer <= 0f || Vector2.Distance(transform.position, _investigatePosition) <= 0.8f)
                    {
                        TransitionToState(AIState.Wander);
                        PickNewWanderDestination();
                    }
                    break;

                case AIState.Raid:
                    Vector2 raidDest = _rivalTerritoryOrigin != Vector2.zero ? _rivalTerritoryOrigin : _wanderDestination;

                    // Raid ends on arrival: go home (Return) instead of staying in enemy territory forever.
                    if (Vector2.Distance(transform.position, raidDest) <= RaidArrivalDistance)
                    {
                        EndRaid();
                        break;
                    }

                    UpdatePathfinding(raidDest);
                    _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                    break;

                case AIState.Return:
                    UpdatePathfinding(_spawnAnchor);
                    _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                    break;

                case AIState.Chase:
                    if (_currentTarget != null)
                    {
                        float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);
                        if (_agent.Stats != null && _agent.Stats.IsRangedKiter && dist < _agent.Stats.MinKeepDistance)
                        {
                            Vector2 retreatDir = ((Vector2)transform.position - (Vector2)_currentTarget.transform.position).normalized;
                            Vector2 kiteTarget = (Vector2)transform.position + retreatDir * 3f;
                            UpdatePathfinding(kiteTarget);
                            _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                        }
                        else
                        {
                            UpdatePathfinding(_currentTarget.transform.position);
                            _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                        }
                    }
                    break;

                case AIState.Attack:
                    if (_currentTarget != null && !_isTelegraphing && _agent.Stats != null && _agent.Stats.IsRangedKiter)
                    {
                        float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);
                        if (dist < _agent.Stats.MinKeepDistance)
                        {
                            Vector2 retreatDir = ((Vector2)transform.position - (Vector2)_currentTarget.transform.position).normalized;
                            Vector2 kiteTarget = (Vector2)transform.position + retreatDir * 3f;
                            UpdatePathfinding(kiteTarget);
                            _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                            HandleTelegraphedAttack();
                            break;
                        }
                    }

                    _desiredVelocity = Vector2.zero;
                    HandleTelegraphedAttack();
                    break;

                case AIState.Flee:
                    Vector2 threatPos = _currentTarget != null ? (Vector2)_currentTarget.transform.position : _rivalTerritoryOrigin;
                    Vector2 fleeDir = ((Vector2)transform.position - threatPos).normalized;
                    Vector2 fleeDest = (Vector2)transform.position + fleeDir * 5f;
                    UpdatePathfinding(fleeDest);
                    _desiredVelocity = ComputeSmoothSteering() * (moveSpeed * 1.15f);
                    break;
            }
        }

        private void HandleTelegraphedAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _isTelegraphing = false;
                ClearRangedTelegraphs();
                if (_agent.VisualRenderer != null)
                {
                    _agent.VisualRenderer.color = _baseSpriteColor;
                }
                _currentState = AIState.Wander;
                return;
            }

            float attackRange = _agent.Stats != null ? _agent.Stats.AttackRange : 1.1f;
            float windupDuration = _agent.Stats != null ? _agent.Stats.AttackWindupSeconds : 0.65f;
            float cooldown = _agent.Stats != null ? _agent.Stats.AttackCooldownSeconds : 1.2f;
            bool isRanged = _agent.Stats != null && _agent.Stats.IsRangedKiter && _agent.Stats.ProjectilePrefab != null;

            Vector2 toTarget = (Vector2)_currentTarget.transform.position - (Vector2)transform.position;
            float dist = toTarget.magnitude;

            if (!_isTelegraphing)
            {
                if (dist > attackRange * 1.15f)
                {
                    _currentState = AIState.Chase;
                    return;
                }

                // If ranged kiter is still retreating from a close enemy, don't freeze until ready to fire
                if (Time.time >= _nextAttackTime)
                {
                    _agent.Rigidbody.linearVelocity = Vector2.zero;
                    _isTelegraphing = true;
                    _telegraphTimer = windupDuration;

                    if (TryGetComponent(out UnitAnimator2D unitAnim))
                    {
                        unitAnim.TriggerAttackAnimation();
                    }

                    if (_agent.VisualRenderer != null)
                    {
                        _agent.VisualRenderer.color = new Color(1f, 0.65f, 0.2f);
                    }

                    // 1. Lock aim position & spawn ranged telegraph visuals at wind-up start
                    if (isRanged)
                    {
                        _lockedAimPosition = _currentTarget.transform.position;

                        if (_agent.Stats.ShowTrajectoryTelegraph && _trajectoryLine != null)
                        {
                            _trajectoryLine.enabled = true;
                            _trajectoryLine.positionCount = 2;
                            _trajectoryLine.SetPosition(0, transform.position);
                            _trajectoryLine.SetPosition(1, _lockedAimPosition);
                        }

                        if (_agent.Stats.ShowGroundWarningCircle && _windupWarningCirclePrefab != null)
                        {
                            _activeWindupCircle = Instantiate(_windupWarningCirclePrefab, _lockedAimPosition, Quaternion.identity);
                            float diameter = Mathf.Max(0.45f, _agent.Stats.AoERadius) * 2f;
                            _activeWindupCircle.transform.localScale = new Vector3(diameter, diameter, 1f);
                        }
                    }
                }
                return;
            }

            // Winding up attack
            _agent.Rigidbody.linearVelocity = Vector2.zero;
            _telegraphTimer -= Time.deltaTime;

            // Keep line start anchored to unit in case of slight physics push
            if (isRanged && _trajectoryLine != null && _trajectoryLine.enabled)
            {
                _trajectoryLine.SetPosition(0, transform.position);
            }

            if (_telegraphTimer <= 0f)
            {
                _isTelegraphing = false;
                _nextAttackTime = Time.time + cooldown;
                ClearRangedTelegraphs();

                if (_agent.VisualRenderer != null)
                {
                    _agent.VisualRenderer.color = _baseSpriteColor;
                }

                if (isRanged)
                {
                    // Fire slow, telegraphed projectile toward the locked aim point
                    GameObject projObj = Instantiate(_agent.Stats.ProjectilePrefab, transform.position, Quaternion.identity);
                    if (projObj.TryGetComponent(out TelegraphedProjectile2D proj))
                    {
                        proj.Initialize(
                            transform.position,
                            _lockedAimPosition,
                            gameObject,
                            _agent.Faction,
                            _agent.Stats,
                            _targetMask);
                    }

                    NoiseEventBus.EmitNoise(transform.position, 6.5f);
                }
                else if (dist <= attackRange * 1.35f)
                {
                    ExecuteCoreCombatAttack();

                    // Hit-and-run jump away (Spider Skitterling)
                    float backstepDist = _agent.Stats != null ? _agent.Stats.PostAttackBackstepDistance : 0f;
                    float backstepDur = _agent.Stats != null ? _agent.Stats.PostAttackBackstepDuration : 0.25f;
                    if (backstepDist > 0.01f && backstepDur > 0.01f)
                    {
                        Vector2 awayDir = dist > 0.01f ? -toTarget.normalized : -((Vector2)transform.up);
                        _backstepVelocity = awayDir * (backstepDist / backstepDur);
                        _backstepTimer = backstepDur;
                    }
                }
            }
        }

        private void ClearRangedTelegraphs()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }

            if (_activeWindupCircle != null)
            {
                Destroy(_activeWindupCircle);
                _activeWindupCircle = null;
            }
        }

        private void ExecuteCoreCombatAttack()
        {
            if (_agent.VisualRenderer != null)
            {
                _agent.VisualRenderer.color = _baseSpriteColor;
            }

            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                return;
            }

            float attackRange = _agent.Stats != null ? _agent.Stats.AttackRange : 1.2f;
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            if (dist > attackRange * 1.25f)
            {
                return;
            }

            int accuracy = _agent.Stats != null ? _agent.Stats.Accuracy : CombatRules.BaseAccuracy;
            HitResult hitResult = CombatRules.RollHit(accuracy);

            if (hitResult == HitResult.Miss)
            {
                return;
            }

            int baseDamage = _agent.Stats != null ? _agent.Stats.AttackDamage : 8;
            int finalDamage = CombatRules.GetDamage(baseDamage, hitResult);
            DamageType damageType = _agent.Stats != null ? _agent.Stats.DamageType : DamageType.Physical;
            bool isCritical = hitResult == HitResult.Critical;
            

            if (_currentTarget.TryGetComponent(out CombatAgent2D targetAgent))
            {
                float knockback = _agent.Stats != null ? _agent.Stats.KnockbackForce : 5f;
                Vector2 hitDir = ((Vector2)_currentTarget.transform.position - (Vector2)transform.position).normalized;
                targetAgent.QueueKnockback(hitDir, knockback);
            }

            DamageInfo damageInfo = new DamageInfo(finalDamage, damageType, gameObject, _agent.Faction, isCritical);
            _currentTarget.TakeDamage(damageInfo);

            NoiseEventBus.EmitNoise(transform.position, 7.5f);
        }

        private void HandleAttackedBy(GameObject attackerObj)
        {
            if (!IsAlive || attackerObj == null)
            {
                return;
            }

            Health attackerHealth = attackerObj.GetComponentInParent<Health>();
            if (attackerHealth == null || !attackerHealth.IsAlive)
            {
                return;
            }

            if (attackerHealth.Faction == _agent.Faction)
            {
                return;
            }

            if (_currentTarget == null || attackerHealth.Faction == Faction.Player)
            {
                _currentTarget = attackerHealth;
                if (_currentState != AIState.Attack && _currentState != AIState.Flee)
                {
                    TransitionToState(AIState.Chase);
                    ForceRepath(attackerHealth.transform.position);
                }
            }
        }

        private Health ScanForHostileTarget()
        {
            float sightRadius = _agent.Stats != null ? _agent.Stats.SightRadius : 6f;
            _targetFilter.useLayerMask = _targetMask.value != 0;
            _targetFilter.layerMask = _targetMask;

            int hitCount = Physics2D.OverlapCircle(transform.position, sightRadius, _targetFilter, _scanBuffer);

            Health bestTarget = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _scanBuffer[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }

                Health candidate = col.GetComponentInParent<Health>();
                if (candidate == null || candidate == _agent.Health || !candidate.IsAlive)
                {
                    continue;
                }

                if (!IsHostileTo(candidate.Faction))
                {
                    continue;
                }

                float dist = Vector2.Distance(transform.position, candidate.transform.position);
                if (dist < closestDist && HasLineOfSight(candidate.transform.position))
                {
                    closestDist = dist;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool IsHostileTo(Faction otherFaction)
        {
            if (_factionData != null)
            {
                return _factionData.IsHostile(_agent.Faction, otherFaction);
            }

            if (_agent.Faction == otherFaction)
            {
                return false;
            }

            return _agent.Faction == Faction.Player || otherFaction == Faction.Player ||
                   (_agent.Faction == Faction.Goblin && otherFaction == Faction.Spider) ||
                   (_agent.Faction == Faction.Spider && otherFaction == Faction.Goblin);
        }

        private bool HasLineOfSight(Vector2 targetPos)
        {
            Vector2 origin = transform.position;
            Vector2 dir = targetPos - origin;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dir.magnitude, _obstacleMask);
            if (hit.collider == null)
            {
                return true;
            }

            // Allow targeting destructible spawners/structures even if they sit on the Walls layer
            return _currentTarget != null && hit.collider.transform.IsChildOf(_currentTarget.transform);
        }

        private void UpdatePathfinding(Vector2 destination)
        {
            float targetDelta = Vector2.Distance(destination, _lastPathTargetPos);

            if ((targetDelta >= _repathThresholdDistance || _waypoints == null || _waypointIndex >= _waypoints.Count) && _repathTimer <= 0f)
            {
                ForceRepath(destination);
            }
        }

        private void ForceRepath(Vector2 destination)
        {
            _repathTimer = _repathCooldown;
            _lastPathTargetPos = destination;
            _waypoints = Pathfinding2D.FindPath(transform.position, destination, _obstacleMask, _floorWalkableQuery);
            _waypointIndex = 0;
        }

        private Vector2 ComputeSmoothSteering()
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Count)
            {
                return Vector2.zero;
            }

            Vector2 currentPos = transform.position;
            Vector2 nextWaypoint = _waypoints[_waypointIndex];

            if (Vector2.Distance(currentPos, nextWaypoint) <= _waypointTolerance)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Count)
                {
                    return Vector2.zero;
                }

                nextWaypoint = _waypoints[_waypointIndex];
            }

            Vector2 desiredHeading = (nextWaypoint - currentPos).normalized;

            // Blend ally pack cohesion during Wander/Chase (Goblin squad behavior)
            if (_currentState == AIState.Wander || _currentState == AIState.Chase)
            {
                Vector2 cohesion = ComputePackCohesionSteering();
                if (cohesion != Vector2.zero)
                {
                    desiredHeading = (desiredHeading + cohesion).normalized;
                }
            }

            Collider2D nearbyWall = Physics2D.OverlapCircle(currentPos, _cornerAvoidanceRadius, _obstacleMask);
            if (nearbyWall != null)
            {
                Vector2 closestWallPoint = nearbyWall.ClosestPoint(currentPos);
                Vector2 pushAway = (currentPos - closestWallPoint).normalized;
                desiredHeading = (desiredHeading + pushAway * 0.45f).normalized;
            }

            return desiredHeading;
        }

        private Vector2 ComputePackCohesionSteering()
        {
            if (_agent.Stats == null || _agent.Stats.PackCohesionWeight <= 0f)
            {
                return Vector2.zero;
            }

            _cohesionTimer = 0.25f;
            float radius = _agent.Stats.PackCohesionRadius;
            _targetFilter.useLayerMask = _targetMask.value != 0;
            _targetFilter.layerMask = _targetMask;

            int hitCount = Physics2D.OverlapCircle(transform.position, radius, _targetFilter, _scanBuffer);

            Vector2 centerOfMass = Vector2.zero;
            int allyCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _scanBuffer[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }

                Health ally = col.GetComponentInParent<Health>();
                if (ally != null && ally.IsAlive && ally.Faction == _agent.Faction)
                {
                    centerOfMass += (Vector2)ally.transform.position;
                    allyCount++;
                }
            }

            if (allyCount == 0)
            {
                _cachedCohesion = Vector2.zero;
                return _cachedCohesion;
            }

            centerOfMass /= allyCount;
            Vector2 toPack = centerOfMass - (Vector2)transform.position;
            

            _cachedCohesion = toPack.magnitude < 1.4f
                ? Vector2.zero
                : toPack.normalized * _agent.Stats.PackCohesionWeight;

            return _cachedCohesion;
        }

        private void PickNewWanderDestination()
        {
            float wanderRadius = _role == UnitRole.Guard ? _guardLeashDistance * 0.7f : _residentLeashDistance * 0.7f;
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * wanderRadius;
            _wanderDestination = _spawnAnchor + randomOffset;
        }

        private void EndRaid()
        {
            _isRaiding = false;
            _desiredVelocity = Vector2.zero;
            TransitionToState(AIState.Return);
            ForceRepath(_spawnAnchor);
        }

        private void TransitionToState(AIState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _currentState = newState;

            if (_agent != null && _agent.VisualRenderer != null && !_isTelegraphing)
            {
                _agent.VisualRenderer.color = Color.white;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_agent == null)
            {
                _agent = GetComponent<CombatAgent2D>();
            }

            Vector3 currentPos = transform.position;

            if (_agent != null && _agent.Stats != null)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
                Gizmos.DrawWireSphere(currentPos, _agent.Stats.SightRadius);
            }

            if (_currentTarget != null && _currentTarget.IsAlive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currentPos, _currentTarget.transform.position);
            }

            if (_waypoints != null && _waypoints.Count > 0)
            {
                Gizmos.color = Color.green;
                Vector3 lastNode = currentPos;

                for (int i = _waypointIndex; i < _waypoints.Count; i++)
                {
                    Vector3 nextNode = _waypoints[i];
                    Gizmos.DrawLine(lastNode, nextNode);
                    Gizmos.DrawWireSphere(nextNode, 0.14f);
                    lastNode = nextNode;
                }
            }

            string targetName = _currentTarget != null ? _currentTarget.name : "None";
            string stateLabel = $"[{_role} | {_currentState}]\nTarget: {targetName}";

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = (_factionData != null && _agent != null)
                ? _factionData.GetDebugColor(_agent.Faction)
                : Color.white;
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            UnityEditor.Handles.Label(currentPos + Vector3.up * 0.9f, stateLabel, labelStyle);
        }
#endif
    }
}
