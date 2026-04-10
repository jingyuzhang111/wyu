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

public class XingXian():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyPlayer
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
        new CalculatedVar("xingxianHpLoss").WithMultiplier(CalcHpLossMultiplier),
        new BlockVar(12m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        // 这里是, 使用this对cardPlay.Target攻击,先上毒,再攻击
        var ownerCreature = Owner.Creature;
        decimal currentHp = ReadCurrentHp(ownerCreature);
        decimal lossHp = currentHp * 0.25m;
        var xingxianHpLoss = DynamicVars["xingxianHpLoss"];
        Log.Info($"行险效果触发,计算失去{lossHp}点生命");
        await CreatureCmd.Damage(choiceContext, base.Owner.Creature, lossHp, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
    private static decimal CalcHpLossMultiplier(CardModel card, Creature? target)
    {
        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null)
            return 0m;

        decimal currentHp = ReadCurrentHp(ownerCreature);
        return Math.Max(0m, currentHp * 0.25m);
    }

    private static decimal ReadCurrentHp(Creature creature)
    {
        var reader = CurrentHpReaders.GetOrAdd(creature.GetType(), BuildCurrentHpReader);
        return reader(creature);
    }

    private static Func<Creature, decimal> BuildCurrentHpReader(Type type)
    {
        var property = type.GetProperty("CurrentHp") ?? type.GetProperty("CurrentHealth");
        if (property != null)
        {
            return creature =>
            {
                object? value = property.GetValue(creature);
                return value != null ? Convert.ToDecimal(value) : 0m;
            };
        }

        var field = type.GetField("CurrentHp") ?? type.GetField("CurrentHealth");
        if (field != null)
        {
            return creature =>
            {
                object? value = field.GetValue(creature);
                return value != null ? Convert.ToDecimal(value) : 0m;
            };
        }

        return _ => 0m;
    }

}