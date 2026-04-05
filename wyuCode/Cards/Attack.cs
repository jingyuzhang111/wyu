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


namespace wyu.wyuCode.Cards;

public class Attack():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Basic,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        // new PowerVar<PoisonPower>("PoisonPower", 3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        // 这里添加提示, 但与实际效果没有任何关系, 只是添加一些关键词的解释,比如可以给打击挂一个回响形态的解释,但本身没有回响的效果
        HoverTipFactory.FromPower<DemonFormPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        // 这里是, 使用this对cardPlay.Target攻击,先上毒,再攻击
        if (cardPlay.Target == null)
            return;
        // await CommonActions.Apply<PoisonPower>(cardPlay.Target, this, DynamicVars.Poison.BaseValue);
        await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
    }

    // 升级
    protected override void OnUpgrade()
    {
        // 这里是对毒伤加层
        // DynamicVars.Poison.UpgradeValueBy(2);

        DynamicVars.Damage.UpgradeValueBy(3);
    }


}
