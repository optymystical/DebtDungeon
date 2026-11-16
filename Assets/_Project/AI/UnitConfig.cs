using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    public enum AttackStyle
    {
        MeleeBrawler,
        MeleeSkitterer,
        RangedSpitter,
        ControlWeaver
    }

    /// <summary>
    /// Configures unit stats, perception ranges, and tactical styles.
    /// </summary>
    [CreateAssetMenu(fileName = "Unit_", menuName = "DebtDungeon/AI/Unit Config")]
    public class UnitConfig : ScriptableObject
    {
        [Header("Tactical Archetype")]
        [Tooltip("Wind-up delay before strike lands. Goblins = slow heavy wind-up (~0.75s); Spiders = quick snap bite (~0.20s).")]
        [SerializeField, Min(0.05f)] private float _attackWindupSeconds = 0.65f;

        [Tooltip("Distance the unit leaps backward immediately after landing a strike. 0 = hold ground (Goblin); > 0 = hit-and-run jump (Spider).")]
        [SerializeField, Min(0f)] private float _postAttackBackstepDistance = 0f;

        [Tooltip("Duration of the post-attack backstep hop before resuming chase/attack.")]
        [SerializeField, Min(0.05f)] private float _postAttackBackstepDuration = 0.25f;

        [Tooltip("Strength of ally pack cohesion while wandering/chasing. > 0 = travels in groups (Goblin); 0 = independent skitterer (Spider).")]
        [SerializeField, Range(0f, 2f)] private float _packCohesionWeight = 0f;

        [Tooltip("Radius used to locate nearby squadmates for group movement.")]
        [SerializeField, Min(1f)] private float _packCohesionRadius = 4.5f;

        [Header("Ranged Kiting & Telegraphs")]
        [Tooltip("If true, unit maintains PreferredRange and retreats if targets get closer than MinKeepDistance.")]
        public bool IsRangedKiter = false;
        [Min(0.5f)] public float MinKeepDistance = 3.2f;
        [Min(1f)] public float PreferredRange = 6.0f;
        public GameObject ProjectilePrefab;
        [Min(1f)] public float ProjectileSpeed = 4.5f;
        [Min(0f)] public float AoERadius = 1.35f;
        [Tooltip("Toggleable visual trajectory line during wind-up so playtesters can read the shot path.")]
        public bool ShowTrajectoryTelegraph = true;
        [Tooltip("Toggleable ground landing circle showing where lobbed rocks or web pools will land.")]
        public bool ShowGroundWarningCircle = true;

        [Header("Control Effects (Web Weaver)")]
        [Range(0.1f, 1f)] public float SlowMultiplier = 0.45f;
        [Min(0f)] public float SlowDurationSeconds = 3.0f;
        [Tooltip("Designer toggle for poison DoT on Web Weaver attacks.")]
        public bool ApplyPoison = true;
        [Min(1)] public int PoisonTickDamage = 2;
        [Min(0.2f)] public float PoisonTickInterval = 1.0f;
        [Min(0.5f)] public float PoisonDurationSeconds = 4.0f;

        [Header("Tactical & Archetype")]
        [Tooltip("Defines engagement behavior, telegraphing style, and pursuit tactics.")]
        public string DisplayName = "Goblin Grunt";
        public AttackStyle Style = AttackStyle.MeleeBrawler;

        [Header("Core Vitals & Kill Rewards")]        
        [Min(0)] public int GoldDropAmount = 5;
        [Min(0)] public int XpReward = 10;

        [Header("Perception & Hearing")]
        [Min(1f)] public float SightRadius = 6.5f;
        [Tooltip("Radius within which combat clashes, projectiles, or broken doors alert this unit to investigate.")]
        [Min(0f)] public float HearingRadius = 9.0f;

        [Header("Movement")]
        [Min(0.5f)] public float MoveSpeed = 3.5f;

        [Header("Combat Rules & Stats")]
        [Min(1)] public int AttackDamage = 8;
        public DamageType DamageType = DamageType.Physical;
        [Min(0.5f)] public float AttackRange = 1.2f;
        [Min(0.1f)] public float AttackCooldownSeconds = 1.2f;
        [Range(0, 200)] public int Accuracy = CombatRules.BaseAccuracy;
        [Min(0f)] public float KnockbackForce = 5f;

        public float AttackWindupSeconds => _attackWindupSeconds;
        public float PostAttackBackstepDistance => _postAttackBackstepDistance;
        public float PostAttackBackstepDuration => _postAttackBackstepDuration;
        public float PackCohesionWeight => _packCohesionWeight;
        public float PackCohesionRadius => _packCohesionRadius;

    }
}
