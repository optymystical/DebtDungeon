using DebtDungeon.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class AbilityRunner : MonoBehaviour, IAbilityUser
    {
        [Header("References")]
        [SerializeField] private GameObject _player;
        [SerializeField] private PlayerCombatState _playerCombatState;
        [SerializeField] private PlayerEnergy _playerEnergy;
        [SerializeField] private StatusEffects _statusEffects;

        private List<AbilityDefinition> _abilities = new List<AbilityDefinition>();
        private Dictionary<AbilityDefinition, float> _readyTime = new Dictionary<AbilityDefinition, float>();
        private IStatProvider _stats;

        // Ability Slots
        private AbilityDefinition _ability1 = null;
        private AbilityDefinition _ability2 = null;
        private AbilityDefinition _ability3 = null;
        private AbilityDefinition _ability4 = null;
        private AbilityDefinition _ability5 = null;
        private AbilityDefinition _defensiveAbility = null;

        // Timer coroutines
        private Coroutine _windingUpCoroutine = null;
        private Coroutine _active1Coroutine = null;
        private Coroutine _active2Coroutine = null;
        private Coroutine _active3Coroutine = null;
        private Coroutine _active4Coroutine = null;
        private Coroutine _active5Coroutine = null;
        private Coroutine _defensiveCoroutine = null;

        // For testing
        //[SerializeField] private List<AbilityDefinition> _testAbilities;

        //private void Start()
        //{
        //    foreach (AbilityDefinition ability in _testAbilities)
        //    {
        //        GrantAbility(ability);
        //    }
        //}

        private void OnDisable()
        {
            _windingUpCoroutine = null;
            _defensiveCoroutine = null;
        }

        /// <summary>Adds an ability and slots it by its Kind. Granting one it already has does nothing.</summary>
        public void GrantAbility(AbilityDefinition ability)
        {
            if (!_abilities.Contains(ability))
            {
                _abilities.Add(ability);

                if (ability.Kind == AbilityKind.Active)
                {
                    TryAddActiveToSlot(ability);
                }
            }
        }

        /// <summary>
        /// Removes an ability and frees its slot. Used when an upgrade node replaces it (Crackle becomes Crackle II)
        /// or when the loadout changes. Revoking one it doesn't have does nothing.
        /// </summary>
        public void RevokeAbility(AbilityDefinition ability)
        {
            _abilities.Remove(ability);

            if (ability.Kind == AbilityKind.Active || ability.Kind == AbilityKind.Defensive)
            {
                RemoveFromSlot(ability);
            }
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

        private bool TryAddActiveToSlot(AbilityDefinition ability)
        {
            if (!_ability1)
            {
                _ability1 = ability;
            }
            else if (!_ability2)
            {
                _ability2 = ability;
            }
            else if (!_ability3)
            {
                _ability3 = ability;
            }
            else if (!_ability4)
            {
                _ability4 = ability;
            }
            else if (!_ability5)
            {
                _ability5 = ability;
            }
            else
            {
                return false;
            }
            return true;
        }

        private void RemoveFromSlot(AbilityDefinition ability)
        {
            if (_ability1 == ability)
            {
                _ability1 = null;
            }
            else if (_ability2 == ability)
            {
                _ability2 = null;
            }
            else if (_ability3 == ability)
            {
                _ability3 = null;
            }
            else if (_ability4 == ability)
            {
                _ability4 = null;
            }
            else if (_ability5 == ability)
            {
                _ability5 = null;
            }
            else if (_defensiveAbility == ability)
            {
                _defensiveAbility = null;
            }
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
            _playerCombatState.MarkInCombat();

            BasicAttackDefinition basicAttack = null;

            // Since player should only have one, find first basic attack
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

        // From PlayerController input. Assumes player can only have one defensive ability at a time
        public bool TryUseAbility(int num)
        {
            _playerCombatState.MarkInCombat();

            // Identify which ability is being used
            AbilityDefinition activeAbility = null;
            switch (num)
            {
                case 1:
                    activeAbility = _ability1;
                    break;
                case 2:
                    activeAbility = _ability2;
                    break;
                case 3:
                    activeAbility = _ability3;
                    break;
                case 4:
                    activeAbility = _ability4;
                    break;
                case 5:
                    activeAbility = _ability5;
                    break;
                case 6:
                    activeAbility = _defensiveAbility;
                    break;
            }

            // Cancel if can't use it now
            if (!activeAbility || !IsReady(activeAbility) || _windingUpCoroutine != null)
            {
                return false;
            }

            // Use energy, canceling if can't
            if (_playerEnergy.UseEnergy(activeAbility.EnergyCost))
            {
                _readyTime[activeAbility] = Time.time + activeAbility.Cooldown;
                if (activeAbility == _ability1)
                {
                    _active1Coroutine = StartCoroutine(Active1Timer(activeAbility.Cooldown));
                }
                else if (activeAbility == _ability2)
                {
                    _active2Coroutine = StartCoroutine(Active2Timer(activeAbility.Cooldown));
                }
                else if (activeAbility == _ability3)
                {
                    _active3Coroutine = StartCoroutine(Active3Timer(activeAbility.Cooldown));
                }
                else if (activeAbility == _ability4)
                {
                    _active4Coroutine = StartCoroutine(Active4Timer(activeAbility.Cooldown));
                }
                else if (activeAbility == _ability5)
                {
                    _active5Coroutine = StartCoroutine(Active5Timer(activeAbility.Cooldown));
                }
                else if (activeAbility == _defensiveAbility)
                {
                    _defensiveCoroutine = StartCoroutine(DefensiveTimer(activeAbility.Cooldown));
                }
                return true;
            }
            return false;
        }

        private IEnumerator WindingUp(BasicAttackDefinition basicAttack, Vector2 direction)
        {
            yield return new WaitForSeconds(basicAttack.Windup);
            _readyTime[basicAttack] = Time.time + basicAttack.Cooldown;
            float power = _stats != null ? StatRules.GetPower(_stats.GetStat(basicAttack.ScalingStat)) : 1f;

            // Detect if player is strong or weak to modify power before Execute
            if (_statusEffects.StrengthenedStacks > 0)
            {
                // Str1 +50%   Str2 +100%   Str3 +150%
                power += (power * _statusEffects.StrengthenedStacks)/2;
            }
            else if (_statusEffects.WeakenedStacks > 0)
            {
                // Weak1 -20%   Weak2 -40%   Weak3 -60%
                power -= power * (0.2f * _statusEffects.WeakenedStacks);
            }

            basicAttack.Execute(_player, Faction.Player, direction, Mathf.RoundToInt(basicAttack.BaseDamage * power), 100);
            _windingUpCoroutine = null;
        }

        private IEnumerator Active1Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active1Coroutine = null;
        }
        private IEnumerator Active2Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active2Coroutine = null;
        }
        private IEnumerator Active3Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active3Coroutine = null;
        }
        private IEnumerator Active4Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active4Coroutine = null;
        }
        private IEnumerator Active5Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active5Coroutine = null;
        }

        private IEnumerator DefensiveTimer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _defensiveCoroutine = null;
        }
    }
}
