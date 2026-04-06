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
using MegaCrit.Sts2.Core.Models;
namespace wyu.wyuCode.Powers;

public class StealDefencePower : wyuPower
{
    // 效果类型Buff, Debuff...
	public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
	DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
		if (target == base.Owner)
		{
			return;
		}

		if (dealer == null)
		{
			return;
		}

		if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered) || result.UnblockedDamage <= 0)
		{
			return;
		}

		decimal blockToGain = result.UnblockedDamage * 0.5m;
		if (blockToGain <= 0m)
		{
			return;
		}

		Flash();
		Log.Info($"偷取防御触发，{dealer} 获得 {blockToGain} 点护甲");
		await CreatureCmd.GainBlock(dealer, blockToGain, ValueProp.Unpowered, null);
		await PowerCmd.Remove(this);
    }


    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{

		await PowerCmd.Remove(this);

	}
}
