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

using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Godot;

using wyu.wyuCode.Powers;

namespace wyu.wyuCode.Cards;

public class Ew():
    wyuCard(cost: 2, 
    type: CardType.Power,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    public override int MaxUpgradeLevel => 0;


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new DynamicVar("turn", 4),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Ew3>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 添加塞牌动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        await Ew3.CreateInHand(base.Owner, DynamicVars.Cards.IntValue, base.CombatState);

        await Ew3.CreateInDraw(base.Owner, DynamicVars.Cards.IntValue, base.CombatState);

        await Ew3.CreateInDiscard(base.Owner, DynamicVars.Cards.IntValue, base.CombatState);
        
        await PowerCmd.Apply<EwPower>(base.Owner.Creature, DynamicVars["turn"].BaseValue, base.Owner.Creature, null);
        PlayerCmd.EndTurn(base.Owner, canBackOut: false);

    }


}
