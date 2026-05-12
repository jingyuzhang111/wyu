using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.GenerateAnimator))]
public static class StabbotAnimatorReplacePatch
{
    private const string TargetMonsterId = "STABBOT";

    /// <summary>
    /// 通过反射获取 CreatureAnimator 当前播放的动画名称
    /// </summary>
    private static string GetCurrentAnimationName(CreatureAnimator animator)
    {
        try
        {
            var field = typeof(CreatureAnimator).GetField("_currentState",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetValue(animator) is AnimState currentState)
            {
                return currentState.Id;
            }
        }
        catch { }
        return "";
    }

    private static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
    {
        if (!string.Equals(__instance.Id.Entry, TargetMonsterId, StringComparison.OrdinalIgnoreCase))
            return true;

        // Idle 触发器：空闲状态
        // 触发时机：战斗开始、其他动画结束后自动回到空闲
        AnimState idle = new AnimState("Idle", isLooping: true);

        AnimState attack = new AnimState("Attack", isLooping: false);

        AnimState start = new AnimState("Start", isLooping: false);

        // Attack 触发器：攻击时
        // 触发时机：FABRICATOR 执行攻击行动时

        AnimState dead = new AnimState( "Die", isLooping: false);

        // 动画流程链接：定义每个动画之后自动跳转到哪个状态

        start.NextState = idle;
        attack.NextState = idle;
        


        // 创建动画控制器并注册所有触发器
        bool hasStart = !string.Equals(start.Id, idle.Id, StringComparison.OrdinalIgnoreCase);
        CreatureAnimator animator = new CreatureAnimator(hasStart ? start : idle, controller);
        animator.AddAnyState("Dead", dead);          // Dead 触发：进入死亡状态
        animator.AddAnyState("Start", start);        // Start 触发：入场动画（若存在）
        animator.AddAnyState("Idle", idle);          // Idle 触发：切回空闲
        animator.AddAnyState("Attack", attack);      // Attack 触发：打断其他动作进行攻击
        animator.AddAnyState("Hit", idle);


        __result = animator;
        return false;
    }


}
