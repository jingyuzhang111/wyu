using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using wyu.wyuCode.Powers;

namespace wyu.wyuCode.Relics;

public sealed class JueShi : wyuRelic
{
    // 遗物稀有度 罕见
	public override RelicRarity Rarity => RelicRarity.Starter;


	// 扣掉回血选项
	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (player != Owner)
		{
			return false;
		}

		var healOptions = options.Where(option => option is HealRestSiteOption).ToList();
		foreach (var option in healOptions)
		{
			options.Remove(option);
		}

		return healOptions.Count > 0;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (base.Owner.Creature.GetPower<warriorPower>() != null)
		{
			return;
		}

		if (cardPlay.Card.Owner != base.Owner || cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}


		if (cardPlay.Card.Owner == base.Owner && cardPlay.Card.Type == CardType.Attack)
		{
			Flash();
			Log.Info("绝食效果触发,回复1点生命");
			await CreatureCmd.Heal(base.Owner.Creature, 1m);
		}
	}

	// 多段加血

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult results,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		// 只处理自己造成的伤害
		if (dealer != Owner.Creature)
			return;

		if (results.UnblockedDamage <= 0m)
			return;
		
		
		if (base.Owner.Creature.GetPower<warriorPower>() == null)
		{
			return;
		}

		// 过滤掉自己掉血的情况
		if (target == Owner.Creature) return;

		// 如果伤害来自封烟, 且为无来源伤害,就加血
		bool fromFengYanPower = cardSource == null && dealer.GetPower<FengYanPower>() != null;
		if (fromFengYanPower && cardSource == null){
			Flash();
			await CreatureCmd.Heal(Owner.Creature, 1m);
			return;
		}
		// 无来源伤害,但不是封烟伤害,过滤
		if (!fromFengYanPower && cardSource == null) return;

		// 只在伤害敌方目标时触发，避免自己挨打也回血
		if (target.Side == Owner.Creature.Side)
			return;

		// 玩家造成了伤害,就回血
		if (cardSource != null && cardSource.Owner == Owner){
			Flash();
			await CreatureCmd.Heal(Owner.Creature, 1m);
		}
		
	}

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		if (creature == base.Owner.Creature)
		{
			return 0m;
		}

		return amount;
	}
}