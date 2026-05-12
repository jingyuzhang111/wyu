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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;



namespace wyu.wyuCode.Cards;

public class Block():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Basic,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    public override bool GainsBlock => true;

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }


}