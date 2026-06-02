using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace wyu.wyuCode.Cards;

/// <summary>
/// Zc325 特殊版君王之剑 VFX，使用 325.png 单张贴图，复用标准君王之剑的轨道/Forge/Attack 动画机制。
/// </summary>
public partial class Zc325SovereignBladeVfx : Node2D
{
    private static readonly string _scenePath = "res://src/zc/zc325_blade.tscn";
    
    private Player _owner;
    private Sprite2D _blade;
    private Tween? _attackTween;
    private Tween? _scaleTween;
    
    private Vector2 _targetOrbitPosition;
    private float _bladeSize;
    private const float _orbitSpeed = 60f;
    private float _orbitProgress;
    private bool _isBehindCharacter;
    private bool _isForging;
    private bool _isAttacking;
    
    public static IEnumerable<string> AssetPaths => new[] { "res://src/zc/325.png" };
    public CardModel Card { get; set; }
    public double OrbitProgress 
    { 
        get => _orbitProgress;
        set
        {
            _orbitProgress = (float)value;
            UpdateOrbitPosition();
        }
    }

    public static Zc325SovereignBladeVfx? Create(CardModel card)
    {
        if (TestMode.IsOn)
            return null;

        PackedScene scene = ResourceLoader.Load<PackedScene>(_scenePath);
        if (scene == null)
            return null;

        Zc325SovereignBladeVfx vfx = scene.Instantiate<Zc325SovereignBladeVfx>();
        vfx.Card = card;
        return vfx;
    }

    public static void PlayForgeVfx(Player player, CardModel card)
    {
        Zc325SovereignBladeVfx? vfx = Create(card);
        if (vfx == null) return;
        
        vfx._owner = player;
        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (nCreature == null) return;

        nCreature.AddChild(vfx);
        vfx.Forge();
    }

    public override void _Ready()
    {
        _blade = GetNode<Sprite2D>("Blade");
        if (_blade == null)
        {
            _blade = new Sprite2D { Name = "Blade", Centered = true };
            AddChild(_blade);
            var tex = ResourceLoader.Load<Texture2D>("res://src/zc/325.png");
            if (tex != null)
                _blade.Texture = tex;
        }
    }

    public override void _Process(double delta)
    {
        if (!_isAttacking && _orbitProgress >= 0)
        {
            UpdateOrbitPosition();
            _orbitProgress = (_orbitProgress + _orbitSpeed * (float)delta) % (Mathf.Tau * 2f);
        }
    }

    private void UpdateOrbitPosition()
    {
        if (_owner?.Creature == null) return;

        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(_owner.Creature);
        if (nCreature == null) return;

        var basePos = nCreature.VfxSpawnPosition;
        float radius = 100f + _bladeSize * 50f;
        
        _targetOrbitPosition = basePos + new Vector2(Mathf.Cos(_orbitProgress), Mathf.Sin(_orbitProgress)) * radius;
        GlobalPosition = _targetOrbitPosition;
        Rotation = _orbitProgress + Mathf.Pi / 2f;

        // 根据 Y 位置判断是否在角色后方
        _isBehindCharacter = GlobalPosition.Y > basePos.Y;
        if (_blade != null)
            _blade.Modulate = new Color(1f, 1f, 1f, _isBehindCharacter ? 0.5f : 1f);
    }

    public void Forge(float chargeFraction = 1f)
    {
        if (_isForging || _isAttacking) return;
        _isForging = true;
        _bladeSize = Mathf.Clamp(chargeFraction, 0f, 1f);

        _scaleTween?.Kill();
        _scaleTween = CreateTween();

        Vector2 pulseScale = Vector2.One * Mathf.Lerp(0.8f, 1.8f, _bladeSize);
        _scaleTween.TweenProperty(this, "scale", pulseScale * 1.3f, 0.1f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
        _scaleTween.Chain().TweenProperty(this, "scale", pulseScale, 0.2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);

        if (_blade != null)
        {
            _scaleTween.Parallel().TweenProperty(_blade, "modulate", new Color(1f, 1f, 0.6f, 1f), 0.08f);
            _scaleTween.Chain().TweenProperty(_blade, "modulate", Colors.White, 0.15f);
        }

        _isForging = false;
    }

    public async Task ForgeAsync(float chargeFraction = 1f)
    {
        Forge(chargeFraction);
        if (_scaleTween != null)
        {
            await ToSignal(_scaleTween, Tween.SignalName.Finished);
        }
    }

    public void Attack(Vector2 targetPos)
    {
        if (_isAttacking) return;
        _isAttacking = true;

        _attackTween?.Kill();
        _attackTween = CreateTween();

        float attackAngle = GetAngleTo(targetPos);
        Vector2 strikeStart = targetPos - (targetPos - GlobalPosition).Normalized() * 80f;

        _attackTween.TweenProperty(this, "rotation", attackAngle, 0.08f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _attackTween.Parallel().TweenProperty(this, "global_position", strikeStart, 0.1f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _attackTween.Chain().TweenProperty(this, "global_position", targetPos, 0.06f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);

        if (_blade != null)
        {
            _attackTween.Chain().TweenProperty(_blade, "modulate:a", 0.7f, 0.04f);
            _attackTween.Chain().TweenProperty(_blade, "modulate:a", 1f, 0.06f);
        }

        _isAttacking = false;
        QueueFree();
    }

    public void RemoveSovereignBlade()
    {
        _attackTween?.Kill();
        _scaleTween?.Kill();
        QueueFree();
    }
}
