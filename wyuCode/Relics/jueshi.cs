using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
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
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	// public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	// {
	// 	if (cardPlay.Card.Owner == base.Owner && cardPlay.Card.Type == CardType.Attack)
	// 	{
	// 		Flash();
	// 		Log.Info("绝食效果触发,回复1点生命");
	// 		await CreatureCmd.Heal(base.Owner.Creature, 1m);
	// 	}
	// }

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

		// 只在伤害敌方目标时触发，避免自己挨打也回血
		if (target.Side == Owner.Creature.Side)
			return;

		// 白名单：只允许 FengYanPower 造成的无牌来源伤害触发回血
		bool fromFengYanPower = cardSource == null && dealer.GetPower<FengYanPower>() != null;

		if (!fromFengYanPower)
			return;

		// 只在实际造成了正伤害时触发
		// 这里属性名请用你本地补全确认：常见是 DamageTaken / FinalDamage / Amount
		if (results.UnblockedDamage <= 0m)
			return;

		Flash();
		await CreatureCmd.Heal(Owner.Creature, 1m);
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