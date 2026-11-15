using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    public enum AttackStyle
    {
        MeleeBrawler,
        MeleeSkitterer,
        RangedSpitter
    }

    /// <summary>
    /// Configures unit stats, perception ranges, and tactical styles.
    /// </summary>
    [CreateAssetMenu(fileName = "Unit_", menuName = "DebtDungeon/AI/Unit Config")]
    public class UnitConfig : ScriptableObject
    {
        [Header("Tactical & Archetype")]
        [Tooltip("Defines engagement behavior, telegraphing style, and pursuit tactics.")]
        public string DisplayName = "Goblin Grunt";
        public AttackStyle Style = AttackStyle.MeleeBrawler;

        [Header("Core Vitals & Kill Rewards")]        
        [Min(0)] public int GoldDropAmount = 5;
        [Min(0)] public int XpReward = 10;

        [Header("Movement & Perception")]
        [Min(0.5f)] public float MoveSpeed = 3.5f;
        [Min(1f)] public float SightRadius = 6.5f;

        [Header("Combat Rules & Stats")]
        [Min(1)] public int AttackDamage = 8;
        public DamageType DamageType = DamageType.Physical;
        [Min(0.5f)] public float AttackRange = 1.2f;
        [Range(0, 200)] public int Accuracy = CombatRules.BaseAccuracy;
        [Min(0f)] public float KnockbackForce = 5f;
    }
}
