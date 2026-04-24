using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;

using MegaCrit.Sts2.Core.Commands;

// 提供数值
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.CardSelection;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class XiaoKeEatKitchen():
    wyuCard(cost: 2, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromCard<XiaoKeFood>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> xiaokes = PileType.Draw.GetPile(base.Owner).Cards// 抽牌堆
            .Concat(PileType.Hand.GetPile(base.Owner).Cards)        // 手牌
            .Concat(PileType.Discard.GetPile(base.Owner).Cards)     // 弃牌堆
            .Where((CardModel c) => c is wyuCard i && i.mytype == "xiaoke")
            .ToList();

        IEnumerable<CardModel> mibings = PileType.Draw.GetPile(base.Owner).Cards// 抽牌堆
            .Concat(PileType.Hand.GetPile(base.Owner).Cards)        // 手牌
            .Concat(PileType.Discard.GetPile(base.Owner).Cards)     // 弃牌堆
            .Where((CardModel c) => c is wyuCard i && i.mytype == "mibing")
            .ToList();

        foreach (CardModel mibing in mibings)
        {
            if (mibing is XiaoKeFood food)
            {
                food.Isxiaokeeat = true;
            }
            await CardCmd.Exhaust(choiceContext, mibing);
        }

        foreach (CardModel xiaoke in xiaokes)
        {
            await CardCmd.Exhaust(choiceContext, xiaoke);
        }
    }

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }


}