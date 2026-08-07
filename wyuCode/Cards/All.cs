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
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;
using BaseLib.Patches.Features;
using wyu.wyuCode.Patch;

namespace wyu.wyuCode.Cards;

public class All():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: CustomTargetType.Anyone
    ),
    IFullArtCard
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("PowerNum", 10),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var allPowers = ModelDb.AllPowers
            .Where(p => p.GetType().Namespace != "MegaCrit.Sts2.Core.Models.Powers.Mocks")
            .Where(p => p.InstanceType == PowerInstanceType.None)
            .Where(p => p.Type != PowerType.None)
            .ToList();
        var rng = base.Owner.RunState.Rng.CombatTargets;
        rng.Shuffle(allPowers);                       // 原地洗牌,需要 IList
        var picked = allPowers.Take(base.DynamicVars["PowerNum"].IntValue).ToList();

        foreach (var power in picked)
        {
            await PowerCmd.Apply(choiceContext, power.ToMutable(), cardPlay.Target, 1m, base.Owner.Creature, this);
        }

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["PowerNum"].UpgradeValueBy(5m);
    }

}