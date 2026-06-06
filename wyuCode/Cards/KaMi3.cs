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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Godot;
using wyu.wyuCode.Powers;

namespace wyu.wyuCode.Cards;

public class KaMi3():
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
        new PowerVar<KaMiPower>(3),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<KaMiAttack>(base.IsUpgraded),
    ];

    private LocString testLoc = new LocString("characters", "WYU-ATTACK.test");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        await KaMiAttack.CreateZeroCostInHand(base.Owner, 2, base.CombatState, base.IsUpgraded);
        await PowerCmd.Apply<KaMiPower>(base.Owner.Creature, base.DynamicVars["KaMiPower"].BaseValue, base.Owner.Creature, this);


    }

    // 升级
    protected override void OnUpgrade()
    {
    }


}
