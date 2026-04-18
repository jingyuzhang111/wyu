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

public class XiaoKeEat():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("ExtraDamage", 0),
        new DynamicVar("BlocktoDamage", 0.5m),
        new DynamicVar("BlocktoDamagePct", 50m),
        new PowerVar<FlexPotionPower>(4m),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var extraDamage = 0m;
        if (cardPlay.Target.Block > 0)
        {
            extraDamage = cardPlay.Target.Block * DynamicVars["BlocktoDamage"].BaseValue;
            DynamicVars["ExtraDamage"].BaseValue = extraDamage;
            Log.Info($"小刻要对{cardPlay.Target.Name}造成额外伤害：{extraDamage}");
        }
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, extraDamage+base.DynamicVars.Damage.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        CardModel cardModel = (await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), context: choiceContext, player: base.Owner, filter: null, source: this)).FirstOrDefault();
		if (cardModel != null)
		{
            if (cardModel is XiaoKeFood)
            {
                await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, cardModel.DynamicVars["StrengthPower"].BaseValue, base.Owner.Creature, null);
            }

			await CardCmd.Exhaust(choiceContext, cardModel);
		}

        await PowerCmd.Apply<FlexPotionPower>(base.Owner.Creature, base.DynamicVars["FlexPotionPower"].BaseValue, base.Owner.Creature, null);


    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["BlocktoDamage"].UpgradeValueBy(0.1m);
        DynamicVars["BlocktoDamagePct"].UpgradeValueBy(10m);
        DynamicVars["FlexPotionPower"].UpgradeValueBy(2m);   
    }


}