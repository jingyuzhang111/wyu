


using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace wyu.wyuCode.Patch;

// 挂在视觉场景根节点：既满足 NCreatureVisuals 类型要求，又可扩展自定义逻辑。
public partial class WyuCreatureVisualBradge : NCreatureVisuals
{
	private MegaSprite? _spine;

	public override void _Ready()
	{
		base._Ready();

		Node2D? visualsNode = GetNodeOrNull<Node2D>("Visuals")
			?? GetNodeOrNull<Node2D>("%Visuals")
			?? GetNodeOrNull<Node2D>("SpineSprite");

		if (visualsNode == null)
		{
			GD.PushWarning($"[{nameof(WyuCreatureVisualBradge)}] Could not find visuals node. Checked: 'Visuals', '%Visuals', 'SpineSprite'.");
			return;
		}

		_spine = new MegaSprite(visualsNode);
	}

	/// <summary>
	/// 播放指定的 Spine 动画
	/// </summary>
	/// <param name="animationName">动画名称</param>
	/// <param name="loop">是否循环播放</param>
	/// <param name="nextAnimation">播完后自动播放的下一个动画（可选）</param>
	/// <param name="nextLoop">下一个动画是否循环</param>
	public void Play(string animationName, bool loop = false, string? nextAnimation = null, bool nextLoop = false)
	{
		if (_spine == null)
		{
			GD.PushWarning($"[{nameof(WyuCreatureVisualBradge)}] Spine controller is not initialized.");
			return;
		}

		var animationState = _spine.GetAnimationState();
		if (animationState == null)
		{
			GD.PushWarning($"[{nameof(WyuCreatureVisualBradge)}] AnimationState is null.");
			return;
		}

		// 设置当前动画
		animationState.SetAnimation(animationName, loop);

		// 如果指定了下一个动画，则加入队列
		if (!string.IsNullOrWhiteSpace(nextAnimation))
		{
			animationState.AddAnimation(nextAnimation, 0f, nextLoop);
		}
	}
}
