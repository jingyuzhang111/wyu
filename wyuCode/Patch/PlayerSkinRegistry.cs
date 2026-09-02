using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace wyu.wyuCode.Patch;

/// <summary>一条语音：显示台词的本地化键 + 音频文件路径。</summary>
public sealed record PlayerVoiceLine(string LocKey, string AudioPath);

/// <summary>
/// 一套"玩家皮肤"：战斗视觉场景 + 商店/休息处基建场景 + 各自的动画/语音映射。
/// 每套皮肤 = 两套完整 spine 资源（战斗骨骼 &amp; 基建骨骼，做成 .tscn）+ 一套语音表。
/// </summary>
public sealed class PlayerSkin
{
    public required string Id { get; init; }                    // 皮肤唯一 ID，如 "yumaobi"
    public required string DisplayName { get; init; }            // 显示名（按钮上显示）
    public required string CombatVisualScenePath { get; init; }  // 战斗视觉场景(根 Node2D + Visuals SpineSprite)
    public required string LeisureVisualScenePath { get; init; } // 商店/休息处玩家形象场景
    public required string RestSiteAnim { get; init; }           // 休息处待机动画名
    public required string ShopAnim { get; init; }               // 商店待机动画名
    /// <summary>战斗动画映射构建器（AnimState 写法）。null = 使用游戏默认动画路径。</summary>
    public Func<MegaSprite, CreatureAnimator?>? CombatAnimatorFactory { get; init; }
    /// <summary>每套皮肤自己的语音表：按卡牌类型 → 候选语音（台词LocKey + 音频路径）。</summary>
    public required IReadOnlyDictionary<CardType, PlayerVoiceLine[]> VoiceLines { get; init; }
}

/// <summary>
/// 玩家皮肤注册表：管理当前皮肤选择 + 切换。
/// 所有视觉挂载点（战斗/商店/休息处）都从这里读"当前皮肤"，不再硬编码羽毛笔路径。
/// </summary>
public static class PlayerSkinRegistry
{
    private static readonly List<PlayerSkin> _skins = [];
    private static int _currentIndex = 0;

    static PlayerSkinRegistry()
    {
        RegisterDefaultSkins();
    }

    /// <summary>当前选中的皮肤。</summary>
    public static PlayerSkin Current => _skins[_currentIndex];

    /// <summary>所有可用皮肤（只读）。</summary>
    public static IReadOnlyList<PlayerSkin> Skins => _skins;

    /// <summary>皮肤切换事件（参数为新索引）。UI / 刷新逻辑订阅它。</summary>
    public static event Action<int>? SkinChanged;

    /// <summary>注册一套皮肤（可在 Mod 初始化时追加自定义皮肤）。</summary>
    public static void Register(PlayerSkin skin)
    {
        _skins.Add(skin);
    }

    /// <summary>切换到指定索引（越界取模）。</summary>
    public static void SetCurrent(int index)
    {
        if (_skins.Count == 0) return;
        _currentIndex = ((index % _skins.Count) + _skins.Count) % _skins.Count;
        SkinChanged?.Invoke(_currentIndex);
    }

    /// <summary>切到下一套皮肤（循环）。</summary>
    public static PlayerSkin CycleNext()
    {
        SetCurrent(_currentIndex + 1);
        return Current;
    }

    private static void RegisterDefaultSkins()
    {
        // 默认皮肤：羽毛笔(yumaobi2)。现有视觉路径与动画映射保持完全一致。
        Register(new PlayerSkin
        {
            Id = "yumaobi",
            DisplayName = "羽毛笔",
            CombatVisualScenePath = "res://wyu/Scenes/creatureVisual/testVisual.tscn",
            LeisureVisualScenePath = "res://wyu/Scenes/creatureVisual/yumaobi_jijian.tscn",
            RestSiteAnim = "Sit",
            ShopAnim = "Relax",
            CombatAnimatorFactory = BuildYumaobiAnimator,
            VoiceLines = new Dictionary<CardType, PlayerVoiceLine[]>
            {
                [CardType.Attack] =
                [
                    new("WYU-ATTACK.type.attack", "res://src/yumaobi2/audio/作战中1.wav"),
                    new("WYU-ATTACK.type.attack.alt1", "res://src/yumaobi2/audio/作战中2.wav"),
                    new("WYU-ATTACK.type.skill.alt1", "res://src/yumaobi2/audio/行动开始.wav"),
                ],
                [CardType.Skill] =
                [
                    new("WYU-ATTACK.type.skill", "res://src/yumaobi2/audio/作战中4.wav"),
                    new("WYU-ATTACK.type.skill.alt1", "res://src/yumaobi2/audio/行动开始.wav"),
                ],
                [CardType.Power] =
                [
                    new("WYU-ATTACK.type.power", "res://src/yumaobi2/audio/部署1.wav"),
                    new("WYU-ATTACK.type.power.alt1", "res://src/yumaobi2/audio/选中干员2.wav"),
                ],
            },
        });
        // TODO: 追加更多皮肤。每套需要：战斗场景 .tscn + 基建场景 .tscn + 语音表 VoiceLines，
        //       以及（若动画名不同）各自的 CombatAnimatorFactory / RestSiteAnim / ShopAnim。
        // Register(new PlayerSkin { Id="...", DisplayName="...", ... });
    }

    // 羽毛笔 char_421_crow.skel 动画名：idle_loop / attack / hurt / die / Skill_1 / Skill_2_*。
    // 游戏默认名 idle_loop/attack/hurt/die 全部匹配，只有 cast 缺失 → 用 Skill_1 兜底。
    private static CreatureAnimator? BuildYumaobiAnimator(MegaSprite controller)
    {
        AnimState idle   = new AnimState("idle_loop", isLooping: true);
        AnimState attack = new AnimState("attack", isLooping: false);
        AnimState hurt   = new AnimState("hurt", isLooping: false);
        AnimState die    = new AnimState("die", isLooping: false);
        AnimState cast   = new AnimState("Skill_1", isLooping: false);

        attack.NextState = idle;
        hurt.NextState = idle;
        cast.NextState = idle;

        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Cast", cast);
        return animator;
    }
}
