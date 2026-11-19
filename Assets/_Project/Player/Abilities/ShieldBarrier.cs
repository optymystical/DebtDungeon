using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Shield's bubble: a pool of hit points that soaks damage before armor and the Shroud (Poison goes through).
    /// Its Limit is whatever it has left, as in Flow State, so one big hit can break it.
    /// With Reflect (Shield II) the amount it soaks is fired back at the attacker.
    /// Plain C# class: AbilityRunner registers it on the player's Health and removes it when time runs out.
    /// </summary>
    public class ShieldBarrier : IDamageAbsorber
    {
        private readonly GameObject _user;
        private readonly GameObject _aura;
        private readonly ReflectSettings _reflect;
        private int _remaining;

        public ShieldBarrier(GameObject user, int health, GameObject aura, ReflectSettings reflect)
        {
            _user = user;
            _remaining = health;
            _aura = aura;
            _reflect = reflect;
        }

        public bool IsBroken => _remaining <= 0;

        public int AbsorbDamage(DamageInfo damage)
        {
            if (IsBroken || damage.Type == DamageType.Poison)
            {
                return damage.Amount;
            }

            int absorbed = Mathf.Min(_remaining, damage.Amount);
            _remaining -= absorbed;
            _reflect.TryReflect(_user, damage, absorbed);

            if (IsBroken && _aura != null)
            {
                Object.Destroy(_aura);
            }

            return damage.Amount - absorbed;
        }
    }

    /// <summary>
    /// Reflect (Shield II, Shield III): sends absorbed damage back at whoever caused it, rolled at 100% Accuracy,
    /// as Magic damage, with a beam effect. Disabled when Enabled is false.
    /// </summary>
    public readonly struct ReflectSettings
    {
        public ReflectSettings(bool enabled, Sprite beamSprite, Color beamColor)
        {
            Enabled = enabled;
            BeamSprite = beamSprite;
            BeamColor = beamColor;
        }

        public bool Enabled { get; }
        public Sprite BeamSprite { get; }
        public Color BeamColor { get; }

        public void TryReflect(GameObject user, DamageInfo damage, int absorbed)
        {
            if (!Enabled || absorbed <= 0 || damage.Source == null || user == null)
            {
                return;
            }

            Health attacker = damage.Source.GetComponentInParent<Health>();
            if (attacker == null || !attacker.IsAlive || attacker.Faction == Faction.Player)
            {
                return;
            }

            HitResult result = CombatRules.RollHit(CombatRules.BaseAccuracy);
            BeamEffect.Spawn(BeamSprite, user.transform.position, attacker.transform.position, BeamColor);
            if (result != HitResult.Miss)
            {
                attacker.TakeDamage(new DamageInfo(CombatRules.GetDamage(absorbed, result), DamageType.Magic,
                    user, Faction.Player, result == HitResult.Critical));
            }
        }
    }
}
