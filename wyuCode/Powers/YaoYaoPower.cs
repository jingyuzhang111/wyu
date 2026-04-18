using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;

namespace wyu.wyuCode.Powers;

public sealed class YaoYaoPower : wyuPower
{
    private readonly HashSet<Creature> _triggeredDealersThisTurn = [];

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == base.Owner
			&& dealer != null
			&& (props.IsPoweredAttack() || cardSource is Omnislice)
			&& _triggeredDealersThisTurn.Add(dealer))
		{
			Flash();
            await PowerCmd.Apply<tornadoPower>(dealer, base.Amount, base.Owner, null);
		}
	}

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            _triggeredDealersThisTurn.Clear();
        }

        if (side == Owner.Side)
        {
            return;
        }

        // await PowerCmd.TickDownDuration(this);  // 自动衰减层数，这么好用的东西，我之前都做的什么依托。
        await PowerCmd.Remove(this);

    }
}
