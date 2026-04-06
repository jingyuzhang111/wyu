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

namespace wyu.wyuCode.Powers;

public class tornadoPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发
    private int _turnsUntilTrigger = -1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("_turnsUntilTrigger", 1m),
        new IntVar("round", 1m)
    ];

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.None;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<tornadoPower>()
    ];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
        // 只在首次施加时初始化延迟。叠层时保持原倒计时，不刷新。
        if (_turnsUntilTrigger < 0)
        {
            _turnsUntilTrigger = 1;
            base.DynamicVars["_turnsUntilTrigger"].BaseValue = _turnsUntilTrigger;
            base.DynamicVars["round"].BaseValue = _turnsUntilTrigger + 1;
            Log.Info("tornadoPower首次被施加，延迟1个回合开始触发");
        }
        else
        {
            base.DynamicVars["_turnsUntilTrigger"].BaseValue = _turnsUntilTrigger;
            base.DynamicVars["round"].BaseValue = _turnsUntilTrigger + 1;
            Log.Info($"tornadoPower叠层，不重置倒计时（剩余 {_turnsUntilTrigger} 回合）");
        }
        return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
        // 抽卡之前触发
        Log.Info($"BeforeHandDraw触发，准备造成伤害,层数: {base.Amount}");
		if (base.Amount <= 0)
		{
			await PowerCmd.Remove(this);
			return;
		}
        if (_turnsUntilTrigger > 0)
        {
            _turnsUntilTrigger--;
            base.DynamicVars["_turnsUntilTrigger"].BaseValue = _turnsUntilTrigger;
            base.DynamicVars["round"].BaseValue = _turnsUntilTrigger + 1;
            Log.Info($"tornadoPower效果未到达回合,不触发（剩余 {_turnsUntilTrigger} 回合）");
            return;
        }
		Log.Info($"tornadoPower触发，造成{base.Amount}点伤害并清空层数");
		await CreatureCmd.Damage(choiceContext, base.Owner, base.Amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		await PowerCmd.Remove(this);
	}
}