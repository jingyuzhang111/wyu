//领袖的馈赠
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.CardSelection;
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


namespace wyu.wyuCode.Cards;

public class SiyeMoney():
    wyuCard(cost: 0, 
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
        new CardsVar(3),
    ];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [

    ];


    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        // 与隐秘匕首相同的"从手牌选择"方法（FromHand 家族），选 1 张手牌
        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);
        CardModel? cardModel = (await CardSelectCmd.FromHand(choiceContext, base.Owner, prefs, null, this)).FirstOrDefault();

        // 放到抽牌堆顶（不是弃掉）
        if (cardModel != null)
        {
            await CardPileCmd.Add(cardModel, PileType.Draw, CardPilePosition.Top);
        }
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }


}