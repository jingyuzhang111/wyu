using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using HarmonyLib;



namespace wyu.wyuCode.Cards;

public class Intimidation():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VulnerablePower>(2m),
        new PowerVar<WeakPower>(1m),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, base.DynamicVars["VulnerablePower"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, base.DynamicVars["WeakPower"].BaseValue, base.Owner.Creature, this);


    }

    // 升级
    protected override void OnUpgrade()
    {
        // AddKeyword(CardKeyword.Retain);
        RemoveKeyword(CardKeyword.Exhaust);
    }


}