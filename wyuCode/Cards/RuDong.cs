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


using wyu.wyuCode.Powers;
namespace wyu.wyuCode.Cards;

public class RuDong():
    wyuCard(cost: 1, 
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
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var currentHp = base.Owner.Creature.CurrentHp;
        if (currentHp > DynamicVars["Hp"].BaseValue)
        {
            await CreatureCmd.SetCurrentHp(base.Owner.Creature, DynamicVars["Hp"].BaseValue);
        }
        await PowerCmd.Apply<RuDongPower>(base.Owner.Creature, 1, base.Owner.Creature, this);

    }

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
        base.DynamicVars["Hp"].BaseValue += 4;
    }


}