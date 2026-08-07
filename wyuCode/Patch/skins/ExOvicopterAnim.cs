using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace wyu.wyuCode.Patch;

/// <summary>
/// OVICOPTER 的自定义动画控制器构建器。
/// 由 MonsterAnimatorPatch 统一注册和调用（不再用静态 HarmonyPatch，版本更新更稳）。
/// 
/// 说明：beta 版 Ovicopter 重写了 GenerateAnimator，期望
/// idle_loop/cast/buff/attack/hurt/die/lay 这些动画；而 ovicopter_mod.tscn 模型
/// 没有 buff 动画，这里把 buffTrigger 映射到 cast 作为兜底，避免播放不存在动画导致崩溃。
/// </summary>
public static class OvicopterAnimatorBuilder
{
    public static CreatureAnimator Build(MegaSprite controller)
    {
        // 空闲（模型有 idle_loop）
        AnimState idle = new AnimState("idle_loop", isLooping: true);

        // 施法
        AnimState cast = new AnimState("cast", isLooping: false);
        // 攻击
        AnimState attack = new AnimState("attack", isLooping: false);
        // 受击
        AnimState hurt = new AnimState("hurt", isLooping: false);
        // 死亡
        AnimState die = new AnimState("die", isLooping: false);
        // 下蛋
        AnimState lay = new AnimState("lay", isLooping: false);
        // 模型没有 buff 动画，映射到 cast 兜底
        AnimState buff = new AnimState("cast", isLooping: false);

        // 动画流程：每个动作结束后回到空闲
        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        lay.NextState = idle;
        buff.NextState = idle;

        // 创建动画控制器并注册所有触发器
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", die);            // 死亡
        animator.AddAnyState("Hit", hurt);            // 受击
        animator.AddAnyState("Cast", cast);           // 施法
        animator.AddAnyState("Attack", attack);       // 攻击
        animator.AddAnyState("layTrigger", lay);      // 下蛋
        animator.AddAnyState("buffTrigger", buff);    // 加buff（兜底 cast）

        return animator;
    }
}
