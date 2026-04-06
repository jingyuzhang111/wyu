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
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;
using wyu.wyuCode.Monsters;


namespace wyu.wyuCode.Cards;

public class YinYin3():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    public override bool GainsBlock => true;

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StealDefencePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        if (cardPlay.Target.Side == CombatSide.Enemy)
        {
            YinYinPlaceholderMinion minionModel = (YinYinPlaceholderMinion)ModelDb.Monster<YinYinPlaceholderMinion>().ToMutable();
            minionModel.Leader = cardPlay.Target;
            Creature summonedMinion = base.CombatState!.CreateCreature(minionModel, CombatSide.Enemy, null);
            await CreatureCmd.Add(summonedMinion);
            // 给新召唤单位挂“爪牙”效果，并将效果来源设为当前卡牌指定的敌方目标。
            await PowerCmd.Apply<MinionPower>(summonedMinion, 1m, cardPlay.Target, this);
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState!)    // 目标设为全体敌人
            .Execute(choiceContext);                    // 执行动作
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }


}