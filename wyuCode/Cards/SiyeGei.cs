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

using wyu.wyuCode.Powers;

public class SiyeGei():
    wyuCard(cost: 0, 
    type: CardType.Attack,
    rarity: CardRarity.Token,    // common uncommon rare 三个稀有度
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    public override string[] mytypes => ["siyebite"];
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Shiv];


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SiyeBitePower>(2m),
        new DamageVar(4m, ValueProp.Move),
        new CardsVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SiyeBitePower>(),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)            
            .WithWaitBeforeHit(0.005f,0.01f)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
        // 加两层狼咬
        await PowerCmd.Apply<SiyeBitePower>(cardPlay.Target, base.DynamicVars["SiyeBitePower"].BaseValue, base.Owner.Creature, this);
        // 抽一张牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    public static async Task<CardModel?> CreateInHand(Player owner, CombatState combatState)
	{
		return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
	}

    // 添加到手牌
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
			shivs.Add(combatState.CreateCard<SiyeGei>(owner));
		}
		await CardPileCmd.AddGeneratedCardsToCombat(shivs, PileType.Hand, addedByPlayer: true);
		return shivs;
	}

    // 升级
    protected override void OnUpgrade()
    {
            base.DynamicVars["SiyeBitePower"].BaseValue += 1m;
            base.DynamicVars.Damage.BaseValue += 1m;
    }


}
