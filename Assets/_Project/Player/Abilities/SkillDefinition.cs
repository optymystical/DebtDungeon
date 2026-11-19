using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Base for skill-tree abilities on keys 1–5 and Q (Whirlwind, Crackle, Brace, Shield...).
    /// AbilityRunner spends the energy and starts the cooldown, waits Windup seconds, then calls Execute.
    /// Each subclass is one ability "shape"; the tree's assets are instances of these with their own numbers.
    /// Design data only: never change these values at runtime.
    /// </summary>
    public abstract class SkillDefinition : AbilityDefinition
    {
        [Header("Skill")]
        [Tooltip("Seconds between pressing the key and the effect. The player can't start another attack meanwhile.")]
        [SerializeField, Min(0f)] private float _windup = 0.2f;

        public float Windup => _windup;

        /// <summary>Runs the ability once its windup ends. Energy and cooldown are already handled.</summary>
        public abstract void Execute(SkillContext context);
    }

    /// <summary>
    /// What a skill needs when it fires: who used it, which way they aimed, and the runner,
    /// which can work out damage and Power and run coroutines (ScriptableObjects can't run them on their own).
    /// </summary>
    public readonly struct SkillContext
    {
        public SkillContext(GameObject user, Faction userFaction, Vector2 direction, int accuracy, AbilityRunner runner)
        {
            User = user;
            UserFaction = userFaction;
            Direction = direction;
            Accuracy = accuracy;
            Runner = runner;
        }

        public GameObject User { get; }
        public Faction UserFaction { get; }

        /// <summary>Normalized direction from the player to the mouse when the key was pressed.</summary>
        public Vector2 Direction { get; }

        /// <summary>Accuracy for this use: 100, plus Limber's bonus if it was waiting.</summary>
        public int Accuracy { get; }

        public AbilityRunner Runner { get; }
    }
}
