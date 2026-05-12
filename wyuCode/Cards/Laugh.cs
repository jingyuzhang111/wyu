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
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;


namespace wyu.wyuCode.Cards;

public class Laugh():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(2m),
        new PowerVar<VulnerablePower>(1m),
        new DynamicVar("targetDamage", 12m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ManPower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, base.DynamicVars["VulnerablePower"].BaseValue, base.Owner.Creature, this);


        Creature targetCreature = (Creature)cardPlay.Target;
        // 判断目标意图是否为攻击且总伤害低于12点
        var intents = targetCreature.Monster.NextMove.Intents;
        var targets = targetCreature.CombatState.Players.Select(p => p.Creature);
        var attackIntent = intents.OfType<MegaCrit.Sts2.Core.MonsterMoves.Intents.AttackIntent>().FirstOrDefault();
        if (attackIntent == null)
        {
            return;
        }
        int totalDamage = attackIntent.GetTotalDamage(targets, targetCreature);
        // totalDamage 就是意图显示的“总伤害”
        if (totalDamage >= base.DynamicVars["targetDamage"].BaseValue)
        {
            return;
        }
        await PowerCmd.Apply<StrengthPower>(cardPlay.Target, -base.DynamicVars["StrengthPower"].BaseValue, base.Owner.Creature, this);



    }

    // 升级
    protected override void OnUpgrade()
    {

        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
        DynamicVars["targetDamage"].UpgradeValueBy(4m);
    }


}