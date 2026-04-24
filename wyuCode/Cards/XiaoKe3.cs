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
using MegaCrit.Sts2.Core.Entities.Powers;
using wyu.wyuCode.Powers;
using wyu.wyuCode.Enchantments;



namespace wyu.wyuCode.Cards;

public class XiaoKe3():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    
    public override string mytype => "xiaoke";

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("ExtraDamage", 0),
        new DynamicVar("BlocktoDamage", 0.5m),
        new DynamicVar("BlocktoDamagePct", 50m),    // 用于百分比显示
        new DynamicVar("BuffLossTime", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
            HoverTipFactory.FromPower<XiaoKeSealPower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // var extraDamage = 0m;
        // if (cardPlay.Target.Block > 0)
        // {
        //     extraDamage = cardPlay.Target.Block * DynamicVars["BlocktoDamage"].BaseValue;
        //     Log.Info($"小刻要对{cardPlay.Target.Name}造成额外伤害：{extraDamage}");
        // }

        // DynamicVars["ExtraDamage"].BaseValue = extraDamage;

        // 基础伤害：吃力量，不吃格挡。
        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            base.DynamicVars.Damage.BaseValue,
                ValueProp.Unblockable | ValueProp.Move,
                this);

        // // 额外伤害：不吃力量，不吃格挡。
        // if (extraDamage > 0)
        // {
        //     await CreatureCmd.Damage(
        //         choiceContext,
        //         cardPlay.Target,
        //         extraDamage,
        //             ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
        //             this);
        // }

        // 使目标身上的正向 Buff 在本回合内失效，并在其回合结束后恢复。
        await PowerCmd.Apply<XiaoKeSealPower>(cardPlay.Target, DynamicVars["BuffLossTime"].BaseValue, base.Owner.Creature, this);

    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        if (Enchantment is null)
        {
            CardCmd.Enchant<XiaoKeEnchantment>(this, DynamicVars["BlocktoDamagePct"].BaseValue);
        }
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["BlocktoDamage"].UpgradeValueBy(0.1m);
        DynamicVars["BlocktoDamagePct"].UpgradeValueBy(10m);
        DynamicVars["BuffLossTime"].UpgradeValueBy(1m);
        if (Enchantment is XiaoKeEnchantment e)
        {
            e.Amount += 10;
            e.ModifyCard(); // 重新计算附魔和卡牌动态值
        }
    }




}