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
using wyu.wyuCode.Cards;

namespace wyu.wyuCode.Powers;

public class SiyeBitePower : wyuPower
{
    // 效果类型Buff, Debuff...
	public override PowerType Type => PowerType.Debuff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, 
    ValueProp props, Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        // target:受到伤害的家伙
        // dealer:造成伤害的家伙
        // props:伤害的属性,如是否无视格挡,是否有加成
        // cardSource:伤害来源的牌，null为无来源

		if (target != base.Owner)
		{
			return 0m;
		}
		if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
		{
			return 0m;
		}
		// if (cardSource is SiyeBite || cardSource is SiyeGei)
		// {
		// 	return base.Amount;
		// }
		// 将cardSource实例化为wyuCard以访问mytype字段
		if (cardSource is wyuCard customCard && customCard.mytype == "siyebite")
		{
			return base.Amount;
		}

		return 0m;
    }


    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == CombatSide.Enemy)
		{
			await PowerCmd.TickDownDuration(this);
		}
	}
}