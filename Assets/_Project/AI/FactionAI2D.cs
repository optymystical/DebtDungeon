using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DebtDungeon.AI
{
    public enum AIState
    {
        Wander,
        Chase,
        Attack,
        Flee,
        Return,
        Raid
    }

    public enum UnitRole
    {
        Resident,
        Guard
    }


    /// <summary>
    /// Modular finite-state machine controlling faction AI perception, stateless A* pathing,
    /// role leashing, raid execution, and telegraphed combat.
    /// </summary>
    [RequireComponent(typeof(CombatAgent2D))]
    public class FactionAI2D : MonoBehaviour
    {
        [Header("Perception & Combat Masks")]
        [Tooltip("Physics layers containing hostile units (e.g., Player, Goblin, Spider).")]
        [SerializeField] private LayerMask _targetMask;

        [Tooltip("Physics layer containing walls and destructible structures.")]
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
        [Tooltip("Wind-up duration (0.5 - 1.0s) before an attack lands.")]
        [SerializeField, Range(0.3f, 1.5f)] private float _attackTelegraphDuration = 0.65f;
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField, Range(0f, 0.5f)] private float _fleeHealthThreshold = 0.2f;

        private CombatAgent2D _agent;
        private AIState _currentState = AIState.Wander;
        private CombatAgent2D _currentTarget;

        // Territory & Raid Anchors
        private Vector2 _spawnAnchor;
        private Vector2 _rivalTerritoryOrigin;
        private Vector2 _wanderDestination;
        private bool _hasSpawnAnchor;

        // Pathfinding Cache
        private List<Vector2> _waypoints = new List<Vector2>();
        private int _waypointIndex;
        private Vector2 _lastPathTargetPos = new Vector2(float.MaxValue, float.MaxValue);
        private float _repathTimer;

        // Combat & State Timers
        private float _nextAttackTime;
        private float _telegraphTimer;
        private bool _isTelegraphing;
        private Vector2 _desiredVelocity;

        // Optional FloorGen query delegate injected via Core events
        private Func<Vector2Int, bool> _floorWalkableQuery;

        public AIState CurrentState => _currentState;
        public UnitRole Role => _role;
        public bool IsAlive => _agent != null && _agent.IsAlive;

        private void Awake()
        {
            _agent = GetComponent<CombatAgent2D>();
        }

        private void Start()
        {
            if (!_hasSpawnAnchor)
            {
                _spawnAnchor = transform.position;
                _hasSpawnAnchor = true;
            }

            PickNewWanderDestination();
        }

        private void OnEnable()
        {
            if (_agent != null)
            {
                _agent.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_agent != null)
            {
                _agent.OnDamaged -= HandleDamaged;
            }
        }

        /// <summary>
        /// Called by spawners upon instantiation to bind role, territory origin, and masks.
        /// </summary>
        public void ConfigureRole(UnitRole role, Vector2 spawnAnchor, LayerMask obstacleMask, Vector2 rivalTerritory = default)
        {
            _role = role;
            _spawnAnchor = spawnAnchor;
            _hasSpawnAnchor = true;
            _obstacleMask = obstacleMask;
            _rivalTerritoryOrigin = rivalTerritory;
            PickNewWanderDestination();
        }

        /// <summary>
        /// Injects the FloorGen walkability query so A* runs over IFloorQuery without tight coupling.
        /// </summary>
        public void SetFloorWalkableQuery(Func<Vector2Int, bool> walkableQuery)
        {
            _floorWalkableQuery = walkableQuery;
        }

        /// <summary>
        /// Commands this unit to join a faction raid party marching on the target coordinate.
        /// </summary>
        public void TriggerRaidState(Vector2 raidTargetPosition)
        {
            if (!IsAlive)
            {
                return;
            }

            _rivalTerritoryOrigin = raidTargetPosition;
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

            EvaluateStateTransitions();
            ExecuteStateLogic();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || _agent.Rigidbody == null)
            {
                return;
            }

            // Lock movement during attack wind-up telegraph
            if (_isTelegraphing)
            {
                _agent.Rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            _agent.Rigidbody.linearVelocity = _desiredVelocity;
        }

        private void EvaluateStateTransitions()
        {
            float healthRatio = _agent.MaxHealth > 0 ? (float)_agent.CurrentHealth / _agent.MaxHealth : 1f;

            // 1. Low-health flee check (Raiders fight to the death; Guards/Residents flee)
            if (_currentState != AIState.Flee && _currentState != AIState.Raid && healthRatio <= _fleeHealthThreshold)
            {
                _isTelegraphing = false;
                TransitionToState(AIState.Flee);
                return;
            }

            // 2. Leash check (Applies to Wander and Chase; bypassed during Raid or Return)
            if (_currentState == AIState.Wander || _currentState == AIState.Chase)
            {
                float maxLeash = _role == UnitRole.Guard ? _guardLeashDistance : _residentLeashDistance;
                if (Vector2.Distance(transform.position, _spawnAnchor) > maxLeash)
                {
                    _currentTarget = null;
                    _isTelegraphing = false;
                    TransitionToState(AIState.Return);
                    ForceRepath(_spawnAnchor);
                    return;
                }
            }

            // 3. Return completion check
            if (_currentState == AIState.Return)
            {
                if (Vector2.Distance(transform.position, _spawnAnchor) <= 1.2f)
                {
                    TransitionToState(AIState.Wander);
                    PickNewWanderDestination();
                }
                return;
            }

            // 4. Target acquisition & validation
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _currentTarget = ScanForHostileTarget();
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
                if (_currentState == AIState.Chase || _currentState == AIState.Attack)
                {
                    TransitionToState(_rivalTerritoryOrigin != Vector2.zero ? AIState.Raid : AIState.Wander);
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

                case AIState.Raid:
                    Vector2 raidDest = _rivalTerritoryOrigin != Vector2.zero ? _rivalTerritoryOrigin : _wanderDestination;
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
                        UpdatePathfinding(_currentTarget.transform.position);
                        _desiredVelocity = ComputeSmoothSteering() * moveSpeed;
                    }
                    break;

                case AIState.Attack:
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
                return;
            }

            if (_isTelegraphing)
            {
                _telegraphTimer -= Time.deltaTime;
                if (_telegraphTimer <= 0f)
                {
                    _isTelegraphing = false;
                    _nextAttackTime = Time.time + _attackCooldown;
                    ExecuteMeleeHit();
                }
                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                _isTelegraphing = true;
                _telegraphTimer = _attackTelegraphDuration;
                // Visual wind-up cue (tint yellow/orange during telegraph)
                if (_agent.VisualRenderer != null)
                {
                    _agent.VisualRenderer.color = new Color(1f, 0.65f, 0.2f);
                }
            }
        }

        private void ExecuteMeleeHit()
        {
            // Restore sprite color after telegraph
            if (_agent.VisualRenderer != null)
            {
                _agent.VisualRenderer.color = Color.white;
            }

            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                return;
            }

            float attackRange = _agent.Stats != null ? _agent.Stats.AttackRange : 1.2f;
            float dist = Vector2.Distance(transform.position, _currentTarget.transform.position);

            // Allow slight grace margin (1.25x) so player can dodge out of wind-up telegraphs
            if (dist <= attackRange * 1.25f)
            {
                int damage = _agent.Stats != null ? _agent.Stats.AttackDamage : 8;
                float knockback = _agent.Stats != null ? _agent.Stats.KnockbackForce : 5f;
                Vector2 hitDir = ((Vector2)_currentTarget.transform.position - (Vector2)transform.position).normalized;

                _currentTarget.ApplyDamage(damage, hitDir, knockback, _agent);
            }
        }

        private void HandleDamaged(CombatAgent2D attacker)
        {
            if (!IsAlive || attacker == null || !attacker.IsAlive)
            {
                return;
            }
            if (_agent.Faction != null && attacker.Faction == _agent.Faction)
            {
                return;
            }

            // Prioritize retaliating against the attacker (especially if it's the player)
            if (_currentTarget == null || attacker.Faction != null && attacker.Faction.IsPlayerFaction)
            {
                _currentTarget = attacker;
                if (_currentState != AIState.Attack && _currentState != AIState.Flee)
                {
                    TransitionToState(AIState.Chase);
                    ForceRepath(attacker.transform.position);
                }
            }
        }

        private CombatAgent2D ScanForHostileTarget()
        {
            float sightRadius = _agent.Stats != null ? _agent.Stats.SightRadius : 6f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sightRadius, _targetMask);

            CombatAgent2D bestTarget = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                CombatAgent2D candidate = hits[i].GetComponent<CombatAgent2D>();
                if (candidate == null || candidate == _agent || !candidate.IsAlive)
                {
                    continue;
                }
                if (!IsHostileTo(candidate))
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

        private bool IsHostileTo(CombatAgent2D other)
        {
            if (_agent.Faction == null || other.Faction == null)
            {
                return false;
            }
            if (_agent.Faction == other.Faction)
            {
                return false;
            }

            // Both Goblins and Spiders are hostile to each other and to the player
            return _agent.Faction.IsHostileTo(other.Faction);
        }

        private bool HasLineOfSight(Vector2 targetPos)
        {
            Vector2 origin = transform.position;
            Vector2 dir = targetPos - origin;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dir.magnitude, _obstacleMask);
            return hit.collider == null;
        }

        private void UpdatePathfinding(Vector2 destination)
        {
            float targetDelta = Vector2.Distance(destination, _lastPathTargetPos);

            // Repath only when target shifts > 2.5 tiles or current waypoint list finishes
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

            // Soft corner repulsion to prevent snagging on 90-degree wall colliders
            Collider2D nearbyWall = Physics2D.OverlapCircle(currentPos, _cornerAvoidanceRadius, _obstacleMask);
            if (nearbyWall != null)
            {
                Vector2 closestWallPoint = nearbyWall.ClosestPoint(currentPos);
                Vector2 pushAway = (currentPos - closestWallPoint).normalized;
                desiredHeading = (desiredHeading + pushAway * 0.45f).normalized;
            }

            return desiredHeading;
        }

        private void PickNewWanderDestination()
        {
            float wanderRadius = _role == UnitRole.Guard ? _guardLeashDistance * 0.7f : _residentLeashDistance * 0.7f;
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * wanderRadius;
            _wanderDestination = _spawnAnchor + randomOffset;
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

            // 1. Sight Range Circle
            if (_agent != null && _agent.Stats != null)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
                Gizmos.DrawWireSphere(currentPos, _agent.Stats.SightRadius);
            }

            // 2. Target Line (Red line to locked hostile)
            if (_currentTarget != null && _currentTarget.IsAlive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currentPos, _currentTarget.transform.position);
            }

            // 3. Active A* Waypoint Path (Green polyline)
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

            // 4. Raid / Territory Route (Cyan line)
            if (_rivalTerritoryOrigin != Vector2.zero && _currentState == AIState.Raid)
            {
                Gizmos.color = new Color(0f, 0.85f, 1f, 0.45f);
                Gizmos.DrawLine(currentPos, _rivalTerritoryOrigin);
                Gizmos.DrawWireCube(_rivalTerritoryOrigin, new Vector3(0.5f, 0.5f, 0f));
            }

            // 5. Floating State & Target Label
            string targetName = _currentTarget != null ? _currentTarget.name : "None";
            string stateLabel = $"[{_role} | {_currentState}]\nTarget: {targetName}";

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = _agent != null && _agent.Faction != null ? _agent.Faction.DebugColor : Color.white;
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            UnityEditor.Handles.Label(currentPos + Vector3.up * 0.9f, stateLabel, labelStyle);
        }
#endif
    }
}
