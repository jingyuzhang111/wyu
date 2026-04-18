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

// 弃用，被力量药水代替

public sealed class LossStrengthPower : wyuPower
{
	private const string _strengthAppliedKey = "StrengthApplied";

	public override PowerType Type => PowerType.None;

	public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthApplied", base.Amount)
    ];

	public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			await PowerCmd.Apply<StrengthPower>(base.Owner, -base.DynamicVars["StrengthApplied"].BaseValue, base.Owner, null, silent: true);
			await PowerCmd.Remove(this);
		}
	}
}