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
using MegaCrit.Sts2.Core.Combat;

namespace wyu.wyuCode.Powers;

public class RuDongPower : wyuPower
{
    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, 
    ValueProp props, Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (target == this.Owner)
        {
            return 0m;
        }   
        return 1m;
    }
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
	}


}