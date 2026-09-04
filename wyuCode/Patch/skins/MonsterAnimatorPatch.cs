using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 统一处理怪物 GenerateAnimator 的替换，最稳妥的方案：
/// 1. 在运行时按具体怪物类型解析真正会被调用的 GenerateAnimator 方法
///    （基类 MonsterModel.GenerateAnimator 或各怪物的重写），
///    所以无论版本里某个怪物是否重写了该方法都能正确拦截。
/// 2. 同一个方法只打一次补丁，用字典按怪物 ID 分发到对应的动画构建器。
/// 3. 如果某个怪物类型不再有 GenerateAnimator，会自动跳过，不会像静态
///    [HarmonyPatch(typeof(X), nameof(X.GenerateAnimator))] 那样在版本更新时抛异常。
/// </summary>
public static class MonsterAnimatorPatch
{
    private static readonly Harmony _harmony = new("wyu.animator.patch");

    // 记录已打过补丁的方法，避免同一方法被重复打补丁
    private static readonly HashSet<MethodBase> _patchedMethods = new();

    // 无 override GenerateAnimator 的怪 ID：基类虚方法打不上，需走 NCreature._Ready 兜底接管
    private static readonly HashSet<string> _fallbackToReadyIds = new(StringComparer.OrdinalIgnoreCase);

    // 已被 GenerateAnimator 补丁成功接管的怪物实例（避免 _Ready 兜底重复接管）
    private static readonly ConditionalWeakTable<MonsterModel, object> _intercepted = new();
    private static readonly object _sentinel = new();

    // 怪物 ID（不区分大小写）→ 动画构建器（只接收控制器）
    private static readonly Dictionary<string, Func<MegaSprite, CreatureAnimator>> _builders =
        new(StringComparer.OrdinalIgnoreCase);

    // 怪物 ID（不区分大小写）→ 动画构建器（额外接收 MonsterModel 实例，
    // 用于需要读取怪物状态的构建器，如 TestSubject 的阶段由 Respawns 决定）
    private static readonly Dictionary<string, Func<MonsterModel, MegaSprite, CreatureAnimator>> _modelBuilders =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册某个怪物的动画控制器生成逻辑。应在模组初始化时调用。
    /// </summary>
    /// <typeparam name="TMonster">怪物类型（游戏里的实体类，如 Fabricator）</typeparam>
    /// <param name="monsterId">怪物 ID（如 "FABRICATOR"）</param>
    /// <param name="build">根据 Spine 控制器生成 CreatureAnimator 的构建器</param>
    public static void Register<TMonster>(string monsterId, Func<MegaSprite, CreatureAnimator> build)
        where TMonster : MonsterModel
    {
        _builders[monsterId] = build;
        TryPatch<TMonster>(monsterId);
    }

    /// <summary>
    /// 注册需要读取怪物实例状态的动画构建器（例如 TestSubject 的三阶段由 Respawns 决定）。
    /// </summary>
    public static void RegisterWithModel<TMonster>(string monsterId, Func<MonsterModel, MegaSprite, CreatureAnimator> build)
        where TMonster : MonsterModel
    {
        _modelBuilders[monsterId] = build;
        TryPatch<TMonster>(monsterId);
    }

    private static void TryPatch<TMonster>(string monsterId) where TMonster : MonsterModel
    {
        try
        {
            // 解析该怪物类型真正会被调用的 GenerateAnimator（基类或重写）
            var method = typeof(TMonster).GetMethod(
                nameof(MonsterModel.GenerateAnimator),
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Godot.GD.PushWarning($"[wyu][动画] 找不到 {typeof(TMonster).Name}.GenerateAnimator，已跳过注册。");
                return;
            }
            // 解析到基类 = 该怪没有 override GenerateAnimator。
            // Harmony 打基类虚方法拦不住"无 override 子类实例"的调用（Fabricator 等 override 能拦），
            // 所以这类怪改为由 NCreature._Ready 的 Postfix 兜底接管（登记到 _fallbackToReadyIds）。
            if (method.DeclaringType == typeof(MonsterModel))
            {
                _fallbackToReadyIds.Add(monsterId);
                return;
            }

            // 同一个方法只打一次补丁（例如多个怪物共用基类方法时）
            if (!_patchedMethods.Add(method)) return;

            var prefix = typeof(MonsterAnimatorPatch).GetMethod(
                nameof(Prefix),
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            _harmony.Patch(method,
                prefix: new HarmonyMethod(prefix));
        }
        catch (System.Exception ex)
        {
            Godot.GD.PrintErr($"[wyu][动画] TryPatch {typeof(TMonster).Name} 异常: {ex}");
        }
    }

    public static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
    {
        string id = __instance.Id.Entry;
        if (_modelBuilders.TryGetValue(id, out var modelBuild))
        {
            _intercepted.Remove(__instance);
            _intercepted.Add(__instance, _sentinel);
            __result = modelBuild(__instance, controller);
            return false; // 跳过原 GenerateAnimator
        }
        if (_builders.TryGetValue(id, out var build))
        {
            _intercepted.Remove(__instance);
            _intercepted.Add(__instance, _sentinel);
            __result = build(controller);
            return false; // 跳过原 GenerateAnimator
        }
        return true; // 非目标怪物，走原逻辑
    }

    /// <summary>该怪是否需要走 _Ready 兜底接管（无 override GenerateAnimator）。</summary>
    public static bool IsFallbackToReady(string monsterId) => _fallbackToReadyIds.Contains(monsterId);

    /// <summary>该怪物是否已被 GenerateAnimator 补丁成功接管。</summary>
    public static bool IsIntercepted(MonsterModel monster) => _intercepted.TryGetValue(monster, out _);

    /// <summary>用已注册的构建器为指定怪生成自定义 CreatureAnimator。</summary>
    public static bool TryBuild(string monsterId, MonsterModel model, MegaSprite controller, out CreatureAnimator animator)
    {
        if (_modelBuilders.TryGetValue(monsterId, out var modelBuild))
        {
            animator = modelBuild(model, controller);
            return true;
        }
        if (_builders.TryGetValue(monsterId, out var build))
        {
            animator = build(controller);
            return true;
        }
        animator = null!;
        return false;
    }
}
