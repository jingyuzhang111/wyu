using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Powers;

public class warriorPower : wyuPower
{
    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, 
    ValueProp props, Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (cardSource?.Type != CardType.Attack)
        {
            return 1m;
        }
        // 联机时防止buff共享给所有人的牌
        if (dealer != Owner)
        {
            return 1m;
        }

        var ownerCreature = Owner;
        decimal currentHp = ReadCreatureDecimal(ownerCreature, "CurrentHp", "CurrentHealth");
        decimal maxHp = ReadCreatureDecimal(ownerCreature, "MaxHp", "MaxHealth");

        if (maxHp <= 0m)
        {
            return 1m;
        }

        decimal ratio = Math.Clamp(currentHp / maxHp, 0m, 1m);
        return 2m - ratio;
    }

    private static decimal ReadCreatureDecimal(Creature creature, params string[] names)
    {
        var type = creature.GetType();

        foreach (var name in names)
        {
            var property = type.GetProperty(name);
            if (property != null)
            {
                object? value = property.GetValue(creature);
                if (value != null)
                {
                    return Convert.ToDecimal(value);
                }
            }

            var field = type.GetField(name);
            if (field != null)
            {
                object? value = field.GetValue(creature);
                if (value != null)
                {
                    return Convert.ToDecimal(value);
                }
            }
        }

        return 0m;
    }
}