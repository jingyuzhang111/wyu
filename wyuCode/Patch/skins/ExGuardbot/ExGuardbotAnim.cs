using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace wyu.wyuCode.Patch;

/// <summary>
/// GUARDBOT 的自定义动画控制器构建器。
/// 由 MonsterAnimatorPatch 统一注册和调用（不再用静态 HarmonyPatch，版本更新更稳）。
/// </summary>
public static class GuardbotAnimatorBuilder
{
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

    public static CreatureAnimator Build(MegaSprite controller)
    {

        // Idle 触发器：空闲状态
        // 触发时机：战斗开始、其他动画结束后自动回到空闲
        AnimState idle = new AnimState("Skill_Idle", isLooping: true);

        AnimState attack = new AnimState("Skill_Loop", isLooping: false);

        AnimState begin = new AnimState("Skill_Begin", isLooping: false);
        AnimState end = new AnimState("Skill_End", isLooping: false);

        AnimState start = new AnimState("Start", isLooping: false);

        // Attack 触发器：攻击时
        // 触发时机：FABRICATOR 执行攻击行动时

        AnimState dead = new AnimState( "Die", isLooping: false);

        // 动画流程链接：定义每个动画之后自动跳转到哪个状态

        start.NextState = begin;
        begin.NextState = idle;
        
        attack.NextState = idle;
        


        // 创建动画控制器并注册所有触发器
        bool hasStart = !string.Equals(start.Id, idle.Id, StringComparison.OrdinalIgnoreCase);
        CreatureAnimator animator = new CreatureAnimator(hasStart ? start : idle, controller);
        animator.AddAnyState("Dead", dead);          // Dead 触发：进入死亡状态
        animator.AddAnyState("Start", start);        // Start 触发：入场动画（若存在）
        animator.AddAnyState("Idle", idle);          // Idle 触发：切回空闲
        animator.AddAnyState("Attack", attack);      // Attack 触发：打断其他动作进行攻击
        animator.AddAnyState("Hit", idle);


        return animator;
    }
}
