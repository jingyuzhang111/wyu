


using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace wyu.wyuCode.Patch;

// 挂在视觉场景根节点：既满足 NCreatureVisuals 类型要求，又可扩展自定义逻辑。
public partial class WyuCreatureVisualBradge : NCreatureVisuals
{
	public override void _Ready()
	{
		base._Ready();
	}

}
