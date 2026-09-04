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
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Godot;
using MegaCrit.Sts2.Core.Models.Powers;

using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Models.Events;

namespace wyu.wyuCode.Cards;

public class FastHarvest():
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
        new DynamicVar("HplossPercent", 30m),
        new DynamicVar("hplosspercent", 0.3m),

        new DamageVar(8m, ValueProp.Move),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(2m),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        
        var ownerCreature = Owner.Creature;
        decimal currentHp = ReadCurrentHp(ownerCreature);
        decimal lossHp = currentHp * DynamicVars["hplosspercent"].BaseValue;
        await CreatureCmd.Damage(choiceContext, base.Owner.Creature, lossHp, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);

        // 快照攻击前在场的活敌（用于统计本卡击杀数——全体攻击不能用单一 target 判断）
        var victims = base.CombatState!.HittableEnemies.Where(e => !e.IsDead).ToList();

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);

        // DamageCmd 执行完时本卡击杀已完成结算；每击杀一只单独触发一次力/敏
        int kills = victims.Count(e => e.IsDead);
        var self = base.Owner.Creature;
        for (int i = 0; i < kills; i++)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, self, base.DynamicVars["StrengthPower"].BaseValue, self, this);
            await PowerCmd.Apply<DexterityPower>(choiceContext, self, base.DynamicVars["DexterityPower"].BaseValue, self, this);
        }
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
