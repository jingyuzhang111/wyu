using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Powers;

/// <summary>
/// 心烛(影子)受到的伤害传递给领袖(被选中的怪物)。
/// 挂到召唤出的心烛上；受伤时把未格挡的伤害等量传给 Leader。
/// </summary>
public class YinYinDamageTransferPower : wyuPower
{
    public override PowerType Type => PowerType.Buff;

    // 不堆叠：同一只心烛只会有一个此效果
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 伤害要传递给的领袖。在施加此 Power 时通过 Apply 的 applier 传入。
    /// </summary>
    public Creature? Leader { get; set; }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理心烛自己受到的伤害
        if (target != base.Owner || Leader == null || !Leader.IsAlive)
        {
            return;
        }

        // 只把实际掉血量传给领袖，避免把被格挡部分也转移过去
        if (result.UnblockedDamage <= 0)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Leader, result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, dealer ?? base.Owner, null, null);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        // 只补"心烛被这一击杀死"的场景
        if (target != base.Owner || Leader == null || !Leader.IsAlive)
        {
            return;
        }

        // 未致死 → AfterDamageReceived 已经处理过，这里不能重复传
        if (!result.WasTargetKilled || result.UnblockedDamage <= 0)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Leader, result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, dealer ?? base.Owner, null, null);
    }
}
