using UnityEngine;
using System.Collections.Generic;
using System;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    [Serializable]
    public struct FactionHostilityEntry
    {
        public Faction FactionA;
        public Faction FactionB;
    }

    [Serializable]
    public struct FactionColorEntry
    {
        public Faction Faction;
        public Color DebugColor;
    }

    /// <summary>
    /// Centralized diplomacy and debug color lookup keyed by Core's Faction enum.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionData", menuName = "DebtDungeon/AI/Faction Data")]
    public class FactionData : ScriptableObject
    {
        [Header("Hostility Matrix")]
        [SerializeField] private List<FactionHostilityEntry> _hostilePairs = new List<FactionHostilityEntry>();

        [Header("Debug Visuals")]
        [SerializeField] private List<FactionColorEntry> _factionColors = new List<FactionColorEntry>();

        public bool IsHostile(Faction a, Faction b)
        {
            if (a == b)
            {
                return false;
            }

            if (a == Faction.Player || b == Faction.Player)
            {
                return true;
            }

            for (int i = 0; i < _hostilePairs.Count; i++)
            {
                FactionHostilityEntry entry = _hostilePairs[i];
                if ((entry.FactionA == a && entry.FactionB == b) ||
                    (entry.FactionA == b && entry.FactionB == a))
                {
                    return true;
                }
            }

            return false;
        }

        public Color GetDebugColor(Faction faction)
        {
            for (int i = 0; i < _factionColors.Count; i++)
            {
                if (_factionColors[i].Faction == faction)
                {
                    return _factionColors[i].DebugColor;
                }
            }

            return Color.white;
        }
    }
}
