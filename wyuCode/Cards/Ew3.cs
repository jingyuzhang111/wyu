using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;

namespace wyu.wyuCode.Cards;

public class Ew3():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Token,
    target: TargetType.AnyEnemy
    )
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(30, ValueProp.Move),
        new DynamicVar("ExtraDamage", 15),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
        await DamageCmd.Attack(base.DynamicVars["ExtraDamage"].BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState)    // 目标设为全体敌人
            .Execute(choiceContext);                    // 执行动作
    }


    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, CombatState combatState)
	{
		if (count == 0)
		{
			return Array.Empty<CardModel>();
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> shivs = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			shivs.Add(combatState.CreateCard<Ew3>(owner));
		}
		await CardPileCmd.AddGeneratedCardsToCombat(shivs, PileType.Hand, addedByPlayer: true);
		return shivs;
	}

    public static async Task<IEnumerable<CardModel>> CreateInDraw(Player owner, int count, CombatState combatState)
	{
		if (count == 0)
		{
			return Array.Empty<CardModel>();
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> shivs = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			shivs.Add(combatState.CreateCard<Ew3>(owner));
		}
		await CardPileCmd.AddGeneratedCardsToCombat(shivs, PileType.Draw, addedByPlayer: true);
		return shivs;
	}

    public static async Task<IEnumerable<CardModel>> CreateInDiscard(Player owner, int count, CombatState combatState)
	{
		if (count == 0)
		{
			return Array.Empty<CardModel>();
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> shivs = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			shivs.Add(combatState.CreateCard<Ew3>(owner));
		}
		await CardPileCmd.AddGeneratedCardsToCombat(shivs, PileType.Discard, addedByPlayer: true);
		return shivs;
	}

    

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
        DynamicVars["ExtraDamage"].UpgradeValueBy(5);
    }


}
