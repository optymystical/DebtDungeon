using UnityEngine;

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
        [Header("Tactical Archetype")]
        [Tooltip("Defines engagement behavior, telegraphing style, and pursuit tactics.")]
        public AttackStyle Style = AttackStyle.MeleeBrawler;

        [Header("Core Vitals & Economy")]
        [Min(1)] public int MaxHealth = 30;
        [Min(0)] public int GoldDropAmount = 5;

        [Header("Movement & Perception")]
        [Min(0.5f)] public float MoveSpeed = 3.5f;
        [Min(1f)] public float SightRadius = 6.5f;

        [Header("Combat Parameters")]
        [Min(1)] public int AttackDamage = 8;
        [Min(0.5f)] public float AttackRange = 1.2f;
        [Min(0f)] public float KnockbackForce = 5f;
    }
}
