using DebtDungeon.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class AbilityRunner : MonoBehaviour, IAbilityUser, IDefenseModifier
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

        // Wrap-up additions (Chris/Claude): passives and timed barriers
        private Health _health;
        private int _pendingAccuracyBonus;   // Limber: waiting for the next attack
        private float _pendingBonusUntil;

        // Events
        public event Action<int> AbilityReady;
        public event Action<int> InsufficientEnergy;

        // For testing
        [SerializeField] private List<AbilityDefinition> _testAbilities;

        private void Start()
        {
            foreach (AbilityDefinition ability in _testAbilities)
            {
                GrantAbility(ability);
            }
        }

        private void Awake()
        {
            GameObject owner = _player != null ? _player : gameObject;
            _health = owner.GetComponent<Health>();
        }

        private void OnEnable()
        {
            GameEvents.ActorDamaged += HandleActorDamaged;
        }

        private void OnDisable()
        {
            GameEvents.ActorDamaged -= HandleActorDamaged;
            _windingUpCoroutine = null;
            _defensiveCoroutine = null;
        }

        // ---- Passives (wrap-up additions) ----

        // Limber: when one of the player's attacks lands on someone else, the next attack gets the bonus.
        private void HandleActorDamaged(Health target, DamageInfo damage, int damageTaken)
        {
            if (damage.Source == null || damage.Source != _player || target == _health)
            {
                return;
            }

            int bonus = 0;
            float window = 0f;
            foreach (AbilityDefinition ability in _abilities)
            {
                if (ability is PassiveDefinition passive && passive.AccuracyBonusAfterHit != 0)
                {
                    bonus += passive.AccuracyBonusAfterHit;
                    window = Mathf.Max(window, passive.BonusWindow);
                }
            }

            if (bonus != 0)
            {
                // Doesn't stack: a new hit just refreshes the same bonus.
                _pendingAccuracyBonus = bonus;
                _pendingBonusUntil = Time.time + window;
            }
        }

        /// <summary>Accuracy for the attack starting now: 100 plus Limber's bonus if it's waiting (then it's used up).</summary>
        private int ConsumeAttackAccuracy()
        {
            int accuracy = CombatRules.BaseAccuracy;
            if (_pendingAccuracyBonus != 0 && Time.time <= _pendingBonusUntil)
            {
                accuracy += _pendingAccuracyBonus;
            }

            _pendingAccuracyBonus = 0;
            return accuracy;
        }

        /// <summary>Core v1.7: Close Quarters makes enemy melee attacks less accurate against the player.</summary>
        public int GetDefenderAccuracyModifier(bool isMeleeAttack)
        {
            if (!isMeleeAttack)
            {
                return 0;
            }

            int modifier = 0;
            foreach (AbilityDefinition ability in _abilities)
            {
                if (ability is PassiveDefinition passive)
                {
                    modifier += passive.MeleeAccuracyAgainstYou;
                }
            }
            return modifier;
        }

        /// <summary>Careful Steps: burden steps taken off by granted passives. Game applies it to movement.</summary>
        public int GetBurdenRelief()
        {
            int relief = 0;
            foreach (AbilityDefinition ability in _abilities)
            {
                if (ability is PassiveDefinition passive)
                {
                    relief += passive.BurdenRelief;
                }
            }
            return relief;
        }

        // ---- Timed barriers (Shield, Brace) ----

        /// <summary>
        /// Puts an absorber in front of armor and the Shroud for duration seconds, then removes it and its visual.
        /// </summary>
        public void AddTimedAbsorber(IDamageAbsorber absorber, float duration, GameObject visual)
        {
            if (_health == null || absorber == null)
            {
                return;
            }

            _health.AddAbsorber(absorber, true);
            StartCoroutine(RemoveAbsorberLater(absorber, duration, visual));
        }

        private IEnumerator RemoveAbsorberLater(IDamageAbsorber absorber, float duration, GameObject visual)
        {
            yield return new WaitForSeconds(duration);

            if (_health != null)
            {
                _health.RemoveAbsorber(absorber);
            }

            if (visual != null)
            {
                Destroy(visual);
            }
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
                else if (ability.Kind == AbilityKind.Defensive)
                {
                    // Wrap-up fix (Chris/Claude): defensives go in the Q slot, so TryUseAbility(6) can find them.
                    _defensiveAbility = ability;
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
            _stats = stats;
        }

        // ---- Wrap-up additions (Chris/Claude): read-only info for skills and the HUD ability bar ----

        /// <summary>The granted basic attack (from the weapon or Foci), or null if none.</summary>
        public BasicAttackDefinition CurrentBasicAttack
        {
            get
            {
                foreach (AbilityDefinition ability in _abilities)
                {
                    if (ability.Kind == AbilityKind.BasicAttack)
                    {
                        return ability as BasicAttackDefinition;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Power for a stat (stat / 10, or 1 before stats are set), after Strengthened / Weakened.
        /// The same rule the basic attack uses, so skills scale the same way.
        /// </summary>
        public float GetModifiedPower(StatType stat)
        {
            float power = _stats != null ? StatRules.GetPower(_stats.GetStat(stat)) : 1f;

            if (_statusEffects != null && _statusEffects.StrengthenedStacks > 0)
            {
                // Str1 +50%   Str2 +100%   Str3 +150%
                power += (power * _statusEffects.StrengthenedStacks) / 2;
            }
            else if (_statusEffects != null && _statusEffects.WeakenedStacks > 0)
            {
                // Weak1 -20%   Weak2 -40%   Weak3 -60%
                power -= power * (0.2f * _statusEffects.WeakenedStacks);
            }
            return power;
        }

        /// <summary>Power for a stat (stat / 10, or 1 before stats are set), without status effects. For shields.</summary>
        public float GetPower(StatType stat)
        {
            return _stats != null ? StatRules.GetPower(_stats.GetStat(stat)) : 1f;
        }

        /// <summary>What one basic attack hit would deal right now (0 with no basic attack).</summary>
        public int GetBasicAttackDamage()
        {
            BasicAttackDefinition basicAttack = CurrentBasicAttack;
            if (basicAttack == null)
            {
                return 0;
            }
            return Mathf.RoundToInt(basicAttack.BaseDamage * GetModifiedPower(basicAttack.ScalingStat));
        }

        /// <summary>The ability in a key slot: 1–5 for actives, 6 for the defensive (Q). Null if empty.</summary>
        public AbilityDefinition GetSlot(int num)
        {
            switch (num)
            {
                case 1: return _ability1;
                case 2: return _ability2;
                case 3: return _ability3;
                case 4: return _ability4;
                case 5: return _ability5;
                case 6: return _defensiveAbility;
                default: return null;
            }
        }

        /// <summary>Seconds until this ability can be used again (0 when ready).</summary>
        public float GetCooldownRemaining(AbilityDefinition ability)
        {
            if (ability == null || !_readyTime.TryGetValue(ability, out float readyAt))
            {
                return 0f;
            }
            return Mathf.Max(0f, readyAt - Time.time);
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
                    basicAttack = ability as BasicAttackDefinition; // Wrap-up fix: 'as' returns null instead of throwing
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
            return TryUseAbility(num, Vector2.right);
        }

        /// <summary>Uses the ability in a key slot, aimed in direction (player to mouse).</summary>
        public bool TryUseAbility(int num, Vector2 direction)
        {
            _playerCombatState.MarkInCombat();

            // Identify which ability is being used
            AbilityDefinition activeAbility = GetSlot(num);

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

                // Wrap-up addition: real skills run after their windup. Placeholder assets still do nothing.
                if (activeAbility is SkillDefinition skill)
                {
                    _windingUpCoroutine = StartCoroutine(SkillWindup(skill, direction));
                }
                return true;
            }
            InsufficientEnergy?.Invoke(num);
            GetComponent<AudioSource>().Play();
            return false;
        }

        private IEnumerator WindingUp(BasicAttackDefinition basicAttack, Vector2 direction)
        {
            yield return new WaitForSeconds(basicAttack.Windup);
            _readyTime[basicAttack] = Time.time + basicAttack.Cooldown;
            // Power after Strengthened / Weakened (moved into GetModifiedPower so skills share it)
            float power = GetModifiedPower(basicAttack.ScalingStat);

            basicAttack.Execute(_player, Faction.Player, direction, Mathf.RoundToInt(basicAttack.BaseDamage * power),
                ConsumeAttackAccuracy());
            _windingUpCoroutine = null;
        }

        private IEnumerator SkillWindup(SkillDefinition skill, Vector2 direction)
        {
            yield return new WaitForSeconds(skill.Windup);
            skill.Execute(new SkillContext(_player, Faction.Player, direction, ConsumeAttackAccuracy(), this));
            _windingUpCoroutine = null;
        }

        private IEnumerator Active1Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active1Coroutine = null;
            AbilityReady?.Invoke(1);
        }
        private IEnumerator Active2Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active2Coroutine = null;
            AbilityReady?.Invoke(2);
        }
        private IEnumerator Active3Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active3Coroutine = null;
            AbilityReady?.Invoke(3);
        }
        private IEnumerator Active4Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active4Coroutine = null;
            AbilityReady?.Invoke(4);
        }
        private IEnumerator Active5Timer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _active5Coroutine = null;
            AbilityReady?.Invoke(5);
        }

        private IEnumerator DefensiveTimer(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            _defensiveCoroutine = null;
            AbilityReady?.Invoke(6);
        }
    }
}
