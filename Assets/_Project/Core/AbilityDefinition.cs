using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Base data for every ability. Progression grants these (from skill trees and equipment);
    /// Player subclasses this (melee strike, projectile, self-buff) and executes them.
    /// Design data only: never change these values at runtime.
    /// </summary>
    public abstract class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique id, e.g. \"crackle_2\". Used for lookups and logs; never shown to the player.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [SerializeField, TextArea] private string _description;

        [SerializeField] private Sprite _icon;

        [Header("Use")]
        [Tooltip("Which control slot this ability goes in.")]
        [SerializeField] private AbilityKind _kind = AbilityKind.Active;

        [Tooltip("Seconds before it can be used again. Passives ignore this.")]
        [SerializeField, Min(0f)] private float _cooldown = 1f;

        [Tooltip("Energy spent per use.")]
        [SerializeField, Min(0)] private int _energyCost;

        /// <summary>Unique id, e.g. "crackle_2".</summary>
        public string Id => _id;

        /// <summary>Name shown in the UI, e.g. "Crackle II".</summary>
        public string DisplayName => _displayName;

        /// <summary>Tooltip text.</summary>
        public string Description => _description;

        /// <summary>Icon for the hotbar and skill tree.</summary>
        public Sprite Icon => _icon;

        /// <summary>Which control slot this ability goes in.</summary>
        public AbilityKind Kind => _kind;

        /// <summary>Seconds between uses.</summary>
        public float Cooldown => _cooldown;

        /// <summary>Energy spent per use.</summary>
        public int EnergyCost => _energyCost;
    }
}
