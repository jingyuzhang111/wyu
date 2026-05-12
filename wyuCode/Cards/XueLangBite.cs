using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace wyu.wyuCode.Cards;

public class XueLangBite() : wyuCard(
    cost: 2,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy)
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SiyeBitePower>(2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SiyeBitePower>(),
        HoverTipFactory.FromCard<SiyeBite>(base.IsUpgraded),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        // 给目标上咬伤
        await CommonActions.Apply<SiyeBitePower>(cardPlay.Target, this, DynamicVars["SiyeBitePower"].BaseValue);

        IEnumerable<CardModel> enumerable = PileType.Draw.GetPile(base.Owner).Cards// 抽牌堆
            .Concat(PileType.Hand.GetPile(base.Owner).Cards)        // 手牌
            .Concat(PileType.Discard.GetPile(base.Owner).Cards)     // 弃牌堆
            .Concat(PileType.Exhaust.GetPile(base.Owner).Cards)     // 消耗堆
            // .Concat(PileType.Play.GetPile(base.Owner).Cards)        // 打出区
            .Where((CardModel c) => c is wyuCard gei && gei.mytypes.Contains("siyebite"))
            .ToList();
            
		bool flag = true;
		foreach (CardModel item in enumerable)
		{
			if (base.IsUpgraded)
			{
				CardCmd.Upgrade(item, CardPreviewStyle.None);
			}
			await CardCmd.AutoPlay(choiceContext, item, cardPlay.Target, AutoPlayType.Default, skipXCapture: false, !flag);
			flag = false;
		}


    }

    protected override void OnUpgrade()
    {
        DynamicVars["SiyeBitePower"].UpgradeValueBy(1m);
    }
    
}
