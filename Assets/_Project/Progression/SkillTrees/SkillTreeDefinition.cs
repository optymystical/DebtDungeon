using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A linear skill tree: up to 5 nodes, unlocked strictly in order. Some trees need a piece of equipment
    /// (Bladed Weapons needs a Bladed weapon). A tree can be bought without it; its abilities just can't be used
    /// until the equipment is worn. Design data only: never change these values at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillTree_New", menuName = "Debt Dungeon/Skill Tree")]
    public class SkillTreeDefinition : ScriptableObject
    {
        /// <summary>The most nodes a tree can have. Alpha content only fills nodes 1-3.</summary>
        public const int MaxNodes = 5;

        [Header("Identity")]
        [SerializeField] private string _displayName;

        [SerializeField, TextArea] private string _description;

        [SerializeField] private Archetype _archetype = Archetype.Martial;

        [Header("Equipment")]
        [Tooltip("If on, this tree's abilities only work while gear with Required Tag is worn.")]
        [SerializeField] private bool _requiresEquipment;

        [Tooltip("The tag the worn gear must have, e.g. Bladed or MediumArmor. Ignored if Requires Equipment is off.")]
        [SerializeField] private EquipmentTag _requiredTag = EquipmentTag.Bladed;

        [Header("Nodes (unlocked top to bottom)")]
        [SerializeField] private List<SkillNode> _nodes = new List<SkillNode>();

        /// <summary>Name shown in the UI, e.g. "Bladed Weapons".</summary>
        public string DisplayName => _displayName;

        /// <summary>Tooltip text.</summary>
        public string Description => _description;

        /// <summary>Only characters of this archetype can buy this tree.</summary>
        public Archetype Archetype => _archetype;

        /// <summary>True if the tree's abilities need certain gear to be used.</summary>
        public bool RequiresEquipment => _requiresEquipment;

        /// <summary>The tag the gear needs. Only meaningful when RequiresEquipment is true.</summary>
        public EquipmentTag RequiredTag => _requiredTag;

        /// <summary>The nodes, in unlock order.</summary>
        public IReadOnlyList<SkillNode> Nodes => _nodes;

        /// <summary>How many nodes the tree has.</summary>
        public int NodeCount => _nodes.Count;

        /// <summary>True if the loadout lets this tree's abilities be used.</summary>
        public bool IsUsableWith(Loadout loadout)
        {
            return !_requiresEquipment || loadout.HasTag(_requiredTag);
        }

        /// <summary>The line the UI shows for the gear this tree needs, e.g. "Needs a Bladed weapon". Null if none.</summary>
        public string GetEquipmentNote()
        {
            if (!_requiresEquipment)
            {
                return null;
            }

            switch (_requiredTag)
            {
                case EquipmentTag.Bladed:
                    return "Needs a Bladed weapon";
                case EquipmentTag.LightArmor:
                    return "Needs Light armor";
                case EquipmentTag.MediumArmor:
                    return "Needs Medium armor";
                default:
                    return $"Needs {_requiredTag} gear";
            }
        }

        /// <summary>
        /// The abilities a character owns from this tree after unlocking the first unlockedNodeCount nodes.
        /// Walks the nodes in order: an upgrade node removes the ability it replaces, then every node adds its own.
        /// So with Crackle, then Crackle II (replaces Crackle), the result is just Crackle II.
        /// </summary>
        public List<AbilityDefinition> GetAbilities(int unlockedNodeCount)
        {
            List<AbilityDefinition> abilities = new List<AbilityDefinition>();
            int count = Mathf.Clamp(unlockedNodeCount, 0, _nodes.Count);

            for (int i = 0; i < count; i++)
            {
                SkillNode node = _nodes[i];

                if (node.IsUpgrade)
                {
                    abilities.Remove(node.ReplacedAbility);
                }

                foreach (AbilityDefinition ability in node.GrantedAbilities)
                {
                    if (ability != null && !abilities.Contains(ability))
                    {
                        abilities.Add(ability);
                    }
                }
            }

            return abilities;
        }

        /// <summary>
        /// Checks the asset is set up right: 1 to 5 nodes, every node grants at least one ability with no empty
        /// entries, and every upgrade replaces an ability the tree has already given by that point.
        /// Returns false with the first problem found.
        /// </summary>
        public bool TryValidate(out string problem)
        {
            if (_nodes.Count == 0 || _nodes.Count > MaxNodes)
            {
                problem = $"has {_nodes.Count} nodes; a tree needs 1 to {MaxNodes}.";
                return false;
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                SkillNode node = _nodes[i];
                int nodeNumber = i + 1;

                if (node.GrantedAbilities.Count == 0)
                {
                    problem = $"node {nodeNumber} grants no abilities.";
                    return false;
                }

                foreach (AbilityDefinition ability in node.GrantedAbilities)
                {
                    if (ability == null)
                    {
                        problem = $"node {nodeNumber} has an empty ability entry.";
                        return false;
                    }
                }

                // The abilities owned just before this node; an upgrade must replace one of them.
                if (node.IsUpgrade && !GetAbilities(i).Contains(node.ReplacedAbility))
                {
                    problem = $"node {nodeNumber} replaces {node.ReplacedAbility.name}, which no earlier node grants.";
                    return false;
                }
            }

            problem = null;
            return true;
        }

        /// <summary>Editor check: warns in the Console when the asset is set up wrong.</summary>
        private void OnValidate()
        {
            if (!TryValidate(out string problem))
            {
                Debug.LogWarning($"{name}: {problem}", this);
            }
        }
    }
}
