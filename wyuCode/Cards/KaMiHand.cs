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
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Godot;

namespace wyu.wyuCode.Cards;

public class KaMiHand():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard(EnchantedKaMi3Preview()),
    ];

    // 创建一张带"播种"(Sown)附魔的辉煌裂片卡牌模型,用于悬浮提示预览
    private static CardModel EnchantedKaMi3Preview()
    {
        var card = ModelDb.Card<KaMi3>().ToMutable();
        CardCmd.Enchant<Sown>(card, 1m);
        return card;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
      CardKeyword.Exhaust,  
    
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 直接生成 N 张辉煌裂片(KaMi3)到手牌，并给每张附加原版"播种"(Sown)附魔：打出时获得能量
        int count = base.DynamicVars.Cards.IntValue;
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding) return;

        var cards = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            var card = base.CombatState!.CreateCard<KaMi3>(base.Owner);
            CardCmd.Enchant<Sown>(card, 1m); // 播种：打出时获得 1 点能量
            if (cardPlay.Card.IsUpgraded && card.CurrentUpgradeLevel == 0)
                card.UpgradeInternal();
            cards.Add(card);
        }
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, base.Owner);
    }


    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.BaseValue += 1;
    }


}
