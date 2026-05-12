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
using MegaCrit.Sts2.Core.Combat.History.Entries;

namespace wyu.wyuCode.Powers;

public class JianHaoFormPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != base.Owner || card.Type != CardType.Attack)
		{
			return playCount;
		}
		int num = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => e.Actor == base.Owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(base.CombatState));
		if (num >= base.Amount)
		{
			return playCount;
		}
		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		Flash();
		return Task.CompletedTask;
	}
}