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

public class ArmorPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			Flash();
			await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
            await PowerCmd.Remove(this);
		}
	}
}