using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Logging;

namespace wyu.wyuCode.Enchantments;

public sealed class XiaoKeEnchantment : wyuEnchantment
{

	public override bool HasExtraCardText => true;

	public override bool ShowAmount => true;
    
    public override bool IsStackable => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlocktoDamage", 0.5m),
        new DynamicVar("BlocktoDamagePct", 50m),
        new DynamicVar("ExtraDamage", 0m),
    ];

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Log.Info($"小刻附魔生效");

        var extraDamage = 0m;
        if (cardPlay.Target.Block > 0)
        {
            extraDamage = cardPlay.Target.Block * DynamicVars["BlocktoDamage"].BaseValue;
            DynamicVars["ExtraDamage"].BaseValue = extraDamage;
            Log.Info($"小刻要对{cardPlay.Target.Name}造成额外伤害：{extraDamage}");
        }
        if (extraDamage > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                cardPlay.Target,
                extraDamage,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                    base.Card);
        }

	}

	public override void RecalculateValues()
	{
        		base.DynamicVars["BlocktoDamage"].BaseValue = base.Amount / 100m;
        		base.DynamicVars["BlocktoDamagePct"].BaseValue = base.Amount;
	}
}
