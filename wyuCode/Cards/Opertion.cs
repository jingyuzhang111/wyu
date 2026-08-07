using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Audio;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using BaseLib.Utils;
using HarmonyLib;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class Opertion():
    wyuCard(cost: 1, 
    type: CardType.Power,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<OpertionPower>(1),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<OpertionPower>(choiceContext, Owner.Creature, base.DynamicVars["OpertionPower"].BaseValue, Owner.Creature, null, false);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["OpertionPower"].BaseValue += 1;
    }

}
