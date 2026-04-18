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

public class XiaoKeFood():
    wyuCard(cost: 0, 
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
        new IntVar("Hp",1),
        new PowerVar<StrengthPower>(1m),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        await CreatureCmd.Heal(Owner.Creature, base.DynamicVars["Hp"].IntValue);

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["Hp"].UpgradeValueBy(1);
    }


}