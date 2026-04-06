using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace wyu.wyuCode.Patch;

public partial class SpineVisualAnimBridge : Node2D
{
	[Export] public NodePath VisualsPath { get; set; } = new("Visuals");

	[Export] public bool AutoPlayIdleOnReady { get; set; } = true;

	private MegaSprite? _spine;

	public override void _Ready()
	{
		Node2D? visualsNode = GetNodeOrNull<Node2D>(VisualsPath)
			?? GetNodeOrNull<Node2D>("%Visuals")
			?? GetNodeOrNull<Node2D>("SpineSprite");

		if (visualsNode == null)
		{
			GD.PushWarning($"[{nameof(SpineVisualAnimBridge)}] Could not find visuals node. Checked: '{VisualsPath}', '%Visuals', 'SpineSprite'.");
			return;
		}

		_spine = new MegaSprite(visualsNode);

		if (AutoPlayIdleOnReady)
		{
			PlayTrigger("Idle");
		}
	}

	public void PlayTrigger(string trigger)
	{
		switch (trigger)
		{
			case "Idle":
				PlayRaw("idle_loop", true);
				break;
			case "Attack":
				PlayRaw("attack", false, nextAnimation: "idle_loop", nextLoop: true);
				break;
			case "Hit":
				PlayRaw("hurt", false, nextAnimation: "idle_loop", nextLoop: true);
				break;
			case "Dead":
				PlayRaw("die", false);
				break;
			case "Cast":
				PlayRaw("cast", false, nextAnimation: "idle_loop", nextLoop: true);
				break;
			case "Revive":
				PlayRaw("revive", false, nextAnimation: "idle_loop", nextLoop: true);
				break;
			default:
				GD.PushWarning($"[{nameof(SpineVisualAnimBridge)}] Unknown trigger '{trigger}'.");
				break;
		}
	}

	public void PlayRaw(string animationName, bool loop, string? nextAnimation = null, bool nextLoop = false)
	{
		if (_spine == null)
		{
			GD.PushWarning($"[{nameof(SpineVisualAnimBridge)}] Spine controller is not initialized.");
			return;
		}

		if (!_spine.HasAnimation(animationName))
		{
			GD.PushWarning($"[{nameof(SpineVisualAnimBridge)}] Missing animation '{animationName}'.");
			return;
		}

		MegaAnimationState state = _spine.GetAnimationState();
		state.SetAnimation(animationName, loop);

		if (!string.IsNullOrWhiteSpace(nextAnimation) && _spine.HasAnimation(nextAnimation))
		{
			state.AddAnimation(nextAnimation, 0f, nextLoop);
		}
	}

	public bool HasAnimation(string animationName)
	{
		return _spine != null && _spine.HasAnimation(animationName);
	}
}
