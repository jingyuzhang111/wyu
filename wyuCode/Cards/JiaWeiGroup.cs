using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using BaseLib.Utils;
using HarmonyLib;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class JiaWeiGroup():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DamageVar(6, ValueProp.Move),
        new PowerVar<SiyeBitePower>("SiyeBitePower", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌
        await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);
		CardPile pile = PileType.Discard.GetPile(base.Owner);
		CardModel cardModel = (await CardSelectCmd.FromSimpleGrid(choiceContext, pile.Cards, base.Owner, prefs)).FirstOrDefault();
		if (cardModel != null)
		{
			await CardPileCmd.Add(cardModel, PileType.Hand);
		}

    }

    // 升级
    protected override void OnUpgrade()
    {
        // AddKeyword(CardKeyword.Retain);
        DynamicVars.Cards.BaseValue += 1;
    }


}