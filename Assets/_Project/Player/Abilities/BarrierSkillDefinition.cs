using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Self-buffs that soak damage for a few seconds: Shield and Shield II (Reflect) on Q, Brace on a number key.
    /// Shield: a bubble with Health Per Power x Power hit points. Brace: every hit is reduced by
    /// Reduction Per Power x Power. Power (stat / 10, at least 1) comes from Power Stat (Build, Constitution).
    /// Both soak the hit before armor and the Shroud (Core v1.7 Health.AddAbsorber runFirst).
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Debt Dungeon/Abilities/Barrier")]
    public class BarrierSkillDefinition : SkillDefinition
    {
        public enum BarrierType
        {
            Shield = 0,
            Brace = 1
        }

        [Header("Barrier")]
        [SerializeField] private BarrierType _type = BarrierType.Shield;

        [Tooltip("Stat whose Power scales it: Build for Shield, Constitution for Brace.")]
        [SerializeField] private StatType _powerStat = StatType.Build;

        [Tooltip("Shield: hit points per Power. (Build 20 = Power 2 = 30 with 15.)")]
        [SerializeField, Min(0f)] private float _healthPerPower = 15f;

        [Tooltip("Brace: damage taken off every hit, per Power. (Constitution 20 = 6 with 3.)")]
        [SerializeField, Min(0f)] private float _reductionPerPower = 3f;

        [Tooltip("Seconds it lasts.")]
        [SerializeField, Min(0.1f)] private float _duration = 3f;

        [Header("Reflect (Shield II)")]
        [SerializeField] private bool _reflect;
        [SerializeField] private Sprite _beamSprite;

        [Header("Look")]
        [Tooltip("Ring sprite, 1 unit across at scale 1.")]
        [SerializeField] private Sprite _auraSprite;
        [SerializeField] private Color _auraColor = new Color(0.45f, 0.6f, 1f, 0.8f);
        [SerializeField, Min(0.1f)] private float _auraDiameter = 1.6f;

        public override void Execute(SkillContext context)
        {
            // Never weaker than the listed value (same rule as armor and Shroud scaling).
            float power = Mathf.Max(1f, context.Runner.GetPower(_powerStat));
            GameObject aura = BeamEffect.CreateAura(_auraSprite, context.User.transform, _auraDiameter, _auraColor);

            IDamageAbsorber barrier;
            if (_type == BarrierType.Shield)
            {
                int health = Mathf.Max(1, Mathf.RoundToInt(_healthPerPower * power));
                barrier = new ShieldBarrier(context.User, health, aura, new ReflectSettings(_reflect, _beamSprite, _auraColor));
            }
            else
            {
                barrier = new BraceBarrier(Mathf.Max(1, Mathf.RoundToInt(_reductionPerPower * power)));
            }

            context.Runner.AddTimedAbsorber(barrier, _duration, aura);
        }
    }
}
