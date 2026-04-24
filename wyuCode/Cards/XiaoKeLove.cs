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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class XiaoKeLove():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Common,
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
        int num = Math.Max(0, 10 - CardPile.GetCards(base.Owner, PileType.Hand).Count());
        List<CardModel> list = new List<CardModel>();
        for (int i = 0; i < num; i++)
        {
            // 创建卡片实例,进行判断是否升级
            CardModel food = base.CombatState!.CreateCard<XiaoKeFood>(base.Owner);
            if (base.IsUpgraded)
            {
                CardCmd.Upgrade(food, CardPreviewStyle.None);
            }
            list.Add(food);
        }

        // 将生成的卡牌添加至战斗中
        await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, addedByPlayer: true);
    }

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }


}