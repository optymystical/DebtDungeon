using DebtDungeon.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class AbilityRunner : MonoBehaviour, IAbilityUser
    {
        [SerializeField] private GameObject _player;
        private List<AbilityDefinition> _abilities = new List<AbilityDefinition>();
        private Dictionary<AbilityDefinition, float> _readyTime = new Dictionary<AbilityDefinition, float>();
        private IStatProvider _stats;
        private Coroutine _windingUpCoroutine = null;

        // For testing
        [SerializeField] private List<AbilityDefinition> _testAbilities;

        private void Start()
        {
            foreach (AbilityDefinition ability in _testAbilities)
            {
                GrantAbility(ability);
            }
        }

        private void OnDisable()
        {
            _windingUpCoroutine = null;
        }

        /// <summary>Adds an ability and slots it by its Kind. Granting one it already has does nothing.</summary>
        public void GrantAbility(AbilityDefinition ability)
        {
            _abilities.Add(ability);
        }

        /// <summary>
        /// Removes an ability and frees its slot. Used when an upgrade node replaces it (Crackle becomes Crackle II)
        /// or when the loadout changes. Revoking one it doesn't have does nothing.
        /// </summary>
        public void RevokeAbility(AbilityDefinition ability)
        {
            _abilities.Remove(ability);
        }

        /// <summary>True if this ability is currently granted.</summary>
        public bool HasAbility(AbilityDefinition ability)
        {
            if (_abilities.Contains(ability))
            {
                return true;
            }
            return false;
        }

        public void SetStats(IStatProvider stats)
        {
            // Power is 1 for now
            _stats = stats;
        }

        // Tests if an ability is off cooldown
        private bool IsReady(AbilityDefinition ability)
        {
            // Never used yet means it isn't in the dictionary, so it's ready
            if (!_readyTime.TryGetValue(ability, out float readyAt))
            {
                return true;
            }

            return Time.time >= readyAt;
        }

        public bool TryUseBasicAttack(Vector2 direction)
        {
            BasicAttackDefinition basicAttack = null;

            foreach (AbilityDefinition ability in _abilities)
            {
                if (ability.Kind == AbilityKind.BasicAttack)
                {
                    basicAttack = (BasicAttackDefinition)ability;
                    break;
                }
            }
            // Cancel if can't attack now
            if (!basicAttack || !IsReady(basicAttack) || _windingUpCoroutine != null)
            {
                return false;
            }

            // Player is able to make a basic attack, so starting attack
            _windingUpCoroutine = StartCoroutine(WindingUp(basicAttack, direction));
            return true;
        }

        private IEnumerator WindingUp(BasicAttackDefinition basicAttack, Vector2 direction)
        {
            yield return new WaitForSeconds(basicAttack.Windup);
            _readyTime[basicAttack] = Time.time + basicAttack.Cooldown;
            float power = _stats != null ? StatRules.GetPower(_stats.GetStat(basicAttack.ScalingStat)) : 1f;
            basicAttack.Execute(_player, Faction.Player, direction, Mathf.RoundToInt(basicAttack.BaseDamage * power), 100);
            _windingUpCoroutine = null;
        }
    }
}
