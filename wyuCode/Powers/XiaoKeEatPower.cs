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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models.Powers;
using wyu.wyuCode.Cards;

namespace wyu.wyuCode.Powers;

public class XiaoKeEatPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Single;

    private bool _isResolving;

    private async Task TryTriggerFromPlayer(PlayerChoiceContext choiceContext, Player? player)
    {
        if (_isResolving || player == null || player.Creature != base.Owner)
        {
            return;
        }

        var handCards = PileType.Hand.GetPile(player).Cards.ToList();
        bool hasFood = handCards.Any(c => c is wyuCard wc && wc.mytypes.Contains("mibing"));
        bool hasXiaoke = handCards.Any(c => c is wyuCard wc && wc.mytypes.Contains("xiaoke"));

        if (!hasFood || !hasXiaoke)
        {
            return;
        }

        _isResolving = true;
        try
        {
            // 遍历所有蜜饼
            foreach (var card in handCards.Where(c => c is wyuCard wc && wc.mytypes.Contains("mibing")))
            {
                if (card is XiaoKeFood food)
                {
                    food.Isxiaokeeat = true;
                }
                await CardCmd.Exhaust(choiceContext, card);
                
            }

            Flash();
            await PowerCmd.Remove(this);
        }
        finally
        {
            _isResolving = false;
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await TryTriggerFromPlayer(choiceContext, card.Owner);
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        await TryTriggerFromPlayer(choiceContext, card.Owner);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await TryTriggerFromPlayer(choiceContext, card.Owner);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await TryTriggerFromPlayer(choiceContext, cardPlay.Card.Owner);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{

	}
}