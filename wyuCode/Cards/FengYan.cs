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
using MegaCrit.Sts2.Core.Models.Powers;
using System.Buffers;
using MegaCrit.Sts2.Core.Entities.Creatures;


namespace wyu.wyuCode.Cards;

using wyu.wyuCode.Powers;

public class FengYan():
    wyuCard(cost: 1, 
    type: CardType.Power,
    rarity: CardRarity.Rare,    // common uncommon rare 三个稀有度
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IntangiblePower>(1m),
        new PowerVar<FengYanPower>(3m),
        new DamageVar(6m, ValueProp.Move),
        new RepeatVar(3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<FengYanPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<IntangiblePower>(base.Owner.Creature, base.DynamicVars["IntangiblePower"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<FengYanPower>(base.Owner.Creature, base.DynamicVars["FengYanPower"].BaseValue, base.Owner.Creature, this);
        
        for (int i = 0; i < base.DynamicVars["Repeat"].IntValue; i++)
		{
			Creature enemy = base.Owner.RunState.Rng.CombatTargets.NextItem(base.CombatState.HittableEnemies);
			if (enemy == null)
			{
				continue;
			}
			
			await DamageCmd.Attack(base.DynamicVars["Damage"].BaseValue).FromCard(this)
                .WithWaitBeforeHit(0.05f, 0.1f)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
		}
    }

    // 升级
    protected override void OnUpgrade()
    {
            base.DynamicVars["FengYanPower"].BaseValue += 1m;
    }


}
