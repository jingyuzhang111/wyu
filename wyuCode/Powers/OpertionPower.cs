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

        // 得到当前状态
        var combatState = Traverse.Create(CombatManager.Instance)
        .Field<CombatState>("_state").Value;

        // 在 AfterCurrentHpChanged 里
        var context = new HookPlayerChoiceContext(
            creature.Player,                    // Player owner
            LocalContext.NetId.Value,           // local player net id
            GameActionType.Combat               // game action type
        );

        if (!creature.IsPlayer)
                return;
            
        if (delta >= 0)
            return;  // 只响应扣血

        if (combatState?.CurrentSide != CombatSide.Player)
            return;  // 不是自己回合不管

        // 找 creature 身上的 OpertionPower
        var power = creature.Powers.OfType<OpertionPower>().FirstOrDefault();
        if (power == null) return;

        // 执行抽牌
        await CardPileCmd.Draw(context, Amount, creature.Player);

    }
    // public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	// {
    //     if (side == CombatSide.Player)
    //     {
    //         await PowerCmd.TickDownDuration(this);
    //     }
	// }


}