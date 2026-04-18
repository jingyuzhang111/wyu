using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Powers;

using MegaCrit.Sts2.Core.Logging;

namespace wyu.wyuCode.Cards;

public class Hearter():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.AllAllies
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new PowerVar<ArmorPower>(10),
        new BlockVar(10, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArmorPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
        foreach (var enemy in base.CombatState!.HittableEnemies)
        {
            await CreatureCmd.GainBlock(enemy, base.DynamicVars.Block, cardPlay);
        }
        await PowerCmd.Apply<ArmorPower>(base.CombatState!.HittableEnemies, base.DynamicVars["ArmorPower"].BaseValue, base.Owner.Creature, this);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars["ArmorPower"].UpgradeValueBy(5m);
    }


}