using Godot;

namespace wyu.wyuCode.Patch;

// Axebot 视觉桥接：当 Spine 切到 die 动画时，触发外挂的精灵序列动画。
public partial class AxebotVisualBridge : WyuCreatureVisualBradge
{
    private const string DeathAnimName = "die";
    private const double DeathSpriteDelaySeconds = 0.85;

    private AnimatedSprite2D? _deathSprite;
    private bool _deathDetected;
    private bool _deathSpriteTriggered;
    private double _deathElapsed;

    public override void _Ready()
    {
        base._Ready();
        _deathSprite = GetNodeOrNull<AnimatedSprite2D>("%Visuals/AnimatedSprite2D");
        if (_deathSprite != null)
        {
            _deathSprite.Stop();
            _deathSprite.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_deathSpriteTriggered || _deathSprite == null || SpineBody == null)
            return;

        string? currentAnim = SpineBody.GetAnimationState()?.GetCurrent(0)?.GetAnimation()?.GetName();
        if (!_deathDetected)
        {
            if (!string.Equals(currentAnim, DeathAnimName, System.StringComparison.OrdinalIgnoreCase))
                return;

            _deathDetected = true;
            _deathElapsed = 0.0;
            GD.Print("[wyu][Axebot] 检测到 die 动画，准备在 0.5 秒后播放精灵图死亡序列。");
            return;
        }

        _deathElapsed += delta;
        if (_deathElapsed < DeathSpriteDelaySeconds)
            return;

        _deathSpriteTriggered = true;
        _deathSprite.Visible = true;
        _deathSprite.Frame = 0;
        _deathSprite.Play("default");
        GD.Print("[wyu][Axebot] die 后 0.5 秒，已触发精灵图死亡序列动画。");
    }
}