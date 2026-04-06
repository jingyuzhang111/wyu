//领袖的馈赠
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

using wyu.wyuCode.Powers;
namespace wyu.wyuCode.Cards;

public class SiyeCall():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AllAllies
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2),
        new PowerVar<SiyeBitePower>(2m),
    ];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];


    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        base.EnergyHoverTip,
    ];



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
        await PowerCmd.Apply<SiyeBitePower>(base.CombatState.HittableEnemies, base.DynamicVars["SiyeBitePower"].BaseValue, base.Owner.Creature, this);

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars["SiyeBitePower"].UpgradeValueBy(2m);
    }


}