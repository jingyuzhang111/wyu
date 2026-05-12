using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace wyu.wyuCode.Cards;

public class XvlaguAngry() : wyuCard(
    cost: 1,
    type: CardType.Power,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SiyeBitePower>(6m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SiyeBitePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SiyeBitePower>(base.CombatState.HittableEnemies, base.DynamicVars["SiyeBitePower"].BaseValue, base.Owner.Creature, this);
        IEnumerable<CardModel> enumerable = PileType.Draw.GetPile(base.Owner).Cards// 抽牌堆
            .Concat(PileType.Hand.GetPile(base.Owner).Cards)        // 手牌
            .Concat(PileType.Discard.GetPile(base.Owner).Cards)     // 弃牌堆
            .Concat(PileType.Exhaust.GetPile(base.Owner).Cards)     // 消耗堆
            // .Concat(PileType.Play.GetPile(base.Owner).Cards)        // 打出区
            .Where((CardModel c) => c is wyuCard gei && gei.Type is CardType.Attack)
            .ToList();

        foreach (CardModel item in enumerable)
        {
            if (item is wyuCard gei && !gei.mytypes.Contains("siyebite"))
            {
                gei.mytypes = gei.mytypes.Append("siyebite").ToArray();
            }
        }


    }

    protected override void OnUpgrade()
    {
        DynamicVars["SiyeBitePower"].UpgradeValueBy(3m);
    }
    
}
