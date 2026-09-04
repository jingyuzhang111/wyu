using System.Linq;
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

public class Harvest():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AllEnemies
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌

        // 记录攻击前在场且活着的敌人（全体攻击要按“本卡击杀几只”结算 buff，不能用单一 target 判断）
        var victims = base.CombatState!.HittableEnemies.Where(e => !e.IsDead).ToList();

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)    // 目标设为全体敌人
            .Execute(choiceContext);                    // 执行动作

        // DamageCmd 执行完时本卡击杀的敌人已完成死亡结算（IsDead 已置位），
        // 对比快照即可得到这次到底杀了几只。
        int kills = victims.Count(e => e.IsDead);

        // 每击杀一个敌人，就单独触发一次 apply（力量 +base、敏捷 +base）
        var self = base.Owner.Creature;
        for (int i = 0; i < kills; i++)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, self, base.DynamicVars["StrengthPower"].BaseValue, self, this);
            await PowerCmd.Apply<DexterityPower>(choiceContext, self, base.DynamicVars["DexterityPower"].BaseValue, self, this);
        }

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }


}