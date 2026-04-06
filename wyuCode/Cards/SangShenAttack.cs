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
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;



namespace wyu.wyuCode.Cards;

public class SangShenAttack():
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
        new DamageVar(10, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .WithWaitBeforeHit(0.05f,0.1f)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

        // 如果目标为爪牙，对其主人造成双倍伤害
        var target = cardPlay.Target;
        var minionPower = target.GetPower<MinionPower>();
        
        if (minionPower is not null)
        {
            var leader = minionPower.Applier;
            Log.Info($"爪牙的主人是: {leader?.Name}");
            
            // if (leader is null && target.IsSecondaryEnemy)
            // {
            //     Log.Info("未找到爪牙主人的信息，进行次级敌人判断");
            //     var hittableEnemies = base.CombatState.HittableEnemies;
            //     leader = target.CombatState
            //         .GetTeammatesOf(target)
            //         .FirstOrDefault(c => c != target && c.IsPrimaryEnemy && c.IsAlive && c.IsHittable);
            //     Log.Info($"次级敌人，尝试搜寻主要敌人并直接造成伤害: {leader?.Name}");
            // }
            if (leader is not null && leader.IsPrimaryEnemy)
            {
                Log.Info($"对主人造成双倍伤害: {base.DynamicVars.Damage.BaseValue * 2}");
                await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue * 2).FromCard(this)
                    .WithWaitBeforeHit(0.05f,0.1f)
                    .Targeting(leader)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                Log.Info("双倍伤害已造成");
                return;
            }

            if (target.IsSecondaryEnemy)
            {
                var hittableEnemies = base.CombatState.HittableEnemies;
                leader = target.CombatState
                    .GetTeammatesOf(target)
                    .FirstOrDefault(c => c != target && c.IsPrimaryEnemy && c.IsAlive && c.IsHittable);
                Log.Info($"次级敌人，尝试搜寻主要敌人并直接造成伤害: {leader?.Name}");
                Log.Info($"对主人造成双倍伤害: {base.DynamicVars.Damage.BaseValue * 2}");
                await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue * 2).FromCard(this)
                    .WithWaitBeforeHit(0.05f,0.1f)
                    .Targeting(leader)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                return;
            }
            
        }



    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }


}