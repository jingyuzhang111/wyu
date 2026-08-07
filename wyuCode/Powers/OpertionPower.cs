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
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;                   // LocalContext
using MegaCrit.Sts2.Core.Entities.Multiplayer;
namespace wyu.wyuCode.Powers;

public class OpertionPower : wyuPower
{
    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // 只关心持有者被扣血(任何回合、任何来源都触发)
        if (creature != Owner) return;
        if (delta >= 0) return;  // 只响应扣血
        if (creature.Player is not { } player) return;

        var context = new HookPlayerChoiceContext(
            player,                             // Player owner
            LocalContext.NetId.GetValueOrDefault(),  // local player net id
            GameActionType.Combat               // game action type
        );

        // 每次扣血抽 [层数] 张牌
        await CardPileCmd.Draw(context, Amount, player);
    }
    // public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	// {
    //     if (side == CombatSide.Player)
    //     {
    //         await PowerCmd.TickDownDuration(this);
    //     }
	// }


}