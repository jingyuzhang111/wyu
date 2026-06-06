using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using wyu.wyuCode.Cards;

namespace wyu.wyuCode.Powers;

public class KaMiPower : wyuPower
{
    private bool _fromUpgradedCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Count", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<KaMiAttack>(),
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _fromUpgradedCard = cardSource?.IsUpgraded ?? false;
        return Task.CompletedTask;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner.Player) return;
        await KaMiAttack.CreateZeroCostInHand(player, base.DynamicVars["Count"].IntValue, combatState, _fromUpgradedCard);
        await PowerCmd.TickDownDuration(this);
    }
}
