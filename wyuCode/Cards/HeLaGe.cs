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
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Godot;

using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Events;

namespace wyu.wyuCode.Cards;

public class HeLaGe():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AllEnemies
    )
{
    private static readonly ConcurrentDictionary<Type, Func<Creature, decimal>> CurrentHpReaders = new();


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("HpLoss").WithMultiplier(CalcHpLossMultiplier),
        new DynamicVar("HplossPercent", 20m),
        new DynamicVar("hplosspercent", 0.2m),

        new DamageVar(6m, ValueProp.Move),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var ownerCreature = Owner.Creature;
        decimal currentHp = ReadCurrentHp(ownerCreature);
        decimal lossHp = currentHp * DynamicVars["hplosspercent"].BaseValue;
        await CreatureCmd.Damage(choiceContext, base.Owner.Creature, lossHp, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        // 清除所有负面效果 + 值为负的正面效果
        var debuffs = ownerCreature.Powers
            .Where(p => p.Type == PowerType.Debuff || (p.Type == PowerType.Buff && p.Amount < 0))
            .ToList();
        foreach (var debuff in debuffs)
            await PowerCmd.Remove(debuff);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            // .WithHitCount(2)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState)    // 目标设为全体敌人
            .Execute(choiceContext);                    // 执行动作

    }


    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }


    private static decimal CalcHpLossMultiplier(CardModel card, Creature? target)
    {
        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null)
            return 0m;

        decimal currentHp = ReadCurrentHp(ownerCreature);
        return Math.Max(0m, currentHp * card.DynamicVars["hplosspercent"].BaseValue);
    }

}
