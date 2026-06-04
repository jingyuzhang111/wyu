using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using System.Collections.Concurrent;
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

namespace wyu.wyuCode.Cards;

public class JiLi():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self
    )
{
    private static readonly ConcurrentDictionary<Type, Func<Creature, decimal>> CurrentHpReaders = new();

    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("HpLoss").WithMultiplier(CalcHpLossMultiplier),
        new EnergyVar(2),

        new DynamicVar("percent",0.25m),
        new DynamicVar("percentInt", 25m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        var ownerCreature = Owner.Creature;
        decimal currentHp = ReadCurrentHp(ownerCreature);
        decimal lossHp = currentHp * DynamicVars["percent"].BaseValue;
        Log.Info($"技能效果触发,计算失去{lossHp}点生命");
        await CreatureCmd.Damage(choiceContext, base.Owner.Creature, lossHp, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["percent"].UpgradeValueBy(-0.05m);
        DynamicVars["percentInt"].UpgradeValueBy(-5m);
    }
    private static decimal CalcHpLossMultiplier(CardModel card, Creature? target)
    {
        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null)
            return 0m;

        decimal currentHp = ReadCurrentHp(ownerCreature);
        return Math.Max(0m, currentHp * card.DynamicVars["percent"].BaseValue);
    }


}