using UnityEngine;
//using System.Collections;
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
        Raid
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

        private CombatAgent2D _agent;
        private AIState _currentState = AIState.Wander;
        private CombatAgent2D _currentTarget;

        private Vector2 _spawnAnchor;
        private Vector2 _rivalTerritoryOrigin;
        private Vector2 _wanderDestination;
        private bool _hasSpawnAnchor;

        private List<Vector2> _waypoints = new List<Vector2>();
        private int _waypointIndex;
        private Vector2 _lastPathTargetPos = new Vector2(float.MaxValue, float.MaxValue);
        private float _repathTimer;

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
                _agent.OnAttackedBy += HandleAttackedBy;
            }
        }

        private void OnDisable()
        {
            if (_agent != null)
            {
                _agent.OnAttackedBy -= HandleAttackedBy;
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

            // 1. Low-health flee check
            if (_currentState != AIState.Flee && _currentState != AIState.Raid && healthRatio <= _fleeHealthThreshold)
            {
                _isTelegraphing = false;
                TransitionToState(AIState.Flee);
                return;
            }

            // 2. Territory leash check
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
                    ExecuteCoreCombatAttack();
                }

                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                _isTelegraphing = true;
                _telegraphTimer = _attackTelegraphDuration;
                if (_agent.VisualRenderer != null)
                {
                    _agent.VisualRenderer.color = new Color(1f, 0.65f, 0.2f);
                }
            }
        }

        private void ExecuteCoreCombatAttack()
        {
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

            float knockback = _agent.Stats != null ? _agent.Stats.KnockbackForce : 5f;
            Vector2 hitDir = ((Vector2)_currentTarget.transform.position - (Vector2)transform.position).normalized;

            _currentTarget.QueueKnockback(hitDir, knockback);

            if (_currentTarget.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo(finalDamage, damageType, gameObject, _agent.Faction, isCritical);
                damageable.TakeDamage(damageInfo);
            }
        }

        private void HandleAttackedBy(GameObject attackerObj)
        {
            if (!IsAlive || attackerObj == null)
            {
                return;
            }

            CombatAgent2D attacker = attackerObj.GetComponent<CombatAgent2D>();
            if (attacker == null || !attacker.IsAlive)
            {
                return;
            }

            if (attacker.Faction == _agent.Faction)
            {
                return;
            }

            if (_currentTarget == null || attacker.Faction == Faction.Player)
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
            return hit.collider == null;
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
