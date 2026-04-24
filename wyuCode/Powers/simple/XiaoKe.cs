
using MegaCrit.Sts2.Core.Entities.Powers;


namespace wyu.wyuCode.Powers;

public class XiaoKe : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.None;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;


}