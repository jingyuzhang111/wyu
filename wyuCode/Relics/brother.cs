using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Relics;

public sealed class Brother : wyuRelic
{
    // 遗物稀有度
	public override RelicRarity Rarity => RelicRarity.Starter;

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (target != Owner.Creature){
			return 1m;
		}
		if (dealer?.Monster is  Exoskeleton){
			return 0.7m;
		}
        if (dealer?.Monster is PhrogParasite){
              return 0.7m;         
        }
        if (dealer?.Monster is Wriggler)
             return 0.7m;          
        if (cardSource is Infection){
            return 0.6m;
        }

		return 1m;
	}
}