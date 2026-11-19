using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// A passive skill (Limber, Close Quarters, Careful Steps): no key, no energy, always on while granted.
    /// It is a set of modifiers; AbilityRunner adds up the ones from every granted passive.
    /// Leave a field at 0 when the passive doesn't use it. Set Kind = Passive on every asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Debt Dungeon/Abilities/Passive")]
    public class PassiveDefinition : AbilityDefinition
    {
        [Header("Limber: after one of your attacks lands")]
        [Tooltip("Accuracy added to your next attack after one of yours lands (above 100 = crit chance).")]
        [SerializeField] private int _accuracyBonusAfterHit;

        [Tooltip("Seconds that bonus waits for your next attack before it's lost.")]
        [SerializeField, Min(0f)] private float _bonusWindow = 3f;

        [Header("Close Quarters: defense")]
        [Tooltip("Accuracy added to enemy melee attacks against you (negative = harder to hit you).")]
        [SerializeField] private int _meleeAccuracyAgainstYou;

        [Header("Careful Steps: movement")]
        [Tooltip("Burden steps taken off while this is granted (each step is 10% movement speed).")]
        [SerializeField, Min(0)] private int _burdenRelief;

        public int AccuracyBonusAfterHit => _accuracyBonusAfterHit;
        public float BonusWindow => _bonusWindow;
        public int MeleeAccuracyAgainstYou => _meleeAccuracyAgainstYou;
        public int BurdenRelief => _burdenRelief;
    }
}
