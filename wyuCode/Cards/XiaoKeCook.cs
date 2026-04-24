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

public class XiaoKeCook():
    wyuCard(cost: 1, 
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

        new PowerVar<FlexPotionPower>(2m),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<XiaoKe>(),

    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 得到手牌列表
        List<CardModel> handCards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        int exhaustedCount = handCards.Count;

        // 一个个消耗了
        foreach (CardModel handCard in handCards)
        {
            await CardCmd.Exhaust(choiceContext, handCard);
        }

        if (exhaustedCount > 0)
        {
            // CardModel food = base.CombatState!.CreateCard<XiaoKeFood>(base.Owner);
            for (int i = 0; i < exhaustedCount; i++)
            {   
                CardModel food = base.CombatState!.CreateCard<XiaoKeFood>(base.Owner);
                await CardPileCmd.AddGeneratedCardToCombat(food, PileType.Hand, addedByPlayer: true);
                await Cmd.Wait(0.1f);
            }
            
        }

        await PowerCmd.Apply<FlexPotionPower>(base.Owner.Creature, base.DynamicVars["FlexPotionPower"].BaseValue, base.Owner.Creature, null);

        List<CardModel> DrawCards = PileType.Draw.GetPile(base.Owner).Cards.ToList();
        List<CardModel> xiaokeCards = DrawCards.Where(c => c is wyuCard wc && wc.mytype == "xiaoke").ToList();
        
        if (xiaokeCards.Count <= 0)
        {
            Log.Info("没有小刻，结束");
            return;
        }
        

        List<CardModel> foodCards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        int foodCount = foodCards.Count;

        // 一个个消耗了
        foreach (CardModel handCard in foodCards)
        {
            await CardCmd.Exhaust(choiceContext, handCard);
        }

        await PowerCmd.Apply<FlexPotionPower>(base.Owner.Creature, foodCount * base.DynamicVars["FlexPotionPower"].BaseValue, base.Owner.Creature, null);



        foreach (CardModel card in xiaokeCards)
        {
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            await CardPileCmd.Add(card, PileType.Hand);
        }
    

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["FlexPotionPower"].UpgradeValueBy(1m);   
    }


}