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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Godot;

namespace wyu.wyuCode.Cards;

public class KaMiAttack():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("JumpCount", 3m),
        new DynamicVar("rate", 20m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .WithWaitBeforeHit(0.05f,0.1f)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_lightning")
			.Execute(choiceContext);

        for (int i = 0; i < base.DynamicVars["JumpCount"].IntValue; i++)
        {
            Creature enemy = base.Owner.RunState.Rng.CombatTargets.NextItem(base.CombatState.HittableEnemies);
			if (enemy == null)
			{
				continue;
			}
            decimal baseRate = 1 - (base.DynamicVars["rate"].BaseValue / 100m);
            decimal rate = (decimal)Math.Pow((double)baseRate, i+1);
            VfxCmd.PlayOnCreature(enemy, "vfx/vfx_attack_lightning");
            await CreatureCmd.Damage(choiceContext, enemy,
                base.DynamicVars.Damage.BaseValue * rate,
                ValueProp.Unpowered,
                dealer: null, cardSource: null);
        }
    }


    

    // 生成零费消耗副本（给 KaMiPower 每回合调用）
    public static async Task CreateZeroCostInHand(Player owner, int count, CombatState combatState, bool upgraded = false)
    {
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding) return;
        var cards = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            var card = combatState.CreateCard<KaMiAttack>(owner);
            card.EnergyCost.SetThisCombat(0);
            card.ExhaustOnNextPlay = true;
            card.AddKeyword(CardKeyword.Exhaust);
            if (upgraded && card.CurrentUpgradeLevel == 0)
                card.UpgradeInternal();
            cards.Add(card);
        }
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
    }

    // 升级
    protected override void OnUpgrade()
    {

        DynamicVars["rate"].UpgradeValueBy(-10);
        DynamicVars["JumpCount"].UpgradeValueBy(1);
    }


}
