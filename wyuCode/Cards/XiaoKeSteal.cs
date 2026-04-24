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
using MegaCrit.Sts2.Core.CardSelection;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class XiaoKeSteal():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<XiaoKeEatPower>(1m),

    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<XiaoKe>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 添加塞牌动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        Log.Info("添加一张蜜饼到弃牌堆");
        CardModel food = base.CombatState!.CreateCard<XiaoKeFood>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(food, PileType.Draw, addedByPlayer: true));
        await Cmd.Wait(0.1f);

        Log.Info("添加一张小刻饿到抽牌堆");
        CardModel xiaoke = base.CombatState!.CreateCard<XiaoKeEat>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(xiaoke, PileType.Draw, addedByPlayer: true));
        await Cmd.Wait(0.1f);

        await PowerCmd.Apply<XiaoKeEatPower>(base.Owner.Creature, base.DynamicVars["XiaoKeEatPower"].BaseValue, base.Owner.Creature, null);
    }

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }


}