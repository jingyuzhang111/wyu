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
using System.Buffers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Godot;


namespace wyu.wyuCode.Cards;

using wyu.wyuCode.Powers;

public class Warrior():
    wyuCard(cost: 1, 
    type: CardType.Power,
    rarity: CardRarity.Basic,    // common uncommon rare 三个稀有度
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 添加标签
    protected override HashSet<CardTag> CanonicalTags => [CardTag.None];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        
    ];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        // 这里添加提示, 但与实际效果没有任何关系, 只是添加一些关键词的解释,比如可以给打击挂一个回响形态的解释,但本身没有回响的效果
        HoverTipFactory.FromPower<warriorPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        await CommonActions.Apply<warriorPower>(Owner.Creature, this, 1m);

    }

    // 升级
    protected override void OnUpgrade()
    {
        // 升级后获得固有的词条
        AddKeyword(CardKeyword.Innate);
    }


}
