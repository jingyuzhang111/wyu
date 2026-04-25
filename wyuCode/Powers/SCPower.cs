using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;

namespace wyu.wyuCode.Powers;
public class SCPower : wyuPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
    ];

    private bool _isResolvingBuffGain;

    /// <summary>
    /// 当拥有者获得 Buff 时触发：消耗自身层数，并顺带消耗其他 Buff 类型 Power。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_isResolvingBuffGain || Amount <= 0 || power == this || power.Owner != Owner || power.Type != PowerType.Buff || amount <= 0m)
        {
            return;
        }

        int triggerAmount = (int)amount;
        if (triggerAmount <= 0)
        {
            return;
        }

        int consumeAmount = Math.Min(Amount, triggerAmount);
        if (consumeAmount <= 0)
        {
            return;
        }

        // Consume the buff that just changed first, then consume other existing buffs.
        List<PowerModel> buffPowers = new List<PowerModel>();
        if (power.Amount > 0)
        {
            buffPowers.Add(power);
        }

        buffPowers.AddRange(
            Owner.Powers.Where(p =>
                p.Type == PowerType.Buff
                && !ReferenceEquals(p, this)
                && !ReferenceEquals(p, power)
                && p.Amount > 0));

        _isResolvingBuffGain = true;
        try
        {
            decimal finalDamage = DynamicVars["Damage"].BaseValue * consumeAmount;

            Flash();
            Log.Info($"SC效果触发：获得Buff时消耗了{consumeAmount}层，造成{finalDamage}点伤害。");

            await CreatureCmd.Damage(
                new BlockingPlayerChoiceContext(),
                base.CombatState!.HittableEnemies,
                finalDamage,
                ValueProp.Unblockable | ValueProp.Move,
                base.Owner,
                null);

            await PowerCmd.ModifyAmount(this, -consumeAmount, base.Owner, null, silent: true);

            int remaining = consumeAmount;
            foreach (PowerModel buffPower in buffPowers)
            {
                if (remaining <= 0)
                {
                    break;
                }

                int actualConsume = Math.Min(remaining, buffPower.Amount);
                if (actualConsume <= 0)
                {
                    continue;
                }

                remaining -= actualConsume;
                await PowerCmd.ModifyAmount(buffPower, -actualConsume, base.Owner, null, silent: true);
            }
        }
        finally
        {
            _isResolvingBuffGain = false;
        }
    }

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (base.Amount <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}