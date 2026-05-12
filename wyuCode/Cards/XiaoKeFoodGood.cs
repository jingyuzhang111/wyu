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
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;



namespace wyu.wyuCode.Cards;

public class XiaoKeFoodGood():XiaoKeFood
{

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Hp",2),
        new PowerVar<StrengthPower>(2m),
        new EnergyVar(1),
        new CardsVar(1),

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<XiaoKe>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        await CreatureCmd.Heal(Owner.Creature, base.DynamicVars["Hp"].IntValue);
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        // 过滤掉消耗其他卡牌的时候
        if (!ReferenceEquals(card, this) || !Isxiaokeeat)
        {
            return;
        }

        Isxiaokeeat = false;
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, base.DynamicVars["StrengthPower"].BaseValue, base.Owner.Creature, null);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["Hp"].UpgradeValueBy(2);
    }


}