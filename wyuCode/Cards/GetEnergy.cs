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
using System.Buffers;
using MegaCrit.Sts2.Core.Entities.Creatures;


namespace wyu.wyuCode.Cards;

using wyu.wyuCode.Powers;

public class GetEnergy():
    wyuCard(cost: 2, 
    type: CardType.Power,
    rarity: CardRarity.Uncommon,    // common uncommon rare 三个稀有度
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
		await PowerCmd.Apply<PyrePower>(base.Owner.Creature, base.DynamicVars.Energy.BaseValue, base.Owner.Creature, this);
    }

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }


}
