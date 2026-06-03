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

public class NiYanT1():
    wyuCard(cost: 3, 
    type: CardType.Power,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    private bool isUpgraded = false;

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NiYanT1Power>(4),
        new PowerVar<BufferPower>(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NiYanT1Power>(base.Owner.Creature, DynamicVars["NiYanT1Power"].BaseValue, base.Owner.Creature, this);
        if(isUpgraded)
        {
            await PowerCmd.Apply<BufferPower>(base.Owner.Creature, DynamicVars["BufferPower"].BaseValue, base.Owner.Creature, this);
        }
    }

    // 升级
    protected override void OnUpgrade()
    {
        isUpgraded = true;
    }


}