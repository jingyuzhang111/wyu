using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Powers;

namespace wyu.wyuCode.Powers;

public class NiYanT1Power : wyuPower
{
    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // Counter 模式：Amount 作为倒计时计数器
    public override PowerStackType StackType => PowerStackType.Counter;

    // 单例化
    public override bool IsInstanced => true;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            int current = base.Amount - 1;
            if (current <= 0)
            {
                Flash();
                await PowerCmd.Apply<BufferPower>(base.Owner, 1, base.Owner, null);
                SetAmount(4);
            }
            else
            {
                SetAmount(current);
            }
        }
    }
}
