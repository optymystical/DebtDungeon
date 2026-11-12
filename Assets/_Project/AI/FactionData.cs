using UnityEngine;
using System.Collections.Generic;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Defines faction identity, team debug colors, and relationship hostility matrices.
    /// </summary>
    [CreateAssetMenu(fileName = "Faction_New", menuName = "DebtDungeon/FactionDefinition")]
    public class FactionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for debugging and logging.")]
        [SerializeField] private string _factionName = "Faction";

        [Tooltip("Color used for prototype debug tints and minimap markers.")]
        [SerializeField] private Color _debugColor = Color.white;

        [Tooltip("True if this faction represents the local or allied player.")]
        [SerializeField] private bool _isPlayerFaction = false;

        [Header("Relationships")]
        [Tooltip("List of other factions that this faction will actively attack.")]
        [SerializeField] private List<FactionDefinition> _hostileFactions = new List<FactionDefinition>();

        public string FactionName => _factionName;
        public Color DebugColor => _debugColor;
        public bool IsPlayerFaction => _isPlayerFaction;

        /// <summary>
        /// Evaluates whether this faction is actively hostile toward another faction.
        /// </summary>
        public bool IsHostileTo(FactionDefinition other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            // Dungeon rule: Monsters always engage the player, and vice versa.
            if (other.IsPlayerFaction || _isPlayerFaction)
            {
                return true;
            }

            // Bidirectional relationship lookup.
            return _hostileFactions.Contains(other) || (other._hostileFactions != null && other._hostileFactions.Contains(this));
        }
    }
}
