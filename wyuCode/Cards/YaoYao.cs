using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Logging;

namespace wyu.wyuCode.Cards;

public class YaoYao() : wyuCard(
    cost: 2,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12, ValueProp.Move),
        new PowerVar<YaoYaoPower>(8),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<YaoYaoPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CommonActions.Apply<YaoYaoPower>(Owner.Creature, this, base.DynamicVars["YaoYaoPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.BaseValue += 3;
        DynamicVars["YaoYaoPower"].BaseValue += 4;
    }

}
