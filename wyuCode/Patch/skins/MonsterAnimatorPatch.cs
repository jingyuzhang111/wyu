using System;
using System.Collections.Generic;
using System.Reflection;
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

    // 怪物 ID（不区分大小写）→ 动画构建器
    private static readonly Dictionary<string, Func<MegaSprite, CreatureAnimator>> _builders =
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

        // 解析该怪物类型真正会被调用的 GenerateAnimator（基类或重写）
        var method = typeof(TMonster).GetMethod(
            nameof(MonsterModel.GenerateAnimator),
            BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            Godot.GD.PushWarning($"[wyu][动画] 找不到 {typeof(TMonster).Name}.GenerateAnimator，已跳过注册。");
            return;
        }

        // 同一个方法只打一次补丁（例如多个怪物共用基类方法时）
        if (!_patchedMethods.Add(method)) return;

        _harmony.Patch(method,
            prefix: new HarmonyMethod(typeof(MonsterAnimatorPatch).GetMethod(nameof(Prefix))));
    }

    private static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
    {
        if (_builders.TryGetValue(__instance.Id.Entry, out var build))
        {
            __result = build(controller);
            return false; // 跳过原 GenerateAnimator
        }
        return true; // 非目标怪物，走原逻辑
    }
}
