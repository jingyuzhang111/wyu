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

public class FengYanPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FengYanPower>()
    ];


	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
        await AttackRandomly(player, combatState, choiceContext);
		await PowerCmd.TickDownDuration(this);  // 自动衰减层数，这么好用的东西，我之前都做的什么依托。
	}

    private async Task AttackRandomly(Player player, CombatState combatState, PlayerChoiceContext choiceContext)
    {
        for (int i = 0; i < 3; i++)
		{
            Creature? enemy = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
			if (enemy == null)
			{
				continue;
			}

            await CreatureCmd.Damage(choiceContext, enemy, 3m, ValueProp.Unblockable | ValueProp.Unpowered, base.Owner, null);
        
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		
	}
}