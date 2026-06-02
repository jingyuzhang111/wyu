using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace wyu.wyuCode.Nodes.Vfx;

public partial class N325Vfx : Node2D
{
    private Sprite2D? _blade;
    private Tween? _tween;
    private Vector2 _orbitCenter;
    private float _orbitPhase;
    private float _orbitRadius = 132f;
    // Lowered orbit speed to make rotation slower and less jarring.
    private float _orbitSpeed = 1.2f;
    private float _bladeSize;
    private bool _isOrbiting;
    private bool _isAttacking;
    private bool _orbitLoopRunning;

    public static N325Vfx Create(CardModel? card = null)
    {
        var node = new N325Vfx();
        node.ProcessMode = ProcessModeEnum.Always;
        node._blade = new Sprite2D
        {
            Name = "Blade",
            Centered = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        node.AddChild(node._blade);

        var tex = ResourceLoader.Load<Texture2D>("res://src/zc/325.png");
        if (tex == null)
            tex = ResourceLoader.Load<Texture2D>("res://src/zc/sovereign_blade_blade.png");
        if (tex != null)
        {
            node._blade.Texture = tex;
            node._blade.Scale = Vector2.One;
        }

        return node;
    }

    public override void _EnterTree()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);
    }

    public void SetBladeTexture(Texture2D tex)
    {
        if (_blade != null)
            _blade.Texture = tex;
    }

    public void BeginOrbit(Vector2 orbitCenter, float orbitPhase, float orbitRadius, float bladeDamage)
    {
        _orbitCenter = orbitCenter;
        _orbitPhase = orbitPhase;
        _orbitRadius = orbitRadius;
        _bladeSize = Mathf.Clamp(Mathf.Lerp(0f, 1f, bladeDamage / 200f), 0f, 1f);
        _isOrbiting = true;
        _isAttacking = false;
        _orbitLoopRunning = true;
        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);
        ApplyOrbitStep(0f);

        _ = OrbitLoopAsync();
    }

    public override void _Process(double delta)
    {
        // The orbit is driven by OrbitLoopAsync so the vfx still moves even if frame processing is skipped.
    }

    private void ApplyOrbitStep(float delta)
    {
        if (!_isOrbiting || _isAttacking)
        {
            return;
        }

        _orbitPhase += _orbitSpeed * delta;
        var orbitPoint = _orbitCenter + new Vector2(Mathf.Cos(_orbitPhase), Mathf.Sin(_orbitPhase)) * _orbitRadius;
        orbitPoint.X = Mathf.Lerp(orbitPoint.X, _orbitCenter.X + 200f, Mathf.Clamp(_bladeSize / 1.25f, 0f, 1f));
        orbitPoint += Vector2.Up * (_bladeSize - 1f) * 30f;

        GlobalPosition = orbitPoint;
        // Keep the blade horizontal while orbiting. Attack will still tween `rotation` to point at the target.
        Rotation = 0f;

        var orbitScale = Vector2.One * Mathf.Lerp(0.9f, 1.18f, _bladeSize);
        if (_blade != null)
        {
            _blade.Scale = orbitScale;
        }
    }

    private async Task OrbitLoopAsync()
    {
        var tree = GetTree() as SceneTree;
        if (tree == null)
        {
            return;
        }

        while (_orbitLoopRunning && IsInsideTree())
        {
            ApplyOrbitStep(0.016f);
            await tree.ToSignal(tree.CreateTimer(0.016f), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public async Task ForgeAsync(float bladeDamage)
    {
        _bladeSize = Mathf.Clamp(Mathf.Lerp(0f, 1f, bladeDamage / 200f), 0f, 1f);

        _tween?.Kill();
        _tween = CreateTween();

        var pulseScale = Vector2.One * Mathf.Lerp(0.9f, 2f, _bladeSize);
        _tween.TweenProperty(this, "scale", pulseScale * 1.2f, 0.05f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _tween.Chain().TweenProperty(this, "scale", pulseScale, 0.3f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);

        if (_blade != null)
        {
            _tween.Parallel().TweenProperty(_blade, "modulate", new Color(1f, 0.95f, 0.8f, 1f), 0.05f);
            _tween.Chain().TweenProperty(_blade, "modulate", Colors.White, 0.18f);
        }

        var tree = GetTree() as SceneTree;
        if (tree != null && _tween != null)
        {
            await tree.ToSignal(_tween, Tween.SignalName.Finished);
        }
    }

    public async Task AttackAsync(SceneTree tree, Vector2 targetPos)
    {
        _orbitLoopRunning = false;
        _isAttacking = true;
        _tween?.Kill();
        _tween = CreateTween();

        var attackStart = new Vector2(GlobalPosition.X - 50f, GlobalPosition.Y);
        var attackAngle = GetAngleTo(targetPos);

        // Slow down rotation tween so the blade turns more gently.
        _tween.TweenProperty(this, "rotation", attackAngle, 0.12f);
        _tween.Parallel().TweenProperty(this, "global_position", attackStart, 0.08f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _tween.Chain().TweenProperty(this, "rotation", attackAngle, 0.06f);
        _tween.Parallel().TweenProperty(this, "global_position", targetPos, 0.05f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);

        if (_blade != null)
        {
            _tween.Chain().TweenProperty(_blade, "modulate:a", 0.92f, 0.03f);
            _tween.Chain().TweenProperty(_blade, "modulate:a", 1f, 0.05f);
        }

        if (tree != null && _tween != null)
        {
            await tree.ToSignal(_tween, Tween.SignalName.Finished);
        }

        _isOrbiting = false;
    }
}
