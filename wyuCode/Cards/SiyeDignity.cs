//领袖的馈赠
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


namespace wyu.wyuCode.Cards;

public class SiyeDignity():
    wyuCard(cost: 2, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    private bool _is_upgraded = false;

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
    ];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [

    ];


    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SiyeBite>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
		for (int i = 0; i < base.DynamicVars.Cards.IntValue; i++)
		{
			await SiyeBite.CreateInHand(base.Owner, base.CombatState);
			await Cmd.Wait(0.1f);
		}
        if (_is_upgraded)
        {
            await SiyeGei.CreateInHand(base.Owner, base.CombatState);
        }
        
    }

    // 升级
    protected override void OnUpgrade()
    {
        
        ExtraHoverTips.Append(HoverTipFactory.FromCard<SiyeGei>());
        _is_upgraded = true;
    }


}